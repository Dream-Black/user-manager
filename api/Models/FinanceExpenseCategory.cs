using System.ComponentModel.DataAnnotations;

namespace ProjectHub.Api.Models;

/// <summary>支出分类</summary>
public class FinanceExpenseCategory
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Icon { get; set; }

    [MaxLength(20)]
    public string Color { get; set; } = "#4A90D9";

    /// <summary>是否为系统预设分类</summary>
    public bool IsSystem { get; set; }

    /// <summary>排序</summary>
    public int SortOrder { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // 导航属性
    public virtual ICollection<FinanceExpense> Expenses { get; set; } = new List<FinanceExpense>();
}
