using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectHub.Api.Models;

/// <summary>
/// 电脑表 - 用于识别和管理不同电脑
/// </summary>
public class Computer
{
    [Key]
    public int Id { get; set; }

    /// <summary>用户自定义的电脑名称</summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>电脑主机名（用于自动识别）</summary>
    [Required]
    [MaxLength(255)]
    public string HostName { get; set; } = string.Empty;

    /// <summary>是否在线（心跳检测）</summary>
    public bool IsOnline { get; set; } = true;

    /// <summary>最后心跳时间</summary>
    public DateTime? LastHeartbeat { get; set; }

    /// <summary>创建时间</summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // 导航属性
    public virtual ICollection<ResourcePath> ResourcePaths { get; set; } = new List<ResourcePath>();
}
