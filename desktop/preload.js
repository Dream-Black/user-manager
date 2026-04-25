// ============================================================
// AI Claw Desktop - Preload 脚本（安全桥接层）
// ============================================================
// 功能：通过 contextBridge 安全暴露 IPC 能力给渲染进程
// 安全约束：contextIsolation: true, nodeIntegration: false
// ============================================================

const { contextBridge, ipcRenderer } = require('electron');

// ============================================================
// 暴露 localBridge API 给渲染进程（Vue 页面）
// ============================================================

contextBridge.exposeInMainWorld('localBridge', {
  /**
   * 通用 fetch 请求（支持 GET/POST）
   * @param {string} url - 相对路径或完整URL（相对路径会自动拼接 PROXY_URL）
   * @param {object} options - 请求选项
   * @param {string} options.method - HTTP 方法（GET/POST）
   * @param {object} options.body - 请求体（POST 时使用）
   * @param {object} options.headers - 自定义请求头
   * @returns {Promise<{ status, data, isBuffer }>}
   */
  fetch: (url, options = {}) => {
    return ipcRenderer.invoke('local-fetch', {
      method: options.method || 'GET',
      url,
      body: options.body,
      headers: options.headers
    });
  },

  /**
   * GET 请求便捷方法
   * @param {string} url - 请求路径
   * @param {object} headers - 自定义请求头（可选）
   */
  get: (url, headers = {}) => {
    return ipcRenderer.invoke('local-fetch', {
      method: 'GET',
      url,
      headers
    });
  },

  /**
   * POST 请求便捷方法
   * @param {string} url - 请求路径
   * @param {object} body - 请求体
   * @param {object} headers - 自定义请求头（可选）
   */
  post: (url, body, headers = {}) => {
    return ipcRenderer.invoke('local-fetch', {
      method: 'POST',
      url,
      body,
      headers
    });
  },

  /**
   * 获取应用版本号
   * @returns {Promise<string>}
   */
  getVersion: () => {
    return ipcRenderer.invoke('get-version');
  },

  /**
   * 打开 GitHub Releases 下载页面
   */
  openDownload: () => {
    return ipcRenderer.invoke('open-download');
  },

  /**
   * 手动触发版本检查
   * @returns {Promise<{ success: boolean, error?: string }>}
   */
  checkUpdate: () => {
    return ipcRenderer.invoke('check-update');
  },

  /**
   * 获取 Python Proxy 连接状态
   * @returns {Promise<{ alive: boolean, url: string }>}
   */
  getProxyStatus: () => {
    return ipcRenderer.invoke('proxy-status');
  }
});

// ============================================================
// 可选：暴露诊断 API（仅开发环境使用）
// ============================================================

contextBridge.exposeInMainWorld('desktopDiagnostics', {
  /**
   * 获取环境信息
   */
  getEnvInfo: () => {
    return {
      platform: process.platform,
      arch: process.arch,
      versions: process.versions,
      electronVersion: process.versions.electron,
      nodeVersion: process.versions.node,
      chromeVersion: process.versions.chrome
    };
  }
});

// ============================================================
// 日志：Preload 加载完成
// ============================================================
console.log('[Desktop] Preload 脚本已加载，localBridge API 已就绪');
