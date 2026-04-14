using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectHub.Api.Models;

/// <summary>
/// 漫画章节表 - 存储漫画的章节信息
/// </summary>
public class ComicChapter
{
    [Key]
    public int Id { get; set; }

    /// <summary>所属漫画ID</summary>
    [Required]
    public int ComicId { get; set; }

    /// <summary>文件夹名称（原始名称）</summary>
    [Required]
    [MaxLength(255)]
    public string FolderName { get; set; } = string.Empty;

    /// <summary>显示名称（用户可编辑）</summary>
    [Required]
    [MaxLength(255)]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>排序顺序</summary>
    public int SortOrder { get; set; }

    /// <summary>创建时间</summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // 导航属性
    [ForeignKey("ComicId")]
    public virtual Comic? Comic { get; set; }
}
