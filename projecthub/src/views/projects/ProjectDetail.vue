<template>
  <div class="projects-page">
    <!-- 页面头部 -->
    <div class="page-header">
      <div class="header-content">
        <div>
          <h1 class="page-title">{{ isNewMode ? '创建新项目' : '项目详情' }}</h1>
          <p class="page-subtitle">{{ isNewMode ? '填写项目基本信息' : project?.name }}</p>
        </div>
        <div class="header-actions">
          <t-button variant="outline" @click="goBack">
            <template #icon><ArrowLeftIcon /></template>
            返回
          </t-button>
        </div>
      </div>
    </div>

    <!-- 创建项目表单 -->
    <div v-if="isNewMode" class="project-form">

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
              <div class="project-title-row">
                <h1 class="project-name">{{ project.name }}</h1>
                <t-tag v-if="stats.isOverdue" type="danger" variant="light" size="large">已逾期</t-tag>
                <t-tag v-else-if="project.status === 'completed'" type="success" variant="light" size="large">已完成</t-tag>
                <t-tag v-else type="primary" variant="light" size="large">进行中</t-tag>
              </div>
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
            <div class="stat-bar-fill" :style="{ width: (project.progress || 0) + '%', background: project.progress >= 100 ? '#10B981' : (project.color || '#2196F3') }"></div>
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
          <span class="stat-value">{{ stats.daysLeft }}</span>
          <span class="stat-label">剩余天数</span>
        </div>
      </div>

      <!-- 标签页 -->
      <div class="tabs-container">
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
            <t-table v-else :data="filteredTasks" :columns="tableColumns" row-key="id" hover stripe :header-height="44">
              <template #title="{ row }">
                <div class="task-cell">
                  <t-checkbox :checked="row.status === 'completed'" @change="() => toggleTaskStatus(row)" />
                  <span :class="{ completed: row.status === 'completed' }">{{ row.title }}</span>
                </div>
              </template>
              <template #category="{ row }">
                <t-tag :type="getCategoryType(row.category)" variant="light" size="small">{{ row.categoryText || row.category }}</t-tag>
              </template>
              <template #priority="{ row }">
                <t-tag :type="getPriorityType(row.priority)" variant="light" size="small">{{ row.priorityText || row.priority }}</t-tag>
              </template>
              <template #status="{ row }">
                <t-tag v-if="isTaskOverdue(row)" type="danger" variant="light" size="small">逾期</t-tag>
                <t-tag v-else :type="getStatusType(row.status)" variant="light" size="small">{{ row.statusText || row.status }}</t-tag>
              </template>
              <template #progress="{ row }">
                <div class="progress-cell">
                  <t-slider v-model="row.progress" :step="10" :show-tooltip="true" :disabled="row.status === 'completed'" :style="{ width: '100px' }" @change="(val) => handleProgressChange(row, val)" />
                  <span class="progress-text">{{ row.progress }}%</span>
                </div>
              </template>
              <template #estimatedHours="{ row }">
                {{ row.estimatedHours ? row.estimatedHours + 'h' : '-' }}
              </template>
              <template #planStartDate="{ row }">
                <span>{{ row.planStartDate ? dayjs(row.planStartDate).format('YYYY年MM月DD日') : '-' }}</span>
              </template>
              <template #planEndDate="{ row }">
                <span :class="{ overdue: isOverdue(row.planEndDate) }">{{ row.planEndDate ? dayjs(row.planEndDate).format('YYYY年MM月DD日') : '-' }}</span>
              </template>
              <template #actions="{ row }">
                <t-button variant="text" size="small" @click="editTask(row)"><EditIcon /></t-button>
                <t-button variant="text" size="small" @click="deleteTask(row.id)"><DeleteIcon /></t-button>
              </template>
            </t-table>
          </div>
        </t-tab-panel>

        </t-tabs>
      </div>
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
        <t-form-item label="任务分类" name="category">
          <t-select v-model="taskForm.category" placeholder="请选择分类">
            <t-option value="dev" label="开发" />
            <t-option value="meeting" label="会议" />
            <t-option value="doc" label="文档" />
            <t-option value="design" label="设计" />
            <t-option value="debug" label="调试" />
            <t-option value="bug" label="BUG" />
          </t-select>
        </t-form-item>
        <t-form-item label="优先级" name="priority">
          <t-select v-model="taskForm.priority" placeholder="请选择优先级">
            <t-option value="high" label="高" />
            <t-option value="medium" label="中" />
            <t-option value="low" label="低" />
          </t-select>
        </t-form-item>
        <t-form-item label="预估工时" name="estimatedHours">
          <t-input-number v-model="taskForm.estimatedHours" :min="0" :step="0.5" placeholder="小时" style="width: 150px" />
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
        <t-form-item label="任务分类" name="category">
          <t-select v-model="taskForm.category" placeholder="请选择分类">
            <t-option value="dev" label="开发" />
            <t-option value="meeting" label="会议" />
            <t-option value="doc" label="文档" />
            <t-option value="design" label="设计" />
            <t-option value="debug" label="调试" />
            <t-option value="bug" label="BUG" />
          </t-select>
        </t-form-item>
        <t-form-item label="优先级" name="priority">
          <t-select v-model="taskForm.priority" placeholder="请选择优先级">
            <t-option value="high" label="高" />
            <t-option value="medium" label="中" />
            <t-option value="low" label="低" />
          </t-select>
        </t-form-item>
        <t-form-item label="预估工时" name="estimatedHours">
          <t-input-number v-model="taskForm.estimatedHours" :min="0" :step="0.5" placeholder="小时" style="width: 150px" />
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
const activeTab = ref('tasks')
const formRef = ref(null)
const taskFormRef = ref(null)
const taskFormRef2 = ref(null)

