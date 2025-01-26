using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace FileUploadService.Models{

public class OutboxMessage
{
    [BsonId]
    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid Id { get; set; } = Guid.NewGuid();

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