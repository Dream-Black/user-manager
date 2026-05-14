using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectHub.Api.Data;
using ProjectHub.Api.Models;

namespace ProjectHub.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FinanceAccountsController : ControllerBase
{
    private readonly AppDbContext _context;

    public FinanceAccountsController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>获取所有账户列表</summary>
    [HttpGet]
    public async Task<IActionResult> GetAccounts()
    {
        var accounts = await _context.FinanceAccounts
            .OrderBy(a => a.SortOrder)
            .ThenBy(a => a.Id)
            .ToListAsync();

        return Ok(new { success = true, data = accounts });
    }

    /// <summary>获取单个账户</summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetAccount(int id)
    {
        var account = await _context.FinanceAccounts.FindAsync(id);
        if (account == null)
            return NotFound(new { success = false, message = "账户不存在" });

        return Ok(new { success = true, data = account });
    }

    /// <summary>创建账户</summary>
    [HttpPost]
    public async Task<IActionResult> CreateAccount([FromBody] CreateFinanceAccountRequest request)
    {
        var account = new FinanceAccount
        {
            Name = request.Name,
            Type = request.Type ?? "cash",
            Icon = request.Icon,
            Color = request.Color ?? "#4A90D9",
            Balance = request.Balance,
            IsDefaultExpense = request.IsDefaultExpense,
            SortOrder = request.SortOrder,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        // 如果设为默认支出账户，先取消其他默认
        if (account.IsDefaultExpense)
        {
            var existingDefaults = await _context.FinanceAccounts
                .Where(a => a.IsDefaultExpense)
                .ToListAsync();
            foreach (var d in existingDefaults)
                d.IsDefaultExpense = false;
        }

        _context.FinanceAccounts.Add(account);
        await _context.SaveChangesAsync();

        return Ok(new { success = true, data = account });
    }

    /// <summary>编辑账户</summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateAccount(int id, [FromBody] UpdateFinanceAccountRequest request)
    {
        var account = await _context.FinanceAccounts.FindAsync(id);
        if (account == null)
            return NotFound(new { success = false, message = "账户不存在" });

        account.Name = request.Name ?? account.Name;
        account.Type = request.Type ?? account.Type;
        account.Icon = request.Icon ?? account.Icon;
        account.Color = request.Color ?? account.Color;
        account.SortOrder = request.SortOrder ?? account.SortOrder;
        account.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        return Ok(new { success = true, data = account });
    }

    /// <summary>删除账户</summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAccount(int id)
    {
        var account = await _context.FinanceAccounts.FindAsync(id);
        if (account == null)
            return NotFound(new { success = false, message = "账户不存在" });

        _context.FinanceAccounts.Remove(account);
        await _context.SaveChangesAsync();

        return Ok(new { success = true, message = "删除成功" });
    }

    /// <summary>设置为默认支出账户</summary>
    [HttpPut("{id}/default")]
    public async Task<IActionResult> SetDefault(int id)
    {
        var account = await _context.FinanceAccounts.FindAsync(id);
        if (account == null)
            return NotFound(new { success = false, message = "账户不存在" });

        // 取消其他默认
        var existingDefaults = await _context.FinanceAccounts
            .Where(a => a.IsDefaultExpense)
            .ToListAsync();
        foreach (var d in existingDefaults)
            d.IsDefaultExpense = false;

        account.IsDefaultExpense = true;
        account.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync();

        return Ok(new { success = true, data = account });
    }
}

public class CreateFinanceAccountRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Type { get; set; }
    public string? Icon { get; set; }
    public string? Color { get; set; }
    public decimal Balance { get; set; }
    public bool IsDefaultExpense { get; set; }
    public int SortOrder { get; set; }
}

public class UpdateFinanceAccountRequest
{
    public string? Name { get; set; }
    public string? Type { get; set; }
    public string? Icon { get; set; }
    public string? Color { get; set; }
    public int? SortOrder { get; set; }
}
