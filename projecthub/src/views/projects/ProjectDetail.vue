<template>
  <div class="project-detail-page">
    <!-- 新建项目表单 -->
    <div v-if="isNewMode" class="project-form">
      <div class="form-header">
        <h2>创建新项目</h2>
        <t-button variant="text" @click="goBack">
          <template #icon><ArrowLeftIcon /></template>
          返回
        </t-button>
      </div>

      <t-form :model="formData" :rules="formRules" ref="formRef" label-width="100px">
        <t-form-item label="项目名称" name="name">
          <t-input v-model="formData.name" placeholder="请输入项目名称" style="max-width: 400px" />
        </t-form-item>

        <t-form-item label="项目描述" name="description">
          <t-textarea v-model="formData.description" placeholder="请输入项目描述" style="max-width: 600px" :rows="3" />
        </t-form-item>

        <t-form-item label="项目类型" name="type">
          <t-select v-model="formData.type" placeholder="请选择项目类型" style="max-width: 200px">
            <t-option value="web" label="Web应用" />
            <t-option value="mobile" label="移动应用" />
            <t-option value="desktop" label="桌面应用" />
            <t-option value="ai" label="AI项目" />
            <t-option value="other" label="其他" />
          </t-select>
        </t-form-item>

        <t-form-item label="分类" name="categoryId">
          <t-select v-model="formData.categoryId" placeholder="请选择分类" style="max-width: 200px">
            <t-option v-for="cat in categories" :key="cat.id" :value="cat.id" :label="cat.name" />
          </t-select>
        </t-form-item>

        <t-form-item label="开始日期" name="startDate">
          <t-date-picker v-model="formData.startDate" style="max-width: 200px" />
        </t-form-item>

        <t-form-item label="截止日期" name="endDate">
          <t-date-picker v-model="formData.endDate" style="max-width: 200px" />
        </t-form-item>

        <t-form-item>
          <t-button theme="primary" @click="handleCreate" :loading="saving">
            创建项目
          </t-button>
          <t-button variant="outline" @click="goBack" style="margin-left: 12px">
            取消
          </t-button>
        </t-form-item>
      </t-form>
    </div>

    <!-- 项目详情 -->
    <template v-else>
      <!-- 加载状态 -->
      <div v-if="loading" class="loading-container">
        <t-loading size="large" text="加载中..." />
      </div>

      <!-- 项目头部 -->
      <div v-else class="project-header" :style="{ background: project.gradient }">
        <div class="header-content">
          <div class="project-info">
            <div class="project-icon">{{ project.name?.charAt(0) || 'P' }}</div>
            <div class="project-text">
              <h1 class="project-name">{{ project.name }}</h1>
              <p class="project-description">{{ project.description || '暂无描述' }}</p>
            </div>
          </div>
          <div class="header-actions">
            <t-button variant="outline" theme="default" @click="goEdit">
              <template #icon><EditIcon /></template>
              编辑
            </t-button>
            <t-button theme="primary" @click="showAddTaskDialog = true">
              <template #icon><AddIcon /></template>
              添加任务
            </t-button>
          </div>
        </div>
      </div>

      <!-- 项目统计 -->
      <div v-if="!loading" class="project-stats">
        <div class="stat-item">
          <span class="stat-value">{{ project.progress || 0 }}%</span>
          <span class="stat-label">完成度</span>
          <div class="stat-bar">
            <div class="stat-bar-fill" :style="{ width: (project.progress || 0) + '%', background: project.color || '#2196F3' }"></div>
          </div>
        </div>
        <div class="stat-item">
          <span class="stat-value">{{ stats.total }}</span>
          <span class="stat-label">总任务</span>
        </div>
        <div class="stat-item">
          <span class="stat-value">{{ stats.completed }}</span>
          <span class="stat-label">已完成</span>
        </div>
        <div class="stat-item">
          <span class="stat-value">{{ stats.members }}</span>
          <span class="stat-label">成员</span>
        </div>
        <div class="stat-item">
          <span class="stat-value">{{ stats.daysLeft }}</span>
          <span class="stat-label">剩余天数</span>
        </div>
      </div>

      <!-- 标签页 -->
      <t-tabs v-if="!loading" v-model="activeTab">
        <t-tab-panel value="tasks" label="任务列表">
          <div class="tasks-content">
            <div class="tasks-filters">
              <t-input v-model="taskFilter.search" placeholder="搜索任务..." style="width: 280px">
                <template #prefix-icon><SearchIcon /></template>
              </t-input>
              <t-select v-model="taskFilter.status" placeholder="状态筛选" style="width: 140px" clearable>
                <t-option value="pending" label="待开始" />
                <t-option value="in_progress" label="进行中" />
                <t-option value="completed" label="已完成" />
              </t-select>
              <t-select v-model="taskFilter.priority" placeholder="优先级" style="width: 140px" clearable>
                <t-option value="high" label="高" />
                <t-option value="medium" label="中" />
                <t-option value="low" label="低" />
              </t-select>
            </div>

            <!-- 空状态 -->
            <t-empty v-if="filteredTasks.length === 0" description="暂无任务，点击上方添加" />

            <!-- 任务列表 -->
            <t-table v-else :data="filteredTasks" row-key="id" hover stripe>
              <t-table-column title="任务" width="35%">
                <template #default="{ row }">
                  <div class="task-cell">
                    <t-checkbox :checked="row.status === 'completed'" @change="() => toggleTaskStatus(row)" />
                    <span :class="{ completed: row.status === 'completed' }">{{ row.title }}</span>
                  </div>
                </template>
              </t-table-column>
              <t-table-column title="负责人" width="15%">
                <template #default="{ row }">
                  <div class="assignee-cell">
                    <div class="avatar" :style="{ background: row.assigneeColor || '#2196F3' }">{{ (row.assigneeName || '未分配').charAt(0) }}</div>
                    {{ row.assigneeName || '未分配' }}
                  </div>
                </template>
              </t-table-column>
              <t-table-column title="优先级" width="12%">
                <template #default="{ row }">
                  <t-tag :type="getPriorityType(row.priority)" variant="light" size="small">{{ row.priorityText || row.priority }}</t-tag>
                </template>
              </t-table-column>
              <t-table-column title="截止日期" width="15%">
                <template #default="{ row }">
                  <span :class="{ overdue: isOverdue(row.planEndDate) }">{{ row.planEndDate || '-' }}</span>
                </template>
              </t-table-column>
              <t-table-column title="状态" width="12%">
                <template #default="{ row }">
                  <t-tag :type="getStatusType(row.status)" variant="light">{{ row.statusText }}</t-tag>
                </template>
              </t-table-column>
              <t-table-column title="操作" width="10%" align="center">
                <template #default="{ row }">
                  <t-button variant="text" size="small" @click="editTask(row)"><EditIcon /></t-button>
                  <t-button variant="text" size="small" @click="deleteTask(row.id)"><DeleteIcon /></t-button>
                </template>
              </t-table-column>
            </t-table>
          </div>
        </t-tab-panel>

        <t-tab-panel value="members" label="团队成员">
          <div class="members-content">
            <div v-if="members.length === 0" class="empty-tip">
              <t-empty description="暂无成员" />
            </div>
            <div class="member-card" v-for="member in members" :key="member.id">
              <div class="member-avatar" :style="{ background: member.color || '#2196F3' }">{{ member.name?.charAt(0) || '?' }}</div>
              <div class="member-info">
                <span class="member-name">{{ member.name }}</span>
                <span class="member-role">{{ member.role || '成员' }}</span>
              </div>
              <t-tag variant="outline">{{ member.taskCount || 0 }} 任务</t-tag>
            </div>
          </div>
        </t-tab-panel>
      </t-tabs>
    </template>

    <!-- 添加任务弹窗 -->
    <t-dialog v-model:visible="showAddTaskDialog" header="添加任务" width="600px" :footer="false">
      <t-form :model="taskForm" :rules="taskRules" ref="taskFormRef" label-width="80px">
        <t-form-item label="任务名称" name="title">
          <t-input v-model="taskForm.title" placeholder="请输入任务名称" />
        </t-form-item>
        <t-form-item label="任务描述" name="description">
          <t-textarea v-model="taskForm.description" placeholder="请输入任务描述" :rows="2" />
        </t-form-item>
        <t-form-item label="优先级" name="priority">
          <t-select v-model="taskForm.priority" placeholder="请选择优先级">
            <t-option value="high" label="高" />
            <t-option value="medium" label="中" />
            <t-option value="low" label="低" />
          </t-select>
        </t-form-item>
        <t-form-item label="开始日期" name="planStartDate">
          <t-date-picker v-model="taskForm.planStartDate" />
        </t-form-item>
        <t-form-item label="截止日期" name="planEndDate">
          <t-date-picker v-model="taskForm.planEndDate" />
        </t-form-item>
        <t-form-item>
          <t-button theme="primary" @click="handleAddTask" :loading="savingTask">添加</t-button>
          <t-button variant="outline" @click="showAddTaskDialog = false" style="margin-left: 12px">取消</t-button>
        </t-form-item>
      </t-form>
    </t-dialog>

    <!-- 编辑任务弹窗 -->
    <t-dialog v-model:visible="showEditTaskDialog" header="编辑任务" width="600px" :footer="false">
      <t-form :model="taskForm" :rules="taskRules" ref="taskFormRef2" label-width="80px">
        <t-form-item label="任务名称" name="title">
          <t-input v-model="taskForm.title" placeholder="请输入任务名称" />
        </t-form-item>
        <t-form-item label="任务描述" name="description">
          <t-textarea v-model="taskForm.description" placeholder="请输入任务描述" :rows="2" />
        </t-form-item>
        <t-form-item label="优先级" name="priority">
          <t-select v-model="taskForm.priority" placeholder="请选择优先级">
            <t-option value="high" label="高" />
            <t-option value="medium" label="中" />
            <t-option value="low" label="低" />
          </t-select>
        </t-form-item>
        <t-form-item label="状态" name="status">
          <t-select v-model="taskForm.status" placeholder="请选择状态">
            <t-option value="pending" label="待开始" />
            <t-option value="in_progress" label="进行中" />
            <t-option value="completed" label="已完成" />
          </t-select>
        </t-form-item>
        <t-form-item label="开始日期" name="planStartDate">
          <t-date-picker v-model="taskForm.planStartDate" />
        </t-form-item>
        <t-form-item label="截止日期" name="planEndDate">
          <t-date-picker v-model="taskForm.planEndDate" />
        </t-form-item>
        <t-form-item>
          <t-button theme="primary" @click="handleUpdateTask" :loading="savingTask">保存</t-button>
          <t-button variant="outline" @click="showEditTaskDialog = false" style="margin-left: 12px">取消</t-button>
        </t-form-item>
      </t-form>
    </t-dialog>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { MessagePlugin } from 'tdesign-vue-next'
