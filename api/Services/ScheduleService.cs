using Microsoft.EntityFrameworkCore;
using ProjectHub.Api.Data;
using ProjectHub.Api.Models;
using System.Text.Json;

namespace ProjectHub.Api.Services;

public class ScheduleService
{
    private readonly AppDbContext _context;

    public ScheduleService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Schedule>> GetAllSchedules()
    {
        return await _context.Schedules
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    public async Task<Schedule?> GetScheduleById(int id)
    {
        return await _context.Schedules
            .Include(s => s.ScheduleDays)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<Schedule> CreateSchedule(Schedule schedule)
    {
        schedule.CreatedAt = DateTime.Now;
        schedule.UpdatedAt = DateTime.Now;
        _context.Schedules.Add(schedule);
        await _context.SaveChangesAsync();
        return schedule;
    }

    public async Task<Schedule?> UpdateSchedule(int id, Schedule updatedSchedule)
    {
        var schedule = await _context.Schedules.FindAsync(id);
        if (schedule == null) return null;

        schedule.Title = updatedSchedule.Title;
        schedule.Content = updatedSchedule.Content;
        schedule.StartDate = updatedSchedule.StartDate;
        schedule.EndDate = updatedSchedule.EndDate;
        schedule.RepeatMode = updatedSchedule.RepeatMode;
        schedule.RepeatDays = updatedSchedule.RepeatDays;
        schedule.StartTime = updatedSchedule.StartTime;
        schedule.EndTime = updatedSchedule.EndTime;
        schedule.ReminderEnabled = updatedSchedule.ReminderEnabled;
        schedule.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();
        return schedule;
    }

    public async Task<bool> DeleteSchedule(int id)
    {
        var schedule = await _context.Schedules.FindAsync(id);
        if (schedule == null) return false;

        _context.Schedules.Remove(schedule);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<ScheduleDay>> GetScheduleDays(int scheduleId)
    {
        return await _context.ScheduleDays
            .Where(sd => sd.ScheduleId == scheduleId)
            .OrderBy(sd => sd.DayDate)
            .ToListAsync();
    }

    public async Task<ScheduleDay?> GetScheduleDay(int scheduleId, DateTime dayDate)
    {
        return await _context.ScheduleDays
            .FirstOrDefaultAsync(sd => sd.ScheduleId == scheduleId && 
                                      sd.DayDate.Date == dayDate.Date);
    }

    public async Task<ScheduleDay> UpsertScheduleDay(int scheduleId, DateTime dayDate, string? content, string? status, string? skipReason)
    {
        var existingDay = await GetScheduleDay(scheduleId, dayDate);
        
        if (existingDay != null)
        {
            if (content != null) existingDay.Content = content;
            if (status != null) 
            {
                existingDay.Status = status;
                if (status == "completed")
                    existingDay.CompletedAt = DateTime.Now;
                else if (status == "skipped")
                {
                    existingDay.SkippedAt = DateTime.Now;
                    existingDay.SkipReason = skipReason;
                }
            }
            existingDay.UpdatedAt = DateTime.Now;
        }
        else
        {
            existingDay = new ScheduleDay
            {
                ScheduleId = scheduleId,
                DayDate = dayDate.Date,
                Content = content,
                Status = status ?? "pending",
                SkipReason = skipReason,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
            _context.ScheduleDays.Add(existingDay);
        }

        await _context.SaveChangesAsync();
        return existingDay;
    }

    public async Task<bool> DeleteScheduleDay(int dayId)
    {
        var day = await _context.ScheduleDays.FindAsync(dayId);
        if (day == null) return false;

        _context.ScheduleDays.Remove(day);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<ScheduleDay>> GenerateAndSaveScheduleDays(int scheduleId, DateTime startDate, DateTime endDate, string repeatMode, string repeatDays)
    {
        var schedule = await _context.Schedules.FindAsync(scheduleId);
        if (schedule == null)
            throw new ArgumentException("日程不存在");

        var existingDays = await _context.ScheduleDays
            .Where(sd => sd.ScheduleId == scheduleId)
            .ToDictionaryAsync(sd => sd.DayDate.Date);

        var generatedDays = new List<ScheduleDay>();
        var datesToGenerate = GenerateActiveDates(startDate.Date, endDate.Date, repeatMode, repeatDays);

        foreach (var date in datesToGenerate)
        {
            if (existingDays.TryGetValue(date, out var existingDay))
            {
                existingDay.UpdatedAt = DateTime.Now;
                generatedDays.Add(existingDay);
            }
            else
            {
                var newDay = new ScheduleDay
                {
                    ScheduleId = scheduleId,
                    DayDate = date,
                    Content = null,
                    Status = "pending",
                    SkipReason = null,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };
                _context.ScheduleDays.Add(newDay);
                generatedDays.Add(newDay);
            }
        }

        var datesToDelete = existingDays.Keys.Except(datesToGenerate).ToList();
        foreach (var dateToDelete in datesToDelete)
        {
            var dayToDelete = existingDays[dateToDelete];
            _context.ScheduleDays.Remove(dayToDelete);
        }

        await _context.SaveChangesAsync();
        return generatedDays;
    }

    public List<DateTime> GenerateActiveDates(DateTime startDate, DateTime endDate, string repeatMode, string repeatDays)
    {
        var dates = new List<DateTime>();
        
        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            if (repeatMode == "week")
            {
                var weekDays = JsonSerializer.Deserialize<List<string>>(repeatDays) ?? new List<string>();
                if (weekDays.Contains(((int)date.DayOfWeek).ToString()))
                {
                    dates.Add(date);
                }
            }
            else
            {
                var monthDays = JsonSerializer.Deserialize<List<int>>(repeatDays) ?? new List<int>();
                if (monthDays.Contains(date.Day))
                {
                    dates.Add(date);
                }
            }
        }
        
        return dates;
    }

    public async Task<ScheduleDay> UpdateScheduleDayContent(int scheduleId, DateTime dayDate, string? content)
    {
        var day = await GetScheduleDay(scheduleId, dayDate);
        if (day == null)
        {
            day = new ScheduleDay
            {
                ScheduleId = scheduleId,
                DayDate = dayDate.Date,
                Content = content,
                Status = "pending",
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
            _context.ScheduleDays.Add(day);
        }
        else
        {
            day.Content = content;
            day.UpdatedAt = DateTime.Now;
        }
        
        await _context.SaveChangesAsync();
        return day;
    }

    public async Task<ScheduleDay> UpdateScheduleDayStatus(int scheduleId, DateTime dayDate, string status, string? skipReason = null)
    {
        var day = await GetScheduleDay(scheduleId, dayDate);
        if (day == null)
        {
            day = new ScheduleDay
            {
                ScheduleId = scheduleId,
                DayDate = dayDate.Date,
                Content = null,
                Status = status,
                SkipReason = status == "skipped" ? skipReason : null,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
            _context.ScheduleDays.Add(day);
        }
        else
        {
            day.Status = status;
            day.SkipReason = status == "skipped" ? skipReason : null;
            day.UpdatedAt = DateTime.Now;
            if (status == "completed")
                day.CompletedAt = DateTime.Now;
            else if (status == "skipped")
                day.SkippedAt = DateTime.Now;
        }
        
        await _context.SaveChangesAsync();
        return day;
    }

    public bool IsDayActive(Schedule schedule, DateTime date)
    {
        if (date.Date < schedule.StartDate.Date || date.Date > schedule.EndDate.Date)
            return false;

        if (schedule.RepeatMode == "week")
        {
            var weekDays = JsonSerializer.Deserialize<List<string>>(schedule.RepeatDays) ?? new List<string>();
            return weekDays.Contains(((int)date.DayOfWeek).ToString());
        }
        else
        {
            var monthDays = JsonSerializer.Deserialize<List<int>>(schedule.RepeatDays) ?? new List<int>();
            return monthDays.Contains(date.Day);
        }
    }

    public async Task<List<Schedule>> GetActiveSchedulesForDate(DateTime date)
    {
        var schedules = await _context.Schedules
            .Where(s => s.ReminderEnabled && s.StartDate <= date.Date && s.EndDate >= date.Date)
            .ToListAsync();

        return schedules.Where(s => IsDayActive(s, date)).ToList();
    }

    public async Task<bool> HasReminderBeenSent(int scheduleId, DateTime reminderDate)
    {
        return await _context.ScheduleReminders
            .AnyAsync(sr => sr.ScheduleId == scheduleId && 
                           sr.ReminderDate.Date == reminderDate.Date);
    }

    public async Task RecordReminderSent(int scheduleId, DateTime reminderDate)
    {
        var reminder = new ScheduleReminder
        {
            ScheduleId = scheduleId,
            ReminderDate = reminderDate.Date,
            SentAt = DateTime.Now,
            CreatedAt = DateTime.Now
        };
        _context.ScheduleReminders.Add(reminder);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteReminderRecord(int scheduleId, DateTime reminderDate)
    {
        var reminder = await _context.ScheduleReminders
            .FirstOrDefaultAsync(sr => sr.ScheduleId == scheduleId && 
                                      sr.ReminderDate.Date == reminderDate.Date);
        if (reminder != null)
        {
            _context.ScheduleReminders.Remove(reminder);
            await _context.SaveChangesAsync();
        }
    }
}