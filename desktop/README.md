# AI Claw Desktop

Windows 桌面端应用程序，基于 Electron 构建。

## 快速开始

### 安装依赖

```bash
cd desktop
npm install
```

### 开发模式运行

1. 确保服务器已启动（前端开发服务器或已部署的服务器）
2. 配置 `.env` 文件（复制 `.env.example` 并修改 `SERVER_URL`）
3. 启动开发：

```bash
npm run dev
```

### 构建 Windows 安装包

```bash
npm run build:win
```

构建完成后，安装包位于 `dist/` 目录。

## 配置说明

编辑 `desktop/.env` 文件：

```env
# 服务器地址（必填）
SERVER_URL=http://你的服务器IP或域名

# 开发模式（可选，会打开 DevTools）
NODE_ENV=development
```

## 功能特性

- 🚀 加载云端服务器页面
- 🔒 通过 IPC Bridge 安全访问本地 Python Proxy
- 🔄 自动版本检查与更新提示
- 📦 跨平台打包（Windows/macOS/Linux）

## Python Proxy 集成

桌面端通过 Electron IPC 机制转发请求到本地 Python Proxy（端口 6789），
从而绕过浏览器的 Mixed Content 限制。

启动 Python Proxy：
```bash
cd proxy
python main.py
```

## 图标说明

- `icon.svg` - 矢量图标源文件
- 正式发布前需要转换为各平台格式：
  - Windows: `.ico`
  - macOS: `.icns`
  - Linux: `.png`