import dayjs from 'dayjs'
import { projectService } from '@/services/dataService'

const route = useRoute()
const router = useRouter()

// 模式判断
const isNewMode = computed(() => route.path === '/projects/new')

// 状态
const loading = ref(false)
const saving = ref(false)
const savingTask = ref(false)
const project = ref({})
const tasks = ref([])
const members = ref([])
const categories = ref([])
const activeTab = ref('tasks')
const formRef = ref(null)
const taskFormRef = ref(null)
const taskFormRef2 = ref(null)

// 任务表单
const taskForm = ref({
  title: '',
  description: '',
  priority: 'medium',
  status: 'pending',
  planStartDate: null,
  planEndDate: null
})
const taskRules = {
  title: [{ required: true, message: '请输入任务名称', trigger: 'blur' }]
}

// 新建表单
const formData = ref({
  name: '',
  description: '',
  type: 'web',
  categoryId: null,
  startDate: null,
  endDate: null
})
const formRules = {
  name: [{ required: true, message: '请输入项目名称', trigger: 'blur' }]
}

// 任务筛选
const taskFilter = ref({
  search: '',
  status: null,
  priority: null
})

// 弹窗
const showAddTaskDialog = ref(false)
const showEditTaskDialog = ref(false)
const editingTaskId = ref(null)

