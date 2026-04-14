using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectHub.Api.Data;
using ProjectHub.Api.Models;
using System.Text.RegularExpressions;

namespace ProjectHub.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ComicsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ComicsController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// 测试端点
    /// </summary>
    [HttpGet("test")]
    public IActionResult Test()
    {
        return Ok(new { message = "ComicsController is working" });
    }

    /// <summary>
    /// 测试POST端点
    /// </summary>
    [HttpPost("test-post")]
    public IActionResult TestPost([FromBody] ScanComicsRequest request)
    {
        return Ok(new { message = "POST works", resourcePathId = request.ResourcePathId });
    }

    /// <summary>
    /// 获取漫画列表
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetComics([FromQuery] int? computerId = null, [FromQuery] string? type = null, [FromQuery] string? search = null)
    {
        var query = _context.Comics.AsQueryable();

        if (computerId.HasValue)
        {
            query = query.Where(c => c.ResourcePath.ComputerId == computerId.Value);
        }

        if (!string.IsNullOrEmpty(type))
            query = query.Where(c => c.Type == type);

        if (!string.IsNullOrEmpty(search))
            query = query.Where(c => c.DisplayName.Contains(search) || c.FolderName.Contains(search));

        var comics = await query
            .Include(c => c.ResourcePath)
            .OrderByDescending(c => c.UpdatedAt ?? c.CreatedAt)
            .Select(c => new
            {
                c.Id,
                c.ResourcePathId,
                c.FolderName,
                c.DisplayName,
                c.Type,
                c.ThumbnailBase64,
                ChapterCount = c.Chapters.Count,
                c.CreatedAt,
                c.UpdatedAt
            })
            .ToListAsync();

        return Ok(new { items = comics, total = comics.Count });
    }

    /// <summary>
    /// 获取漫画详情
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetComic(int id)
    {
        var comic = await _context.Comics
            .Include(c => c.ResourcePath)
            .Include(c => c.Chapters.OrderBy(ch => ch.SortOrder))
            .FirstOrDefaultAsync(c => c.Id == id);

        if (comic == null)
            return NotFound(new { success = false, error = new { code = "RESOURCE_NOT_FOUND", message = "漫画不存在" } });

        return Ok(new
        {
            comic.Id,
            comic.ResourcePathId,
            comic.FolderName,
            comic.DisplayName,
            comic.Type,
            comic.ThumbnailBase64,
            comic.CreatedAt,
            comic.UpdatedAt,
            Chapters = comic.Chapters.Select(ch => new
            {
                ch.Id,
                ch.FolderName,
                ch.DisplayName,
                ch.SortOrder
            })
        });
    }

    /// <summary>
    /// 创建漫画
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateComic([FromBody] CreateComicRequest request)
    {
        var resourcePath = await _context.ResourcePaths.FindAsync(request.ResourcePathId);
        if (resourcePath == null)
            return NotFound(new { success = false, error = new { code = "RESOURCE_NOT_FOUND", message = "资源路径不存在" } });

        var comic = new Comic
        {
            ResourcePathId = request.ResourcePathId,
            FolderName = request.FolderName,
            DisplayName = string.IsNullOrEmpty(request.DisplayName) ? request.FolderName : request.DisplayName,
            Type = request.Type ?? "manga",
            CreatedAt = DateTime.Now
        };

        _context.Comics.Add(comic);
        await _context.SaveChangesAsync();

        return Created($"/api/comics/{comic.Id}", comic);
    }

    /// <summary>
    /// 更新漫画信息
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateComic(int id, [FromBody] UpdateComicRequest request)
    {
        var comic = await _context.Comics.FindAsync(id);
        if (comic == null)
            return NotFound(new { success = false, error = new { code = "RESOURCE_NOT_FOUND", message = "漫画不存在" } });

        if (!string.IsNullOrEmpty(request.DisplayName))
            comic.DisplayName = request.DisplayName;

        if (!string.IsNullOrEmpty(request.Type))
            comic.Type = request.Type;

        comic.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync();

        return Ok(comic);
    }

    /// <summary>
    /// 上传封面
    /// </summary>
    [HttpPost("{id}/thumbnail")]
    public async Task<IActionResult> UploadThumbnail(int id, [FromBody] UploadThumbnailRequest request)
    {
        var comic = await _context.Comics.FindAsync(id);
        if (comic == null)
            return NotFound(new { success = false, error = new { code = "RESOURCE_NOT_FOUND", message = "漫画不存在" } });

        if (string.IsNullOrEmpty(request.ThumbnailBase64))
            return BadRequest(new { success = false, error = new { code = "VALIDATION_ERROR", message = "封面数据不能为空" } });

        // 验证Base64格式
        if (!request.ThumbnailBase64.StartsWith("data:image/"))
            return BadRequest(new { success = false, error = new { code = "VALIDATION_ERROR", message = "封面必须是图片格式" } });

        // 简单验证大小（前端已压缩）
        if (request.ThumbnailBase64.Length > 300000)
            return BadRequest(new { success = false, error = new { code = "VALIDATION_ERROR", message = "封面过大，请重新压缩" } });

        comic.ThumbnailBase64 = request.ThumbnailBase64;
        comic.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync();

        return Ok(new { success = true, thumbnailBase64 = comic.ThumbnailBase64 });
    }

    /// <summary>
    /// 删除漫画（级联删除章节）
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteComic(int id)
    {
        var comic = await _context.Comics
            .Include(c => c.Chapters)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (comic == null)
            return NotFound(new { success = false, error = new { code = "RESOURCE_NOT_FOUND", message = "漫画不存在" } });

        _context.Comics.Remove(comic);
        await _context.SaveChangesAsync();

        return Ok(new { success = true, message = "删除成功" });
    }

    /// <summary>
    /// 扫描文件夹
    /// 读取资源路径下的文件夹结构，创建/更新漫画和章节记录
    /// </summary>
    [HttpPost("scan")]
    public async Task<IActionResult> ScanComics([FromBody] ScanComicsRequest request)
    {
        Console.WriteLine($"[Scan] 收到扫描请求: {System.Text.Json.JsonSerializer.Serialize(request)}");
        try
        {
        var resourcePath = await _context.ResourcePaths.FindAsync(request.ResourcePathId);
        Console.WriteLine($"[Scan] 找到资源路径: {resourcePath?.Path}");
        if (resourcePath == null)
            return NotFound(new { success = false, error = new { code = "RESOURCE_NOT_FOUND", message = "资源路径不存在" } });

        // 将路径转换为Windows格式
        var basePath = resourcePath.Path.Replace("/", "\\");
        Console.WriteLine($"[Scan] basePath: {basePath}");
        Console.WriteLine($"[Scan] Directory.Exists: {Directory.Exists(basePath)}");

        if (!Directory.Exists(basePath))
        {
            Console.WriteLine("[Scan] 路径不存在，返回400");
            return BadRequest(new { success = false, error = new { code = "VALIDATION_ERROR", message = "路径不存在或无法访问" } });
        }

        Console.WriteLine("[Scan] 开始扫描...");
        int comicsFound = 0;
        int chaptersFound = 0;
        var comicsList = new List<object>();

        // 获取一级目录作为漫画
        var comicFolders = Directory.GetDirectories(basePath)
            .Select(d => new
            {
                FolderName = Path.GetFileName(d),
                Chapters = Directory.GetDirectories(d)
                    .Select(ch => Path.GetFileName(ch))
                    .ToList()
            })
            .Where(f => f.Chapters.Any())
            .ToList();

        foreach (var comicFolder in comicFolders)
        {
            // 查找或创建漫画
            var comic = await _context.Comics
                .Include(c => c.Chapters)
                .FirstOrDefaultAsync(c => c.ResourcePathId == resourcePath.Id && c.FolderName == comicFolder.FolderName);

            if (comic == null)
            {
                comic = new Comic
                {
                    ResourcePathId = resourcePath.Id,
                    FolderName = comicFolder.FolderName,
                    DisplayName = comicFolder.FolderName,
                    Type = resourcePath.Type == "comic" ? "manga" : resourcePath.Type,
                    CreatedAt = DateTime.Now
                };
                _context.Comics.Add(comic);
                await _context.SaveChangesAsync();
                comicsFound++;
            }

            int sortOrder = 0;
            foreach (var chapterFolder in comicFolder.Chapters)
            {
                sortOrder++;

                // 检查章节是否已存在
                var existingChapter = comic.Chapters.FirstOrDefault(ch => ch.FolderName == chapterFolder);

                if (existingChapter == null)
                {
                    var chapter = new ComicChapter
                    {
                        ComicId = comic.Id,
                        FolderName = chapterFolder,
                        DisplayName = chapterFolder,
                        SortOrder = sortOrder,
                        CreatedAt = DateTime.Now
                    };
                    _context.ComicChapters.Add(chapter);
                    chaptersFound++;
                }
            }

            if (sortOrder > 0)
            {
                await _context.SaveChangesAsync();
            }

            // 获取更新后的章节列表
            var chapters = await _context.ComicChapters
                .Where(ch => ch.ComicId == comic.Id)
                .OrderBy(ch => ch.SortOrder)
                .Select(ch => new { ch.FolderName, ch.DisplayName, ch.SortOrder })
                .ToListAsync();

            comicsList.Add(new
            {
                comic.Id,
                comic.FolderName,
                comic.DisplayName,
                Chapters = chapters
            });
        }

        return Ok(new
        {
            success = true,
            comicsFound,
            chaptersFound,
            comics = comicsList
        });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { 
                success = false, 
                error = new { 
                    code = "SCAN_ERROR", 
                    message = ex.Message,
                    stackTrace = ex.StackTrace
                }
            });
        }
    }

    /// <summary>
    /// 批量扫描接口
    /// 删除旧数据后新增新数据（只存漫画，不存章节）
    /// </summary>
    [HttpPost("batch-scan")]
    public async Task<IActionResult> BatchScanComics([FromBody] BatchScanRequest request)
    {
        Console.WriteLine($"[BatchScan] 收到批量扫描请求, resourcePathId: {request.ResourcePathId}, comics count: {request.Comics?.Count ?? 0}");
        
        try
        {
            // 1. 获取资源路径
            var resourcePath = await _context.ResourcePaths.FindAsync(request.ResourcePathId);
            if (resourcePath == null)
                return NotFound(new { success = false, error = new { code = "RESOURCE_NOT_FOUND", message = "资源路径不存在" } });

            // 2. 删除旧数据（该路径下的所有漫画，章节会级联删除）
            var oldComics = await _context.Comics
                .Where(c => c.ResourcePathId == request.ResourcePathId)
                .ToListAsync();
            
            int deletedComics = oldComics.Count;
            
            _context.Comics.RemoveRange(oldComics);
            await _context.SaveChangesAsync();
            
            Console.WriteLine($"[BatchScan] 删除旧数据: {deletedComics} 漫画");

            // 3. 新增新数据（只存漫画）
            int newComics = 0;

            if (request.Comics != null && request.Comics.Any())
            {
                foreach (var comicData in request.Comics)
                {
                    var comic = new Comic
                    {
                        ResourcePathId = request.ResourcePathId,
                        FolderName = comicData.FolderName,
                        DisplayName = comicData.DisplayName ?? comicData.FolderName,
                        Type = comicData.Type ?? (resourcePath.Type == "comic" ? "manga" : resourcePath.Type),
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    };
                    _context.Comics.Add(comic);
                    newComics++;
                }
                await _context.SaveChangesAsync();
            }

            Console.WriteLine($"[BatchScan] 新增数据: {newComics} 漫画");

            return Ok(new
            {
                success = true,
                deleted = deletedComics,
                created = newComics
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[BatchScan] 错误: {ex.Message}");
            Console.WriteLine($"[BatchScan] Inner: {ex.InnerException?.Message}");
            Console.WriteLine($"[BatchScan] StackTrace: {ex.StackTrace}");
            return StatusCode(500, new
            {
                success = false,
                error = new
                {
                    code = "BATCH_SCAN_ERROR",
                    message = ex.Message,
                    innerMessage = ex.InnerException?.Message
                }
            });
        }
    }

    /// <summary>
    /// 获取漫画的章节列表
    /// </summary>
    [HttpGet("{id}/chapters")]
    public async Task<IActionResult> GetChapters(int id)
    {
        var comic = await _context.Comics.FindAsync(id);
        if (comic == null)
            return NotFound(new { success = false, error = new { code = "RESOURCE_NOT_FOUND", message = "漫画不存在" } });

        var chapters = await _context.ComicChapters
            .Where(ch => ch.ComicId == id)
            .OrderBy(ch => ch.SortOrder)
            .Select(ch => new
            {
                ch.Id,
                ch.ComicId,
                ch.FolderName,
                ch.DisplayName,
                ch.SortOrder,
                ch.CreatedAt
            })
            .ToListAsync();

        return Ok(new { items = chapters, total = chapters.Count });
    }
}

public class CreateComicRequest
{
    public int ResourcePathId { get; set; }
    public string FolderName { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? Type { get; set; }
}

public class UpdateComicRequest
{
    public string? DisplayName { get; set; }
    public string? Type { get; set; }
}

public class UploadThumbnailRequest
{
    public string ThumbnailBase64 { get; set; } = string.Empty;
}

public class ScanComicsRequest
{
    public int ResourcePathId { get; set; }
}

public class BatchScanRequest
{
    public int ResourcePathId { get; set; }
    public List<BatchScanComic>? Comics { get; set; }
}

public class BatchScanComic
{
    public string FolderName { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? Type { get; set; }
}
