using System.ComponentModel.DataAnnotations;

namespace ProjectHub.Api.Models;

/// <summary>账户转账记录</summary>
public class FinanceAccountTransfer
{
    [Key]
    public int Id { get; set; }

    /// <summary>转出账户ID</summary>
    public int FromAccountId { get; set; }

    /// <summary>转入账户ID</summary>
    public int ToAccountId { get; set; }

    /// <summary>转账金额</summary>
    public decimal Amount { get; set; }

    [MaxLength(500)]
    public string? Remark { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // 导航属性
    public virtual FinanceAccount? FromAccount { get; set; }
    public virtual FinanceAccount? ToAccount { get; set; }
}
