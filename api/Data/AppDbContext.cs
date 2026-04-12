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
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<TaskCategory> TaskCategories => Set<TaskCategory>();
    public DbSet<UserSettings> UserSettings => Set<UserSettings>();

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

        // Review 配置
        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasIndex(e => e.ProjectId);
            entity.HasIndex(e => e.CreatedAt);

            entity.HasOne(r => r.Project)
                  .WithMany(p => p.Reviews)
                  .HasForeignKey(r => r.ProjectId)
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
    }
}
