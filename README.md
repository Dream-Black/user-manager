# ProjectHub / AI Claw

一个用于个人项目、任务、资源与 AI 辅助分析的工作台。

## 现在的核心能力

- **项目管理**：项目列表、详情、进度概览
- **任务管理**：创建/编辑/删除、分类、优先级、状态流转、延期、追加需求
- **时间过程记录**：项目时间线、任务时间线、任务变更日志
- **AI 助手**：基于项目与任务上下文的分析、问答、建议
- **资源管理**：电脑、资源路径、漫画/章节/图片浏览
- **本地代理**：用于访问本机资源与扫描文件结构

## 技术栈

- **前端**：Vue 3 + Vite + Pinia + TDesign
- **后端**：ASP.NET Core 9 + Entity Framework Core + MySQL
- **代理服务**：Python FastAPI
- **部署**：Docker + Docker Compose

## 项目结构

```text
AI Claw/
├── api/          # ASP.NET Core API
├── projecthub/   # Vue 前端
├── proxy/        # Python 本地代理服务
├── ai/           # 给 AI 看的项目记忆与协作文档
├── docker-compose.yml
├── docker-compose.prod.yml
└── README.md
```

## 启动方式

### 前端

```bash
cd projecthub
npm install
npm run dev
```

### 后端

```bash
cd api
dotnet restore
dotnet run
```

### 本地代理

```bash
cd proxy
pip install -r requirements.txt
python main.py
```

## 关键约定

### 1. 后端启动时会同步数据库结构

后端在 `api/Program.cs` 中包含数据库自动同步逻辑。启动时会检查表和列是否存在，并按需创建或补齐。

这意味着：

- 开发业务时如果新增模型或字段，必须同步检查 `api/Program.cs`
- 不要只改实体类而忽略启动时的建表/补列逻辑
- 新增数据库结构时，要同时更新运行时同步和对应模型

### 2. 日志是排查问题的重要依据

项目中的日志文件主要用于开发与问题定位。开发时遇到前后端问题，应优先查看日志而不是盲猜。

建议习惯：

- 后端在关键流程中记录日志
- 前端在关键 API 调用、状态变更、异常处理中记录日志
- 测试发现问题后，先通过日志定位，再修改代码

### 3. 代码与文档以当前实现为准

仓库中可能保留部分历史文档或过时说明。当前项目规范以实际代码结构、后端启动逻辑和 `ai/` 目录中的协作文档为准。

## API 概览

- `GET /api/projects`
- `GET /api/tasks`
- `GET /api/computers`
- `GET /api/resource-paths`
- `GET /api/comics`
- `POST /api/ai/chat`
- `POST /api/ai/analyze`

## 说明

本仓库包含前端、后端和代理服务，适合做个人工作流、项目追踪与本地资源管理的一体化工具。