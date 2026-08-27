namespace ImageGeneratorApi.Models;

public class GenerateResponse
{
    public Guid TaskId { get; set; }
    public string Status { get; set; } = "Queued";
    public string Message { get; set; } = "Task has been queued for processing.";
}