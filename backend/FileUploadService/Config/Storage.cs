namespace FileUploadService.Configuration
{
    public class StorageSettings
    {
        /// This path is used to store the video files.
        /// For production, it might be something like "/mnt/nfs/videos".
        /// For development, it might be a local directory (e.g., "C:/projects/myapp/videos").
        public string VideosPath { get; set; } = "videos"; // default fallback value
        public string PreviewPath { get; set; } = "previews"; // default fallback value
    }
}
