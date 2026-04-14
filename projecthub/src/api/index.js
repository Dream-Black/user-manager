import axios from 'axios'

const api = axios.create({
  baseURL: '/api',
  timeout: 10000,
  headers: {
    'Content-Type': 'application/json'
  }
})

// 响应拦截器
api.interceptors.response.use(
  response => response.data,
  error => {
    console.error('API Error:', error)
    return Promise.reject(error)
  }
)

// 项目 API
export const projectApi = {
  list: (params = {}) => api.get('/projects', { params }),
  get: (id) => api.get(`/projects/${id}`),
  create: (data) => api.post('/projects', data),
  update: (id, data) => api.put(`/projects/${id}`, data),
  delete: (id) => api.delete(`/projects/${id}`)
}

// 任务 API
export const taskApi = {
  listByProject: (projectId) => api.get(`/tasks/project/${projectId}`),
  get: (id) => api.get(`/tasks/${id}`),
  create: (data) => api.post('/tasks', data),
  update: (id, data) => api.put(`/tasks/${id}`, data),
  delete: (id) => api.delete(`/tasks/${id}`),
  delay: (id, data) => api.post(`/tasks/${id}/delay`, data),
  addExtra: (id, data) => api.post(`/tasks/${id}/extra`, data)
}

// 时间线 API
export const timelineApi = {
  list: (projectId) => api.get('/timelines', { params: { projectId } }),
  listByProject: (projectId) => api.get(`/timelines/project/${projectId}`)
}

// 复盘 API
export const reviewApi = {
  list: (params = {}) => api.get('/reviews', { params }),
  get: (id) => api.get(`/reviews/${id}`),
  create: (data) => api.post('/reviews', data),
  update: (id, data) => api.put(`/reviews/${id}`, data),
  delete: (id) => api.delete(`/reviews/${id}`)
}

// 分类 API
export const categoryApi = {
  list: () => api.get('/categories'),
  create: (data) => api.post('/categories', data),
  update: (id, data) => api.put(`/categories/${id}`, data),
  delete: (id) => api.delete(`/categories/${id}`)
}

// 设置 API
export const settingsApi = {
  get: () => api.get('/settings'),
  update: (data) => api.put('/settings', data)
}

// AI API
export const aiApi = {
  chat: (data) => api.post('/ai/chat', data),
  reminder: () => api.get('/ai/reminder')
}

// 甘特图 API
export const ganttApi = {
  getData: (params = {}) => api.get('/gantt', { params })
}

export default api
