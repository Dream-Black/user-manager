using System.ComponentModel.DataAnnotations;

namespace ProjectHub.Api.Models;

/// <summary>收入记录</summary>
public class FinanceIncome
{
    [Key]
    public int Id { get; set; }

    /// <summary>收入类型: salary-工资, misc-零散</summary>
    [MaxLength(20)]
    public string Type { get; set; } = "misc";

    /// <summary>收入金额</summary>
    public decimal Amount { get; set; }

    [Required]
    [MaxLength(500)]
    public string Content { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Remark { get; set; }

    /// <summary>关联项目ID</summary>
    public int? ProjectId { get; set; }

    /// <summary>收入日期</summary>
    public DateTime IncomeDate { get; set; } = DateTime.Now;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    // 导航属性
    public virtual Project? Project { get; set; }
    public virtual FinanceSalaryDetail? SalaryDetail { get; set; }
    public virtual ICollection<FinanceIncomeAccount> IncomeAccounts { get; set; } = new List<FinanceIncomeAccount>();
}
