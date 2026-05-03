<template>
  <div class="projects-page">
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

    <template v-else>
      <div class="project-header" :style="{ background: project.gradient }">
        <div class="header-content">
          <div class="project-info">
            <div class="project-icon">{{ project.name?.charAt(0) || 'P' }}</div>
            <div class="project-text">
              <div class="project-title-row">
                <h1 class="project-name">{{ project.name }}</h1>
                <t-tag :theme="getProjectTypeTheme(project.type)" variant="outline" size="large">{{ getProjectTypeText(project.type) }}</t-tag>
                <t-tag v-if="stats.isOverdue" theme="danger" variant="light" size="large">已逾期</t-tag>
                <t-tag v-else-if="project.status === 'completed'" theme="success" variant="light" size="large">已完成</t-tag>
                <t-tag v-else theme="primary" variant="light" size="large">进行中</t-tag>
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

      <div class="project-stats">
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

      <div class="tabs-container">
        <t-tabs v-model="activeTab">
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

            <t-empty v-if="filteredTasks.length === 0" description="暂无任务，点击上方添加" />

            <div v-else class="task-list-container">
              <template v-for="task in filteredTasks" :key="task.id">
                <div
                  class="task-row"
                  :class="{ expanded: expandedTasks.has(task.id) }"
                  @contextmenu.prevent.stop="showContextMenu($event, task)"
                >
                  <div class="task-main">
                    <div class="expand-icon" @click.stop="toggleExpand(task.id)">
                      <t-icon :name="expandedTasks.has(task.id) ? 'caret-down' : 'caret-right'" size="16px" />
                    </div>

                    <span :class="{ completed: task.status === 'completed' }">{{ task.title }}</span>

                    <span v-if="subTasksMap[task.id]?.length > 0" class="subtask-count">
                      ({{ subTasksMap[task.id].filter(s => s.isCompleted).length }}/{{ subTasksMap[task.id].length }})
                    </span>
                  </div>
                  <div class="task-meta">
                    <t-tag :theme="getPriorityType(task.priority)" variant="light" size="small">{{ task.priorityText || task.priority }}</t-tag>
                    <span v-if="isTaskOverdue(task)" class="overdue-text">逾期</span>
                    <t-tag v-else :theme="getStatusType(task.status)" variant="light" size="small">{{ task.statusText || task.status }}</t-tag>
                    <div class="progress-cell">
                      <t-slider :model-value="getTaskProgress(task)" :step="1" :show-tooltip="true" :style="{ width: '100px' }" />
                      <span class="progress-text">{{ getTaskProgress(task) }}%</span>
                    </div>
                    <span class="hours-text">{{ task.totalEstimatedHours ?? task.estimatedHours }}h</span>
                    <span class="date-text">{{ task.planStartDate ? dayjs(task.planStartDate).format('MM/DD') : '-' }}</span>
                    <span class="date-text">{{ task.planEndDate ? dayjs(task.planEndDate).format('MM/DD') : '-' }}</span>
                    <t-button variant="text" size="small" @click.stop="editTask(task)"><EditIcon /></t-button>
                    <t-button variant="text" size="small" @click.stop="deleteTask(task.id)"><DeleteIcon /></t-button>
                  </div>
                </div>

                <div v-if="expandedTasks.has(task.id)" class="subtask-list">
                  <t-loading v-if="subTasksLoading[task.id]" size="small" text="加载中..." />
                  <template v-else>
                    <div
                      v-for="subTask in subTasksMap[task.id]"
                      :key="subTask.id"
                      class="subtask-row"
                      :class="{ completed: subTask.isCompleted }"
                      @contextmenu.prevent.stop="showContextMenu($event, { ...task, subTask })"
                    >
                      <t-checkbox :checked="subTask.isCompleted" @change="() => toggleSubTaskComplete(subTask, task.id)" />

                      <t-input
                        v-if="inlineEditTask === subTask.id"
                        :ref="(el) => setInputRef(el, subTask.id)"
                        :value="inlineEditValue"
                        size="small"
                        style="width: 200px"
                        @update:value="(val) => inlineEditValue = val"
                        @blur="finishInlineEdit(subTask)"
                        @enter="finishInlineEdit(subTask)"
                        @keydown="(val, ctx) => { if (ctx?.e?.key === 'Escape') cancelInlineEdit() }"
                      />
                      <span
                        v-else
                        class="subtask-title"
                        @dblclick.stop="startInlineEdit(subTask)"
                      >{{ subTask.title }}</span>

                      <t-dropdown>
                        <t-tag
                          :theme="getCategoryTheme(subTask.category)"
                          variant="light"
                          size="small"
                          style="cursor: pointer"
                        >{{ getCategoryLabel(subTask.category) }}</t-tag>
                        <t-dropdown-menu>
                          <t-dropdown-item
                            v-for="cat in categories"
                            :key="cat.value"
                            @click="selectCategory(subTask, cat.value)"
                          >{{ cat.label }}</t-dropdown-item>
                        </t-dropdown-menu>
                      </t-dropdown>

                      <t-input-number
                        v-model="subTask.estimatedHours"
                        :min="0"
                        :step="0.5"
                        size="small"
                        style="width: 100px"
                        @change="() => updateSubTaskHours(subTask)"
                      />
                      <span class="subtask-hours">h</span>

                      <t-button variant="text" size="small" @click.stop="editSubTask(subTask)"><EditIcon /></t-button>
                      <t-button variant="text" size="small" @click.stop="deleteSubTask(task.id, subTask.id)"><DeleteIcon /></t-button>
                    </div>

                    <div class="subtask-add" @click.stop="addSubTask(task.id)">
                      <t-icon name="add" size="14px" />
                      <span>添加子任务</span>
                    </div>
                  </template>
                </div>
              </template>
            </div>
          </div>
        </t-tab-panel>
        </t-tabs>
      </div>
    </template>

    <div
      v-show="contextMenuVisible"
      class="custom-context-menu"
      :style="contextMenuStyle"
    >
      <div class="menu-item" @click.stop="handleContextMenuEdit">
        <t-icon name="edit" style="margin-right: 8px" />编辑
      </div>
      <div class="menu-item error" @click.stop="handleContextMenuDelete">
        <t-icon name="delete" style="margin-right: 8px" />删除
      </div>
    </div>

    <t-dialog v-model:visible="showAddTaskDialog" header="添加任务" width="600px" :footer="false">
      <t-form :data="taskForm" :rules="taskRules" ref="taskFormRef" label-width="80px">
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

    <!-- 编辑项目弹窗 -->
    <t-dialog v-model:visible="showEditProjectDialog" header="编辑项目" width="500px" :footer="false">
      <t-form :data="editProjectForm" :rules="editProjectRules" ref="formRef" label-width="100px">
        <t-form-item label="项目名称" name="name">
          <t-input v-model="editProjectForm.name" placeholder="请输入项目名称" style="max-width: 350px" />
        </t-form-item>
        <t-form-item label="项目描述" name="description">
          <t-textarea v-model="editProjectForm.description" placeholder="请输入项目描述" style="max-width: 400px" :rows="3" />
        </t-form-item>
        <t-form-item label="项目类型" name="type">
          <t-select v-model="editProjectForm.type" placeholder="请选择项目类型" style="max-width: 200px">
            <t-option value="web" label="Web应用" />
            <t-option value="mobile" label="移动应用" />
            <t-option value="desktop" label="桌面应用" />
            <t-option value="ai" label="AI项目" />
            <t-option value="other" label="其他" />
          </t-select>
        </t-form-item>
        <t-form-item>
          <t-button theme="primary" @click="handleEditProject" :loading="saving">保存</t-button>
          <t-button variant="outline" @click="showEditProjectDialog = false" style="margin-left: 12px">取消</t-button>
        </t-form-item>
      </t-form>
    </t-dialog>

    <t-dialog v-model:visible="showEditTaskDialog" header="编辑任务" width="600px" :footer="false">
      <t-form :data="taskForm" :rules="taskRules" ref="taskFormRef2" label-width="80px">
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
import { ref, computed, onMounted, watch, nextTick } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { MessagePlugin } from 'tdesign-vue-next'
import { AddIcon, EditIcon, SearchIcon, DeleteIcon, ArrowLeftIcon } from 'tdesign-icons-vue-next'
import dayjs from 'dayjs'
import { projectService, subTaskService } from '@/services/dataService'

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
  priority: 'medium',
  status: 'pending',
  progress: 0,
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

