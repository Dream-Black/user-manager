using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectHub.Api.Models;

/// <summary>
/// AI 对话消息
/// </summary>
public class ChatMessage
{
    [Key]
    public int Id { get; set; }

    /// <summary>所属对话ID</summary>
    public int ConversationId { get; set; }

    /// <summary>角色：user/assistant/system/tool</summary>
    [MaxLength(20)]
    public string Role { get; set; } = "user";

    /// <summary>消息内容</summary>
    [Column(TypeName = "LONGTEXT")]
    public string? Content { get; set; }

    /// <summary>思考过程内容（深度思考模式时）</summary>
    [Column(TypeName = "LONGTEXT")]
    public string? ReasoningContent { get; set; }

    /// <summary>工具调用记录 JSON</summary>
    [Column(TypeName = "LONGTEXT")]
    public string? ToolCalls { get; set; }

    /// <summary>AI 草案 JSON</summary>
    [Column(TypeName = "LONGTEXT")]
    public string? Attachments { get; set; }

    /// <summary>用户上传附件 JSON（文件名、类型等）</summary>
    [Column(TypeName = "LONGTEXT")]
    public string? FilesJson { get; set; }

    /// <summary>创建时间</summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // 导航属性
    [ForeignKey("ConversationId")]
    public virtual Conversation? Conversation { get; set; }
}
