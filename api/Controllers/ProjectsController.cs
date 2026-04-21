using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectHub.Api.Data;
using ProjectHub.Api.Models;

namespace ProjectHub.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProjectsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ProjectsController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>获取所有项目</summary>
    [HttpGet]
    public async Task<IActionResult> GetProjects([FromQuery] string? type = null, [FromQuery] string? status = null, [FromQuery] string? search = null)
    {
        var query = _context.Projects
            .Include(p => p.Tasks)
            .AsQueryable();

        if (!string.IsNullOrEmpty(type) && type != "all")
            query = query.Where(p => p.Type == type);

        if (!string.IsNullOrEmpty(status) && status != "all")
            query = query.Where(p => p.Status == status);

        if (!string.IsNullOrEmpty(search))
            query = query.Where(p => p.Name.Contains(search) || (p.Customer != null && p.Customer.Contains(search)));

        var projects = await query
            .OrderByDescending(p => p.UpdatedAt)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Description,
                p.Type,
                p.Customer,
                p.Status,
                p.CreatedAt,
                p.UpdatedAt,
                p.CompletedAt,
                TaskCount = p.Tasks.Count,
                CompletedTaskCount = p.Tasks.Count(t => t.Status == "completed"),
                // 项目进度 = 所有任务(预估工时 × 进度) 的总和 / 所有任务的预估工时总和
                // 修复：确保预估工时总和大于0，避免除零错误
                Progress = p.Tasks.Any()
                    ? (int)Math.Round(p.Tasks.Sum(t => t.EstimatedHours * t.Progress / 100m) / Math.Max(p.Tasks.Sum(t => t.EstimatedHours), 1) * 100)
                    : 0,
                // 项目截止日期 = 所有任务中最晚的计划完成时间
                MaxPlanEndDate = p.Tasks.Any(t => t.PlanEndDate.HasValue) 
                    ? p.Tasks.Max(t => t.PlanEndDate) 
                    : (DateTime?)null
            })
            .ToListAsync();

        return Ok(projects);
    }

    /// <summary>获取单个项目</summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetProject(int id)
    {
        var project = await _context.Projects
            .Include(p => p.Tasks.Where(t => t.Status != "archived"))
            .Include(p => p.Reviews)
            .Include(p => p.Timelines.OrderByDescending(t => t.OccurredAt))
            .FirstOrDefaultAsync(p => p.Id == id);

        if (project == null)
            return NotFound(new { message = "项目不存在" });

        return Ok(project);
    }

    /// <summary>创建项目</summary>
    [HttpPost]
    public async Task<IActionResult> CreateProject([FromBody] CreateProjectRequest request)
    {
        var project = new Project
        {
            Name = request.Name,
            Description = request.Description,
            Type = request.Type,
            Customer = request.Customer,
            Status = string.IsNullOrEmpty(request.Status) ? "active" : request.Status,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        // 创建时间线记录
        var timeline = new Timeline
        {
            ProjectId = project.Id,
            EventType = "created",
            Title = "项目创建",
            Description = $"创建了项目「{project.Name}」",
            OccurredAt = DateTime.Now
        };
        _context.Timelines.Add(timeline);
        await _context.SaveChangesAsync();

        return Created($"/api/projects/{project.Id}", project);
    }

    /// <summary>更新项目</summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProject(int id, [FromBody] UpdateProjectRequest request)
    {
        var project = await _context.Projects.FindAsync(id);
        if (project == null)
            return NotFound(new { message = "项目不存在" });

        project.Name = request.Name ?? project.Name;
        project.Description = request.Description ?? project.Description;
        project.Type = request.Type ?? project.Type;
        project.Customer = request.Customer ?? project.Customer;
        project.Status = request.Status ?? project.Status;
        project.UpdatedAt = DateTime.Now;

        if (request.Status == "completed" && project.CompletedAt == null)
        {
            project.CompletedAt = DateTime.Now;

            // 添加完成时间线
            _context.Timelines.Add(new Timeline
            {
                ProjectId = project.Id,
                EventType = "completed",
                Title = "项目完成",
                Description = $"项目「{project.Name}」已标记完成",
                OccurredAt = DateTime.Now
            });
        }

        await _context.SaveChangesAsync();
        return Ok(project);
    }

    /// <summary>删除项目</summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProject(int id)
    {
        var project = await _context.Projects.FindAsync(id);
        if (project == null)
            return NotFound(new { message = "项目不存在" });

        _context.Projects.Remove(project);
        await _context.SaveChangesAsync();

        return Ok(new { message = "删除成功" });
    }

    /// <summary>获取项目任务列表</summary>
    [HttpGet("{id}/tasks")]
    public async Task<IActionResult> GetProjectTasks(int id)
    {
        var project = await _context.Projects.FindAsync(id);
        if (project == null)
            return NotFound(new { message = "项目不存在" });

        var tasks = await _context.Tasks
            .Where(t => t.ProjectId == id && t.Status != "archived")
            .OrderByDescending(t => t.UpdatedAt)
            .Select(t => new
            {
                t.Id,
                t.Title,
                t.Description,
                t.Category,
                t.Priority,
                t.Status,
                t.Progress,
                t.EstimatedHours,
                t.PlanStartDate,
                t.PlanEndDate,
                t.ActualStartDate,
                t.ActualEndDate,
                t.CreatedAt,
                t.UpdatedAt
            })
            .ToListAsync();

        return Ok(tasks);
    }

    /// <summary>创建项目任务</summary>
    [HttpPost("{id}/tasks")]
    public async Task<IActionResult> CreateProjectTask(int id, [FromBody] CreateProjectTaskRequest request)
    {
        var project = await _context.Projects.FindAsync(id);
        if (project == null)
            return NotFound(new { message = "项目不存在" });

        var task = new ProjectTask
        {
            ProjectId = id,
            Title = request.Title,
            Description = request.Description,
            Category = request.Category ?? "dev",
            Priority = request.Priority ?? "medium",
            Status = request.Status ?? "todo",
            Progress = request.Progress,
            EstimatedHours = request.EstimatedHours,
            PlanStartDate = request.PlanStartDate,
            PlanEndDate = request.PlanEndDate,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        // 创建时间线记录
        var timeline = new Timeline
        {
            ProjectId = id,
            EventType = "task_created",
            Title = "任务创建",
            Description = $"创建了任务「{task.Title}」",
            OccurredAt = DateTime.Now
        };
        _context.Timelines.Add(timeline);
        await _context.SaveChangesAsync();

        return Created($"/api/tasks/{task.Id}", task);
    }

    /// <summary>更新项目任务</summary>
    [HttpPut("tasks/{taskId}")]
    public async Task<IActionResult> UpdateTask(int taskId, [FromBody] UpdateTaskRequest request)
    {
        var task = await _context.Tasks.FindAsync(taskId);
        if (task == null)
            return NotFound(new { message = "任务不存在" });

        if (request.Title != null) task.Title = request.Title;
        if (request.Description != null) task.Description = request.Description;
        if (request.Category != null) task.Category = request.Category;
        if (request.Priority != null) task.Priority = request.Priority;
        if (request.Status != null)
        {
            task.Status = request.Status;
            // 如果状态设为完成，进度自动设为100
            if (request.Status == "completed")
            {
                task.Progress = 100;
                task.ActualEndDate = DateTime.Now;
            }
        }
        if (request.Progress.HasValue)
        {
            task.Progress = request.Progress.Value;
            // 如果进度到100，自动设状态为完成
            if (request.Progress.Value >= 100 && task.Status != "completed")
            {
                task.Status = "completed";
                task.ActualEndDate = DateTime.Now;
            }
            else if (request.Progress.Value < 100 && task.Status == "completed")
            {
                task.Status = "in_progress";
            }
        }
        if (request.EstimatedHours.HasValue) task.EstimatedHours = request.EstimatedHours.Value;
        if (request.PlanStartDate.HasValue) task.PlanStartDate = request.PlanStartDate;
        if (request.PlanEndDate.HasValue) task.PlanEndDate = request.PlanEndDate;

        task.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync();

        return Ok(task);
    }

    /// <summary>更新任务进度</summary>
    [HttpPatch("tasks/{taskId}/progress")]
    public async Task<IActionResult> UpdateTaskProgress(int taskId, [FromBody] UpdateTaskProgressRequest request)
    {
        var task = await _context.Tasks.FindAsync(taskId);
        if (task == null)
            return NotFound(new { message = "任务不存在" });

        task.Progress = request.Progress;

        // 进度到100时自动完成
        if (request.Progress >= 100 && task.Status != "completed")
        {
            task.Status = "completed";
            task.ActualEndDate = DateTime.Now;
        }
        // 进度低于100时恢复为进行中
        else if (request.Progress < 100 && task.Status == "completed")
        {
            task.Status = "in_progress";
        }

        task.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync();

        return Ok(new { task.Id, task.Progress, task.Status });
    }

    /// <summary>删除任务</summary>
    [HttpDelete("tasks/{taskId}")]
    public async Task<IActionResult> DeleteTask(int taskId)
    {
        var task = await _context.Tasks.FindAsync(taskId);
        if (task == null)
            return NotFound(new { message = "任务不存在" });

        _context.Tasks.Remove(task);
        await _context.SaveChangesAsync();

        return Ok(new { message = "删除成功" });
    }
}

public class CreateProjectRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Type { get; set; } = "web";
    public string? Customer { get; set; }
    public string Status { get; set; } = "in_progress";
}

public class UpdateProjectRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Type { get; set; }
    public string? Customer { get; set; }
    public string? Status { get; set; }
}

public class CreateProjectTaskRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string? Priority { get; set; }
    public string? Status { get; set; }
    public int Progress { get; set; }
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
    public string? Status { get; set; }
    public int? Progress { get; set; }
    public decimal? EstimatedHours { get; set; }
    public DateTime? PlanStartDate { get; set; }
    public DateTime? PlanEndDate { get; set; }
}

public class UpdateTaskProgressRequest
{
    public int Progress { get; set; }
}