// 任务表单
const taskForm = ref({
  title: '',
  description: '',
  category: 'dev',
  priority: 'medium',
  status: 'pending',
  progress: 0,
  estimatedHours: 0,
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
  type: 'web'
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

// 表格列配置
const tableColumns = [
  { colKey: 'title', title: '任务', width: '25%' },
  { colKey: 'status', title: '状态', width: '10%' },
  { colKey: 'category', title: '分类', width: '8%' },
  { colKey: 'priority', title: '优先级', width: '8%' },
  { colKey: 'progress', title: '进度', width: '17%' },
  { colKey: 'estimatedHours', title: '工时', width: '7%' },
  { colKey: 'planStartDate', title: '开始日期', width: '11%' },
  { colKey: 'planEndDate', title: '截止日期', width: '11%' },
  { colKey: 'actions', title: '操作', align: 'center' }
]

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
  // 进度 = (各任务预估工时 × 各自进度) / 总预估工时
  const totalHours = tasks.value.reduce((sum, t) => sum + (t.estimatedHours || 0), 0)
  const weightedProgress = tasks.value.reduce((sum, t) => sum + ((t.estimatedHours || 0) * (t.progress || 0) / 100), 0)
  const progress = totalHours > 0 ? Math.round((weightedProgress / totalHours) * 100) : 0
  const endDate = project.value.endDate ? dayjs(project.value.endDate) : dayjs()
  const daysLeft = Math.max(0, endDate.diff(dayjs(), 'day'))
  // 项目逾期：进度<100%且有逾期任务
  const isOverdue = progress < 100 && tasks.value.some(t => isTaskOverdue(t))
  return {
    total,
    completed,
    progress,
    daysLeft,
    isOverdue
  }
})

