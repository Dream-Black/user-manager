/**
 * 资源管理模块 API
 * 后端API: http://localhost:5000
 * 本地代理: http://localhost:6789
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

// ==================== 电脑相关 API ====================

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

// ==================== 资源路径相关 API ====================

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
export function testResourcePath(path: string) {
  return proxyClient.post('/resource-paths/test', { path });
}

// ==================== 漫画相关 API ====================

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
 * 扫描漫画文件夹（调用6789代理）
 * 返回原始扫描结果（含 chapters[]），供前端使用
 */
export async function scanComics(resourcePathId: number) {
  // 1. 先获取资源路径信息
  const pathResponse = await apiClient.get(`/resource-paths/${resourcePathId}`);
  const resourcePath = pathResponse.data;
  
  if (!resourcePath?.path) {
    throw new Error('资源路径不存在');
  }
  
  // 2. 调用6789代理获取本地扫描结果
  const scanPath = resourcePath.path.replace(/\\/g, '/');
  const response = await proxyClient.get('/comics/scan', { 
    params: { path: scanPath } 
  });
  
  const data = response.data;
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

// ==================== 章节相关 API ====================

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

// ==================== 本地代理 API ====================

const proxyClient = axios.create({
  baseURL: 'http://localhost:6789',
  timeout: 30000,
});

/**
 * 获取代理服务健康状态
 */
export function getProxyHealth() {
  return proxyClient.get('/health');
}

/**
 * 获取系统信息
 */
export function getSystemInfo() {
  return proxyClient.get('/system/info');
}

/**
 * 获取文件列表
 */
export function getFileList(path: string) {
  return proxyClient.get('/files/list', { params: { path } });
}

/**
 * 读取文件（图片）
 */
export function getFileContent(path: string) {
  return `${proxyClient.defaults.baseURL}/files/read?path=${encodeURIComponent(path)}`;
}

/**
 * 扫描漫画文件夹
 */
export function scanComicFolder(path: string) {
  return proxyClient.get('/comics/scan', { params: { path } });
}

/**
 * 添加允许路径
 */
export function addAllowedPath(path: string) {
  return proxyClient.post('/config/paths', { path });
}

// ==================== 类型定义 ====================

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
  type: 'manga' | 'comic' | 'novel' | 'picture';
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
