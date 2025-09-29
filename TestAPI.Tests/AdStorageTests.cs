using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using TestAPI.InMemoryStorage;
using Xunit;

namespace TestAPI.Tests
{
    public class AdStorageTests
    {
        private readonly Mock<ILogger<AdStorage>> _mockLogger;
        private readonly MemoryCache _realCache;
        private readonly AdStorage _storage;

        public AdStorageTests()
        {
            _mockLogger = new Mock<ILogger<AdStorage>>();
            _realCache = new MemoryCache(new MemoryCacheOptions());
            _storage = new AdStorage(_mockLogger.Object, _realCache);
        }

        [Fact]
        public async Task LoadFromFileAsync_ValidFile_LoadsPlatforms()
        {
            var tempFile = Path.GetTempFileName();
            await File.WriteAllTextAsync(tempFile, "Platform1:/ru/msk,/ru/spb\nPlatform2:/en/lon");

            await _storage.LoadFromFileAsync(tempFile);

            var platforms = _storage.GetAllPlatforms();

            Assert.Equal(2, platforms.Count);
            Assert.Contains("Platform1", platforms.Keys);
            Assert.Contains("Platform2", platforms.Keys);

            File.Delete(tempFile);
        }

        [Fact]
        public async Task LoadFromFileAsync_EmptyFile_ResultsEmptyPlatforms()
        {
            var tempFile = Path.GetTempFileName();

            await _storage.LoadFromFileAsync(tempFile);

            var platforms = _storage.GetAllPlatforms();
            Assert.Empty(platforms);

            File.Delete(tempFile);
        }


        [Fact]
        public async Task LoadFromFileAsync_InvalidLines_SkipsThem()
        {
            var tempFile = Path.GetTempFileName();
            await File.WriteAllTextAsync(tempFile, "InvalidLine\nPlatform1:/ru/msk\nAnotherInvalidLine");

            await _storage.LoadFromFileAsync(tempFile);

            var platforms = _storage.GetAllPlatforms();
            Assert.Single(platforms);
            Assert.True(platforms.ContainsKey("Platform1"));

            File.Delete(tempFile);
        }

        [Fact]
        public async Task LoadFromFileAsync_InvalidLocation_SkipsIt()
        {
            var tempFile = Path.GetTempFileName();
            await File.WriteAllTextAsync(tempFile, "Platform1:invalid-location,/ru/msk");

            await _storage.LoadFromFileAsync(tempFile);

            var platforms = _storage.GetAllPlatforms();
            Assert.Single(platforms);
            Assert.Contains("/ru/msk", platforms["Platform1"]);

            File.Delete(tempFile);
        }

        [Fact]
        public async Task FindPlatforms_ValidLocation_ReturnsMatchingPlatforms()
        {
            var tempFile = Path.GetTempFileName();
            await File.WriteAllTextAsync(tempFile,
                "Platform1:/ru/msk,/ru/spb\nPlatform2:/en/lon");

            await _storage.LoadFromFileAsync(tempFile);

            var result = _storage.FindPlatforms("/ru/msk");

            Assert.Contains("Platform1", result);
            Assert.DoesNotContain("Platform2", result);

            File.Delete(tempFile);
        }

        [Fact]
        public void FindPlatforms_InvalidLocation_ReturnsEmptyList()
        {
            var result = _storage.FindPlatforms("invalid-location");
            Assert.Empty(result);
        }

        [Fact]
        public void FindPlatforms_CachesResults()
        {
            var dict = new Dictionary<string, List<string>>
            {
                { "Platform1", new List<string> { "/ru/msk" } }
            };

            var firstCall = _storage.FindPlatforms("/ru/msk");

            var secondCall = _storage.FindPlatforms("/ru/msk");

            Assert.Equal(firstCall, secondCall);
        }

        [Fact]
        public void IsValidLocation_ValidFormat_ReturnsTrue()
        {
            var validLocations = new[] { "/ru/msk", "/en/lon", "/ru", "/123", "/ru_msk" };

            foreach (var location in validLocations)
            {
                Assert.True(_storage.TestIsValidLocation(location));
            }
        }

        [Fact]
        public void IsValidLocation_InvalidChars_ReturnsFalse()
        {
            var invalidLocations = new[] { "ru/msk", "/ru/msk?", "/ru/msk#", "", " ", null, "/ru/msk/" };

            foreach (var location in invalidLocations)
            {
                Assert.False(_storage.TestIsValidLocation(location));
            }
        }
    }

    public static class AdStorageTestExtensions
    {
        public static bool TestIsValidLocation(this AdStorage storage, string location)
        {
            var method = typeof(AdStorage).GetMethod("IsValidLocation",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            return (bool)method.Invoke(null, new object[] { location });
        }
    }
}
