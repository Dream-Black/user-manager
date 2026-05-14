using System.ComponentModel.DataAnnotations;

namespace ProjectHub.Api.Models;

/// <summary>工资模板子项</summary>
public class FinanceSalaryTemplateItem
{
    [Key]
    public int Id { get; set; }

    /// <summary>所属模板ID</summary>
    public int TemplateId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>排序</summary>
    public int SortOrder { get; set; }

    // 导航属性
    public virtual FinanceSalaryTemplate? Template { get; set; }
    public virtual ICollection<FinanceSalaryDetailItem> DetailItems { get; set; } = new List<FinanceSalaryDetailItem>();
}
