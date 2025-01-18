namespace FileUploadService.Models
{
    public class FileCollection
    {
        public string Id { get; set; } = default!;
        public string FilePath { get; set; } = default!;
        public string? Title { get; set; }
        public DateTime UploadedAt { get; set; }
    }
}
