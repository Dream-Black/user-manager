<template>
  <div class="tasks-page">
    <!-- 页面头部 -->
    <div class="page-header">
      <div class="header-content">
        <h1 class="page-title">任务管理</h1>
        <p class="page-subtitle">管理团队任务，跟踪工作进度</p>
      </div>
      <div class="header-actions">
        <t-button theme="primary" @click="showCreateDialog = true">
          <template #icon><AddIcon /></template>
          新建任务
        </t-button>
      </div>
    </div>

    <!-- 任务统计卡片 -->
    <div class="task-stats">
      <div
        v-for="(stat, index) in taskStats"
        :key="stat.key"
        class="stat-card"
        :style="{ '--accent-color': stat.color, animationDelay: `${index * 0.1}s` }"
      >
        <div class="stat-icon">
          <component :is="stat.icon" />
        </div>
        <div class="stat-content">
          <span class="stat-value">{{ stat.value }}</span>
          <span class="stat-label">{{ stat.label }}</span>
        </div>
      </div>
    </div>

    <!-- 筛选栏 -->
    <div class="filter-bar">
      <div class="filter-left">
        <t-input
          v-model="searchQuery"
          placeholder="搜索任务..."
          clearable
        >
          <template #prefix-icon>
            <SearchIcon />
          </template>
        </t-input>

        <t-select
          v-model="filterStatus"
          placeholder="状态"
          clearable
          style="width: 130px"
        >
          <t-option value="all" label="全部状态" />
          <t-option value="pending" label="待开始" />
          <t-option value="in_progress" label="进行中" />
          <t-option value="completed" label="已完成" />
        </t-select>

        <t-select
          v-model="filterPriority"
          placeholder="优先级"
          clearable
          style="width: 130px"
        >
          <t-option value="all" label="全部优先级" />
          <t-option value="high" label="高优先级" />
          <t-option value="medium" label="中优先级" />
          <t-option value="low" label="低优先级" />
        </t-select>

        <t-date-range-picker
          v-model="dateRange"
          placeholder="时间范围"
          allow-input
          clearable
        />
      </div>

      <div class="filter-right">
        <t-radio-group v-model="viewMode" variant="default-filled" size="medium">
          <t-radio-button value="board">
            <ViewColumnIcon />
            看板
          </t-radio-button>
          <t-radio-button value="list">
            <ViewListIcon />
            列表
          </t-radio-button>
        </t-radio-group>
      </div>
    </div>

    <!-- 看板视图 -->
    <transition name="fade" mode="out-in">
      <div v-if="viewMode === 'board'" key="board" class="task-board">
        <div
          v-for="column in boardColumns"
          :key="column.key"
          class="board-column"
          :class="{ 'drop-target': draggedTask && dragOverColumn === column.key }"
          :style="{ '--column-color': column.color }"
          @dragover.prevent="dragOverColumn = column.key"
          @dragleave="dragOverColumn = null"
          @drop="handleDrop($event, column.key)"
        >
          <div class="column-header">
            <div class="column-title">
              <span class="column-dot"></span>
              {{ column.title }}
            </div>
            <span class="column-count">{{ getColumnTasks(column.key).length }}</span>
          </div>

          <div class="column-content">
            <div
              v-for="(task, index) in getColumnTasks(column.key)"
              :key="task.id"
              class="task-card"
              :data-task-id="task.id"
              :style="{ animationDelay: `${index * 0.05}s` }"
              :class="{ 'is-dragging': draggedTask?.id === task.id }"
              draggable="true"
              @dragstart="handleDragStart($event, task)"
              @dragover.prevent
              @drop="handleDrop($event, column.key)"
            >
              <div class="task-card-header">
                <t-tag :type="getPriorityType(task.priority)" variant="light" size="small">
                  {{ task.priorityText }}
                </t-tag>
                <div class="task-actions" @click.stop>
                  <t-dropdown trigger="click">
                    <t-button variant="text" size="small">
                      <template #icon><MoreIcon /></template>
                    </t-button>
                    <template #dropdown>
                      <t-dropdown-menu>
                        <t-dropdown-item @click="handleEdit(task)">编辑</t-dropdown-item>
                        <t-dropdown-item @click="handleChangeStatus(task, 'completed')">标记完成</t-dropdown-item>
                        <t-dropdown-item divider />
                        <t-dropdown-item theme="error" @click="handleDelete(task)">删除</t-dropdown-item>
                      </t-dropdown-menu>
                    </template>
                  </t-dropdown>
                </div>
              </div>

              <h4 class="task-title">{{ task.title }}</h4>
              <p class="task-description">{{ task.description }}</p>

              <div class="task-meta">
                <div class="task-project">
                  <span class="project-dot" :style="{ background: task.projectColor }"></span>
                  {{ task.project }}
                </div>
                <div class="task-date">
                  <CalendarIcon />
                  {{ task.dueDate }}
                </div>
              </div>

              <div class="task-progress-bar" @click.stop>
                <div
                  class="task-progress-fill"
                  :style="{ width: task.progress + '%' }"
                  :class="{ dragging: draggingProgressTask?.id === task.id, completed: task.progress === 100 }"
                ></div>
                <div
                  class="progress-handle"
                  :style="{ left: task.progress + '%' }"
                  :class="{ active: draggingProgressTask?.id === task.id }"
                  @mousedown.stop="startProgressDrag($event, task)"
                ></div>
              </div>
              <div class="task-progress-text" @click.stop>{{ task.progress }}%</div>

              <div class="task-footer">
                <div class="task-category">
                  <t-tag :type="getCategoryType(task.categoryName)" variant="light" size="small">
                    {{ getCategoryText(task.categoryName) }}
                  </t-tag>
                </div>
                <div class="task-hours">
                  {{ task.estimatedHours ? task.estimatedHours + 'h' : '-' }}
                </div>
              </div>
            </div>

            <div
              v-if="getColumnTasks(column.key).length === 0"
              class="column-empty"
            >
              暂无任务
            </div>

            <div class="column-add" @click="quickAddTask(column.key)">
              <AddIcon />
              添加任务
            </div>
          </div>
        </div>
      </div>

      <!-- 列表视图 -->
      <div v-else key="list" class="task-list-view">
        <t-table
          :data="filteredTasks"
          :columns="columns"
          row-key="id"
          hover
          stripe
          :pagination="pagination"
        >
          <template #title="{ row }">
            <div class="task-title-cell">
              <t-checkbox
                :checked="row.status === 'completed'"
                @change="() => toggleTaskStatus(row)"
              />
              <span class="title-text" :class="{ completed: row.status === 'completed' }">
                {{ row.title }}
              </span>
            </div>
          </template>

          <template #status="{ row }">
            <t-tag :type="getStatusType(row.status)" variant="light">
              {{ row.statusText }}
            </t-tag>
          </template>

          <template #categoryName="{ row }">
            <t-tag :type="getCategoryType(row.categoryName)" variant="light" size="small">
              {{ getCategoryText(row.categoryName) }}
            </t-tag>
          </template>

          <template #priority="{ row }">
            <t-tag :type="getPriorityType(row.priority)" variant="light">
              {{ row.priorityText }}
            </t-tag>
          </template>

          <template #progress="{ row }">
            <div class="progress-cell">
                <div class="progress-bar-sm">
                <div class="progress-fill" :class="{ completed: row.progress === 100 }" :style="{ width: row.progress + '%' }"></div>
              </div>
              <span>{{ row.progress }}%</span>
            </div>
          </template>

          <template #estimatedHours="{ row }">
            {{ row.totalEstimatedHours ?? row.estimatedHours ? (row.totalEstimatedHours ?? row.estimatedHours) + 'h' : '-' }}
          </template>

          <template #startDate="{ row }">
            <span>{{ row.startDate ? formatDate(row.startDate) : '-' }}</span>
          </template>

          <template #dueDate="{ row }">
            <span :class="{ overdue: isOverdue(row.dueDate) && row.status !== 'completed' }">
              {{ row.dueDate }}
            </span>
          </template>

          <template #actions="{ row }">
            <div class="action-buttons">
              <t-button variant="text" size="small" @click="handleEdit(row)">
                <template #icon><EditIcon /></template>
              </t-button>
              <t-button variant="text" size="small" theme="danger" @click="handleDelete(row)">
                <template #icon><DeleteIcon /></template>
              </t-button>
            </div>
          </template>
        </t-table>
      </div>
    </transition>

    <!-- 创建/编辑任务弹窗 -->
    <t-dialog
      v-model:visible="showCreateDialog"
      :header="editingTask ? '编辑任务' : '新建任务'"
      width="600px"
      :footer="null"
    >
      <t-form ref="formRef" :data="formData" :rules="rules" label-align="top">
        <t-form-item label="任务标题" name="title">
          <t-input v-model="formData.title" placeholder="请输入任务标题" />
        </t-form-item>

        <t-form-item label="任务描述" name="description">
          <t-textarea v-model="formData.description" placeholder="请输入任务描述" />
        </t-form-item>

        <div class="form-row">
          <t-form-item label="所属项目" name="project" class="flex-1">
            <t-select v-model="formData.project">
              <t-option v-for="p in projects" :key="p.id" :value="p.name" :label="p.name" />
            </t-select>
          </t-form-item>

          <t-form-item label="负责人" name="assignee" class="flex-1">
            <t-select v-model="formData.assignee">
              <t-option v-for="m in members" :key="m.id" :value="m.name" :label="m.name" />
            </t-select>
          </t-form-item>
        </div>

        <div class="form-row">
          <t-form-item label="状态" name="status" class="flex-1">
            <t-select v-model="formData.status">
              <t-option value="pending" label="待开始" />
              <t-option value="in_progress" label="进行中" />
              <t-option value="completed" label="已完成" />
            </t-select>
          </t-form-item>

          <t-form-item label="优先级" name="priority" class="flex-1">
            <t-select v-model="formData.priority">
              <t-option value="high" label="高优先级" />
              <t-option value="medium" label="中优先级" />
              <t-option value="low" label="低优先级" />
            </t-select>
          </t-form-item>
        </div>

        <t-form-item label="截止日期" name="dueDate">
          <t-date-picker v-model="formData.dueDate" />
        </t-form-item>

        <div class="form-actions">
          <t-button variant="outline" @click="showCreateDialog = false">取消</t-button>
          <t-button theme="primary" @click="handleSubmit">确定</t-button>
        </div>
      </t-form>
    </t-dialog>

    <!-- 删除确认 -->
    <t-dialog
      v-model:visible="showDeleteDialog"
      header="删除任务"
      width="400px"
      :footer="null"
    >
      <div class="delete-confirm">
        <p class="delete-text">确定要删除任务 <strong>{{ deletingTask?.title }}</strong> 吗？</p>
        <div class="delete-actions">
          <t-button variant="outline" @click="showDeleteDialog = false">取消</t-button>
          <t-button theme="danger" @click="confirmDelete">确认删除</t-button>
        </div>
      </div>
    </t-dialog>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { MessagePlugin } from 'tdesign-vue-next'

