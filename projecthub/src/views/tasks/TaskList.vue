<template>
  <div class="task-page">
    <!-- 页面头部 -->
    <div class="page-header fade-in">
      <div class="page-header-left">
        <h2 class="page-title">任务管理</h2>
        <span class="page-subtitle">共 {{ filteredTasks.length }} 个任务</span>
      </div>
      <div class="page-header-right">
        <t-button theme="primary" @click="showCreateDialog = true">
          <template #icon>
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <line x1="12" y1="5" x2="12" y2="19"/><line x1="5" y1="12" x2="19" y2="12"/>
            </svg>
          </template>
          新建任务
        </t-button>
      </div>
    </div>

    <!-- 筛选栏 -->
    <div class="filter-bar fade-in stagger-1">
      <div class="filter-tabs">
        <button
          v-for="tab in statusTabs"
          :key="tab.value"
          class="filter-tab"
          :class="{ active: filterStatus === tab.value }"
          @click="filterStatus = tab.value"
        >
          {{ tab.label }}
          <span class="tab-count" v-if="tab.count">{{ tab.count }}</span>
        </button>
      </div>
      <div class="filter-search">
        <t-input v-model="searchQuery" placeholder="搜索任务..." clearable>
          <template #prefix-icon>
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <circle cx="11" cy="11" r="8"/><path d="m21 21-4.35-4.35"/>
            </svg>
          </template>
        </t-input>
      </div>
    </div>

    <!-- 任务列表 -->
    <div class="task-list">
      <div
        v-for="(task, index) in filteredTasks"
        :key="task.id"
        class="task-card fade-in-up"
        :style="{ animationDelay: `${0.05 * index}s` }"
        @click="openTaskDetail(task)"
      >
        <div class="task-card-main">
          <t-checkbox
            :checked="task.status === 'completed'"
            @change="() => toggleStatus(task)"
            @click.stop
          />
          <div class="task-info">
            <span class="task-title" :class="{ completed: task.status === 'completed' }">
              {{ task.title }}
            </span>
            <div class="task-meta">
              <t-tag :theme="getProjectTheme(task.projectType)" variant="light" size="small">
                {{ task.projectName }}
              </t-tag>
              <span class="task-due" :class="{ overdue: isOverdue(task) }">
                <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <rect x="3" y="4" width="18" height="18" rx="2" ry="2"/><line x1="16" y1="2" x2="16" y2="6"/><line x1="8" y1="2" x2="8" y2="6"/><line x1="3" y1="10" x2="21" y2="10"/>
                </svg>
                {{ formatDate(task.dueDate) }}
              </span>
            </div>
          </div>
        </div>
        <div class="task-card-right">
          <t-tag :theme="getPriorityTheme(task.priority)" variant="light" size="small">
            {{ getPriorityLabel(task.priority) }}
          </t-tag>
          <t-tag v-if="task.categoryName" :color="task.categoryColor" variant="light" size="small">
            {{ task.categoryName }}
          </t-tag>
        </div>
        <div class="task-card-actions">
          <t-button variant="text" size="small" @click.stop="editTask(task)">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"/><path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"/>
            </svg>
          </t-button>
          <t-button variant="text" size="small" @click.stop="deleteTask(task)">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <polyline points="3 6 5 6 21 6"/><path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"/>
            </svg>
          </t-button>
        </div>
      </div>

      <!-- 空状态 -->
      <div v-if="filteredTasks.length === 0" class="empty-state fade-in">
        <svg width="64" height="64" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
          <path d="M9 11l3 3L22 4"/>
          <path d="M21 12v7a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11"/>
        </svg>
        <p>{{ searchQuery ? '没有找到匹配的任务' : '暂无任务' }}</p>
        <t-button theme="primary" variant="outline" @click="showCreateDialog = true">
          创建第一个任务
        </t-button>
      </div>
    </div>

    <!-- 创建任务弹窗 -->
    <t-dialog
      v-model:visible="showCreateDialog"
      header="新建任务"
      :footer="false"
      width="500px"
      :close-on-overlay-click="true"
    >
      <div class="form-group">
        <label>任务标题</label>
        <t-input v-model="newTask.title" placeholder="请输入任务标题" />
      </div>
      <div class="form-row">
        <div class="form-group">
          <label>所属项目</label>
          <t-select v-model="newTask.projectId" placeholder="选择项目" :options="projectOptions" />
        </div>
        <div class="form-group">
          <label>优先级</label>
          <t-select v-model="newTask.priority" placeholder="选择优先级" :options="priorityOptions" />
        </div>
      </div>
      <div class="form-row">
        <div class="form-group">
          <label>截止日期</label>
          <t-date-picker v-model="newTask.dueDate" placeholder="选择日期" />
        </div>
        <div class="form-group">
          <label>分类</label>
          <t-select v-model="newTask.categoryId" placeholder="选择分类" :options="categoryOptions" />
        </div>
      </div>
      <div class="form-group">
        <label>描述</label>
        <t-textarea v-model="newTask.description" placeholder="任务描述（可选）" />
      </div>
      <div class="form-actions">
        <t-button variant="outline" @click="showCreateDialog = false">取消</t-button>
        <t-button theme="primary" @click="createTask">创建</t-button>
      </div>
    </t-dialog>

    <!-- 编辑任务弹窗 -->
    <t-dialog
      v-model:visible="showEditDialog"
      header="编辑任务"
      :footer="false"
      width="500px"
    >
      <div class="form-group">
        <label>任务标题</label>
        <t-input v-model="editForm.title" placeholder="请输入任务标题" />
      </div>
      <div class="form-row">
        <div class="form-group">
          <label>所属项目</label>
          <t-select v-model="editForm.projectId" placeholder="选择项目" :options="projectOptions" />
        </div>
        <div class="form-group">
          <label>优先级</label>
          <t-select v-model="editForm.priority" placeholder="选择优先级" :options="priorityOptions" />
        </div>
      </div>
      <div class="form-row">
        <div class="form-group">
          <label>截止日期</label>
          <t-date-picker v-model="editForm.dueDate" placeholder="选择日期" />
        </div>
        <div class="form-group">
          <label>状态</label>
          <t-select v-model="editForm.status" placeholder="选择状态" :options="statusOptions" />
        </div>
      </div>
      <div class="form-group">
        <label>描述</label>
        <t-textarea v-model="editForm.description" placeholder="任务描述（可选）" />
      </div>
      <div class="form-actions">
        <t-button variant="outline" @click="showEditDialog = false">取消</t-button>
        <t-button theme="primary" @click="saveTask">保存</t-button>
      </div>
    </t-dialog>

    <!-- 任务详情弹窗 -->
    <t-dialog
      v-model:visible="showDetailDialog"
      header="任务详情"
      :footer="false"
      width="600px"
    >
      <div v-if="currentTask" class="task-detail">
        <div class="detail-header">
          <h3>{{ currentTask.title }}</h3>
          <t-tag :theme="getPriorityTheme(currentTask.priority)" variant="light">
            {{ getPriorityLabel(currentTask.priority) }}
          </t-tag>
        </div>
        <div class="detail-meta">
          <div class="meta-item">
            <span class="meta-label">所属项目：</span>
            <span>{{ currentTask.projectName }}</span>
          </div>
          <div class="meta-item">
            <span class="meta-label">状态：</span>
            <t-tag :theme="getStatusTheme(currentTask.status)" variant="light">
              {{ getStatusLabel(currentTask.status) }}
            </t-tag>
          </div>
          <div class="meta-item">
            <span class="meta-label">创建时间：</span>
            <span>{{ formatDateTime(currentTask.createdAt) }}</span>
          </div>
        </div>
        <div v-if="currentTask.description" class="detail-description">
          <h4>描述</h4>
          <p>{{ currentTask.description }}</p>
        </div>
        <div class="detail-actions">
          <t-button theme="primary" @click="startEditTask">
            <template #icon>
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"/>
                <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"/>
              </svg>
            </template>
            编辑
          </t-button>
        </div>
      </div>
    </t-dialog>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { MessagePlugin } from 'tdesign-vue-next'
