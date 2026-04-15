using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectHub.Api.Data;
using ProjectHub.Api.Models;

namespace ProjectHub.Api.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _context;

    public UsersController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// 获取当前用户信息
    /// </summary>
    [HttpGet("current")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var user = await _context.Users.FirstOrDefaultAsync();
        
        if (user == null)
        {
            // 如果不存在，创建默认用户
            user = new User { Id = 1, Name = "用户" };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }

        return Ok(new
        {
            user.Id,
            user.Name,
            user.Email,
            user.Phone,
            user.Department,
            user.Role,
            user.Avatar,
            user.CreatedAt,
            user.UpdatedAt
        });
    }

    /// <summary>
    /// 更新当前用户信息
    /// </summary>
    [HttpPut("current")]
    public async Task<IActionResult> UpdateCurrentUser([FromBody] UpdateUserRequest request)
    {
        var user = await _context.Users.FirstOrDefaultAsync();
        
        if (user == null)
        {
            user = new User { Id = 1 };
            _context.Users.Add(user);
        }

        // 更新字段
        if (!string.IsNullOrEmpty(request.Name))
            user.Name = request.Name;
        if (request.Email != null)
            user.Email = request.Email;
        if (request.Phone != null)
            user.Phone = request.Phone;
        if (request.Department != null)
            user.Department = request.Department;
        if (request.Role != null)
            user.Role = request.Role;

        user.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync();

        return Ok(new
        {
            user.Id,
            user.Name,
            user.Email,
            user.Phone,
            user.Department,
            user.Role,
            user.Avatar,
            user.CreatedAt,
            user.UpdatedAt
        });
    }

    /// <summary>
    /// 上传头像（Base64）
    /// </summary>
    [HttpPost("current/avatar")]
    public async Task<IActionResult> UploadAvatar([FromBody] UploadAvatarRequest request)
    {
        if (string.IsNullOrEmpty(request.Avatar))
        {
            return BadRequest(new { message = "头像数据不能为空" });
        }

        // 验证Base64格式
        if (!request.Avatar.StartsWith("data:image/"))
        {
            return BadRequest(new { message = "无效的图片格式" });
        }

        var user = await _context.Users.FirstOrDefaultAsync();
        
        if (user == null)
        {
            user = new User { Id = 1 };
            _context.Users.Add(user);
        }

        // 限制头像大小（约50KB）
        if (request.Avatar.Length > 50000)
        {
            return BadRequest(new { message = "图片太大，请重新裁剪" });
        }

        user.Avatar = request.Avatar;
        user.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync();

        return Ok(new { message = "头像上传成功", avatar = user.Avatar });
    }
}

public class UpdateUserRequest
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Department { get; set; }
    public string? Role { get; set; }
}

public class UploadAvatarRequest
{
    public string Avatar { get; set; } = string.Empty;
}
