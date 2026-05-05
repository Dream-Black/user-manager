using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using ProjectHub.Api.Models;
using ProjectHub.Api.Services;

namespace ProjectHub.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SchedulesController : ControllerBase
{
    private readonly ScheduleService _scheduleService;
    private readonly SseService _sseService;

    public SchedulesController(ScheduleService scheduleService, SseService sseService)
    {
        _scheduleService = scheduleService;
        _sseService = sseService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllSchedules()
    {
        var schedules = await _scheduleService.GetAllSchedules();
        return Ok(schedules);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetSchedule(int id)
    {
        var schedule = await _scheduleService.GetScheduleById(id);
        if (schedule == null)
            return NotFound(new { message = "日程不存在" });
        return Ok(schedule);
    }

    [HttpPost]
    public async Task<IActionResult> CreateSchedule([FromBody] CreateScheduleRequest request)
    {
        var schedule = new Schedule
        {
            Title = request.Title,
            Content = request.Content,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            RepeatMode = request.RepeatMode,
            RepeatDays = request.RepeatDays,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            ReminderEnabled = request.ReminderEnabled
        };

        var created = await _scheduleService.CreateSchedule(schedule);
        return Created($"/api/schedules/{created.Id}", created);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateSchedule(int id, [FromBody] UpdateScheduleRequest request)
    {
        var schedule = await _scheduleService.GetScheduleById(id);
        if (schedule == null)
            return NotFound(new { message = "日程不存在" });

        schedule.Title = request.Title ?? schedule.Title;
        schedule.Content = request.Content;
        schedule.StartDate = request.StartDate ?? schedule.StartDate;
        schedule.EndDate = request.EndDate ?? schedule.EndDate;
        schedule.RepeatMode = request.RepeatMode ?? schedule.RepeatMode;
        schedule.RepeatDays = request.RepeatDays ?? schedule.RepeatDays;
        schedule.StartTime = request.StartTime ?? schedule.StartTime;
        schedule.EndTime = request.EndTime ?? schedule.EndTime;
        schedule.ReminderEnabled = request.ReminderEnabled ?? schedule.ReminderEnabled;

        var updated = await _scheduleService.UpdateSchedule(id, schedule);
        return Ok(updated);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSchedule(int id)
    {
        var success = await _scheduleService.DeleteSchedule(id);
        if (!success)
            return NotFound(new { message = "日程不存在" });
        return Ok(new { message = "删除成功" });
    }

    [HttpGet("{id}/days")]
    public async Task<IActionResult> GetScheduleDays(int id)
    {
        var days = await _scheduleService.GetScheduleDays(id);
        return Ok(days);
    }

    [HttpPost("{id}/days")]
    public async Task<IActionResult> UpsertScheduleDay(int id, [FromBody] UpsertScheduleDayRequest request)
    {
        var day = await _scheduleService.UpsertScheduleDay(
            id,
            request.DayDate,
            request.Content,
            request.Status,
            request.SkipReason
        );
        return Ok(day);
    }

    [HttpPut("{id}/days/{dayId}")]
    public async Task<IActionResult> UpdateScheduleDay(int id, int dayId, [FromBody] UpdateScheduleDayRequest request)
    {
        var day = await _scheduleService.GetScheduleDay(id, request.DayDate);
        if (day == null)
            return NotFound(new { message = "日程日期不存在" });

        if (request.Content != null) day.Content = request.Content;
        if (request.Status != null) 
        {
            day.Status = request.Status;
            if (request.Status == "completed")
                day.CompletedAt = DateTime.Now;
            else if (request.Status == "skipped")
            {
                day.SkippedAt = DateTime.Now;
                day.SkipReason = request.SkipReason;
            }
        }
        day.UpdatedAt = DateTime.Now;

        await _scheduleService.UpsertScheduleDay(id, day.DayDate, day.Content, day.Status, day.SkipReason);
        return Ok(day);
    }

    [HttpDelete("{id}/days/{dayId}")]
    public async Task<IActionResult> DeleteScheduleDay(int id, int dayId)
    {
        var success = await _scheduleService.DeleteScheduleDay(dayId);
        if (!success)
            return NotFound(new { message = "日程日期不存在" });
        return Ok(new { message = "删除成功" });
    }

    [HttpPost("{id}/generate-days")]
    public async Task<IActionResult> GenerateScheduleDays(int id, [FromBody] GenerateScheduleDaysRequest request)
    {
        try
        {
            var days = await _scheduleService.GenerateAndSaveScheduleDays(
                id,
                request.StartDate,
                request.EndDate,
                request.RepeatMode,
                request.RepeatDays
            );
            return Ok(days);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}/days/{dayDate}/content")]
    public async Task<IActionResult> UpdateScheduleDayContent(int id, string dayDate, [FromBody] UpdateScheduleDayContentRequest request)
    {
        if (!DateTime.TryParse(dayDate, out var date))
            return BadRequest(new { message = "日期格式不正确" });
        
        var day = await _scheduleService.UpdateScheduleDayContent(id, date, request.Content);
        return Ok(day);
    }

    [HttpPut("{id}/days/{dayDate}/status")]
    public async Task<IActionResult> UpdateScheduleDayStatus(int id, string dayDate, [FromBody] UpdateScheduleDayStatusRequest request)
    {
        if (!DateTime.TryParse(dayDate, out var date))
            return BadRequest(new { message = "日期格式不正确" });
        
        var day = await _scheduleService.UpdateScheduleDayStatus(id, date, request.Status, request.SkipReason);
        return Ok(day);
    }

    [HttpGet("stream")]
    public async Task GetStream()
    {
        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");

        var clientId = Guid.NewGuid().ToString();
        var writer = new StreamWriter(Response.Body);

        _sseService.AddConnection(clientId, writer);

        Console.WriteLine($"[SSE] Client connected: {clientId}");

        var welcomeMessage = new ReminderMessage
        {
            Type = "connected",
            Title = "SSE Connected",
            Content = $"Client {clientId} connected successfully"
        };
        var welcomeJson = System.Text.Json.JsonSerializer.Serialize(welcomeMessage);
        await writer.WriteLineAsync($"data: {welcomeJson}\n\n");
        await writer.FlushAsync();

        try
        {
            while (!HttpContext.RequestAborted.IsCancellationRequested)
            {
                await Task.Delay(1000);
            }
        }
        finally
        {
            _sseService.RemoveConnection(clientId);
            Console.WriteLine($"[SSE] Client disconnected: {clientId}");
        }
    }
}

public class CreateScheduleRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Content { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string RepeatMode { get; set; } = "week";
    public string RepeatDays { get; set; } = "[]";
    public string StartTime { get; set; } = "09:00";
    public string EndTime { get; set; } = "10:00";
    public bool ReminderEnabled { get; set; } = false;
}

public class UpdateScheduleRequest
{
    public string? Title { get; set; }
    public string? Content { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? RepeatMode { get; set; }
    public string? RepeatDays { get; set; }
    public string? StartTime { get; set; }
    public string? EndTime { get; set; }
    public bool? ReminderEnabled { get; set; }
}

public class UpsertScheduleDayRequest
{
    public DateTime DayDate { get; set; }
    public string? Content { get; set; }
    public string? Status { get; set; }
    public string? SkipReason { get; set; }
}

public class UpdateScheduleDayRequest
{
    public DateTime DayDate { get; set; }
    public string? Content { get; set; }
    public string? Status { get; set; }
    public string? SkipReason { get; set; }
}

public class GenerateScheduleDaysRequest
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string RepeatMode { get; set; } = "week";
    public string RepeatDays { get; set; } = "[]";
}

public class UpdateScheduleDayContentRequest
{
    public string? Content { get; set; }
}

public class UpdateScheduleDayStatusRequest
{
    public string Status { get; set; } = "pending";
    public string? SkipReason { get; set; }
}