// 弹窗
const showAddTaskDialog = ref(false)
const showEditTaskDialog = ref(false)
const showEditProjectDialog = ref(false)
const editingTaskId = ref(null)

// 编辑项目表单
const editProjectForm = ref({
  name: '',
  description: '',
  type: 'web'
})
const editProjectRules = {
  name: [{ required: true, message: '请输入项目名称', trigger: 'blur' }]
}

// ===== 子任务相关状态 =====
const expandedTasks = ref(new Set()) // 展开的任务ID集合
const subTasksMap = ref({}) // { taskId: [subTasks] }
const subTasksLoading = ref({}) // { taskId: boolean }
const contextMenuVisible = ref(false)
const contextMenuTask = ref(null)
const contextMenuStyle = ref({})

// 子任务类型配置
const categories = [
  { value: 'dev', label: '开发', theme: 'primary' },
  { value: 'meeting', label: '会议', theme: 'warning' },
  { value: 'doc', label: '文档', theme: 'success' },
  { value: 'design', label: '设计', theme: 'primary' },
  { value: 'debug', label: '调试', theme: 'warning' },
  { value: 'bug', label: 'BUG', theme: 'danger' }
]

// 获取任务类型标签名
const getCategoryLabel = (category) => {
  const cat = categories.find(c => c.value === category)
  return cat ? cat.label : category
}

