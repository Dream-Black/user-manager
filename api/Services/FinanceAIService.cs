using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ProjectHub.Api.Data;
using ProjectHub.Api.Models;

namespace ProjectHub.Api.Services;

/// <summary>财务AI服务 - 支出分类等AI能力</summary>
public class FinanceAIService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AppDbContext _context;
    private readonly ILogger<FinanceAIService> _logger;
    private const string DeepSeekBaseUrl = "https://api.deepseek.com";

    public FinanceAIService(IHttpClientFactory httpClientFactory, AppDbContext context, ILogger<FinanceAIService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _context = context;
        _logger = logger;
    }

    /// <summary>调用DeepSeek对支出进行自动分类</summary>
    /// <param name="purpose">支出用途</param>
    /// <param name="itemsText">清单子项文本（可选）</param>
    /// <returns>分类名称字符串</returns>
    public async Task<string?> ClassifyExpenseAsync(string purpose, string? itemsText)
    {
        try
        {
            // 从UserSettings读取API Key和Model
            var settings = await _context.UserSettings.FirstOrDefaultAsync();
            if (settings == null || string.IsNullOrEmpty(settings.DeepSeekApiKey))
            {
                _logger.LogWarning("[FinanceAI] 未配置DeepSeek API Key，跳过分类");
                return null;
            }

            // 获取所有现有分类
            var categories = await _context.FinanceExpenseCategories
                .OrderBy(c => c.SortOrder)
                .Select(c => c.Name)
                .ToListAsync();

            var categoryList = string.Join("、", categories);

            // 构建Prompt
            var userContent = $"请对以下支出进行分类：\n用途：{purpose}";
            if (!string.IsNullOrEmpty(itemsText))
                userContent += $"\n清单：{itemsText}";

            userContent += $"\n\n可选分类：{categoryList}\n如果都不匹配，请创建一个最合适的分类名。只返回分类名称，不要其他内容。";

            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(60);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", settings.DeepSeekApiKey);

            var requestBody = new Dictionary<string, object>
            {
                ["model"] = settings.DeepSeekModel ?? "deepseek-chat",
                ["messages"] = new[]
                {
                    new { role = "system", content = "你是一个财务支出分类助手。用户会告诉你支出的用途，你需要从给定的分类列表中选择最匹配的分类。如果都不匹配，你可以创建一个新的分类名。你只需要返回分类名称，不要返回任何其他内容。" },
                    new { role = "user", content = userContent }
                },
                ["temperature"] = 0.3,
                ["max_tokens"] = 20
            };

            var json = JsonSerializer.Serialize(requestBody);
            var request = new HttpRequestMessage(HttpMethod.Post, $"{DeepSeekBaseUrl}/chat/completions")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            var response = await client.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("[FinanceAI] DeepSeek API 请求失败: {StatusCode} - {Error}", response.StatusCode, error);
                return null;
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            var result = JsonDocument.Parse(responseBody);
            var content = result.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            // 清理返回值
            if (!string.IsNullOrEmpty(content))
            {
                content = content.Trim().Trim('"').Trim('\'');
                _logger.LogInformation("[FinanceAI] 分类结果: {Purpose} -> {Category}", purpose, content);
            }

            return content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[FinanceAI] 分类异常");
            return null;
        }
    }
}
