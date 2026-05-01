/**
 * 资源管理模块 API
 * 后端API: /api (通过 Nginx 反向代理)
 * 本地代理: http://localhost:6789
 *
 * 桌面端适配：
 * - 在 Electron 环境中使用 window.localBridge.fetch() 绕过 CORS
 * - 在 Web 环境中保持原有的 axios 直连方式
 */
import axios from 'axios';

// 创建axios实例
const apiClient = axios.create({
  baseURL: '/api',
  timeout: 30000,
});

// 请求拦截器：添加计算机标识
apiClient.interceptors.request.use(
  (config) => {
    const computerHostname = getComputerHostName();
    if (computerHostname) {
      config.headers['X-Computer-Hostname'] = computerHostname;
    }
    return config;
  },
  (error) => Promise.reject(error)
);

// 获取计算机主机名
function getComputerHostName(): string {
  return localStorage.getItem('computer-hostname') || window.location.hostname;
}

// ============================================================
// 桌面端兼容层：本地代理请求
// ============================================================

// 检测是否在 Electron 桌面端环境中
function isDesktop(): boolean {
  return typeof window !== 'undefined' && typeof (window as any).localBridge !== 'undefined';
}

// 本地代理基础路径
const PROXY_BASE_URL = 'http://localhost:6789';

/**
 * 封装本地代理请求（兼容桌面端和 Web 端）
 * - 桌面端：通过 Electron IPC 转发，绕过 CORS
 * - Web 端：直接请求（仅开发环境可用）
 */
async function proxyRequest<T = any>(
  method: 'GET' | 'POST' | 'PUT' | 'DELETE',
  path: string,
  options?: { params?: Record<string, any>; data?: any }
): Promise<T> {
  // 构造完整 URL
  let url = path;
  if (options?.params) {
    const searchParams = new URLSearchParams();
    Object.entries(options.params).forEach(([key, value]) => {
      if (value !== undefined && value !== null) {
        searchParams.append(key, String(value));
      }
    });
    const queryString = searchParams.toString();
    if (queryString) {
      url += (url.includes('?') ? '&' : '?') + queryString;
    }
  }

  // 桌面端：使用 Electron IPC 转发
  if (isDesktop()) {
    console.log(`[Desktop Proxy] ${method} ${url}`);
    const result = await (window as any).localBridge.fetch(url, {
      method,
      body: options?.data
    });

    if (result.status >= 200 && result.status < 300) {
      return result.data;
    } else {
      const error = new Error(result.data?.error || result.data?.message || `请求失败 (${result.status})`);
      (error as any).response = { status: result.status, data: result.data };
      throw error;
    }
  }

  // Web 端：使用 axios 直连
  const fullUrl = `${PROXY_BASE_URL}${url}`;
  console.log(`[Web Proxy] ${method} ${fullUrl}`);

  const response = await axios({
    method,
    url: fullUrl,
    data: options?.data,
    timeout: 30000,
  });

  return response.data;
}

/**
 * 获取本地代理健康状态
 */
export async function getProxyHealth() {
  return proxyRequest<{ status: string; service: string; version: string }>('GET', '/health');
}

/**
 * 获取系统信息
 */
export async function getSystemInfo() {
  return proxyRequest<any>('GET', '/system/info');
}

/**
 * 获取文件列表
 */
export async function getFileList(path: string) {
  return proxyRequest<any>('GET', '/files/list', { params: { path } });
}

/**
 * 读取文件（图片预览）
 * 返回文件 URL
 * - 桌面端：返回通过 localBridge 获取的 base64 数据 URL
 * - Web 端：返回直接访问的 URL（仅开发环境可用）
 */
export async function getFileContent(path: string): Promise<string> {
  if (isDesktop()) {
    // 桌面端：通过 IPC 获取文件内容
    const result = await (window as any).localBridge.get(`/files/read?path=${encodeURIComponent(path)}`);

    if (result.status === 200 && result.data) {
      // 如果是 base64 图片，构造 data URL
      const contentType = result.data.contentType || 'image/jpeg';
      const base64 = result.data.content || result.data;
      if (typeof base64 === 'string' && base64.length > 100) {
        // 假设是 base64 编码
        return `data:${contentType};base64,${base64}`;
      }
      return base64;
    }
    throw new Error(result.data?.error || '获取文件失败');
  }

  // Web 端：返回直接访问的 URL
  return `${PROXY_BASE_URL}/files/read?path=${encodeURIComponent(path)}`;
}

/**
 * 扫描漫画文件夹
 */
export async function scanComicFolder(path: string) {
  return proxyRequest<any>('GET', '/comics/scan', { params: { path } });
}

/**
 * 添加允许路径
 */
export async function addAllowedPath(path: string) {
  return proxyRequest<any>('POST', '/config/paths', { data: { path } });
}

/**
 * 获取允许路径列表
 */
export async function getAllowedPaths() {
  return proxyRequest<any[]>('GET', '/config/paths');
}

/**
 * 测试路径是否存在
 */
export async function testPath(path: string) {
  return proxyRequest<any>('POST', '/resource-paths/test', { data: { path } });
}

