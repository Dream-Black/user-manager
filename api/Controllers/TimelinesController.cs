using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectHub.Api.Data;
using ProjectHub.Api.Models;

namespace ProjectHub.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TimelinesController : ControllerBase
{
    private readonly AppDbContext _context;

    public TimelinesController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>获取最近活动时间线（用于首页展示）</summary>
    [HttpGet("recent")]
    public async Task<IActionResult> GetRecentTimelines([FromQuery] int limit = 10)
    {
        var timelines = await _context.Timelines
            .Include(t => t.Project)
            .OrderByDescending(t => t.OccurredAt)
            .Take(limit)
            .ToListAsync();

        var result = timelines.Select(t => new {
            id = t.Id,
            action = t.EventType switch {
                "created" => "创建了项目",
                "status_changed" => "更新了状态",
                "task_completed" => "任务完成",
                "task_delayed" => "任务延期",
                "extra_added" => "追加需求",
                _ => "更新了项目"
            },
            details = t.Description,
            projectName = t.Project != null ? t.Project.Name : "未知项目",
            color = t.EventType switch {
                "created" => "#2563EB",
                "status_changed" => "#3B82F6",
                "task_completed" => "#10B981",
                "task_delayed" => "#EF4444",
                "extra_added" => "#F59E0B",
                _ => "#6B7280"
            },
            createdAt = t.OccurredAt
        }).ToList();

        return Ok(result);
    }

    /// <summary>获取所有项目时间线</summary>
    [HttpGet]
    public async Task<IActionResult> GetTimelines([FromQuery] int? projectId = null)
    {
        var query = _context.Timelines
            .Include(t => t.Project)
            .AsQueryable();

        if (projectId.HasValue)
            query = query.Where(t => t.ProjectId == projectId.Value);

        var timelines = await query
            .OrderByDescending(t => t.OccurredAt)
            .Take(100)
            .ToListAsync();

        return Ok(timelines);
    }

    /// <summary>获取项目时间线</summary>
    [HttpGet("project/{projectId}")]
    public async Task<IActionResult> GetProjectTimelines(int projectId)
    {
        var timelines = await _context.Timelines
            .Where(t => t.ProjectId == projectId)
            .OrderByDescending(t => t.OccurredAt)
            .ToListAsync();

        return Ok(timelines);
    }
}
