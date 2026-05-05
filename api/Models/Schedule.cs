using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ProjectHub.Api.Models;

public class Schedule
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Content { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    [MaxLength(20)]
    public string RepeatMode { get; set; } = "week";

    public string RepeatDays { get; set; } = "[]";

    [MaxLength(5)]
    public string StartTime { get; set; } = "09:00";

    [MaxLength(5)]
    public string EndTime { get; set; } = "10:00";

    public bool ReminderEnabled { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    [JsonIgnore]
    public virtual ICollection<ScheduleDay> ScheduleDays { get; set; } = new List<ScheduleDay>();
    
    [JsonIgnore]
    public virtual ICollection<ScheduleReminder> ScheduleReminders { get; set; } = new List<ScheduleReminder>();
}

public class ScheduleDay
{
    [Key]
    public int Id { get; set; }

    public int ScheduleId { get; set; }

    public DateTime DayDate { get; set; }

    [MaxLength(500)]
    public string? Content { get; set; }

    [MaxLength(20)]
    public string Status { get; set; } = "pending";

    [MaxLength(20)]
    public string? SkipReason { get; set; }

    public DateTime? CompletedAt { get; set; }
    public DateTime? SkippedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    [ForeignKey("ScheduleId")]
    [JsonIgnore]
    public virtual Schedule? Schedule { get; set; }
}

public class ScheduleReminder
{
    [Key]
    public int Id { get; set; }

    public int ScheduleId { get; set; }

    public DateTime ReminderDate { get; set; }

    public DateTime SentAt { get; set; } = DateTime.Now;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [ForeignKey("ScheduleId")]
    [JsonIgnore]
    public virtual Schedule? Schedule { get; set; }
}