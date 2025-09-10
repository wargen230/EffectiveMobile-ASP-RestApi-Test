using Moq;
using TestAPI.InMemoryStorage;
using Xunit;

namespace TestAPI.Tests
{
    public class LocationValidationTests
    {
        [Theory]
        [InlineData("/ru/msk", true)]
        [InlineData("/en/lon", true)]
        [InlineData("/123", true)]
        [InlineData("/ru_msk", true)]
        [InlineData("ru/msk", false)]
        [InlineData("/ru/msk/", false)]
        [InlineData("/ru/msk?", false)]
        [InlineData("/ru/msk#", false)]
        [InlineData("", false)]
        [InlineData(" ", false)]
        [InlineData(null, false)]
        public void IsValidLocation_VariousInputs_ReturnsExpectedResult(string location, bool expected)
        {
            var storage = new AdStorage(Mock.Of<Microsoft.Extensions.Logging.ILogger<AdStorage>>(),
                new Microsoft.Extensions.Caching.Memory.MemoryCache(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions()));

            var result = storage.TestIsValidLocation(location);

            Assert.Equal(expected, result);
        }
    }
}