// ============================================================
// 电脑相关 API
// ============================================================

/**
 * 获取当前电脑（自动识别）
 */
export function getCurrentComputer() {
  return apiClient.get('/computers/current');
}

/**
 * 更新电脑信息
 */
export function updateComputer(id: number, data: { name?: string }) {
  return apiClient.put(`/computers/${id}`, data);
}

/**
 * 发送心跳
 */
export function sendHeartbeat(id: number) {
  return apiClient.post(`/computers/${id}/heartbeat`);
}

// ============================================================
// 资源路径相关 API
// ============================================================

/**
 * 获取资源路径列表
 */
export function getResourcePaths(computerId?: number) {
  return apiClient.get('/resource-paths/list', { params: { computerId } });
}

/**
 * 创建资源路径
 */
export function createResourcePath(data: {
  computerId: number;
  type: string;
  path: string;
}) {
  return apiClient.post('/resource-paths', data);
}

/**
 * 更新资源路径
 */
export function updateResourcePath(
  id: number,
  data: { path?: string; isEnabled?: boolean }
) {
  return apiClient.put(`/resource-paths/${id}`, data);
}

/**
 * 删除资源路径
 */
export function deleteResourcePath(id: number) {
  return apiClient.delete(`/resource-paths/${id}`);
}

/**
 * 测试资源路径
 */
export async function testResourcePath(path: string) {
  return testPath(path);
}

// ============================================================
// 漫画相关 API
// ============================================================

/**
 * 获取漫画列表
 */
export function getComics(params?: {
  resourcePathId?: number;
  type?: string;
  search?: string;
  page?: number;
  pageSize?: number;
}) {
  return apiClient.get('/comics', { params });
}

/**
 * 获取单个漫画详情
 */
export function getComic(id: number) {
  return apiClient.get(`/comics/${id}`);
}

/**
 * 更新漫画
 */
export function updateComic(
  id: number,
  data: { displayName?: string; type?: string }
) {
  return apiClient.put(`/comics/${id}`, data);
}

/**
 * 删除漫画
 */
export function deleteComic(id: number) {
  return apiClient.delete(`/comics/${id}`);
}

/**
 * 上传漫画封面
 */
export function uploadThumbnail(id: number, base64: string) {
  return apiClient.post(`/comics/${id}/thumbnail`, { thumbnailBase64: base64 });
}

/**
 * 扫描漫画文件夹（调用本地代理）
 * 返回原始扫描结果（含 chapters[]），供前端使用
 */
export async function scanComics(resourcePathId: number) {
  // 1. 先获取资源路径信息
  const pathResponse = await apiClient.get(`/resource-paths/${resourcePathId}`);
  const resourcePath = pathResponse.data;

  if (!resourcePath?.path) {
    throw new Error('资源路径不存在');
  }

  // 2. 调用本地代理获取本地扫描结果
  const scanPath = resourcePath.path.replace(/\\/g, '/');
  const data = await scanComicFolder(scanPath);

  console.log('[scanComics] 本地扫描结果:', data);

  // 返回原始数据，包含 chapters 供后续使用
  return {
    resourcePath,
    comics: data.comics || [],
    totalCount: data.comicCount || 0
  };
}

/**
 * 批量保存扫描结果到数据库
 */
export async function batchSaveComics(resourcePathId: number, comics: any[]) {
  const response = await apiClient.post('/comics/batch-scan', {
    resourcePathId,
    comics: comics.map(comic => ({
      folderName: comic.name,
      displayName: comic.name,
      type: 'manga'
    }))
  });

  return response.data;
}

/**
 * 获取漫画章节
 */
export function getChapters(comicId: number) {
  return apiClient.get(`/comics/${comicId}/chapters`);
}

// ============================================================
// 章节相关 API
// ============================================================

/**
 * 获取章节详情
 */
export function getChapter(id: number) {
  return apiClient.get(`/chapters/${id}`);
}

/**
 * 获取章节页面列表
 */
export function getChapterPages(chapterId: number) {
  return apiClient.get(`/chapters/${chapterId}/pages`);
}

// ============================================================
// 类型定义
// ============================================================

export interface Computer {
  id: number;
  name: string;
  hostName: string;
  isOnline: boolean;
  lastHeartbeat?: string;
  createdAt: string;
}

export interface ResourcePath {
  id: number;
  computerId: number;
  type: 'comic' | 'video' | 'novel' | 'image';
  path: string;
  isEnabled: boolean;
  createdAt: string;
}

export interface Comic {
  id: number;
  resourcePathId: number;
  folderName: string;
  displayName: string;
  type: 'manga' | 'comic' | 'novel' | 'picture' | 'scroll';
  thumbnailBase64?: string;
  chapterCount?: number;
  createdAt: string;
  updatedAt: string;
}

export interface ComicChapter {
  id: number;
  comicId: number;
  folderName: string;
  displayName: string;
  sortOrder: number;
  createdAt: string;
}

export interface ChapterPage {
  index: number;
  name: string;
  path: string;
  url?: string;
}

export default apiClient;
