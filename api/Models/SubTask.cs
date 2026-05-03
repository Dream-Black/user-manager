using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectHub.Api.Models;

/// <summary>
/// 子任务
/// </summary>
[Table("SubTasks")]
public class SubTask
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// 父任务ID
    /// </summary>
    public int ParentTaskId { get; set; }

    /// <summary>
    /// 子任务标题
    /// </summary>
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 子任务描述
    /// </summary>
    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// 是否完成
    /// </summary>
    public bool IsCompleted { get; set; }

    /// <summary>
/// 排序序号
/// </summary>
public int SortOrder { get; set; }

/// <summary>
/// 任务分类: dev-开发, meeting-会议, doc-文档, design-设计, debug-调试, bug-BUG
/// </summary>
[MaxLength(50)]
public string Category { get; set; } = "dev";

/// <summary>
/// 预估工时（小时）
/// </summary>
public decimal EstimatedHours { get; set; }

/// <summary>
/// 创建时间
/// </summary>
public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    // 导航属性
    [ForeignKey("ParentTaskId")]
    public ProjectTask? ParentTask { get; set; }
}