// 状态
const searchQuery = ref('')
const filterStatus = ref('all')
const filterPriority = ref('all')
const dateRange = ref([])
const viewMode = ref('board')
const showCreateDialog = ref(false)
const showDeleteDialog = ref(false)
const editingTask = ref(null)
const deletingTask = ref(null)
const formRef = ref(null)
const draggedTask = ref(null)
const dragOverColumn = ref(null)
const draggingProgressTask = ref(null)
const progressDragStartX = ref(0)
const progressDragStartValue = ref(0)

// 分页
const pagination = ref({
  defaultCurrent: 1,
  defaultPageSize: 10,
  total: 0
})

// 表格列
const columns = [
  { colKey: 'title', title: '任务', width: '25%' },
  { colKey: 'categoryName', title: '分类', width: '10%' },
  { colKey: 'status', title: '状态', width: '10%' },
  { colKey: 'priority', title: '优先级', width: '10%' },
  { colKey: 'progress', title: '进度', width: '15%' },
  { colKey: 'estimatedHours', title: '预估工时', width: '10%' },
  { colKey: 'startDate', title: '开始日期', width: '10%' },
  { colKey: 'dueDate', title: '截止日期', width: '10%' },
  { colKey: 'actions', title: '操作', width: '8%', align: 'center' }
]