// 获取任务类型主题
const getCategoryTheme = (category) => {
  const cat = categories.find(c => c.value === category)
  return cat ? cat.theme : 'default'
}

// 行内编辑状态
const inlineEditTask = ref(null) // 正在行内编辑的子任务ID
const inlineEditValue = ref('')

// 【修复：通过函数将所有 v-for 生成的 input 引用保存到对象中】
const inputRefs = ref({})
const setInputRef = (el, id) => {
  if (el) inputRefs.value[id] = el
}

// 加载子任务
const loadSubTasks = async (taskId) => {
  if (subTasksMap.value[taskId]) return

  subTasksLoading.value[taskId] = true
  try {
    const data = await subTaskService.getByTaskId(taskId)
    subTasksMap.value[taskId] = data || []
  } catch (error) {
    console.error('加载子任务失败:', error)
    subTasksMap.value[taskId] = []
  } finally {
    subTasksLoading.value[taskId] = false
  }
}

// 切换展开/收起
const toggleExpand = async (taskId) => {
  if (expandedTasks.value.has(taskId)) {
    expandedTasks.value.delete(taskId)
  } else {
    expandedTasks.value.add(taskId)
    await loadSubTasks(taskId)
  }
  expandedTasks.value = new Set(expandedTasks.value)
}

// 添加子任务
const addSubTask = async (taskId) => {
  try {
    const newSubTask = await subTaskService.create({
      ParentTaskId: taskId,
      Title: '新子任务',
      Category: 'dev',
      EstimatedHours: 0,
      SortOrder: (subTasksMap.value[taskId]?.length || 0) + 1
    })
    if (!subTasksMap.value[taskId]) {
      subTasksMap.value[taskId] = []
    }
    subTasksMap.value[taskId].push(newSubTask)
    startInlineEdit(newSubTask)
  } catch (error) {
    console.error('添加子任务失败:', error)
    MessagePlugin.error('添加子任务失败')
  }
}

// 获取子任务预估工时总和
const getTotalEstimatedHours = (taskId) => {
  const subTasks = subTasksMap.value[taskId] || []
  return subTasks.reduce((sum, st) => sum + (st.estimatedHours || 0), 0)
}

// 计算任务进度：优先使用后端返回的progress（不展开时），有子任务数据时本地计算
const getTaskProgress = (task) => {
  // 如果任务没有子任务数据或没有展开，使用后端返回的progress
  const subTasks = subTasksMap.value[task.id]
  if (!subTasks || subTasks.length === 0) {
    return task.progress || 0
  }
  // 有子任务数据时，本地计算
  const totalHours = subTasks.reduce((sum, st) => sum + (st.estimatedHours || 0), 0)
  if (totalHours === 0) return 0
  const completedHours = subTasks.filter(st => st.isCompleted).reduce((sum, st) => sum + (st.estimatedHours || 0), 0)
  return Math.round((completedHours / totalHours) * 100)
}

