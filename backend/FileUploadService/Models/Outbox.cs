namespace FileUploadService.Models{

public class OutboxMessage
{
    public string Id { get; set; } = default!;
    public string EventType { get; set; } = default!;
    public string PayloadJson { get; set; } = default!;
    public DateTime CreatedAt { get; set; }

    public bool IsPublished { get; set; } // false => not published, true => published
}
}