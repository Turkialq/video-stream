using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace FileUploadService.Models{

public class OutboxMessage
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("eventType")]
    public string EventType { get; set; } = string.Empty;

     [BsonElement("payloadJson")]
    public string PayloadJson { get; set; } = string.Empty;

    [BsonElement("uploadedAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreatedAt { get; set; }
            
    [BsonElement("isPublished")]
    public bool IsPublished { get; set; }  = false;
}
}