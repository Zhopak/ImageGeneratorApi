namespace ImageGeneratorApi.Models;

public class TaskStatusResponse
{
    public Guid TaskId { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Completed, Failed
    public byte[]? ImageData { get; set; } // base64 или null, если не готово
    public string? ErrorMessage { get; set; }
}