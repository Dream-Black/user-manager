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

// ============ 项目相关 ============
export const projectService = {
  // 获取所有项目
  getAll: async (params = {}) => {
    return api.get('/projects', { params })
  },

  // 获取单个项目
  getById: async (id) => {
    return api.get(`/projects/${id}`)
  },

  // 创建项目
  create: async (data) => {
    return api.post('/projects', data)
  },

  // 更新项目
  update: async (id, data) => {
    return api.put(`/projects/${id}`, data)
  },

  // 删除项目
  delete: async (id) => {
    return api.delete(`/projects/${id}`)
  },

  // 获取项目任务
  getTasks: async (projectId) => {
    return api.get(`/projects/${projectId}/tasks`)
  },

  // 创建项目任务
  createTask: async (projectId, data) => {
    return api.post(`/projects/${projectId}/tasks`, data)
  },

  // 更新任务
  updateTask: async (taskId, data) => {
    return api.patch(`/tasks/${taskId}`, data)
  },

  // 删除任务
  deleteTask: async (taskId) => {
    return api.delete(`/tasks/${taskId}`)
  }
}

// ============ 任务相关 ============
export const taskService = {
  // 获取所有任务
  getAll: async (params = {}) => {
    return api.get('/tasks', { params })
  },

  // 按项目获取任务
  getByProject: async (projectId) => {
    return api.get(`/tasks/project/${projectId}`)
  },

  // 获取单个任务
  getById: async (id) => {
    return api.get(`/tasks/${id}`)
  },

  // 创建任务
  create: async (data) => {
    return api.post('/tasks', data)
  },

  // 更新任务
  update: async (id, data) => {
    return api.put(`/tasks/${id}`, data)
  },

  // 删除任务
  delete: async (id) => {
    return api.delete(`/tasks/${id}`)
  },

  // 延期任务
  delay: async (id, data) => {
    return api.post(`/tasks/${id}/delay`, data)
  },

  // 添加计划外需求
  addExtra: async (id, data) => {
    return api.post(`/tasks/${id}/extra`, data)
  }
}

// ============ 时间线相关 ============
export const timelineService = {
  getAll: async (params = {}) => {
    return api.get('/timelines', { params })
  },

  getByProject: async (projectId) => {
    return api.get(`/timelines/project/${projectId}`)
  }
}

// ============ 复盘相关 ============
export const reviewService = {
  getAll: async (params = {}) => {
    return api.get('/reviews', { params })
  },

  getById: async (id) => {
    return api.get(`/reviews/${id}`)
  },

  create: async (data) => {
    return api.post('/reviews', data)
  },

  update: async (id, data) => {
    return api.put(`/reviews/${id}`, data)
  },

  delete: async (id) => {
    return api.delete(`/reviews/${id}`)
  }
}

// ============ 分类相关 ============
export const categoryService = {
  getAll: async () => {
    return api.get('/categories')
  },

  create: async (data) => {
    return api.post('/categories', data)
  },

  update: async (id, data) => {
    return api.put(`/categories/${id}`, data)
  },

  delete: async (id) => {
    return api.delete(`/categories/${id}`)
  }
}

// ============ 设置相关 ============
export const settingsService = {
  get: async () => {
    return api.get('/settings')
  },

  update: async (data) => {
    return api.put('/settings', data)
  }
}

// ============ AI 相关 ============
export const aiService = {
  chat: async (data) => {
    return api.post('/ai/chat', data)
  },

  getReminder: async () => {
    return api.get('/ai/reminder')
  }
}

// ============ 甘特图相关 ============
export const ganttService = {
  getData: async (params = {}) => {
    return api.get('/gantt', { params })
  }
}

export default api
