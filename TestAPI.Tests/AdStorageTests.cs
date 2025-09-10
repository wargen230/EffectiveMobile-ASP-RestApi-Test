using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using TestAPI.InMemoryStorage;
using TestAPI.Models;
using Xunit;

namespace TestAPI.Tests
{
    public class AdStorageTests
    {
        private readonly Mock<ILogger<AdStorage>> _mockLogger;
        private readonly Mock<IMemoryCache> _mockCache;
        private readonly AdStorage _storage;
        private readonly MemoryCache _realCache;

        public AdStorageTests()
        {
            _mockLogger = new Mock<ILogger<AdStorage>>();
            _mockCache = new Mock<IMemoryCache>();
            _realCache = new MemoryCache(new MemoryCacheOptions());
            _storage = new AdStorage(_mockLogger.Object, _realCache);
        }

        [Fact]
        public async Task LoadFromFileAsync_ValidFile_LoadsPlatforms()
        {
            var tempFile = Path.GetTempFileName();
            await File.WriteAllTextAsync(tempFile, "Platform1: /ru/msk,/ru/spb\nPlatform2: /en/lon");

            await _storage.LoadFromFileAsync(tempFile);
            
            Assert.Equal(2, _storage.Platforms.Count);
            Assert.Contains(_storage.Platforms, p => p.Name == "Platform1");
            Assert.Contains(_storage.Platforms, p => p.Name == "Platform2");

            File.Delete(tempFile);
        }

        [Fact]
        public async Task LoadFromFileAsync_EmptyFile_LogsWarning()
        {
            var tempFile = Path.GetTempFileName();

            await _storage.LoadFromFileAsync(tempFile);

            Assert.Empty(_storage.Platforms);
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("File is empty")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.AtLeastOnce);

            File.Delete(tempFile);
        }

        [Fact]
        public async Task LoadFromFileAsync_InvalidLines_SkipsThem()
        {
            var tempFile = Path.GetTempFileName();
            await File.WriteAllTextAsync(tempFile, "InvalidLine\nPlatform1: /ru/msk\nAnotherInvalidLine");

            await _storage.LoadFromFileAsync(tempFile);

            Assert.Single(_storage.Platforms);
            Assert.Equal("Platform1", _storage.Platforms[0].Name);

            File.Delete(tempFile);
        }

        [Fact]
        public async Task LoadFromFileAsync_DuplicateNames_SkipsDuplicates()
        {
            var tempFile = Path.GetTempFileName();
            await File.WriteAllTextAsync(tempFile, "Platform1: /ru/msk\nPlatform1: /en/lon");

            await _storage.LoadFromFileAsync(tempFile);

            Assert.Single(_storage.Platforms);

            File.Delete(tempFile);
        }

        [Fact]
        public void FindPlatforms_ValidLocation_ReturnsMatchingPlatforms()
        {
            _storage.Platforms = new List<AdModel>
            {
                new AdModel { Name = "Platform1", Locations = new List<string> { "/ru/msk", "/ru/spb" } },
                new AdModel { Name = "Platform2", Locations = new List<string> { "/en/lon" } }
            };

            var result = _storage.FindPlatforms("/ru/msk");

            Assert.Single(result);
            Assert.Contains("Platform1", result);
        }

        [Fact]
        public void FindPlatforms_LocationWithMultiplePlatforms_ReturnsDistinct()
        {
            // Arrange
            _storage.Platforms = new List<AdModel>
            {
                new AdModel { Name = "Platform1", Locations = new List<string> { "/ru/msk", "/ru/spb" } },
                new AdModel { Name = "Platform2", Locations = new List<string> { "/ru/msk" } }
            };

            // Act
            var result = _storage.FindPlatforms("/ru/msk");

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Contains("Platform1", result);
            Assert.Contains("Platform2", result);
        }

        [Fact]
        public void FindPlatforms_InvalidLocation_ReturnsEmptyList()
        {
            _storage.Platforms = new List<AdModel>
            {
                new AdModel { Name = "Platform1", Locations = new List<string> { "/ru/msk" } }
            };
            var result = _storage.FindPlatforms("invalid-location");

            Assert.Empty(result);
        }

        [Fact]
        public void FindPlatforms_CachedLocation_ReturnsFromCache()
        {
            var cachedResult = new List<string> { "CachedPlatform" };
            var cacheKey = "/cached/location";

            var mockCacheEntry = new Mock<ICacheEntry>();
            _mockCache.Setup(m => m.CreateEntry(It.IsAny<object>())).Returns(mockCacheEntry.Object);

            var storageWithMockCache = new AdStorage(_mockLogger.Object, _mockCache.Object);
            storageWithMockCache.Platforms = new List<AdModel>();

            object cacheValue = cachedResult;
            _mockCache
                .Setup(x => x.TryGetValue(cacheKey, out cacheValue))
                .Returns(true);

            var result = storageWithMockCache.FindPlatforms(cacheKey);

            Assert.Single(result);
            Assert.Equal("CachedPlatform", result[0]);
            _mockCache.Verify(x => x.TryGetValue(cacheKey, out It.Ref<object>.IsAny), Times.Once);
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
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return (bool)method.Invoke(storage, new object[] { location });
        }
    }
}