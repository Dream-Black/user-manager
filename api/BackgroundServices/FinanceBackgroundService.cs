using Microsoft.EntityFrameworkCore;
using ProjectHub.Api.Data;
using ProjectHub.Api.Models;
using ProjectHub.Api.Services;

namespace ProjectHub.Api.BackgroundServices;

/// <summary>财务定时任务 - AI分类 + 账户余额快照</summary>
public class FinanceBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<FinanceBackgroundService> _logger;
    private int _lastClassifyDay = -1;
    private int _lastSnapshotDay = -1;

    public FinanceBackgroundService(IServiceProvider serviceProvider, ILogger<FinanceBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[FinanceBG] 财务定时任务已启动");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.Now;
                var today = now.DayOfYear;

                // 23:59 → 执行AI分类（每天只执行一次）
                if (now.Hour == 23 && now.Minute >= 59 && _lastClassifyDay != today)
                {
                    _lastClassifyDay = today;
                    _logger.LogInformation("[FinanceBG] 23:59 - 开始执行AI自动分类");
                    await ClassifyUncategorizedExpenses();
                }

                // 00:00 → 执行余额快照（每天只执行一次）
                if (now.Hour == 0 && _lastSnapshotDay != today)
                {
                    _lastSnapshotDay = today;
                    _logger.LogInformation("[FinanceBG] 00:00 - 开始执行账户余额快照");
                    await CreateDailySnapshots();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[FinanceBG] 定时任务执行异常");
            }

            await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
        }

        _logger.LogInformation("[FinanceBG] 财务定时任务已停止");
    }

    /// <summary>查询未分类支出，调用AI分类，更新分类</summary>
    private async Task ClassifyUncategorizedExpenses()
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var financeAIService = scope.ServiceProvider.GetRequiredService<FinanceAIService>();

        var uncategorized = await dbContext.FinanceExpenses
            .Where(e => e.CategoryId == null)
            .ToListAsync();

        if (uncategorized.Count == 0)
        {
            _logger.LogInformation("[FinanceBG] 没有未分类的支出");
            return;
        }

        _logger.LogInformation("[FinanceBG] 发现 {Count} 条未分类支出", uncategorized.Count);

        foreach (var expense in uncategorized)
        {
            try
            {
                // 构建清单文本
                var itemsText = string.Empty;
                if (expense.Type == "list")
                {
                    var items = await dbContext.FinanceExpenseItems
                        .Where(i => i.ExpenseId == expense.Id)
                        .ToListAsync();
                    if (items.Count > 0)
                        itemsText = string.Join("、", items.Select(i => $"{i.Name}×{i.Quantity}"));
                }

                var categoryName = await financeAIService.ClassifyExpenseAsync(expense.Purpose, itemsText);
                if (string.IsNullOrEmpty(categoryName))
                    continue;

                // 查找对应分类
                var category = await dbContext.FinanceExpenseCategories
                    .FirstOrDefaultAsync(c => c.Name == categoryName);

                // 分类不存在则创建
                if (category == null)
                {
                    var maxSort = await dbContext.FinanceExpenseCategories
                        .Where(c => !c.IsSystem)
                        .MaxAsync(c => (int?)c.SortOrder) ?? 0;

                    category = new FinanceExpenseCategory
                    {
                        Name = categoryName,
                        Icon = "circle",
                        Color = "#999999",
                        IsSystem = false,
                        SortOrder = maxSort + 1,
                        CreatedAt = DateTime.Now
                    };
                    dbContext.FinanceExpenseCategories.Add(category);
                    await dbContext.SaveChangesAsync();
                    _logger.LogInformation("[FinanceBG] 新建分类: {Name} (Id={Id})", category.Name, category.Id);
                }

                expense.CategoryId = category.Id;
                expense.UpdatedAt = DateTime.Now;
                await dbContext.SaveChangesAsync();

                _logger.LogInformation("[FinanceBG] 支出 #{Id} 已分类为: {Category}", expense.Id, category.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[FinanceBG] 分类支出 #{Id} 失败", expense.Id);
            }
        }
    }

    /// <summary>查询所有账户，插入每日余额快照</summary>
    private async Task CreateDailySnapshots()
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var accounts = await dbContext.FinanceAccounts.ToListAsync();
        if (accounts.Count == 0)
        {
            _logger.LogInformation("[FinanceBG] 没有账户，跳过快照");
            return;
        }

        var today = DateTime.Now.Date;
        var createdCount = 0;

        foreach (var account in accounts)
        {
            // 检查当日是否已有快照
            var existing = await dbContext.FinanceAccountSnapshots
                .FirstOrDefaultAsync(s => s.AccountId == account.Id && s.SnapshotDate.Date == today);

            if (existing == null)
            {
                dbContext.FinanceAccountSnapshots.Add(new FinanceAccountSnapshot
                {
                    AccountId = account.Id,
                    SnapshotDate = today,
                    Balance = account.Balance,
                    CreatedAt = DateTime.Now
                });
                createdCount++;
            }
        }

        if (createdCount > 0)
        {
            await dbContext.SaveChangesAsync();
            _logger.LogInformation("[FinanceBG] 已创建 {Count} 条账户快照", createdCount);
        }
        else
        {
            _logger.LogInformation("[FinanceBG] 今日快照已存在，跳过");
        }
    }
}
