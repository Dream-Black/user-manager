using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectHub.Api.Data;
using ProjectHub.Api.Models;

namespace ProjectHub.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ComputersController : ControllerBase
{
    private readonly AppDbContext _context;

    public ComputersController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// 获取当前电脑（自动识别）
    /// 根据请求头 X-Computer-Hostname 识别，不存在则自动创建
    /// </summary>
    [HttpGet("current")]
    public async Task<IActionResult> GetCurrentComputer()
    {
        var hostName = Request.Headers["X-Computer-Hostname"].FirstOrDefault();

        if (string.IsNullOrEmpty(hostName))
        {
            return BadRequest(new { success = false, error = new { code = "VALIDATION_ERROR", message = "X-Computer-Hostname 请求头必填" } });
        }

        var computer = await _context.Computers.FirstOrDefaultAsync(c => c.HostName == hostName);

        if (computer == null)
        {
            // 自动创建新电脑
            computer = new Computer
            {
                Name = hostName,
                HostName = hostName,
                IsOnline = true,
                LastHeartbeat = DateTime.Now,
                CreatedAt = DateTime.Now
            };

            _context.Computers.Add(computer);
            await _context.SaveChangesAsync();

            return Created($"/api/computers/{computer.Id}", computer);
        }

        // 更新心跳
        computer.IsOnline = true;
        computer.LastHeartbeat = DateTime.Now;
        await _context.SaveChangesAsync();

        return Ok(computer);
    }

    /// <summary>
    /// 获取所有电脑
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetComputers()
    {
        var computers = await _context.Computers
            .OrderByDescending(c => c.LastHeartbeat)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.HostName,
                c.IsOnline,
                c.LastHeartbeat,
                c.CreatedAt,
                ResourcePathCount = c.ResourcePaths.Count
            })
            .ToListAsync();

        return Ok(new { items = computers, total = computers.Count });
    }

    /// <summary>
    /// 获取单个电脑
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetComputer(int id)
    {
        var computer = await _context.Computers
            .Include(c => c.ResourcePaths)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (computer == null)
            return NotFound(new { success = false, error = new { code = "RESOURCE_NOT_FOUND", message = "电脑不存在" } });

        return Ok(computer);
    }

    /// <summary>
    /// 创建/更新电脑
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateOrUpdateComputer([FromBody] CreateComputerRequest request)
    {
        var hostName = Request.Headers["X-Computer-Hostname"].FirstOrDefault() ?? request.HostName ?? "";

        var computer = await _context.Computers.FirstOrDefaultAsync(c => c.HostName == hostName);

        if (computer == null)
        {
            computer = new Computer
            {
                Name = request.Name ?? hostName,
                HostName = hostName,
                IsOnline = true,
                LastHeartbeat = DateTime.Now,
                CreatedAt = DateTime.Now
            };
            _context.Computers.Add(computer);
        }
        else
        {
            if (!string.IsNullOrEmpty(request.Name))
                computer.Name = request.Name;
            computer.IsOnline = true;
            computer.LastHeartbeat = DateTime.Now;
        }

        await _context.SaveChangesAsync();
        return Ok(computer);
    }

    /// <summary>
    /// 更新电脑信息
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateComputer(int id, [FromBody] UpdateComputerRequest request)
    {
        var computer = await _context.Computers.FindAsync(id);
        if (computer == null)
            return NotFound(new { success = false, error = new { code = "RESOURCE_NOT_FOUND", message = "电脑不存在" } });

        if (!string.IsNullOrEmpty(request.Name))
            computer.Name = request.Name;

        await _context.SaveChangesAsync();
        return Ok(computer);
    }

    /// <summary>
    /// 发送心跳
    /// </summary>
    [HttpPost("{id}/heartbeat")]
    public async Task<IActionResult> SendHeartbeat(int id)
    {
        var computer = await _context.Computers.FindAsync(id);
        if (computer == null)
            return NotFound(new { success = false, error = new { code = "RESOURCE_NOT_FOUND", message = "电脑不存在" } });

        computer.IsOnline = true;
        computer.LastHeartbeat = DateTime.Now;
        await _context.SaveChangesAsync();

        return Ok(new { success = true, lastHeartbeat = computer.LastHeartbeat });
    }

    /// <summary>
    /// 删除电脑
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteComputer(int id)
    {
        var computer = await _context.Computers.FindAsync(id);
        if (computer == null)
            return NotFound(new { success = false, error = new { code = "RESOURCE_NOT_FOUND", message = "电脑不存在" } });

        _context.Computers.Remove(computer);
        await _context.SaveChangesAsync();

        return Ok(new { success = true, message = "删除成功" });
    }
}

public class CreateComputerRequest
{
    public string? Name { get; set; }
    public string? HostName { get; set; }
}

public class UpdateComputerRequest
{
    public string? Name { get; set; }
}
