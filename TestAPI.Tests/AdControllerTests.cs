using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using TestAPI.Controllers;
using TestAPI.Interfaces;
using TestAPI.Models;
using Xunit;

namespace TestAPI.Tests
{
    public class AdPlatformsControllerTests
    {
        private readonly Mock<IAdStorage> _mockStorage;
        private readonly Mock<ILogger<AdPlatformsController>> _mockLogger;
        private readonly AdPlatformsController _controller;

        public AdPlatformsControllerTests()
        {
            _mockStorage = new Mock<IAdStorage>();
            _mockLogger = new Mock<ILogger<AdPlatformsController>>();
            _controller = new AdPlatformsController(_mockStorage.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task Upload_ValidFile_ReturnsOk()
        {
            var fileMock = new Mock<IFormFile>();
            var content = "Platform1: /ru/msk,/ru/spb\nPlatform2: /en/lon";
            var fileName = "test.txt";
            var ms = new MemoryStream(Encoding.UTF8.GetBytes(content));
            fileMock.Setup(f => f.FileName).Returns(fileName);
            fileMock.Setup(f => f.Length).Returns(ms.Length);
            fileMock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), default))
                    .Callback<Stream, CancellationToken>((s, ct) => ms.CopyTo(s))
                    .Returns(Task.CompletedTask);

            var result = await _controller.Upload(fileMock.Object);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("File is loaded", okResult.Value);
            _mockStorage.Verify(s => s.LoadFromFileAsync(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task Upload_EmptyFile_ReturnsBadRequest()
        {
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.Length).Returns(0);

            var result = await _controller.Upload(fileMock.Object);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("File is empty", badRequestResult.Value);
            _mockStorage.Verify(s => s.LoadFromFileAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Upload_NullFile_ReturnsBadRequest()
        {
            var result = await _controller.Upload(null);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("File is empty", badRequestResult.Value);
            _mockStorage.Verify(s => s.LoadFromFileAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void Search_ValidLocation_ReturnsPlatforms()
        {
            var platforms = new List<string> { "Platform1", "Platform2" };
            _mockStorage.Setup(s => s.FindPlatforms("/ru/msk")).Returns(platforms);

            var result = _controller.Search("/ru/msk");

            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedPlatforms = Assert.IsType<List<string>>(okResult.Value);
            Assert.Equal(2, returnedPlatforms.Count);
            _mockStorage.Verify(s => s.FindPlatforms("/ru/msk"), Times.Once);
        }

        [Fact]
        public void Search_EmptyLocation_ReturnsBadRequest()
        {
            var result = _controller.Search("");

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Location is incorrect", badRequestResult.Value);
            _mockStorage.Verify(s => s.FindPlatforms(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void Search_NullLocation_ReturnsBadRequest()
        {
            var result = _controller.Search(null);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Location is incorrect", badRequestResult.Value);
            _mockStorage.Verify(s => s.FindPlatforms(It.IsAny<string>()), Times.Never);
        }
    }
}