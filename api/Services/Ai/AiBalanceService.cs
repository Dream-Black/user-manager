using System.Net.Http.Headers;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using ProjectHub.Api.Data;

namespace ProjectHub.Api.Services.Ai;

public class AiBalanceService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AppDbContext _context;
    private readonly ILogger<AiBalanceService> _logger;
    private const string DeepSeekBaseUrl = "https://api.deepseek.com";
    private static (decimal balance, DateTime fetchedAt)? _balanceCache;

    public AiBalanceService(IHttpClientFactory httpClientFactory, AppDbContext context, ILogger<AiBalanceService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _context = context;
        _logger = logger;
    }

    public async Task<object?> GetBalanceAsync()
    {
        var settings = await _context.UserSettings.FirstOrDefaultAsync();
        if (settings == null || string.IsNullOrEmpty(settings.DeepSeekApiKey))
        {
            return new { hasApiKey = false, isAvailable = false, message = "未配置 API Key" };
        }

        if (_balanceCache != null && (DateTime.UtcNow - _balanceCache.Value.fetchedAt).TotalSeconds < 10)
        {
            return new
            {
                hasApiKey = true,
                isAvailable = true,
                totalBalance = _balanceCache.Value.balance,
                cached = true
            };
        }

        try
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", settings.DeepSeekApiKey);

            var response = await client.GetAsync($"{DeepSeekBaseUrl}/user/balance");
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("获取 DeepSeek 余额失败: {StatusCode}", response.StatusCode);
                return new { hasApiKey = true, isAvailable = false, message = $"API 返回 {response.StatusCode}" };
            }

            var json = await response.Content.ReadAsStringAsync();
            var data = JsonNode.Parse(json);
            if (data == null)
                return new { hasApiKey = true, isAvailable = false, message = "无法解析余额数据" };

            var isAvailable = data["is_available"]?.GetValue<bool>() ?? false;
            var balanceInfos = data["balance_infos"]?.AsArray();
            decimal totalBalance = 0;

            if (balanceInfos != null)
            {
                foreach (var info in balanceInfos)
                {
                    var balanceStr = info?["total_balance"]?.GetValue<string>();
                    if (!string.IsNullOrWhiteSpace(balanceStr) && decimal.TryParse(balanceStr, out var parsedBalance))
                    {
                        totalBalance += parsedBalance;
                    }
                }
            }

            _balanceCache = (totalBalance, DateTime.UtcNow);

            return new
            {
                hasApiKey = true,
                isAvailable,
                totalBalance,
                balanceInfos = balanceInfos?.ToList(),
                cached = false
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取 DeepSeek 余额异常");
            return new { hasApiKey = true, isAvailable = false, message = $"查询异常: {ex.Message}" };
        }
    }
}