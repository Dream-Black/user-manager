// ============================================================
// AI Claw Desktop - Electron 主进程
// ============================================================
// 功能：窗口管理、IPC Bridge（转发请求到本地Python Proxy）、更新检查
// ============================================================

const { app, BrowserWindow, ipcMain, shell, dialog } = require('electron');
const path = require('path');
const fs = require('fs');
const http = require('http');
const https = require('https');

// ============================================================
// 1. 常量与配置
// ============================================================

// 读取 .env 文件配置（如果存在）
function loadEnvConfig() {
  const envPath = path.join(__dirname, '.env');
  const config = {};

  if (fs.existsSync(envPath)) {
    const envContent = fs.readFileSync(envPath, 'utf-8');
    envContent.split('\n').forEach(line => {
      const match = line.match(/^([^#=]+)=(.*)$/);
      if (match) {
        config[match[1].trim()] = match[2].trim();
      }
    });
  }

  return config;
}

const envConfig = loadEnvConfig();

// 服务器地址：从 .env 或环境变量读取
const SERVER_URL = envConfig.SERVER_URL || process.env.SERVER_URL || 'http://localhost:3000';

// Python Proxy 固定地址
const PROXY_URL = 'http://localhost:6789';

// GitHub Releases 下载页面
const DOWNLOAD_PAGE = 'https://github.com/your-org/ai-claw/releases/latest';

// 版本检查记录文件路径
const VERSION_CHECK_FILE = path.join(app.getPath('userData'), 'last-version-check.txt');

// ============================================================
// 2. 工具函数
// ============================================================

/**
 * 日志输出统一格式
 */
function log(module, message, status = 'INFO') {
  const timestamp = new Date().toISOString();
  console.log(`[${timestamp}] [Desktop] [${module}] [${status}] ${message}`);
}

/**
 * 发送 HTTP/HTTPS 请求（Node.js 方式，不走浏览器）
 * @param {string} url - 完整 URL
 * @param {object} options - 请求选项 { method, headers, body, timeout }
 * @returns {Promise<{ status, data, isBuffer }>}
 */
function httpRequest(url, options = {}) {
  return new Promise((resolve, reject) => {
    const urlObj = new URL(url);
    const isHttps = urlObj.protocol === 'https:';
    const lib = isHttps ? https : http;

    const requestOptions = {
      hostname: urlObj.hostname,
      port: urlObj.port || (isHttps ? 443 : 80),
      path: urlObj.pathname + urlObj.search,
      method: options.method || 'GET',
      headers: options.headers || {},
      timeout: options.timeout || 10000
    };

    const req = lib.request(requestOptions, (res) => {
      // 根据 Content-Type 判断返回类型
      const contentType = res.headers['content-type'] || '';
      const isJson = contentType.includes('application/json');
      const isBuffer = contentType.includes('image/') ||
                       contentType.includes('application/octet-stream');

      const chunks = [];

      res.on('data', (chunk) => {
        chunks.push(chunk);
      });

      res.on('end', () => {
        const buffer = Buffer.concat(chunks);

        if (isBuffer) {
          resolve({ status: res.statusCode, data: buffer, isBuffer: true });
        } else if (isJson) {
          try {
            resolve({ status: res.statusCode, data: JSON.parse(buffer.toString()), isBuffer: false });
          } catch (e) {
            resolve({ status: res.statusCode, data: buffer.toString(), isBuffer: false });
          }
        } else {
          resolve({ status: res.statusCode, data: buffer.toString(), isBuffer: false });
        }
      });
    });

    req.on('error', reject);
    req.on('timeout', () => {
      req.destroy();
      reject(new Error('请求超时'));
    });

    // 写入 body（用于 POST 请求）
    if (options.body) {
      const body = typeof options.body === 'object' ? JSON.stringify(options.body) : options.body;
      req.write(body);
    }

    req.end();
  });
}

/**
 * 检查 Python Proxy 是否在线
 */
async function isProxyAlive() {
  try {
    await httpRequest(`${PROXY_URL}/health`, { timeout: 2000 });
    return true;
  } catch (e) {
    return false;
  }
}

/**
 * 语义化版本比较
 * @returns {number} 1: a更新, -1: b更新, 0: 相同
 */
function compareVersions(a, b) {
  const partsA = a.split('.').map(Number);
  const partsB = b.split('.').map(Number);

  for (let i = 0; i < Math.max(partsA.length, partsB.length); i++) {
    const numA = partsA[i] || 0;
    const numB = partsB[i] || 0;

    if (numA > numB) return 1;
    if (numA < numB) return -1;
  }

  return 0;
}

/**
 * 检查是否有新版本（每天最多一次）
 */
async function checkForUpdate(win) {
  // 延迟检查，等待首屏加载
  setTimeout(async () => {
    try {
      // 检查频率控制：读取上次检查时间
      let lastCheck = null;
      if (fs.existsSync(VERSION_CHECK_FILE)) {
        lastCheck = fs.readFileSync(VERSION_CHECK_FILE, 'utf-8').trim();
      }

      const today = new Date().toISOString().split('T')[0];

      // 同一天内不重复检查
      if (lastCheck === today) {
        log('Update', '今日已检查版本，跳过');
        return;
      }

      // 请求版本信息
      log('Update', '正在检查版本...');
      const { data } = await httpRequest(`${SERVER_URL}/version.json`, { timeout: 5000 });

      const serverVersion = data.version;
      const currentVersion = app.getVersion();

      log('Update', `服务器版本: ${serverVersion}, 当前版本: ${currentVersion}`);

      if (compareVersions(serverVersion, currentVersion) > 0) {
        // 发现新版本，弹出提示
        const result = await dialog.showMessageBox(win, {
          type: 'info',
          title: '发现新版本',
          message: `发现新版本 v${serverVersion}`,
          detail: `当前版本：v${currentVersion}\n是否下载更新？`,
          buttons: ['下载更新', '稍后再说'],
          defaultId: 0,
          cancelId: 1
        });

        if (result.response === 0) {
          // 用户点击"下载更新"
          shell.openExternal(DOWNLOAD_PAGE);
        }
      }

      // 记录今日已检查
      fs.writeFileSync(VERSION_CHECK_FILE, today);
      log('Update', '版本检查完成');

    } catch (e) {
      log('Update', `版本检查失败（可能离线）: ${e.message}`, 'WARN');
    }
  }, 8000); // 延迟 8 秒检查
}

// ============================================================
// 3. 窗口管理
// ============================================================

let mainWindow = null;

/**
 * 创建主窗口
 */
function createWindow() {
  log('Window', '正在创建主窗口...');

  mainWindow = new BrowserWindow({
    width: 1400,
    height: 900,
    minWidth: 1024,
    minHeight: 600,
    title: 'AI Claw',
    show: false, // 等加载完成再显示

    // 安全设置
    webPreferences: {
      preload: path.join(__dirname, 'preload.js'),
      contextIsolation: true,
      nodeIntegration: false,
      sandbox: false // preload 需要访问 Node.js
    }
  });

  // 加载服务器页面
  log('Window', `正在加载: ${SERVER_URL}`);
  mainWindow.loadURL(SERVER_URL);

  // 页面加载完成后显示窗口
  mainWindow.once('ready-to-show', () => {
    mainWindow.show();
    log('Window', '主窗口已显示');

    // 开发模式打开 DevTools
    if (envConfig.NODE_ENV === 'development' || process.env.NODE_ENV === 'development') {
      mainWindow.webContents.openDevTools();
    }

    // 检查更新（Phase 1 先搭骨架）
    checkForUpdate(mainWindow);
  });

  // 处理外部链接（用系统浏览器打开）
  mainWindow.webContents.setWindowOpenHandler(({ url }) => {
    // 允许打开外部链接
    shell.openExternal(url);
    return { action: 'deny' };
  });

  // 窗口关闭时
  mainWindow.on('closed', () => {
    mainWindow = null;
    log('Window', '主窗口已关闭');
  });

  // 加载失败时提示
  mainWindow.webContents.on('did-fail-load', (event, errorCode, errorDescription) => {
    log('Window', `页面加载失败: ${errorCode} - ${errorDescription}`, 'ERROR');
    dialog.showErrorBox('加载失败', `无法连接到服务器 (${SERVER_URL})\n\n${errorDescription}`);
  });
}

// ============================================================
// 4. IPC Bridge（核心功能）
// ============================================================

/**
 * IPC Handler: 转发请求到本地 Python Proxy
 * Vue 页面通过 window.localBridge.fetch() 调用此方法
 */
ipcMain.handle('local-fetch', async (event, { method = 'GET', url, body, headers = {} }) => {
  try {
    // 构造完整 URL
    const fullUrl = url.startsWith('http') ? url : `${PROXY_URL}${url}`;

    log('IPC', `${method} ${fullUrl}`);

    const result = await httpRequest(fullUrl, {
      method: method.toUpperCase(),
      headers: {
        'Content-Type': 'application/json',
        ...headers
      },
      body: body,
      timeout: 10000
    });

    return result;

  } catch (error) {
    log('IPC', `请求失败: ${error.message}`, 'ERROR');

    // 区分 Proxy 未启动和其他错误
    if (error.message.includes('ECONNREFUSED')) {
      return {
        status: 503,
        data: {
          error: 'Python Proxy 未启动',
          message: '请确保 LocalProxy 服务正在运行（python proxy/main.py）',
          proxyUrl: PROXY_URL
        },
        isBuffer: false
      };
    }

    return {
      status: 500,
      data: { error: error.message },
      isBuffer: false
    };
  }
});

/**
 * IPC Handler: 获取应用版本
 */
ipcMain.handle('get-version', () => {
  return app.getVersion();
});

/**
 * IPC Handler: 打开下载页面
 */
ipcMain.handle('open-download', () => {
  shell.openExternal(DOWNLOAD_PAGE);
});

/**
 * IPC Handler: 手动检查更新
 */
ipcMain.handle('check-update', async () => {
  if (mainWindow) {
    await checkForUpdate(mainWindow);
    return { success: true };
  }
  return { success: false, error: '窗口未就绪' };
});

/**
 * IPC Handler: 获取 Python Proxy 状态
 */
ipcMain.handle('proxy-status', async () => {
  const alive = await isProxyAlive();
  return {
    alive,
    url: PROXY_URL
  };
});

// ============================================================
// 5. 应用生命周期
// ============================================================

// 应用准备就绪
app.whenReady().then(async () => {
  log('App', 'AI Claw Desktop 启动中...');
  log('App', `服务器地址: ${SERVER_URL}`);
  log('App', `Python Proxy: ${PROXY_URL}`);

  // 检查 Proxy 是否在线（不阻塞启动）
  const proxyAlive = await isProxyAlive();
  if (proxyAlive) {
    log('App', 'Python Proxy 在线 ✓');
  } else {
    log('App', 'Python Proxy 未启动（本地功能将在首次使用时提示）', 'WARN');
  }

  createWindow();

  // macOS 特殊处理：dock 点击时创建新窗口
  app.on('activate', () => {
    if (BrowserWindow.getAllWindows().length === 0) {
      createWindow();
    }
  });
});

// 所有窗口关闭时
app.on('window-all-closed', () => {
  log('App', '所有窗口已关闭');
  // macOS 除外，大多数平台退出应用
  if (process.platform !== 'darwin') {
    app.quit();
  }
});

// 应用退出前
app.on('before-quit', () => {
  log('App', '应用即将退出');
});

// 捕获未处理的异常
process.on('uncaughtException', (error) => {
  log('App', `未捕获异常: ${error.message}`, 'ERROR');
  console.error(error);
});

// ============================================================
// Phase 1 完成
// 验收方法见 README
// ============================================================
