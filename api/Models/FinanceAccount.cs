using System.ComponentModel.DataAnnotations;

namespace ProjectHub.Api.Models;

/// <summary>财务账户</summary>
public class FinanceAccount
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>账户类型: cash-现金, bank-银行, wechat-微信, alipay-支付宝</summary>
    [MaxLength(50)]
    public string Type { get; set; } = "cash";

    [MaxLength(100)]
    public string? Icon { get; set; }

    [MaxLength(20)]
    public string Color { get; set; } = "#4A90D9";

    /// <summary>账户余额</summary>
    public decimal Balance { get; set; }

    /// <summary>是否为默认支出账户</summary>
    public bool IsDefaultExpense { get; set; }

    /// <summary>排序</summary>
    public int SortOrder { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    // 导航属性
    public virtual ICollection<FinanceExpense> Expenses { get; set; } = new List<FinanceExpense>();
    public virtual ICollection<FinanceIncomeAccount> IncomeAccounts { get; set; } = new List<FinanceIncomeAccount>();
    public virtual ICollection<FinanceAccountTransfer> FromTransfers { get; set; } = new List<FinanceAccountTransfer>();
    public virtual ICollection<FinanceAccountTransfer> ToTransfers { get; set; } = new List<FinanceAccountTransfer>();
    public virtual ICollection<FinanceAccountSnapshot> Snapshots { get; set; } = new List<FinanceAccountSnapshot>();
}
