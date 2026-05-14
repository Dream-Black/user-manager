using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectHub.Api.Data;
using ProjectHub.Api.Models;

namespace ProjectHub.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FinanceTransfersController : ControllerBase
{
    private readonly AppDbContext _context;

    public FinanceTransfersController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>获取转账记录列表</summary>
    [HttpGet]
    public async Task<IActionResult> GetTransfers()
    {
        var transfers = await _context.FinanceAccountTransfers
            .Include(t => t.FromAccount)
            .Include(t => t.ToAccount)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new
            {
                t.Id,
                FromAccountId = t.FromAccountId,
                FromAccountName = t.FromAccount != null ? t.FromAccount.Name : null,
                ToAccountId = t.ToAccountId,
                ToAccountName = t.ToAccount != null ? t.ToAccount.Name : null,
                t.Amount,
                t.Remark,
                t.CreatedAt
            })
            .ToListAsync();

        return Ok(new { success = true, data = transfers });
    }

    /// <summary>创建转账（检查余额，更新两账户余额）</summary>
    [HttpPost]
    public async Task<IActionResult> CreateTransfer([FromBody] CreateFinanceTransferRequest request)
    {
        if (request.FromAccountId == request.ToAccountId)
            return BadRequest(new { success = false, message = "转出账户和转入账户不能相同" });

        var fromAccount = await _context.FinanceAccounts.FindAsync(request.FromAccountId);
        if (fromAccount == null)
            return NotFound(new { success = false, message = "转出账户不存在" });

        var toAccount = await _context.FinanceAccounts.FindAsync(request.ToAccountId);
        if (toAccount == null)
            return NotFound(new { success = false, message = "转入账户不存在" });

        if (fromAccount.Balance < request.Amount)
            return BadRequest(new { success = false, message = "转出账户余额不足" });

        // 更新两账户余额（使用事务保证一致性）
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            fromAccount.Balance -= request.Amount;
            fromAccount.UpdatedAt = DateTime.Now;
            toAccount.Balance += request.Amount;
            toAccount.UpdatedAt = DateTime.Now;

            var transfer = new FinanceAccountTransfer
            {
                FromAccountId = request.FromAccountId,
                ToAccountId = request.ToAccountId,
                Amount = request.Amount,
                Remark = request.Remark,
                CreatedAt = DateTime.Now
            };

            _context.FinanceAccountTransfers.Add(transfer);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new { success = true, data = transfer });
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>获取账户余额变化曲线数据（按日期范围）</summary>
    [HttpGet("stats/balance-trend")]
    public async Task<IActionResult> GetBalanceTrend(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int? accountId)
    {
        var start = startDate ?? DateTime.Now.AddMonths(-1);
        var end = endDate ?? DateTime.Now;

        var query = _context.FinanceAccountSnapshots
            .Where(s => s.SnapshotDate >= start && s.SnapshotDate <= end.AddDays(1));

        if (accountId.HasValue)
            query = query.Where(s => s.AccountId == accountId.Value);

        var snapshots = await query
            .OrderBy(s => s.SnapshotDate)
            .ThenBy(s => s.AccountId)
            .Select(s => new
            {
                s.AccountId,
                s.SnapshotDate,
                s.Balance
            })
            .ToListAsync();

        return Ok(new { success = true, data = snapshots });
    }

    /// <summary>获取月存款曲线数据</summary>
    [HttpGet("stats/monthly-savings")]
    public async Task<IActionResult> GetMonthlySavings(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int? accountId)
    {
        var start = startDate ?? new DateTime(DateTime.Now.Year, 1, 1);
        var end = endDate ?? new DateTime(DateTime.Now.Year, 12, 31);

        var expenseQuery = _context.FinanceExpenses
            .Where(e => e.ExpenseDate >= start && e.ExpenseDate <= end.AddDays(1));
        var incomeQuery = _context.FinanceIncomes
            .Where(i => i.IncomeDate >= start && i.IncomeDate <= end.AddDays(1));

        if (accountId.HasValue)
        {
            expenseQuery = expenseQuery.Where(e => e.AccountId == accountId.Value);
            incomeQuery = incomeQuery.Where(i => i.IncomeAccounts.Any(ia => ia.AccountId == accountId.Value));
        }

        // 按月统计
        var monthlyExpense = await expenseQuery
            .GroupBy(e => new { e.ExpenseDate.Year, e.ExpenseDate.Month })
            .Select(g => new { Year = g.Key.Year, Month = g.Key.Month, Total = g.Sum(e => e.Amount) })
            .ToListAsync();

        var monthlyIncome = await incomeQuery
            .GroupBy(i => new { i.IncomeDate.Year, i.IncomeDate.Month })
            .Select(g => new { Year = g.Key.Year, Month = g.Key.Month, Total = g.Sum(i => i.Amount) })
            .ToListAsync();

        var monthsInRange = Enumerable.Range(0, 12)
            .Select(i => start.AddMonths(i))
            .Where(d => d >= start && d <= end)
            .Select(d => new { d.Year, d.Month })
            .Distinct()
            .OrderBy(x => x.Year)
            .ThenBy(x => x.Month)
            .ToList();

        var result = monthsInRange.Select(m => new
        {
            m.Year,
            m.Month,
            Expense = monthlyExpense.FirstOrDefault(e => e.Year == m.Year && e.Month == m.Month)?.Total ?? 0,
            Income = monthlyIncome.FirstOrDefault(i => i.Year == m.Year && i.Month == m.Month)?.Total ?? 0,
            Savings = (monthlyIncome.FirstOrDefault(i => i.Year == m.Year && i.Month == m.Month)?.Total ?? 0)
                     - (monthlyExpense.FirstOrDefault(e => e.Year == m.Year && e.Month == m.Month)?.Total ?? 0)
        }).ToList();

        return Ok(new { success = true, data = result });
    }
}

public class CreateFinanceTransferRequest
{
    public int FromAccountId { get; set; }
    public int ToAccountId { get; set; }
    public decimal Amount { get; set; }
    public string? Remark { get; set; }
}
