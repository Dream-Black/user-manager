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

// ===== 财务模块 API =====
// 账户 API
export const financeAccountApi = {
  list: () => api.get('/financeaccounts'),
  get: (id) => api.get(`/financeaccounts/${id}`),
  create: (data) => api.post('/financeaccounts', data),
  update: (id, data) => api.put(`/financeaccounts/${id}`, data),
  delete: (id) => api.delete(`/financeaccounts/${id}`),
  setDefault: (id) => api.put(`/financeaccounts/${id}/default`)
}

// 支出 API
export const financeExpenseApi = {
  list: (params = {}) => api.get('/financeexpenses', { params }),
  get: (id) => api.get(`/financeexpenses/${id}`),
  create: (data) => api.post('/financeexpenses', data),
  update: (id, data) => api.put(`/financeexpenses/${id}`, data),
  delete: (id) => api.delete(`/financeexpenses/${id}`)
}

// 支出分类 API
export const financeCategoryApi = {
  list: () => api.get('/financecategories'),
  create: (data) => api.post('/financecategories', data),
  update: (id, data) => api.put(`/financecategories/${id}`, data),
  delete: (id) => api.delete(`/financecategories/${id}`)
}

// 收入 API
export const financeIncomeApi = {
  list: (params = {}) => api.get('/financeincomes', { params }),
  get: (id) => api.get(`/financeincomes/${id}`),
  create: (data) => api.post('/financeincomes', data),
  update: (id, data) => api.put(`/financeincomes/${id}`, data),
  delete: (id) => api.delete(`/financeincomes/${id}`),
  stats: (params = {}) => api.get('/financeincomes/stats', { params })
}

// 工资模板 API
export const financeSalaryTemplateApi = {
  list: () => api.get('/financesalarytemplates'),
  get: (id) => api.get(`/financesalarytemplates/${id}`),
  create: (data) => api.post('/financesalarytemplates', data),
  update: (id, data) => api.put(`/financesalarytemplates/${id}`, data),
  delete: (id) => api.delete(`/financesalarytemplates/${id}`)
}

// 转账 API
export const financeTransferApi = {
  list: (params = {}) => api.get('/financetransfers', { params }),
  create: (data) => api.post('/financetransfers', data),
  balanceTrend: (params = {}) => api.get('/financetransfers/stats/balance-trend', { params }),
  monthlySavings: (params = {}) => api.get('/financetransfers/stats/monthly-savings', { params })
}

// 账户快照 API
export const financeSnapshotApi = {
  list: (params = {}) => api.get('/financesnapshots', { params }),
  manual: () => api.post('/financesnapshots/manual')
}

export default api
