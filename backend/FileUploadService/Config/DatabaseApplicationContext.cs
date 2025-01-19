using MongoDB.Driver;
using FileUploadService.Models;

namespace FileUploadService.Config
{
    public interface IDatabaseApplicationContext
    {
        IMongoCollection<FileCollection> Files { get; }
        IMongoCollection<OutboxMessage> OutboxMessages { get; }

    }

    public class DatabaseApplicationContext : IDatabaseApplicationContext
    {
        private readonly IMongoDatabase _database;

        public DatabaseApplicationContext(IMongoDatabase database)
        {
            _database = database;
            Files = _database.GetCollection<FileCollection>("FileCollection");
            OutboxMessages = _database.GetCollection<OutboxMessage>("OutboxMessages");
        }

        public IMongoCollection<FileCollection> Files { get; }
        public IMongoCollection<OutboxMessage> OutboxMessages { get; }
    }
}