// 看板列
const boardColumns = [
  { key: 'pending', title: '待开始', color: '#9CA3AF' },
  { key: 'in_progress', title: '进行中', color: '#2196F3' },
  { key: 'completed', title: '已完成', color: '#10B981' }
]

// 项目
const projects = ref([
  { id: 1, name: 'Website Redesign', color: '#2196F3' },
  { id: 2, name: 'App Development', color: '#4CAF50' },
  { id: 3, name: 'API Integration', color: '#FF9800' }
])

// 成员
const members = ref([
  { id: 1, name: '张三', color: '#2196F3' },
  { id: 2, name: '李四', color: '#4CAF50' },
  { id: 3, name: '王五', color: '#FF9800' },
  { id: 4, name: '赵六', color: '#9C27B0' }
])

// 任务数据
const tasks = ref([])

// 加载任务数据
const loadTasks = async () => {
  try {
    const data = await taskService.getAll()
    tasks.value = data.map(t => ({
      id: t.id,
      title: t.title,
      description: t.description || '',
      status: t.status,
      statusText: getStatusText(t.status),
      priority: t.priority,
      priorityText: getPriorityText(t.priority),
      project: t.projectName || '未分配',
      projectColor: '#2196F3',
      assignee: '未分配',
      assigneeColor: '#9CA3AF',
      dueDate: t.dueDate ? dayjs(t.dueDate).format('YYYY年MM月DD日') : '未设置',
      startDate: t.startDate,
      progress: t.progress || 0,
      estimatedHours: t.estimatedHours || 0,
      subtaskCompleted: 0,
      subtaskTotal: 0,
      createdAt: t.createdAt,
      updatedAt: t.updatedAt
    }))
    pagination.value.total = tasks.value.length
  } catch (error) {
    console.error('加载任务失败:', error)
  }
}

