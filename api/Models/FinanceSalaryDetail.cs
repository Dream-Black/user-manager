using System.ComponentModel.DataAnnotations;

namespace ProjectHub.Api.Models;

/// <summary>某月工资录入</summary>
public class FinanceSalaryDetail
{
    [Key]
    public int Id { get; set; }

    /// <summary>关联收入ID</summary>
    public int IncomeId { get; set; }

    /// <summary>关联模板ID</summary>
    public int TemplateId { get; set; }

    /// <summary>工资日期</summary>
    public DateTime SalaryDate { get; set; } = DateTime.Now;

    [MaxLength(500)]
    public string? Remark { get; set; }

    /// <summary>实际到手项的模板子项ID</summary>
    public int? ActualItemId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    // 导航属性
    public virtual FinanceIncome? Income { get; set; }
    public virtual FinanceSalaryTemplate? Template { get; set; }
    public virtual ICollection<FinanceSalaryDetailItem> DetailItems { get; set; } = new List<FinanceSalaryDetailItem>();
}
