using Microsoft.AspNetCore.Mvc;
using FileUploadService.Services;

namespace FileUploadService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VideoController : ControllerBase
    {
        private readonly IFileUploadService _fileUploadService;

        public VideoController(IFileUploadService  fileUploadService)
        {
            _fileUploadService = fileUploadService;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadVideo([FromForm] string title)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                return BadRequest("Title is required");
            }

            if (!Request.HasFormContentType)
            {
                return BadRequest("Content-Type must be multipart/form-data");
            }

            var form = Request.Form;
            var file = form.Files["file"];
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded");
            }
            
            var result = await _fileUploadService.UploadVideoAsync(file, title);

            return Ok(new { VideoId = result });
        }
    }
}