// 加载项目列表
const loadProjects = async () => {
  try {
    const data = await projectService.getAll()
    projects.value = data.map(p => ({
      id: p.id,
      name: p.name,
      color: '#2196F3'
    }))
  } catch (error) {
    console.error('加载项目失败:', error)
  }
}

// 表单数据
const formData = ref({
  title: '',
  description: '',
  project: '',
  assignee: '',
  status: 'pending',
  priority: 'medium',
  dueDate: ''
})

// 规则
const rules = {
  title: [{ required: true, message: '请输入任务标题', trigger: 'blur' }],
  project: [{ required: true, message: '请选择所属项目', trigger: 'change' }]
}

// 获取状态文本
const getStatusText = (status) => {
  const map = {
    'todo': '待开始',
    'pending': '待开始',
    'in_progress': '进行中',
    'completed': '已完成'
  }
  return map[status] || '待开始'
}

// 获取优先级文本
const getPriorityText = (priority) => {
  const map = {
    'high': '高',
    'medium': '中',
    'low': '低'
  }
  return map[priority] || '中'
}

// 获取分类文本
const getCategoryText = (category) => {
  const map = {
    'dev': '开发',
    'meeting': '会议',
    'doc': '文档',
    'design': '设计',
    'debug': '调试',
    'bug': 'BUG'
  }
  return map[category] || category || '-'
}

// 获取分类类型
const getCategoryType = (category) => {
  const types = {
    'dev': 'primary',
    'meeting': 'warning',
    'doc': 'success',
    'design': 'purple',
    'debug': 'danger',
    'bug': 'danger'
  }
  return types[category] || 'default'
}

// 格式化日期
const formatDate = (date) => {
  return dayjs(date).format('YYYY年MM月DD日')
}

// 任务统计
const taskStats = computed(() => [
  {
    key: 'total',
    label: '总任务数',
    value: tasks.value.length,
    color: '#2196F3',
    icon: TaskIcon
  },
  {
    key: 'todo',
    label: '待开始',
    value: tasks.value.filter(t => t.status === 'todo' || t.status === 'pending').length,
    color: '#9CA3AF',
    icon: TimeIcon
  },
  {
    key: 'in_progress',
    label: '进行中',
    value: tasks.value.filter(t => t.status === 'in_progress').length,
    color: '#FF9800',
    icon: PlayIcon
  },
  {
    key: 'completed',
    label: '已完成',
    value: tasks.value.filter(t => t.status === 'completed').length,
    color: '#10B981',
    icon: CheckCircleIcon
  }
])

// 筛选后的任务
const filteredTasks = computed(() => {
  return tasks.value.filter(task => {
    const matchSearch = !searchQuery.value ||
      task.title.toLowerCase().includes(searchQuery.value.toLowerCase())
    const matchStatus = filterStatus.value === 'all' || task.status === filterStatus.value
    const matchPriority = filterPriority.value === 'all' || task.priority === filterPriority.value
    return matchSearch && matchStatus && matchPriority
  })
})

