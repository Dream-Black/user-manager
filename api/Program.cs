using ProjectHub.Api.Data;
using ProjectHub.Api.Services;
using ProjectHub.Api.Filters;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// 配置 Kestrel，取消聊天接口的超时限制
builder.Services.Configure<Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions>(options =>
{
    options.Limits.MinRequestBodyDataRate = null;
    options.Limits.MinResponseDataRate = null;
    options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(30);
    options.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(2);
});

// 配置请求超时，默认30分钟
builder.Services.AddRequestTimeouts(options =>
{
    options.DefaultPolicy = new Microsoft.AspNetCore.Http.Timeouts.RequestTimeoutPolicy
    {
        Timeout = TimeSpan.FromMinutes(30)
    };
});

var isDevelopment = builder.Environment.IsDevelopment();

// 配置 MySQL 数据库
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=mysql;Port=3306;Database=projecthub;User=root;Password=ProjectHub@2026!Secure#Pass;";
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// 注册 HttpClient 和 AI 服务，延长超时时间到 10 分钟
builder.Services.AddHttpClient("", client =>
{
    client.Timeout = TimeSpan.FromMinutes(10);
});
builder.Services.AddScoped<AiService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<IFileLogService, FileLogService>(); // 所有环境都需要

// 配置控制器 + JSON选项
builder.Services.AddControllers(options =>
{
    options.SuppressAsyncSuffixInActionNames = false;
    options.MaxModelBindingRecursionDepth = 32;
})
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

// 配置 JWT
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? "ProjectHub-Development-Secret-Key-2026-Long-Enough";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "ProjectHub";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "ProjectHub";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
        };
    });
builder.Services.AddAuthorization();

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

