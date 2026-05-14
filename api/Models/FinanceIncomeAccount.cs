using System.ComponentModel.DataAnnotations;

namespace ProjectHub.Api.Models;

/// <summary>收入分配到账户</summary>
public class FinanceIncomeAccount
{
    [Key]
    public int Id { get; set; }

    /// <summary>关联收入ID</summary>
    public int IncomeId { get; set; }

    /// <summary>关联账户ID</summary>
    public int AccountId { get; set; }

    /// <summary>分配金额</summary>
    public decimal Amount { get; set; }

    // 导航属性
    public virtual FinanceIncome? Income { get; set; }
    public virtual FinanceAccount? Account { get; set; }
}
