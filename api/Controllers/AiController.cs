using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectHub.Api.Data;
using ProjectHub.Api.Models;
using ProjectHub.Api.Services;

namespace ProjectHub.Api.Controllers;

[ApiController]
[Route("api/ai")]
public class AiController : ControllerBase
{
    private readonly AiService _aiService;
    private readonly AppDbContext _context;
    private readonly ILogger<AiController> _logger;

    public AiController(AiService aiService, AppDbContext context, ILogger<AiController> logger)
    {
        _aiService = aiService;
        _context = context;
        _logger = logger;
    }

    // ==================== 对话管理 ====================

    /// <summary>获取所有对话列表</summary>
    [HttpGet("conversations")]
    public async Task<IActionResult> GetConversations([FromQuery] string? search = null, [FromQuery] bool includeArchived = false)
    {
        var conversations = await _aiService.GetConversations();
        var query = conversations.AsQueryable();

        if (!includeArchived)
            query = query.Where(c => !c.IsArchived);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(c =>
                c.Title.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (c.MemorySummary != null && c.MemorySummary.Contains(search, StringComparison.OrdinalIgnoreCase)));

        return Ok(query
            .OrderByDescending(c => c.IsPinned)
            .ThenByDescending(c => c.UpdatedAt)
            .Select(c => new
            {
                c.Id,
                c.Title,
                c.CreatedAt,
                c.UpdatedAt,
                c.IsArchived,
                c.IsPinned,
                c.MemorySummary,
                MessageCount = c.Messages.Count
            }));
    }

    /// <summary>新建对话</summary>
    [HttpPost("conversations")]
    public async Task<IActionResult> CreateConversation([FromBody] CreateConversationRequest request)
    {
        var conversation = await _aiService.CreateConversation(request.Title);
        return Created($"/api/ai/conversations/{conversation.Id}", conversation);
    }

    /// <summary>获取对话消息</summary>
    [HttpGet("conversations/{id:int}")]
    public async Task<IActionResult> GetConversationMessages(int id)
    {
        var messages = await _aiService.GetConversationMessages(id);
        if (messages.Count == 0)
        {
            var conv = await _context.Conversations.FindAsync(id);
            if (conv == null) return NotFound(new { message = "对话不存在" });
        }
        return Ok(messages.Select(m => new
        {
            m.Id,
            m.ConversationId,
            m.Role,
            m.Content,
            m.ReasoningContent,
            m.ToolCalls,
            m.Attachments,
            m.FilesJson,
            m.CreatedAt
        }));
    }

    /// <summary>删除对话</summary>
    [HttpDelete("conversations/{id:int}")]
    public async Task<IActionResult> DeleteConversation(int id)
    {
        var conv = await _context.Conversations.FindAsync(id);
        if (conv == null) return NotFound(new { message = "对话不存在" });

        await _aiService.DeleteConversation(id);
        return Ok(new { message = "对话已删除" });
    }

