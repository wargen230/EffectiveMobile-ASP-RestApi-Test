using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TestAPI.InMemoryStorage;
using TestAPI.Interfaces;
using TestAPI.Models;

namespace TestAPI.Controllers
{
    [ApiController]
    [Route("api/")]
    public class AdPlatformsController : ControllerBase
    {
        private readonly IAdStorage _adStorage;
        private readonly ILogger<AdPlatformsController> _logger;
        public AdPlatformsController(IAdStorage adStorage, ILogger<AdPlatformsController> logger)
        {
            _adStorage = adStorage;
            _logger = logger;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                _logger.LogError("Attempt to upload an empty file");
                return BadRequest("File is empty");
            }

            var tmp = Path.GetTempFileName();
            using (var stream = System.IO.File.Create(tmp))
            {
                await file.CopyToAsync(stream);
            }

            await _adStorage.LoadFromFileAsync(tmp);

            _logger.LogInformation($"The file was uploaded successfully: {file.FileName}");
            return Ok("File is loaded");
        }

        [HttpGet("search")]
        public IActionResult Search([FromQuery] string location)
        {
            if (string.IsNullOrEmpty(location))
            {
                _logger.LogError("Trying to find data on an empty location");
                return BadRequest("Location is incorrect");
            }

            var platforms = _adStorage.FindPlatforms(location);

            if(platforms.Count == 0)
            {
                _logger.LogWarning($"There are no platforms in this location: {location} , or the platform file has not been uploaded");
                return BadRequest("No one find");
            }

            _logger.LogInformation($"Platforms have been searched by location: {location}");
            return Ok(platforms);
        }
    }
}
