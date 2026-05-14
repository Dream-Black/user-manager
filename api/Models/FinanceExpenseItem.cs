using System.ComponentModel.DataAnnotations;

namespace ProjectHub.Api.Models;

/// <summary>支出清单子项</summary>
public class FinanceExpenseItem
{
    [Key]
    public int Id { get; set; }

    /// <summary>所属支出ID</summary>
    public int ExpenseId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>数量</summary>
    public int Quantity { get; set; }

    /// <summary>单位</summary>
    [MaxLength(20)]
    public string? Unit { get; set; }

    /// <summary>单价</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>小计</summary>
    public decimal Subtotal { get; set; }

    /// <summary>排序</summary>
    public int SortOrder { get; set; }

    // 导航属性
    public virtual FinanceExpense? Expense { get; set; }
}
