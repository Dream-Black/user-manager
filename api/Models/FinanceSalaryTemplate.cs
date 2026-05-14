using System.ComponentModel.DataAnnotations;

namespace ProjectHub.Api.Models;

/// <summary>工资明细模板</summary>
public class FinanceSalaryTemplate
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Remark { get; set; }

    /// <summary>是否启用</summary>
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    // 导航属性
    public virtual ICollection<FinanceSalaryTemplateItem> TemplateItems { get; set; } = new List<FinanceSalaryTemplateItem>();
    public virtual ICollection<FinanceSalaryDetail> SalaryDetails { get; set; } = new List<FinanceSalaryDetail>();
}
