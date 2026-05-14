using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectHub.Api.Data;
using ProjectHub.Api.Models;

namespace ProjectHub.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FinanceSalaryTemplatesController : ControllerBase
{
    private readonly AppDbContext _context;

    public FinanceSalaryTemplatesController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>获取所有模板列表</summary>
    [HttpGet]
    public async Task<IActionResult> GetTemplates()
    {
        var templates = await _context.FinanceSalaryTemplates
            .Include(t => t.TemplateItems.OrderBy(ti => ti.SortOrder))
            .OrderByDescending(t => t.IsActive)
            .ThenByDescending(t => t.UpdatedAt)
            .ToListAsync();

        return Ok(new { success = true, data = templates });
    }

    /// <summary>获取模板详情（含子项）</summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetTemplate(int id)
    {
        var template = await _context.FinanceSalaryTemplates
            .Include(t => t.TemplateItems.OrderBy(ti => ti.SortOrder))
            .FirstOrDefaultAsync(t => t.Id == id);

        if (template == null)
            return NotFound(new { success = false, message = "模板不存在" });

        return Ok(new { success = true, data = template });
    }

    /// <summary>创建模板（含子项）</summary>
    [HttpPost]
    public async Task<IActionResult> CreateTemplate([FromBody] CreateFinanceSalaryTemplateRequest request)
    {
        var template = new FinanceSalaryTemplate
        {
            Title = request.Title,
            Remark = request.Remark,
            IsActive = request.IsActive,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        if (request.TemplateItems != null)
        {
            template.TemplateItems = request.TemplateItems.Select((item, index) => new FinanceSalaryTemplateItem
            {
                Name = item.Name,
                SortOrder = index
            }).ToList();
        }

        _context.FinanceSalaryTemplates.Add(template);
        await _context.SaveChangesAsync();

        return Ok(new { success = true, data = template });
    }

    /// <summary>编辑模板（含子项）</summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTemplate(int id, [FromBody] CreateFinanceSalaryTemplateRequest request)
    {
        var template = await _context.FinanceSalaryTemplates
            .Include(t => t.TemplateItems)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (template == null)
            return NotFound(new { success = false, message = "模板不存在" });

        template.Title = request.Title ?? template.Title;
        template.Remark = request.Remark ?? template.Remark;
        template.IsActive = request.IsActive;
        template.UpdatedAt = DateTime.Now;

        // 更新子项：先删除旧的，再添加新的
        if (request.TemplateItems != null)
        {
            _context.FinanceSalaryTemplateItems.RemoveRange(template.TemplateItems);
            template.TemplateItems = request.TemplateItems.Select((item, index) => new FinanceSalaryTemplateItem
            {
                TemplateId = id,
                Name = item.Name,
                SortOrder = index
            }).ToList();
        }

        await _context.SaveChangesAsync();

        return Ok(new { success = true, data = template });
    }

    /// <summary>删除模板</summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTemplate(int id)
    {
        var template = await _context.FinanceSalaryTemplates
            .Include(t => t.TemplateItems)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (template == null)
            return NotFound(new { success = false, message = "模板不存在" });

        _context.FinanceSalaryTemplates.Remove(template);
        await _context.SaveChangesAsync();

        return Ok(new { success = true, message = "删除成功" });
    }
}

public class CreateFinanceSalaryTemplateRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Remark { get; set; }
    public bool IsActive { get; set; } = true;
    public List<TemplateItemRequest>? TemplateItems { get; set; }
}

public class TemplateItemRequest
{
    public string Name { get; set; } = string.Empty;
}
