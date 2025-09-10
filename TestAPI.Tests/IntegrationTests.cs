using Microsoft.AspNetCore.Mvc.Testing;
using TestAPI;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace TestAPI.Tests
{
    public class IntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public IntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Search_ValidLocation_ReturnsOk()
        {
            var client = _factory.CreateClient();

            var response = await client.GetAsync("/api/search?location=/ru/msk");

            response.EnsureSuccessStatusCode();
            Assert.Equal("application/json; charset=utf-8", response.Content.Headers.ContentType.ToString());
        }

        [Fact]
        public async Task Search_EmptyLocation_ReturnsBadRequest()
        {
            var client = _factory.CreateClient();

            var response = await client.GetAsync("/api/search?location=");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}