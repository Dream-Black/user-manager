# AI Claw 桌面端适配 - 执行方案（新对话提示词）

> **使用方式**：将以下内容复制粘贴到新的 AI 对话中，AI 将按此方案逐步执行。

---

## 角色定位

你是 AI Claw 项目的全栈开发工程师。你的任务是在现有 Web 项目基础上新增 Windows 桌面端支持。

## 项目背景

### 现有架构
- **前端**：Vue 3 + Vite + TDesign，位于 `projecthub/` 目录
- **后端**：ASP.NET Core 9 + MySQL，位于 `api/` 目录
- **本地代理**：Python FastAPI 服务，位于 `proxy/` 目录，端口 **6789**
- **部署**：Docker Compose + GitHub Actions CI/CD → 腾讯云服务器
- **CI/CD**：`.github/workflows/deploy.yml`，push to main 时触发

### Python Proxy 接口清单（已实现，不能改）
| 方法 | 路径 | 用途 |
|------|------|------|
| GET | `/health` | 健康检查 |
| GET | `/system/info` | 系统信息 |
| GET | `/files/list?path=xxx` | 列出目录内容 |
| GET | `/files/tree?path=xxx&depth=2` | 文件夹树结构 |
| GET | `/files/read?path=xxx` | 读取文件（图片预览等） |
| GET | `/comics/scan?path=xxx` | 扫描漫画文件夹 |
| POST | `/resource-paths/test` | 测试路径是否存在 |
| POST | `/config/paths` | 添加允许访问的路径 |
| GET | `/config/paths` | 获取允许访问的路径列表 |

### 核心痛点
- 本地开发时：前端在 `localhost:3000`，可以正常访问 `localhost:6789` 的 Python Proxy
- 公网部署后：前端加载的是 `https://服务器IP`，**浏览器禁止 HTTPS 页面向 HTTP localhost 发请求**（Mixed Content / CORS），导致所有本地资源功能失效
- **解决方案**：用 Electron 做桌面壳层，通过 IPC Bridge 让 Vue 页面绕过浏览器安全限制访问本地 Python Proxy

## 架构设计

```
用户电脑                          云端服务器
┌─────────────────────────┐      ┌──────────────────┐
│ Electron 主进程          │      │ ASP.NET API      │
│   ├─ BrowserWindow      │◀─────│ (业务数据)        │
│   │   └─ 加载服务器URL    │ HTTP │                  │
│   │                     │─────▶│                  │
│   ├─ IPC Handler        │      └──────────────────┘
│   │   └─ 转发请求给     │
│   │     localhost:6789  │
├─────────────────────────┤
│ Python Proxy (:6789)    │
│   ├─ 文件系统访问       │
│   ├─ 漫画扫描           │
│   └─ 未来: AI工作流+模型 │
└─────────────────────────┘
```

**数据流向**：
1. Vue 页面通过 `window.localBridge.fetch(url)` 发起 IPC 请求
2. Electron 主进程收到 IPC，用 Node.js 的 `http.request()` 转发到 `http://localhost:6789`
3. Python Proxy 返回结果，原路返回给渲染进程
4. **全程不走浏览器网络栈，不受 Mixed Content / CORS 限制**

## 三阶段实施计划

---

## Phase 1：开发环境跑通桌面端构建

### 目标
- 在本机执行 `npm run dev` 能打开 Electron 窗口，加载云端服务器页面，功能完整
- 执行 `npm run build:win` 能产出 `.exe` 安装包
- **IPC Bridge 能正常转发请求到本地 Python Proxy**

### 需要新建的目录和文件

```
AI Claw/
└── desktop/                    # 新建此目录及以下全部文件
    ├── package.json            # Electron + electron-builder 配置
    ├── main.js                 # 主进程：窗口创建、IPC Bridge、子进程管理、更新检查骨架
    ├── preload.js              # 安全桥接层（contextBridge）
    ├── .env.example            # SERVER_URL 示例配置
    └── assets/                 # 应用图标等静态资源
        └── icon.ico            # 应用图标（先用默认占位）
```

