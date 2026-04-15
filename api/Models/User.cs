using System.ComponentModel.DataAnnotations;

namespace ProjectHub.Api.Models;

/// <summary>
/// 用户信息
/// </summary>
public class User
{
    [Key]
    public int Id { get; set; } = 1;

    /// <summary>用户姓名</summary>
    [MaxLength(100)]
    [Required]
    public string Name { get; set; } = "用户";

    /// <summary>邮箱</summary>
    [MaxLength(200)]
    [EmailAddress]
    public string? Email { get; set; }

    /// <summary>手机号</summary>
    [MaxLength(20)]
    public string? Phone { get; set; }

    /// <summary>部门</summary>
    [MaxLength(50)]
    public string? Department { get; set; }

    /// <summary>职位</summary>
    [MaxLength(100)]
    public string? Role { get; set; }

    /// <summary>头像（Base64编码的100x100 JPG图片）</summary>
    [MaxLength(50000)]
    public string? Avatar { get; set; }

    /// <summary>创建时间</summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>更新时间</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
