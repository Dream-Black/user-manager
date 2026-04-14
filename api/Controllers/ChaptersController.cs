using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectHub.Api.Data;
using ProjectHub.Api.Models;

namespace ProjectHub.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChaptersController : ControllerBase
{
    private readonly AppDbContext _context;
    private const string LocalProxyUrl = "http://localhost:6789";

    public ChaptersController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// 获取章节详情
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetChapter(int id)
    {
        var chapter = await _context.ComicChapters
            .Include(ch => ch.Comic)
                .ThenInclude(c => c!.ResourcePath)
            .FirstOrDefaultAsync(ch => ch.Id == id);

        if (chapter == null)
            return NotFound(new { success = false, error = new { code = "RESOURCE_NOT_FOUND", message = "章节不存在" } });

        return Ok(new
        {
            chapter.Id,
            chapter.ComicId,
            chapter.FolderName,
            chapter.DisplayName,
            chapter.SortOrder,
            chapter.CreatedAt,
            ComicName = chapter.Comic?.DisplayName
        });
    }

    /// <summary>
    /// 获取章节页面列表
    /// 返回图片URL，通过本地代理服务访问
    /// </summary>
    [HttpGet("{id}/pages")]
    public async Task<IActionResult> GetChapterPages(int id)
    {
        var chapter = await _context.ComicChapters
            .Include(ch => ch.Comic)
                .ThenInclude(c => c!.ResourcePath)
            .FirstOrDefaultAsync(ch => ch.Id == id);

        if (chapter == null)
            return NotFound(new { success = false, error = new { code = "RESOURCE_NOT_FOUND", message = "章节不存在" } });

        if (chapter.Comic?.ResourcePath == null)
            return NotFound(new { success = false, error = new { code = "RESOURCE_NOT_FOUND", message = "资源路径不存在" } });

        // 构建完整路径
        var basePath = chapter.Comic.ResourcePath.Path;
        var comicFolderName = chapter.Comic.FolderName;
        var chapterFolderName = chapter.FolderName;
        var fullPath = $"{basePath}{comicFolderName}/{chapterFolderName}";

        // 转换为Windows路径格式
        var windowsPath = fullPath.Replace("/", "\\");

        if (!Directory.Exists(windowsPath))
        {
            return Ok(new
            {
                chapterId = id,
                path = fullPath,
                pages = new List<object>(),
                message = "章节文件夹不存在"
            });
        }

        // 获取所有图片文件
        var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp" };
        var files = Directory.GetFiles(windowsPath)
            .Where(f => imageExtensions.Contains(Path.GetExtension(f).ToLower()))
            .OrderBy(f => f, new NaturalStringComparer())
            .Select((f, index) => new
            {
                index,
                filename = Path.GetFileName(f),
                // 通过本地代理服务访问文件
                url = $"{LocalProxyUrl}/files/read?path={Uri.EscapeDataString(f)}"
            })
            .ToList();

        return Ok(new
        {
            chapterId = id,
            comicId = chapter.ComicId,
            comicName = chapter.Comic?.DisplayName,
            chapterName = chapter.DisplayName,
            path = fullPath,
            pages = files,
            total = files.Count
        });
    }

    /// <summary>
    /// 更新章节信息
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateChapter(int id, [FromBody] UpdateChapterRequest request)
    {
        var chapter = await _context.ComicChapters.FindAsync(id);
        if (chapter == null)
            return NotFound(new { success = false, error = new { code = "RESOURCE_NOT_FOUND", message = "章节不存在" } });

        if (!string.IsNullOrEmpty(request.DisplayName))
            chapter.DisplayName = request.DisplayName;

        if (request.SortOrder.HasValue)
            chapter.SortOrder = request.SortOrder.Value;

        await _context.SaveChangesAsync();

        return Ok(chapter);
    }

    /// <summary>
    /// 删除章节
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteChapter(int id)
    {
        var chapter = await _context.ComicChapters.FindAsync(id);
        if (chapter == null)
            return NotFound(new { success = false, error = new { code = "RESOURCE_NOT_FOUND", message = "章节不存在" } });

        _context.ComicChapters.Remove(chapter);
        await _context.SaveChangesAsync();

        return Ok(new { success = true, message = "删除成功" });
    }
}

public class UpdateChapterRequest
{
    public string? DisplayName { get; set; }
    public int? SortOrder { get; set; }
}

/// <summary>
/// 自然排序比较器（按数字排序，如 "1", "2", "10" 而不是 "1", "10", "2"）
/// </summary>
public class NaturalStringComparer : IComparer<string>
{
    public int Compare(string? x, string? y)
    {
        if (x == null || y == null) return 0;

        int ix = 0, iy = 0;

        while (ix < x.Length && iy < y.Length)
        {
            if (char.IsDigit(x[ix]) && char.IsDigit(y[iy]))
            {
                // 提取数字
                int nx = 0, ny = 0;
                while (ix < x.Length && char.IsDigit(x[ix]))
                {
                    nx = nx * 10 + (x[ix] - '0');
                    ix++;
                }
                while (iy < y.Length && char.IsDigit(y[iy]))
                {
                    ny = ny * 10 + (y[iy] - '0');
                    iy++;
                }
                if (nx != ny) return nx.CompareTo(ny);
            }
            else
            {
                if (x[ix] != y[iy]) return x[ix].CompareTo(y[iy]);
                ix++;
                iy++;
            }
        }

        return x.Length.CompareTo(y.Length);
    }
}
