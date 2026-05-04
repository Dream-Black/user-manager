using System.Text;
using Microsoft.AspNetCore.Mvc;
using ProjectHub.Api.Data;
using ProjectHub.Api.Dtos;
using ProjectHub.Api.Services.Ai;

namespace ProjectHub.Api.Controllers;

[ApiController]
[Route("api/ai")]
public class AiController : ControllerBase
{
    private readonly AiConversationService _conversationService;
    private readonly AiDraftService _draftService;
    private readonly AiStreamService _streamService;
    private readonly AiSettingsService _settingsService;
    private readonly AiBalanceService _balanceService;
    private readonly AppDbContext _context;
    private readonly ILogger<AiController> _logger;

    public AiController(
        AiConversationService conversationService,
        AiDraftService draftService,
        AiStreamService streamService,
        AiSettingsService settingsService,
        AiBalanceService balanceService,
        AppDbContext context,
        ILogger<AiController> logger)
    {
        _conversationService = conversationService;
        _draftService = draftService;
        _streamService = streamService;
        _settingsService = settingsService;
        _balanceService = balanceService;
        _context = context;
        _logger = logger;
    }

    [HttpGet("test-sse")]
    public async Task TestSse()
    {
        Response.ContentType = "text/event-stream";
        Response.Headers["Cache-Control"] = "no-cache";
        HttpContext.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpResponseBodyFeature>()?.DisableBuffering();
        await Response.StartAsync();
        var writer = Response.BodyWriter;
        for (int i = 1; i <= 10; i++)
        {
            var msg = $"data: {{\"type\":\"content\",\"content\":\"第{i}行\"}}\n\n";
            var bytes = Encoding.UTF8.GetBytes(msg);
            var mem = writer.GetMemory(bytes.Length);
            bytes.CopyTo(mem);
            writer.Advance(bytes.Length);
            await writer.FlushAsync();
            await Task.Delay(1000);
        }
        var done = "data: {\"type\":\"done\"}\n\n"u8.ToArray();
        var doneMem = writer.GetMemory(done.Length);
        done.CopyTo(doneMem);
        writer.Advance(done.Length);
        await writer.FlushAsync();
    }

    [HttpGet("conversations")]
    public async Task<IActionResult> GetConversations([FromQuery] string? search = null, [FromQuery] bool includeArchived = false)
    {
        var conversations = await _conversationService.GetConversationsAsync();
        var query = conversations.AsQueryable();
        if (!includeArchived) query = query.Where(c => !c.IsArchived);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(c => c.Title.Contains(search, StringComparison.OrdinalIgnoreCase) || (c.MemorySummary != null && c.MemorySummary.Contains(search, StringComparison.OrdinalIgnoreCase)));
        return Ok(query.OrderByDescending(c => c.IsPinned).ThenByDescending(c => c.UpdatedAt).Select(c => new { c.Id, c.Title, c.CreatedAt, c.UpdatedAt, c.IsArchived, c.IsPinned, c.MemorySummary, MessageCount = c.Messages.Count }));
    }

    [HttpPost("conversations")]
    public async Task<IActionResult> CreateConversation([FromBody] CreateConversationRequest request)
        => Created($"/api/ai/conversations/{(await _conversationService.CreateConversationAsync(request.Title)).Id}", await _conversationService.CreateConversationAsync(request.Title));

    [HttpGet("conversations/{id:int}")]
    public async Task<IActionResult> GetConversationMessages(int id)
    {
        var messages = await _conversationService.GetConversationMessagesAsync(id);
        if (messages.Count == 0)
        {
            var conv = await _context.Conversations.FindAsync(id);
            if (conv == null) return NotFound(new { message = "对话不存在" });
        }
        return Ok(messages.Select(m => new { m.Id, m.ConversationId, m.Role, m.Content, m.ReasoningContent, m.ToolCalls, m.Attachments, m.FilesJson, m.CreatedAt }));
    }

