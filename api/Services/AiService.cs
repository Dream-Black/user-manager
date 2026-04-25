using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using ProjectHub.Api.Data;
using ProjectHub.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ProjectHub.Api.Services;

public class AiService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AppDbContext _context;
    private readonly ILogger<AiService> _logger;
    private const string DeepSeekBaseUrl = "https://api.deepseek.com/v1";
    private const int MaxFunctionCallRounds = 5;

    public AiService(IHttpClientFactory httpClientFactory, AppDbContext context, ILogger<AiService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _context = context;
        _logger = logger;
    }

    // ==================== 对话管理 ====================

    public async Task<List<Conversation>> GetConversations()
    {
        return await _context.Conversations
            .Include(c => c.Messages)
            .OrderByDescending(c => c.IsPinned)
            .ThenByDescending(c => c.UpdatedAt)
            .ToListAsync();
    }

    public async Task<Conversation> CreateConversation(string? title = null)
    {
        var conversation = new Conversation
        {
            Title = title ?? "新对话",
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };
        _context.Conversations.Add(conversation);
        await _context.SaveChangesAsync();
        return conversation;
    }

    public async Task<List<ChatMessage>> GetConversationMessages(int conversationId)
    {
        return await _context.ChatMessages
            .Where(m => m.ConversationId == conversationId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();
    }

    public async Task DeleteConversation(int conversationId)
    {
        var conversation = await _context.Conversations.FindAsync(conversationId);
        if (conversation != null)
        {
            _context.Conversations.Remove(conversation);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<ChatMessage?> UpdateMessageAsync(int conversationId, int messageId, string content)
    {
        var message = await _context.ChatMessages
            .FirstOrDefaultAsync(m => m.Id == messageId && m.ConversationId == conversationId);

        if (message == null) return null;
        if (message.Role != "user")
            throw new InvalidOperationException("仅支持编辑用户消息");

        message.Content = content;
        message.CreatedAt = DateTime.Now;

        var conversation = await _context.Conversations.FindAsync(conversationId);
        if (conversation != null)
        {
            conversation.UpdatedAt = DateTime.Now;
            if (conversation.Title == "新对话" && !string.IsNullOrWhiteSpace(content))
            {
                conversation.Title = content.Length > 30 ? content[..30] + "..." : content;
            }
        }

        await _context.SaveChangesAsync();
        return message;
    }

    public async Task<ChatMessage?> RegenerateFromMessageAsync(int conversationId, int messageId)
    {
        var target = await _context.ChatMessages
            .FirstOrDefaultAsync(m => m.Id == messageId && m.ConversationId == conversationId);
        if (target == null) return null;
        if (target.Role != "assistant")
            throw new InvalidOperationException("仅支持对助手回复进行重新生成");

        var messagesToRemove = await _context.ChatMessages
            .Where(m => m.ConversationId == conversationId && m.CreatedAt >= target.CreatedAt)
            .ToListAsync();

        _context.ChatMessages.RemoveRange(messagesToRemove);
        var conversation = await _context.Conversations.FindAsync(conversationId);
        if (conversation != null)
        {
            conversation.UpdatedAt = DateTime.Now;
        }

        await _context.SaveChangesAsync();
        return target;
    }

    public async Task<object?> ConfirmActionDraftAsync(int conversationId, int messageId)
    {
        var message = await _context.ChatMessages
            .FirstOrDefaultAsync(m => m.Id == messageId && m.ConversationId == conversationId);
        if (message == null) return null;
        if (message.Role != "assistant" || string.IsNullOrWhiteSpace(message.Attachments))
            throw new InvalidOperationException("当前消息没有可确认的草案");

        var draft = ParseDraft(message.Attachments);
        if (draft == null || string.IsNullOrWhiteSpace(draft.Kind) || string.IsNullOrWhiteSpace(draft.Mode))
            throw new InvalidOperationException("草案格式无效");

        object? result = draft.Kind switch
        {
            "project" => await ConfirmProjectDraftAsync(draft),
            "task" => await ConfirmTaskDraftAsync(draft),
            "resource" => await ConfirmResourceDraftAsync(draft),
            _ => throw new InvalidOperationException("暂不支持该类型草案")
        };

        message.Content = $"已确认并执行草案：{draft.Title ?? draft.Kind}";
        message.Attachments = null;
        message.CreatedAt = DateTime.Now;
        await _context.SaveChangesAsync();

        return result;
    }

    // ==================== 核心聊天（流式） ====================

    public async Task ChatStreamAsync(
        int conversationId,
        string userMessage,
        bool deepThink,
        string? attachmentsJson,
        Func<string, Task> onEvent)
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

        // 保存用户消息
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

        // 更新对话标题（取首条消息前30字）
        if (conversation.Title == "新对话")
        {
            conversation.Title = userMessage.Length > 30 ? userMessage[..30] + "..." : userMessage;
        }

        try
        {
            await UpsertConversationMemoryAsync(conversationId, userMessage, conversation);

            // 获取对话历史
            var history = await GetConversationMessages(conversationId);
            var messages = BuildMessageList(history, deepThink, conversation);
            var wantsDraft = LooksLikeActionRequest(userMessage);

            await StreamDeepSeekResponse(
                messages, deepThink, settings,
                conversationId, userMessage, wantsDraft, onEvent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DeepSeek 流式对话失败");
            await onEvent(SseEvent("error", $"AI 服务异常: {ex.Message}"));
            await onEvent(SseDone());
        }
    }

    // ==================== DeepSeek API 调用 ====================

    private async Task StreamDeepSeekResponse(
        List<object> messages,
        bool deepThink,
        UserSettings settings,
        int conversationId,
        string userMessage,
        bool wantsDraft,
        Func<string, Task> onEvent)
    {
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", settings.DeepSeekApiKey);

        var tools = BuildFunctionTools();
        var model = deepThink ? "deepseek-reasoner" : (settings.DeepSeekModel ?? "deepseek-chat");

        var requestBody = new Dictionary<string, object>
        {
            ["model"] = model,
            ["messages"] = messages,
            ["tools"] = tools,
            ["stream"] = true,
            ["temperature"] = 0.7,
            ["max_tokens"] = deepThink ? 4096 : 2048
        };

        if (wantsDraft)
        {
            requestBody["response_format"] = new { type = "json_object" };
        }

        if (deepThink)
        {
            requestBody["thinking"] = new { type = "enabled" };
        }

        int round = 0;
        var fullContent = new StringBuilder();
        var reasoningContent = new StringBuilder();
        var toolCallsList = new List<object>();

        while (round < MaxFunctionCallRounds)
        {
            round++;
            var json = JsonSerializer.Serialize(requestBody);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PostAsync($"{DeepSeekBaseUrl}/chat/completions", httpContent);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("DeepSeek API error: {Error}", error);
                await onEvent(SseEvent("error", $"API 请求失败 ({response.StatusCode})"));
                break;
            }

            var assistantContent = new StringBuilder();
            var assistantReasoning = new StringBuilder();
            var assistantToolCalls = new List<Dictionary<string, object>>();

            // 读取 SSE 流
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

                    // 思考内容
                    var reasoning = delta["reasoning_content"]?.GetValue<string>();
                    if (!string.IsNullOrEmpty(reasoning))
                    {
                        assistantReasoning.Append(reasoning);
                        await onEvent(SseEvent("reasoning", reasoning));
                    }

                    // 正常内容
                    var content = delta["content"]?.GetValue<string>();
                    if (!string.IsNullOrEmpty(content))
                    {
                        assistantContent.Append(content);
                        await onEvent(SseEvent("content", content));
                    }

                    // 工具调用
                    var toolCallsDelta = delta["tool_calls"]?.AsArray();
                    if (toolCallsDelta != null)
                    {
                        foreach (var tc in toolCallsDelta)
                        {
                            var index = tc?["index"]?.GetValue<int>() ?? 0;
                            while (assistantToolCalls.Count <= index)
                                assistantToolCalls.Add(new Dictionary<string, object>());

                            var id = tc?["id"]?.GetValue<string>();
                            if (id != null) assistantToolCalls[index]["id"] = id;

                            var func = tc?["function"];
                            if (func == null) continue;

                            var funcName = func["name"]?.GetValue<string>();
                            if (funcName != null) assistantToolCalls[index]["name"] = funcName;

                            var funcArgs = func["arguments"]?.GetValue<string>();
                            if (funcArgs != null)
                            {
                                assistantToolCalls[index]["arguments"] =
                                    (assistantToolCalls[index].ContainsKey("arguments")
                                        ? assistantToolCalls[index]["arguments"] as string ?? ""
                                        : "") + funcArgs;
                            }
                        }
                    }
                }
                catch (JsonException) { /* 忽略解析错误 */ }
            }

            if (assistantToolCalls.Count > 0)
            {
                // 通知前端工具调用
                foreach (var tc in assistantToolCalls)
                {
                    var funcName = tc.ContainsKey("name") ? tc["name"]?.ToString() ?? "" : "";
                    await onEvent(SseEvent("tool_call", JsonSerializer.Serialize(new
                    {
                        name = funcName,
                        arguments = tc.ContainsKey("arguments") ? tc["arguments"]?.ToString() : ""
                    })));
                }

                // 添加 assistant 消息
                messages.Add(new
                {
                    role = "assistant",
                    content = assistantContent.Length > 0 ? assistantContent.ToString() : (string?)null,
                    tool_calls = assistantToolCalls.Select(tc => new
                    {
                        id = tc.ContainsKey("id") ? tc["id"]?.ToString() ?? "" : "call_1",
                        type = "function",
                        function = new
                        {
                            name = tc.ContainsKey("name") ? tc["name"]?.ToString() ?? "" : "",
                            arguments = tc.ContainsKey("arguments") ? tc["arguments"]?.ToString() ?? "" : ""
                        }
                    }).ToArray()
                });

                // 执行工具并添加结果
                foreach (var tc in assistantToolCalls)
                {
                    var funcName = tc.ContainsKey("name") ? tc["name"]?.ToString() ?? "" : "";
                    var funcArgs = tc.ContainsKey("arguments") ? tc["arguments"]?.ToString() ?? "{}" : "{}";
                    var toolResult = await ExecuteToolCall(funcName, funcArgs);
                    await onEvent(SseEvent("tool_result", JsonSerializer.Serialize(new
                    {
                        name = funcName,
                        result = toolResult
                    })));
                    messages.Add(new
                    {
                        role = "tool",
                        tool_call_id = tc.ContainsKey("id") ? tc["id"]?.ToString() ?? "" : "call_1",
                        content = toolResult
                    });
                }

                // 保存工具调用记录
                toolCallsList.AddRange(assistantToolCalls);
                fullContent.Append(assistantContent);
                reasoningContent.Append(assistantReasoning);

                // 继续下一轮
                requestBody["messages"] = messages;
                continue;
            }

            // 没有工具调用，对话结束
            fullContent.Append(assistantContent);
            reasoningContent.Append(assistantReasoning);
            break;
        }

        var rawAssistantText = fullContent.ToString().Trim();
        string? actionDraftJson = null;
        string cleanContent = rawAssistantText;

        if (wantsDraft)
        {
            actionDraftJson = NormalizeDraftJson(rawAssistantText)
                ?? ExtractActionDraft(rawAssistantText)
                ?? BuildHeuristicDraft(userMessage, rawAssistantText);

            cleanContent = string.IsNullOrWhiteSpace(actionDraftJson)
                ? rawAssistantText
                : "已生成待确认草案，请点击确认卡完成写入。";
        }
        else
        {
            actionDraftJson = ExtractActionDraft(rawAssistantText);
            cleanContent = RemoveActionDraft(rawAssistantText);
            if (string.IsNullOrWhiteSpace(cleanContent))
            {
                cleanContent = rawAssistantText;
            }
        }

        fullContent.Clear();
        fullContent.Append(cleanContent);

        if (round >= MaxFunctionCallRounds && fullContent.Length == 0)
        {
            fullContent.Append("抱歉，我无法完成这个查询。请尝试更具体的问题。");
        }

        // 保存 AI 回复
        var assistantMsg = new ChatMessage
        {
            ConversationId = conversationId,
            Role = "assistant",
            Content = fullContent.ToString(),
            ReasoningContent = reasoningContent.Length > 0 ? reasoningContent.ToString() : null,
            ToolCalls = toolCallsList.Count > 0 ? JsonSerializer.Serialize(toolCallsList) : null,
            Attachments = string.IsNullOrWhiteSpace(actionDraftJson) ? null : actionDraftJson,
            CreatedAt = DateTime.Now
        };
        _context.ChatMessages.Add(assistantMsg);

        // 更新对话时间
        var conv = await _context.Conversations.FindAsync(conversationId);
        if (conv != null) conv.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        await onEvent(SseDone());
    }

    // ==================== Function Calling Tools ====================

    private object[] BuildFunctionTools()
    {
        return new object[]
        {
            new
            {
                type = "function",
                function = new
                {
                    name = "get_today_tasks",
                    description = "查询用户今日待办任务、进行中任务和即将到期的任务。返回任务列表包含标题、状态、优先级、进度、截止日期和所属项目信息。",
                    parameters = new
                    {
                        type = "object",
                        properties = new { },
                        required = Array.Empty<string>()
                    }
                }
            },
            new
            {
                type = "function",
                function = new
                {
                    name = "get_projects",
                    description = "查询所有项目列表及其进度统计信息。可按状态过滤（active/completed/all）。",
                    parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            status = new
                            {
                                type = "string",
                                description = "项目状态过滤：active（进行中）、completed（已完成）、all（全部）",
                                Enum = new[] { "active", "completed", "all" }
                            }
                        },
                        required = Array.Empty<string>()
                    }
                }
            },
            new
            {
                type = "function",
                function = new
                {
                    name = "get_task_detail",
                    description = "查询指定任务的详细信息，包括延期记录、子任务列表等。",
                    parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            task_id = new
                            {
                                type = "integer",
                                description = "任务ID"
                            }
                        },
                        required = new[] { "task_id" }
                    }
                }
            },
            new
            {
                type = "function",
                function = new
                {
                    name = "get_resource_paths",
                    description = "查询资源路径列表，可按电脑ID或资源类型筛选，用于资源管理分析。",
                    parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            computer_id = new
                            {
                                type = "integer",
                                description = "电脑ID"
                            },
                            type = new
                            {
                                type = "string",
                                description = "资源类型：comic/video/novel/image"
                            }
                        },
                        required = Array.Empty<string>()
                    }
                }
            },
            new
            {
                type = "function",
                function = new
                {
                    name = "get_statistics",
                    description = "查询系统统计数据：任务总数、完成率、各状态分布、各项目进度等。",
                    parameters = new
                    {
                        type = "object",
                        properties = new { },
                        required = Array.Empty<string>()
                    }
                }
            }
        };
    }

    private async Task<string> ExecuteToolCall(string functionName, string arguments)
    {
        try
        {
            return functionName switch
            {
                "get_today_tasks" => await GetTodayTasks(),
                "get_projects" => await GetProjects(arguments),
                "get_task_detail" => await GetTaskDetail(arguments),
                "get_resource_paths" => await GetResourcePaths(arguments),
                "get_statistics" => await GetStatistics(),
                _ => JsonSerializer.Serialize(new { error = $"未知工具: {functionName}" })
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "工具调用失败: {Tool}", functionName);
            return JsonSerializer.Serialize(new { error = $"查询失败: {ex.Message}" });
        }
    }

    private async Task<string> GetTodayTasks()
    {
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);

        var tasks = await _context.Tasks
            .Include(t => t.Project)
            .Where(t => t.Status != "archived")
            .OrderBy(t => t.Priority == "high" ? 0 : t.Priority == "medium" ? 1 : 2)
            .ThenBy(t => t.PlanEndDate)
            .Select(t => new
            {
                t.Id,
                t.Title,
                t.Status,
                t.Priority,
                t.Progress,
                t.EstimatedHours,
                PlanStartDate = t.PlanStartDate,
                PlanEndDate = t.PlanEndDate,
                ActualEndDate = t.ActualEndDate,
                ProjectName = t.Project != null ? t.Project.Name : null,
                ProjectStatus = t.Project != null ? t.Project.Status : null
            })
            .ToListAsync();

        var todayTasks = tasks
            .Where(t =>
                (t.PlanStartDate != null && t.PlanStartDate <= today && t.PlanEndDate >= today) ||
                (t.PlanEndDate.HasValue && t.PlanEndDate.Value.Date == today) ||
                (t.Status == "in_progress") ||
                (t.Status == "todo" && t.Priority == "high"))
            .ToList();

        var result = new
        {
            today_date = today.ToString("yyyy-MM-dd"),
            total_tasks = tasks.Count,
            today_active_tasks = todayTasks,
            high_priority = tasks.Where(t => t.Priority == "high" && t.Status != "completed").ToList(),
            overdue = tasks.Where(t =>
                t.PlanEndDate.HasValue && t.PlanEndDate.Value.Date < today &&
                t.Status != "completed").ToList(),
            completed_today = tasks.Count(t =>
                t.Status == "completed" && t.ActualEndDate?.Date == today)
        };

        return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = false });
    }

    private async Task<string> GetProjects(string arguments)
    {
        var args = JsonSerializer.Deserialize<Dictionary<string, string>>(arguments);
        var statusFilter = args?.ContainsKey("status") == true ? args["status"] : "all";

        var query = _context.Projects
            .Include(p => p.Tasks)
            .AsQueryable();

        if (statusFilter != "all")
            query = query.Where(p => p.Status == statusFilter);

        var projects = await query
            .OrderByDescending(p => p.UpdatedAt)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Type,
                p.Status,
                p.Customer,
                TaskCount = p.Tasks.Count,
                CompletedTasks = p.Tasks.Count(t => t.Status == "completed"),
                InProgressTasks = p.Tasks.Count(t => t.Status == "in_progress"),
                Progress = p.Tasks.Any()
                    ? (int)Math.Round(p.Tasks.Sum(t => t.EstimatedHours * t.Progress / 100m) /
                        Math.Max(p.Tasks.Sum(t => t.EstimatedHours), 1) * 100)
                    : 0,
                p.CreatedAt,
                p.UpdatedAt
            })
            .ToListAsync();

        return JsonSerializer.Serialize(projects, new JsonSerializerOptions { WriteIndented = false });
    }

    private async Task<string> GetTaskDetail(string arguments)
    {
        var args = JsonSerializer.Deserialize<Dictionary<string, int>>(arguments);
        if (args == null || !args.ContainsKey("task_id"))
            return JsonSerializer.Serialize(new { error = "缺少 task_id 参数" });

        var taskId = args["task_id"];
        var task = await _context.Tasks
            .Include(t => t.Project)
            .Include(t => t.Delays)
            .Include(t => t.ExtraRequirements)
            .Include(t => t.SubTasks)
            .FirstOrDefaultAsync(t => t.Id == taskId);

        if (task == null)
            return JsonSerializer.Serialize(new { error = $"任务 {taskId} 不存在" });

        var result = new
        {
            task.Id,
            task.Title,
            task.Description,
            task.Status,
            task.Priority,
            task.Progress,
            task.EstimatedHours,
            PlanStartDate = task.PlanStartDate,
            PlanEndDate = task.PlanEndDate,
            ActualStartDate = task.ActualStartDate,
            ActualEndDate = task.ActualEndDate,
            Project = task.Project != null ? new { task.Project.Id, task.Project.Name } : null,
            Delays = task.Delays.Select(d => new
            {
                d.Reason,
                OriginalEnd = d.OriginalPlanEndDate,
                NewEnd = d.NewPlanEndDate,
                d.CreatedAt
            }),
            ExtraRequirements = task.ExtraRequirements.Select(e => new
            {
                e.Description,
                e.CreatedAt
            }),
            SubTasks = task.SubTasks.Select(s => new
            {
                s.Id,
                s.Title,
                s.IsCompleted
            }),
            task.CreatedAt,
            task.UpdatedAt
        };

        return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = false });
    }

    private async Task<string> GetResourcePaths(string arguments)
    {
        var args = JsonSerializer.Deserialize<Dictionary<string, string>>(arguments);
        var query = _context.ResourcePaths
            .Include(r => r.Computer)
            .AsQueryable();

        if (args != null)
        {
            if (args.TryGetValue("computer_id", out var computerIdStr) && int.TryParse(computerIdStr, out var computerId))
                query = query.Where(r => r.ComputerId == computerId);
            if (args.TryGetValue("type", out var type) && !string.IsNullOrWhiteSpace(type))
                query = query.Where(r => r.Type == type);
        }

        var paths = await query
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new
            {
                r.Id,
                r.ComputerId,
                ComputerName = r.Computer != null ? r.Computer.Name : null,
                r.Type,
                r.Path,
                r.IsEnabled,
                r.CreatedAt
            })
            .ToListAsync();

        return JsonSerializer.Serialize(paths, new JsonSerializerOptions { WriteIndented = false });
    }

    private async Task<string> GetStatistics()
    {
        var allTasks = await _context.Tasks
            .Where(t => t.Status != "archived")
            .ToListAsync();

        var allProjects = await _context.Projects.ToListAsync();

        var allResources = await _context.ResourcePaths.ToListAsync();

        var result = new
        {
            total_tasks = allTasks.Count,
            completed_tasks = allTasks.Count(t => t.Status == "completed"),
            in_progress_tasks = allTasks.Count(t => t.Status == "in_progress"),
            todo_tasks = allTasks.Count(t => t.Status == "todo"),
            high_priority_tasks = allTasks.Count(t => t.Priority == "high" && t.Status != "completed"),
            overdue_tasks = allTasks.Count(t =>
                t.PlanEndDate.HasValue && t.PlanEndDate.Value.Date < DateTime.Today && t.Status != "completed"),
            total_projects = allProjects.Count,
            active_projects = allProjects.Count(p => p.Status == "active"),
            completed_projects = allProjects.Count(p => p.Status == "completed"),
            total_resources = allResources.Count,
            enabled_resources = allResources.Count(r => r.IsEnabled),
            overall_completion_rate = allTasks.Count > 0
                ? $"{Math.Round((double)allTasks.Count(t => t.Status == "completed") / allTasks.Count * 100, 1)}%"
                : "0%"
        };

        return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = false });
    }

    // ==================== 工具方法 ====================

    private static bool LooksLikeActionRequest(string text)
    {
        var keywords = new[] { "新增", "创建", "新建", "添加", "编辑", "修改", "更新", "改成", "变更", "调整" };
        return keywords.Any(text.Contains);
    }

    private static ActionDraftDto? BuildDraftFromJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            return JsonSerializer.Deserialize<ActionDraftDto>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch
        {
            return null;
        }
    }

    private List<object> BuildMessageList(List<ChatMessage> history, bool deepThink, Conversation conversation)
    {
        var messages = new List<object>();

        var memorySummary = string.IsNullOrWhiteSpace(conversation.MemorySummary)
            ? ""
            : $"\n\n用户偏好摘要：{conversation.MemorySummary}";

        // 系统提示词
        var draftFormat = "<action_draft>{\"kind\":\"project|task|resource\",\"mode\":\"create|update\",\"title\":\"...\",\"targetLabel\":\"...\",\"before\":\"...\",\"after\":\"...\",\"preview\":\"...\",\"payload\":{...}}</action_draft>";
        var permissionNote = "你具备生成新增/编辑草案的能力。你不能直接落库，但可以输出待确认草案。不要说“我没有权限”，应表述为“需要你确认后由系统执行”。";
        var systemPrompt = deepThink
            ? $"{permissionNote} 你是 ProjectHub 的个人工作助理，具备数据库查询能力。你可以调用工具来获取项目、任务等数据。请用中文回答，给出具体的分析和建议。当你需要数据时，直接调用相应的工具函数获取实时数据。若你判断用户意图是新增/编辑项目、任务、资源，请先生成一个结构化写入草案，但不要直接写库；草案必须是严格 JSON，并且只放在回复末尾的 {draftFormat} 中，正文要先解释依据。{memorySummary}"
            : $"{permissionNote} 你是 ProjectHub 的个人工作助理，可以用中文帮助用户查询项目进度、任务状态等。请简洁专业地回答。当用户询问今天该做什么或项目情况时，请先调用工具查询实时数据再回答。若你判断用户意图是新增/编辑项目、任务、资源，请先生成一个结构化写入草案，但不要直接写库；草案必须是严格 JSON，并且只放在回复末尾的 {draftFormat} 中，正文要先解释依据。{memorySummary}";

        messages.Add(new
        {
            role = "system",
            content = systemPrompt
        });

        foreach (var msg in history)
        {
            if (msg.Role == "system") continue;

            if (msg.Role == "assistant" && !string.IsNullOrEmpty(msg.ToolCalls))
            {
                messages.Add(new
                {
                    role = "assistant",
                    content = msg.Content ?? (string?)null
                });
                continue;
            }

            if (msg.Role == "tool")
            {
                continue;
            }

            messages.Add(new
            {
                role = msg.Role,
                content = msg.Content ?? ""
            });
        }

        return messages;
    }

    private static string SseEvent(string type, string content)
    {
        return $"data: {JsonSerializer.Serialize(new { type, content })}\n\n";
    }

    private async Task UpsertConversationMemoryAsync(int conversationId, string userMessage, Conversation? conversation = null)
    {
        conversation ??= await _context.Conversations.FindAsync(conversationId);
        if (conversation == null) return;

        var current = conversation.MemorySummary ?? string.Empty;
        var summarySeed = userMessage.Length > 80 ? userMessage[..80] : userMessage;
        var keyPhrase = ExtractMemoryKeywords(userMessage);

        if (!string.IsNullOrWhiteSpace(keyPhrase) && !current.Contains(keyPhrase, StringComparison.OrdinalIgnoreCase))
        {
            conversation.MemorySummary = string.IsNullOrWhiteSpace(current)
                ? $"最近关注：{keyPhrase}; 最新讨论：{summarySeed}"
                : $"{current} | 最近关注：{keyPhrase}; 最新讨论：{summarySeed}";
        }

        conversation.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync();
    }

    private static string ExtractMemoryKeywords(string text)
    {
        var keywords = new[] { "项目", "任务", "进度", "资源", "漫画", "文档", "问题", "计划", "上线", "延期" };
        return string.Join("、", keywords.Where(text.Contains));
    }

    private static string? ExtractActionDraft(string text)
    {
        var match = Regex.Match(text, "<action_draft>([\\s\\S]*?)</action_draft>", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value.Trim() : null;
    }

    private static string? NormalizeDraftJson(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        try
        {
            var parsed = JsonNode.Parse(raw);
            if (parsed is JsonObject obj)
            {
                if (!obj.ContainsKey("kind") || !obj.ContainsKey("mode")) return null;
                return obj.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
            }
        }
        catch
        {
            // ignore
        }
        return null;
    }

    private string? BuildHeuristicDraft(string userMessage, string assistantText)
    {
        var kind = GuessDraftKind(userMessage);
        if (string.IsNullOrWhiteSpace(kind)) return null;

        var title = ExtractDraftTitle(userMessage, kind);
        var targetLabel = kind switch
        {
            "project" => $"项目：{title}",
            "task" => $"任务：{title}",
            "resource" => $"资源：{title}",
            _ => title
        };

        var payload = kind == "task"
            ? BuildTaskPayloadFromText(userMessage)
            : new JsonObject();

        var draft = new ActionDraftDto
        {
            Kind = kind,
            Mode = GuessDraftMode(userMessage),
            Title = $"新增{getDraftKindLabel(kind)}：{title}",
            TargetLabel = targetLabel,
            Before = "无",
            After = assistantText,
            Preview = assistantText,
            Payload = payload
        };

        return JsonSerializer.Serialize(draft, new JsonSerializerOptions { WriteIndented = false, PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    }

    private static string GuessDraftKind(string text)
    {
        if (text.Contains("项目")) return "project";
        if (text.Contains("任务")) return "task";
        if (text.Contains("资源")) return "resource";
        return string.Empty;
    }

    private static string GuessDraftMode(string text)
    {
        return text.Contains("加") || text.Contains("新增") || text.Contains("创建") || text.Contains("新建") ? "create" : "update";
    }

    private static string ExtractDraftTitle(string text, string kind)
    {
        var cleaned = text.Replace("帮我", string.Empty).Replace("再", string.Empty).Replace("加个", string.Empty).Replace("新增", string.Empty).Replace("创建", string.Empty).Replace("新建", string.Empty).Trim();
        if (kind == "project")
        {
            var idx = cleaned.LastIndexOf('，');
            if (idx >= 0 && idx < cleaned.Length - 1) return cleaned[(idx + 1)..].Trim();
        }
        return cleaned.Length > 20 ? cleaned[..20] : cleaned;
    }

    private static JsonObject BuildTaskPayloadFromText(string userMessage)
    {
        var payload = new JsonObject();
        var titleMatch = Regex.Match(userMessage, @"(?:加个任务|新增任务|创建任务|新加个任务|帮我加个任务)[，,\s]*(.+?)(?:今天|截止|到期|$)");
        var title = titleMatch.Success ? titleMatch.Groups[1].Value.Trim() : userMessage.Trim();
        payload["title"] = title;

        if (userMessage.Contains("今天"))
        {
            payload["planEndDate"] = DateTime.Today.ToString("yyyy-MM-dd");
        }

        return payload;
    }

    private static string getDraftKindLabel(string kind) => kind switch
    {
        "project" => "项目",
        "task" => "任务",
        "resource" => "资源",
        _ => "草案"
    };

    private static string RemoveActionDraft(string text)
    {
        return Regex.Replace(text, "<action_draft>[\\s\\S]*?</action_draft>", string.Empty, RegexOptions.IgnoreCase).Trim();
    }

    private static ActionDraftDto? ParseDraft(string raw)
    {
        try
        {
            return JsonSerializer.Deserialize<ActionDraftDto>(raw, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch
        {
            return null;
        }
    }

    private async Task<object> ConfirmProjectDraftAsync(ActionDraftDto draft)
    {
        var payload = draft.Payload ?? new JsonObject();
        if (draft.Mode == "create")
        {
            var project = new Project
            {
                Name = payload["name"]?.ToString() ?? draft.Title ?? "未命名项目",
                Description = payload["description"]?.ToString(),
                Type = payload["type"]?.ToString() ?? "web",
                Customer = payload["customer"]?.ToString(),
                Status = payload["status"]?.ToString() ?? "active",
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
            _context.Projects.Add(project);
            await _context.SaveChangesAsync();
            return new { kind = "project", action = "created", project.Id, project.Name };
        }

        if (draft.Mode == "update")
        {
            var projectId = payload["id"]?.GetValue<int?>() ?? 0;
            var project = await _context.Projects.FindAsync(projectId);
            if (project == null) throw new InvalidOperationException("待更新项目不存在");
            if (payload["name"] != null) project.Name = payload["name"]!.ToString();
            if (payload["description"] != null) project.Description = payload["description"]!.ToString();
            if (payload["type"] != null) project.Type = payload["type"]!.ToString();
            if (payload["customer"] != null) project.Customer = payload["customer"]!.ToString();
            if (payload["status"] != null) project.Status = payload["status"]!.ToString();
            project.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            return new { kind = "project", action = "updated", project.Id, project.Name };
        }

        throw new InvalidOperationException("暂不支持该项目操作");
    }

    private async Task<object> ConfirmTaskDraftAsync(ActionDraftDto draft)
    {
        var payload = draft.Payload ?? new JsonObject();
        if (draft.Mode == "create")
        {
            var projectId = payload["projectId"]?.GetValue<int?>() ?? 0;
            var project = await _context.Projects.FindAsync(projectId);
            if (project == null)
                throw new InvalidOperationException("任务草案关联的项目不存在");

            var task = new ProjectTask
            {
                ProjectId = projectId,
                Title = payload["title"]?.ToString() ?? draft.Title ?? "未命名任务",
                Description = payload["description"]?.ToString(),
                Category = payload["category"]?.ToString() ?? "dev",
                Priority = payload["priority"]?.ToString() ?? "medium",
                Status = payload["status"]?.ToString() ?? "todo",
                Progress = payload["progress"]?.GetValue<int?>() ?? 0,
                EstimatedHours = payload["estimatedHours"]?.GetValue<decimal?>() ?? 1,
                PlanStartDate = payload["planStartDate"]?.GetValue<DateTime?>(),
                PlanEndDate = payload["planEndDate"]?.GetValue<DateTime?>(),
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
            _context.Tasks.Add(task);
            await _context.SaveChangesAsync();
            return new { kind = "task", action = "created", task.Id, task.Title };
        }

        if (draft.Mode == "update")
        {
            var taskId = payload["id"]?.GetValue<int?>() ?? 0;
            var task = await _context.Tasks.FindAsync(taskId);
            if (task == null) throw new InvalidOperationException("待更新任务不存在");
            if (payload["title"] != null) task.Title = payload["title"]!.ToString();
            if (payload["description"] != null) task.Description = payload["description"]!.ToString();
            if (payload["category"] != null) task.Category = payload["category"]!.ToString();
            if (payload["priority"] != null) task.Priority = payload["priority"]!.ToString();
            if (payload["status"] != null) task.Status = payload["status"]!.ToString();
            if (payload["progress"] != null) task.Progress = payload["progress"]!.GetValue<int>();
            if (payload["estimatedHours"] != null) task.EstimatedHours = payload["estimatedHours"]!.GetValue<decimal>();
            if (payload["planStartDate"] != null) task.PlanStartDate = payload["planStartDate"]!.GetValue<DateTime?>();
            if (payload["planEndDate"] != null) task.PlanEndDate = payload["planEndDate"]!.GetValue<DateTime?>();
            task.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            return new { kind = "task", action = "updated", task.Id, task.Title };
        }

        throw new InvalidOperationException("暂不支持该任务操作");
    }

    private async Task<object> ConfirmResourceDraftAsync(ActionDraftDto draft)
    {
        var payload = draft.Payload ?? new JsonObject();
        if (draft.Mode == "create")
        {
            var computerId = payload["computerId"]?.GetValue<int?>() ?? 0;
            var computer = await _context.Computers.FindAsync(computerId);
            if (computer == null)
                throw new InvalidOperationException("资源草案关联的电脑不存在");

            var resourcePath = new ResourcePath
            {
                ComputerId = computerId,
                Type = payload["type"]?.ToString() ?? "comic",
                Path = payload["path"]?.ToString() ?? string.Empty,
                IsEnabled = payload["isEnabled"]?.GetValue<bool?>() ?? true,
                CreatedAt = DateTime.Now
            };
            _context.ResourcePaths.Add(resourcePath);
            await _context.SaveChangesAsync();
            return new { kind = "resource", action = "created", resourcePath.Id, resourcePath.Type, resourcePath.Path };
        }

        if (draft.Mode == "update")
        {
            var resourceId = payload["id"]?.GetValue<int?>() ?? 0;
            var resourcePath = await _context.ResourcePaths.FindAsync(resourceId);
            if (resourcePath == null) throw new InvalidOperationException("待更新资源不存在");
            if (payload["path"] != null) resourcePath.Path = payload["path"]!.ToString();
            if (payload["isEnabled"] != null) resourcePath.IsEnabled = payload["isEnabled"]!.GetValue<bool>();
            await _context.SaveChangesAsync();
            return new { kind = "resource", action = "updated", resourcePath.Id, resourcePath.Type, resourcePath.Path };
        }

        throw new InvalidOperationException("暂不支持该资源操作");
    }

    private sealed class ActionDraftDto
    {
        public string? Kind { get; set; }
        public string? Mode { get; set; }
        public string? Title { get; set; }
        public string? TargetLabel { get; set; }
        public string? Before { get; set; }
        public string? After { get; set; }
        public string? Preview { get; set; }
        public JsonObject? Payload { get; set; }
    }

    private static string SseDone()
    {
        return $"data: {JsonSerializer.Serialize(new { type = "done" })}\n\n";
    }
}
