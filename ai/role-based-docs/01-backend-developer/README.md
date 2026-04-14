# 后端开发指南

## 📋 角色信息
- **角色**：后端开发
- **核心职责**：API开发、数据库设计、数据模型实现
- **预计工作量**：8个任务，约6小时

---

## 🚀 快速开始

### 1. 查看数据模型设计
→ 阅读 `data-models.md` 了解完整的数据库结构

### 2. 查看API设计文档
→ 阅读 `api-design.md` 了解所有接口定义

### 3. 执行数据库迁移
→ 阅读 `database-migration.md` 了解迁移步骤

---

## 📁 后端代码位置
```
api/
├── Models/
│   ├── Computer.cs        # 电脑模型
│   ├── ResourcePath.cs    # 资源路径模型
│   ├── Comic.cs           # 漫画模型
│   └── ComicChapter.cs    # 漫画章节模型
├── Controllers/
│   ├── ComputersController.cs
│   ├── ResourcePathsController.cs
│   ├── ComicsController.cs
│   └── ChaptersController.cs
└── Data/
    └── AppDbContext.cs    # 数据库上下文
```

---

## ✅ 任务清单

| # | 任务 | 优先级 | 验收标准 |
|---|------|--------|----------|
| 1.1 | 创建数据模型 | P0 | 4个模型类创建完成，属性正确 |
| 1.2 | 实现ComputersController | P0 | CRUD + 心跳 + 当前电脑识别 |
| 1.3 | 实现ResourcePathsController | P0 | 资源路径CRUD |
| 1.4 | 实现ComicsController | P0 | 漫画CRUD + 扫描 + 封面上传 |
| 1.5 | 实现ChaptersController | P0 | 章节管理 + 页面获取 |
| 1.6 | 更新AppDbContext | P0 | DbSet配置正确 |
| 1.7 | 数据库迁移 | P0 | 迁移成功，4张表创建 |
| 1.8 | 编写前端API封装 | P1 | 提供给前端调用的API封装 |

---

## 🔧 开发注意事项

### 1. 电脑自动识别逻辑
- 通过请求头 `X-Computer-Hostname` 或 `X-Forwarded-For` 获取客户端主机名
- 首次访问时自动创建Computer记录
- 定时心跳更新 `LastHeartbeat` 字段

### 2. 封面色处理
- 前端将图片转为Base64后上传
- 后端直接存储Base64字符串
- 单张封面大小限制：200KB（前端压缩）

### 3. 路径格式
- 数据库存储Windows绝对路径：`D:/MyComics/`
- 路径分隔符统一使用 `/`
- 扫描时需要将路径转为操作系统格式

---

## 📦 依赖项
无新增依赖，使用现有EF Core + MySQL配置

---

## 🧪 自测清单
- [ ] Computer表增删改查
- [ ] ResourcePath表增删改查
- [ ] Comic表增删改查
- [ ] ComicChapter表增删改查
- [ ] 心跳接口正常更新LastHeartbeat
- [ ] 扫描接口返回正确的文件夹结构
- [ ] 封面上传接口正常存储Base64