// 获取列任务
const getColumnTasks = (status) => {
  return filteredTasks.value.filter(t => t.status === status)
}

// 获取状态类型
const getStatusType = (status) => {
  const types = {
    pending: 'default',
    in_progress: 'primary',
    completed: 'success'
  }
  return types[status] || 'default'
}

// 获取优先级类型
const getPriorityType = (priority) => {
  const types = {
    high: 'danger',
    medium: 'warning',
    low: 'primary'
  }
  return types[priority] || 'default'
}

// 是否过期
const isOverdue = (dueDate) => {
  return new Date(dueDate) < new Date()
}

// 切换任务状态
const toggleTaskStatus = async (task) => {
  const newStatus = task.status === 'completed' ? 'todo' : 'completed'
  try {
    await taskService.update(task.id, { status: newStatus })
    task.status = newStatus
    task.statusText = getStatusText(newStatus)
    MessagePlugin.success(newStatus === 'completed' ? '任务已完成' : '任务已重置')
  } catch (error) {
    console.error('更新任务状态失败:', error)
    MessagePlugin.error('更新失败，请重试')
  }
}

// 编辑任务
const handleEdit = (task) => {
  editingTask.value = task
  formData.value = { ...task }
  showCreateDialog.value = true
}

// 删除任务
const handleDelete = (task) => {
  deletingTask.value = task
  showDeleteDialog.value = true
}

// 确认删除
const confirmDelete = async () => {
  try {
    await taskService.delete(deletingTask.value.id)
    tasks.value = tasks.value.filter(t => t.id !== deletingTask.value.id)
    showDeleteDialog.value = false
    MessagePlugin.success('任务已删除')
  } catch (error) {
    console.error('删除任务失败:', error)
    MessagePlugin.error('删除失败，请重试')
  }
}

// 提交表单
const handleSubmit = async () => {
  const result = await formRef.value.validate()
  if (result === true) {
    try {
      const project = projects.value.find(p => p.name === formData.value.project)
      const member = members.value.find(m => m.name === formData.value.assignee)

      if (editingTask.value) {
        // 更新任务
        await taskService.update(editingTask.value.id, {
          title: formData.value.title,
          description: formData.value.description,
          category: formData.value.project,
          priority: formData.value.priority,
          status: formData.value.status === 'pending' ? 'todo' : formData.value.status,
          planEndDate: formData.value.dueDate ? new Date(formData.value.dueDate) : null
        })
        
        const index = tasks.value.findIndex(t => t.id === editingTask.value.id)
        if (index !== -1) {
          tasks.value[index] = {
            ...tasks.value[index],
            ...formData.value,
            statusText: getStatusText(formData.value.status),
            priorityText: getPriorityText(formData.value.priority),
            projectColor: project?.color || '#2196F3',
            assigneeColor: member?.color || '#2196F3'
          }
        }
        MessagePlugin.success('任务已更新')
      } else {
        // 创建任务
        const newTask = await taskService.create({
          title: formData.value.title,
          description: formData.value.description,
          category: formData.value.project || 'dev',
          priority: formData.value.priority || 'medium',
          status: formData.value.status === 'pending' ? 'todo' : formData.value.status,
          projectId: project?.id || 0,
          planEndDate: formData.value.dueDate ? new Date(formData.value.dueDate) : null
        })
        
        tasks.value.unshift({
          id: newTask.id,
          title: newTask.title,
          description: newTask.description || '',
          status: newTask.status,
          statusText: getStatusText(newTask.status),
          priority: newTask.priority,
          priorityText: getPriorityText(newTask.priority),
          project: project?.name || '未分配',
          projectColor: project?.color || '#2196F3',
          assignee: member?.name || '未分配',
          assigneeColor: member?.color || '#9CA3AF',
          dueDate: formData.value.dueDate || '未设置',
          subtaskCompleted: 0,
          subtaskTotal: 0
        })
        MessagePlugin.success('任务已创建')
      }
      showCreateDialog.value = false
      editingTask.value = null
      formData.value = {
        title: '', description: '', project: '', assignee: '',
        status: 'todo', priority: 'medium', dueDate: ''
      }
    } catch (error) {
      console.error('保存任务失败:', error)
      MessagePlugin.error('保存失败，请重试')
    }
  }
}

// 拖拽
const handleDragStart = (event, task) => {
  draggedTask.value = task
  event.dataTransfer.effectAllowed = 'move'
}

