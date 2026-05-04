using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using ProjectHub.Api.Data;
using ProjectHub.Api.Models;

namespace ProjectHub.Api.Services.Ai;

public class AiStreamService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AppDbContext _context;
    private readonly ILogger<AiStreamService> _logger;
    private readonly IFileLogService _fileLog;
    private readonly AiToolService _aiToolService;
    private readonly AiPromptBuilder _promptBuilder;
    private const string DeepSeekBaseUrl = "https://api.deepseek.com";
    private const int MaxFunctionCallRounds = 5;

    public AiStreamService(IHttpClientFactory httpClientFactory, AppDbContext context, ILogger<AiStreamService> logger, IFileLogService fileLog, AiToolService aiToolService, AiPromptBuilder promptBuilder)
    {
        _httpClientFactory = httpClientFactory;
        _context = context;
        _logger = logger;
        _fileLog = fileLog;
        _aiToolService = aiToolService;
        _promptBuilder = promptBuilder;
    }

    public async Task ChatStreamAsync(int conversationId, string userMessage, bool deepThink, string? attachmentsJson, string? model, Func<string, Task> onEvent)
    {
        var settings = await _context.UserSettings.FirstOrDefaultAsync();
        if (settings == null || string.IsNullOrEmpty(settings.DeepSeekApiKey))
        {
            await onEvent(SseEvent("error", "请先在设置中配置 DeepSeek API Key"));
            await onEvent(SseDone());
            return;
        }

        var conversation = await _context.Conversations.FindAsync(conversationId);
        if (conversation == null)
        {
            await _fileLog.WriteAsync("AI", "warn", "对话不存在", new { conversationId, userMessage });
            await onEvent(SseEvent("error", "对话不存在"));
            await onEvent(SseDone());
            return;
        }

        await _fileLog.WriteAsync("AI", "info", "收到聊天请求", new { conversationId, deepThink, userMessage, attachmentsJson });

        var userMsg = new ChatMessage
        {
            ConversationId = conversationId,
            Role = "user",
            Content = userMessage,
            FilesJson = attachmentsJson,
            CreatedAt = DateTime.Now
        };
        _context.ChatMessages.Add(userMsg);
        await _context.SaveChangesAsync();

        if (conversation.Title == "新对话")
            conversation.Title = userMessage.Length > 30 ? userMessage[..30] + "..." : userMessage;

        try
        {
            await UpsertConversationMemoryAsync(conversationId, userMessage, conversation);
            var history = await GetConversationMessagesAsync(conversationId);
            var messages = _promptBuilder.BuildMessageList(history, deepThink, conversation);
            var wantsDraft = LooksLikeActionRequest(userMessage);
            await StreamDeepSeekResponseAsync(messages, deepThink, settings, conversationId, userMessage, wantsDraft, model, onEvent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DeepSeek 流式对话失败");
            await onEvent(SseEvent("error", $"AI 服务异常: {ex.Message}"));
            await onEvent(SseDone());
        }
    }

    public async Task ContinueStreamAsync(int conversationId, string toolResult, Func<string, Task> onEvent)
    {
        var settings = await _context.UserSettings.FirstOrDefaultAsync();
        if (settings == null || string.IsNullOrEmpty(settings.DeepSeekApiKey))
        {
            await onEvent(SseEvent("error", "请先在设置中配置 DeepSeek API Key"));
            await onEvent(SseDone());
            return;
        }

        var conversation = await _context.Conversations.FindAsync(conversationId);
        if (conversation == null)
        {
            await onEvent(SseEvent("error", "对话不存在"));
            await onEvent(SseDone());
            return;
        }

        await _fileLog.WriteAsync("AI", "info", "继续对话请求", new { conversationId, toolResult });
        try
        {
            var history = await GetConversationMessagesAsync(conversationId);
            var messages = new List<object> { new { role = "system", content = "你是 ProjectHub 的个人工作助理。工具已执行完毕，请根据结果继续回答用户问题。" } };
            foreach (var msg in history)
            {
                if (msg.Role == "system") continue;
                if (msg.Role == "assistant" && !string.IsNullOrEmpty(msg.ToolCalls)) { messages.Add(new { role = "assistant", content = msg.Content ?? (string?)null }); continue; }
                if (msg.Role == "tool") continue;
                messages.Add(new { role = msg.Role, content = msg.Content ?? "" });
            }
            messages.Add(new { role = "user", content = $"工具执行结果：\n{toolResult}\n\n请继续分析结果并回答用户。" });
            await StreamDeepSeekResponseAsync(messages, false, settings, conversationId, "", false, null, onEvent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "继续对话失败");
            await onEvent(SseEvent("error", $"继续对话失败: {ex.Message}"));
            await onEvent(SseDone());
        }
    }

    private async Task StreamDeepSeekResponseAsync(List<object> messages, bool deepThink, UserSettings settings, int conversationId, string userMessage, bool wantsDraft, string? model, Func<string, Task> onEvent)
    {
        var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromMinutes(30);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", settings.DeepSeekApiKey);
        var tools = _aiToolService.BuildFunctionTools();
        var selectedModel = !string.IsNullOrWhiteSpace(model) ? model : (settings.DeepSeekModel ?? "deepseek-v4-flash");

        var requestBody = new Dictionary<string, object> { ["model"] = selectedModel, ["messages"] = messages, ["tools"] = tools, ["stream"] = true };
        if (deepThink) { requestBody["thinking"] = new { type = "enabled" }; requestBody["reasoning_effort"] = "high"; } else { requestBody["temperature"] = 0.7; requestBody["max_tokens"] = 2048; }

        int round = 0;
        var fullContent = new StringBuilder();
        var reasoningContent = new StringBuilder();
        var toolCallsList = new List<object>();

        while (round < MaxFunctionCallRounds)
        {
            round++;
            if (round == 1 && wantsDraft) requestBody["response_format"] = new { type = "json_object" }; else requestBody.Remove("response_format");
            var json = JsonSerializer.Serialize(requestBody);
            await _fileLog.WriteRawAsync("AI", "debug", $"DeepSeek request round={round} body={json}");
            var request = new HttpRequestMessage(HttpMethod.Post, $"{DeepSeekBaseUrl}/chat/completions") { Content = new StringContent(json, Encoding.UTF8, "application/json") };
            var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("DeepSeek API error: {Error}", error);
                await _fileLog.WriteAsync("AI", "error", "DeepSeek API 请求失败", new { round, statusCode = (int)response.StatusCode, error });
                await onEvent(SseEvent("error", $"API 请求失败 ({response.StatusCode})"));
                break;
            }

            var assistantContent = new StringBuilder();
            var assistantReasoning = new StringBuilder();
            var assistantToolCalls = new List<Dictionary<string, object>>();
            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);
            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data: ")) continue;
                var data = line.Substring(6);
                if (data == "[DONE]") continue;
                try
                {
                    var chunk = JsonNode.Parse(data);
                    if (chunk == null) continue;
                    var choices = chunk["choices"]?.AsArray();
                    if (choices == null || choices.Count == 0) continue;
                    var delta = choices[0]?["delta"];
                    if (delta == null) continue;
                    var reasoning = delta["reasoning_content"]?.GetValue<string>();
                    if (!string.IsNullOrEmpty(reasoning)) { assistantReasoning.Append(reasoning); await onEvent(SseEvent("reasoning", reasoning)); }
                    var content = delta["content"]?.GetValue<string>();
                    if (!string.IsNullOrEmpty(content)) { assistantContent.Append(content); await onEvent(SseEvent("content", content)); }
                    var toolCallsDelta = delta["tool_calls"]?.AsArray();
                    if (toolCallsDelta != null)
                    {
                        foreach (var tc in toolCallsDelta)
                        {
                            var index = tc?["index"]?.GetValue<int>() ?? 0;
                            while (assistantToolCalls.Count <= index) assistantToolCalls.Add(new Dictionary<string, object>());
                            var id = tc?["id"]?.GetValue<string>(); if (id != null) assistantToolCalls[index]["id"] = id;
                            var func = tc?["function"]; if (func == null) continue;
                            var funcName = func["name"]?.GetValue<string>(); if (funcName != null) assistantToolCalls[index]["name"] = funcName;
                            var funcArgs = func["arguments"]?.GetValue<string>();
                            if (funcArgs != null) assistantToolCalls[index]["arguments"] = (assistantToolCalls[index].ContainsKey("arguments") ? assistantToolCalls[index]["arguments"] as string ?? "" : "") + funcArgs;
                        }
                    }
                }
                catch (JsonException) { }
            }

            if (assistantToolCalls.Count > 0)
            {
                foreach (var tc in assistantToolCalls)
                {
                    var funcName = tc.ContainsKey("name") ? tc["name"]?.ToString() ?? "" : "";
                    var funcArgs = tc.ContainsKey("arguments") ? tc["arguments"]?.ToString() ?? "{}" : "{}";
                    await onEvent(SseEvent("tool_call", new JsonObject { ["name"] = funcName, ["arguments"] = funcArgs }.ToJsonString()));
                }

                var assistantToolMsg = new JsonObject { ["role"] = "assistant" };
                if (assistantContent.Length > 0) assistantToolMsg["content"] = assistantContent.ToString();
                if (assistantReasoning.Length > 0) assistantToolMsg["reasoning_content"] = assistantReasoning.ToString();
                if (assistantToolCalls.Count > 0)
                {
                    var toolCallsArray = new JsonArray();
                    foreach (var tc in assistantToolCalls)
                    {
                        toolCallsArray.Add(new JsonObject { ["id"] = tc.ContainsKey("id") ? tc["id"]?.ToString() ?? "call_1" : "call_1", ["type"] = "function", ["function"] = new JsonObject { ["name"] = tc.ContainsKey("name") ? tc["name"]?.ToString() ?? "" : "", ["arguments"] = tc.ContainsKey("arguments") ? tc["arguments"]?.ToString() ?? "{}" : "{}" } });
                    }
                    assistantToolMsg["tool_calls"] = toolCallsArray;
                }
                messages.Add(assistantToolMsg);

                foreach (var tc in assistantToolCalls)
                {
                    var funcName = tc.ContainsKey("name") ? tc["name"]?.ToString() ?? "" : "";
                    var funcArgs = tc.ContainsKey("arguments") ? tc["arguments"]?.ToString() ?? "{}" : "{}";
                    var toolResult = await _aiToolService.ExecuteToolCallAsync(funcName, funcArgs);
                    await onEvent(SseEvent("tool_result", new JsonObject { ["name"] = funcName, ["result"] = JsonNode.Parse(toolResult) ?? toolResult }.ToJsonString()));
                    messages.Add(new JsonObject { ["role"] = "tool", ["tool_call_id"] = tc.ContainsKey("id") ? tc["id"]?.ToString() ?? "call_1" : "call_1", ["content"] = toolResult });
                }

                toolCallsList.AddRange(assistantToolCalls);
                fullContent.Append(assistantContent);
                reasoningContent.Append(assistantReasoning);
                requestBody["messages"] = messages;
                continue;
            }

            fullContent.Append(assistantContent);
            reasoningContent.Append(assistantReasoning);
            break;
        }

        var rawAssistantText = fullContent.ToString().Trim();
        string? actionDraftJson = null;
        string cleanContent = rawAssistantText;
        if (wantsDraft)
        {
            actionDraftJson = NormalizeDraftJson(rawAssistantText) ?? ExtractActionDraft(rawAssistantText) ?? BuildHeuristicDraft(userMessage, rawAssistantText);
            await _fileLog.WriteAsync("AI", string.IsNullOrWhiteSpace(actionDraftJson) ? "warn" : "info", "草案解析完成", new { conversationId, wantsDraft, hasDraft = !string.IsNullOrWhiteSpace(actionDraftJson), rawAssistantText, actionDraftJson });
            cleanContent = string.IsNullOrWhiteSpace(actionDraftJson) ? rawAssistantText : "已生成待确认草案，请点击确认卡完成写入。";
        }
        else
        {
            actionDraftJson = ExtractActionDraft(rawAssistantText);
            cleanContent = RemoveActionDraft(rawAssistantText);
            if (string.IsNullOrWhiteSpace(cleanContent)) cleanContent = rawAssistantText;
        }

        fullContent.Clear();
        fullContent.Append(cleanContent);
        if (round >= MaxFunctionCallRounds && fullContent.Length == 0) fullContent.Append("抱歉，我无法完成这个查询。请尝试更具体的问题。");

        var assistantMsg = new ChatMessage { ConversationId = conversationId, Role = "assistant", Content = fullContent.ToString(), ReasoningContent = reasoningContent.Length > 0 ? reasoningContent.ToString() : null, ToolCalls = toolCallsList.Count > 0 ? JsonSerializer.Serialize(toolCallsList) : null, Attachments = string.IsNullOrWhiteSpace(actionDraftJson) ? null : actionDraftJson, CreatedAt = DateTime.Now };
        await _fileLog.WriteAsync("AI", "info", "AI 回复保存", new { conversationId, hasDraft = !string.IsNullOrWhiteSpace(actionDraftJson), draftJson = actionDraftJson, assistantContent = fullContent.ToString(), reasoningLength = reasoningContent.Length, toolCallCount = toolCallsList.Count });
        _context.ChatMessages.Add(assistantMsg);
        var conv = await _context.Conversations.FindAsync(conversationId); if (conv != null) conv.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync();
        await onEvent(SseDone());
    }

    private async Task<List<ChatMessage>> GetConversationMessagesAsync(int conversationId) => await _context.ChatMessages.Where(m => m.ConversationId == conversationId).OrderBy(m => m.CreatedAt).ToListAsync();

    private static bool LooksLikeActionRequest(string text) => new[] { "新增", "创建", "新建", "添加", "编辑", "修改", "更新", "改成", "变更", "调整" }.Any(text.Contains);
    private static string SseEvent(string type, string content) => type == "tool_result" || type == "tool_call" ? $"data: {content}\n\n" : $"data: {JsonSerializer.Serialize(new { type, content })}\n\n";
    private static string SseDone() => $"data: {JsonSerializer.Serialize(new { type = "done" })}\n\n";

    private async Task UpsertConversationMemoryAsync(int conversationId, string userMessage, Conversation? conversation = null)
    {
        conversation ??= await _context.Conversations.FindAsync(conversationId);
        if (conversation == null) return;
        var current = conversation.MemorySummary ?? string.Empty;
        var summarySeed = userMessage.Length > 80 ? userMessage[..80] : userMessage;
        var keyPhrase = ExtractMemoryKeywords(userMessage);
        if (!string.IsNullOrWhiteSpace(keyPhrase) && !current.Contains(keyPhrase, StringComparison.OrdinalIgnoreCase)) conversation.MemorySummary = string.IsNullOrWhiteSpace(current) ? $"最近关注：{keyPhrase}; 最新讨论：{summarySeed}" : $"{current} | 最近关注：{keyPhrase}; 最新讨论：{summarySeed}";
        conversation.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync();
    }

    private static string ExtractMemoryKeywords(string text) => string.Join("、", new[] { "项目", "任务", "进度", "资源", "漫画", "文档", "问题", "计划", "上线", "延期" }.Where(text.Contains));
    private static string? ExtractActionDraft(string text) => Regex.Match(text, "<action_draft>([\\s\\S]*?)</action_draft>", RegexOptions.IgnoreCase).Success ? Regex.Match(text, "<action_draft>([\\s\\S]*?)</action_draft>", RegexOptions.IgnoreCase).Groups[1].Value.Trim() : null;
    private static string? NormalizeDraftJson(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        try { var parsed = JsonNode.Parse(raw); if (parsed is JsonObject obj && obj.ContainsKey("kind") && obj.ContainsKey("mode")) return obj.ToJsonString(new JsonSerializerOptions { WriteIndented = false }); } catch { }
        return null;
    }
    private string? BuildHeuristicDraft(string userMessage, string assistantText) => null;
    private static string RemoveActionDraft(string text) => Regex.Replace(text, "<action_draft>[\\s\\S]*?</action_draft>", string.Empty, RegexOptions.IgnoreCase).Trim();
}
