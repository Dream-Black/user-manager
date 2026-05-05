using System.Collections.Concurrent;

namespace ProjectHub.Api.Services;

public class ReminderMessage
{
    public string Type { get; set; } = "reminder";
    public int ScheduleId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime ReminderTime { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string? Content { get; set; }
    // 新增：日子详情
    public string? DayContent { get; set; }
    public string? DayStatus { get; set; }
}

public class SseService
{
    private readonly ConcurrentDictionary<string, StreamWriter> _connections = new();
    private readonly ILogger<SseService> _logger;

    public SseService(ILogger<SseService> logger)
    {
        _logger = logger;
    }

    public void AddConnection(string clientId, StreamWriter writer)
    {
        _connections.TryAdd(clientId, writer);
        _logger.LogInformation($"[SSE] 客户端 {clientId} 已连接，当前连接数: {_connections.Count}");
    }

    public void RemoveConnection(string clientId)
    {
        if (_connections.TryRemove(clientId, out var writer))
        {
            _logger.LogInformation($"[SSE] 客户端 {clientId} 已断开，当前连接数: {_connections.Count}");
            try
            {
                writer.Close();
            }
            catch
            {
            }
        }
    }

    public async Task SendMessageToAll(ReminderMessage message)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(message);
        var dataLine = $"data: {json}\n\n";

        _logger.LogInformation($"[SSE] 准备发送消息，连接数: {_connections.Count}");

        foreach (var connection in _connections.ToArray())
        {
            try
            {
                _logger.LogInformation($"[SSE] 正在发送给 {connection.Key}...");
                await connection.Value.WriteLineAsync(dataLine);
                await connection.Value.FlushAsync();
                _logger.LogInformation($"[SSE] 发送成功");
            }
            catch
            {
                RemoveConnection(connection.Key);
            }
        }
    }

    public int GetConnectionCount()
    {
        return _connections.Count;
    }
}