### desktop/package.json 关键要求
```json
{
  "name": "ai-claw-desktop",
  "version": "1.0.0",
  "main": "main.js",
  "scripts": {
    "dev": "electron .",
    "build:win": "electron-builder --win"
  },
  "devDependencies": {
    "electron": "^31.0.0",
    "electron-builder": "^24.13.0"
  },
  "build": {
    "appId": "com.aiclaw.desktop",
    "productName": "AI Claw",
    "directories": { "output": "dist" },
    "win": {
      "target": [
        { "target": "nsis", "arch": ["x64"] }
      ],
      "icon": "assets/icon.ico"
    },
    "nsis": {
      "oneClick": false,
      "allowToChangeInstallationDirectory": true,
      "createDesktopShortcut": true
    }
  }
}
```

### desktop/main.js 核心逻辑要求

必须包含以下模块：

#### 1. 常量与配置
```javascript
const SERVER_URL = process.env.SERVER_URL || 'http://你的公网IP'  // 从 .env 或环境变量读取
const PROXY_URL = 'http://localhost:6789'  // Python Proxy 固定地址
const DOWNLOAD_PAGE = 'https://github.com/你的仓库/releases/latest'
```

> ⚠️ 注意：SERVER_URL 需要从项目根目录或 desktop/.env 文件读取，不要硬编码。可以用 dotenv 或 fs 手动读取。

#### 2. 窗口创建函数 createWindow()
- 宽度 1400, 高度 900, 最小尺寸 1024x600
- 标题栏显示 "AI Claw"
- 加载 `SERVER_URL`
- 外部链接用系统浏览器打开（`shell.openExternal`）
- 开发模式启用 DevTools（`process.env.NODE_ENV === 'development'`）

#### 3. IPC Bridge（最核心部分）
注册以下 IPC handler：

```javascript
// 主进程接收渲染进程的请求 → 转发给 Python Proxy → 返回响应
ipcMain.handle('local-fetch', async (event, { method = 'GET', url, body }) => {
  // 构造完整 URL：如果 url 是相对路径，拼接 PROXY_URL 前缀
  const fullUrl = url.startsWith('http') ? url : `${PROXY_URL}${url}`
  
  // 使用 Node.js http/https 模块发起请求（不走浏览器）
  // 支持 method、headers、body 参数
  // 返回 JSON 或 Buffer（根据 Content-Type 判断）
})

// 版本信息查询
ipcMain.handle('get-version', () => app.getVersion())

// 打开下载页
ipcMain.handle('open-download', () => shell.openExternal(DOWNLOAD_PAGE))
```

**重要细节**：
- `local-fetch` 必须支持 GET 和 POST 两种方法
- POST 请求需要正确序列化 body（JSON.stringify）
- 响应需要根据 `Content-Type` 自动判断返回 JSON 还是原始数据
- 错误处理要完善（Proxy 未启动时给出明确提示）
- 请求超时设置 10 秒

#### 4. 更新检查（Phase 1 先搭骨架）
```javascript
async function checkForUpdate(win) {
  try {
    const data = await fetchJson(`${SERVER_URL}/version.json`)
    if (isNewerVersion(data.version, app.getVersion())) {
      // 弹出对话框提示有新版本
      // 按钮："下载更新" / "稍后再说"
      // 点"下载更新"跳转到 GitHub Releases
    }
  } catch (e) {
    console.log('版本检查失败（可能离线）:', e.message)
  }
}
```

> Phase 1 只需骨架代码能跑通即可，Phase 2 再完善。

#### 5. Python Proxy 存活检测
启动窗口前先检测 Python Proxy 是否在运行：
```javascript
async function isProxyAlive() {
  try {
    await fetch(`${PROXY_URL}/health`, { signal: AbortSignal.timeout(2000) })
    return true
  } catch {
    return false
  }
}
```
- 如果 Proxy 不在线，**不阻塞应用启动**（因为不是所有页面都需要本地功能），但在首次尝试调用 local-fetch 失败时给出友好提示