// 计算属性
const filteredTasks = computed(() => {
  return tasks.value.filter(task => {
    if (taskFilter.value.search && !task.title?.toLowerCase().includes(taskFilter.value.search.toLowerCase())) {
      return false
    }
    if (taskFilter.value.status && task.status !== taskFilter.value.status) {
      return false
    }
    if (taskFilter.value.priority && task.priority !== taskFilter.value.priority) {
      return false
    }
    return true
  })
})

const stats = computed(() => {
  const total = tasks.value.length
  const completed = tasks.value.filter(t => t.status === 'completed').length
  const progress = total > 0 ? Math.round((completed / total) * 100) : 0
  const endDate = project.value.endDate ? dayjs(project.value.endDate) : dayjs()
  const daysLeft = Math.max(0, endDate.diff(dayjs(), 'day'))
  return {
    total,
    completed,
    progress,
    members: members.value.length,
    daysLeft
  }
})

// 加载项目数据
const loadProject = async () => {
  if (isNewMode.value) {
    await loadCategories()
    return
  }

  loading.value = true
  try {
    const projectId = route.params.id
    const [projectData, tasksData] = await Promise.all([
      projectService.getById(projectId),
      projectService.getTasks(projectId).catch(() => [])
    ])
    
    project.value = projectData
    tasks.value = (tasksData || []).map(t => ({
      ...t,
      priorityText: { high: '高', medium: '中', low: '低' }[t.priority] || t.priority,
      statusText: { pending: '待开始', in_progress: '进行中', completed: '已完成' }[t.status] || t.status
    }))
    
    // 计算项目进度
    if (project.value) {
      project.value.progress = stats.value.progress
    }
  } catch (error) {
    console.error('加载项目失败:', error)
    MessagePlugin.error('加载项目失败')
  } finally {
    loading.value = false
  }
}