// 选择子任务分类
const selectCategory = async (subTask, category) => {
  const originalCategory = subTask.category
  subTask.category = category
  try {
    await subTaskService.update(subTask.id, { Category: category })
  } catch (error) {
    console.error('更新子任务分类失败:', error)
    subTask.category = originalCategory // 失败则回滚
    MessagePlugin.error('更新失败')
  }
}

// 更新子任务预估工时
const updateSubTaskHours = async (subTask) => {
  try {
    await subTaskService.update(subTask.id, { EstimatedHours: subTask.estimatedHours })
    // 重新加载任务列表，获取更新后的父任务状态和进度
    await loadProject()
  } catch (error) {
    console.error('更新子任务工时失败:', error)
    MessagePlugin.error('更新失败')
  }
}

// 【修复：乐观 UI 更新，保证复选框瞬间响应】
const toggleSubTaskComplete = async (subTask, taskId) => {
  const originalState = subTask.isCompleted
  subTask.isCompleted = !originalState
  try {
    const result = await subTaskService.toggleComplete(subTask.id)
    if (result && typeof result.isCompleted === 'boolean') {
      subTask.isCompleted = result.isCompleted
    }
    // 重新加载任务列表，获取更新后的父任务状态和进度
    await loadProject()
  } catch (error) {
    console.error('切换状态失败:', error)
    subTask.isCompleted = originalState // 失败则回滚
    MessagePlugin.error('操作失败')
  }
}

// 从本地子任务数据计算进度（用于切换子任务状态后）
const getTaskProgressFromSubTasks = (taskId) => {
  const subTasks = subTasksMap.value[taskId] || []
  if (subTasks.length === 0) return 0
  const totalHours = subTasks.reduce((sum, st) => sum + (st.estimatedHours || 0), 0)
  if (totalHours === 0) return 0
  const completedHours = subTasks.filter(st => st.isCompleted).reduce((sum, st) => sum + (st.estimatedHours || 0), 0)
  return Math.round((completedHours / totalHours) * 100)
}

// 开始行内编辑
const startInlineEdit = (subTask) => {
  inlineEditTask.value = subTask.id
  inlineEditValue.value = subTask.title
  nextTick(() => {
    // 【修复：通过准确的 ID 寻找 Input 实例而不是粗暴指向一个 ref】
    const inputInstance = inputRefs.value[subTask.id]
    if (inputInstance) {
      if (typeof inputInstance.focus === 'function') {
        inputInstance.focus()
      }
      const inputEl = inputInstance.$el?.querySelector?.('input') || inputInstance.$el
      if (inputEl?.select) {
        inputEl.select()
      }
    }
  })
}

// 【修复：防止 Blur 和 Enter 同时触发导致更新二次调用】
const finishInlineEdit = async (subTask) => {
  if (inlineEditTask.value !== subTask.id) return // 不匹配说明已经保存过了，防止重复
  inlineEditTask.value = null

  const newTitle = inlineEditValue.value.trim()
  if (newTitle && newTitle !== subTask.title) {
    const oldTitle = subTask.title
    subTask.title = newTitle // 乐观更新
    try {
      await subTaskService.update(subTask.id, { Title: newTitle })
    } catch (error) {
      console.error('更新失败:', error)
      subTask.title = oldTitle // 回滚
      MessagePlugin.error('更新失败')
    }
  }
}

// 取消行内编辑
const cancelInlineEdit = () => {
  inlineEditTask.value = null
}

// 删除子任务
const deleteSubTask = async (taskId, subTaskId) => {
  try {
    await subTaskService.delete(subTaskId)
    subTasksMap.value[taskId] = subTasksMap.value[taskId].filter(s => s.id !== subTaskId)
    MessagePlugin.success('删除成功')
  } catch (error) {
    console.error('删除失败:', error)
    MessagePlugin.error('删除失败')
  }
}

