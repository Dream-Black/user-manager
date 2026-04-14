using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectHub.Api.Models;

public class Project
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>项目类型: work-工作, freelance-私单</summary>
    [MaxLength(50)]
    public string Type { get; set; } = "work";

    /// <summary>客户名称</summary>
    [MaxLength(200)]
    public string? Customer { get; set; }

    /// <summary>项目状态: active-进行中, completed-已完成, archived-已归档</summary>
    [MaxLength(50)]
    public string Status { get; set; } = "active";

    /// <summary>创建时间</summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>更新时间</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    /// <summary>完成时间</summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>开始日期</summary>
    public DateTime? StartDate { get; set; }

    /// <summary>截止日期</summary>
    public DateTime? EndDate { get; set; }

    /// <summary>分类ID</summary>
    public int? CategoryId { get; set; }

    // 导航属性
    public virtual ICollection<ProjectTask> Tasks { get; set; } = new List<ProjectTask>();
    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
    public virtual ICollection<Timeline> Timelines { get; set; } = new List<Timeline>();
}
