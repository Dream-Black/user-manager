using Microsoft.EntityFrameworkCore;
using ProjectHub.Api.Data;
using ProjectHub.Api.Models;

namespace ProjectHub.Api.Services.Ai;

public class AiSettingsService
{
    private readonly AppDbContext _context;

    public AiSettingsService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<object> GetSettingsAsync()
    {
        var settings = await _context.UserSettings.FirstOrDefaultAsync();
        return new
        {
            deepSeekApiKey = settings?.DeepSeekApiKey != null ? "***" + (settings.DeepSeekApiKey.Length > 4 ? settings.DeepSeekApiKey[^4..] : "") : null,
            deepSeekModel = settings?.DeepSeekModel ?? "deepseek-chat",
            hasApiKey = !string.IsNullOrEmpty(settings?.DeepSeekApiKey)
        };
    }

    public async Task UpdateSettingsAsync(string? deepSeekApiKey, string? deepSeekModel)
    {
        var settings = await _context.UserSettings.FirstOrDefaultAsync();
        if (settings == null)
        {
            settings = new UserSettings { Id = 1, UpdatedAt = DateTime.Now };
            _context.UserSettings.Add(settings);
        }

        if (!string.IsNullOrEmpty(deepSeekApiKey))
            settings.DeepSeekApiKey = deepSeekApiKey;
        if (!string.IsNullOrEmpty(deepSeekModel))
            settings.DeepSeekModel = deepSeekModel;

        settings.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync();
    }
}