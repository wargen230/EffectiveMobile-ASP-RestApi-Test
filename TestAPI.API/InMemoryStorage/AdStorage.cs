using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using TestAPI.Interfaces;

namespace TestAPI.InMemoryStorage
{
    public class AdStorage : IAdStorage
    {
        private readonly ILogger<AdStorage> _logger;
        private readonly IMemoryCache _cache;

        private Dictionary<string, HashSet<string>> _declaredLocationToPlatforms = new();

        private Dictionary<string, List<string>> _platformToLocations = new();

        private readonly ReaderWriterLockSlim _rw = new(LockRecursionPolicy.NoRecursion);

        public AdStorage(ILogger<AdStorage> logger, IMemoryCache cache)
        {
            _logger = logger;
            _cache = cache;
        }

        /// <summary>
        /// Load file and fully replace in-memory data.
        /// Format: PlatformName:loc1,loc2,...
        /// Lines with invalid format are skipped (service must not crash).
        /// </summary>
        public async Task LoadFromFileAsync(string path)
        {
            if (!File.Exists(path))
            {
                _logger.LogError("File not found: {Path}", path);
                return;
            }

            string[] lines;
            try
            {
                lines = await File.ReadAllLinesAsync(path);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read file {Path}", path);
                return;
            }

            var newDeclared = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            var newPlatformMap = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            foreach (var raw in lines)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var parts = raw.Split(new[] { ':' }, 2);
                if (parts.Length != 2)
                {
                    _logger.LogWarning("Skipping invalid line (no colon): {Line}", raw);
                    continue;
                }

                var platform = parts[0].Trim();
                if (string.IsNullOrEmpty(platform))
                {
                    _logger.LogWarning("Skipping line with empty platform name: {Line}", raw);
                    continue;
                }

                var locs = parts[1]
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => NormalizeLocation(x.Trim()))
                    .Where(x => !string.IsNullOrEmpty(x) && IsValidLocation(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (locs.Count == 0)
                {
                    _logger.LogWarning("No valid locations for platform {Platform}, line: {Line}", platform, raw);
                    continue;
                }

                newPlatformMap[platform] = locs;

                foreach (var loc in locs)
                {
                    if (!newDeclared.TryGetValue(loc, out var set))
                    {
                        set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        newDeclared[loc] = set;
                    }
                    set.Add(platform);
                }
            }

            _rw.EnterWriteLock();
            try
            {
                _declaredLocationToPlatforms = newDeclared;
                _platformToLocations = newPlatformMap;

                (_cache as MemoryCache)?.Compact(1.0);
            }
            finally
            {
                _rw.ExitWriteLock();
            }

            _logger.LogInformation("Loaded {PlatformCount} platforms, {DeclLocCount} declared locations from {Path}",
                _platformToLocations.Count, _declaredLocationToPlatforms.Count, path);
        }

        /// <summary>
        /// Find platforms that are applicable for the given location.
        /// A platform declared at some prefix of the query location is applicable.
        /// Example: platform declared at /ru/svrd is returned for query /ru/svrd/revda
        /// </summary>
        public List<string> FindPlatforms(string location)
        {
            if (string.IsNullOrWhiteSpace(location)) return new List<string>();
            location = NormalizeLocation(location);
            if (!IsValidLocation(location)) return new List<string>();

            if (_cache.TryGetValue(location, out List<string> cached)) return cached;

            _rw.EnterReadLock();
            try
            {
                var parts = location.Split('/', StringSplitOptions.RemoveEmptyEntries);
                var resultSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                var current = "";
                foreach (var p in parts)
                {
                    current += "/" + p;
                    if (_declaredLocationToPlatforms.TryGetValue(current, out var set))
                        resultSet.UnionWith(set);
                }

                if (location == "/" && _declaredLocationToPlatforms.TryGetValue("/", out var rootSet))
                    resultSet.UnionWith(rootSet);

                var result = resultSet.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();

                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromMinutes(30));
                _cache.Set(location, result, cacheOptions);

                return result;
            }
            finally
            {
                _rw.ExitReadLock();
            }
        }

        public Dictionary<string, List<string>> GetAllPlatforms()
        {
            _rw.EnterReadLock();
            try
            {
                return _platformToLocations.ToDictionary(kv => kv.Key, kv => kv.Value.ToList(), StringComparer.OrdinalIgnoreCase);
            }
            finally { _rw.ExitReadLock(); }
        }

        public int GetDeclaredLocationCount()
        {
            _rw.EnterReadLock();
            try { return _declaredLocationToPlatforms.Count; }
            finally { _rw.ExitReadLock(); }
        }

        private static string NormalizeLocation(string loc)
        {
            if (string.IsNullOrWhiteSpace(loc)) return "";
            loc = loc.Trim();
            while (loc.Contains("//")) loc = loc.Replace("//", "/");
            if (loc.Length > 1 && loc.EndsWith("/")) loc = loc.TrimEnd('/');
            return loc;
        }

        private static bool IsValidLocation(string loc)
        {
            if (string.IsNullOrWhiteSpace(loc)) return false;
            if (!loc.StartsWith("/")) return false;
            if (loc.Length > 1 && loc.EndsWith("/")) return false;
            if (loc.Contains("//")) return false;

            foreach (var ch in loc)
            {
                if (ch == '/' || ch == '_' || ch == '-') continue;
                if (char.IsLetterOrDigit(ch)) continue;
                return false;
            }
            return true;
        }
    }
}
