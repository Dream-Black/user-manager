using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectHub.Api.Data;
using ProjectHub.Api.Models;

namespace ProjectHub.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FinanceSnapshotsController : ControllerBase
{
    private readonly AppDbContext _context;

    public FinanceSnapshotsController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>获取快照列表（按账户、日期范围筛选）</summary>
    [HttpGet]
    public async Task<IActionResult> GetSnapshots(
        [FromQuery] int? accountId,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        var query = _context.FinanceAccountSnapshots
            .Include(s => s.Account)
            .AsQueryable();

        if (accountId.HasValue)
            query = query.Where(s => s.AccountId == accountId.Value);

        if (startDate.HasValue)
            query = query.Where(s => s.SnapshotDate >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(s => s.SnapshotDate <= endDate.Value.AddDays(1));

        var snapshots = await query
            .OrderByDescending(s => s.SnapshotDate)
            .ThenBy(s => s.AccountId)
            .Select(s => new
            {
                s.Id,
                s.AccountId,
                AccountName = s.Account != null ? s.Account.Name : null,
                s.SnapshotDate,
                s.Balance,
                s.CreatedAt
            })
            .ToListAsync();

        return Ok(new { success = true, data = snapshots });
    }

    /// <summary>手动触发快照</summary>
    [HttpPost("manual")]
    public async Task<IActionResult> ManualSnapshot([FromBody] ManualSnapshotRequest? request = null)
    {
        var accounts = await _context.FinanceAccounts.ToListAsync();

        if (accounts.Count == 0)
            return Ok(new { success = true, message = "暂无账户，跳过快照" });

        var snapshotDate = request?.SnapshotDate ?? DateTime.Now;
        var createdCount = 0;

        foreach (var account in accounts)
        {
            // 检查当日是否已有快照
            var existing = await _context.FinanceAccountSnapshots
                .FirstOrDefaultAsync(s => s.AccountId == account.Id && s.SnapshotDate.Date == snapshotDate.Date);

            if (existing == null)
            {
                var snapshot = new FinanceAccountSnapshot
                {
                    AccountId = account.Id,
                    SnapshotDate = snapshotDate,
                    Balance = account.Balance,
                    CreatedAt = DateTime.Now
                };
                _context.FinanceAccountSnapshots.Add(snapshot);
                createdCount++;
            }
            else
            {
                // 更新已有快照的余额
                existing.Balance = account.Balance;
            }
        }

        await _context.SaveChangesAsync();

        return Ok(new { success = true, message = $"快照完成，新增 {createdCount} 条记录" });
    }
}

public class ManualSnapshotRequest
{
    public DateTime? SnapshotDate { get; set; }
}