// 【修复：右键菜单显示位置溢出处理】
const showContextMenu = (event, task) => {
  contextMenuTask.value = task

  let x = event.clientX
  let y = event.clientY
  
  // 简单的屏幕边缘检测，防止右键菜单超出视口
  if (window.innerWidth - x < 150) x = window.innerWidth - 150
  if (window.innerHeight - y < 100) y = window.innerHeight - 100

  contextMenuStyle.value = {
    left: x + 'px',
    top: y + 'px'
  }
  contextMenuVisible.value = true
}

const hideContextMenu = () => {
  contextMenuVisible.value = false
  contextMenuTask.value = null
}

// 【修复：分离模板和逻辑，并正确访问 .value 内的数据】
const handleContextMenuEdit = () => {
  if (contextMenuTask.value?.subTask) {
    startInlineEdit(contextMenuTask.value.subTask)
  } else {
    editTask(contextMenuTask.value)
  }
  hideContextMenu()
}

const handleContextMenuDelete = () => {
  if (contextMenuTask.value?.subTask) {
    deleteSubTask(contextMenuTask.value.id, contextMenuTask.value.subTask.id)
  } else {
    deleteTask(contextMenuTask.value.id)
  }
  hideContextMenu()
}

// 点击其他地方关闭右键菜单
onMounted(() => {
  document.addEventListener('click', hideContextMenu)
  document.addEventListener('contextmenu', (e) => {
    // 点击非任务区域的右键也应该关闭原有的右键菜单
    if (contextMenuVisible.value && !e.target.closest('.task-row') && !e.target.closest('.subtask-row')) {
      hideContextMenu()
    }
  })
})

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
  }).sort((a, b) => {
    // 按开始日期升序排序
    const aDate = a.planStartDate ? new Date(a.planStartDate).getTime() : 0
    const bDate = b.planStartDate ? new Date(b.planStartDate).getTime() : 0
    return aDate - bDate
  })
})

