using System.ComponentModel.DataAnnotations;

namespace ProjectHub.Api.Models;

/// <summary>
/// 任务分类
/// </summary>
public class TaskCategory
{
    [Key]
    public int Id { get; set; }

    /// <summary>分类名称</summary>
    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    /// <summary>分类图标</summary>
    [MaxLength(100)]
    public string? Icon { get; set; }

    /// <summary>分类颜色</summary>
    [MaxLength(20)]
    public string Color { get; set; } = "#4A90D9";

    /// <summary>是否为系统默认分类</summary>
    public bool IsSystem { get; set; }

    /// <summary>排序</summary>
    public int SortOrder { get; set; }

    /// <summary>创建时间</summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
