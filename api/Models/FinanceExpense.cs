using System.ComponentModel.DataAnnotations;

namespace ProjectHub.Api.Models;

/// <summary>支出记录</summary>
public class FinanceExpense
{
    [Key]
    public int Id { get; set; }

    /// <summary>支出类型: simple-简单, list-清单</summary>
    [MaxLength(20)]
    public string Type { get; set; } = "simple";

    /// <summary>支出金额</summary>
    public decimal Amount { get; set; }

    [Required]
    [MaxLength(500)]
    public string Purpose { get; set; } = string.Empty;

    /// <summary>分类ID（可空，待AI分类）</summary>
    public int? CategoryId { get; set; }

    /// <summary>支出账户ID</summary>
    public int AccountId { get; set; }

    /// <summary>支出日期</summary>
    public DateTime ExpenseDate { get; set; } = DateTime.Now;

    [MaxLength(500)]
    public string? Remark { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    // 导航属性
    public virtual FinanceExpenseCategory? Category { get; set; }
    public virtual FinanceAccount? Account { get; set; }
    public virtual ICollection<FinanceExpenseItem> Items { get; set; } = new List<FinanceExpenseItem>();
}
