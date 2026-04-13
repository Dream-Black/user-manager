# ProjectHub - 项目管理平台

<p align="center">
  <img src="https://img.shields.io/badge/Vue-3.5+-4FC08D?style=flat&logo=vue.js&logoColor=white" />
  <img src="https://img.shields.io/badge/ASP.NET_Core-9.0-512BD4?style=flat&logo=.net&logoColor=white" />
  <img src="https://img.shields.io/badge/MySQL-8.0-4479A1?style=flat&logo=mysql&logoColor=white" />
  <img src="https://img.shields.io/badge/Docker-Compose-2496ED?style=flat&logo=docker&logoColor=white" />
</p>

> 一个功能完善的项目与任务管理平台，支持甘特图、AI 智能助手、多维度统计等功能。

---

## ✨ 功能特性

### 核心功能
- 📊 **项目概览** - 仪表盘展示项目进度、任务统计、近期动态
- 📋 **任务管理** - 创建、编辑、删除任务，支持优先级、截止日期、状态管理
- 📅 **甘特图** - 可视化项目时间线，支持拖拽调整任务时间
- 🤖 **AI 助手** - 集成 DeepSeek AI，智能分析项目数据
- 📈 **数据统计** - 多维度项目统计与可视化图表

### 页面模块
| 模块 | 说明 |
|------|------|
| 概览 | 项目仪表盘，展示关键指标 |
| 今日待办 | 当天需要处理的任务 |
| 高优先级 | 重要紧急任务集中展示 |
| 评审 | 待评审任务管理 |
| 知识库 | 项目文档与知识沉淀 |
| 统计 | 数据可视化分析 |
| 设置 | 系统配置与个性化 |

---

## 🏗️ 技术架构

```
user-manager/
├── api/                    # ASP.NET Core 9 后端 API
│   ├── Controllers/        # API 控制器
│   ├── Data/               # 数据库上下文
│   ├── Models/             # 数据模型
│   ├── Services/           # 业务服务（含 AI 服务）
│   └── Dockerfile          # API 容器配置
├── projecthub/             # Vue 3 + Vite 前端
│   ├── src/
│   │   ├── components/     # 公共组件
│   │   ├── views/          # 页面视图
│   │   ├── stores/         # Pinia 状态管理
│   │   └── router/         # Vue Router 路由
│   ├── Dockerfile          # 前端容器配置
│   └── nginx.conf          # Nginx 反向代理配置
├── scripts/                # 部署脚本
├── .github/workflows/      # GitHub Actions CI/CD
├── docker-compose.yml      # 开发环境编排
├── docker-compose.prod.yml # 生产环境编排
└── VERSION                 # 版本号文件
```

### 技术栈
- **前端**: Vue 3 + TypeScript + Vite + TDesign Vue Next
- **后端**: ASP.NET Core 9 + Entity Framework Core
- **数据库**: MySQL 8.0
- **部署**: Docker Compose + GitHub Actions
- **AI 集成**: DeepSeek API

---

## 🚀 快速开始

### 环境要求
- Docker 20.10+
- Docker Compose 2.0+
- Node.js 22+（本地开发）
- .NET 9 SDK（本地开发）

### 1. 克隆项目

```bash
git clone https://github.com/Dream-Black/user-manager.git
cd user-manager
```

### 2. 配置环境变量

```bash
# 复制示例配置
cp .env.example .env

# 编辑 .env 文件，设置数据库密码和 API Key
vim .env
```

`.env` 示例：
```env
MYSQL_ROOT_PASSWORD=your_secure_password
DEEPSEEK_API_KEY=sk-your-deepseek-api-key
```

### 3. 启动服务

```bash
# 使用 Docker Compose 启动所有服务
docker-compose up -d --build

# 查看服务状态
docker-compose ps

# 查看日志
docker-compose logs -f
```

### 4. 访问应用

- 🌐 **前端**: http://localhost:8081
- 🔌 **API**: http://localhost:5000
- 📚 **Swagger**: http://localhost:5000/swagger

---

## 🛠️ 本地开发

### 前端开发

```bash
cd projecthub
npm install
npm run dev
```

