using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using ProjectHub.Api.Data;
using ProjectHub.Api.Models;

namespace ProjectHub.Api.Services.Ai;

public class AiDraftService
{
    private readonly AppDbContext _context;
    private readonly IFileLogService _fileLog;

    public AiDraftService(AppDbContext context, IFileLogService fileLog)
    {
        _context = context;
        _fileLog = fileLog;
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
        EnsureDraftIsActive(draft);
        ValidateDraft(draft);

        await _fileLog.WriteAsync("AI", "info", "确认草案请求", new
        {
            conversationId,
            messageId,
            draft.Id,
            draft.Kind,
            draft.Mode,
            draft.Title,
            draft.Status,
            draft.ExpiresAt,
            payload = draft.Payload
        });

        var result = draft.Kind switch
        {
            "project" => await ConfirmProjectDraftAsync(draft),
            "task" => await ConfirmTaskDraftAsync(draft),
            "resource" => await ConfirmResourceDraftAsync(draft),
            _ => throw new InvalidOperationException("暂不支持该类型草案")
        };

        draft.Status = "confirmed";

        message.Content = $"已确认并执行草案：{draft.Title ?? draft.Kind}";
        message.Attachments = JsonSerializer.Serialize(new
        {
            id = draft.Id,
            schemaVersion = draft.SchemaVersion,
            type = "action_result",
            status = "confirmed",
            kind = draft.Kind,
            mode = draft.Mode,
            title = draft.Title,
            result
        });
        message.CreatedAt = DateTime.Now;

        _context.ChatMessages.Add(new ChatMessage
        {
            ConversationId = conversationId,
            Role = "system",
            Content = JsonSerializer.Serialize(new
            {
                type = "action_result",
                status = "confirmed",
                kind = draft.Kind,
                mode = draft.Mode,
                title = draft.Title,
                result
            }),
            CreatedAt = DateTime.Now
        });

        await _context.SaveChangesAsync();
        return result;
    }

    public async Task<object?> CancelActionDraftAsync(int conversationId, int messageId)
    {
        var message = await _context.ChatMessages
            .FirstOrDefaultAsync(m => m.Id == messageId && m.ConversationId == conversationId);
        if (message == null) return null;
        if (message.Role != "assistant" || string.IsNullOrWhiteSpace(message.Attachments))
            throw new InvalidOperationException("当前消息没有可取消的草案");

        var draft = ParseDraft(message.Attachments);
        if (draft == null || string.IsNullOrWhiteSpace(draft.Kind) || string.IsNullOrWhiteSpace(draft.Mode))
            throw new InvalidOperationException("草案格式无效");

        EnsureDraftIsActive(draft);
        ValidateDraft(draft);

        message.Content = $"已取消草案：{draft.Title ?? draft.Kind}";
        message.Attachments = JsonSerializer.Serialize(new
        {
            id = draft.Id,
            schemaVersion = draft.SchemaVersion,
            type = "action_result",
            status = "cancelled",
            kind = draft.Kind,
            mode = draft.Mode,
            title = draft.Title
        });
        message.CreatedAt = DateTime.Now;

        _context.ChatMessages.Add(new ChatMessage
        {
            ConversationId = conversationId,
            Role = "system",
            Content = JsonSerializer.Serialize(new
            {
                id = draft.Id,
                schemaVersion = draft.SchemaVersion,
                type = "action_result",
                status = "cancelled",
                kind = draft.Kind,
                mode = draft.Mode,
                title = draft.Title
            }),
            CreatedAt = DateTime.Now
        });

        await _context.SaveChangesAsync();
        return new { kind = draft.Kind, action = "cancelled", title = draft.Title };
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

    private static void EnsureDraftIsActive(ActionDraftDto draft)
    {
        if (!string.Equals(draft.Status, "pending", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("草案已处理，不能重复操作");

        if (draft.ExpiresAt.HasValue && draft.ExpiresAt.Value.ToUniversalTime() < DateTime.UtcNow)
            throw new InvalidOperationException("草案已过期，请重新生成");
    }

    private static void ValidateDraft(ActionDraftDto draft)
    {
        var payload = draft.Payload ?? new JsonObject();

        string RequireString(string key, string message)
        {
            var value = payload[key]?.ToString();
            if (string.IsNullOrWhiteSpace(value)) throw new InvalidOperationException(message);
            return value;
        }

        int RequireInt(string key, string message)
        {
            var value = payload[key]?.GetValue<int?>();
            if (!value.HasValue || value.Value <= 0) throw new InvalidOperationException(message);
            return value.Value;
        }

        switch ($"{draft.Kind}.{draft.Mode}")
        {
            case "project.create":
                RequireString("name", "项目草案缺少名称");
                break;
            case "project.update":
                RequireInt("id", "项目更新草案缺少 ID");
                RequireString("name", "项目更新草案缺少名称");
                break;
            case "task.create":
                RequireString("title", "任务草案缺少标题");
                RequireInt("projectId", "任务草案缺少项目 ID");
                break;
            case "task.update":
                RequireInt("id", "任务更新草案缺少 ID");
                RequireString("title", "任务更新草案缺少标题");
                break;
            case "resource.create":
                RequireInt("computerId", "资源草案缺少电脑 ID");
                RequireString("path", "资源草案缺少路径");
                break;
            case "resource.update":
                RequireInt("id", "资源更新草案缺少 ID");
                RequireString("path", "资源更新草案缺少路径");
                break;
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
                Type = payload["type"]?.ToString() ?? "work",
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
            if (project == null) throw new InvalidOperationException("任务草案关联的项目不存在");

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
            if (computer == null) throw new InvalidOperationException("资源草案关联的电脑不存在");

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
        public string? Id { get; set; }
        public int SchemaVersion { get; set; } = 1;
        public string? Kind { get; set; }
        public string? Mode { get; set; }
        public string? Title { get; set; }
        public string? TargetLabel { get; set; }
        public string? Before { get; set; }
        public string? After { get; set; }
        public string? Preview { get; set; }
        public JsonObject? Payload { get; set; }
        public string? Status { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }
}