    [HttpDelete("conversations/{id:int}")]
    public async Task<IActionResult> DeleteConversation(int id)
    {
        var conv = await _context.Conversations.FindAsync(id);
        if (conv == null) return NotFound(new { message = "对话不存在" });
        await _conversationService.DeleteConversationAsync(id);
        return Ok(new { message = "对话已删除" });
    }

    [HttpPut("conversations/{conversationId:int}/messages/{messageId:int}")]
    public async Task<IActionResult> UpdateMessage(int conversationId, int messageId, [FromBody] UpdateMessageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Content)) return BadRequest(new { message = "消息内容不能为空" });
        try
        {
            var message = await _conversationService.UpdateMessageAsync(conversationId, messageId, request.Content);
            if (message == null) return NotFound(new { message = "消息不存在" });
            return Ok(new { message = "消息已更新" });
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("conversations/{conversationId:int}/messages/{messageId:int}/regenerate")]
    public async Task<IActionResult> RegenerateMessage(int conversationId, int messageId)
    {
        try
        {
            var target = await _conversationService.RegenerateFromMessageAsync(conversationId, messageId);
            if (target == null) return NotFound(new { message = "消息不存在" });
            return Ok(new { message = "已清理目标消息后的上下文，可重新发送最后一条用户消息" });
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("conversations/{conversationId:int}/messages/{messageId:int}/confirm-draft")]
    public async Task<IActionResult> ConfirmDraft(int conversationId, int messageId)
    {
        try
        {
            var result = await _draftService.ConfirmActionDraftAsync(conversationId, messageId);
            if (result == null) return NotFound(new { message = "消息不存在" });
            return Ok(result);
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("conversations/{conversationId:int}/messages/{messageId:int}/cancel-draft")]
    public async Task<IActionResult> CancelDraft(int conversationId, int messageId)
    {
        try
        {
            var result = await _draftService.CancelActionDraftAsync(conversationId, messageId);
            if (result == null) return NotFound(new { message = "消息不存在" });
            return Ok(result);
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPatch("conversations/{id:int}/archive")]
    public async Task<IActionResult> ArchiveConversation(int id, [FromBody] ArchiveConversationRequest request)
    {
        var conv = await _context.Conversations.FindAsync(id);
        if (conv == null) return NotFound(new { message = "对话不存在" });
        conv.IsArchived = request.IsArchived; conv.UpdatedAt = DateTime.Now; await _context.SaveChangesAsync();
        return Ok(new { message = request.IsArchived ? "已归档" : "已取消归档" });
    }

    [HttpPatch("conversations/{id:int}/pin")]
    public async Task<IActionResult> PinConversation(int id, [FromBody] PinConversationRequest request)
    {
        var conv = await _context.Conversations.FindAsync(id);
        if (conv == null) return NotFound(new { message = "对话不存在" });
        conv.IsPinned = request.IsPinned; conv.UpdatedAt = DateTime.Now; await _context.SaveChangesAsync();
        return Ok(new { message = request.IsPinned ? "已置顶" : "已取消置顶" });
    }

    [HttpPost("conversations/{id:int}/chat")]
    public async Task ChatStream(int id, [FromBody] ChatRequest? request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Message)) { Response.StatusCode = StatusCodes.Status400BadRequest; await Response.WriteAsJsonAsync(new { message = "消息内容不能为空" }); return; }
        Response.ContentType = "text/event-stream"; Response.Headers["Cache-Control"] = "no-cache"; HttpContext.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpResponseBodyFeature>()?.DisableBuffering(); await Response.StartAsync();
        var writer = Response.BodyWriter;
        try
        {
            await _streamService.ChatStreamAsync(id, request.Message, request.DeepThink, request.Attachments, request.Model, async (sseLine) => { var bytes = Encoding.UTF8.GetBytes(sseLine); var mem = writer.GetMemory(bytes.Length); bytes.CopyTo(mem); writer.Advance(bytes.Length); await writer.FlushAsync(); });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SSE 流式聊天异常"); var errorEvent = $"data: {{\"type\":\"error\",\"content\":\"服务器异常: {ex.Message}\"}}\n\n" + $"data: {{\"type\":\"done\"}}\n\n"; var errorBytes = Encoding.UTF8.GetBytes(errorEvent); var emem = writer.GetMemory(errorBytes.Length); errorBytes.CopyTo(emem); writer.Advance(errorBytes.Length); await writer.FlushAsync();
        }
    }

    [HttpPost("conversations/{id:int}/continue")]
    public async Task ContinueStream(int id, [FromBody] ContinueRequest? request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.ToolResult)) { Response.StatusCode = StatusCodes.Status400BadRequest; await Response.WriteAsJsonAsync(new { message = "工具结果不能为空" }); return; }
        Response.ContentType = "text/event-stream"; Response.Headers["Cache-Control"] = "no-cache"; HttpContext.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpResponseBodyFeature>()?.DisableBuffering(); await Response.StartAsync();
        var writer = Response.BodyWriter;
        try
        {
            await _streamService.ContinueStreamAsync(id, request.ToolResult, async (sseLine) => { var bytes = Encoding.UTF8.GetBytes(sseLine); var mem = writer.GetMemory(bytes.Length); bytes.CopyTo(mem); writer.Advance(bytes.Length); await writer.FlushAsync(); });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SSE 继续对话异常"); var errorEvent = $"data: {{\"type\":\"error\",\"content\":\"服务器异常: {ex.Message}\"}}\n\n" + $"data: {{\"type\":\"done\"}}\n\n"; var errorBytes = Encoding.UTF8.GetBytes(errorEvent); var emem = writer.GetMemory(errorBytes.Length); errorBytes.CopyTo(emem); writer.Advance(errorBytes.Length); await writer.FlushAsync();
        }
    }

    [HttpPost("upload")]
    [RequestSizeLimit(10_485_760)]
    public async Task<IActionResult> UploadAttachment(IFormFile file)
    {
        if (file == null || file.Length == 0) return BadRequest(new { message = "请选择文件" });
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".txt", ".md", ".json", ".xml", ".csv", ".log" };
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(ext)) return BadRequest(new { message = $"不支持的文件类型: {ext}" });
        try
        {
            var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "uploads"); if (!Directory.Exists(uploadsDir)) Directory.CreateDirectory(uploadsDir);
            var fileName = $"{Guid.NewGuid()}{ext}"; var filePath = Path.Combine(uploadsDir, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create)) { await file.CopyToAsync(stream); }
            string? preview = null; if (ext is ".txt" or ".md" or ".json" or ".xml" or ".csv" or ".log") { var fileContent = await System.IO.File.ReadAllTextAsync(filePath); preview = fileContent.Length > 4000 ? fileContent[..4000] + "\n...(内容已截断)" : fileContent; }
            return Ok(new { fileName, originalName = file.FileName, size = file.Length, type = ext, preview, url = $"/uploads/{fileName}" });
        }
        catch (Exception ex) { _logger.LogError(ex, "文件上传失败"); return StatusCode(500, new { message = "文件上传失败" }); }
    }

    [HttpGet("balance")]
    public async Task<IActionResult> GetBalance() => Ok(await _balanceService.GetBalanceAsync());

    [HttpGet("settings")]
    public async Task<IActionResult> GetSettings() => Ok(await _settingsService.GetSettingsAsync());

    [HttpPut("settings")]
    public async Task<IActionResult> UpdateSettings([FromBody] UpdateAiSettingsRequest request)
    {
        await _settingsService.UpdateSettingsAsync(request.DeepSeekApiKey, request.DeepSeekModel);
        return Ok(new { message = "设置已更新" });
    }
}