const stats = computed(() => {
  const total = tasks.value.length
  const completed = tasks.value.filter(t => t.status === 'completed').length
  // 使用后端返回的子任务预估工时总和计算项目进度
  const totalHours = tasks.value.reduce((sum, t) => sum + (t.totalEstimatedHours ?? t.estimatedHours ?? 0), 0)
  const weightedProgress = tasks.value.reduce((sum, t) => {
    const taskHours = t.totalEstimatedHours ?? t.estimatedHours ?? 0
    return sum + (taskHours * (t.progress || 0) / 100)
  }, 0)
  const progress = totalHours > 0 ? Math.round((weightedProgress / totalHours) * 100) : 0
  // 使用 tasks 中最早截止日期计算剩余天数
  const uncompletedTasks = tasks.value.filter(t => t.status !== 'completed' && t.planEndDate)
  const earliestEndDate = uncompletedTasks.length > 0
    ? dayjs(Math.min(...uncompletedTasks.map(t => new Date(t.planEndDate).getTime())))
    : null
  const daysLeft = earliestEndDate ? Math.max(0, earliestEndDate.diff(dayjs(), 'day')) : null
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
  if (isNewMode.value) return

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

watch(() => stats.value.progress, async (newProgress) => {
  if (!project.value || isNewMode.value) return
  if (newProgress >= 100 && project.value.status !== 'completed') {
    project.value.status = 'completed'
    project.value.statusText = '已完成'
    await projectService.update(project.value.id, { status: 'completed' })
  }
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

const goBack = () => {
  router.push('/projects')
}

// 打开编辑项目弹窗
const goEdit = () => {
  editProjectForm.value = {
    name: project.value.name || '',
    description: project.value.description || '',
    type: project.value.type || 'web'
  }
  showEditProjectDialog.value = true
}

// 提交编辑项目
const handleEditProject = async () => {
  try {
    await formRef.value.validate()
  } catch {
    return
  }

  saving.value = true
  try {
    await projectService.update(route.params.id, {
      name: editProjectForm.value.name,
      description: editProjectForm.value.description,
      type: editProjectForm.value.type
    })
    // 更新本地数据
    project.value.name = editProjectForm.value.name
    project.value.description = editProjectForm.value.description
    project.value.type = editProjectForm.value.type
    MessagePlugin.success('项目更新成功')
    showEditProjectDialog.value = false
  } catch (error) {
    console.error('更新项目失败:', error)
    MessagePlugin.error('更新项目失败')
  } finally {
    saving.value = false
  }
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
      progress: 0,
      planStartDate: taskForm.value.planStartDate ? dayjs(taskForm.value.planStartDate).format('YYYY-MM-DD') : null,
      planEndDate: taskForm.value.planEndDate ? dayjs(taskForm.value.planEndDate).format('YYYY-MM-DD') : null
    })
    MessagePlugin.success('任务添加成功')
    showAddTaskDialog.value = false
    taskForm.value = { title: '', description: '', priority: 'medium', status: 'pending', progress: 0, planStartDate: null, planEndDate: null }
    await loadProject()
  } catch (error) {
    console.error('添加任务失败:', error)
    MessagePlugin.error('添加任务失败')
  } finally {
    savingTask.value = false
  }
}

// 【修复：日期反显为格式化字符串而不是 Date 对象】
const editTask = (task) => {
  editingTaskId.value = task.id
  taskForm.value = {
    title: task.title,
    description: task.description,
    priority: task.priority,
    status: task.status,
    progress: task.progress,
    planStartDate: task.planStartDate ? dayjs(task.planStartDate).format('YYYY-MM-DD') : null,
    planEndDate: task.planEndDate ? dayjs(task.planEndDate).format('YYYY-MM-DD') : null
  }
  showEditTaskDialog.value = true
}

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
      progress: taskForm.value.progress,
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

const toggleTaskStatus = async (task) => {
  const newStatus = task.status === 'completed' ? 'pending' : 'completed'
  const newProgress = newStatus === 'completed' ? 100 : 0
  try {
    await projectService.updateTask(task.id, { status: newStatus, progress: newProgress })
    task.status = newStatus
    task.progress = newProgress
    task.statusText = { pending: '待开始', in_progress: '进行中', completed: '已完成' }[newStatus]
    project.value.progress = stats.value.progress
  } catch (error) {
    console.error('更新状态失败:', error)
    MessagePlugin.error('更新状态失败')
  }
}

const handleProgressChange = async (task, progress) => {
  try {
    await projectService.updateTask(task.id, { progress })
    task.progress = progress
    if (progress >= 100 && task.status !== 'completed') {
      task.status = 'completed'
      task.statusText = '已完成'
    } else if (progress < 100 && task.status === 'completed') {
      task.status = 'in_progress'
      task.statusText = '进行中'
    }
    project.value.progress = stats.value.progress
  } catch (error) {
    console.error('更新进度失败:', error)
    MessagePlugin.error('更新进度失败')
  }
}

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
// 类型文本和颜色
const getProjectTypeText = (type) => {
  const map = { web: 'Web应用', mobile: '移动应用', desktop: '桌面应用', ai: 'AI项目', other: '其他' }
  return map[type] || type || '其他'
}
const getProjectTypeTheme = (type) => {
  const map = { web: 'primary', mobile: 'warning', desktop: 'primary', ai: 'success', other: 'default' }
  return map[type] || 'default'
}

// 优先级颜色：红色(紧急), 橙色(中等), 蓝色(低)
const getPriorityType = (p) => ({ high: 'danger', medium: 'warning', low: 'primary' }[p] || 'default')

// 状态颜色：灰色(待开始), 蓝色(进行中), 绿色(已完成), 红色(逾期)
const getStatusType = (s) => {
  const types = {
    pending: 'default',      // 灰色 - 待开始
    in_progress: 'primary',  // 蓝色 - 进行中
    completed: 'success'     // 绿色 - 已完成
  }
  return types[s] || 'default'
}

// 分类颜色：开发(蓝), 会议(橙), 文档(绿), 设计(紫), 调试(红), BUG(红)
const getCategoryType = (c) => {
  const types = {
    dev: 'primary',      // 蓝色 - 开发
    meeting: 'warning',  // 橙色 - 会议
    doc: 'success',      // 绿色 - 文档
    design: 'purple',    // 紫色 - 设计
    debug: 'danger',     // 红色 - 调试
    bug: 'danger'        // 红色 - BUG
  }
  return types[c] || 'default'
}

const isOverdue = (date) => date && dayjs(date).isBefore(dayjs(), 'day')
const isTaskOverdue = (task) => task.planEndDate && !task.status?.includes('completed') && dayjs(task.planEndDate).isBefore(dayjs(), 'day')

// 监听路由变化
watch(() => route.params.id, loadProject)

onMounted(loadProject)
</script>

<style scoped>
/* 页面容器 */
.projects-page {
  padding: var(--space-6);
  max-width: var(--content-max-width);
  margin: 0 auto;
  animation: fadeIn 0.5s ease;
}

/* 页面头部 */
.page-header {
  margin-bottom: var(--space-6);
}

.header-content {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: var(--space-6);
  width: 100%;
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

/* 任务列表容器 */
.task-list-container {
  display: flex;
  flex-direction: column;
}

/* 主任务行 */
.task-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 12px 16px;
  border-bottom: 1px solid var(--border-light);
  transition: background-color 0.2s;
}

.task-row:hover {
  background: var(--bg-hover);
}

.task-row.expanded {
  background: var(--bg-page);
  border-bottom-color: var(--border-color);
}

.task-main {
  display: flex;
  align-items: center;
  gap: 12px;
  flex: 1;
  min-width: 0;
}

.expand-icon {
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  width: 20px;
  height: 20px;
  color: var(--text-tertiary);
  transition: color 0.2s;
}

.expand-icon:hover {
  color: var(--text-primary);
}

.task-meta {
  display: flex;
  align-items: center;
  gap: 12px;
  flex-shrink: 0;
}

.subtask-count {
  font-size: 12px;
  color: var(--text-tertiary);
}

.overdue-text {
  font-size: 12px;
  color: var(--error-color);
}

.progress-cell {
  display: flex;
  align-items: center;
  gap: 8px;
}

.progress-text {
  font-size: 12px;
  color: var(--text-secondary);
  min-width: 36px;
}

.hours-text, .date-text {
  font-size: 12px;
  color: var(--text-secondary);
  min-width: 45px;
}

/* 子任务列表 */
.subtask-list {
  background: var(--bg-page);
  padding: 8px 16px 8px 56px;
  border-bottom: 1px solid var(--border-color);
}

.subtask-row {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 8px 12px;
  border-radius: var(--radius-md);
  transition: background-color 0.2s;
}

.subtask-row:hover {
  background: var(--bg-hover);
}

.subtask-row.completed .subtask-title {
  text-decoration: line-through;
  color: var(--text-tertiary);
}

.subtask-title {
  cursor: pointer;
  flex: 1;
  min-width: 0;
}

.subtask-title:hover {
  color: var(--text-primary);
}

.subtask-hours {
  font-size: 12px;
  color: var(--text-tertiary);
  margin-right: 8px;
}

/* 添加子任务 */
.subtask-add {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 8px 12px;
  color: var(--text-tertiary);
  cursor: pointer;
  border-radius: var(--radius-md);
  font-size: 13px;
  transition: all 0.2s;
}

.subtask-add:hover {
  color: var(--text-primary);
  background: var(--bg-hover);
}

/* 修复后的右键菜单统一样式 */
.custom-context-menu {
  position: fixed;
  background: var(--bg-container, #ffffff);
  border: 1px solid var(--border-color, #e7e7e7);
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
  border-radius: 6px;
  padding: 4px 0;
  min-width: 120px;
  z-index: 9999;
}

.custom-context-menu .menu-item {
  padding: 8px 16px;
  display: flex;
  align-items: center;
  cursor: pointer;
  font-size: 14px;
  color: var(--text-primary, #333);
  transition: background-color 0.2s;
}

.custom-context-menu .menu-item:hover {
  background: var(--bg-hover, #f3f3f3);
}

.custom-context-menu .menu-item.error {
  color: var(--error-color, #d54941);
}

.task-cell { display: flex; align-items: center; gap: 12px; }
.task-cell .completed { text-decoration: line-through; color: var(--text-tertiary); }
.assignee-cell { display: flex; align-items: center; gap: 8px; }
.avatar { width: 28px; height: 28px; border-radius: var(--radius-full); display: flex; align-items: center; justify-content: center; color: white; font-size: 11px; font-weight: 600; }
.overdue { color: var(--error-color); }
.project-title-row { display: flex; align-items: center; gap: 12px; }

@keyframes fadeInUp {
  from { opacity: 0; transform: translateY(20px); }
  to { opacity: 1; transform: translateY(0); }
}
</style>