// 加载分类
const loadCategories = async () => {
  try {
    const data = await fetch('http://localhost:5000/api/categories').then(r => r.json())
    categories.value = Array.isArray(data) ? data : (data.items || [])
  } catch (error) {
    console.error('加载分类失败:', error)
  }
}

// 创建项目
const handleCreate = async () => {
  try {
    await formRef.value.validate()
  } catch {
    return
  }

  saving.value = true
  try {
    const newProject = await projectService.create({
      name: formData.value.name,
      description: formData.value.description,
      type: formData.value.type,
      categoryId: formData.value.categoryId,
      startDate: formData.value.startDate ? dayjs(formData.value.startDate).format('YYYY-MM-DD') : null,
      endDate: formData.value.endDate ? dayjs(formData.value.endDate).format('YYYY-MM-DD') : null
    })
    MessagePlugin.success('项目创建成功')
    router.push(`/projects/${newProject.id}`)
  } catch (error) {
    console.error('创建项目失败:', error)
    MessagePlugin.error('创建项目失败')
  } finally {
    saving.value = false
  }
}

// 返回列表
const goBack = () => {
  router.push('/projects')
}

// 编辑项目
const goEdit = () => {
  // 可以扩展为编辑模式
  MessagePlugin.info('编辑功能开发中')
}

// 添加任务
const handleAddTask = async () => {
  try {
    await taskFormRef.value.validate()
  } catch {
    return
  }

  savingTask.value = true
  try {
    await projectService.createTask(route.params.id, {
      title: taskForm.value.title,
      description: taskForm.value.description,
      priority: taskForm.value.priority,
      status: 'pending',
      planStartDate: taskForm.value.planStartDate ? dayjs(taskForm.value.planStartDate).format('YYYY-MM-DD') : null,
      planEndDate: taskForm.value.planEndDate ? dayjs(taskForm.value.planEndDate).format('YYYY-MM-DD') : null
    })
    MessagePlugin.success('任务添加成功')
    showAddTaskDialog.value = false
    taskForm.value = { title: '', description: '', priority: 'medium', status: 'pending', planStartDate: null, planEndDate: null }
    await loadProject()
  } catch (error) {
    console.error('添加任务失败:', error)
    MessagePlugin.error('添加任务失败')
  } finally {
    savingTask.value = false
  }
}

// 编辑任务
const editTask = (task) => {
  editingTaskId.value = task.id
  taskForm.value = {
    title: task.title,
    description: task.description,
    priority: task.priority,
    status: task.status,
    planStartDate: task.planStartDate ? dayjs(task.planStartDate).toDate() : null,
    planEndDate: task.planEndDate ? dayjs(task.planEndDate).toDate() : null
  }
  showEditTaskDialog.value = true
}

// 更新任务
const handleUpdateTask = async () => {
  try {
    await taskFormRef2.value.validate()
  } catch {
    return
  }

  savingTask.value = true
  try {
    await projectService.updateTask(editingTaskId.value, {
      title: taskForm.value.title,
      description: taskForm.value.description,
      priority: taskForm.value.priority,
      status: taskForm.value.status,
      planStartDate: taskForm.value.planStartDate ? dayjs(taskForm.value.planStartDate).format('YYYY-MM-DD') : null,
      planEndDate: taskForm.value.planEndDate ? dayjs(taskForm.value.planEndDate).format('YYYY-MM-DD') : null
    })
    MessagePlugin.success('任务更新成功')
    showEditTaskDialog.value = false
    await loadProject()
  } catch (error) {
    console.error('更新任务失败:', error)
    MessagePlugin.error('更新任务失败')
  } finally {
    savingTask.value = false
  }
}