const handleDrop = async (event, newStatus) => {
  dragOverColumn.value = null
  if (draggedTask.value) {
    const task = draggedTask.value
    try {
      // 如果拖到"已完成"列，自动设置进度为100%
      const updateData = {
        status: newStatus === 'pending' ? 'todo' : newStatus
      }
      if (newStatus === 'completed') {
        updateData.progress = 100
      }
      await taskService.update(task.id, updateData)
      task.status = newStatus
      task.statusText = getStatusText(newStatus)
      if (newStatus === 'completed') {
        task.progress = 100
        MessagePlugin.success('任务已完成')
      }
    } catch (error) {
      console.error('更新任务状态失败:', error)
      MessagePlugin.error('更新失败，请重试')
    }
    draggedTask.value = null
  }
}

// 进度条拖拽
const startProgressDrag = (event, task) => {
  event.preventDefault()
  event.stopPropagation()
  draggingProgressTask.value = task
  progressDragStartX.value = event.clientX
  progressDragStartValue.value = task.progress

  document.addEventListener('mousemove', onProgressDrag)
  document.addEventListener('mouseup', onProgressDragEnd)
}

const onProgressDrag = (event) => {
  if (!draggingProgressTask.value) return

  const task = draggingProgressTask.value
  const progressBar = document.querySelector(`[data-task-id="${task.id}"] .task-progress-bar`)
  if (!progressBar) return

  const barRect = progressBar.getBoundingClientRect()
  const deltaX = event.clientX - progressDragStartX.value
  const deltaPercent = Math.round((deltaX / barRect.width) * 100)
  const newProgress = Math.max(0, Math.min(100, progressDragStartValue.value + deltaPercent))

  task.progress = newProgress

  // 如果拖到100%，自动标记为已完成
  if (newProgress === 100 && task.status !== 'completed') {
    task.status = 'completed'
    task.statusText = getStatusText('completed')
  } else if (newProgress < 100 && task.status === 'completed') {
    task.status = 'in_progress'
    task.statusText = getStatusText('in_progress')
  }
}

const onProgressDragEnd = async () => {
  if (!draggingProgressTask.value) return

  const task = draggingProgressTask.value
  try {
    await taskService.update(task.id, {
      progress: task.progress,
      status: task.status === 'pending' ? 'todo' : task.status
    })
  } catch (error) {
    console.error('更新进度失败:', error)
    MessagePlugin.error('更新进度失败')
  }

  draggingProgressTask.value = null
  document.removeEventListener('mousemove', onProgressDrag)
  document.removeEventListener('mouseup', onProgressDragEnd)
}

// 快速添加
const quickAddTask = (status) => {
  formData.value.status = status === 'pending' ? 'todo' : status
  showCreateDialog.value = true
}

// 图标
import {
  AddIcon, SearchIcon, ViewListIcon, MoreIcon, EditIcon, DeleteIcon,
  CalendarIcon, TaskIcon, TimeIcon, PlayIcon, CheckCircleIcon,
  CheckIcon, ViewColumnIcon
} from 'tdesign-icons-vue-next'

// 导入 API 服务和 dayjs
import { taskService, projectService } from '@/services/dataService'
import dayjs from 'dayjs'

onMounted(() => {
  loadTasks()
  loadProjects()
})

onUnmounted(() => {
  document.removeEventListener('mousemove', onProgressDrag)
  document.removeEventListener('mouseup', onProgressDragEnd)
})
</script>

<style scoped>
.tasks-page {
  max-width: 1600px;
  margin: 0 auto;
}

/* 页面头部 */
.page-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: 24px;
  animation: fadeInUp 0.5s ease;
}

