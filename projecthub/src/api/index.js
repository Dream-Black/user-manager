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

// 日程 API
export const scheduleApi = {
  list: () => api.get('/schedules'),
  get: (id) => api.get(`/schedules/${id}`),
  create: (data) => api.post('/schedules', data),
  update: (id, data) => api.put(`/schedules/${id}`, data),
  delete: (id) => api.delete(`/schedules/${id}`),
  getDays: (scheduleId) => api.get(`/schedules/${scheduleId}/days`),
  upsertDay: (scheduleId, data) => api.post(`/schedules/${scheduleId}/days`, data),
  updateDay: (scheduleId, dayDate, data) => api.put(`/schedules/${scheduleId}/days/${dayDate}`, data),
  deleteDay: (scheduleId, dayId) => api.delete(`/schedules/${scheduleId}/days/${dayId}`),
  generateDays: (scheduleId, data) => api.post(`/schedules/${scheduleId}/generate-days`, data),
  updateDayContent: (scheduleId, dayDate, content) => api.put(`/schedules/${scheduleId}/days/${dayDate}/content`, { content }),
  updateDayStatus: (scheduleId, dayDate, status, skipReason) => api.put(`/schedules/${scheduleId}/days/${dayDate}/status`, { status, skipReason })
}

export default api
