using ProjectHub.Api.Data;
using ProjectHub.Api.Services;
using ProjectHub.Api.Filters;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// 配置 MySQL 数据库
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=mysql;Port=3306;Database=projecthub;User=root;Password=ProjectHub@2026!Secure#Pass;";
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// 注册 HttpClient 和 AI 服务
builder.Services.AddHttpClient();
builder.Services.AddScoped<AiService>();

// 配置控制器 + JSON选项
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// 配置 CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// 配置 Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "ProjectHub API",
        Version = "v1",
        Description = "项目管理与资源管理 API"
    });
});

var app = builder.Build();

// 全局异常处理 - 捕获Swagger生成错误
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERROR] {context.Request.Path}: {ex.Message}");
        Console.WriteLine($"[STACK] {ex.StackTrace}");
        throw;
    }
});

// 自动创建缺失的数据库表
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        // 检查并创建缺失的表
        var connection = db.Database.GetDbConnection();
        connection.Open();
        
        using var command = connection.CreateCommand();
        
        // ResourcePaths 表
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS ResourcePaths (
                Id INT AUTO_INCREMENT PRIMARY KEY,
                ComputerId INT NOT NULL,
                Type VARCHAR(50) NOT NULL,
                Path VARCHAR(1000) NOT NULL,
                IsEnabled BIT NOT NULL DEFAULT 1,
                CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY (ComputerId) REFERENCES Computers(Id) ON DELETE CASCADE
            )";
        command.ExecuteNonQuery();
        logger.LogInformation("✓ ResourcePaths 表检查完成");
        
        // Comics 表
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Comics (
                Id INT AUTO_INCREMENT PRIMARY KEY,
                ResourcePathId INT NOT NULL,
                FolderName VARCHAR(255) NOT NULL,
                DisplayName VARCHAR(255) NOT NULL,
                Type VARCHAR(50) NOT NULL DEFAULT 'manga',
                ThumbnailBase64 MEDIUMTEXT NULL,
                CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
                FOREIGN KEY (ResourcePathId) REFERENCES ResourcePaths(Id) ON DELETE CASCADE
            )";
        command.ExecuteNonQuery();
        logger.LogInformation("✓ Comics 表检查完成");
        
        // ComicChapters 表
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS ComicChapters (
                Id INT AUTO_INCREMENT PRIMARY KEY,
                ComicId INT NOT NULL,
                FolderName VARCHAR(255) NOT NULL,
                DisplayName VARCHAR(255) NOT NULL,
                SortOrder INT NOT NULL DEFAULT 0,
                CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY (ComicId) REFERENCES Comics(Id) ON DELETE CASCADE
            )";
        command.ExecuteNonQuery();
        logger.LogInformation("✓ ComicChapters 表检查完成");
        
        connection.Close();
        logger.LogInformation("✓ 数据库表结构检查完成");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "数据库表创建失败");
    }
}

app.UseSwagger(options =>
{
    options.PreSerializeFilters.Add((document, request) =>
    {
        // 捕获Swagger生成时的错误
        Console.WriteLine($"[Swagger] Generating OpenAPI document for: {request.Path}");
    });
});
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "ProjectHub API V1");
    c.RoutePrefix = "swagger";
});

// 根路径重定向到Swagger
app.MapGet("/", () => Results.Redirect("/swagger", permanent: true));

app.UseCors();
app.UseAuthorization();
app.MapControllers();

app.Run();