前端开发服务器运行在 http://localhost:3000

### 后端开发

```bash
cd api
dotnet restore
dotnet run
```

API 服务运行在 http://localhost:5000

---

## 🔄 CI/CD 部署

项目已配置完整的 GitHub Actions 自动化部署流程。

### 配置 Secrets

在 GitHub 仓库 Settings → Secrets and variables → Actions 中添加：

| Secret | 说明 |
|--------|------|
| `SERVER_HOST` | 服务器公网 IP |
| `SERVER_USER` | SSH 用户名 |
| `SERVER_SSH_KEY` | SSH 私钥 |

### 部署流程

1. 推送代码到 `main` 分支
2. GitHub Actions 自动：
   - ✅ 编译检查
   - ✅ 构建 Docker 镜像
   - ✅ 递增版本号（语义化版本）
   - ✅ SSH 部署到服务器

### 版本管理

项目采用语义化版本控制（SemVer），格式为 `MAJOR.MINOR.PATCH`：
- 每次提交自动递增 PATCH 版本号
- 版本信息展示在页面页脚

---

## 📁 项目结构详解

```
api/
├── Controllers/
│   ├── ProjectsController.cs    # 项目管理 API
│   ├── TasksController.cs       # 任务管理 API
│   ├── CategoriesController.cs  # 分类管理 API
│   └── AiController.cs          # AI 助手 API
├── Data/
│   └── AppDbContext.cs          # EF Core 数据库上下文
├── Models/
│   ├── Project.cs               # 项目实体
│   ├── ProjectTask.cs           # 任务实体
│   └── Category.cs              # 分类实体
└── Services/
    └── AiService.cs             # DeepSeek AI 服务

projecthub/src/
├── components/
│   ├── layout/
│   │   ├── Sidebar.vue          # 侧边栏导航
│   │   ├── Header.vue           # 顶部导航
│   │   └── Footer.vue           # 页脚（含版本信息）
│   └── common/                  # 公共组件
├── views/
│   ├── Dashboard.vue            # 概览页
│   ├── tasks/                   # 任务相关页面
│   ├── projects/                # 项目相关页面
│   ├── gantt/                   # 甘特图页面
│   ├── review/                  # 评审页面
│   ├── settings/                # 设置页面
│   └── ai/                      # AI 助手页面
├── stores/
│   └── taskStore.js             # 任务状态管理
└── router/
    └── index.js                 # 路由配置
```

---

## 🔧 常用命令

```bash
# 查看所有容器状态
docker-compose ps

# 查看服务日志
docker-compose logs -f api
docker-compose logs -f frontend
docker-compose logs -f mysql

# 重启服务
docker-compose restart api
docker-compose restart frontend

# 完全重建（代码有大改动时）
docker-compose down && docker-compose up -d --build

# 进入数据库
docker-compose exec mysql mysql -u root -p projecthub

# 备份数据库
docker-compose exec mysql mysqldump -u root -p projecthub > backup.sql
```

---

## 🐛 故障排查

| 问题 | 排查方式 |
|------|----------|
| 前端 502 错误 | `docker-compose logs frontend` 检查 Nginx 配置 |
| 数据库连接失败 | 检查 `.env` 密码配置，`docker-compose logs mysql` |
| CI/CD SSH 失败 | 检查 Secrets 配置，确认服务器防火墙放行 22 端口 |
| 端口冲突 | 修改 `docker-compose.yml` 中的端口映射 |

---

## 📝 更新日志

版本号遵循 [语义化版本](https://semver.org/lang/zh-CN/) 规范。

当前版本：`1.0.0`

详细更新记录请查看 [GitHub Releases](https://github.com/Dream-Black/user-manager/releases)。

---

## 🤝 贡献指南

1. Fork 本仓库
2. 创建特性分支 (`git checkout -b feature/AmazingFeature`)
3. 提交更改 (`git commit -m 'Add some AmazingFeature'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 创建 Pull Request

---

## 📄 许可证

[MIT](LICENSE) © Dream-Black

---

<p align="center">
  Made with ❤️ by ProjectHub Team
</p>
