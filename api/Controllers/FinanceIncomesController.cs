using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectHub.Api.Data;
using ProjectHub.Api.Models;

namespace ProjectHub.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FinanceIncomesController : ControllerBase
{
    private readonly AppDbContext _context;

    public FinanceIncomesController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>获取收入列表（支持筛选）</summary>
    [HttpGet]
    public async Task<IActionResult> GetIncomes(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] string? type,
        [FromQuery] string? keyword,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _context.FinanceIncomes
            .Include(i => i.IncomeAccounts)
            .ThenInclude(ia => ia.Account)
            .Include(i => i.Project)
            .AsQueryable();

        if (startDate.HasValue)
            query = query.Where(i => i.IncomeDate >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(i => i.IncomeDate <= endDate.Value.AddDays(1));

        if (!string.IsNullOrEmpty(type))
            query = query.Where(i => i.Type == type);

        if (!string.IsNullOrEmpty(keyword))
            query = query.Where(i => i.Content.Contains(keyword));

        var total = await query.CountAsync();
        var incomes = await query
            .OrderByDescending(i => i.IncomeDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(i => new
            {
                i.Id,
                i.Type,
                i.Amount,
                i.Content,
                i.Remark,
                i.ProjectId,
                ProjectName = i.Project != null ? i.Project.Name : null,
                i.IncomeDate,
                i.CreatedAt,
                Accounts = i.IncomeAccounts.Select(ia => new
                {
                    ia.Id,
                    ia.AccountId,
                    AccountName = ia.Account != null ? ia.Account.Name : null,
                    ia.Amount
                })
            })
            .ToListAsync();

        return Ok(new { success = true, data = incomes, total });
    }

    /// <summary>获取收入详情（含工资明细和账户分配）</summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetIncome(int id)
    {
        var income = await _context.FinanceIncomes
            .Include(i => i.Project)
            .Include(i => i.SalaryDetail)
            .ThenInclude(sd => sd!.Template)
            .Include(i => i.SalaryDetail)
            .ThenInclude(sd => sd!.DetailItems)
            .ThenInclude(di => di.TemplateItem)
            .Include(i => i.IncomeAccounts)
            .ThenInclude(ia => ia.Account)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (income == null)
            return NotFound(new { success = false, message = "收入记录不存在" });

        return Ok(new { success = true, data = income });
    }

    /// <summary>创建收入（工资/零散），自动加对应账户余额</summary>
    [HttpPost]
    public async Task<IActionResult> CreateIncome([FromBody] CreateFinanceIncomeRequest request)
    {
        var income = new FinanceIncome
        {
            Type = request.Type ?? "misc",
            Amount = request.Amount,
            Content = request.Content,
            Remark = request.Remark,
            ProjectId = request.ProjectId,
            IncomeDate = request.IncomeDate,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        _context.FinanceIncomes.Add(income);
        await _context.SaveChangesAsync();

        // 如果是工资收入，创建工资明细
        if (income.Type == "salary" && request.SalaryDetail != null)
        {
            var salaryDetail = new FinanceSalaryDetail
            {
                IncomeId = income.Id,
                TemplateId = request.SalaryDetail.TemplateId,
                SalaryDate = request.IncomeDate,
                Remark = request.SalaryDetail.Remark,
                ActualItemId = request.SalaryDetail.ActualItemId,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _context.FinanceSalaryDetails.Add(salaryDetail);
            await _context.SaveChangesAsync();

            // 创建工资各子项
            if (request.SalaryDetail.DetailItems != null)
            {
                foreach (var item in request.SalaryDetail.DetailItems)
                {
                    var detailItem = new FinanceSalaryDetailItem
                    {
                        DetailId = salaryDetail.Id,
                        TemplateItemId = item.TemplateItemId,
                        Amount = item.Amount
                    };
                    _context.FinanceSalaryDetailItems.Add(detailItem);
                }
                await _context.SaveChangesAsync();
            }
        }

        // 处理收入分配到账户
        if (request.IncomeAccounts != null && request.IncomeAccounts.Count > 0)
        {
            foreach (var ia in request.IncomeAccounts)
            {
                var account = await _context.FinanceAccounts.FindAsync(ia.AccountId);
                if (account != null)
                {
                    account.Balance += ia.Amount;
                    account.UpdatedAt = DateTime.Now;
                }

                var incomeAccount = new FinanceIncomeAccount
                {
                    IncomeId = income.Id,
                    AccountId = ia.AccountId,
                    Amount = ia.Amount
                };
                _context.FinanceIncomeAccounts.Add(incomeAccount);
            }
            await _context.SaveChangesAsync();
        }

        return Ok(new { success = true, data = income });
    }

    /// <summary>编辑收入</summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateIncome(int id, [FromBody] UpdateFinanceIncomeRequest request)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var income = await _context.FinanceIncomes
                .FirstOrDefaultAsync(i => i.Id == id);

            if (income == null)
                return NotFound(new { success = false, message = "收入记录不存在" });

            // 先恢复旧账户余额（用独立查询避免跟踪问题）
            var oldIAs = await _context.FinanceIncomeAccounts
                .Where(ia => ia.IncomeId == id)
                .ToListAsync();
            foreach (var oldIA in oldIAs)
            {
                var account = await _context.FinanceAccounts.FindAsync(oldIA.AccountId);
                if (account != null)
                {
                    account.Balance -= oldIA.Amount;
                    account.UpdatedAt = DateTime.Now;
                }
            }
            _context.FinanceIncomeAccounts.RemoveRange(oldIAs);

            income.Type = request.Type ?? income.Type;
            income.Amount = request.Amount ?? income.Amount;
            income.Content = request.Content ?? income.Content;
            income.Remark = request.Remark ?? income.Remark;
            income.ProjectId = request.ProjectId ?? income.ProjectId;
            income.IncomeDate = request.IncomeDate ?? income.IncomeDate;
            income.UpdatedAt = DateTime.Now;

            // 重新分配到账户
            if (request.IncomeAccounts != null && request.IncomeAccounts.Count > 0)
            {
                foreach (var ia in request.IncomeAccounts)
                {
                    var account = await _context.FinanceAccounts.FindAsync(ia.AccountId);
                    if (account != null)
                    {
                        account.Balance += ia.Amount;
                        account.UpdatedAt = DateTime.Now;
                    }

                    _context.FinanceIncomeAccounts.Add(new FinanceIncomeAccount
                    {
                        IncomeId = income.Id,
                        AccountId = ia.AccountId,
                        Amount = ia.Amount
                    });
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new { success = true, data = income });
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>删除收入（恢复账户余额）</summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteIncome(int id)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var income = await _context.FinanceIncomes
                .Include(i => i.IncomeAccounts)
                .Include(i => i.SalaryDetail)
                .ThenInclude(sd => sd!.DetailItems)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (income == null)
                return NotFound(new { success = false, message = "收入记录不存在" });

            // 恢复所有账户余额（删除收入时加回余额）
            foreach (var ia in income.IncomeAccounts)
            {
                var account = await _context.FinanceAccounts.FindAsync(ia.AccountId);
                if (account != null)
                {
                    account.Balance += ia.Amount;
                    account.UpdatedAt = DateTime.Now;
                }
            }

            _context.FinanceIncomes.Remove(income);
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

    /// <summary>获取统计（按时间范围）</summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        var start = startDate ?? DateTime.Now.AddMonths(-1);
        var end = endDate ?? DateTime.Now;

        // 总支出
        var totalExpense = await _context.FinanceExpenses
            .Where(e => e.ExpenseDate >= start && e.ExpenseDate <= end.AddDays(1))
            .SumAsync(e => e.Amount);

        // 总收入
        var totalIncome = await _context.FinanceIncomes
            .Where(i => i.IncomeDate >= start && i.IncomeDate <= end.AddDays(1))
            .SumAsync(i => i.Amount);

        // 各分类支出占比
        var categoryStats = await _context.FinanceExpenses
            .Where(e => e.ExpenseDate >= start && e.ExpenseDate <= end.AddDays(1) && e.CategoryId != null)
            .GroupBy(e => e.CategoryId)
            .Select(g => new
            {
                CategoryId = g.Key,
                Total = g.Sum(e => e.Amount)
            })
            .ToListAsync();

        var categoryDetails = new List<object>();
        foreach (var stat in categoryStats)
        {
            var cat = await _context.FinanceExpenseCategories.FindAsync(stat.CategoryId);
            categoryDetails.Add(new
            {
                CategoryId = stat.CategoryId,
                CategoryName = cat != null ? cat.Name : "未分类",
                CategoryIcon = cat != null ? cat.Icon : null,
                CategoryColor = cat != null ? cat.Color : null,
                Total = stat.Total,
                Percentage = totalExpense > 0 ? Math.Round(stat.Total / totalExpense * 100, 1) : 0
            });
        }

        return Ok(new
        {
            success = true,
            data = new
            {
                startDate = start,
                endDate = end,
                totalExpense,
                totalIncome,
                netIncome = totalIncome - totalExpense,
                categoryStats = categoryDetails
            }
        });
    }
}

public class CreateFinanceIncomeRequest
{
    public string? Type { get; set; }
    public decimal Amount { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? Remark { get; set; }
    public int? ProjectId { get; set; }
    public DateTime IncomeDate { get; set; } = DateTime.Now;
    public SalaryDetailRequest? SalaryDetail { get; set; }
    public List<IncomeAccountRequest>? IncomeAccounts { get; set; }
}

public class UpdateFinanceIncomeRequest
{
    public string? Type { get; set; }
    public decimal? Amount { get; set; }
    public string? Content { get; set; }
    public string? Remark { get; set; }
    public int? ProjectId { get; set; }
    public DateTime? IncomeDate { get; set; }
    public List<IncomeAccountRequest>? IncomeAccounts { get; set; }
}

public class SalaryDetailRequest
{
    public int TemplateId { get; set; }
    public string? Remark { get; set; }
    public int? ActualItemId { get; set; }
    public List<SalaryDetailItemRequest>? DetailItems { get; set; }
}

public class SalaryDetailItemRequest
{
    public int TemplateItemId { get; set; }
    public decimal Amount { get; set; }
}

public class IncomeAccountRequest
{
    public int AccountId { get; set; }
    public decimal Amount { get; set; }
}
