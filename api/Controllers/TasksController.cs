using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectHub.Api.Data;
using ProjectHub.Api.Models;

namespace ProjectHub.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TasksController : ControllerBase
{
    private readonly AppDbContext _context;

    public TasksController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>获取所有任务（支持按项目筛选）</summary>
    [HttpGet]
    public async Task<IActionResult> GetAllTasks([FromQuery] int? projectId = null, [FromQuery] string? status = null)
    {
        var query = _context.Tasks
            .Include(t => t.Project)
            .AsQueryable();

        if (projectId.HasValue)
            query = query.Where(t => t.ProjectId == projectId.Value);
        
        if (!string.IsNullOrEmpty(status))
            query = query.Where(t => t.Status == status);

        var tasks = await query
            .OrderByDescending(t => t.UpdatedAt)
            .ToListAsync();

        // 转换为前端需要的格式
        var result = tasks.Select(t => new {
            id = t.Id,
            title = t.Title,
            description = t.Description,
            status = t.Status,
            priority = t.Priority,
            progress = t.Progress,
            projectId = t.ProjectId,
            projectName = t.Project?.Name,
            projectType = t.Project?.Type,
            categoryName = t.Category,
            categoryColor = t.Category,
            dueDate = t.PlanEndDate,
            startDate = t.PlanStartDate,
            estimatedHours = t.EstimatedHours,
            createdAt = t.CreatedAt,
            updatedAt = t.UpdatedAt
        });

        return Ok(result);
    }

    /// <summary>获取项目的所有任务</summary>
    [HttpGet("project/{projectId}")]
    public async Task<IActionResult> GetTasksByProject(int projectId)
    {
        var tasks = await _context.Tasks
            .Where(t => t.ProjectId == projectId && t.Status != "archived")
            .OrderBy(t => t.SortOrder)
            .Include(t => t.TaskTimelines.OrderByDescending(tt => tt.OccurredAt))
            .Include(t => t.Delays.OrderByDescending(d => d.CreatedAt))
            .Include(t => t.ExtraRequirements)
            .ToListAsync();

        return Ok(tasks);
    }

    /// <summary>获取单个任务</summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetTask(int id)
    {
        var task = await _context.Tasks
            .Include(t => t.Project)
            .Include(t => t.TaskTimelines.OrderByDescending(tt => tt.OccurredAt))
            .Include(t => t.Delays.OrderByDescending(d => d.CreatedAt))
            .Include(t => t.ExtraRequirements)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (task == null)
            return NotFound(new { message = "任务不存在" });

        return Ok(task);
    }

