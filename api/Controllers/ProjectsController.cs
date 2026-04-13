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
                Progress = p.Tasks.Any()
                    ? (int)Math.Round(p.Tasks.Sum(t => t.EstimatedHours * t.Progress / 100m) / p.Tasks.Sum(t => t.EstimatedHours) * 100)
                    : 0
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
                t.Priority,
                t.Status,
                t.Progress,
                t.PlanStartDate,
                t.PlanEndDate,
                t.ActualStartDate,
                t.ActualEndDate,
                t.EstimatedHours,
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
            Priority = request.Priority ?? "medium",
            Status = request.Status ?? "todo",
            Progress = 0,
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
    public string? Priority { get; set; }
    public string? Status { get; set; }
    public DateTime? PlanStartDate { get; set; }
    public DateTime? PlanEndDate { get; set; }
}
