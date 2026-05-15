# ProjectHub / AI Claw 项目架构文档

> **版本**: 1.0.0  
> **最后更新**: 2026-05-15  
> **项目定位**: 个人项目、任务、资源与 AI 辅助分析的工作台

---

## 目录

1. [项目概述](#1-项目概述)
2. [技术栈全景](#2-技术栈全景)
3. [系统上下文图（C4-Context）](#3-系统上下文图c4-context)
4. [容器图（C4-Container）](#4-容器图c4-container)
5. [后端架构](#5-后端架构)
6. [前端架构](#6-前端架构)
7. [本地代理服务](#7-本地代理服务)
8. [数据库模型](#8-数据库模型)
9. [模块目录](#9-模块目录)
10. [部署架构](#10-部署架构)
11. [关键架构决策（ADRs）](#11-关键架构决策adrs)
12. [数据流示例](#12-数据流示例)
13. [演进路线](#13-演进路线)

---

## 1. 项目概述

**ProjectHub / AI Claw** 是一个面向个人的工作管理综合平台，旨在将项目管理、任务追踪、AI 助手、资源管理、财务管理和日程安排整合到统一的系统中。

### 核心能力

| 模块 | 能力 |
|------|------|
| **项目管理** | 项目列表、详情、进度概览、甘特图 |
| **任务管理** | 创建/编辑/删除、分类、优先级、状态流转、延期、追加需求、子任务 |
| **AI 助手** | 基于项目与任务上下文的分析、问答、建议、工具调用 |
| **日程管理** | 日常安排、循环模式、提醒通知（SSE 推送） |
| **资源管理** | 电脑注册、资源路径、漫画浏览 |
| **笔记管理** | 笔记 CRUD、标签分类 |
| **财务管理** | 多账户管理、收支记录、工资模板、转账、余额快照、统计报表 |
| **本地代理** | 本地文件系统访问、漫画扫描、终端执行 |

### 设计原则

- **数据库结构自动同步** — 后端启动时检查表和列的完整性，自动建表/补列，无需手动迁移
- **日志驱动排障** — 前后端统一日志体系，优先通过日志定位问题
- **领域优先** — 模块按业务领域组织，前端按模块分层，后端按职责分层
- **持续演进** — 代码与文档以当前实现为准，增量开发优于整体重构

---

## 2. 技术栈全景

### 2.1 后端

| 技术 | 用途 | 版本 |
|------|------|------|
| ASP.NET Core | Web API 框架 | 9.0 |
| Entity Framework Core | ORM | 9.0 (MySQL) |
| MySQL | 关系型数据库 | 8.0 |
| JWT Bearer | 认证鉴权 | — |
| Swagger / OpenAPI | API 文档 | — |
| DeepSeek API | AI 对话服务 | — |

### 2.2 前端

| 技术 | 用途 | 版本 |
|------|------|------|
| Vue 3 | 前端框架 | ^3.5.32 |
| Vite | 构建工具 | ^8.0.4 |
| Pinia | 状态管理 | ^3.0.4 |
| Vue Router | 路由管理 | ^4.6.4 |
| TDesign Vue Next | UI 组件库 | ^1.19.0 |
| Axios | HTTP 客户端 | ^1.15.0 |
| ECharts | 数据可视化 | ^6.0.0 |
| Three.js | 3D 背景效果 | ^0.184.0 |
| dayjs | 日期处理 | — |

### 2.3 代理服务

| 技术 | 用途 | 版本 |
|------|------|------|
| Python FastAPI | 本地文件代理 | — |
| uvicorn | ASGI 服务器 | — |
| PyInstaller | 桌面端打包工具 | — |

### 2.4 基础设施

| 技术 | 用途 |
|------|------|
| Docker + Docker Compose | 容器化部署 |
| Nginx | 前端静态托管 + 反向代理 |
| GitHub Actions | CI/CD |
| GHCR (GitHub Container Registry) | Docker 镜像仓库 |
| Electron | 桌面端壳层（开发中） |

---

## 3. 系统上下文图（C4-Context）

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                          ProjectHub / AI Claw 系统                            │
│                                                                             │
│  ┌──────────────┐    HTTP/JSON    ┌──────────────┐    HTTP/JSON             │
│  │  用户（Web）  │ ──────────────► │  Vue 3 前端  │ ──────────────►          │
│  │  (浏览器)     │ ◄────────────── │  (Nginx)     │ ◄──────────────          │
│  └──────────────┘                 └──────────────┘           │              │
│                                                               │              │
│  ┌──────────────┐                  ┌──────────────┐           │              │
│  │  用户（桌面）  │ ──────────────► │ Electron 壳  │ ──────►   │              │
│  │  (Windows)   │ ◄────────────── │  (开发中)     │ ◄──────   │              │
│  └──────────────┘                 └──────────────┘           │              │
│                                                               ▼              │
│                                                    ┌──────────────────┐      │
│                                                    │  ASP.NET Core 9  │      │
│                                                    │  API 后端         │      │
│                                                    │  (端口 5000)     │      │
│                                                    └────────┬─────────┘      │
│                                                             │                │
│                    ┌─────────────────────────────────────────┤                │
│                    │                    │                    │                │
│                    ▼                    ▼                    ▼                │
│  ┌────────────────────┐  ┌────────────────────┐  ┌────────────────────┐     │
│  │     MySQL 8.0      │  │  Python FastAPI     │  │   DeepSeek API     │     │
│  │     (数据库)        │  │  本地代理服务        │  │   (AI 服务)        │     │
│  │  容器: mysql:3306   │  │  端口 6789          │  │   外部 API         │     │
│  └────────────────────┘  └────────────────────┘  └────────────────────┘     │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 外部依赖

- **DeepSeek API** — AI 对话、智能分析、自动分类
- **本地文件系统** — 通过 Python Proxy 桥接访问
- **浏览器 / Electron** — 前端运行环境

---

## 4. 容器图（C4-Container）

### 4.1 部署容器拓扑（Docker Compose）

```
┌──────────────────────────────────────────────────────────────────┐
│                        Docker Compose                             │
│                                                                   │
│  ┌──────────────────────┐    ┌─────────────────────────────────┐  │
│  │     Nginx (frontend)  │    │  ASP.NET Core 9 (api)          │  │
│  │  ┌────────────────┐   │    │  ┌──────────────────────────┐  │  │
│  │  │ Vue 3 SPA      │   │    │  │ Controllers (25个)       │  │  │
│  │  │ (dist/index.html│   │    │  │ Services (12个)          │  │  │
│  │  │ 静态文件)       │   │    │  │ AppDbContext             │  │  │
│  │  └────────────────┘   │    │  │ BackgroundServices (2个)  │  │  │
│  │  · 端口 80            │    │  └──────────────────────────┘  │  │
│  │  · SPA fallback       │    │  · 端口 5000 → 8080(容器内)   │  │
│  │  · /api → api:8080    │    │  · JWT 鉴权                  │  │  │
│  │  · Gzip 压缩          │    │  · Swagger UI                │  │  │
│  └──────────────────────┘    └──────────────┬──────────────────┘  │
│                                             │                      │
│                                             ▼                      │
│                              ┌──────────────────────────┐         │
│                              │     MySQL 8.0 (mysql)     │         │
│                              │  · 数据卷: mysql_data      │         │
│                              │  · 端口 3306              │         │
│                              │  · 健康检查: 10s间隔      │         │
│                              └──────────────────────────┘         │
│                                                                   │
│  网络: default (projecthub_default)                               │
│  外部网络: projecthub-network (prod)                              │
└──────────────────────────────────────────────────────────────────┘
```

### 4.2 前端架构分层

```
┌───────────────────────────────────────────────────────────────┐
│                       App.vue 根组件                           │
│           Sidebar ── Header ── <router-view> fade 动画         │
├───────────────────────────────────────────────────────────────┤
│                    Pinia 状态管理层 (5 stores)                   │
│  ┌──────────┐ ┌──────┐ ┌─────────┐ ┌──────────┐ ┌─────────┐  │
│  │ project  │ │ task │ │ finance │ │ settings │ │  theme  │  │
│  └──────────┘ └──────┘ └─────────┘ └──────────┘ └─────────┘  │
├───────────────────────────────────────────────────────────────┤
│                    Vue Router (23+ 路由)                        │
│  Dashboard  │ Projects  │ Tasks  │ Gantt  │ AI               │
│  Schedule   │ Notes     │ Res.   │ Settings │ Finance (9)     │
├───────────────────────────────────────────────────────────────┤
│                    Views 页面视图层 (9 模块)                     │
├───────────────────────────────────────────────────────────────┤
│                    Components 可复用组件层                       │
│  Layout (Header/Sidebar/Footer)                               │
│  Finance (AccountSelect/CategorySelect/...)                   │
│  Settings (AvatarCropper)                                     │
├───────────────────────────────────────────────────────────────┤
│   Composables (组合式函数层)                                    │
│   useLayoutState · useScheduleSSE                             │
├───────────────────────────────────────────────────────────────┤
│              Services / API 请求层                               │
│   axios instance → /api/*  (Vite代理 → localhost:5000)        │
│   authService · dataService · logger (远程日志)                │
└───────────────────────────────────────────────────────────────┘
```

### 4.3 后端架构分层

```
┌───────────────────────────────────────────────────────────────┐
│                  Controllers (API 层)                          │
│  Projects │ Tasks │ Auth │ AI │ Gantt │ Schedule              │
│  Notes │ Settings │ Users │ Computers │ ResourcePaths          │
│  Comics │ Chapters │ SubTasks │ Logs                           │
│  Finance(7)                                                   │
├───────────────────────────────────────────────────────────────┤
│                  Services (业务服务层)                          │
│  ┌─────────────────────────────────────────────────────┐      │
│  │  Ai/ 子目录:                                         │      │
│  │  AiConversationService · AiStreamService             │      │
│  │  AiDraftService · AiToolService · AiPromptBuilder    │      │
│  │  AiSettingsService · AiBalanceService                │      │
│  ├─────────────────────────────────────────────────────┤      │
│  │  AuthService · ScheduleService · SseService          │      │
│  │  FinanceAIService · FileLogService                   │      │
│  └─────────────────────────────────────────────────────┘      │
├───────────────────────────────────────────────────────────────┤
│                  Background Services (后台任务)                 │
│  ReminderBackgroundService (日程提醒)                          │
│  FinanceBackgroundService (AI自动分类 + 余额快照)              │
├───────────────────────────────────────────────────────────────┤
│                  Models / Data Layer                           │
│  AppDbContext (EF Core) + 31+ 实体模型                        │
│  Program.cs 启动时自动同步数据库结构                             │
├───────────────────────────────────────────────────────────────┤
│                  Middleware / Infrastructure                   │
│  JWT Auth · CORS · Kestrel配置 · Swagger                     │
│  Request Timeouts (30min) · 全局异常处理                      │
└───────────────────────────────────────────────────────────────┘
```

---

## 5. 后端架构

### 5.1 Controller 目录 (25个)

| 控制器 | 路由前缀 | 说明 |
|--------|---------|------|
| `AuthController` | `/api/auth` | 登录、注册、RSA 公钥 |
| `ProjectsController` | `/api/projects` | 项目 CRUD |
| `TasksController` | `/api/tasks` | 任务 CRUD、延期、追加需求 |
| `SubTasksController` | `/api/subtasks` | 子任务 CRUD |
| `GanttController` | `/api/gantt` | 甘特图数据 |
| `TimelinesController` | `/api/timelines` | 项目时间线 |
| `TaskTimelinesController` | `/api/task-timelines` | 任务变更日志 |
| `CategoriesController` | `/api/categories` | 任务分类 |
| `AiController` | `/api/ai` | AI 对话、分析、提醒 |
| `SchedulesController` | `/api/schedules` | 日程 CRUD、日子管理、SSE 流 |
| `NotesController` | `/api/notes` | 笔记 CRUD |
| `UsersController` | `/api/users` | 用户信息、头像 |
| `SettingsController` | `/api/settings` | 系统设置 |
| `LogsController` | `/api/logs` | 前端远程日志上报 |
| `ComputersController` | `/api/computers` | 电脑管理 |
| `ResourcePathsController` | `/api/resource-paths` | 资源路径 |
| `ComicsController` | `/api/comics` | 漫画 |
| `ChaptersController` | `/api/chapters` | 漫画章节 |
| `FinanceAccountsController` | `/api/financeaccounts` | 财务账户 |
| `FinanceExpensesController` | `/api/financeexpenses` | 支出 |
| `FinanceCategoriesController` | `/api/financecategories` | 支出分类 |
| `FinanceIncomesController` | `/api/financeincomes` | 收入 |
| `FinanceSalaryTemplatesController` | `/api/financesalarytemplates` | 工资模板 |
| `FinanceTransfersController` | `/api/financetransfers` | 转账 |
| `FinanceSnapshotsController` | `/api/financesnapshots` | 账户快照 |

### 5.2 核心服务

| 服务 | 生命周期 | 职责 |
|------|---------|------|
| `AiConversationService` | Scoped | 对话管理、消息历史 |
| `AiStreamService` | Scoped | 流式 AI 响应 |
| `AiDraftService` | Scoped | AI 执行草案生成 |
| `AiToolService` | Scoped | AI 工具调用（查项目/任务等） |
| `AiPromptBuilder` | Scoped | 提示词构建 |
| `AiSettingsService` | Scoped | AI 配置读取 |
| `AiBalanceService` | Scoped | DeepSeek 余额查询 |
| `AuthService` | Scoped | 用户认证、密码加密 |
| `ScheduleService` | Scoped | 日程生成逻辑 |
| `SseService` | Singleton | SSE 连接管理 |
| `FinanceAIService` | Scoped | 财务 AI 自动分类 |
| `FileLogService` | Scoped | 文件日志写入 |
| `ReminderBackgroundService` | Hosted | 日程提醒后台任务 |
| `FinanceBackgroundService` | Hosted | 财务定时任务（AI分类、余额快照） |

### 5.3 关键机制：数据库自动同步

数据库结构不在 EF Core Migration 中管理，而是在 `Program.cs` 启动时通过原始 SQL 自动同步：

```
应用启动
  │
  ├─ 打开数据库连接
  ├─ 创建 __EFMigrationsHistory 表（兼容 EF Core）
  ├─ 逐表检查 (INFORMATION_SCHEMA.TABLES)
  │   ├─ 表不存在 → CREATE TABLE ... ENGINE=InnoDB CHARSET=utf8mb4
  │   └─ 表存在 → 检查列 (INFORMATION_SCHEMA.COLUMNS)
  │                  └─ 列缺失 → ALTER TABLE ADD COLUMN
  ├─ 插入种子数据（默认用户、分类等）
  └─ 完成（共 31+ 个表已检查/创建）
```

**约束**：新增模型或字段时，必须同步更新 `Program.cs` 中的建表/补列逻辑。

### 5.4 JWT 认证流程

```
客户端请求
  │
  ├─ POST /api/auth/login (用户名+密码)
  │   └─ 返回 JWT Token (包含签发者、受众、过期时间)
  │
  ├─ 后续请求带 Authorization: Bearer <token>
  │   └─ JWT Bearer 中间件验证
  │       ├─ ValidateIssuer = true
  │       ├─ ValidateAudience = true
  │       ├─ ValidateIssuerSigningKey = true
  │       ├─ ValidateLifetime = true
  │       └─ ClockSkew = TimeSpan.Zero
  │
  └─ 未登录访问需鉴权路由 → 重定向到 /login
```

### 5.5 CORS 与 Kestrel 配置

- **CORS**: AllowAnyOrigin / AllowAnyMethod / AllowAnyHeader
- **Kestrel**: 禁用请求体最小速率限制，KeepAlive 超时 30 分钟，专为 AI 流式接口优化
- **请求超时**: 默认 30 分钟，AI 接口不受限

---

## 6. 前端架构

### 6.1 目录结构

```
projecthub/
├── public/                  # 静态资源（favicon, icons）
├── src/
│   ├── main.js              # 应用入口 (Vue + Pinia + Router + TDesign)
│   ├── App.vue              # 根组件（Layout + Router View）
│   │
│   ├── api/                 # API 请求层
│   │   ├── index.js         # axios 实例 + 15+ API 模块
│   │   └── resources.ts     # 资源管理独立 API
│   │
│   ├── services/            # 业务服务封装
│   │   ├── authService.js   # 认证服务
│   │   ├── dataService.js   # 数据服务（旧版封装）
│   │   └── logger.js        # 远程日志上报
│   │
│   ├── stores/              # Pinia 状态管理
│   │   ├── project.js       # 项目状态
│   │   ├── task.js          # 任务状态
│   │   ├── finance.js       # 财务状态
│   │   ├── settings.js      # 设置状态
│   │   └── theme.js         # 主题切换
│   │
│   ├── router/              # 路由配置
│   │   └── index.js         # 23+ 路由定义 + 全局守卫
│   │
│   ├── views/               # 页面视图（9 个功能模块）
│   │   ├── Dashboard.vue
│   │   ├── LoginView.vue
│   │   ├── projects/
│   │   ├── tasks/
│   │   ├── gantt/
│   │   ├── daily/
│   │   ├── ai/  &  ai-refactor/
│   │   ├── notes/
│   │   ├── settings/
│   │   ├── resources/
│   │   └── finance/ (9 个页面)
│   │
│   ├── components/          # 可复用组件
│   │   ├── layout/          # Header, Sidebar, Footer
│   │   ├── finance/         # 7 个财务组件
│   │   └── settings/        # AvatarCropper
│   │
│   ├── composables/         # 组合式函数
│   │   ├── useLayoutState.js
│   │   └── useScheduleSSE.js
│   │
│   ├── styles/              # 样式体系
│   │   ├── variables.css
│   │   ├── design-system.css
│   │   ├── tdesign-overrides.css
│   │   └── animations.css
│   │
│   ├── utils/               # 工具函数
│   │   ├── imageCompress.ts
│   │   └── imageProcessor.js
│   │
│   └── assets/              # 静态资源
│
├── vite.config.js           # Vite 构建配置
├── nginx.conf               # Nginx 部署配置
├── Dockerfile               # 多阶段构建
└── package.json             # 依赖管理
```

### 6.2 路由表

| 路径 | 视图 | 说明 |
|------|------|------|
| `/login` | LoginView | 登录页（无布局） |
| `/` | Dashboard | 工作台首页 |
| `/projects` | ProjectList | 项目列表 |
| `/projects/:id` | ProjectDetail | 项目详情 |
| `/tasks` | TaskList | 任务管理 |
| `/gantt` | GanttView | 甘特图 |
| `/schedule` | ScheduleManagementView | 日程管理 |
| `/schedule/:id` | ScheduleDetailView | 日程详情 |
| `/ai` | AiView | AI 助手（新旧两版） |
| `/settings` | SettingsView | 个人设置 |
| `/notes` | NotesView | 笔记列表 |
| `/notes/:id` | NoteDetailView | 笔记详情 |
| `/resources` | ResourceList | 资源管理 |
| `/resources/comics/:id` | ComicReader | 漫画阅读 |
| `/finance-manager/*` (9条) | 财务套件 | 财务概览/支出/收入/工资/账户/报表等 |

### 6.3 路由守卫

```
router.beforeEach
  │
  ├─ 检查 localStorage 中的 token 和 tokenExpiresAt
  ├─ 未登录访问需鉴权路由 → 重定向 /login?redirect=...
  └─ 已登录访问 /login → 重定向 /
```

### 6.4 状态管理模式

```
用户操作 → View (Vue组件)
            │
            ▼
  ┌─────────────────┐
  │ Pinia Store      │  → API 请求层 → 后端接口
  │ (状态+动作)      │  ← 响应数据更新 state
  └─────────────────┘
            │
            ▼
      响应式绑定 (<template>)
            │
            ▼
        UI 更新
```

### 6.5 样式体系

- **设计风格**: 清新·简洁·留白·轻盈
- **品牌色**: `#4A90D9`（清新蓝）
- **组件库**: TDesign Vue Next (覆盖定制样式)
- **暗色模式**: 通过 `useThemeStore` 切换，持久化到 localStorage
- **CSS 变量**: 全局色彩、间距、圆角统一管理

---

## 7. 本地代理服务

### 7.1 架构位置

```
浏览器/Electron  ←→  Python FastAPI (端口 6789)  ←→  本地文件系统
```

### 7.2 核心 API

| 接口 | 方法 | 功能 |
|------|------|------|
| `/files/list?path=xxx` | GET | 列出目录（文件夹/文件/图片分类返回） |
| `/files/tree?path=xxx&depth=2` | GET | 递归获取多层级目录树 |
| `/files/read?path=xxx` | GET | 分块流式读取文件（1MB块，支持图片/文本/PDF） |
| `/comics/scan?path=xxx` | GET | 扫描漫画目录（漫画→章节→图片，自然排序） |
| `/config/paths` | GET/POST | 查询/添加允许访问的路径 |
| `/terminal/execute` | POST | 执行 shell 命令（60s 超时） |

### 7.3 安全机制

- **路径白名单**: `config.json` 中的 `allowed_paths` 控制访问范围
- 白名单为空时允许所有路径（开发模式）
- MIME 类型映射支持: jpg/png/gif/webp/bmp/svg/pdf/txt

### 7.4 桌面端集成

Electron 主进程会 spawn Python Proxy 子进程，前端通过 IPC Bridge 与其通信，绕过浏览器安全限制。

---

## 8. 数据库模型

### 8.1 实体关系总览

项目包含 **31+ 张数据表**，按业务领域分组：

```
┌─────────────────────────────────────────────────────────────────────┐
│                         ProjectHub 数据库                             │
│                                                                     │
│  ┌── 项目管理 ──────────────────────────────────────────────────┐   │
│  │  Projects (1) ──── Tasks (N) ──── SubTasks (N)               │   │
│  │       │                    │                                  │   │
│  │       ├── Timelines (N)   ├── TaskTimelines (N)               │   │
│  │       │                    ├── TaskDelays (N)                  │   │
│  │       │                    └── TaskExtraRequirements (N)       │   │
│  │       └── TaskCategories (参考)                                │   │
│  └──────────────────────────────────────────────────────────────┘   │
│                                                                     │
│  ┌── 用户与认证 ───────────────────────────────────────────────┐   │
│  │  Users (1) ──── UserSettings (1)                             │   │
│  └──────────────────────────────────────────────────────────────┘   │
│                                                                     │
│  ┌── AI 助手 ──────────────────────────────────────────────────┐   │
│  │  Conversations (1) ──── ChatMessages (N)                     │   │
│  └──────────────────────────────────────────────────────────────┘   │
│                                                                     │
│  ┌── 资源管理 ────────────────────────────────────────────────┐   │
│  │  Computers (1) ──── ResourcePaths (N) ──── Comics (N)        │   │
│  │                              │               │                │   │
│  │                              │               └── ComicChapters│   │
│  │                              └── 支持多种资源类型              │   │
│  └──────────────────────────────────────────────────────────────┘   │
│                                                                     │
│  ┌── 日程管理 ────────────────────────────────────────────────┐   │
│  │  Schedules (1) ──── ScheduleDays (N)                        │   │
│  │       └── ScheduleReminders (N)                              │   │
│  └──────────────────────────────────────────────────────────────┘   │
│                                                                     │
│  ┌── 笔记 ─────────────────────────────────────────────────────┐   │
│  │  Notes (1) ──── NoteTags (N)                                │   │
│  └──────────────────────────────────────────────────────────────┘   │
│                                                                     │
│  ┌── 财务管理 ────────────────────────────────────────────────┐   │
│  │  FinanceAccounts (1) ──── FinanceAccountTransfers (N)       │   │
│  │       ├── FinanceAccountSnapshots (N)                       │   │
│  │       ├── FinanceExpenses (N) ──── FinanceExpenseItems (N)  │   │
│  │       └── FinanceIncomes (N) ──── FinanceIncomeAccounts (N) │   │
│  │                                                              │   │
│  │  FinanceExpenseCategories (参考)                             │   │
│  │                                                              │   │
│  │  FinanceSalaryTemplates (1) ──── FinanceSalaryTemplateItems  │   │
│  │       └── FinanceSalaryDetails (N) ──── FinanceSalaryDetail..│   │
│  └──────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────┘
```

### 8.2 数据表清单

| 编号 | 表名 | 所属模块 | 说明 |
|------|------|---------|------|
| 1 | `Projects` | 项目管理 | 项目主表 |
| 2 | `Tasks` | 项目管理 | 任务主表 |
| 3 | `SubTasks` | 项目管理 | 子任务 |
| 4 | `TaskCategories` | 项目管理 | 任务分类（预设6种） |
| 5 | `Timelines` | 项目管理 | 项目时间线 |
| 6 | `TaskTimelines` | 项目管理 | 任务变更日志 |
| 7 | `TaskDelays` | 项目管理 | 任务延期记录 |
| 8 | `TaskExtraRequirements` | 项目管理 | 追加需求 |
| 9 | `Users` | 用户 | 用户表 |
| 10 | `UserSettings` | 用户 | 用户设置（AI配置/工作时间等） |
| 11 | `Conversations` | AI助手 | AI对话 |
| 12 | `ChatMessages` | AI助手 | 聊天消息（含推理内容/工具调用） |
| 13 | `Computers` | 资源管理 | 电脑设备 |
| 14 | `ResourcePaths` | 资源管理 | 资源路径 |
| 15 | `Comics` | 资源管理 | 漫画 |
| 16 | `ComicChapters` | 资源管理 | 漫画章节 |
| 17-19 | `Schedules`/`ScheduleDays`/`ScheduleReminders` | 日程 | 日程管理 |
| 20-21 | `Notes`/`NoteTags` | 笔记 | 笔记管理 |
| 22 | `FinanceAccounts` | 财务 | 财务账户 |
| 23 | `FinanceExpenses` | 财务 | 支出 |
| 24 | `FinanceExpenseItems` | 财务 | 支出明细 |
| 25 | `FinanceExpenseCategories` | 财务 | 支出分类（预设9种） |
| 26 | `FinanceIncomes` | 财务 | 收入 |
| 27 | `FinanceIncomeAccounts` | 财务 | 收入-账户关联 |
| 28-29 | `FinanceSalaryTemplates`/`Items` | 财务 | 工资模板 |
| 30-31 | `FinanceSalaryDetails`/`Items` | 财务 | 工资明细 |
| 32 | `FinanceAccountTransfers` | 财务 | 账户转账 |
| 33 | `FinanceAccountSnapshots` | 财务 | 账户余额快照 |

---

## 9. 模块目录

### 9.1 项目管理模块

**后端**: `ProjectsController`, `TasksController`, `SubTasksController`, `GanttController`, `TimelinesController`, `CategoriesController`

**前端**: 项目列表/详情、任务管理、甘特图

**核心流程**:
```
创建项目 → 添加任务 → 分配分类/优先级
  ├─ 任务延期（记录原因 + 新截止日期）
  ├─ 追加需求（记录描述）
  ├─ 子任务管理（完成状态跟踪）
  └─ 时间线记录（项目维度 + 任务维度）
```

### 9.2 AI 助手模块

**后端**: `AiController` + `Services/Ai/*` (7个服务)

**前端**: `/ai` → AiView（新旧两版，新版使用 composables 重构）

**核心能力**:
- 多轮对话（基于 Conversation + ChatMessage）
- 流式响应（Chat/SSE）
- 对话管理（创建/归档/置顶/删除）
- 工具调用（查项目、查任务、执行终端命令）
- DeepSeek API 集成（模型切换、余额查询）
- 分析功能（基于项目/任务上下文的智能分析）

**流式处理架构**:
```
用户输入 → AiController.Chat()
  → AiConversationService 加载对话历史
  → AiPromptBuilder 构建提示词（含项目/任务上下文）
  → AiStreamService 调用 DeepSeek API (流式)
  → 逐 chunk 返回给前端
  → 完成后保存到 ChatMessages
```

### 9.3 财务管理模块

**后端**: 7 个 Controller + `FinanceAIService` + `FinanceBackgroundService`

**前端**: 9 个视图页面 + 7 个可复用组件

**核心功能**:
- 多账户管理（现金/银行卡/支付宝/微信，颜色/图标自定义）
- 支出记录（简单模式 + 清单模式，含分类、账户关联）
- 收入记录（零散收入 + 项目关联）
- 工资管理（模板驱动：预设薪资项 → 按月录入明细）
- 账户转账
- 余额快照（每日自动记录，可视化曲线）
- AI 自动分类（每天 23:59 对未分类支出调用 DeepSeek 分类）

```
支出流程:
  用户记录支出 → 选择账户 → 选择分类
    ├─ 简单模式: 输入金额 + 用途
    ├─ 清单模式: 输入多个商品项（数量/单价/小计）
    └─ 自动更新账户余额

AI 自动分类（定时每天 23:59）:
  FinanceBackgroundService
    → 查询当天未分类支出
    → 调用 FinanceAIService (DeepSeek)
    → 智能匹配 FinanceExpenseCategories
    → 更新 CategoryId
```

### 9.4 日程管理模块

**后端**: `SchedulesController` + `ScheduleService` + `SseService` + `ReminderBackgroundService`

**前端**: 日程管理视图 + 详情视图

**核心功能**:
- 日程 CRUD（含重复模式：每日/每周/工作日）
- 日程日子管理（标记完成/跳过，含原因）
- 提醒通知（SSE 实时推送 + 后台定时检查）
- 桌面通知（通过 Electron localBridge）

### 9.5 资源管理模块

**后端**: `ComputersController`, `ResourcePathsController`, `ComicsController`, `ChaptersController`

**前端**: 资源管理页面 + 漫画阅读器

**核心功能**:
- 电脑设备注册
- 资源路径管理（按类型组织）
- 漫画浏览（目录扫描 → 章节 → 图片列表）
- 本地代理桥接文件系统访问

### 9.6 笔记管理模块

**后端**: `NotesController`

**前端**: 笔记列表 + 详情视图

**核心功能**: 笔记 CRUD、标签管理

### 9.7 桌面端（开发中）

**位置**: `desktop/`

**技术**: Electron + preload.js

**架构**:
```
Electron Main Process
  ├─ spawn .NET API 子进程
  ├─ spawn Python Proxy 子进程
  └─ BrowserWindow 加载 http://localhost:5000
```

---

## 10. 部署架构

### 10.1 生产环境拓扑

```
                        用户浏览器
                            │
                            ▼
                     ┌──────────────┐
                     │   Nginx 80   │
                     │  (frontend)  │
                     └──────┬───────┘
                            │
              ┌─────────────┴─────────────┐
              │                           │
              ▼                           ▼
     ┌────────────────┐         ┌─────────────────┐
     │  /api/* 代理    │         │  SPA 静态文件    │
     │  → api:8080    │         │  index.html + 资源│
     └────────┬───────┘         └─────────────────┘
              │
              ▼
     ┌────────────────┐
     │ ASP.NET Core   │
     │  API (端口8080) │
     └────────┬───────┘
              │
              ▼
     ┌────────────────┐
     │  MySQL 8.0     │
     │  端口 3306     │
     └────────────────┘
```

### 10.2 Docker Compose 配置

```yaml
services:
  mysql:
    image: mysql:8.0
    volumes: [mysql_data:/var/lib/mysql]
    healthcheck: { test: ..., interval: 10s }

  api:
    build: ./api
    environment:
      - ConnectionStrings__DefaultConnection=...
      - DeepSeek__ApiKey=...
    depends_on: [mysql]

  frontend:
    build: ./projecthub
    ports: ["80:80"]
    depends_on: [api]
```

### 10.3 CI/CD 流程

```
开发者 push → GitHub Actions
                  │
          ┌───────┴───────┐
          │               │
     main 分支        desktop-v* tag
          │               │
    deploy.yml        desktop.yml
          │               │
    ┌─────┴─────┐    Electron 构建
    │ 编译检查   │    (独立流程)
    │ Docker 构建│
    │ GHCR 推送  │
    │ SSH 部署   │
    └───────────┘
          │
          ▼
    腾讯云服务器
    docker compose pull + up -d
```

### 10.4 环境变量

| 变量 | 说明 | 默认值 |
|------|------|--------|
| `MYSQL_ROOT_PASSWORD` | 数据库密码 | — |
| `ConnectionStrings__DefaultConnection` | 完整连接串 | 内置默认值 |
| `DeepSeek__ApiKey` | DeepSeek API 密钥 | — |
| `Jwt__Secret` | JWT 签名密钥 | 开发默认密钥 |
| `Jwt__Issuer/Audience` | JWT 发行方 | ProjectHub |

---

## 11. 关键架构决策（ADRs）

### ADR-001: 采用代码内建表替代 EF Core Migration

**状态**: 已接受  
**上下文**: 项目早期需要快速迭代，数据库结构频繁变化  
**决策**: 在 `Program.cs` 启动时通过原始 SQL `CREATE TABLE IF NOT EXISTS` 和 `ALTER TABLE` 自动同步  
**后果**: 
- 容易: 快速建表、部署零人工干预
- 困难: 复杂迁移需手动编写 SQL，无回滚机制

### ADR-002: 后端 MVC + 直接注入 DbContext

**状态**: 已接受  
**上下文**: 中小规模个人项目，团队规模小  
**决策**: Controller 直接注入 `AppDbContext`，跨越专用 Service 层（AI 等复杂模块除外）  
**后果**: 
- 容易: 开发速度快，CRUD 操作透明
- 困难: 业务逻辑分散在 Controller 中，测试难度增加

### ADR-003: Electron 壳层实现桌面端

**状态**: 已接受（开发中）  
**上下文**: 需要 Windows 桌面端支持，但不修改现有 Web 端代码和 CI/CD  
**决策**: Electron 主进程管理 .NET API + Python Proxy 子进程，BrowserWindow 加载 localhost
**后果**: 
- 容易: Web 端代码零修改，CI/CD 零感知
- 困难: 安装包较大（~200MB），需要代码签名

### ADR-004: DeepSeek API 作为 AI 后端

**上下文**: 需要 AI 对话、智能分析能力  
**决策**: 集成 DeepSeek API，支持流式响应 + 工具调用  
**后果**:
- 容易: API 成熟，集成成本低
- 困难: 依赖外部服务，需管理 API Key 和配额

### ADR-005: SSE 实时推送日程提醒

**上下文**: 需要浏览器端的实时提醒通知  
**决策**: 使用 Server-Sent Events (SSE) 而非 WebSocket  
**后果**:
- 容易: 单向推送、浏览器原生支持、实现简单
- 困难: 不支持双向通信，不适合复杂实时场景

---

## 12. 数据流示例

### 12.1 创建任务数据流

```
用户在前端点击"新建任务"
  → TaskList.vue 触发 store.createTask(data)
  → useTaskStore 调用 taskApi.create(data)
  → Axios POST /api/tasks → TasksController.Create()
      ├─ 验证请求数据
      ├─ 创建 ProjectTask 实体
      ├─ 写入数据库 (AppDbContext.Tasks.Add())
      ├─ 记录时间线 (TaskTimelines)
      ├─ 记录日志 (_logger.LogInformation)
      └─ 返回 Created 201 + task 对象
  ← Axios 响应 → store 更新 tasks[]
  ← 视图响应式更新
```

### 12.2 AI 流式对话数据流

```
用户在 AI 页面输入消息
  → AiView 调用 aiApi.chat(message, conversationId)
  → Axios POST /api/ai/chat
  → AiController.Chat()
      ├─ AiConversationService 加载历史消息
      ├─ AiPromptBuilder 构建提示词
      ├─ AiStreamService 调用 DeepSeek API (流式)
      │   ├─ 逐行读取 SSE 响应
      │   ├─ 处理工具调用 (AiToolService)
      │   └─ 流式写入 HTTP Response
      └─ 完成后保存 ChatMessage
  ← 前端逐 chunk 渲染 MessageBubble
  ← 完成时保存完整消息
```

### 12.3 财务支出 + 余额更新数据流

```
用户记录一笔支出
  → ExpenseList → ExpenseForm → submit()
  → Axios POST /api/financeexpenses
  → FinanceExpensesController.Create()
      ├─ 创建 FinanceExpense
      ├─ 如果有明细 → 创建 FinanceExpenseItems
      ├─ 更新关联账户余额
      │   FinanceAccount.Balance -= Amount
      ├─ 记录日志
      └─ 返回 Created
  ← store 更新 expenses[]
  ← 余额变化自动反映在 Dashboard
```

### 12.4 资源管理（漫画浏览）数据流

```
用户打开漫画阅读
  → ComicReader.vue → load comic detail
  → Axios GET /api/comics/{id}
  → ComicsController.GetById()
      └─ 返回 Comic + Chapters 列表
  ← 渲染章节列表

用户点击章节
  → 前端通过 localBridge / 直接调用 Python Proxy
  → GET http://localhost:6789/files/read?path=xxx
  → Proxy 分块读取图片文件 (1MB chunks)
  ← 流式返回图片数据
  → 前端渲染图片
```

---

## 13. 演进路线

### 已完成

| 模块 | 状态 |
|------|------|
| 项目管理 CRUD | ✅ 完成 |
| 任务管理（含延期/追加需求） | ✅ 完成 |
| 子任务管理 | ✅ 完成 |
| 甘特图 | ✅ 完成 |
| 项目/任务时间线 | ✅ 完成 |
| AI 助手（对话、分析、工具调用） | ✅ 完成 |
| 日程管理 + SSE 提醒 | ✅ 完成 |
| 笔记 + 标签 | ✅ 完成 |
| 用户认证 + JWT | ✅ 完成 |
| 资源管理（电脑/路径/漫画） | ✅ 完成 |
| 财务管理（完整版） | ✅ v1.0 完成 |
| 远程日志上报 | ✅ 完成 |
| Dark/Light 主题 | ✅ 完成 |
| Docker 部署 + CI/CD | ✅ 完成 |

### 开发中 / 规划中

| 功能 | 状态 |
|------|------|
| 桌面端（Electron 壳层） | 🔄 开发中 |
| AI 智能体重构（独立子项目） | 📋 规划中 |
| 数据导出/报表增强 | 📋 规划中 |
| 离线模式（SQLite 本地缓存） | 📋 规划中 |

### 架构原则（持续演进）

1. **模块内聚** — 新增模块遵循现有模式：Controller + Models + Program.cs 建表 + 前端 views + api + stores
2. **数据库第一** — 任何模型变更必须同步更新 Program.cs 建表逻辑
3. **日志先行** — 关键业务流程必须记录日志
4. **增量迭代** — 复杂功能分 phase 实现，每 phase 可独立上线
5. **兼容优先** — 新增功能不破坏现有接口和数据库结构

---

## 附录

### A. 项目文件统计

| 目录 | 类型 | 文件数 |
|------|------|--------|
| `api/` | 后端 C# 源文件 | ~79 |
| `api/` | 后端 依赖/配置 | ~30 |
| `projecthub/src/` | 前端源文件 | ~80 |
| `projecthub/` | 前端配置 | ~10 |
| `proxy/` | 代理 Python 源文件 | ~5 |
| `desktop/` | 桌面端 | ~5 |
| `ai/` | 协作文档 | ~26 |
| **总计** | | **~230+** |

### B. 端口映射

| 服务 | 开发端口 | 容器端口 | 说明 |
|------|---------|---------|------|
| 前端 | 3000 | 80 | Vite Dev / Nginx |
| 后端 API | 5000 | 8080 | ASP.NET Core |
| MySQL | 3306 | 3306 | 数据库 |
| Python Proxy | 6789 | — | 本地代理（不部署） |
| Swagger UI | — | 5000/swagger | API 文档 |

### C. 关键文件索引

| 文件 | 说明 |
|------|------|
| `api/Program.cs` | 应用入口 + 数据库自动同步 (950+ 行) |
| `api/Data/AppDbContext.cs` | EF Core DbContext + 实体配置 |
| `api/Controllers/*` | 25 个 API 控制器 |
| `api/Services/Ai/*` | 7 个 AI 服务 |
| `projecthub/src/main.js` | 前端应用入口 |
| `projecthub/src/App.vue` | 根布局组件 |
| `projecthub/src/router/index.js` | 路由定义 + 守卫 |
| `projecthub/src/api/index.js` | API 请求封装 |
| `proxy/main.py` | 本地代理服务 |
| `docker-compose.yml` | 开发和部署编排 |
| `desktop/main.js` | Electron 主进程 |
| `ai/design/*` | 架构决策和设计文档 |

---

*本文档由 Software Architect Agent 基于项目代码和协作文档自动生成*
