using Microsoft.AspNetCore.Mvc;
using ProjectHub.Api.Services;

namespace ProjectHub.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AiController : ControllerBase
{
    private readonly AiService _aiService;

    public AiController(AiService aiService)
    {
        _aiService = aiService;
    }

    /// <summary>发送消息给 AI</summary>
    [HttpPost("chat")]
    public async Task<IActionResult> Chat([FromBody] ChatRequest request)
    {
        var response = await _aiService.SendMessage(request.Message, request.ProjectId, request.TaskId);
        return Ok(response);
    }

    /// <summary>获取每日提醒</summary>
    [HttpGet("reminder")]
    public async Task<IActionResult> GetDailyReminder()
    {
        var reminder = await _aiService.GetDailyReminder();
        return Ok(new { content = reminder });
    }
}

public class ChatRequest
{
    public string Message { get; set; } = string.Empty;
    public int? ProjectId { get; set; }
    public int? TaskId { get; set; }
}
