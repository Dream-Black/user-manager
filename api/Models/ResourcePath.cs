using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ProjectHub.Api.Models;

/// <summary>
/// 资源路径表 - 存储每台电脑上的资源根目录路径
/// </summary>
public class ResourcePath
{
    [Key]
    public int Id { get; set; }

    /// <summary>所属电脑ID</summary>
    [Required]
    public int ComputerId { get; set; }

    /// <summary>资源类型: comic/video/novel/image</summary>
    [Required]
    [MaxLength(50)]
    public string Type { get; set; } = "comic";

    /// <summary>资源根目录路径（以/结尾）</summary>
    [Required]
    [MaxLength(1000)]
    public string Path { get; set; } = string.Empty;

    /// <summary>是否启用</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>创建时间</summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // 导航属性
    [ForeignKey("ComputerId")]
    [JsonIgnore] // 避免 Swagger 循环引用
    public virtual Computer? Computer { get; set; }

    public virtual ICollection<Comic> Comics { get; set; } = new List<Comic>();
}