.header-content {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.page-title {
  font-size: 28px;
  font-weight: 700;
  color: var(--text-primary);
  margin: 0;
}

.page-subtitle {
  font-size: 14px;
  color: var(--text-secondary);
  margin: 0;
}

/* 任务统计 */
.task-stats {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 16px;
  margin-bottom: 24px;
}

.stat-card {
  display: flex;
  align-items: center;
  gap: 16px;
  padding: 20px;
  background: var(--bg-card-solid);
  border-radius: var(--radius-xl);
  border: 1px solid var(--border-light);
  animation: fadeInUp 0.5s ease backwards;
  transition: all var(--transition-normal);
  box-shadow: var(--shadow-card);
}

.stat-card:hover {
  transform: translateY(-2px);
  box-shadow: var(--shadow-md);
}

.stat-icon {
  width: 48px;
  height: 48px;
  border-radius: var(--radius-lg);
  background: color-mix(in srgb, var(--accent-color) 15%, transparent);
  color: var(--accent-color);
  display: flex;
  align-items: center;
  justify-content: center;
}

.stat-content {
  display: flex;
  flex-direction: column;
}

.stat-value {
  font-size: 28px;
  font-weight: 700;
  color: var(--text-primary);
  line-height: 1;
}

.stat-label {
  font-size: 13px;
  color: var(--text-secondary);
  margin-top: 4px;
}

/* 筛选栏 */
.filter-bar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 16px;
  margin-bottom: 20px;
  animation: fadeInUp 0.5s ease 0.1s backwards;
}

.filter-left {
  display: flex;
  gap: 12px;
  flex: 1;
}

.filter-right {
  display: flex;
  gap: 8px;
}

/* 看板视图 */
.task-board {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 20px;
  min-height: 500px;
}

.board-column {
  display: flex;
  flex-direction: column;
  background: var(--bg-color-secondary);
  border-radius: var(--radius-xl);
  padding: 16px;
  border: 2px solid transparent;
  transition: border-color 0.2s ease, background 0.2s ease;
}

.board-column.drop-target {
  border-color: var(--column-color);
  background: color-mix(in srgb, var(--column-color) 8%, var(--bg-color-secondary));
}

.column-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 16px;
  padding: 0 4px;
}

.column-title {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 14px;
  font-weight: 600;
  color: var(--text-primary);
}

.column-dot {
  width: 8px;
  height: 8px;
  border-radius: var(--radius-full);
  background: var(--column-color);
}

.column-count {
  font-size: 12px;
  font-weight: 600;
  color: var(--text-secondary);
  background: var(--bg-container);
  padding: 2px 8px;
  border-radius: var(--radius-full);
}

.column-content {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.task-card {
  background: var(--bg-card-solid);
  border-radius: var(--radius-lg);
  padding: 16px;
  border: 1px solid var(--border-light);
  cursor: grab;
  transition: all var(--transition-normal);
  animation: fadeInUp 0.4s ease backwards;
  box-shadow: var(--shadow-sm);
}

.task-card:hover {
  border-color: var(--primary-color);
  box-shadow: var(--shadow-md);
  transform: translateY(-2px);
}

.task-card:active {
  cursor: grabbing;
  transform: scale(0.98);
}

.task-card.is-dragging {
  opacity: 0.5;
  transform: scale(0.95);
}

.task-card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 12px;
}

.task-title {
  font-size: 14px;
  font-weight: 600;
  color: var(--text-primary);
  margin: 0 0 8px 0;
  line-height: 1.4;
}

.task-description {
  font-size: 12px;
  color: var(--text-secondary);
  margin: 0 0 12px 0;
  line-height: 1.5;
}

.task-meta {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 12px;
  padding-bottom: 12px;
  border-bottom: 1px solid var(--border-light);
}

.task-project {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 12px;
  color: var(--text-secondary);
}

.project-dot {
  width: 6px;
  height: 6px;
  border-radius: var(--radius-full);
}

.task-date {
  display: flex;
  align-items: center;
  gap: 4px;
  font-size: 12px;
  color: var(--text-tertiary);
}

.task-date svg {
  width: 12px;
  height: 12px;
}

.task-progress-bar {
  height: 8px;
  background: var(--bg-color-secondary);
  border-radius: var(--radius-full);
  margin-bottom: 4px;
  overflow: visible;
  position: relative;
  cursor: pointer;
}

.task-progress-fill {
  height: 100%;
  background: var(--gradient-primary);
  border-radius: var(--radius-full);
  transition: width 0.15s ease, background 0.3s ease;
}

.task-progress-fill.dragging {
  transition: none;
}

