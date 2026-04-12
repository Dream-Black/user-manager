using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ProjectHub.Api.Data;
using ProjectHub.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ProjectHub.Api.Services;

public class AiService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AppDbContext _context;
    private readonly ILogger<AiService> _logger;

    public AiService(IHttpClientFactory httpClientFactory, AppDbContext context, ILogger<AiService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _context = context;
        _logger = logger;
    }

    /// <summary>发送消息给 DeepSeek</summary>
    public async Task<AiResponse> SendMessage(string message, int? projectId = null, int? taskId = null)
    {
        var settings = await _context.UserSettings.FirstOrDefaultAsync();
        if (settings == null || string.IsNullOrEmpty(settings.DeepSeekApiKey))
        {
            return new AiResponse
            {
                Success = false,
                Error = "请先在设置中配置 DeepSeek API Key"
            };
        }

        try
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", settings.DeepSeekApiKey);

            // 构建上下文
            var contextInfo = await BuildContextInfo(projectId, taskId);

            var systemPrompt = $@"你是一个专业的个人工作管理助手。用户正在使用 ProjectHub 项目管理系统。

{contextInfo}

请根据上下文信息，帮助用户：
1. 分析项目/任务情况
2. 提供工作建议和优化方案
3. 帮助拆分复杂任务
4. 总结复盘经验
5. 制定合理的工作计划

回答要简洁、专业、有针对性。如果需要更多信息，请询问用户。";

            var requestBody = new
            {
                model = settings.DeepSeekModel,
                messages = new object[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = message }
                },
                temperature = 0.7,
                max_tokens = 2000
            };

            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var response = await client.PostAsync("https://api.deepseek.com/v1/chat/completions", content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("DeepSeek API error: {Error}", error);
                return new AiResponse
                {
                    Success = false,
                    Error = $"API 请求失败: {response.StatusCode}"
                };
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseJson);
            var choices = doc.RootElement.GetProperty("choices");

            if (choices.GetArrayLength() > 0)
            {
                var messageContent = choices[0].GetProperty("message").GetProperty("content").GetString();
                return new AiResponse
                {
                    Success = true,
                    Content = messageContent ?? "我没有得到有效的回复"
                };
            }

            return new AiResponse
            {
                Success = false,
                Error = "未收到有效的响应"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DeepSeek API 调用失败");
            return new AiResponse
            {
                Success = false,
                Error = $"调用失败: {ex.Message}"
            };
        }
    }

    /// <summary>获取每日提醒内容</summary>
    public async Task<string> GetDailyReminder()
    {
        var settings = await _context.UserSettings.FirstOrDefaultAsync();
        if (settings == null)
            return "请先配置系统设置";

        // 获取今天的任务
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);

        var todayTasks = await _context.Tasks
            .Include(t => t.Project)
            .Where(t =>
                (t.PlanStartDate != null && t.PlanStartDate <= today && t.PlanEndDate >= today) ||
                (t.PlanStartDate != null && t.PlanStartDate.Value.Date == today) ||
                (t.Status != "completed" && t.PlanEndDate != null && t.PlanEndDate.Value.Date <= today))
            .OrderBy(t => t.Priority == "high" ? 0 : t.Priority == "medium" ? 1 : 2)
            .ThenBy(t => t.PlanEndDate)
            .ToListAsync();

        if (!todayTasks.Any())
        {
            return "今日没有待办任务，好好休息吧！☕";
        }

        // 尝试调用 AI 生成建议
        var aiResponse = await GenerateDailySummary(todayTasks, settings);
        return aiResponse;
    }

    private async Task<string> GenerateDailySummary(List<Models.ProjectTask> tasks, UserSettings settings)
    {
        if (string.IsNullOrEmpty(settings.DeepSeekApiKey))
        {
            return BuildBasicReminder(tasks);
        }

        try
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", settings.DeepSeekApiKey);

            var taskList = string.Join("\n", tasks.Select(t =>
                $"- [{t.Title}] ({t.Project?.Name}) | 优先级: {t.Priority} | 截止: {t.PlanEndDate:yyyy-MM-dd} | 进度: {t.Progress}%"));

            var requestBody = new
            {
                model = settings.DeepSeekModel,
                messages = new object[]
                {
                    new { role = "system", content = "你是一个专业的工作助手。请根据用户今日的任务列表，生成简洁的工作建议。" },
                    new { role = "user", content = $@"我的今日任务：
{taskList}

请给出：
1. 今日工作建议（2-3条）
2. 优先级提醒
3. 简短鼓励

回复要简洁，总字数控制在200字以内。" }
                },
                temperature = 0.7,
                max_tokens = 500
            };

            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var response = await client.PostAsync("https://api.deepseek.com/v1/chat/completions", content);

            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseJson);
                var choices = doc.RootElement.GetProperty("choices");
                if (choices.GetArrayLength() > 0)
                {
                    return choices[0].GetProperty("message").GetProperty("content").GetString() ?? BuildBasicReminder(tasks);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI 每日总结生成失败");
        }

        return BuildBasicReminder(tasks);
    }

    private string BuildBasicReminder(List<Models.ProjectTask> tasks)
    {
        var highPriority = tasks.Where(t => t.Priority == "high").ToList();
        var overdue = tasks.Where(t => t.PlanEndDate != null && t.PlanEndDate < DateTime.Today && t.Status != "completed").ToList();

        var sb = new StringBuilder();
        sb.AppendLine("📋 今日任务概览");
        sb.AppendLine($"共 {tasks.Count} 个任务");

        if (highPriority.Any())
        {
            sb.AppendLine($"\n🔴 高优先级 ({highPriority.Count})：");
            foreach (var t in highPriority.Take(3))
            {
                sb.AppendLine($"  • {t.Title}");
            }
        }

        if (overdue.Any())
        {
            sb.AppendLine($"\n⚠️ 已逾期 ({overdue.Count})：");
            foreach (var t in overdue)
            {
                sb.AppendLine($"  • {t.Title} (截止: {t.PlanEndDate:MM-dd})");
            }
        }

        return sb.ToString();
    }

    private async Task<string> BuildContextInfo(int? projectId, int? taskId)
    {
        var sb = new StringBuilder();

        if (projectId.HasValue)
        {
            var project = await _context.Projects
                .Include(p => p.Tasks)
                .FirstOrDefaultAsync(p => p.Id == projectId);
            if (project != null)
            {
                sb.AppendLine($"当前项目: {project.Name}");
                sb.AppendLine($"项目类型: {(project.Type == "work" ? "工作" : "私单")}");
                sb.AppendLine($"项目状态: {(project.Status == "active" ? "进行中" : project.Status == "completed" ? "已完成" : "已归档")}");
                sb.AppendLine($"任务数量: {project.Tasks.Count}");
                sb.AppendLine($"已完成: {project.Tasks.Count(t => t.Status == "completed")}");

                if (project.Tasks.Any())
                {
                    sb.AppendLine("任务列表:");
                    foreach (var t in project.Tasks.OrderBy(t => t.SortOrder).Take(10))
                    {
                        sb.AppendLine($"  - [{t.Title}] 状态:{t.Status} 进度:{t.Progress}%");
                    }
                }
            }
        }

        if (taskId.HasValue)
        {
            var task = await _context.Tasks
                .Include(t => t.Project)
                .FirstOrDefaultAsync(t => t.Id == taskId);
            if (task != null)
            {
                sb.AppendLine($"\n当前任务: {task.Title}");
                sb.AppendLine($"所属项目: {task.Project?.Name}");
                sb.AppendLine($"状态: {task.Status}");
                sb.AppendLine($"进度: {task.Progress}%");
                sb.AppendLine($"预估工时: {task.EstimatedHours}h");
                if (task.PlanStartDate.HasValue)
                    sb.AppendLine($"计划: {task.PlanStartDate:yyyy-MM-dd} ~ {task.PlanEndDate:yyyy-MM-dd}");
            }
        }

        return sb.ToString();
    }
}

public class AiResponse
{
    public bool Success { get; set; }
    public string? Content { get; set; }
    public string? Error { get; set; }
}
