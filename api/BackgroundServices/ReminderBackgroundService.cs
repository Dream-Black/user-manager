using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;

namespace ProjectHub.Api.BackgroundServices;

public class ReminderBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ReminderBackgroundService> _logger;

    public ReminderBackgroundService(IServiceProvider serviceProvider, ILogger<ReminderBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ReminderBackgroundService is starting...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAndSendReminders();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while checking reminders");
            }

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }

        _logger.LogInformation("ReminderBackgroundService is stopping...");
    }

    private async Task CheckAndSendReminders()
    {
        using var scope = _serviceProvider.CreateScope();
        var scheduleService = scope.ServiceProvider.GetRequiredService<Services.ScheduleService>();
        var sseService = scope.ServiceProvider.GetRequiredService<Services.SseService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<Data.AppDbContext>();

        var now = DateTime.Now;
        _logger.LogInformation($"[Reminder] 检查提醒: {now:HH:mm:ss}, 连接数: {sseService.GetConnectionCount()}");

        // 1. 获取所有打开了定时提醒的日程
        var schedules = await dbContext.Schedules
            .Where(s => s.ReminderEnabled)
            .ToListAsync();

        foreach (var schedule in schedules)
        {
            // 2. 解析开始时间
            if (!TimeSpan.TryParse(schedule.StartTime, out var startTimeSpan))
            {
                continue;
            }

            var scheduleStartTime = now.Date.Add(startTimeSpan);
            var reminderStartTime = scheduleStartTime.AddMinutes(-15);
            var reminderEndTime = scheduleStartTime.AddMinutes(-14);

            _logger.LogInformation($"[Reminder] 日程 {schedule.Id}: {schedule.Title}, 开始={scheduleStartTime:HH:mm}, 提醒区间={reminderStartTime:HH:mm}-{reminderEndTime:HH:mm}, 当前={now:HH:mm}");

            // 3. 检查当前时间是否在 [开始-15分钟, 开始-14分钟] 区间内
            if (now >= reminderStartTime && now < reminderEndTime)
            {
                _logger.LogInformation($"[Reminder] 符合提醒时间区间: {schedule.Title}");

                // 4. 判断今天是否是生效日期
                if (!scheduleService.IsDayActive(schedule, now.Date))
                {
                    _logger.LogInformation($"[Reminder] 今天不是生效日期: {schedule.Title}");
                    continue;
                }

                // 5. 查询当天是否有未完成任务（pending 或不存在）
                var todayDay = await dbContext.ScheduleDays
                    .FirstOrDefaultAsync(sd => sd.ScheduleId == schedule.Id && 
                                             sd.DayDate.Date == now.Date);

                if (todayDay != null && todayDay.Status != "pending")
                {
                    _logger.LogInformation($"[Reminder] 当天任务已处理 ({todayDay.Status}): {schedule.Title}");
                    continue;
                }

                // 6. 检查消息表是否已有记录
                var reminderRecord = await dbContext.ScheduleReminders
                    .FirstOrDefaultAsync(sr => sr.ScheduleId == schedule.Id &&
                                             sr.ReminderDate.Date == now.Date);

                if (reminderRecord != null)
                {
                    _logger.LogInformation($"[Reminder] 提醒已发送过: {schedule.Title}");
                    continue;
                }

                // 7. 构造消息，包含日程详情和日子详情
                var message = new Services.ReminderMessage
                {
                    Type = "reminder",
                    ScheduleId = schedule.Id,
                    Title = schedule.Title,
                    Content = schedule.Content,
                    StartTime = scheduleStartTime,
                    EndTime = now.Date.Add(TimeSpan.Parse(schedule.EndTime)),
                    ReminderTime = reminderStartTime,
                    DayContent = todayDay?.Content,
                    DayStatus = todayDay?.Status ?? "pending"
                };

                // 8. 发送 SSE 消息
                await sseService.SendMessageToAll(message);

                // 9. 保存记录到消息表
                var newReminder = new Models.ScheduleReminder
                {
                    ScheduleId = schedule.Id,
                    ReminderDate = now.Date,
                    SentAt = DateTime.Now,
                    CreatedAt = DateTime.Now
                };
                dbContext.ScheduleReminders.Add(newReminder);
                await dbContext.SaveChangesAsync();

                _logger.LogInformation($"[Reminder] 提醒发送成功并记录: {schedule.Title}");
            }
            else
            {
                _logger.LogInformation($"[Reminder] 不在提醒时间区间: {schedule.Title}");
            }
        }
    }
}