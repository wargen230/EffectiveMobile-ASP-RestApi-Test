using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using TestAPI.Interfaces;
using TestAPI.Models;

namespace TestAPI.InMemoryStorage
{
    public class AdStorage : IAdStorage
    {
        private readonly ILogger<AdStorage> _logger;
        private readonly IMemoryCache _cache;

        public List<AdModel> Platforms { get; set; } = new();

        public AdStorage(ILogger<AdStorage> logger, IMemoryCache cache)
        {
            _logger = logger;
            _cache = cache;
        }
        public async Task LoadFromFileAsync(string path)
        {
            var lines = await File.ReadAllLinesAsync(path);
            if (lines.Count() == 0)
            {
                _logger.LogError("File is empty");
                return;
            }
            var _platforms = new List<AdModel>();
            int indexer = 0;

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    _logger.LogWarning($"Line {indexer} is empty, skipping.");
                    indexer++;
                    continue;
                }

                var parts = line.Split(':', 2);
                if (parts.Length != 2)
                {
                    _logger.LogWarning($"Line {indexer} missing ':' separator, skipping.");
                    indexer++;
                    continue;
                }

                var name = parts[0].Trim();
                if (string.IsNullOrEmpty(name))
                {
                    _logger.LogWarning($"Line {indexer} has empty platform name, skipping.");
                    indexer++;
                    continue;
                }

                if (_platforms.Any(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                {
                    _logger.LogWarning($"Duplicate platform name '{name}' at line {indexer}, skipping.");
                    indexer++;
                    continue;
                }

                var locations = parts[1]
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(l => l.Trim())
                    .Where(l => IsValidLocation(l))
                    .Distinct()
                    .ToList();

                if (locations.Count == 0)
                {
                    _logger.LogWarning($"Line {indexer} skipped: no valid locations.");
                    indexer++;
                    continue;
                }

                _platforms.Add(new AdModel
                {
                    Name = name,
                    Locations = locations
                });

                indexer++;
            }

            Platforms = _platforms;
            _cache.Dispose();
            _logger.LogInformation($"Loaded {Platforms.Count} ad platforms from file '{path}'.");
        }

        public List<string> FindPlatforms(string location)
        {
            if (!IsValidLocation(location))
            {
                _logger.LogWarning($"Invalid location request: '{location}'");
                return new List<string>();
            }

            if (_cache.TryGetValue(location, out List<string> cached))
            {
                return cached;
            }

            var result = new List<string>();

            foreach (var platform in Platforms)
            {
                foreach (var loc in platform.Locations)
                {
                    if (loc.StartsWith(location))
                    {
                        result.Add(platform.Name);
                        break;
                    }
                }
            }

            var distinct = result.Distinct().ToList();
            _cache.Set(location, distinct, TimeSpan.FromMinutes(10));
            _logger.LogInformation($"Returned {distinct.Count} platforms for location '{location}'");

            return distinct;
        }

        private bool IsValidLocation(string location)
        {
            if (string.IsNullOrWhiteSpace(location)) return false;
            if (!location.StartsWith("/")) return false;

            foreach (var ch in location)
            {
                if (!char.IsLetterOrDigit(ch) && ch != '/' && ch != '_') return false;
                if(location.EndsWith("/") && location.Length > 1) return false;
            }
            return true;
        }
    }
}