// 切换任务状态
const toggleTaskStatus = async (task) => {
  const newStatus = task.status === 'completed' ? 'pending' : 'completed'
  try {
    await projectService.updateTask(task.id, { status: newStatus })
    task.status = newStatus
    task.statusText = { pending: '待开始', in_progress: '进行中', completed: '已完成' }[newStatus]
    // 更新进度
    project.value.progress = stats.value.progress
  } catch (error) {
    console.error('更新状态失败:', error)
    MessagePlugin.error('更新状态失败')
  }
}

// 删除任务
const deleteTask = async (taskId) => {
  try {
    await projectService.deleteTask(taskId)
    MessagePlugin.success('任务已删除')
    await loadProject()
  } catch (error) {
    console.error('删除任务失败:', error)
    MessagePlugin.error('删除任务失败')
  }
}

// 工具函数
const getPriorityType = (p) => ({ high: 'danger', medium: 'warning', low: 'primary' }[p] || 'default')
const getStatusType = (s) => ({ completed: 'success', in_progress: 'primary', pending: 'default' }[s] || 'default')
const isOverdue = (date) => date && dayjs(date).isBefore(dayjs(), 'day')

import { AddIcon, EditIcon, SearchIcon, DeleteIcon, ArrowLeftIcon } from 'tdesign-icons-vue-next'

// 监听路由变化
watch(() => route.params.id, loadProject)

onMounted(loadProject)
</script>

<style scoped>
.project-detail-page { }
.loading-container { display: flex; justify-content: center; align-items: center; min-height: 400px; }

/* 表单样式 */
.project-form { background: var(--bg-container); border-radius: var(--radius-xl); padding: 32px; animation: fadeInUp 0.5s ease; }
.form-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 32px; }
.form-header h2 { font-size: 20px; font-weight: 600; color: var(--text-primary); }

/* 头部样式 */
.project-header { padding: 32px; border-radius: var(--radius-xl); margin-bottom: 24px; animation: fadeInUp 0.5s ease; }
.header-content { display: flex; justify-content: space-between; align-items: flex-start; }
.project-info { display: flex; gap: 20px; }
.project-icon { width: 64px; height: 64px; border-radius: var(--radius-xl); background: rgba(255,255,255,0.2); display: flex; align-items: center; justify-content: center; color: white; font-size: 28px; font-weight: 700; backdrop-filter: blur(4px); }
.project-text h1 { font-size: 24px; font-weight: 700; color: white; margin: 0 0 8px 0; }
.project-text p { font-size: 14px; color: rgba(255,255,255,0.8); margin: 0; }
.header-actions { display: flex; gap: 12px; }

/* 统计区域 */
.project-stats { display: flex; gap: 32px; padding: 24px; background: var(--bg-container); border-radius: var(--radius-xl); border: 1px solid var(--border-color); margin-bottom: 24px; animation: fadeInUp 0.5s ease 0.1s backwards; }
.stat-item { text-align: center; }
.stat-value { display: block; font-size: 28px; font-weight: 700; color: var(--text-primary); }
.stat-label { font-size: 12px; color: var(--text-tertiary); }
.stat-bar { width: 80px; height: 4px; background: var(--border-color); border-radius: 2px; margin-top: 8px; overflow: hidden; }
.stat-bar-fill { height: 100%; border-radius: 2px; transition: width 0.8s ease; }

/* 任务列表 */
.tasks-content, .members-content, .files-content { padding-top: 20px; }
.tasks-filters { display: flex; gap: 12px; margin-bottom: 16px; }
.task-cell { display: flex; align-items: center; gap: 12px; }
.task-cell .completed { text-decoration: line-through; color: var(--text-tertiary); }
.assignee-cell { display: flex; align-items: center; gap: 8px; }
.avatar { width: 28px; height: 28px; border-radius: var(--radius-full); display: flex; align-items: center; justify-content: center; color: white; font-size: 11px; font-weight: 600; }
.overdue { color: var(--error-color); }

/* 成员列表 */
.member-card { display: flex; align-items: center; gap: 16px; padding: 16px; background: var(--bg-page); border-radius: var(--radius-lg); margin-bottom: 12px; }
.member-avatar { width: 48px; height: 48px; border-radius: var(--radius-full); display: flex; align-items: center; justify-content: center; color: white; font-size: 18px; font-weight: 600; }
.member-info { flex: 1; }
.member-name { display: block; font-weight: 600; color: var(--text-primary); }
.member-role { font-size: 12px; color: var(--text-tertiary); }
.empty-tip { padding: 40px 0; }

@keyframes fadeInUp {
  from { opacity: 0; transform: translateY(20px); }
  to { opacity: 1; transform: translateY(0); }
}
</style>
