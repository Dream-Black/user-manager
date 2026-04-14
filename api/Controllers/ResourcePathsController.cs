using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectHub.Api.Data;
using ProjectHub.Api.Models;

namespace ProjectHub.Api.Controllers;

[ApiController]
[Route("api/resource-paths")]
public class ResourcePathsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<ResourcePathsController> _logger;

    public ResourcePathsController(AppDbContext context, ILogger<ResourcePathsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// 测试端点
    /// </summary>
    [HttpGet("test")]
    public IActionResult Test()
    {
        return Ok(new { message = "ResourcePaths API is working", timestamp = DateTime.Now });
    }

    /// <summary>
    /// 获取资源路径列表
    /// </summary>
    [HttpGet("list")]
    public IActionResult GetResourcePaths([FromQuery] int? computerId = null, [FromQuery] string? type = null)
    {
        try
        {
            var paths = _context.ResourcePaths.ToList();
            return Ok(new { items = paths, total = paths.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取资源路径列表失败");
            return StatusCode(500, new { error = ex.Message, inner = ex.InnerException?.Message });
        }
    }

    /// <summary>
    /// 获取单个资源路径
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetResourcePath(int id)
    {
        var path = await _context.ResourcePaths
            .Include(r => r.Computer)
            .Include(r => r.Comics)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (path == null)
            return NotFound(new { success = false, error = new { code = "RESOURCE_NOT_FOUND", message = "资源路径不存在" } });

        return Ok(path);
    }

    /// <summary>
    /// 创建资源路径
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateResourcePath([FromBody] CreateResourcePathRequest request)
    {
        // 验证路径格式
        if (string.IsNullOrEmpty(request.Path) || !request.Path.EndsWith("/"))
        {
            return BadRequest(new { success = false, error = new { code = "VALIDATION_ERROR", message = "路径必须以 / 结尾" } });
        }

        // 验证资源类型
        var validTypes = new[] { "comic", "video", "novel", "image" };
        if (!validTypes.Contains(request.Type))
        {
            return BadRequest(new { success = false, error = new { code = "VALIDATION_ERROR", message = "资源类型必须是: comic, video, novel, image" } });
        }

        // 验证电脑存在
        var computer = await _context.Computers.FindAsync(request.ComputerId);
        if (computer == null)
        {
            return NotFound(new { success = false, error = new { code = "RESOURCE_NOT_FOUND", message = "电脑不存在" } });
        }

        // 检查重复
        var existing = await _context.ResourcePaths
            .FirstOrDefaultAsync(r => r.ComputerId == request.ComputerId && r.Type == request.Type);

        if (existing != null)
        {
            return Conflict(new { success = false, error = new { code = "DUPLICATE_RESOURCE", message = "该电脑已存在此类型路径" } });
        }

        var resourcePath = new ResourcePath
        {
            ComputerId = request.ComputerId,
            Type = request.Type,
            Path = request.Path,
            IsEnabled = request.IsEnabled,
            CreatedAt = DateTime.Now
        };

        _context.ResourcePaths.Add(resourcePath);
        await _context.SaveChangesAsync();

        return Created($"/api/resource-paths/{resourcePath.Id}", resourcePath);
    }

    /// <summary>
    /// 更新资源路径
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateResourcePath(int id, [FromBody] UpdateResourcePathRequest request)
    {
        var resourcePath = await _context.ResourcePaths.FindAsync(id);
        if (resourcePath == null)
            return NotFound(new { success = false, error = new { code = "RESOURCE_NOT_FOUND", message = "资源路径不存在" } });

        if (!string.IsNullOrEmpty(request.Path))
        {
            if (!request.Path.EndsWith("/"))
                return BadRequest(new { success = false, error = new { code = "VALIDATION_ERROR", message = "路径必须以 / 结尾" } });
            resourcePath.Path = request.Path;
        }

        if (request.IsEnabled.HasValue)
            resourcePath.IsEnabled = request.IsEnabled.Value;

        await _context.SaveChangesAsync();
        return Ok(resourcePath);
    }

    /// <summary>
    /// 删除资源路径（级联删除关联的漫画和章节）
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteResourcePath(int id)
    {
        var resourcePath = await _context.ResourcePaths
            .Include(r => r.Comics)
                .ThenInclude(c => c.Chapters)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (resourcePath == null)
            return NotFound(new { success = false, error = new { code = "RESOURCE_NOT_FOUND", message = "资源路径不存在" } });

        // 级联删除已在数据库配置，此处只需删除ResourcePath
        _context.ResourcePaths.Remove(resourcePath);
        await _context.SaveChangesAsync();

        return Ok(new { success = true, message = "删除成功" });
    }
}

public class CreateResourcePathRequest
{
    public int ComputerId { get; set; }
    public string Type { get; set; } = "comic";
    public string Path { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
}

public class UpdateResourcePathRequest
{
    public string? Path { get; set; }
    public bool? IsEnabled { get; set; }
}
