using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectHub.Api.Models;

/// <summary>
/// 项目级时间线事件
/// </summary>
public class Timeline
{
    [Key]
    public int Id { get; set; }

    public int ProjectId { get; set; }

    /// <summary>事件类型: created-创建, task_completed-任务完成, task_delayed-延期, reviewed-复盘, status_changed-状态变更</summary>
    [MaxLength(50)]
    public string EventType { get; set; } = string.Empty;

    /// <summary>事件标题</summary>
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>事件描述</summary>
    [MaxLength(2000)]
    public string? Description { get; set; }

    /// <summary>关联任务ID（可选）</summary>
    public int? TaskId { get; set; }

    /// <summary>事件时间</summary>
    public DateTime OccurredAt { get; set; } = DateTime.Now;

    /// <summary>额外数据（JSON）</summary>
    public string? Metadata { get; set; }

    // 导航属性
    [ForeignKey("ProjectId")]
    public virtual Project? Project { get; set; }
}

/// <summary>
/// 任务专属时间线（更详细的任务变更历史）
/// </summary>
public class TaskTimeline
{
    [Key]
    public int Id { get; set; }

    public int TaskId { get; set; }

    /// <summary>变更类型: created-创建, status_changed-状态变更, progress_updated-进度更新, delayed-延期, completed-完成, extra_added-追加需求</summary>
    [MaxLength(50)]
    public string ChangeType { get; set; } = string.Empty;

    /// <summary>变更标题</summary>
    [MaxLength(100)]
    public string Title { get; set; } = string.Empty;

    /// <summary>变更详情</summary>
    [MaxLength(500)]
    public string? Details { get; set; }

    /// <summary>变更前值</summary>
    [MaxLength(200)]
    public string? OldValue { get; set; }

    /// <summary>变更后值</summary>
    [MaxLength(200)]
    public string? NewValue { get; set; }

    /// <summary>变更时间</summary>
    public DateTime OccurredAt { get; set; } = DateTime.Now;

    // 导航属性
    [ForeignKey("TaskId")]
    public virtual ProjectTask? Task { get; set; }
}
