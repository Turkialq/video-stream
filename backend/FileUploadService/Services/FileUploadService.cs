using MongoDB.Driver;
using Newtonsoft.Json;
using FileUploadService.Models;
using Hangfire;


namespace FileUploadService.Services
{
    public class FileUploadService : IFileUploadService
    {
        private readonly IConfiguration _configuration;
        private readonly IMongoClient _mongoClient;
        private readonly IMongoCollection<FileCollection> _fileCollection;
        private readonly IMongoCollection<OutboxMessage> _outboxCollection;

        public FileUploadService(
            IConfiguration configuration,
            IMongoClient mongoClient,                            
            IMongoCollection<FileCollection> fileCollection,
            IMongoCollection<OutboxMessage> outboxCollection)
        {
            _configuration = configuration;
            _mongoClient = mongoClient;
            _fileCollection = fileCollection;
            _outboxCollection = outboxCollection;
        }

        public async Task<string> UploadVideoAsync(IFormFile file, string title)
        {
            // 1) Generate an ID for the new video (Guid) and build the file path
            var videoId = Guid.NewGuid();
            var rootPath = _configuration["StorageSettings:VideosPath"];
            var filePath = Path.Combine(rootPath, $"{videoId}.mp4");

            // 2) Start an empty file path as "not written"
            bool fileCreated = false;

            // 3) Start a Mongo session
            using var session = await _mongoClient.StartSessionAsync();
            session.StartTransaction();

            try
            {
                Directory.CreateDirectory(rootPath); 

                // stream the file into disk

                using (var outputStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    fileCreated = true;

                    using var inputStream = file.OpenReadStream();

                    // A typical buffer size is 80KB, but feel free to adjust
                    byte[] buffer = new byte[81920];
                    int bytesRead;
                    while ((bytesRead = await inputStream.ReadAsync(buffer, 0, buffer.Length)) != 0)
                    {
                        await outputStream.WriteAsync(buffer, 0, bytesRead);
                    }
                }

                var videoDoc = new FileCollection
                {
                    Id = videoId,
                    FilePath = filePath,
                    Title = title,
                    UploadedAt = DateTime.UtcNow
                };

                await _fileCollection.InsertOneAsync(session, videoDoc);

                var outboxMsg = new OutboxMessage
                {
                    Id = Guid.NewGuid(),
                    EventType = "VideoUploaded",
                    PayloadJson = JsonConvert.SerializeObject(new
                    {
                        VideoId = videoId,
                        Title = title,
                        FilePath = filePath
                    }),
                    CreatedAt = DateTime.UtcNow,
                };

                await _outboxCollection.InsertOneAsync(session, outboxMsg);

                await session.CommitTransactionAsync();

                return videoDoc.Id.ToString();
            }
            catch
            {

                await session.AbortTransactionAsync();

                if (fileCreated && File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                throw;
            }
        }
    }
}

