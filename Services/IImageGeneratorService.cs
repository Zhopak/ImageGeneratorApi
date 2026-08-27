namespace ImageGeneratorApi.Services;

public interface IImageGeneratorService
{
    Task<byte[]> GenerateImageAsync(string prompt);
}