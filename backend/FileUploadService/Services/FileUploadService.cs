using MongoDB.Driver;
using Newtonsoft.Json;
using FileUploadService.Models;

namespace FileUploadService.Services
{
    public class FileUploadService : IFileUploadService
    {
       
        private readonly IMongoCollection<FileCollection> _fileCollection;
        private readonly IMongoCollection<OutboxMessage> _outboxCollection;
      

        public FileUploadService(IMongoCollection<FileCollection> collection, IMongoCollection<OutboxMessage> outboxCollection)
        {
            _fileCollection = collection;
            _outboxCollection = outboxCollection;
        }
        

        public async Task<string> UploadVideoAsync(IFormFile file, string title)
        {
            // 1) Store file on disk
            var videoId = Guid.NewGuid().ToString("N");
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "videos",$"{videoId}.mp4");
            Directory.CreateDirectory("videos");
            using (var stream = File.Create(filePath))
            {
                await file.CopyToAsync(stream);
            }

            // 2) Insert video doc into Mongo
            var videoDoc = new FileCollection
            {
                Id = videoId,
                FilePath = filePath,
                Title = title,
                UploadedAt = DateTime.UtcNow
            };
            await _fileCollection.InsertOneAsync(videoDoc);

            // 3) Insert outbox message
            var outboxMsg = new OutboxMessage
            {
                Id = Guid.NewGuid().ToString("N"),
                EventType = "VideoUploaded",
                PayloadJson = JsonConvert.SerializeObject(new
                {
                    VideoId = videoId,
                    Title = title,
                    FilePath = filePath,
                }),
                CreatedAt = DateTime.UtcNow,
            };
            await _outboxCollection.InsertOneAsync(outboxMsg);

            return videoId;
        }
    }
}
