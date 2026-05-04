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
    [HttpGet]
    public async Task<IActionResult> GetGanttData([FromQuery] int days = 30, [FromQuery] DateTime? startDate = null)
    {
        var baseDate = startDate ?? DateTime.Today.AddDays(-7); // 默认过去7天

        var start = baseDate.Date;
        var end = start.AddDays(days);

        // 获取时间范围内的所有任务
        var tasks = await _context.Tasks
            .Include(t => t.Project)
            .Where(t =>
                t.Status != "archived" && // 只排除归档任务
                ((t.PlanStartDate != null && t.PlanStartDate <= end) || (t.PlanEndDate != null && t.PlanEndDate >= start)) ||
                t.PlanStartDate == null && t.PlanEndDate == null)
            .OrderBy(t => t.ProjectId)
            .ThenBy(t => t.SortOrder)
            .ToListAsync();

        // 获取所有相关子任务
        var taskIds = tasks.Select(t => t.Id).ToList();
        var subTasks = await _context.SubTasks
            .Where(s => taskIds.Contains(s.ParentTaskId))
            .ToListAsync();

        // 构建甘特图数据，日期转为字符串避免时区问题
        var ganttItems = tasks.Select(t => 
        {
            // 计算该任务的子任务进度
            var taskSubTasks = subTasks.Where(s => s.ParentTaskId == t.Id).ToList();
            var totalHours = taskSubTasks.Sum(s => s.EstimatedHours);
            var completedHours = taskSubTasks.Where(s => s.IsCompleted).Sum(s => s.EstimatedHours);
            var progress = totalHours > 0 ? (int)Math.Round(completedHours * 100m / totalHours) : 0;

            return new
            {
                t.Id,
                t.Title,
                t.ProjectId,
                ProjectName = t.Project != null ? t.Project.Name : "",
                ProjectColor = t.Project != null ? (t.Project.Type == "work" ? "#4A90D9" : "#67CBAB") : "#999",
                planStartDate = (t.PlanStartDate ?? DateTime.Today).ToString("yyyy-MM-dd"),
                planEndDate = (t.PlanEndDate ?? (t.PlanStartDate?.AddDays(1) ?? DateTime.Today.AddDays(1))).ToString("yyyy-MM-dd"),
                Duration = t.PlanStartDate.HasValue && t.PlanEndDate.HasValue
                    ? (int)(t.PlanEndDate.Value - t.PlanStartDate.Value).TotalDays + 1
                    : 1,
                t.Status,
                t.Priority,
                t.Category,
                Progress = progress,
                TotalEstimatedHours = totalHours
            };
        }).ToList();

        return Ok(new
        {
            startDate = start.ToString("yyyy-MM-dd"),
            endDate = end.ToString("yyyy-MM-dd"),
            totalDays = days,
            items = ganttItems
        });
    }
}
