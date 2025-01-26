using MongoDB.Driver;
using Newtonsoft.Json;
using FileUploadService.Models;


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
                // -------------- STEP 1: Write the file to disk --------------
                Directory.CreateDirectory(rootPath); // ensure directory exists

                // If the file is very large, consider streaming in chunks, etc.
                // For this example, we just copy in one shot:
                using (var stream = File.Create(filePath))
                {
                    await file.CopyToAsync(stream);
                }
                fileCreated = true;  // we have created the file

                // -------------- STEP 2: Insert video doc into Mongo --------------
                var videoDoc = new FileCollection
                {
                    // We’re using a Guid as our _id
                    // Make sure your FileCollection model does NOT have [BsonRepresentation(BsonType.ObjectId)] on Id
                    // See Option A from previous discussion
                    Id = videoId,
                    FilePath = filePath,
                    Title = title,
                    UploadedAt = DateTime.UtcNow
                };

                // Use the session-aware version of InsertOne
                await _fileCollection.InsertOneAsync(session, videoDoc);

                // -------------- STEP 3: Insert outbox message --------------
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

                // Insert into outbox, with session
                await _outboxCollection.InsertOneAsync(session, outboxMsg);

                // -------------- COMMIT TRANSACTION --------------
                await session.CommitTransactionAsync();

                // If we get here, all three steps are good. Return the new video ID.
                return videoDoc.Id.ToString();
            }
            catch
            {
                // -------------- ROLLBACK / CLEANUP --------------
                // 1) Abort the Mongo transaction so no documents are saved
                await session.AbortTransactionAsync();

                // 2) Remove the file from disk if we created it
                if (fileCreated && File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                // Re-throw the exception so the caller knows something went wrong
                throw;
            }
        }
    }
}