    /// <summary>创建任务</summary>
    [HttpPost]
    public async Task<IActionResult> CreateTask([FromBody] CreateTaskRequest request)
    {
        // 如果没有指定项目ID，创建一个默认项目
        int effectiveProjectId = request.ProjectId;
        if (effectiveProjectId == 0)
        {
            var defaultProject = await _context.Projects.FirstOrDefaultAsync(p => p.Name == "默认项目");
            if (defaultProject == null)
            {
                defaultProject = new Project
                {
                    Name = "默认项目",
                    Description = "系统自动创建的项目",
                    Type = "other",
                    Status = "in_progress",
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };
                _context.Projects.Add(defaultProject);
                await _context.SaveChangesAsync();
            }
            effectiveProjectId = defaultProject.Id;
        }

        var project = await _context.Projects.FindAsync(effectiveProjectId);
        if (project == null)
            return NotFound(new { message = "项目不存在" });

        // 获取最大排序号
        var maxSort = await _context.Tasks
            .Where(t => t.ProjectId == effectiveProjectId)
            .MaxAsync(t => (int?)t.SortOrder) ?? 0;

        var task = new ProjectTask
        {
            ProjectId = effectiveProjectId,
            Title = request.Title,
            Description = request.Description,
            Category = request.Category,
            Priority = request.Priority,
            EstimatedHours = request.EstimatedHours,
            PlanStartDate = request.PlanStartDate,
            PlanEndDate = request.PlanEndDate,
            SortOrder = maxSort + 1,
            Status = "todo",
            Progress = 0,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        // 添加任务时间线
        await AddTaskTimeline(task.Id, "created", "任务创建", null, null, $"创建了任务「{task.Title}」");

        // 更新项目更新时间
        project.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync();

        return Created($"/api/tasks/{task.Id}", task);
    }

    /// <summary>更新任务（PATCH 部分更新）</summary>
    [HttpPatch("{id}")]
    public async Task<IActionResult> PatchTask(int id, [FromBody] UpdateTaskRequest request)
    {
        return await UpdateTask(id, request);
    }

    /// <summary>更新任务</summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTask(int id, [FromBody] UpdateTaskRequest request)
    {
        var task = await _context.Tasks.FindAsync(id);
        if (task == null)
            return NotFound(new { message = "任务不存在" });

        var oldStatus = task.Status;
        var oldProgress = task.Progress;

        if (request.Title != null) task.Title = request.Title;
        if (request.Description != null) task.Description = request.Description;
        if (request.Category != null) task.Category = request.Category;
        if (request.Priority != null) task.Priority = request.Priority;
        if (request.EstimatedHours.HasValue) task.EstimatedHours = request.EstimatedHours.Value;
        if (request.PlanStartDate.HasValue) task.PlanStartDate = request.PlanStartDate;
        if (request.PlanEndDate.HasValue) task.PlanEndDate = request.PlanEndDate;
        if (request.SortOrder.HasValue) task.SortOrder = request.SortOrder.Value;
        if (request.Progress.HasValue) task.Progress = request.Progress.Value;

        // 处理状态变更
        if (request.Status != null && request.Status != oldStatus)
        {
            task.Status = request.Status;

            // 待办 → 进行中：记录实际开始时间
            if (request.Status == "in_progress" && task.ActualStartDate == null)
            {
                task.ActualStartDate = DateTime.Now;
                await AddTaskTimeline(task.Id, "status_changed", "开始执行", oldStatus, request.Status, "任务开始执行");
            }
            // 进行中 → 已完成：记录实际完成时间
            else if (request.Status == "completed" && task.ActualEndDate == null)
            {
                task.ActualEndDate = DateTime.Now;
                task.Progress = 100;
                await AddTaskTimeline(task.Id, "completed", "任务完成", oldStatus, request.Status, "任务已完成");

                // 添加项目时间线
                await AddProjectTimeline(task.ProjectId, "task_completed", "任务完成", $"任务「{task.Title}」已完成", task.Id);
            }
        }

        task.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync();

        return Ok(task);
    }

    /// <summary>删除任务</summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTask(int id)
    {
        var task = await _context.Tasks.FindAsync(id);
        if (task == null)
            return NotFound(new { message = "任务不存在" });

        _context.Tasks.Remove(task);
        await _context.SaveChangesAsync();

        return Ok(new { message = "删除成功" });
    }

    /// <summary>延期任务</summary>
    [HttpPost("{id}/delay")]
    public async Task<IActionResult> DelayTask(int id, [FromBody] DelayTaskRequest request)
    {
        var task = await _context.Tasks.FindAsync(id);
        if (task == null)
            return NotFound(new { message = "任务不存在" });

        if (!task.PlanEndDate.HasValue)
            return BadRequest(new { message = "任务没有计划结束时间" });

        // 记录延期
        var delay = new TaskDelay
        {
            TaskId = task.Id,
            Reason = request.Reason,
            OriginalPlanEndDate = task.PlanEndDate.Value,
            NewPlanEndDate = request.NewPlanEndDate,
            CreatedAt = DateTime.Now
        };
        _context.TaskDelays.Add(delay);

        // 更新任务计划时间
        task.PlanEndDate = request.NewPlanEndDate;
        task.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        // 添加时间线
        await AddTaskTimeline(task.Id, "delayed", "任务延期", task.PlanEndDate.ToString(), request.NewPlanEndDate.ToString(), request.Reason);
        await AddProjectTimeline(task.ProjectId, "task_delayed", "任务延期", $"任务「{task.Title}」延期至{request.NewPlanEndDate:yyyy-MM-dd}", task.Id);

        return Ok(new { delay, task });
    }

    /// <summary>添加计划外需求</summary>
    [HttpPost("{id}/extra")]
    public async Task<IActionResult> AddExtraRequirement(int id, [FromBody] AddExtraRequest request)
    {
        var task = await _context.Tasks.FindAsync(id);
        if (task == null)
            return NotFound(new { message = "任务不存在" });

        // 添加计划外需求
        var extra = new TaskExtraRequirement
        {
            TaskId = task.Id,
            Description = request.Description,
            CreatedAt = DateTime.Now
        };
        _context.TaskExtraRequirements.Add(extra);

        // 重置任务状态
        var oldStatus = task.Status;
        task.Status = "todo";
        task.Progress = 0;
        task.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        // 添加时间线
        await AddTaskTimeline(task.Id, "extra_added", "追加需求", oldStatus, "todo", request.Description);
        await AddProjectTimeline(task.ProjectId, "extra_added", "追加需求", $"任务「{task.Title}」追加了新需求", task.Id);

        return Ok(new { extra, task });
    }

    private async Task AddTaskTimeline(int taskId, string changeType, string title, string? oldValue, string? newValue, string? details)
    {
        _context.TaskTimelines.Add(new TaskTimeline
        {
            TaskId = taskId,
            ChangeType = changeType,
            Title = title,
            OldValue = oldValue,
            NewValue = newValue,
            Details = details,
            OccurredAt = DateTime.Now
        });
        await _context.SaveChangesAsync();
    }

    private async Task AddProjectTimeline(int projectId, string eventType, string title, string? description, int? taskId)
    {
        _context.Timelines.Add(new Timeline
        {
            ProjectId = projectId,
            EventType = eventType,
            Title = title,
            Description = description,
            TaskId = taskId,
            OccurredAt = DateTime.Now
        });
        await _context.SaveChangesAsync();
    }
}

public class CreateTaskRequest
{
    public int ProjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = "dev";
    public string Priority { get; set; } = "medium";
    public decimal EstimatedHours { get; set; }
    public DateTime? PlanStartDate { get; set; }
    public DateTime? PlanEndDate { get; set; }
}

public class UpdateTaskRequest
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string? Priority { get; set; }
    public decimal? EstimatedHours { get; set; }
    public DateTime? PlanStartDate { get; set; }
    public DateTime? PlanEndDate { get; set; }
    public int? SortOrder { get; set; }
    public string? Status { get; set; }
    public int? Progress { get; set; }
}

public class DelayTaskRequest
{
    public string Reason { get; set; } = string.Empty;
    public DateTime NewPlanEndDate { get; set; }
}

public class AddExtraRequest
{
    public string Description { get; set; } = string.Empty;
}
