using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace FileUploadService.Models
{
    public class FileCollection
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        [BsonElement("filePath")]
        public string FilePath { get; set; } = string.Empty;

        [BsonElement("title")]
        public string Title { get; set; } = string.Empty;

        [BsonElement("uploadedAt")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime UploadedAt { get; set; }

        [BsonElement("isPreviewAvailable")]
        public bool IsPreviewAvailable { get; set; } = false;

        
    }
}
