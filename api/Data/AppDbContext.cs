using Microsoft.EntityFrameworkCore;
using ProjectHub.Api.Models;

namespace ProjectHub.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectTask> Tasks => Set<ProjectTask>();
    public DbSet<Timeline> Timelines => Set<Timeline>();
    public DbSet<TaskTimeline> TaskTimelines => Set<TaskTimeline>();
    public DbSet<TaskDelay> TaskDelays => Set<TaskDelay>();
    public DbSet<TaskExtraRequirement> TaskExtraRequirements => Set<TaskExtraRequirement>();
    public DbSet<TaskCategory> TaskCategories => Set<TaskCategory>();
    public DbSet<UserSettings> UserSettings => Set<UserSettings>();
    
    // 用户模块
    public DbSet<User> Users => Set<User>();

    // 资源管理模块
    public DbSet<Computer> Computers => Set<Computer>();
    public DbSet<ResourcePath> ResourcePaths => Set<ResourcePath>();
    public DbSet<Comic> Comics => Set<Comic>();
    public DbSet<ComicChapter> ComicChapters => Set<ComicChapter>();

    // 子任务模块
    public DbSet<SubTask> SubTasks => Set<SubTask>();

    // AI 助手模块
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();

    // 笔记模块
    public DbSet<Note> Notes => Set<Note>();
    public DbSet<NoteTag> NoteTags => Set<NoteTag>();

    // 日程模块
    public DbSet<Schedule> Schedules => Set<Schedule>();
    public DbSet<ScheduleDay> ScheduleDays => Set<ScheduleDay>();
    public DbSet<ScheduleReminder> ScheduleReminders => Set<ScheduleReminder>();

    // 财务模块
    public DbSet<FinanceAccount> FinanceAccounts => Set<FinanceAccount>();
    public DbSet<FinanceExpense> FinanceExpenses => Set<FinanceExpense>();
    public DbSet<FinanceExpenseItem> FinanceExpenseItems => Set<FinanceExpenseItem>();
    public DbSet<FinanceExpenseCategory> FinanceExpenseCategories => Set<FinanceExpenseCategory>();
    public DbSet<FinanceIncome> FinanceIncomes => Set<FinanceIncome>();
    public DbSet<FinanceSalaryTemplate> FinanceSalaryTemplates => Set<FinanceSalaryTemplate>();
    public DbSet<FinanceSalaryTemplateItem> FinanceSalaryTemplateItems => Set<FinanceSalaryTemplateItem>();
    public DbSet<FinanceSalaryDetail> FinanceSalaryDetails => Set<FinanceSalaryDetail>();
    public DbSet<FinanceSalaryDetailItem> FinanceSalaryDetailItems => Set<FinanceSalaryDetailItem>();
    public DbSet<FinanceIncomeAccount> FinanceIncomeAccounts => Set<FinanceIncomeAccount>();
    public DbSet<FinanceAccountTransfer> FinanceAccountTransfers => Set<FinanceAccountTransfer>();
    public DbSet<FinanceAccountSnapshot> FinanceAccountSnapshots => Set<FinanceAccountSnapshot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Project 配置
        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.CreatedAt);
        });

        // ProjectTask 配置
        modelBuilder.Entity<ProjectTask>(entity =>
        {
            entity.HasIndex(e => e.ProjectId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => new { e.ProjectId, e.SortOrder });

            entity.HasOne(t => t.Project)
                  .WithMany(p => p.Tasks)
                  .HasForeignKey(t => t.ProjectId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Timeline 配置
        modelBuilder.Entity<Timeline>(entity =>
        {
            entity.HasIndex(e => e.ProjectId);
            entity.HasIndex(e => e.OccurredAt);
            entity.HasIndex(e => e.EventType);

            entity.HasOne(t => t.Project)
                  .WithMany(p => p.Timelines)
                  .HasForeignKey(t => t.ProjectId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // TaskTimeline 配置
        modelBuilder.Entity<TaskTimeline>(entity =>
        {
            entity.HasIndex(e => e.TaskId);
            entity.HasIndex(e => e.OccurredAt);

            entity.HasOne(t => t.Task)
                  .WithMany(p => p.TaskTimelines)
                  .HasForeignKey(t => t.TaskId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // TaskDelay 配置
        modelBuilder.Entity<TaskDelay>(entity =>
        {
            entity.HasIndex(e => e.TaskId);

            entity.HasOne(d => d.Task)
                  .WithMany(t => t.Delays)
                  .HasForeignKey(d => d.TaskId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // TaskExtraRequirement 配置
        modelBuilder.Entity<TaskExtraRequirement>(entity =>
        {
            entity.HasIndex(e => e.TaskId);

            entity.HasOne(e => e.Task)
                  .WithMany(t => t.ExtraRequirements)
                  .HasForeignKey(e => e.TaskId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // TaskCategory 配置
        modelBuilder.Entity<TaskCategory>(entity =>
        {
            entity.HasIndex(e => e.SortOrder);
        });

        // 种子数据 - 默认分类
        modelBuilder.Entity<TaskCategory>().HasData(
            new TaskCategory { Id = 1, Name = "开发", Icon = "code", Color = "#4A90D9", IsSystem = true, SortOrder = 1 },
            new TaskCategory { Id = 2, Name = "会议", Icon = "users", Color = "#F5C26B", IsSystem = true, SortOrder = 2 },
            new TaskCategory { Id = 3, Name = "文档", Icon = "file-text", Color = "#67CBAB", IsSystem = true, SortOrder = 3 },
            new TaskCategory { Id = 4, Name = "设计", Icon = "pen-tool", Color = "#A78BFA", IsSystem = true, SortOrder = 4 },
            new TaskCategory { Id = 5, Name = "调试", Icon = "bug", Color = "#E8908A", IsSystem = true, SortOrder = 5 },
            new TaskCategory { Id = 6, Name = "BUG", Icon = "alert-circle", Color = "#E8908A", IsSystem = true, SortOrder = 6 }
        );

        // 种子数据 - 用户设置
        modelBuilder.Entity<UserSettings>().HasData(
            new UserSettings { Id = 1 }
        );

        // User 配置
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.Name);
            entity.Property(e => e.Password).HasMaxLength(512);
        });

        // 种子数据 - 默认用户
        modelBuilder.Entity<User>().HasData(
            new User { Id = 1, Name = "用户", Password = null, CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now }
        );

        // ===== 资源管理模块配置 =====

        // Computer 配置
        modelBuilder.Entity<Computer>(entity =>
        {
            entity.HasIndex(e => e.HostName).IsUnique();
            entity.HasIndex(e => e.Name);
        });

        // ResourcePath 配置
        modelBuilder.Entity<ResourcePath>(entity =>
        {
            entity.HasIndex(e => e.ComputerId);
            entity.HasIndex(e => new { e.ComputerId, e.Type }).IsUnique();

            entity.HasOne(r => r.Computer)
                  .WithMany(c => c.ResourcePaths)
                  .HasForeignKey(r => r.ComputerId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Comic 配置
        modelBuilder.Entity<Comic>(entity =>
        {
            entity.HasIndex(e => e.ResourcePathId);
            entity.HasIndex(e => e.DisplayName);

            entity.HasOne(c => c.ResourcePath)
                  .WithMany(r => r.Comics)
                  .HasForeignKey(c => c.ResourcePathId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ComicChapter 配置
        modelBuilder.Entity<ComicChapter>(entity =>
        {
            entity.HasIndex(e => e.ComicId);
            entity.HasIndex(e => new { e.ComicId, e.SortOrder }).IsUnique();

            entity.HasOne(c => c.Comic)
                  .WithMany(m => m.Chapters)
                  .HasForeignKey(c => c.ComicId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ===== 子任务模块配置 =====

        // SubTask 配置
        modelBuilder.Entity<SubTask>(entity =>
        {
            entity.HasIndex(e => e.ParentTaskId);
            entity.HasIndex(e => e.SortOrder);

            entity.HasOne(s => s.ParentTask)
                  .WithMany(t => t.SubTasks)
                  .HasForeignKey(s => s.ParentTaskId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ===== AI 助手模块配置 =====

        // Conversation 配置
        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.UpdatedAt);
            entity.HasIndex(e => e.IsArchived);
            entity.HasIndex(e => e.IsPinned);
        });

        // ChatMessage 配置
        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.HasIndex(e => e.ConversationId);
            entity.HasIndex(e => e.CreatedAt);

            entity.HasOne(m => m.Conversation)
                  .WithMany(c => c.Messages)
                  .HasForeignKey(m => m.ConversationId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ===== 笔记模块配置 =====

        // Note 配置
        modelBuilder.Entity<Note>(entity =>
        {
            entity.HasIndex(e => e.UpdatedAt);
            entity.HasIndex(e => e.CreatedAt);
        });

        // NoteTag 配置
        modelBuilder.Entity<NoteTag>(entity =>
        {
            entity.HasIndex(e => e.NoteId);
            entity.HasIndex(e => e.TagId);
            entity.HasIndex(e => new { e.NoteId, e.TagId }).IsUnique();

            entity.HasOne(nt => nt.Note)
                  .WithMany(n => n.Tags)
                  .HasForeignKey(nt => nt.NoteId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ===== 日程模块配置 =====

        // Schedule 配置
        modelBuilder.Entity<Schedule>(entity =>
        {
            entity.HasIndex(e => e.StartDate);
            entity.HasIndex(e => e.EndDate);
            entity.HasIndex(e => e.ReminderEnabled);
            entity.HasIndex(e => e.CreatedAt);
        });

        // ScheduleDay 配置
        modelBuilder.Entity<ScheduleDay>(entity =>
        {
            entity.HasIndex(e => e.ScheduleId);
            entity.HasIndex(e => e.DayDate);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => new { e.ScheduleId, e.DayDate }).IsUnique();

            entity.HasOne(sd => sd.Schedule)
                  .WithMany(s => s.ScheduleDays)
                  .HasForeignKey(sd => sd.ScheduleId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ScheduleReminder 配置
        modelBuilder.Entity<ScheduleReminder>(entity =>
        {
            entity.HasIndex(e => e.ScheduleId);
            entity.HasIndex(e => e.ReminderDate);
            entity.HasIndex(e => new { e.ScheduleId, e.ReminderDate }).IsUnique();

            entity.HasOne(sr => sr.Schedule)
                  .WithMany(s => s.ScheduleReminders)
                  .HasForeignKey(sr => sr.ScheduleId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ===== 财务模块配置 =====

        // FinanceAccount 配置
        modelBuilder.Entity<FinanceAccount>(entity =>
        {
            entity.HasIndex(e => e.SortOrder);
        });

        // FinanceExpense 配置
        modelBuilder.Entity<FinanceExpense>(entity =>
        {
            entity.HasIndex(e => e.CategoryId);
            entity.HasIndex(e => e.AccountId);
            entity.HasIndex(e => e.ExpenseDate);
            entity.HasIndex(e => e.Type);

            entity.HasOne(e => e.Category)
                  .WithMany(c => c.Expenses)
                  .HasForeignKey(e => e.CategoryId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Account)
                  .WithMany(a => a.Expenses)
                  .HasForeignKey(e => e.AccountId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // FinanceExpenseItem 配置
        modelBuilder.Entity<FinanceExpenseItem>(entity =>
        {
            entity.HasIndex(e => e.ExpenseId);
            entity.HasIndex(e => new { e.ExpenseId, e.SortOrder });

            entity.HasOne(e => e.Expense)
                  .WithMany(ex => ex.Items)
                  .HasForeignKey(e => e.ExpenseId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // FinanceExpenseCategory 配置
        modelBuilder.Entity<FinanceExpenseCategory>(entity =>
        {
            entity.HasIndex(e => e.SortOrder);
            entity.HasIndex(e => e.Name);
        });

        // FinanceIncome 配置
        modelBuilder.Entity<FinanceIncome>(entity =>
        {
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.IncomeDate);
            entity.HasIndex(e => e.ProjectId);

            entity.HasOne(e => e.Project)
                  .WithMany()
                  .HasForeignKey(e => e.ProjectId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // FinanceSalaryTemplate 配置
        modelBuilder.Entity<FinanceSalaryTemplate>(entity =>
        {
            entity.HasIndex(e => e.IsActive);
        });

        // FinanceSalaryTemplateItem 配置
        modelBuilder.Entity<FinanceSalaryTemplateItem>(entity =>
        {
            entity.HasIndex(e => e.TemplateId);
            entity.HasIndex(e => new { e.TemplateId, e.SortOrder });

            entity.HasOne(e => e.Template)
                  .WithMany(t => t.TemplateItems)
                  .HasForeignKey(e => e.TemplateId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // FinanceSalaryDetail 配置
        modelBuilder.Entity<FinanceSalaryDetail>(entity =>
        {
            entity.HasIndex(e => e.IncomeId);
            entity.HasIndex(e => e.TemplateId);
            entity.HasIndex(e => e.SalaryDate);

            entity.HasOne(e => e.Income)
                  .WithOne(i => i.SalaryDetail)
                  .HasForeignKey<FinanceSalaryDetail>(e => e.IncomeId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Template)
                  .WithMany(t => t.SalaryDetails)
                  .HasForeignKey(e => e.TemplateId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // FinanceSalaryDetailItem 配置
        modelBuilder.Entity<FinanceSalaryDetailItem>(entity =>
        {
            entity.HasIndex(e => e.DetailId);
            entity.HasIndex(e => e.TemplateItemId);

            entity.HasOne(e => e.Detail)
                  .WithMany(d => d.DetailItems)
                  .HasForeignKey(e => e.DetailId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.TemplateItem)
                  .WithMany(ti => ti.DetailItems)
                  .HasForeignKey(e => e.TemplateItemId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // FinanceIncomeAccount 配置
        modelBuilder.Entity<FinanceIncomeAccount>(entity =>
        {
            entity.HasIndex(e => e.IncomeId);
            entity.HasIndex(e => e.AccountId);

            entity.HasOne(e => e.Income)
                  .WithMany(i => i.IncomeAccounts)
                  .HasForeignKey(e => e.IncomeId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Account)
                  .WithMany(a => a.IncomeAccounts)
                  .HasForeignKey(e => e.AccountId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // FinanceAccountTransfer 配置
        modelBuilder.Entity<FinanceAccountTransfer>(entity =>
        {
            entity.HasIndex(e => e.FromAccountId);
            entity.HasIndex(e => e.ToAccountId);
            entity.HasIndex(e => e.CreatedAt);

            entity.HasOne(e => e.FromAccount)
                  .WithMany(a => a.FromTransfers)
                  .HasForeignKey(e => e.FromAccountId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.ToAccount)
                  .WithMany(a => a.ToTransfers)
                  .HasForeignKey(e => e.ToAccountId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // FinanceAccountSnapshot 配置
        modelBuilder.Entity<FinanceAccountSnapshot>(entity =>
        {
            entity.HasIndex(e => e.AccountId);
            entity.HasIndex(e => e.SnapshotDate);
            entity.HasIndex(e => new { e.AccountId, e.SnapshotDate }).IsUnique();

            entity.HasOne(e => e.Account)
                  .WithMany(a => a.Snapshots)
                  .HasForeignKey(e => e.AccountId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
