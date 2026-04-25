using System.Text;
using System.Text.Json;

namespace ProjectHub.Api.Services;

public interface IFileLogService
{
    Task WriteAsync(string source, string level, string message, object? data = null, Exception? exception = null, CancellationToken cancellationToken = default);
    Task WriteRawAsync(string source, string level, string message, CancellationToken cancellationToken = default);
}

public sealed class FileLogService : IFileLogService
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<FileLogService> _logger;
    private static readonly SemaphoreSlim FileLock = new(1, 1);

    public FileLogService(IWebHostEnvironment env, ILogger<FileLogService> logger)
    {
        _env = env;
        _logger = logger;
    }

    public Task WriteAsync(string source, string level, string message, object? data = null, Exception? exception = null, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            timestamp = DateTime.Now.ToString("O"),
            source,
            level,
            message,
            data,
            exception = exception?.ToString()
        };

        return AppendLineAsync(payload, cancellationToken);
    }

    public Task WriteRawAsync(string source, string level, string message, CancellationToken cancellationToken = default)
    {
        return AppendLineAsync(new
        {
            timestamp = DateTime.Now.ToString("O"),
            source,
            level,
            message
        }, cancellationToken);
    }

    private async Task AppendLineAsync(object payload, CancellationToken cancellationToken)
    {
        var logsDir = Path.Combine(_env.ContentRootPath, "logs");
        Directory.CreateDirectory(logsDir);
        var filePath = Path.Combine(logsDir, $"app-{DateTime.Now:yyyy-MM-dd}.log");
        var line = JsonSerializer.Serialize(payload);

        await FileLock.WaitAsync(cancellationToken);
        try
        {
            await File.AppendAllTextAsync(filePath, line + Environment.NewLine, Encoding.UTF8, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "写入文件日志失败");
        }
        finally
        {
            FileLock.Release();
        }
    }
}