import dayjs from 'dayjs'

const tasks = ref([])
const projects = ref([])
const categories = ref([])
const filterStatus = ref('all')
const searchQuery = ref('')
const showCreateDialog = ref(false)
const showEditDialog = ref(false)
const showDetailDialog = ref(false)
const currentTask = ref(null)

const newTask = ref({
  title: '',
  projectId: null,
  priority: 'medium',
  dueDate: null,
  categoryId: null,
  description: ''
})

const editForm = ref({
  id: null,
  title: '',
  projectId: null,
  priority: 'medium',
  status: 'pending',
  dueDate: null,
  description: ''
})

const statusOptions = [
  { value: 'pending', label: '待处理' },
  { value: 'in_progress', label: '进行中' },
  { value: 'completed', label: '已完成' },
  { value: 'overdue', label: '已延期' }
]

const statusTabs = computed(() => [
  { label: '全部', value: 'all', count: tasks.value.length },
  { label: '待处理', value: 'pending', count: tasks.value.filter(t => t.status === 'pending').length },
  { label: '进行中', value: 'in_progress', count: tasks.value.filter(t => t.status === 'in_progress').length },
  { label: '已完成', value: 'completed', count: tasks.value.filter(t => t.status === 'completed').length },
  { label: '已延期', value: 'overdue', count: tasks.value.filter(t => t.status === 'overdue').length }
])

