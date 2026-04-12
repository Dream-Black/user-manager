using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectHub.Api.Data;
using ProjectHub.Api.Models;

namespace ProjectHub.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReviewsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ReviewsController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>获取所有复盘</summary>
    [HttpGet]
    public async Task<IActionResult> GetReviews([FromQuery] int? projectId = null, [FromQuery] string? search = null)
    {
        var query = _context.Reviews
            .Include(r => r.Project)
            .AsQueryable();

        if (projectId.HasValue)
            query = query.Where(r => r.ProjectId == projectId.Value);

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(r =>
                (r.GoodPoints != null && r.GoodPoints.Contains(search)) ||
                (r.Improvements != null && r.Improvements.Contains(search)) ||
                (r.NextActions != null && r.NextActions.Contains(search)));
        }

        var reviews = await query
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return Ok(reviews);
    }

    /// <summary>获取单个复盘</summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetReview(int id)
    {
        var review = await _context.Reviews
            .Include(r => r.Project)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (review == null)
            return NotFound(new { message = "复盘不存在" });

        return Ok(review);
    }

    /// <summary>创建复盘</summary>
    [HttpPost]
    public async Task<IActionResult> CreateReview([FromBody] CreateReviewRequest request)
    {
        var project = await _context.Projects.FindAsync(request.ProjectId);
        if (project == null)
            return NotFound(new { message = "项目不存在" });

        var review = new Review
        {
            ProjectId = request.ProjectId,
            TaskId = request.TaskId,
            GoodPoints = request.GoodPoints,
            Improvements = request.Improvements,
            NextActions = request.NextActions,
            CreatedAt = DateTime.Now
        };

        _context.Reviews.Add(review);
        await _context.SaveChangesAsync();

        // 添加时间线
        _context.Timelines.Add(new Timeline
        {
            ProjectId = request.ProjectId,
            EventType = "reviewed",
            Title = "添加复盘",
            Description = $"添加了项目复盘",
            TaskId = request.TaskId,
            OccurredAt = DateTime.Now
        });
        await _context.SaveChangesAsync();

        return Created($"/api/reviews/{review.Id}", review);
    }

    /// <summary>更新复盘</summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateReview(int id, [FromBody] UpdateReviewRequest request)
    {
        var review = await _context.Reviews.FindAsync(id);
        if (review == null)
            return NotFound(new { message = "复盘不存在" });

        if (request.GoodPoints != null) review.GoodPoints = request.GoodPoints;
        if (request.Improvements != null) review.Improvements = request.Improvements;
        if (request.NextActions != null) review.NextActions = request.NextActions;

        await _context.SaveChangesAsync();
        return Ok(review);
    }

    /// <summary>删除复盘</summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteReview(int id)
    {
        var review = await _context.Reviews.FindAsync(id);
        if (review == null)
            return NotFound(new { message = "复盘不存在" });

        _context.Reviews.Remove(review);
        await _context.SaveChangesAsync();

        return Ok(new { message = "删除成功" });
    }
}

public class CreateReviewRequest
{
    public int ProjectId { get; set; }
    public int? TaskId { get; set; }
    public string? GoodPoints { get; set; }
    public string? Improvements { get; set; }
    public string? NextActions { get; set; }
}

public class UpdateReviewRequest
{
    public string? GoodPoints { get; set; }
    public string? Improvements { get; set; }
    public string? NextActions { get; set; }
}
