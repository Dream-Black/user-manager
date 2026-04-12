using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectHub.Api.Data;
using ProjectHub.Api.Models;

namespace ProjectHub.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GanttController : ControllerBase
{
    private readonly AppDbContext _context;

    public GanttController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>获取甘特图数据</summary>
    /// <param name="days">时间范围天数，默认30天</param>
    /// <param name="startDate">开始日期</param>
    public async Task<IActionResult> GetGanttData([FromQuery] int days = 30, [FromQuery] DateTime? startDate = null)
    {
        var baseDate = startDate ?? DateTime.Today.AddDays(-7); // 默认过去7天

        var start = baseDate.Date;
        var end = start.AddDays(days);

        // 获取时间范围内的所有任务
        var tasks = await _context.Tasks
            .Include(t => t.Project)
            .Where(t =>
                t.Status != "archived" &&
                t.Status != "completed" && // 排除已完成的任务
                ((t.PlanStartDate != null && t.PlanStartDate <= end) || (t.PlanEndDate != null && t.PlanEndDate >= start)) ||
                t.PlanStartDate == null && t.PlanEndDate == null)
            .OrderBy(t => t.ProjectId)
            .ThenBy(t => t.SortOrder)
            .Select(t => new
            {
                t.Id,
                t.Title,
                t.ProjectId,
                ProjectName = t.Project != null ? t.Project.Name : "",
                ProjectColor = t.Project != null ? (t.Project.Type == "work" ? "#4A90D9" : "#67CBAB") : "#999",
                t.PlanStartDate,
                t.PlanEndDate,
                t.Status,
                t.Priority,
                t.Category,
                t.Progress
            })
            .ToListAsync();

        // 构建甘特图数据
        var ganttItems = tasks.Select(t => new
        {
            t.Id,
            t.Title,
            t.ProjectId,
            t.ProjectName,
            t.ProjectColor,
            StartDate = t.PlanStartDate ?? DateTime.Today,
            EndDate = t.PlanEndDate ?? (t.PlanStartDate?.AddDays(1) ?? DateTime.Today.AddDays(1)),
            Duration = t.PlanStartDate.HasValue && t.PlanEndDate.HasValue
                ? (int)(t.PlanEndDate.Value - t.PlanStartDate.Value).TotalDays + 1
                : 1,
            t.Status,
            t.Priority,
            t.Category,
            t.Progress
        }).ToList();

        return Ok(new
        {
            startDate = start,
            endDate = end,
            totalDays = days,
            items = ganttItems
        });
    }
}
