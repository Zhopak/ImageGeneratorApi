using Microsoft.AspNetCore.Mvc;
using ImageGeneratorApi.Models;
using ImageGeneratorApi.Services;
using System.Collections.Concurrent;

namespace ImageGeneratorApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ImageController : ControllerBase
{
    private readonly IImageGeneratorService _generator;
    private readonly IBackgroundTaskQueue _taskQueue;
    private readonly ILogger<ImageController> _logger;

    // Временное хранилище для результатов (в реальном проекте заменить на БД или файлы)
    private static readonly ConcurrentDictionary<Guid, TaskCompletionSource<byte[]>> _taskResults = new();

    public ImageController(IImageGeneratorService generator, IBackgroundTaskQueue taskQueue, ILogger<ImageController> logger)
    {
        _generator = generator;
        _taskQueue = taskQueue;
        _logger = logger;
    }

    [HttpPost("generate")]
    public async Task<IActionResult> Generate([FromBody] GenerateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Prompt))
            return BadRequest(new { error = "Prompt не может быть пустым" });

        var taskId = Guid.NewGuid();
        var tcs = new TaskCompletionSource<byte[]>();
        _taskResults[taskId] = tcs;

        // Ставим задачу в очередь
        await _taskQueue.QueueBackgroundWorkItemAsync(async (cancellationToken) =>
        {
            try
            {
                _logger.LogInformation("Обработка задачи {TaskId} с промптом: {Prompt}", taskId, request.Prompt);
                var imageBytes = await _generator.GenerateImageAsync(request.Prompt);
                tcs.SetResult(imageBytes);
                _logger.LogInformation("Задача {TaskId} завершена успешно.", taskId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обработке задачи {TaskId}", taskId);
                tcs.SetException(ex);
            }
        });

        return Accepted(new GenerateResponse
        {
            TaskId = taskId,
            Status = "Queued",
            Message = "Задача поставлена в очередь. Используйте GET /api/image/result/{taskId} для получения результата."
        });
    }

    [HttpGet("result/{taskId}")]
    public async Task<IActionResult> GetResult(Guid taskId)
    {
        if (!_taskResults.TryGetValue(taskId, out var tcs))
            return NotFound(new { error = "Задача с таким ID не найдена" });

        // Проверяем, выполнена ли задача
        if (tcs.Task.IsCompleted)
        {
            try
            {
                var imageBytes = await tcs.Task;
                // Удаляем задачу из словаря, чтобы не занимать память
                _taskResults.TryRemove(taskId, out _);
                return File(imageBytes, "image/png");
            }
            catch (Exception ex)
            {
                _taskResults.TryRemove(taskId, out _);
                return StatusCode(500, new { error = "Ошибка генерации", details = ex.Message });
            }
        }
        else if (tcs.Task.IsFaulted)
        {
            var ex = tcs.Task.Exception?.InnerException;
            _taskResults.TryRemove(taskId, out _);
            return StatusCode(500, new { error = "Ошибка генерации", details = ex?.Message });
        }
        else
        {
            // Задача ещё в процессе
            return Ok(new TaskStatusResponse
            {
                TaskId = taskId,
                Status = "Pending"
            });
        }
    }
}