using ImageGeneratorApi.Middleware;
using ImageGeneratorApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Настройка таймаутов
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.ValueLengthLimit = int.MaxValue;
    options.MultipartBodyLengthLimit = int.MaxValue;
});

builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = int.MaxValue;
});

// Добавляем контроллеры
builder.Services.AddControllers();

// Регистрируем сервис генерации (синглтон — модель загружается один раз)
builder.Services.AddSingleton<IImageGeneratorService, StableDiffusionService>();

// Регистрируем очередь задач (синглтон, чтобы она была общей)
builder.Services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
// Регистрируем фоновый сервис, который будет обрабатывать очередь
builder.Services.AddHostedService<QueuedHostedService>();

// Добавляем Swagger
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// Добавляем middleware для аутентификации по API-ключу
app.UseMiddleware<ApiKeyAuthMiddleware>();

app.UseAuthorization();
app.MapControllers();

app.Run();