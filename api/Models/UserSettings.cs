using System.ComponentModel.DataAnnotations;

namespace ProjectHub.Api.Models;

/// <summary>
/// 用户设置
/// </summary>
public class UserSettings
{
    [Key]
    public int Id { get; set; } = 1;

    /// <summary>DeepSeek API Key</summary>
    [MaxLength(500)]
    public string? DeepSeekApiKey { get; set; }

    /// <summary>DeepSeek 模型</summary>
    [MaxLength(100)]
    public string DeepSeekModel { get; set; } = "deepseek-chat";

    /// <summary>上班时间</summary>
    public TimeSpan WorkStartTime { get; set; } = new TimeSpan(9, 0, 0);

    /// <summary>下班时间</summary>
    public TimeSpan WorkEndTime { get; set; } = new TimeSpan(18, 0, 0);

    /// <summary>午休开始时间</summary>
    public TimeSpan? LunchBreakStart { get; set; }

    /// <summary>午休结束时间</summary>
    public TimeSpan? LunchBreakEnd { get; set; }

    /// <summary>通勤时间（小时）</summary>
    public decimal CommuteHours { get; set; } = 1;

    /// <summary>当前职业</summary>
    [MaxLength(200)]
    public string? CurrentJob { get; set; }

    /// <summary>当前公司</summary>
    [MaxLength(200)]
    public string? CurrentCompany { get; set; }

    /// <summary>当前计划</summary>
    [MaxLength(500)]
    public string? CurrentPlan { get; set; }

    /// <summary>每日提醒时间</summary>
    public TimeSpan ReminderTime { get; set; } = new TimeSpan(8, 30, 0);

    /// <summary>是否启用提醒</summary>
    public bool ReminderEnabled { get; set; } = true;

    /// <summary>主题模式: light-明亮, dark-深色, system-跟随系统</summary>
    [MaxLength(20)]
    public string ThemeMode { get; set; } = "light";

    /// <summary>更新时间</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