    [HttpPut("conversations/{conversationId:int}/messages/{messageId:int}")]
    public async Task<IActionResult> UpdateMessage(int conversationId, int messageId, [FromBody] UpdateMessageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
            return BadRequest(new { message = "消息内容不能为空" });

        try
        {
            var message = await _aiService.UpdateMessageAsync(conversationId, messageId, request.Content);
            if (message == null) return NotFound(new { message = "消息不存在" });
            return Ok(new { message = "消息已更新" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("conversations/{conversationId:int}/messages/{messageId:int}/regenerate")]
    public async Task<IActionResult> RegenerateMessage(int conversationId, int messageId)
    {
        try
        {
            var target = await _aiService.RegenerateFromMessageAsync(conversationId, messageId);
            if (target == null) return NotFound(new { message = "消息不存在" });
            return Ok(new { message = "已清理目标消息后的上下文，可重新发送最后一条用户消息" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("conversations/{conversationId:int}/messages/{messageId:int}/confirm-draft")]
    public async Task<IActionResult> ConfirmDraft(int conversationId, int messageId)
    {
        try
        {
            var result = await _aiService.ConfirmActionDraftAsync(conversationId, messageId);
            if (result == null) return NotFound(new { message = "消息不存在" });
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("conversations/{conversationId:int}/messages/{messageId:int}/cancel-draft")]
    public async Task<IActionResult> CancelDraft(int conversationId, int messageId)
    {
        try
        {
            var result = await _aiService.CancelActionDraftAsync(conversationId, messageId);
            if (result == null) return NotFound(new { message = "消息不存在" });
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("conversations/{id:int}/archive")]
    public async Task<IActionResult> ArchiveConversation(int id, [FromBody] ArchiveConversationRequest request)
    {
        var conv = await _context.Conversations.FindAsync(id);
        if (conv == null) return NotFound(new { message = "对话不存在" });

        conv.IsArchived = request.IsArchived;
        conv.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync();
        return Ok(new { message = request.IsArchived ? "已归档" : "已取消归档" });
    }

    [HttpPatch("conversations/{id:int}/pin")]
    public async Task<IActionResult> PinConversation(int id, [FromBody] PinConversationRequest request)
    {
        var conv = await _context.Conversations.FindAsync(id);
        if (conv == null) return NotFound(new { message = "对话不存在" });

        conv.IsPinned = request.IsPinned;
        conv.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync();
        return Ok(new { message = request.IsPinned ? "已置顶" : "已取消置顶" });
    }

    // ==================== 流式聊天 ====================

    /// <summary>发送消息（SSE流式返回）</summary>
    [HttpPost("conversations/{id:int}/chat")]
    public async Task ChatStream(int id, [FromBody] ChatRequest? request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Message))
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            await Response.WriteAsJsonAsync(new { message = "消息内容不能为空" });
            return;
        }

        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");
        Response.Headers.Append("X-Accel-Buffering", "no");

        var writer = Response.BodyWriter.AsStream();
        try
        {
            await _aiService.ChatStreamAsync(
                id,
                request.Message,
                request.DeepThink,
                request.Attachments,
                async (sseLine) =>
                {
                    var bytes = Encoding.UTF8.GetBytes(sseLine);
                    await writer.WriteAsync(bytes);
                    await writer.FlushAsync();
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SSE 流式聊天异常");
            var errorEvent = $"data: {{\"type\":\"error\",\"content\":\"服务器异常: {ex.Message}\"}}\n\n" +
                             $"data: {{\"type\":\"done\"}}\n\n";
            var errorBytes = Encoding.UTF8.GetBytes(errorEvent);
            await writer.WriteAsync(errorBytes);
            await writer.FlushAsync();
        }
    }

    // ==================== 附件上传 ====================

    /// <summary>上传附件</summary>
    [HttpPost("upload")]
    [RequestSizeLimit(10_485_760)] // 10MB
    public async Task<IActionResult> UploadAttachment(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "请选择文件" });

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".txt", ".md", ".json", ".xml", ".csv", ".log" };
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(ext))
            return BadRequest(new { message = $"不支持的文件类型: {ext}" });

        try
        {
            var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
            if (!Directory.Exists(uploadsDir)) Directory.CreateDirectory(uploadsDir);

            var fileName = $"{Guid.NewGuid()}{ext}";
            var filePath = Path.Combine(uploadsDir, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // 读取文件内容（用于AI上下文）
            string? preview = null;
            if (ext is ".txt" or ".md" or ".json" or ".xml" or ".csv" or ".log")
            {
                var fileContent = await System.IO.File.ReadAllTextAsync(filePath);
                preview = fileContent.Length > 4000 ? fileContent[..4000] + "\n...(内容已截断)" : fileContent;
            }

            var result = new
            {
                fileName,
                originalName = file.FileName,
                size = file.Length,
                type = ext,
                preview,
                url = $"/uploads/{fileName}"
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "文件上传失败");
            return StatusCode(500, new { message = "文件上传失败" });
        }
    }

    // ==================== 设置 ====================

    /// <summary>获取AI设置</summary>
    [HttpGet("settings")]
    public async Task<IActionResult> GetSettings()
    {
        var settings = await _context.UserSettings.FirstOrDefaultAsync();
        return Ok(new
        {
            deepSeekApiKey = settings?.DeepSeekApiKey != null ? "***" + (settings.DeepSeekApiKey.Length > 4 ? settings.DeepSeekApiKey[^4..] : "") : null,
            deepSeekModel = settings?.DeepSeekModel ?? "deepseek-chat",
            hasApiKey = !string.IsNullOrEmpty(settings?.DeepSeekApiKey)
        });
    }

    /// <summary>更新AI设置</summary>
    [HttpPut("settings")]
    public async Task<IActionResult> UpdateSettings([FromBody] UpdateAiSettingsRequest request)
    {
        var settings = await _context.UserSettings.FirstOrDefaultAsync();
        if (settings == null)
        {
            settings = new UserSettings { Id = 1, UpdatedAt = DateTime.Now };
            _context.UserSettings.Add(settings);
        }

        if (!string.IsNullOrEmpty(request.DeepSeekApiKey))
            settings.DeepSeekApiKey = request.DeepSeekApiKey;
        if (!string.IsNullOrEmpty(request.DeepSeekModel))
            settings.DeepSeekModel = request.DeepSeekModel;

        settings.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync();

        return Ok(new { message = "设置已更新" });
    }
}

// ==================== 请求/响应模型 ====================

public class ChatRequest
{
    public string Message { get; set; } = string.Empty;
    public bool DeepThink { get; set; } = false;
    public string? Attachments { get; set; } // JSON 字符串
}

public class CreateConversationRequest
{
    public string? Title { get; set; }
}

public class UpdateAiSettingsRequest
{
    public string? DeepSeekApiKey { get; set; }
    public string? DeepSeekModel { get; set; }
}

public class ConversationFlagRequest
{
    public bool IsArchived { get; set; }
    public bool IsPinned { get; set; }
}

public class ArchiveConversationRequest
{
    public bool IsArchived { get; set; }
}

public class PinConversationRequest
{
    public bool IsPinned { get; set; }
}

public class UpdateMessageRequest
{
    public string Content { get; set; } = string.Empty;
}
