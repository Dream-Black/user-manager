using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectHub.Api.Data;
using ProjectHub.Api.Models;

namespace ProjectHub.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SettingsController : ControllerBase
{
    private readonly AppDbContext _context;

    public SettingsController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>获取用户设置</summary>
    [HttpGet]
    public async Task<IActionResult> GetSettings()
    {
        var settings = await _context.UserSettings.FirstOrDefaultAsync();
        if (settings == null)
        {
            settings = new UserSettings();
            _context.UserSettings.Add(settings);
            await _context.SaveChangesAsync();
        }

        // 不返回 API Key 的完整内容
        if (!string.IsNullOrEmpty(settings.DeepSeekApiKey))
        {
            settings.DeepSeekApiKey = MaskApiKey(settings.DeepSeekApiKey);
        }

        return Ok(settings);
    }

    /// <summary>更新用户设置</summary>
    [HttpPut]
    public async Task<IActionResult> UpdateSettings([FromBody] UpdateSettingsRequest request)
    {
        var settings = await _context.UserSettings.FirstOrDefaultAsync();
        if (settings == null)
        {
            settings = new UserSettings();
            _context.UserSettings.Add(settings);
        }

        if (request.DeepSeekApiKey != null && !request.DeepSeekApiKey.Contains("*"))
            settings.DeepSeekApiKey = request.DeepSeekApiKey;
        if (request.DeepSeekModel != null) settings.DeepSeekModel = request.DeepSeekModel;
        if (request.WorkStartTime.HasValue) settings.WorkStartTime = request.WorkStartTime.Value;
        if (request.WorkEndTime.HasValue) settings.WorkEndTime = request.WorkEndTime.Value;
        if (request.LunchBreakStart.HasValue) settings.LunchBreakStart = request.LunchBreakStart;
        if (request.LunchBreakEnd.HasValue) settings.LunchBreakEnd = request.LunchBreakEnd;
        if (request.CommuteHours.HasValue) settings.CommuteHours = request.CommuteHours.Value;
        if (request.CurrentJob != null) settings.CurrentJob = request.CurrentJob;
        if (request.CurrentCompany != null) settings.CurrentCompany = request.CurrentCompany;
        if (request.CurrentPlan != null) settings.CurrentPlan = request.CurrentPlan;
        if (request.ReminderTime.HasValue) settings.ReminderTime = request.ReminderTime.Value;
        if (request.ReminderEnabled.HasValue) settings.ReminderEnabled = request.ReminderEnabled.Value;
        if (request.ThemeMode != null) settings.ThemeMode = request.ThemeMode;

        settings.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync();

        return Ok(settings);
    }

    private string MaskApiKey(string apiKey)
    {
        if (apiKey.Length <= 8)
            return "****";
        return apiKey[..4] + "****" + apiKey[^4..];
    }
}

public class UpdateSettingsRequest
{
    public string? DeepSeekApiKey { get; set; }
    public string? DeepSeekModel { get; set; }
    public TimeSpan? WorkStartTime { get; set; }
    public TimeSpan? WorkEndTime { get; set; }
    public TimeSpan? LunchBreakStart { get; set; }
    public TimeSpan? LunchBreakEnd { get; set; }
    public decimal? CommuteHours { get; set; }
    public string? CurrentJob { get; set; }
    public string? CurrentCompany { get; set; }
    public string? CurrentPlan { get; set; }
    public TimeSpan? ReminderTime { get; set; }
    public bool? ReminderEnabled { get; set; }
    public string? ThemeMode { get; set; }
}
