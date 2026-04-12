using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectHub.Api.Models;

/// <summary>
/// 任务延期记录
/// </summary>
public class TaskDelay
{
    [Key]
    public int Id { get; set; }

    public int TaskId { get; set; }

    /// <summary>延期原因</summary>
    [Required]
    [MaxLength(1000)]
    public string Reason { get; set; } = string.Empty;

    /// <summary>原计划结束时间</summary>
    public DateTime OriginalPlanEndDate { get; set; }

    /// <summary>新计划结束时间</summary>
    public DateTime NewPlanEndDate { get; set; }

    /// <summary>延期创建时间</summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // 导航属性
    [ForeignKey("TaskId")]
    public virtual ProjectTask? Task { get; set; }
}

/// <summary>
/// 计划外需求（已完成任务追加的新需求）
/// </summary>
public class TaskExtraRequirement
{
    [Key]
    public int Id { get; set; }

    public int TaskId { get; set; }

    /// <summary>需求描述</summary>
    [Required]
    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    /// <summary>创建时间</summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // 导航属性
    [ForeignKey("TaskId")]
    public virtual ProjectTask? Task { get; set; }
}