### desktop/preload.js 要求

```javascript
const { contextBridge, ipcRenderer } = require('electron')

contextBridge.exposeInMainWorld('localBridge', {
  fetch: (url, options = {}) => ipcRenderer.invoke('local-fetch', { 
    method: options.method || 'GET',
    url, 
    body: options.body,
    headers: options.headers 
  }),
  
  get: (url, headers) => ipcRenderer.invoke('local-fetch', { 
    method: 'GET', 
    url, 
    headers 
  }),
  
  post: (url, body, headers) => ipcRenderer.invoke('local-fetch', { 
    method: 'POST', 
    url, 
    body,
    headers 
  }),
  
  getVersion: () => ipcRenderer.invoke('get-version'),
  openDownload: () => ipcRenderer.invoke('open-download'),
  checkUpdate: () => ipcRenderer.invoke('check-update')
})
```

**安全约束**：
- `contextIsolation: true`
- `nodeIntegration: false`
- 所有能力通过 contextBridge 暴露

### desktop/.env.example 内容
```env
# 云端服务器地址（Electron 窗口加载的地址）
SERVER_URL=http://your-server-ip

# 开发环境标记
NODE_ENV=development
```

### Phase 1 验收标准

1. ✅ `cd desktop && npm install && npm run dev` → 弹出 Electron 窗口，显示服务器页面
2. ✅ 窗口中所有原有功能（项目管理、任务管理、AI 助手）正常运行
3. ✅ 启动 Python Proxy 后，在开发者工具 Console 中输入：
   ```js
   window.localBridge.get('/health').then(r => console.log(r))
   ```
   应该返回 `{ status: "ok", service: "LocalProxy", version: "1.0.0" }`
4. ✅ `npm run build:win` → `desktop/dist/` 产生 `.exe` 安装包
5. ✅ 安装包双击安装后，应用正常启动并加载服务器页面

---

## Phase 2：开发环境跑通自动更新

### 目标
- 用户安装旧版桌面端后，启动时能检测到新版本并提示下载
- 更新机制基于现有的 `version.json`（Web CI 已经生成并部署到服务器根目录）

### version.json 格式（已有，不需要改）
```json
{
  "version": "1.0.5",
  "buildTime": "2026-04-25T10:00:00Z",
  "buildDate": "2026-04-25",
  "commit": "abc1234"
}
```

### 更新流程
```
应用启动 → 等待首屏加载完成（延迟 8~10 秒）→ 请求 SERVER_URL/version.json
    │
    ├─ 版本相同或更低 → 无操作
    │
    └─ 服务器版本更高 → 弹窗提示：
       "发现新版本 v1.0.5
        当前版本：v1.0.3
        [下载更新]  [稍后再说]"
       
       点击[下载更新] → 打开浏览器访问 GitHub Releases 页面
```

### main.js 中需要完善的更新检查逻辑

1. **版本比较函数**：语义化版本比较（major > minor > patch）
2. **对话框定制**：使用 `dialog.showMessageBox`，样式清晰
3. **错误静默处理**：网络不通时不应影响应用启动
4. **频率控制**：每天最多检查一次（用 localStorage 或文件记录上次检查时间）

> ⚠️ Electron 渲染进程中没有 localStorage（因为是加载远程网页），所以频率控制需要在主进程中做，用文件记录。

### Phase 2 验收标准

1. ✅ 手动把 `package.json` 的 version 改低（如 0.9.0），启动后弹出更新提示
2. ✅ 点"下载更新"正确跳转到 GitHub Releases 页面
3. ✅ 点"稍后再说"关闭弹窗，应用正常使用
4. ✅ 断网情况下无报错，应用正常启动
5. ✅ 同一天内多次重启不会重复弹窗

---

## Phase 3：跑通桌面端 CI/CD

