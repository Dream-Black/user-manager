using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using ProjectHub.Api.Services;

namespace ProjectHub.Api.Controllers;

[ApiController]
[Route("api/logs")]
public class LogsController : ControllerBase
{
    private readonly IFileLogService _fileLogService;
    private readonly IWebHostEnvironment _env;

    public LogsController(IFileLogService fileLogService, IWebHostEnvironment env)
    {
        _fileLogService = fileLogService;
        _env = env;
    }

    [HttpPost]
    public async Task<IActionResult> Write([FromBody] ClientLogRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Source) || string.IsNullOrWhiteSpace(request.Level) || string.IsNullOrWhiteSpace(request.Message))
            return BadRequest(new { message = "source/level/message 不能为空" });

        await _fileLogService.WriteAsync(request.Source, request.Level, request.Message, request.Data, request.Exception == null ? null : new Exception(request.Exception), cancellationToken);
        return Ok(new { message = "logged" });
    }

    [HttpGet]
    public IActionResult Read([FromQuery] string? date = null)
    {
        var targetDate = string.IsNullOrWhiteSpace(date) ? DateTime.Now : DateTime.Parse(date);
        var filePath = Path.Combine(_env.ContentRootPath, "logs", $"app-{targetDate:yyyy-MM-dd}.log");
        if (!System.IO.File.Exists(filePath)) return Ok(Array.Empty<object>());

        var lines = System.IO.File.ReadAllLines(filePath)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => JsonSerializer.Deserialize<object>(line))
            .ToArray();

        return Ok(lines);
    }
}

public sealed class ClientLogRequest
{
    public string Source { get; set; } = string.Empty;
    public string Level { get; set; } = "info";
    public string Message { get; set; } = string.Empty;
    public object? Data { get; set; }
    public string? Exception { get; set; }
}