const filteredTasks = computed(() => {
  let result = tasks.value
  
  if (filterStatus.value !== 'all') {
    result = result.filter(t => t.status === filterStatus.value)
  }
  
  if (searchQuery.value) {
    const query = searchQuery.value.toLowerCase()
    result = result.filter(t => 
      t.title.toLowerCase().includes(query) ||
      t.projectName?.toLowerCase().includes(query)
    )
  }
  
  return result
})

const projectOptions = computed(() =>
  projects.value.map(p => ({ value: p.id, label: p.name }))
)

const priorityOptions = [
  { value: 'high', label: '高优先级' },
  { value: 'medium', label: '中优先级' },
  { value: 'low', label: '低优先级' }
]

const categoryOptions = computed(() =>
  categories.value.map(c => ({ value: c.id, label: c.name }))
)

const fetchData = async () => {
  try {
    const [tasksRes, projectsRes, categoriesRes] = await Promise.all([
      fetch('/api/tasks'),
      fetch('/api/projects'),
      fetch('/api/categories')
    ])
    
    if (tasksRes.ok) tasks.value = await tasksRes.json()
    if (projectsRes.ok) projects.value = await projectsRes.json()
    if (categoriesRes.ok) categories.value = await categoriesRes.json()
  } catch (error) {
    console.error('Failed to fetch data:', error)
  }
}

const createTask = async () => {
  if (!newTask.value.title) {
    MessagePlugin.warning('请输入任务标题')
    return
  }
  
  try {
    const res = await fetch('/api/tasks', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        ...newTask.value,
        status: 'pending'
      })
    })
    
    if (res.ok) {
      const task = await res.json()
      tasks.value.unshift(task)
      showCreateDialog.value = false
      newTask.value = { title: '', projectId: null, priority: 'medium', dueDate: null, categoryId: null, description: '' }
      MessagePlugin.success('任务创建成功')
    }
  } catch (error) {
    MessagePlugin.error('创建失败')
  }
}

