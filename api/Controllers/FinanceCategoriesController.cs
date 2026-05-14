using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectHub.Api.Data;
using ProjectHub.Api.Models;

namespace ProjectHub.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FinanceCategoriesController : ControllerBase
{
    private readonly AppDbContext _context;

    public FinanceCategoriesController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>获取所有分类列表</summary>
    [HttpGet]
    public async Task<IActionResult> GetCategories()
    {
        var categories = await _context.FinanceExpenseCategories
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Id)
            .ToListAsync();

        return Ok(new { success = true, data = categories });
    }

    /// <summary>创建自定义分类</summary>
    [HttpPost]
    public async Task<IActionResult> CreateCategory([FromBody] CreateFinanceCategoryRequest request)
    {
        var category = new FinanceExpenseCategory
        {
            Name = request.Name,
            Icon = request.Icon,
            Color = request.Color ?? "#4A90D9",
            IsSystem = false,
            SortOrder = request.SortOrder,
            CreatedAt = DateTime.Now
        };

        _context.FinanceExpenseCategories.Add(category);
        await _context.SaveChangesAsync();

        return Ok(new { success = true, data = category });
    }

    /// <summary>编辑分类</summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCategory(int id, [FromBody] UpdateFinanceCategoryRequest request)
    {
        var category = await _context.FinanceExpenseCategories.FindAsync(id);
        if (category == null)
            return NotFound(new { success = false, message = "分类不存在" });

        category.Name = request.Name ?? category.Name;
        category.Icon = request.Icon ?? category.Icon;
        category.Color = request.Color ?? category.Color;
        category.SortOrder = request.SortOrder ?? category.SortOrder;

        await _context.SaveChangesAsync();

        return Ok(new { success = true, data = category });
    }

    /// <summary>删除自定义分类（系统分类不可删除）</summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var category = await _context.FinanceExpenseCategories.FindAsync(id);
        if (category == null)
            return NotFound(new { success = false, message = "分类不存在" });

        if (category.IsSystem)
            return BadRequest(new { success = false, message = "系统预设分类不可删除" });

        _context.FinanceExpenseCategories.Remove(category);
        await _context.SaveChangesAsync();

        return Ok(new { success = true, message = "删除成功" });
    }
}

public class CreateFinanceCategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string? Color { get; set; }
    public int SortOrder { get; set; }
}

public class UpdateFinanceCategoryRequest
{
    public string? Name { get; set; }
    public string? Icon { get; set; }
    public string? Color { get; set; }
    public int? SortOrder { get; set; }
}