// 自动同步数据库结构（所有环境都执行）
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        var connection = db.Database.GetDbConnection();
        connection.Open();
        
        using var command = connection.CreateCommand();
        
        // 辅助函数：检查表是否存在
        bool TableExists(string tableName)
        {
            command.CommandText = $@"
                SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES 
                WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = '{tableName}'";
            return Convert.ToInt32(command.ExecuteScalar()) > 0;
        }
        
        // 辅助函数：检查列是否存在
        bool ColumnExists(string tableName, string columnName)
        {
            command.CommandText = $@"
                SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE TABLE_SCHEMA = DATABASE() 
                AND TABLE_NAME = '{tableName}' 
                AND COLUMN_NAME = '{columnName}'";
            return Convert.ToInt32(command.ExecuteScalar()) > 0;
        }
        
        // 1. 确保 __EFMigrationsHistory 表存在
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS __EFMigrationsHistory (
                MigrationId VARCHAR(150) NOT NULL PRIMARY KEY,
                ProductVersion VARCHAR(32) NOT NULL
            )";
        command.ExecuteNonQuery();
        
        // 2. 创建 Computers 表（如果不存在）
        if (!TableExists("Computers"))
        {
            command.CommandText = @"
                CREATE TABLE Computers (
                    Id INT AUTO_INCREMENT PRIMARY KEY,
                    Name VARCHAR(100) NOT NULL,
                    HostName VARCHAR(255) NOT NULL,
                    IsOnline TINYINT(1) NOT NULL DEFAULT 1,
                    LastHeartbeat DATETIME(6) NULL,
                    CreatedAt DATETIME(6) NOT NULL
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4";
            command.ExecuteNonQuery();
            logger.LogInformation("✓ Computers 表已创建");
        }
        
        // 3. 创建 Projects 表（如果不存在）
        if (!TableExists("Projects"))
        {
            command.CommandText = @"
                CREATE TABLE Projects (
                    Id INT AUTO_INCREMENT PRIMARY KEY,
                    Name VARCHAR(200) NOT NULL,
                    Description VARCHAR(1000) NULL,
                    Type VARCHAR(50) NOT NULL DEFAULT 'work',
                    Customer VARCHAR(200) NULL,
                    Status VARCHAR(50) NOT NULL DEFAULT 'active',
                    CreatedAt DATETIME(6) NOT NULL,
                    UpdatedAt DATETIME(6) NOT NULL,
                    CompletedAt DATETIME(6) NULL
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4";
            command.ExecuteNonQuery();
            logger.LogInformation("✓ Projects 表已创建");
        }
        
        // 4. 创建 TaskCategories 表（如果不存在）
        if (!TableExists("TaskCategories"))
        {
            command.CommandText = @"
                CREATE TABLE TaskCategories (
                    Id INT AUTO_INCREMENT PRIMARY KEY,
                    Name VARCHAR(50) NOT NULL,
                    Icon VARCHAR(100) NULL,
                    Color VARCHAR(20) NOT NULL DEFAULT '#4A90D9',
                    IsSystem TINYINT(1) NOT NULL,
                    SortOrder INT NOT NULL,
                    CreatedAt DATETIME(6) NOT NULL
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4";
            command.ExecuteNonQuery();
            
            // 插入默认分类
            command.CommandText = @"
                INSERT INTO TaskCategories (Id, Name, Icon, Color, IsSystem, SortOrder, CreatedAt) VALUES
                (1, '开发', 'code', '#4A90D9', 1, 1, NOW()),
                (2, '会议', 'users', '#F5C26B', 1, 2, NOW()),
                (3, '文档', 'file-text', '#67CBAB', 1, 3, NOW()),
                (4, '设计', 'pen-tool', '#A78BFA', 1, 4, NOW()),
                (5, '调试', 'bug', '#E8908A', 1, 5, NOW()),
                (6, 'BUG', 'alert-circle', '#E8908A', 1, 6, NOW())
                ON DUPLICATE KEY UPDATE Name=VALUES(Name)";
            command.ExecuteNonQuery();
            logger.LogInformation("✓ TaskCategories 表已创建并填充默认数据");
        }
        
        // 5. 创建 Users 表（如果不存在）
        if (!TableExists("Users"))
        {
            command.CommandText = @"
                CREATE TABLE Users (
                    Id INT AUTO_INCREMENT PRIMARY KEY,
                    Name VARCHAR(100) NOT NULL,
                    Email VARCHAR(200) NULL,
                    Password VARCHAR(512) NULL,
                    Phone VARCHAR(20) NULL,
                    Department VARCHAR(50) NULL,
                    Role VARCHAR(100) NULL,
                    Avatar LONGTEXT NULL,
                    Theme VARCHAR(20) NULL DEFAULT 'light',
                    Density VARCHAR(20) NULL DEFAULT 'normal',
                    CreatedAt DATETIME(6) NOT NULL,
                    UpdatedAt DATETIME(6) NOT NULL
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4";
            command.ExecuteNonQuery();
            
            // 插入默认用户
            command.CommandText = @"
                INSERT INTO Users (Id, Name, Password, CreatedAt, UpdatedAt) VALUES (1, '用户', '8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92', NOW(), NOW())
                ON DUPLICATE KEY UPDATE 
                    Name=VALUES(Name),
                    Password=CASE WHEN Password IS NULL OR Password = '' THEN VALUES(Password) ELSE Password END";
            command.ExecuteNonQuery();
            logger.LogInformation("✓ Users 表已创建并填充默认数据");
        }
        
        // 6. 创建 UserSettings 表（如果不存在）
        if (!TableExists("UserSettings"))
        {
            command.CommandText = @"
                CREATE TABLE UserSettings (
                    Id INT AUTO_INCREMENT PRIMARY KEY,
                    DeepSeekApiKey VARCHAR(500) NULL,
                    DeepSeekModel VARCHAR(100) NOT NULL DEFAULT 'deepseek-chat',
                    WorkStartTime TIME(6) NOT NULL DEFAULT '09:00:00',
                    WorkEndTime TIME(6) NOT NULL DEFAULT '18:00:00',
                    LunchBreakStart TIME(6) NULL,
                    LunchBreakEnd TIME(6) NULL,
                    CommuteHours DECIMAL(65,30) NOT NULL DEFAULT 1,
                    CurrentJob VARCHAR(200) NULL,
                    CurrentCompany VARCHAR(200) NULL,
                    CurrentPlan VARCHAR(500) NULL,
                    ReminderTime TIME(6) NOT NULL DEFAULT '08:30:00',
                    ReminderEnabled TINYINT(1) NOT NULL DEFAULT 1,
                    ThemeMode VARCHAR(20) NOT NULL DEFAULT 'light',
                    UpdatedAt DATETIME(6) NOT NULL
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4";
            command.ExecuteNonQuery();
            
            // 插入默认设置
            command.CommandText = @"
                INSERT INTO UserSettings (Id, DeepSeekModel, UpdatedAt) VALUES (1, 'deepseek-chat', NOW())
                ON DUPLICATE KEY UPDATE UpdatedAt=VALUES(UpdatedAt)";
            command.ExecuteNonQuery();
            logger.LogInformation("✓ UserSettings 表已创建并填充默认数据");
        }
        
        // 7. 创建 ResourcePaths 表（如果不存在）
        
        // 7. 创建 ResourcePaths 表（如果不存在）
        if (!TableExists("ResourcePaths"))
        {
            command.CommandText = @"
                CREATE TABLE ResourcePaths (
                    Id INT AUTO_INCREMENT PRIMARY KEY,
                    ComputerId INT NOT NULL,
                    Type VARCHAR(50) NOT NULL,
                    Path VARCHAR(1000) NOT NULL,
                    IsEnabled TINYINT(1) NOT NULL DEFAULT 1,
                    CreatedAt DATETIME(6) NOT NULL,
                    FOREIGN KEY (ComputerId) REFERENCES Computers(Id) ON DELETE CASCADE
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4";
            command.ExecuteNonQuery();
            logger.LogInformation("✓ ResourcePaths 表已创建");
        }
        
        // 8. 创建 Tasks 表（如果不存在）
        if (!TableExists("Tasks"))
        {
            command.CommandText = @"
                CREATE TABLE Tasks (
                    Id INT AUTO_INCREMENT PRIMARY KEY,
                    Title VARCHAR(200) NOT NULL,
                    Description VARCHAR(2000) NULL,
                    ProjectId INT NOT NULL,
                    Category VARCHAR(50) NOT NULL DEFAULT 'dev',
                    Priority VARCHAR(50) NOT NULL DEFAULT 'medium',
                    Status VARCHAR(50) NOT NULL DEFAULT 'todo',
                    EstimatedHours DECIMAL(65,30) NOT NULL DEFAULT 0,
                    Progress INT NOT NULL DEFAULT 0,
                    SortOrder INT NOT NULL DEFAULT 0,
                    PlanStartDate DATETIME(6) NULL,
                    PlanEndDate DATETIME(6) NULL,
                    ActualStartDate DATETIME(6) NULL,
                    ActualEndDate DATETIME(6) NULL,
                    CreatedAt DATETIME(6) NOT NULL,
                    UpdatedAt DATETIME(6) NOT NULL,
                    FOREIGN KEY (ProjectId) REFERENCES Projects(Id) ON DELETE CASCADE
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4";
            command.ExecuteNonQuery();
            logger.LogInformation("✓ Tasks 表已创建");
        }
        
        // 9. 创建 Timelines 表（如果不存在）
        if (!TableExists("Timelines"))
        {
            command.CommandText = @"
                CREATE TABLE Timelines (
                    Id INT AUTO_INCREMENT PRIMARY KEY,
                    ProjectId INT NOT NULL,
                    EventType VARCHAR(50) NOT NULL,
                    Title VARCHAR(200) NOT NULL,
                    Description VARCHAR(2000) NULL,
                    TaskId INT NULL,
                    OccurredAt DATETIME(6) NOT NULL,
                    Metadata LONGTEXT NULL,
                    FOREIGN KEY (ProjectId) REFERENCES Projects(Id) ON DELETE CASCADE
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4";
            command.ExecuteNonQuery();
            logger.LogInformation("✓ Timelines 表已创建");
        }
        
        // 10. 创建 Comics 表（如果不存在）
        if (!TableExists("Comics"))
        {
            command.CommandText = @"
                CREATE TABLE Comics (
                    Id INT AUTO_INCREMENT PRIMARY KEY,
                    ResourcePathId INT NOT NULL,
                    FolderName VARCHAR(255) NOT NULL,
                    DisplayName VARCHAR(255) NOT NULL,
                    Type VARCHAR(50) NOT NULL DEFAULT 'manga',
                    ThumbnailBase64 MEDIUMTEXT NULL,
                    CreatedAt DATETIME(6) NOT NULL,
                    UpdatedAt DATETIME(6) NULL,
                    FOREIGN KEY (ResourcePathId) REFERENCES ResourcePaths(Id) ON DELETE CASCADE
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4";
            command.ExecuteNonQuery();
            logger.LogInformation("✓ Comics 表已创建");
        }
        
        // 11. 创建 TaskDelays 表（如果不存在）
        if (!TableExists("TaskDelays"))
        {
            command.CommandText = @"
                CREATE TABLE TaskDelays (
                    Id INT AUTO_INCREMENT PRIMARY KEY,
                    TaskId INT NOT NULL,
                    Reason VARCHAR(1000) NOT NULL,
                    OriginalPlanEndDate DATETIME(6) NOT NULL,
                    NewPlanEndDate DATETIME(6) NOT NULL,
                    CreatedAt DATETIME(6) NOT NULL,
                    FOREIGN KEY (TaskId) REFERENCES Tasks(Id) ON DELETE CASCADE
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4";
            command.ExecuteNonQuery();
            logger.LogInformation("✓ TaskDelays 表已创建");
        }
        
        // 12. 创建 TaskExtraRequirements 表（如果不存在）
        if (!TableExists("TaskExtraRequirements"))
        {
            command.CommandText = @"
                CREATE TABLE TaskExtraRequirements (
                    Id INT AUTO_INCREMENT PRIMARY KEY,
                    TaskId INT NOT NULL,
                    Description VARCHAR(1000) NOT NULL,
                    CreatedAt DATETIME(6) NOT NULL,
                    FOREIGN KEY (TaskId) REFERENCES Tasks(Id) ON DELETE CASCADE
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4";
            command.ExecuteNonQuery();
            logger.LogInformation("✓ TaskExtraRequirements 表已创建");
        }
        
        // 13. 创建 TaskTimelines 表（如果不存在）
        if (!TableExists("TaskTimelines"))
        {
            command.CommandText = @"
                CREATE TABLE TaskTimelines (
                    Id INT AUTO_INCREMENT PRIMARY KEY,
                    TaskId INT NOT NULL,
                    ChangeType VARCHAR(50) NOT NULL,
                    Title VARCHAR(100) NOT NULL,
                    Details VARCHAR(500) NULL,
                    OldValue VARCHAR(200) NULL,
                    NewValue VARCHAR(200) NULL,
                    OccurredAt DATETIME(6) NOT NULL,
                    FOREIGN KEY (TaskId) REFERENCES Tasks(Id) ON DELETE CASCADE
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4";
            command.ExecuteNonQuery();
            logger.LogInformation("✓ TaskTimelines 表已创建");
        }
        
        // 14. 创建 ComicChapters 表（如果不存在）
        if (!TableExists("ComicChapters"))
        {
            command.CommandText = @"
                CREATE TABLE ComicChapters (
                    Id INT AUTO_INCREMENT PRIMARY KEY,
                    ComicId INT NOT NULL,
                    FolderName VARCHAR(255) NOT NULL,
                    DisplayName VARCHAR(255) NOT NULL,
                    SortOrder INT NOT NULL,
                    CreatedAt DATETIME(6) NOT NULL,
                    FOREIGN KEY (ComicId) REFERENCES Comics(Id) ON DELETE CASCADE
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4";
            command.ExecuteNonQuery();
            logger.LogInformation("✓ ComicChapters 表已创建");
        }
        
        // 15. 创建 SubTasks 表（如果不存在）
        if (!TableExists("SubTasks"))
        {
            command.CommandText = @"
                CREATE TABLE SubTasks (
                    Id INT AUTO_INCREMENT PRIMARY KEY,
                    ParentTaskId INT NOT NULL,
                    Title VARCHAR(200) NOT NULL,
                    Description VARCHAR(1000) NULL,
                    Category VARCHAR(50) NOT NULL DEFAULT 'dev',
                    EstimatedHours DECIMAL(65,30) NOT NULL DEFAULT 0,
                    IsCompleted TINYINT(1) NOT NULL DEFAULT 0,
                    SortOrder INT NOT NULL DEFAULT 0,
                    CreatedAt DATETIME(6) NOT NULL,
                    UpdatedAt DATETIME(6) NOT NULL,
                    FOREIGN KEY (ParentTaskId) REFERENCES Tasks(Id) ON DELETE CASCADE
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4";
            command.ExecuteNonQuery();
            logger.LogInformation("✓ SubTasks 表已创建");
        }
        else
        {
            if (!ColumnExists("SubTasks", "Category"))
            {
                command.CommandText = "ALTER TABLE SubTasks ADD COLUMN Category VARCHAR(50) NOT NULL DEFAULT 'dev'";
                command.ExecuteNonQuery();
                logger.LogInformation("✓ SubTasks.Category 列已添加");
            }
            if (!ColumnExists("SubTasks", "EstimatedHours"))
            {
                command.CommandText = "ALTER TABLE SubTasks ADD COLUMN EstimatedHours DECIMAL(65,30) NOT NULL DEFAULT 0";
                command.ExecuteNonQuery();
                logger.LogInformation("✓ SubTasks.EstimatedHours 列已添加");
            }
        }
        
        // 16. 创建 Conversations 表（如果不存在）
        if (!TableExists("Conversations"))
        {
            command.CommandText = @"
                CREATE TABLE Conversations (
                    Id INT AUTO_INCREMENT PRIMARY KEY,
                    Title VARCHAR(200) NOT NULL DEFAULT '新对话',
                    CreatedAt DATETIME(6) NOT NULL,
                    UpdatedAt DATETIME(6) NOT NULL,
                    IsArchived TINYINT(1) NOT NULL DEFAULT 0,
                    IsPinned TINYINT(1) NOT NULL DEFAULT 0,
                    MemorySummary LONGTEXT NULL
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4";
            command.ExecuteNonQuery();
            logger.LogInformation("✓ Conversations 表已创建");
        }
        else
        {
            if (!ColumnExists("Conversations", "IsArchived"))
            {
                command.CommandText = "ALTER TABLE Conversations ADD COLUMN IsArchived TINYINT(1) NOT NULL DEFAULT 0";
                command.ExecuteNonQuery();
                logger.LogInformation("✓ Conversations.IsArchived 列已添加");
            }
            if (!ColumnExists("Conversations", "IsPinned"))
            {
                command.CommandText = "ALTER TABLE Conversations ADD COLUMN IsPinned TINYINT(1) NOT NULL DEFAULT 0";
                command.ExecuteNonQuery();
                logger.LogInformation("✓ Conversations.IsPinned 列已添加");
            }
            if (!ColumnExists("Conversations", "MemorySummary"))
            {
                command.CommandText = "ALTER TABLE Conversations ADD COLUMN MemorySummary LONGTEXT NULL";
                command.ExecuteNonQuery();
                logger.LogInformation("✓ Conversations.MemorySummary 列已添加");
            }
        }
        
        // 17. 创建 ChatMessages 表（如果不存在）
        if (!TableExists("ChatMessages"))
        {
            command.CommandText = @"
                CREATE TABLE ChatMessages (
                    Id INT AUTO_INCREMENT PRIMARY KEY,
                    ConversationId INT NOT NULL,
                    Role VARCHAR(20) NOT NULL DEFAULT 'user',
                    Content LONGTEXT NULL,
                    ReasoningContent LONGTEXT NULL,
                    ToolCalls LONGTEXT NULL,
                    Attachments LONGTEXT NULL,
                    FilesJson LONGTEXT NULL,
                    CreatedAt DATETIME(6) NOT NULL,
                    FOREIGN KEY (ConversationId) REFERENCES Conversations(Id) ON DELETE CASCADE
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4";
            command.ExecuteNonQuery();
            logger.LogInformation("✓ ChatMessages 表已创建");
        }
        else
        {
            if (!ColumnExists("ChatMessages", "FilesJson"))
            {
                command.CommandText = "ALTER TABLE ChatMessages ADD COLUMN FilesJson LONGTEXT NULL AFTER Attachments";
                command.ExecuteNonQuery();
                logger.LogInformation("✓ ChatMessages.FilesJson 列已添加");
            }
        }
        
        // 18. 检查并添加 Users 表的新列（兼容旧代码）
        if (!ColumnExists("Users", "Theme"))
        {
            command.CommandText = "ALTER TABLE Users ADD COLUMN Theme VARCHAR(20) NULL DEFAULT 'light'";
            command.ExecuteNonQuery();
            logger.LogInformation("✓ Users.Theme 列已添加");
        }
        
        if (!ColumnExists("Users", "Density"))
        {
            command.CommandText = "ALTER TABLE Users ADD COLUMN Density VARCHAR(20) NULL DEFAULT 'normal'";
            command.ExecuteNonQuery();
            logger.LogInformation("✓ Users.Density 列已添加");
        }
        
        if (!ColumnExists("Users", "Password"))
        {
            command.CommandText = "ALTER TABLE Users ADD COLUMN Password VARCHAR(512) NULL AFTER Email";
            command.ExecuteNonQuery();
            logger.LogInformation("✓ Users.Password 列已添加");
        }

        command.CommandText = @"
            UPDATE Users
            SET Password = '8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92'
            WHERE (Password IS NULL OR Password = '')
              AND Id <> 1";
        var rowsUpdated = command.ExecuteNonQuery();
        if (rowsUpdated > 0)
        {
            logger.LogInformation("✓ Users.Password 空值已初始化为默认哈希，共 {Count} 条", rowsUpdated);
        }
        
        // 19. 记录已应用的迁移
        command.CommandText = @"
            INSERT IGNORE INTO __EFMigrationsHistory (MigrationId, ProductVersion)
            VALUES ('20260415165055_AddUserThemeAndDensity', '8.0.0')";
        command.ExecuteNonQuery();
        
        connection.Close();
        logger.LogInformation("✓ 数据库结构同步完成 - 共 17 个表已检查/创建");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "数据库结构同步失败");
    }
}

app.UseRequestTimeouts();

app.UseSwagger(options =>
{
    options.PreSerializeFilters.Add((document, request) =>
    {
        if (isDevelopment)
        {
            Console.WriteLine($"[Swagger] Generating OpenAPI document for: {request.Path}");
        }
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
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
