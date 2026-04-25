using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectHub.Api.Models;

/// <summary>
/// AI 对话会话
/// </summary>
public class Conversation
{
    [Key]
    public int Id { get; set; }

    /// <summary>对话标题（自动取首条消息摘要）</summary>
    [MaxLength(200)]
    public string Title { get; set; } = "新对话";

    /// <summary>创建时间</summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>最后活跃时间</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    /// <summary>是否归档</summary>
    public bool IsArchived { get; set; } = false;

    /// <summary>是否置顶</summary>
    public bool IsPinned { get; set; } = false;

    /// <summary>用户偏好摘要</summary>
    [Column(TypeName = "LONGTEXT")]
    public string? MemorySummary { get; set; }

    // 导航属性
    public virtual ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
}
