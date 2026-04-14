using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ProjectHub.Api.Models;

/// <summary>
/// 漫画表 - 存储漫画基本信息
/// </summary>
public class Comic
{
    [Key]
    public int Id { get; set; }

    /// <summary>所属资源路径ID</summary>
    [Required]
    public int ResourcePathId { get; set; }

    /// <summary>文件夹名称（原始名称）</summary>
    [Required]
    [MaxLength(255)]
    public string FolderName { get; set; } = string.Empty;

    /// <summary>显示名称（用户可编辑）</summary>
    [Required]
    [MaxLength(255)]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>漫画类型: manga/comic/novel/picture</summary>
    [MaxLength(50)]
    public string Type { get; set; } = "manga";

    /// <summary>封面图Base64（前端压缩后上传）</summary>
    [Column(TypeName = "mediumtext")]
    public string? ThumbnailBase64 { get; set; }

    /// <summary>创建时间</summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>更新时间</summary>
    public DateTime? UpdatedAt { get; set; }

    // 导航属性
    [ForeignKey("ResourcePathId")]
    [JsonIgnore] // 避免 Swagger 循环引用
    public virtual ResourcePath? ResourcePath { get; set; }

    public virtual ICollection<ComicChapter> Chapters { get; set; } = new List<ComicChapter>();
}
