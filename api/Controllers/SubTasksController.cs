using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectHub.Api.Data;
using ProjectHub.Api.Models;

namespace ProjectHub.Api.Controllers;

[ApiController]
[Route("api/subtasks")]
public class SubTasksController : ControllerBase
{
    private readonly AppDbContext _context;

    public SubTasksController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// 获取父任务的所有子任务
    /// </summary>
    [HttpGet("by-task/{taskId}")]
    public async Task<IActionResult> GetByTaskId(int taskId)
    {
        var subTasks = await _context.SubTasks
            .Where(s => s.ParentTaskId == taskId)
            .OrderBy(s => s.SortOrder)
            .ThenBy(s => s.Id)
            .ToListAsync();

        return Ok(subTasks);
    }

    /// <summary>
    /// 创建子任务
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSubTaskRequest request)
    {
        var subTask = new SubTask
        {
            ParentTaskId = request.ParentTaskId,
            Title = request.Title,
            Description = request.Description,
            Category = request.Category ?? "dev",
            EstimatedHours = request.EstimatedHours ?? 0,
            IsCompleted = false,
            SortOrder = request.SortOrder,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        _context.SubTasks.Add(subTask);
        await _context.SaveChangesAsync();

        return Ok(subTask);
    }

    /// <summary>
    /// 更新子任务
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateSubTaskRequest request)
    {
        var subTask = await _context.SubTasks.FindAsync(id);
        if (subTask == null)
        {
            return NotFound(new { message = "子任务不存在" });
        }

        if (request.Title != null)
            subTask.Title = request.Title;
        if (request.Description != null)
            subTask.Description = request.Description;
        if (request.Category != null)
            subTask.Category = request.Category;
        if (request.EstimatedHours.HasValue)
            subTask.EstimatedHours = request.EstimatedHours.Value;
        if (request.IsCompleted.HasValue)
            subTask.IsCompleted = request.IsCompleted.Value;
        if (request.SortOrder.HasValue)
            subTask.SortOrder = request.SortOrder.Value;

        subTask.UpdatedAt = DateTime.Now;

        // 检查并更新父任务状态
        var parentTask = await _context.Tasks.FindAsync(subTask.ParentTaskId);
        if (parentTask != null)
        {
            // 获取该父任务的所有子任务
            var allSubTasks = await _context.SubTasks
                .Where(s => s.ParentTaskId == subTask.ParentTaskId)
                .ToListAsync();

            // 计算新的进度
            var totalHours = allSubTasks.Sum(s => s.EstimatedHours);
            var completedHours = allSubTasks.Where(s => s.IsCompleted).Sum(s => s.EstimatedHours);
            parentTask.Progress = totalHours > 0 ? (int)Math.Round(completedHours * 100m / totalHours) : 0;

            // 判断是否所有子任务都已完成
            var allCompleted = allSubTasks.All(s => s.IsCompleted);
            if (allCompleted && parentTask.Status != "completed")
            {
                // 所有子任务都完成，父任务改为完成
                parentTask.Status = "completed";
                parentTask.ActualEndDate = DateTime.Now;
            }
            else if (!allCompleted && parentTask.Status == "completed")
            {
                // 有子任务未完成，父任务改为进行中
                parentTask.Status = "in_progress";
                parentTask.ActualEndDate = null;
            }

            parentTask.UpdatedAt = DateTime.Now;
        }

        await _context.SaveChangesAsync();

        return Ok(subTask);
    }

    /// <summary>
    /// 切换子任务完成状态
    /// </summary>
    [HttpPatch("{id}/toggle")]
    public async Task<IActionResult> ToggleComplete(int id)
    {
        var subTask = await _context.SubTasks.FindAsync(id);
        if (subTask == null)
        {
            return NotFound(new { message = "子任务不存在" });
        }

        subTask.IsCompleted = !subTask.IsCompleted;
        subTask.UpdatedAt = DateTime.Now;

        // 检查并更新父任务状态
        var parentTask = await _context.Tasks.FindAsync(subTask.ParentTaskId);
        if (parentTask != null)
        {
            // 获取该父任务的所有子任务
            var allSubTasks = await _context.SubTasks
                .Where(s => s.ParentTaskId == subTask.ParentTaskId)
                .ToListAsync();

            // 计算新的进度
            var totalHours = allSubTasks.Sum(s => s.EstimatedHours);
            var completedHours = allSubTasks.Where(s => s.IsCompleted).Sum(s => s.EstimatedHours);
            parentTask.Progress = totalHours > 0 ? (int)Math.Round(completedHours * 100m / totalHours) : 0;

            // 判断是否所有子任务都已完成
            var allCompleted = allSubTasks.All(s => s.IsCompleted);
            if (allCompleted && parentTask.Status != "completed")
            {
                // 所有子任务都完成，父任务改为完成
                parentTask.Status = "completed";
                parentTask.ActualEndDate = DateTime.Now;
            }
            else if (!allCompleted && parentTask.Status == "completed")
            {
                // 有子任务未完成，父任务改为进行中
                parentTask.Status = "in_progress";
                parentTask.ActualEndDate = null;
            }

            parentTask.UpdatedAt = DateTime.Now;
        }

        await _context.SaveChangesAsync();

        return Ok(new { subTask.Id, subTask.IsCompleted });
    }

    /// <summary>
    /// 删除子任务
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var subTask = await _context.SubTasks.FindAsync(id);
        if (subTask == null)
        {
            return NotFound(new { message = "子任务不存在" });
        }

        _context.SubTasks.Remove(subTask);
        await _context.SaveChangesAsync();

        return Ok(new { message = "删除成功" });
    }

    /// <summary>
    /// 批量删除子任务（按父任务ID）
    /// </summary>
    [HttpDelete("by-task/{taskId}")]
    public async Task<IActionResult> DeleteByTaskId(int taskId)
    {
        var subTasks = await _context.SubTasks
            .Where(s => s.ParentTaskId == taskId)
            .ToListAsync();

        _context.SubTasks.RemoveRange(subTasks);
        await _context.SaveChangesAsync();

        return Ok(new { message = $"已删除 {subTasks.Count} 个子任务" });
    }
}

public class CreateSubTaskRequest
{
    public int ParentTaskId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public decimal? EstimatedHours { get; set; }
    public int SortOrder { get; set; }
}

public class UpdateSubTaskRequest
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
    public decimal? EstimatedHours { get; set; }
    public bool? IsCompleted { get; set; }
    public int? SortOrder { get; set; }
}