// 加载项目数据
const loadProject = async () => {
  if (isNewMode.value) {
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
      statusText: { pending: '待开始', in_progress: '进行中', completed: '已完成' }[t.status] || t.status,
      categoryText: { dev: '开发', meeting: '会议', doc: '文档', design: '设计', debug: '调试', bug: 'BUG' }[t.category] || t.category
    }))
    
    // 计算项目进度
    if (project.value) {
      project.value.progress = stats.value.progress
      // 进度100%自动完成项目
      if (stats.value.progress >= 100 && project.value.status !== 'completed') {
        project.value.status = 'completed'
        project.value.statusText = '已完成'
        await projectService.update(projectId, { status: 'completed' })
      }
    }
  } catch (error) {
    console.error('加载项目失败:', error)
    MessagePlugin.error('加载项目失败')
  } finally {
    loading.value = false
  }
}

// 监听stats变化，自动更新项目状态
watch(() => stats.value.progress, async (newProgress) => {
  if (!project.value || isNewMode.value) return
  // 进度100%自动完成项目
  if (newProgress >= 100 && project.value.status !== 'completed') {
    project.value.status = 'completed'
    project.value.statusText = '已完成'
    await projectService.update(project.value.id, { status: 'completed' })
  }
  // 进度<100且状态为completed时恢复进行中
  else if (newProgress < 100 && project.value.status === 'completed') {
    project.value.status = 'in_progress'
    project.value.statusText = '进行中'
    await projectService.update(project.value.id, { status: 'in_progress' })
  }
})

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
      type: formData.value.type
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
      category: taskForm.value.category,
      priority: taskForm.value.priority,
      status: 'pending',
      progress: 0,
      estimatedHours: taskForm.value.estimatedHours,
      planStartDate: taskForm.value.planStartDate ? dayjs(taskForm.value.planStartDate).format('YYYY-MM-DD') : null,
      planEndDate: taskForm.value.planEndDate ? dayjs(taskForm.value.planEndDate).format('YYYY-MM-DD') : null
    })
    MessagePlugin.success('任务添加成功')
    showAddTaskDialog.value = false
    taskForm.value = { title: '', description: '', category: 'dev', priority: 'medium', status: 'pending', progress: 0, estimatedHours: 0, planStartDate: null, planEndDate: null }
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
    category: task.category,
    priority: task.priority,
    status: task.status,
    progress: task.progress,
    estimatedHours: task.estimatedHours || 0,
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
      category: taskForm.value.category,
      priority: taskForm.value.priority,
      status: taskForm.value.status,
      progress: taskForm.value.progress,
      estimatedHours: taskForm.value.estimatedHours,
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
  const newProgress = newStatus === 'completed' ? 100 : 0
  try {
    await projectService.updateTask(task.id, { status: newStatus, progress: newProgress })
    task.status = newStatus
    task.progress = newProgress
    task.statusText = { pending: '待开始', in_progress: '进行中', completed: '已完成' }[newStatus]
    // 更新进度
    project.value.progress = stats.value.progress
  } catch (error) {
    console.error('更新状态失败:', error)
    MessagePlugin.error('更新状态失败')
  }
}

