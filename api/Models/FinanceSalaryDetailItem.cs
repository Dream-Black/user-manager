using System.ComponentModel.DataAnnotations;

namespace ProjectHub.Api.Models;

/// <summary>工资每项的值</summary>
public class FinanceSalaryDetailItem
{
    [Key]
    public int Id { get; set; }

    /// <summary>所属工资明细ID</summary>
    public int DetailId { get; set; }

    /// <summary>关联模板子项ID</summary>
    public int TemplateItemId { get; set; }

    /// <summary>金额</summary>
    public decimal Amount { get; set; }

    // 导航属性
    public virtual FinanceSalaryDetail? Detail { get; set; }
    public virtual FinanceSalaryTemplateItem? TemplateItem { get; set; }
}
