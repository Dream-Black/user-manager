using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectHub.Api.Models;

/// <summary>
/// 复盘记录
/// </summary>
public class Review
{
    [Key]
    public int Id { get; set; }

    /// <summary>关联项目ID</summary>
    public int ProjectId { get; set; }

    /// <summary>关联任务ID（可选）</summary>
    public int? TaskId { get; set; }

    /// <summary>复盘标题</summary>
    [MaxLength(200)]
    public string? Title { get; set; }

    /// <summary>做得好的地方</summary>
    [MaxLength(2000)]
    public string? GoodPoints { get; set; }

    /// <summary>需要改进的地方</summary>
    [MaxLength(2000)]
    public string? Improvements { get; set; }

    /// <summary>下一步行动</summary>
    [MaxLength(2000)]
    public string? NextActions { get; set; }

    /// <summary>创建时间</summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // 导航属性
    [ForeignKey("ProjectId")]
    public virtual Project? Project { get; set; }
}