.task-progress-fill.completed {
  background: linear-gradient(90deg, #10B981, #34D399);
}

.progress-handle {
  position: absolute;
  top: 50%;
  transform: translate(-50%, -50%);
  width: 14px;
  height: 14px;
  background: var(--bg-card-solid);
  border: 2px solid var(--primary-color);
  border-radius: var(--radius-full);
  cursor: grab;
  opacity: 0;
  transition: opacity 0.15s ease, transform 0.15s ease;
  z-index: 2;
  box-shadow: var(--shadow-sm);
}

.task-progress-bar:hover .progress-handle,
.progress-handle.active {
  opacity: 1;
}

.progress-handle.active {
  cursor: grabbing;
  transform: translate(-50%, -50%) scale(1.2);
  box-shadow: var(--shadow-md);
}

.task-progress-text {
  font-size: 11px;
  color: var(--text-tertiary);
  text-align: right;
  cursor: pointer;
}

.task-footer {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.task-category {
  display: flex;
  align-items: center;
}

.task-hours {
  font-size: 12px;
  color: var(--text-tertiary);
}

.task-assignee {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 12px;
  color: var(--text-secondary);
}

.assignee-avatar {
  width: 24px;
  height: 24px;
  border-radius: var(--radius-full);
  display: flex;
  align-items: center;
  justify-content: center;
  color: white;
  font-size: 10px;
  font-weight: 600;
}

.task-subtasks {
  display: flex;
  align-items: center;
  gap: 4px;
  font-size: 12px;
  color: var(--text-tertiary);
}

.task-subtasks svg {
  width: 14px;
  height: 14px;
}

.column-empty {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 13px;
  color: var(--text-tertiary);
  min-height: 100px;
}

.column-add {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 6px;
  padding: 12px;
  border-radius: var(--radius-lg);
  border: 2px dashed var(--border-color);
  color: var(--text-tertiary);
  font-size: 13px;
  cursor: pointer;
  transition: all var(--transition-fast);
}

.column-add:hover {
  border-color: var(--primary-color);
  color: var(--primary-color);
  background: var(--primary-lighter);
}

/* 列表视图 */
.task-list-view {
  background: var(--bg-card-solid);
  border-radius: var(--radius-xl);
  overflow: hidden;
  border: 1px solid var(--border-light);
  animation: fadeInUp 0.5s ease;
  box-shadow: var(--shadow-card);
}

.task-title-cell {
  display: flex;
  align-items: center;
  gap: 12px;
}

.title-text {
  font-weight: 500;
  color: var(--text-primary);
}

.title-text.completed {
  text-decoration: line-through;
  color: var(--text-tertiary);
}

.overdue {
  color: var(--error-color);
}

.assignee-cell {
  display: flex;
  align-items: center;
  gap: 8px;
}

.assignee-avatar-sm {
  width: 24px;
  height: 24px;
  border-radius: var(--radius-full);
  display: flex;
  align-items: center;
  justify-content: center;
  color: white;
  font-size: 10px;
  font-weight: 600;
}

.progress-cell {
  display: flex;
  align-items: center;
  gap: 8px;
}

.progress-bar-sm {
  width: 60px;
  height: 4px;
  background: var(--bg-color-secondary);
  border-radius: var(--radius-full);
  overflow: hidden;
}

.progress-fill {
  height: 100%;
  background: var(--gradient-primary);
  border-radius: var(--radius-full);
  transition: width 0.3s ease, background 0.3s ease;
}

.progress-fill.completed {
  background: linear-gradient(90deg, #10B981, #34D399);
}

.action-buttons {
  display: flex;
  gap: 4px;
  justify-content: center;
}

/* 表单 */
.form-row {
  display: flex;
  gap: 16px;
}

.flex-1 {
  flex: 1;
}

.form-actions {
  display: flex;
  justify-content: flex-end;
  gap: 12px;
  margin-top: 24px;
  padding-top: 24px;
  border-top: 1px solid var(--border-light);
}

/* 删除确认 */
.delete-confirm {
  text-align: center;
}

.delete-text {
  font-size: 14px;
  color: var(--text-secondary);
  margin: 0 0 24px 0;
}

.delete-actions {
  display: flex;
  justify-content: center;
  gap: 12px;
}

/* 过渡 */
.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.3s ease;
}

.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}

/* 响应式 */
@media (max-width: 1200px) {
  .task-board {
    grid-template-columns: repeat(2, 1fr);
  }
}

@media (max-width: 768px) {
  .task-stats {
    grid-template-columns: repeat(2, 1fr);
  }

  .task-board {
    grid-template-columns: 1fr;
  }

  .filter-bar {
    flex-direction: column;
    align-items: stretch;
  }

  .filter-left {
    flex-wrap: wrap;
  }
}
</style>
