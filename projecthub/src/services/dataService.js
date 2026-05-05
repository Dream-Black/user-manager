import axios from 'axios'

const api = axios.create({
  baseURL: '/api',
  timeout: 30000,
  headers: {
    'Content-Type': 'application/json'
  }
})

api.interceptors.request.use((config) => {
  const token = localStorage.getItem('token')
  const expiresAt = Number(localStorage.getItem('tokenExpiresAt') || 0)

  if (token) {
    if (expiresAt && Date.now() > expiresAt) {
      localStorage.removeItem('token')
      localStorage.removeItem('tokenExpiresAt')
      localStorage.removeItem('user')
    } else {
      config.headers.Authorization = `Bearer ${token}`
    }
  }

  return config
})

// 响应拦截器
api.interceptors.response.use(
  response => response.data,
  error => {
    if (error?.response?.status === 401) {
      localStorage.removeItem('token')
      localStorage.removeItem('tokenExpiresAt')
      localStorage.removeItem('user')
      if (window.location.pathname !== '/login') {
        window.location.href = '/login'
      }
    }
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
    return api.put(`/projects/tasks/${taskId}`, data)
  },

  // 更新任务进度
  updateTaskProgress: async (taskId, progress) => {
    return api.patch(`/projects/tasks/${taskId}/progress`, { progress })
  },

  // 删除任务
  deleteTask: async (taskId) => {
    return api.delete(`/projects/tasks/${taskId}`)
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

  // 更新任务进度
  updateProgress: async (id, progress) => {
    return api.patch(`/tasks/${id}`, { progress })
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

// ============ 用户相关 ============
export const userService = {
  // 获取当前用户信息
  getCurrent: async () => {
    return api.get('/users/current')
  },

  // 更新用户信息
  update: async (data) => {
    return api.put('/users/current', data)
  },

  // 上传头像
  uploadAvatar: async (base64) => {
    return api.post('/users/current/avatar', { avatar: base64 })
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

// ============ 用户设置相关 ============
export const userSettingsService = {
  // 获取用户设置
  get: async () => {
    return api.get('/users/current/settings')
  },

  // 更新用户设置
  update: async (data) => {
    return api.put('/users/current/settings', data)
  }
}

// ============ AI 相关 ============
export const aiService = {
  // 获取所有对话列表
  getConversations: async (params = {}) => {
    return api.get('/ai/conversations', { params })
  },

  archiveConversation: async (id, isArchived) => {
    return api.patch(`/ai/conversations/${id}/archive`, { isArchived })
  },

  pinConversation: async (id, isPinned) => {
    return api.patch(`/ai/conversations/${id}/pin`, { isPinned })
  },

  // 新建对话
  createConversation: async (title) => {
    return api.post('/ai/conversations', { title })
  },

  // 获取对话消息
  getConversationMessages: async (id) => {
    return api.get(`/ai/conversations/${id}`)
  },

  updateMessage: async (conversationId, messageId, data) => {
    return api.put(`/ai/conversations/${conversationId}/messages/${messageId}`, data)
  },

  regenerateMessage: async (conversationId, messageId) => {
    return api.post(`/ai/conversations/${conversationId}/messages/${messageId}/regenerate`)
  },

  confirmDraft: async (conversationId, messageId) => {
    return api.post(`/ai/conversations/${conversationId}/messages/${messageId}/confirm-draft`)
  },

  cancelDraft: async (conversationId, messageId) => {
    return api.post(`/ai/conversations/${conversationId}/messages/${messageId}/cancel-draft`)
  },

  // 删除对话
  deleteConversation: async (id) => {
    return api.delete(`/ai/conversations/${id}`)
  },

  // 获取AI设置
  getSettings: async () => {
    return api.get('/ai/settings')
  },

  // 更新AI设置
  updateSettings: async (data) => {
    return api.put('/ai/settings', data)
  },

  // 上传附件
  uploadAttachment: async (file) => {
    const formData = new FormData()
    formData.append('file', file)
    return api.post('/ai/upload', formData, {
      timeout: 30000
    })
  },

  // 流式聊天（返回 fetch Response 用于读取 SSE）
  chatStream: async (conversationId, message, deepThink, attachments, model) => {
    const response = await fetch(`/api/ai/conversations/${conversationId}/chat`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${localStorage.getItem('token') || ''}` },
      body: JSON.stringify({
        message,
        deepThink,
        model,
        attachments: attachments ? JSON.stringify(attachments) : null
      })
    })

    return response
  },

  // 继续对话（用于工具执行结果返回后继续 AI 回复）
  continueChat: async (conversationId, toolResult) => {
    const response = await fetch(`/api/ai/conversations/${conversationId}/continue`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${localStorage.getItem('token') || ''}` },
      body: JSON.stringify({
        toolResult
      })
    })

    return response
  },

  getBalance: async () => {
    return api.get('/ai/balance')
  }
}

// ============ 子任务相关 ============
export const subTaskService = {
  // 获取父任务的所有子任务
  getByTaskId: async (taskId) => {
    return api.get(`/subtasks/by-task/${taskId}`)
  },

  // 创建子任务
  create: async (data) => {
    return api.post('/subtasks', data)
  },

  // 更新子任务
  update: async (id, data) => {
    return api.put(`/subtasks/${id}`, data)
  },

  // 切换完成状态
  toggleComplete: async (id) => {
    return api.patch(`/subtasks/${id}/toggle`)
  },

  // 删除子任务
  delete: async (id) => {
    return api.delete(`/subtasks/${id}`)
  }
}

// ============ 甘特图相关 ============
export const ganttService = {
    getData: async (params = {}) => {
        return api.get('/gantt', { params });
    }
}

// ============ 笔记相关 ============
export const noteService = {
    getAll: async (params = {}) => {
        return api.get('/notes', { params })
    },

    getById: async (id) => {
        return api.get(`/notes/${id}`)
    },

    getTags: async () => {
        return api.get('/notes/tags')
    },

    create: async (data) => {
        return api.post('/notes', data)
    },

    update: async (id, data) => {
        return api.put(`/notes/${id}`, data)
    },

    delete: async (id) => {
        return api.delete(`/notes/${id}`)
    }
}

// ============ 终端执行相关（桌面端） ============
export const terminalService = {
    // 检测是否在桌面端环境
    isDesktopEnv: () => {
        return window.localBridge && typeof window.localBridge.post === 'function';
    },

    // 执行终端命令
    executeCommand: async (command) => {
        if (!window.localBridge) {
            throw new Error('Not in desktop environment');
        }
        
        const result = await window.localBridge.post('/terminal/execute', { command });
        
        if (result.status !== 200) {
            throw new Error(result.data?.error || 'Execute failed');
        }
        
        return result.data;
    },

    // 检测 AI 回复是否包含终端命令
    detectTerminalCommand: (text) => {
        // 检测是否有 <terminal> 标签包裹的命令
        const terminalRegex = /<terminal>([\s\S]*?)<\/terminal>/i;
        const match = text.match(terminalRegex);
        
        if (match && match[1]) {
            return {
                hasCommand: true,
                command: match[1].trim(),
                originalText: text
            };
        }
        
        return { hasCommand: false };
    },

    // 从文本中移除 terminal 标签
    stripTerminalTags: (text) => {
        return text.replace(/<terminal>[\s\S]*?<\/terminal>/gi, '').trim();
    }
}

export default api
