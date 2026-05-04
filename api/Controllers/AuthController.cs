using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectHub.Api.Data;
using ProjectHub.Api.Models;
using ProjectHub.Api.Services;

namespace ProjectHub.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly AuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        AppDbContext context,
        AuthService authService,
        ILogger<AuthController> logger)
    {
        _context = context;
        _authService = authService;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { message = "邮箱和密码不能为空" });
        }

        _logger.LogInformation("Login attempt for email: {Email}", request.Email);

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null)
        {
            _logger.LogWarning("Login failed: user not found for email {Email}", request.Email);
            return Unauthorized(new { message = "邮箱或密码错误" });
        }

        if (string.IsNullOrWhiteSpace(user.Password))
        {
            _logger.LogWarning("Login failed: user password is empty for email {Email}, userId {UserId}", request.Email, user.Id);
            return Unauthorized(new { message = "邮箱或密码错误" });
        }

        string plainPassword;
        try
        {
            plainPassword = _authService.DecryptPassword(request.Password);
            _logger.LogInformation("Login decrypt success for {Email}, decrypted password length: {Length}", request.Email, plainPassword.Length);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Password decrypt failed for {Email}", request.Email);
            return BadRequest(new { message = "密码加密格式无效" });
        }

        var storedHash = user.Password.Trim();
        var incomingHash = _authService.HashPassword(plainPassword);
        var isMatch = string.Equals(incomingHash, storedHash, StringComparison.OrdinalIgnoreCase);

        _logger.LogInformation(
            "Password verify for {Email}, userId {UserId}, decryptedPasswordLength {Length}, incomingHash {IncomingHash}, storedHash {StoredHash}, match {Match}",
            request.Email,
            user.Id,
            plainPassword.Length,
            incomingHash,
            storedHash,
            isMatch);

        if (!isMatch)
        {
            return Unauthorized(new { message = "邮箱或密码错误" });
        }

        var token = _authService.GenerateToken(user);
        return Ok(new
        {
            token,
            expiresIn = 7200,
            user = new
            {
                user.Id,
                user.Name,
                user.Email,
                user.Phone,
                user.Department,
                user.Role,
                user.Avatar,
                user.Theme,
                user.Density
            }
        });
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { message = "邮箱和密码不能为空" });
        }

        if (await _context.Users.AnyAsync(u => u.Email == request.Email))
        {
            return Conflict(new { message = "邮箱已存在" });
        }

        string plainPassword;
        try
        {
            plainPassword = _authService.DecryptPassword(request.Password);
        }
        catch
        {
            return BadRequest(new { message = "密码加密格式无效" });
        }

        var user = new User
        {
            Name = string.IsNullOrWhiteSpace(request.Name) ? "用户" : request.Name.Trim(),
            Email = request.Email.Trim(),
            Password = _authService.HashPassword(plainPassword),
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var token = _authService.GenerateToken(user);
        return Ok(new { token, expiresIn = 7200 });
    }
}

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class RegisterRequest
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