const toggleStatus = async (task) => {
  const newStatus = task.status === 'completed' ? 'pending' : 'completed'
  try {
    await fetch(`/api/tasks/${task.id}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ ...task, status: newStatus })
    })
    task.status = newStatus
    MessagePlugin.success(newStatus === 'completed' ? '任务已完成' : '任务已重置')
  } catch (error) {
    MessagePlugin.error('操作失败')
  }
}

const editTask = (task) => {
  editForm.value = {
    id: task.id,
    title: task.title,
    projectId: task.projectId,
    priority: task.priority,
    status: task.status,
    dueDate: task.dueDate,
    description: task.description || ''
  }
  showEditDialog.value = true
}

const openTaskDetail = async (task) => {
  try {
    const res = await fetch(`/api/tasks/${task.id}`)
    if (res.ok) {
      currentTask.value = await res.json()
      showDetailDialog.value = true
    }
  } catch (error) {
    MessagePlugin.error('获取任务详情失败')
  }
}

const saveTask = async () => {
  if (!editForm.value.title) {
    MessagePlugin.warning('请输入任务标题')
    return
  }
  
  try {
    const res = await fetch(`/api/tasks/${editForm.value.id}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(editForm.value)
    })
    
    if (res.ok) {
      const updated = await res.json()
      const index = tasks.value.findIndex(t => t.id === updated.id)
      if (index !== -1) {
        tasks.value[index] = { ...tasks.value[index], ...updated }
      }
      showEditDialog.value = false
      showDetailDialog.value = false
      MessagePlugin.success('任务已更新')
    }
  } catch (error) {
    MessagePlugin.error('保存失败')
  }
}

const startEditTask = () => {
  if (currentTask.value) {
    editForm.value = {
      id: currentTask.value.id,
      title: currentTask.value.title,
      projectId: currentTask.value.projectId,
      priority: currentTask.value.priority,
      status: currentTask.value.status,
      dueDate: currentTask.value.dueDate || currentTask.value.planEndDate,
      description: currentTask.value.description || ''
    }
    showDetailDialog.value = false
    showEditDialog.value = true
  }
}

const deleteTask = async (task) => {
  try {
    await fetch(`/api/tasks/${task.id}`, { method: 'DELETE' })
    tasks.value = tasks.value.filter(t => t.id !== task.id)
    MessagePlugin.success('任务已删除')
  } catch (error) {
    MessagePlugin.error('删除失败')
  }
}

const formatDate = (date) => {
  return date ? dayjs(date).format('MM/DD') : '-'
}

const formatDateTime = (date) => {
  return date ? dayjs(date).format('YYYY-MM-DD HH:mm') : '-'
}

const isOverdue = (task) => {
  return task.dueDate && dayjs(task.dueDate).isBefore(dayjs(), 'day') && task.status !== 'completed'
}

const getPriorityTheme = (priority) => {
  return { high: 'danger', medium: 'warning', low: 'success' }[priority] || 'default'
}

const getPriorityLabel = (priority) => {
  return { high: '高', medium: '中', low: '低' }[priority] || '普通'
}

const getStatusTheme = (status) => {
  return { pending: 'default', in_progress: 'primary', completed: 'success', overdue: 'warning' }[status] || 'default'
}

const getStatusLabel = (status) => {
  return { pending: '待处理', in_progress: '进行中', completed: '已完成', overdue: '已延期' }[status] || status
}

const getProjectTheme = (type) => {
  return { web: 'primary', mobile: 'success', design: 'warning' }[type] || 'default'
}

onMounted(() => {
  fetchData()
})
</script>

<style scoped>
.task-page {
  animation: fadeIn 0.3s ease-out;
}

.page-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: var(--space-5);
}

.page-header-left {
  display: flex;
  align-items: baseline;
  gap: var(--space-3);
}

.page-title {
  font-size: var(--font-size-2xl);
  font-weight: var(--font-weight-semibold);
  color: var(--text-primary);
}

.page-subtitle {
  font-size: var(--font-size-sm);
  color: var(--text-tertiary);
}

/* 筛选栏 */
.filter-bar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: var(--space-3) var(--space-4);
  background: var(--bg-secondary);
  border-radius: var(--radius-xl);
  border: 1px solid var(--border-light);
  margin-bottom: var(--space-5);
}

.filter-tabs {
  display: flex;
  gap: var(--space-1);
}

.filter-tab {
  padding: var(--space-2) var(--space-3);
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  color: var(--text-secondary);
  border-radius: var(--radius-lg);
  transition: all var(--transition-fast);
}