// 进度变更
const handleProgressChange = async (task, progress) => {
  try {
    await projectService.updateTask(task.id, { progress })
    task.progress = progress
    // 进度到100自动完成
    if (progress >= 100 && task.status !== 'completed') {
      task.status = 'completed'
      task.statusText = '已完成'
    } else if (progress < 100 && task.status === 'completed') {
      task.status = 'in_progress'
      task.statusText = '进行中'
    }
    // 更新项目进度
    project.value.progress = stats.value.progress
  } catch (error) {
    console.error('更新进度失败:', error)
    MessagePlugin.error('更新进度失败')
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
const getCategoryType = (c) => ({ dev: 'primary', meeting: 'warning', doc: 'success', design: 'purple', debug: 'danger', bug: 'danger' }[c] || 'default')
const isOverdue = (date) => date && dayjs(date).isBefore(dayjs(), 'day')
const isTaskOverdue = (task) => task.planEndDate && !task.status?.includes('completed') && dayjs(task.planEndDate).isBefore(dayjs(), 'day')

import { AddIcon, EditIcon, SearchIcon, DeleteIcon, ArrowLeftIcon } from 'tdesign-icons-vue-next'

// 监听路由变化
watch(() => route.params.id, loadProject)

onMounted(loadProject)
</script>

<style scoped>
/* 页面容器 - 与项目列表页保持一致 */
.projects-page {
  padding: var(--space-6);
  max-width: var(--content-max-width);
  margin: 0 auto;
  animation: fadeIn 0.5s ease;
}

/* 页面头部 - 与其他页面保持一致 */
.page-header {
  margin-bottom: var(--space-6);
}

.header-content {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: var(--space-6);
}

.page-title {
  font-size: var(--font-size-2xl);
  font-weight: var(--font-weight-bold);
  color: var(--text-primary);
  margin-bottom: var(--space-2);
}

.page-subtitle {
  font-size: var(--font-size-sm);
  color: var(--text-secondary);
}

/* 表单样式 */
.project-form {
  background: var(--bg-container);
  border-radius: var(--radius-xl);
  padding: 32px;
  animation: fadeInUp 0.5s ease;
  max-width: 600px;
}

.loading-container { display: flex; justify-content: center; align-items: center; min-height: 400px; }

/* 头部样式 */
.project-header {
  display: flex;
  align-items: center;
  gap: 24px;
  padding: 24px;
  background: var(--bg-container);
  border: 1px solid var(--border-color);
  border-radius: var(--radius-xl);
  margin-bottom: 24px;
  animation: fadeInUp 0.5s ease;
}
.header-content { display: flex; justify-content: space-between; align-items: flex-start; flex: 1; }
.project-info { display: flex; gap: 20px; align-items: center; flex: 1; }
.project-icon { width: 64px; height: 64px; border-radius: var(--radius-xl); background: var(--gradient-primary); display: flex; align-items: center; justify-content: center; color: white; font-size: 28px; font-weight: 700; flex-shrink: 0; }
.project-text { flex: 1; min-width: 0; }
.project-text h1 { font-size: 24px; font-weight: 700; color: var(--text-primary); margin: 0 0 8px 0; }
.project-text p { font-size: 14px; color: var(--text-secondary); margin: 0; }
.header-actions { display: flex; gap: 12px; flex-shrink: 0; }

/* 统计区域 */
.project-stats { display: flex; gap: 32px; padding: 24px; background: var(--bg-container); border-radius: var(--radius-xl); border: 1px solid var(--border-color); margin-bottom: 24px; animation: fadeInUp 0.5s ease 0.1s backwards; }
.stat-item { text-align: center; }
.stat-value { display: block; font-size: 28px; font-weight: 700; color: var(--text-primary); }
.stat-label { font-size: 12px; color: var(--text-tertiary); }
.stat-bar { width: 80px; height: 4px; background: var(--border-color); border-radius: 2px; margin-top: 8px; overflow: hidden; }
.stat-bar-fill { height: 100%; border-radius: 2px; transition: width 0.8s ease; }

/* 任务列表 */
.tabs-container {
  background: var(--bg-container);
  border: 1px solid var(--border-color);
  border-radius: var(--radius-xl);
  padding: 16px;
}
.tasks-content { padding-top: 16px; }
.tasks-filters { display: flex; gap: 12px; margin-bottom: 16px; }
.task-cell { display: flex; align-items: center; gap: 12px; }
.task-cell .completed { text-decoration: line-through; color: var(--text-tertiary); }
.assignee-cell { display: flex; align-items: center; gap: 8px; }
.avatar { width: 28px; height: 28px; border-radius: var(--radius-full); display: flex; align-items: center; justify-content: center; color: white; font-size: 11px; font-weight: 600; }
.overdue { color: var(--error-color); }
.progress-cell { display: flex; align-items: center; gap: 8px; }
.progress-text { font-size: 12px; color: var(--text-secondary); min-width: 36px; }
.project-title-row { display: flex; align-items: center; gap: 12px; }

@keyframes fadeInUp {
  from { opacity: 0; transform: translateY(20px); }
  to { opacity: 1; transform: translateY(0); }
}
</style>
