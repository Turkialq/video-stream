using MongoDB.Driver;
using Newtonsoft.Json;
using FileUploadService.Models;

namespace FileUploadService.Services
{
    public class FileUploadService : IFileUploadService
    {
        private readonly IMongoDatabase _db;

        public FileUploadService(IMongoDatabase db)
        {
            _db = db;
        }

        public async Task<string> UploadVideoAsync(IFormFile file, string? title)
        {
            // 1) Store file on disk
            var videoId = Guid.NewGuid().ToString("N");
            var filePath = Path.Combine("videos", $"{videoId}.mp4");
            Directory.CreateDirectory("videos");
            using (var stream = File.Create(filePath))
            {
                await file.CopyToAsync(stream);
            }

            // 2) Insert video doc into Mongo
            var videos = _db.GetCollection<FileCollection>("Videos");
            var videoDoc = new FileCollection
            {
                Id = videoId,
                FilePath = filePath,
                Title = title,
                UploadedAt = DateTime.UtcNow
            };
            await videos.InsertOneAsync(videoDoc);

            // 3) Insert outbox message
            var outbox = _db.GetCollection<OutboxMessage>("Outbox");
            var outboxMsg = new OutboxMessage
            {
                Id = Guid.NewGuid().ToString("N"),
                EventType = "VideoUploaded",
                PayloadJson = JsonConvert.SerializeObject(new
                {
                    VideoId = videoId,
                    Title = title,
                    FilePath = filePath
                }),
                CreatedAt = DateTime.UtcNow
            };
            await outbox.InsertOneAsync(outboxMsg);

            return videoId;
        }
    }
}
