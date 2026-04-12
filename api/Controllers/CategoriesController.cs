using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectHub.Api.Data;
using ProjectHub.Api.Models;

namespace ProjectHub.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly AppDbContext _context;

    public CategoriesController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>获取所有分类</summary>
    [HttpGet]
    public async Task<IActionResult> GetCategories()
    {
        var categories = await _context.TaskCategories
            .OrderBy(c => c.SortOrder)
            .ToListAsync();

        return Ok(categories);
    }

    /// <summary>创建分类</summary>
    [HttpPost]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryRequest request)
    {
        var maxSort = await _context.TaskCategories.MaxAsync(c => (int?)c.SortOrder) ?? 0;

        var category = new TaskCategory
        {
            Name = request.Name,
            Icon = request.Icon,
            Color = request.Color,
            IsSystem = false,
            SortOrder = maxSort + 1,
            CreatedAt = DateTime.Now
        };

        _context.TaskCategories.Add(category);
        await _context.SaveChangesAsync();

        return Created($"/api/categories/{category.Id}", category);
    }

    /// <summary>更新分类</summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCategory(int id, [FromBody] UpdateCategoryRequest request)
    {
        var category = await _context.TaskCategories.FindAsync(id);
        if (category == null)
            return NotFound(new { message = "分类不存在" });

        if (category.IsSystem)
            return BadRequest(new { message = "系统分类不能修改" });

        if (request.Name != null) category.Name = request.Name;
        if (request.Icon != null) category.Icon = request.Icon;
        if (request.Color != null) category.Color = request.Color;

        await _context.SaveChangesAsync();
        return Ok(category);
    }

    /// <summary>删除分类</summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var category = await _context.TaskCategories.FindAsync(id);
        if (category == null)
            return NotFound(new { message = "分类不存在" });

        if (category.IsSystem)
            return BadRequest(new { message = "系统分类不能删除" });

        // 检查是否有任务使用此分类
        var hasTasks = await _context.Tasks.AnyAsync(t => t.Category == category.Name);
        if (hasTasks)
            return BadRequest(new { message = "有任务正在使用此分类，无法删除" });

        _context.TaskCategories.Remove(category);
        await _context.SaveChangesAsync();

        return Ok(new { message = "删除成功" });
    }
}

public class CreateCategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string Color { get; set; } = "#4A90D9";
}

public class UpdateCategoryRequest
{
    public string? Name { get; set; }
    public string? Icon { get; set; }
    public string? Color { get; set; }
}
