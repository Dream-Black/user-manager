using System.ComponentModel.DataAnnotations;

namespace ProjectHub.Api.Models;

/// <summary>账户每日余额快照</summary>
public class FinanceAccountSnapshot
{
    [Key]
    public int Id { get; set; }

    /// <summary>关联账户ID</summary>
    public int AccountId { get; set; }

    /// <summary>快照日期</summary>
    public DateTime SnapshotDate { get; set; } = DateTime.Now;

    /// <summary>当时余额</summary>
    public decimal Balance { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // 导航属性
    public virtual FinanceAccount? Account { get; set; }
}
