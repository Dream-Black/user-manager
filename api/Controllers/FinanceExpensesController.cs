using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectHub.Api.Data;
using ProjectHub.Api.Models;

namespace ProjectHub.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FinanceExpensesController : ControllerBase
{
    private readonly AppDbContext _context;

    public FinanceExpensesController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>获取支出列表（支持筛选）</summary>
    [HttpGet]
    public async Task<IActionResult> GetExpenses(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int? categoryId,
        [FromQuery] string? keyword,
        [FromQuery] string? type,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _context.FinanceExpenses
            .Include(e => e.Category)
            .Include(e => e.Account)
            .AsQueryable();

        if (startDate.HasValue)
            query = query.Where(e => e.ExpenseDate >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(e => e.ExpenseDate <= endDate.Value.AddDays(1));

        if (categoryId.HasValue)
            query = query.Where(e => e.CategoryId == categoryId.Value);

        if (!string.IsNullOrEmpty(keyword))
            query = query.Where(e => e.Purpose.Contains(keyword));

        if (!string.IsNullOrEmpty(type))
            query = query.Where(e => e.Type == type);

        var total = await query.CountAsync();
        var expenses = await query
            .OrderByDescending(e => e.ExpenseDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new
            {
                e.Id,
                e.Type,
                e.Amount,
                e.Purpose,
                CategoryId = e.CategoryId,
                CategoryName = e.Category != null ? e.Category.Name : null,
                CategoryIcon = e.Category != null ? e.Category.Icon : null,
                CategoryColor = e.Category != null ? e.Category.Color : null,
                e.AccountId,
                AccountName = e.Account != null ? e.Account.Name : null,
                e.ExpenseDate,
                e.Remark,
                e.CreatedAt
            })
            .ToListAsync();

        return Ok(new { success = true, data = expenses, total });
    }

    /// <summary>获取支出详情（含清单子项）</summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetExpense(int id)
    {
        var expense = await _context.FinanceExpenses
            .Include(e => e.Category)
            .Include(e => e.Account)
            .Include(e => e.Items.OrderBy(i => i.SortOrder))
            .FirstOrDefaultAsync(e => e.Id == id);

        if (expense == null)
            return NotFound(new { success = false, message = "支出记录不存在" });

        return Ok(new { success = true, data = expense });
    }

    /// <summary>创建支出（自动扣默认账户余额）</summary>
    [HttpPost]
    public async Task<IActionResult> CreateExpense([FromBody] CreateFinanceExpenseRequest request)
    {
        // 获取账户，如果未指定则使用默认支出账户
        FinanceAccount? account;
        if (request.AccountId.HasValue)
        {
            account = await _context.FinanceAccounts.FindAsync(request.AccountId.Value);
        }
        else
        {
            account = await _context.FinanceAccounts.FirstOrDefaultAsync(a => a.IsDefaultExpense);
        }

        if (account == null)
            return BadRequest(new { success = false, message = "请先创建账户或设置默认支出账户" });

        var expense = new FinanceExpense
        {
            Type = request.Type ?? "simple",
            Amount = request.Amount,
            Purpose = request.Purpose,
            CategoryId = request.CategoryId,
            AccountId = account.Id,
            ExpenseDate = request.ExpenseDate,
            Remark = request.Remark,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        // 清单子项
        if (request.Items != null && request.Items.Count > 0)
        {
            expense.Items = request.Items.Select((item, index) => new FinanceExpenseItem
            {
                Name = item.Name,
                Quantity = item.Quantity,
                Unit = item.Unit,
                UnitPrice = item.UnitPrice,
                Subtotal = item.Quantity * item.UnitPrice,
                SortOrder = index
            }).ToList();

            // 重新计算总额
            expense.Amount = expense.Items.Sum(i => i.Subtotal);
        }

        // 扣减账户余额（使用事务保证一致性）
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            account.Balance -= expense.Amount;
            account.UpdatedAt = DateTime.Now;

            _context.FinanceExpenses.Add(expense);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new { success = true, data = expense });
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>编辑支出（更新余额差异）</summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateExpense(int id, [FromBody] UpdateFinanceExpenseRequest request)
    {
        var expense = await _context.FinanceExpenses
            .Include(e => e.Items)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (expense == null)
            return NotFound(new { success = false, message = "支出记录不存在" });

        var oldAmount = expense.Amount;

        expense.Type = request.Type ?? expense.Type;
        expense.Purpose = request.Purpose ?? expense.Purpose;
        expense.CategoryId = request.CategoryId ?? expense.CategoryId;
        expense.ExpenseDate = request.ExpenseDate ?? expense.ExpenseDate;
        expense.Remark = request.Remark ?? expense.Remark;
        expense.UpdatedAt = DateTime.Now;

        // 更新清单子项
        if (request.Items != null)
        {
            _context.FinanceExpenseItems.RemoveRange(expense.Items);
            expense.Items = request.Items.Select((item, index) => new FinanceExpenseItem
            {
                Name = item.Name,
                Quantity = item.Quantity,
                Unit = item.Unit,
                UnitPrice = item.UnitPrice,
                Subtotal = item.Quantity * item.UnitPrice,
                SortOrder = index
            }).ToList();
            expense.Amount = expense.Items.Sum(i => i.Subtotal);
        }
        else
        {
            expense.Amount = request.Amount ?? expense.Amount;
        }

        // 更新账户余额差异（使用事务保证一致性）
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var diff = expense.Amount - oldAmount;
            var account = await _context.FinanceAccounts.FindAsync(expense.AccountId);
            if (account != null)
            {
                account.Balance -= diff;
                account.UpdatedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new { success = true, data = expense });
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>删除支出（恢复账户余额）</summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteExpense(int id)
    {
        var expense = await _context.FinanceExpenses
            .Include(e => e.Items)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (expense == null)
            return NotFound(new { success = false, message = "支出记录不存在" });

        // 恢复账户余额（使用事务保证一致性）
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var account = await _context.FinanceAccounts.FindAsync(expense.AccountId);
            if (account != null)
            {
                account.Balance += expense.Amount;
                account.UpdatedAt = DateTime.Now;
            }

            _context.FinanceExpenses.Remove(expense);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new { success = true, message = "删除成功" });
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}

public class CreateFinanceExpenseRequest
{
    public string? Type { get; set; }
    public decimal Amount { get; set; }
    public string Purpose { get; set; } = string.Empty;
    public int? CategoryId { get; set; }
    public int? AccountId { get; set; }
    public DateTime ExpenseDate { get; set; } = DateTime.Now;
    public string? Remark { get; set; }
    public List<ExpenseItemRequest>? Items { get; set; }
}

public class UpdateFinanceExpenseRequest
{
    public string? Type { get; set; }
    public decimal? Amount { get; set; }
    public string? Purpose { get; set; }
    public int? CategoryId { get; set; }
    public DateTime? ExpenseDate { get; set; }
    public string? Remark { get; set; }
    public List<ExpenseItemRequest>? Items { get; set; }
}

public class ExpenseItemRequest
{
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string? Unit { get; set; }
    public decimal UnitPrice { get; set; }
}