### 目标
- 新增 `.github/workflows/desktop.yml`
- **现有 `deploy.yml` 一行不改**
- 推送 Git tag `v*.*.*` 时自动构建 Windows 安装包并发布到 GitHub Releases

### 触发机制
```
日常开发 push main          → deploy.yml（Web 部署，完全不变）
git tag v1.0.5 && git push  → deploy.yml + desktop.yml 双触发
                                （desktop.yml 仅由 tag 触发，不影响 Web 流程）
```

### .github/workflows/desktop.yml 要求

```yaml
name: Desktop - Build & Release

on:
  push:
    tags:
      - 'v*.*.*'

jobs:
  build-desktop:
    name: "构建 Windows 桌面端"
    runs-on: windows-latest   # 必须用 Windows runner

    steps:
      # ... 具体步骤见下方 ...
```

关键步骤：

1. **检出代码** `actions/checkout@v4`
2. **读取 VERSION 文件**作为构建版本号
3. **Node.js 22**
4. **安装 desktop 依赖** `cd desktop && npm install`
5. **同步版本号**：读取 VERSION 写入 desktop/package.json 的 version 字段
6. **注入 SERVER_URL**：从 GitHub Secret `DESKTOP_SERVER_URL` 读取
7. **构建** `cd desktop && npm run build:win`
8. **发布 Release**：用 `softprops/action-gh-release@v2`
   - Release 名称含版本号
   - Body 包含安装说明
   - 上传 `desktop/dist/*.exe`

### 需要在 GitHub 仓库添加的 Secret

| Secret 名称 | 说明 | 示例值 |
|------------|------|--------|
| `DESKTOP_SERVER_URL` | 桌面端连接的服务器地址 | `http://1.2.3.4` |

> 现有的 `SERVER_HOST` / `SERVER_USER` / `SERVER_SSH_KEY` 三个 Secret **完全不动**

### Phase 3 验收标准

1. ✅ 创建 tag `v1.0.5` 并推送 → Actions 自动触发 desktop 构建
2. ✅ GitHub Releases 出现 v1.0.5 的 Release
3. ✅ Release 中包含 `.exe` 安装包
4. ✅ 下载安装包 → 安装 → 启动 → 显示服务器页面
5. ✅ 现有 Web CI/CD（deploy.yml）行为不变，push to main 正常部署

---

## 重要规则（违反则方案无效）

### 绝对不能改的
1. ❌ **不能修改** `projecthub/` 下任何文件（Vue 前端代码零改动）
2. ❌ **不能修改** `api/` 下任何文件（后端代码零改动）
3. ❌ **不能修改** `proxy/` 下任何文件（Python Proxy 已完成，不动）
4. ❌ **不能修改** `.github/workflows/deploy.yml`（Web CI/CD 保持不变）
5. ❌ **不能修改** `docker-compose.yml` 及任何 Docker 配置
6. ❌ **不能修改** `scripts/bump-version.sh` 及版本递增逻辑

### 可以改的
1. ✅ 新增 `desktop/` 目录及其全部文件
2. ✅ 新增 `.github/workflows/desktop.yml`
3. ✅ 新增 GitHub Secrets（`DESKTOP_SERVER_URL`）
4. ✅ 如果 Phase 2/3 需要，可以在 `api/` 的 Nginx 配置或部署脚本中**确保** `version.json` 可被公开访问（但优先确认是否已经可以）

### 代码质量要求
- `main.js` 要有完整的注释（中文），每个模块说明用途
- 错误处理要完善，特别是 Python Proxy 未启动时的场景
- `preload.js` 必须遵守 Electron 安全最佳实践（contextIsolation + nodeIntegration: false）
- 所有对外部服务的请求都要有超时控制
- 日志输出统一格式 `[Desktop] 模块名 操作 结果`

### 输出顺序
请严格按 **Phase 1 → Phase 2 → Phase 3** 的顺序实施，每个阶段完成后：
1. 列出该阶段创建/修改的所有文件清单
2. 说明验收方法
3. 等待确认后再进入下一阶段

---

*文档生成日期：2026-04-25*
