# ADR-001: 桌面端适配架构决策

## 状态

已接受

## 上下文

ProjectHub / AI Claw 是一个运行在 Docker + 腾讯云上的 Web 应用，技术栈为 Vue3 + ASP.NET Core 9 + MySQL + Python Proxy。

现有 CI/CD 流程（GitHub Actions → 腾讯云）已稳定运行，Web 端发布流程不可受影响。

用户需求：在保留 Web 端全部功能的前提下，新增 Windows 11 桌面端支持，使该应用可以作为本地客户端安装使用。

**关键约束**：
1. 不修改现有任何 CI/CD 配置文件
2. 前端代码（Vue3）不进行平台差异化改造
3. 桌面端构建流程独立于 Web 端构建

## 决策

采用 **Electron 壳层方案**，以最小侵入性实现桌面端支持。

### 核心设计

```
┌─────────────────────────────────────────────────────────┐
│                    共享代码层（不变）                      │
│  ┌──────────────┐  ┌──────────────┐  ┌───────────────┐  │
│  │  Vue3 前端   │  │ ASP.NET API  │  │ Python Proxy  │  │
│  │ (projecthub) │  │    (api/)    │  │   (proxy/)    │  │
│  └──────────────┘  └──────────────┘  └───────────────┘  │
└─────────────────────────────────────────────────────────┘
          │                                    │
          ▼                                    ▼
┌──────────────────┐              ┌───────────────────────┐
│   WEB 端（现有）  │              │    桌面端（新增）       │
│  Docker Compose  │              │  Electron 主进程       │
│  CI/CD 不动      │              │  ├─ spawn .NET API    │
│                  │              │  ├─ spawn Python Proxy│
│                  │              │  └─ BrowserWindow     │
└──────────────────┘              └───────────────────────┘
```

### 目录结构新增

```
AI Claw/
└── desktop/                    # 新增，Electron 壳层
    ├── package.json
    ├── main.js                  # 主进程：子进程管理 + 窗口
    ├── preload.js               # 安全桥接层
    ├── resources/               # 内嵌后端二进制
    │   ├── api-win-x64/         # .NET self-contained 发布产物
    │   └── proxy/               # PyInstaller 打包产物
    └── scripts/
        ├── build-api.ps1        # 编译 .NET → Windows exe
        └── build-proxy.ps1      # PyInstaller 打包
```

### CI/CD 扩展策略

在 `.github/workflows/` 下**新增** `desktop.yml`，现有 `deploy.yml` **完全不修改**。

触发条件：仅当推送 `desktop-*` 格式的 Git tag 时触发，与 Web 端的 `main` 分支推送触发完全解耦。

```
push to main → 仅触发 Web CI/CD（deploy.yml）
git tag desktop-v1.0.0 → 触发 desktop.yml（桌面端构建）
```

### 数据库策略（第一版）

桌面端直接连接云端 MySQL，配置文件指定服务器地址。理由：
- 零额外工作量
- 数据与 Web 端完全同步
- 离线能力列为后续迭代需求

## 方案对比与放弃原因

| 方案 | 放弃原因 |
|------|---------|
| Tauri | 需要 Rust bridge 层，学习成本高；与现有 .NET 后端整合复杂 |
| PWA  | 无法访问本地文件系统；系统集成能力几乎为零 |
| NW.js | 社区生态远不及 Electron；长期维护风险 |

## 后果

### 变得更容易的事
- Web 端代码零修改，CI/CD 零感知
- 桌面版可以直接访问本地文件系统（通过 Electron Node.js 层）
- 系统托盘、开机自启、系统通知均可实现
- 未来可去除 Python Proxy，改用 Electron 原生文件系统 API

### 变得更难的事
- 桌面安装包约 150~250MB（.NET runtime + Electron Chromium）
- .NET 需要发布为 self-contained，构建时间增加
- Windows 11 SmartScreen 可能拦截未签名安装包（需考虑签名）
- 离线模式需要额外的 SQLite 切换 + 数据同步逻辑（暂不实现）

## 实施路径

```
Phase 1（1-2周）：搭壳 + 能跑
  ├─ 创建 desktop/ 目录
  ├─ Electron 主进程启动 .NET API + Python Proxy 子进程
  ├─ BrowserWindow 加载 localhost:5000
  └─ 新增 desktop.yml CI（仅 tag 触发，不影响现有流程）

Phase 2（1周）：打包发布
  ├─ .NET publish -r win-x64 --self-contained true
  ├─ electron-builder 配置 NSIS 安装包
  └─ 产物上传到 GitHub Releases

Phase 3（按需迭代）：桌面体验增强
  ├─ 系统托盘 + 最小化到托盘
  ├─ 开机自启（Windows Registry）
  ├─ 本地通知（Electron Notification API）
  └─ 离线模式 + SQLite 本地存储
```

---

*记录日期：2026-04-25*
*决策人：ProjectHub 架构团队*