.filter-tab:hover {
  background: var(--gray-100);
  color: var(--text-primary);
}

.filter-tab.active {
  background: var(--primary-50);
  color: var(--primary-600);
}

.tab-count {
  margin-left: var(--space-1);
  font-size: var(--font-size-xs);
  opacity: 0.7;
}

.filter-search {
  width: 240px;
}

/* 任务卡片 */
.task-list {
  display: flex;
  flex-direction: column;
  gap: var(--space-3);
}

.task-card {
  display: flex;
  align-items: center;
  gap: var(--space-4);
  padding: var(--space-4);
  background: var(--bg-secondary);
  border-radius: var(--radius-xl);
  border: 1px solid var(--border-light);
  cursor: pointer;
  transition: all var(--transition-base);
  opacity: 0;
}

.task-card:hover {
  border-color: var(--gray-300);
  box-shadow: var(--shadow-sm);
  transform: translateX(4px);
}

.task-card-main {
  display: flex;
  align-items: center;
  gap: var(--space-3);
  flex: 1;
  min-width: 0;
}

.task-info {
  flex: 1;
  min-width: 0;
}

.task-title {
  display: block;
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  color: var(--text-primary);
  margin-bottom: var(--space-2);
}

.task-title.completed {
  text-decoration: line-through;
  color: var(--text-tertiary);
}

.task-meta {
  display: flex;
  align-items: center;
  gap: var(--space-3);
}

.task-due {
  display: flex;
  align-items: center;
  gap: var(--space-1);
  font-size: var(--font-size-xs);
  color: var(--text-tertiary);
}

.task-due.overdue {
  color: var(--error-500);
}

.task-card-right {
  display: flex;
  gap: var(--space-2);
}

.task-card-actions {
  display: flex;
  gap: var(--space-1);
  opacity: 0;
  transition: opacity var(--transition-fast);
}

.task-card:hover .task-card-actions {
  opacity: 1;
}

/* 表单 */
.form-group {
  margin-bottom: var(--space-4);
}

.form-group label {
  display: block;
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  color: var(--text-primary);
  margin-bottom: var(--space-2);
}

.form-row {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: var(--space-4);
}

.form-actions {
  display: flex;
  justify-content: flex-end;
  gap: var(--space-3);
  margin-top: var(--space-6);
}

/* 空状态 */
.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: var(--space-12) var(--space-4);
  text-align: center;
  color: var(--text-tertiary);
}

.empty-state svg {
  margin-bottom: var(--space-4);
  opacity: 0.4;
}

.empty-state p {
  margin-bottom: var(--space-4);
}

/* 任务详情弹窗 */
.task-detail {
  padding: var(--space-2);
}

.detail-header {
  display: flex;
  align-items: center;
  gap: var(--space-3);
  margin-bottom: var(--space-5);
  padding-bottom: var(--space-4);
  border-bottom: 1px solid var(--border-light);
}

.detail-header h3 {
  flex: 1;
  font-size: var(--font-size-lg);
  font-weight: var(--font-weight-semibold);
  color: var(--text-primary);
}

.detail-meta {
  display: flex;
  flex-wrap: wrap;
  gap: var(--space-4);
  margin-bottom: var(--space-5);
}

.detail-meta .meta-item {
  display: flex;
  align-items: center;
  gap: var(--space-2);
}

.detail-meta .meta-label {
  font-size: var(--font-size-sm);
  color: var(--text-tertiary);
}

.detail-meta span:not(.meta-label) {
  font-size: var(--font-size-sm);
  color: var(--text-primary);
}

.detail-description {
  margin-bottom: var(--space-5);
  padding: var(--space-4);
  background: var(--gray-50);
  border-radius: var(--radius-lg);
}

.detail-description h4 {
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  color: var(--text-secondary);
  margin-bottom: var(--space-2);
}

.detail-description p {
  font-size: var(--font-size-sm);
  color: var(--text-primary);
  line-height: var(--line-height-relaxed);
}

.detail-actions {
  display: flex;
  justify-content: flex-end;
  gap: var(--space-3);
}
</style>
