using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using ProjectHub.Api.Data;

namespace ProjectHub.Api.Services.Ai;

public class AiToolService
{
    private readonly AppDbContext _context;
    private readonly ILogger<AiToolService> _logger;

    public AiToolService(AppDbContext context, ILogger<AiToolService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public object[] BuildFunctionTools()
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
                    parameters = new { type = "object", properties = new { }, required = Array.Empty<string>() }
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
                            status = new { type = "string", description = "项目状态过滤：active（进行中）、completed（已完成）、all（全部）", Enum = new[] { "active", "completed", "all" } }
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
                        properties = new { task_id = new { type = "integer", description = "任务ID" } },
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
                            computer_id = new { type = "integer", description = "电脑ID" },
                            type = new { type = "string", description = "资源类型：comic/video/novel/image" }
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
                    name = "search_notes",
                    description = "搜索用户的笔记内容，支持按关键词、标签进行筛选。返回笔记列表包含标题、内容摘要、标签和更新时间。",
                    parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            keyword = new { type = "string", description = "搜索关键词，用于匹配笔记标题和内容" },
                            tag = new { type = "string", description = "标签筛选，只返回包含该标签的笔记" }
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
                    parameters = new { type = "object", properties = new { }, required = Array.Empty<string>() }
                }
            },
            new
            {
                type = "function",
                function = new
                {
                    name = "get_terminal_usage_guide",
                    description = "提供当前项目中正确使用终端命令的方法、注意事项和推荐目录。用户要求查看项目架构或目录结构时优先调用此工具，再决定是否输出终端命令。",
                    parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            path = new { type = "string", description = "目标项目路径，例如 C:\\Users\\22618\\Desktop\\AI Claw" },
                            objective = new { type = "string", description = "用户目标，例如了解架构、查看目录、定位文件" }
                        },
                        required = Array.Empty<string>()
                    }
                }
            }
        };
    }

    public async Task<string> ExecuteToolCallAsync(string functionName, string arguments)
    {
        try
        {
            return functionName switch
            {
                "get_today_tasks" => await GetTodayTasksAsync(),
                "get_projects" => await GetProjectsAsync(arguments),
                "get_task_detail" => await GetTaskDetailAsync(arguments),
                "get_resource_paths" => await GetResourcePathsAsync(arguments),
                "search_notes" => await SearchNotesAsync(arguments),
                "get_statistics" => await GetStatisticsAsync(),
                "get_terminal_usage_guide" => await GetTerminalUsageGuideAsync(arguments),
                _ => JsonSerializer.Serialize(new { error = $"未知工具: {functionName}" })
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "工具调用失败: {Tool}", functionName);
            return JsonSerializer.Serialize(new { error = $"查询失败: {ex.Message}" });
        }
    }

    private async Task<string> GetTodayTasksAsync()
    {
        var today = DateTime.Today;
        var tasks = await _context.Tasks.Include(t => t.Project).Where(t => t.Status != "archived").OrderBy(t => t.Priority == "high" ? 0 : t.Priority == "medium" ? 1 : 2).ThenBy(t => t.PlanEndDate).Select(t => new { t.Id, t.Title, t.Status, t.Priority, t.Progress, t.EstimatedHours, PlanStartDate = t.PlanStartDate, PlanEndDate = t.PlanEndDate, ActualEndDate = t.ActualEndDate, ProjectName = t.Project != null ? t.Project.Name : null, ProjectStatus = t.Project != null ? t.Project.Status : null }).ToListAsync();
        var todayTasks = tasks.Where(t => (t.PlanStartDate != null && t.PlanStartDate <= today && t.PlanEndDate >= today) || (t.PlanEndDate.HasValue && t.PlanEndDate.Value.Date == today) || (t.Status == "in_progress") || (t.Status == "todo" && t.Priority == "high")).ToList();
        return JsonSerializer.Serialize(new { today_date = today.ToString("yyyy-MM-dd"), total_tasks = tasks.Count, today_active_tasks = todayTasks, high_priority = tasks.Where(t => t.Priority == "high" && t.Status != "completed").ToList(), overdue = tasks.Where(t => t.PlanEndDate.HasValue && t.PlanEndDate.Value.Date < today && t.Status != "completed").ToList(), completed_today = tasks.Count(t => t.Status == "completed" && t.ActualEndDate?.Date == today) });
    }

    private async Task<string> GetProjectsAsync(string arguments)
    {
        var args = JsonSerializer.Deserialize<Dictionary<string, string>>(arguments);
        var statusFilter = args?.ContainsKey("status") == true ? args["status"] : "all";
        var query = _context.Projects.Include(p => p.Tasks).AsQueryable();
        if (statusFilter != "all") query = query.Where(p => p.Status == statusFilter);
        var projects = await query.OrderByDescending(p => p.UpdatedAt).Select(p => new { p.Id, p.Name, p.Type, p.Status, p.Customer, TaskCount = p.Tasks.Count, CompletedTasks = p.Tasks.Count(t => t.Status == "completed"), InProgressTasks = p.Tasks.Count(t => t.Status == "in_progress"), Progress = p.Tasks.Any() ? (int)Math.Round(p.Tasks.Sum(t => t.EstimatedHours * t.Progress / 100m) / Math.Max(p.Tasks.Sum(t => t.EstimatedHours), 1) * 100) : 0, p.CreatedAt, p.UpdatedAt }).ToListAsync();
        return JsonSerializer.Serialize(projects);
    }

    private async Task<string> GetTaskDetailAsync(string arguments)
    {
        var args = JsonSerializer.Deserialize<Dictionary<string, int>>(arguments);
        if (args == null || !args.ContainsKey("task_id")) return JsonSerializer.Serialize(new { error = "缺少 task_id 参数" });
        var taskId = args["task_id"];
        var task = await _context.Tasks.Include(t => t.Project).Include(t => t.Delays).Include(t => t.ExtraRequirements).Include(t => t.SubTasks).FirstOrDefaultAsync(t => t.Id == taskId);
        if (task == null) return JsonSerializer.Serialize(new { error = $"任务 {taskId} 不存在" });
        return JsonSerializer.Serialize(new { task.Id, task.Title, task.Description, task.Status, task.Priority, task.Progress, task.EstimatedHours, PlanStartDate = task.PlanStartDate, PlanEndDate = task.PlanEndDate, ActualStartDate = task.ActualStartDate, ActualEndDate = task.ActualEndDate, Project = task.Project != null ? new { task.Project.Id, task.Project.Name } : null, Delays = task.Delays.Select(d => new { d.Reason, OriginalEnd = d.OriginalPlanEndDate, NewEnd = d.NewPlanEndDate, d.CreatedAt }), ExtraRequirements = task.ExtraRequirements.Select(e => new { e.Description, e.CreatedAt }), SubTasks = task.SubTasks.Select(s => new { s.Id, s.Title, s.IsCompleted }), task.CreatedAt, task.UpdatedAt });
    }

    private async Task<string> GetResourcePathsAsync(string arguments)
    {
        var args = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(arguments);
        var query = _context.ResourcePaths.Include(r => r.Computer).AsQueryable();
        if (args != null)
        {
            if (args.TryGetValue("computer_id", out var computerIdElement) && computerIdElement.TryGetInt32(out var computerId)) query = query.Where(r => r.ComputerId == computerId);
            if (args.TryGetValue("type", out var typeElement) && typeElement.ValueKind == JsonValueKind.String) query = query.Where(r => r.Type == typeElement.GetString());
        }
        var paths = await query.OrderByDescending(r => r.CreatedAt).Select(r => new { r.Id, r.ComputerId, ComputerName = r.Computer != null ? r.Computer.Name : null, r.Type, r.Path, r.IsEnabled, r.CreatedAt }).ToListAsync();
        return JsonSerializer.Serialize(paths);
    }

    private async Task<string> SearchNotesAsync(string arguments)
    {
        var args = JsonSerializer.Deserialize<Dictionary<string, string>>(arguments);
        var keyword = args?.ContainsKey("keyword") == true ? args["keyword"] : null;
        var tag = args?.ContainsKey("tag") == true ? args["tag"] : null;

        var query = _context.Notes.Include(n => n.Tags).AsQueryable();

        if (!string.IsNullOrEmpty(tag))
        {
            query = query.Where(n => n.Tags.Any(t => t.TagId == tag));
        }

        if (!string.IsNullOrEmpty(keyword))
        {
            query = query.Where(n => (n.Title != null && n.Title.Contains(keyword)) || (n.Content != null && n.Content.Contains(keyword)));
        }

        var notes = await query.OrderByDescending(n => n.UpdatedAt)
            .Select(n => new
            {
                n.Id,
                n.Title,
                ContentPreview = n.Content != null && n.Content.Length > 200 ? n.Content.Substring(0, 200) + "..." : n.Content ?? "",
                Tags = n.Tags.Select(t => t.TagId).ToList(),
                n.UpdatedAt
            })
            .ToListAsync();

        return JsonSerializer.Serialize(new { notes, count = notes.Count });
    }

    private async Task<string> GetStatisticsAsync()
    {
        var allTasks = await _context.Tasks.Where(t => t.Status != "archived").ToListAsync();
        var allProjects = await _context.Projects.ToListAsync();
        var allResources = await _context.ResourcePaths.ToListAsync();
        return JsonSerializer.Serialize(new { total_tasks = allTasks.Count, completed_tasks = allTasks.Count(t => t.Status == "completed"), in_progress_tasks = allTasks.Count(t => t.Status == "in_progress"), todo_tasks = allTasks.Count(t => t.Status == "todo"), high_priority_tasks = allTasks.Count(t => t.Priority == "high" && t.Status != "completed"), overdue_tasks = allTasks.Count(t => t.PlanEndDate.HasValue && t.PlanEndDate.Value.Date < DateTime.Today && t.Status != "completed"), total_projects = allProjects.Count, active_projects = allProjects.Count(p => p.Status == "active"), completed_projects = allProjects.Count(p => p.Status == "completed"), total_resources = allResources.Count, enabled_resources = allResources.Count(r => r.IsEnabled), overall_completion_rate = allTasks.Count > 0 ? $"{Math.Round((double)allTasks.Count(t => t.Status == "completed") / allTasks.Count * 100, 1)}%" : "0%" });
    }

    private Task<string> GetTerminalUsageGuideAsync(string arguments)
    {
        var args = JsonSerializer.Deserialize<Dictionary<string, string>>(arguments ?? "{}") ?? new Dictionary<string, string>();
        var path = args.TryGetValue("path", out var p) ? p : "C:\\Users\\22618\\Desktop\\AI Claw";
        var objective = args.TryGetValue("objective", out var o) ? o : "了解项目架构";
        return Task.FromResult(JsonSerializer.Serialize(new { objective, path, rules = new[] { "先切换到项目根目录再执行目录查看命令", "优先使用 dir / tree / rg 来了解目录结构与文件位置", "如需找代码入口，优先查看 README、sln、package.json、Program.cs、main.js", "不要直接执行危险命令，涉及删除、覆盖、格式化时必须二次确认", "如果目标是理解架构，先查看根目录结构，再按前端、后端、代理分层查看" }, examples = new[] { $"cd \"{path}\"", "dir", "tree /f /a", "rg -n \"AiController|AiService|localBridge|chatStream|terminal\" ." } }));
    }
}
