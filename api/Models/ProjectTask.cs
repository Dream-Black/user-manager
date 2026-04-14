using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ProjectHub.Api.Models;

public class ProjectTask
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    /// <summary>所属项目ID</summary>
    public int ProjectId { get; set; }

    /// <summary>任务分类: dev-开发, meeting-会议, doc-文档, design-设计, debug-调试, bug-BUG</summary>
    [MaxLength(50)]
    public string Category { get; set; } = "dev";

    /// <summary>优先级: high-高, medium-中, low-低</summary>
    [MaxLength(50)]
    public string Priority { get; set; } = "medium";

    /// <summary>状态: todo-待办, in_progress-进行中, completed-已完成</summary>
    [MaxLength(50)]
    public string Status { get; set; } = "todo";

    /// <summary>预估工时（小时）</summary>
    public decimal EstimatedHours { get; set; }

    /// <summary>进度百分比 0-100</summary>
    public int Progress { get; set; }

    /// <summary>排序序号</summary>
    public int SortOrder { get; set; }

    /// <summary>计划开始时间</summary>
    public DateTime? PlanStartDate { get; set; }

    /// <summary>计划结束时间</summary>
    public DateTime? PlanEndDate { get; set; }

    /// <summary>实际开始时间</summary>
    public DateTime? ActualStartDate { get; set; }

    /// <summary>实际完成时间</summary>
    public DateTime? ActualEndDate { get; set; }

    /// <summary>创建时间</summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>更新时间</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    // 导航属性
    [ForeignKey("ProjectId")]
    [JsonIgnore] // 避免 Swagger 循环引用
    public virtual Project? Project { get; set; }
    public virtual ICollection<TaskTimeline> TaskTimelines { get; set; } = new List<TaskTimeline>();
    public virtual ICollection<TaskDelay> Delays { get; set; } = new List<TaskDelay>();
    public virtual ICollection<TaskExtraRequirement> ExtraRequirements { get; set; } = new List<TaskExtraRequirement>();
}
