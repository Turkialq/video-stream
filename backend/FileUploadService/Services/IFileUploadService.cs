namespace FileUploadService.Services
{
    public interface IFileUploadService
    {
        Task<string> UploadVideoAsync(IFormFile file, string? title);
        
    }
}
