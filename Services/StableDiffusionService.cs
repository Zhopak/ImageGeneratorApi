using HPPH.SkiaSharp;
using StableDiffusion.NET;

namespace ImageGeneratorApi.Services;

public class StableDiffusionService : IImageGeneratorService, IDisposable
{
    private readonly DiffusionModel _model;
    private readonly ILogger<StableDiffusionService> _logger;

    public StableDiffusionService(ILogger<StableDiffusionService> logger)
    {
        _logger = logger;

        string modelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Models", "stable-diffusion-v1-5-pruned-emaonly-Q4_0.gguf");
        if (!File.Exists(modelPath))
        {
            throw new FileNotFoundException($"Модель не найдена: {modelPath}");
        }

        _logger.LogInformation("Загрузка модели...");
        _model = new DiffusionModel(DiffusionModelParameter.Create()
            .WithModelPath(modelPath)
            .WithMultithreading());
        _logger.LogInformation("Модель загружена.");
    }

    public async Task<byte[]> GenerateImageAsync(string prompt)
    {
        return await Task.Run(() =>
        {
            try
            {
                var genParams = ImageGenerationParameter.TextToImage(prompt)
                    .WithNegativePrompt("low quality, blurry, ugly, bad anatomy, deformed, watermark, text")
                    .WithSize(512, 512)
                    .WithSteps(25)
                    .WithCfg(9.0f)
                    .WithSeed(42);

                _logger.LogInformation("Генерация для промпта: {Prompt}", prompt);
                var result = _model.GenerateImage(genParams);
                if (result == null)
                    throw new Exception("Результат генерации null");

                byte[] pngBytes = result.ToPng();
                _logger.LogInformation("Генерация завершена, размер {Size} байт", pngBytes.Length);
                return pngBytes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при генерации");
                throw;
            }
        });
    }

    public void Dispose()
    {
        _model?.Dispose();
        _logger.LogInformation("Модель выгружена.");
    }
}