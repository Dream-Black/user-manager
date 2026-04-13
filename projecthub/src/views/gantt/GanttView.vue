<template>
  <div class="gantt-page">
    <div class="page-header">
      <div class="header-content">
        <h1 class="page-title">甘特图</h1>
        <p class="page-subtitle">可视化项目进度和时间安排</p>
      </div>
      <div class="header-actions">
        <t-button variant="outline" @click="zoomOut">缩小</t-button>
        <t-button variant="outline" @click="zoomIn">放大</t-button>
        <t-button theme="primary">
          <template #icon><AddIcon /></template>
          添加任务
        </t-button>
      </div>
    </div>

    <!-- 加载状态 -->
    <div v-if="loading" class="gantt-loading">
      <t-loading size="large" text="加载甘特图数据..." />
    </div>

    <!-- 空状态 -->
    <div v-else-if="ganttData.length === 0" class="gantt-empty">
      <t-empty title="暂无甘特图数据" description="请先创建项目或添加任务" />
    </div>

    <div v-else class="gantt-container" ref="ganttContainer">
      <!-- 时间轴头部 -->
      <div class="timeline-header">
        <div class="task-name-header">任务名称</div>
        <div class="timeline-dates" :style="{ '--day-width': dayWidth + 'px' }">
          <div
            v-for="date in dateRange"
            :key="date"
            class="date-cell"
            :class="{ today: isToday(date), weekend: isWeekend(date) }"
          >
            <span class="date-day">{{ formatDay(date) }}</span>
            <span class="date-month">{{ formatMonth(date) }}</span>
          </div>
        </div>
      </div>

      <!-- 甘特图内容 -->
      <div class="gantt-content">
        <div
          v-for="(project, pIndex) in ganttData"
          :key="project.id"
          class="project-row"
        >
          <!-- 项目行 -->
          <div class="project-header">
            <div class="task-name-cell">
              <div class="project-icon" :style="{ background: project.color }">
                {{ project.name.charAt(0) }}
              </div>
              <span class="project-name">{{ project.name }}</span>
            </div>
            <div class="task-bar-wrapper" :style="{ '--day-width': dayWidth + 'px' }">
              <div
                class="task-bar project-bar"
                :style="{
                  left: getLeftPosition(project.startDate) + 'px',
                  width: getWidth(project.startDate, project.endDate) + 'px',
                  background: project.color
                }"
              >
                <span class="bar-label">{{ project.progress }}%</span>
              </div>
            </div>
          </div>

          <!-- 任务行 -->
          <div
            v-for="(task, tIndex) in project.tasks"
            :key="task.id"
            class="task-row"
            :style="{ animationDelay: `${(pIndex * 3 + tIndex) * 0.05}s` }"
          >
            <div class="task-name-cell">
              <span class="task-indent"></span>
              <span class="task-name">{{ task.name }}</span>
            </div>
            <div class="task-bar-wrapper" :style="{ '--day-width': dayWidth + 'px' }">
              <!-- 拖拽手柄 - 左侧 -->
              <div
                class="resize-handle resize-handle-left"
                @mousedown.stop="startResize($event, project, task, 'left')"
              ></div>

              <!-- 任务条主体 - 可拖拽移动 -->
              <div
                class="task-bar"
                :class="{
                  completed: task.status === 'completed',
                  dragging: draggingTask?.id === task.id && dragType === 'move',
                  'resizing': draggingTask?.id === task.id && (dragType === 'left' || dragType === 'right')
                }"
                :style="{
                  left: (tempTaskPositions[task.id]?.left ?? getLeftPosition(task.startDate)) + 'px',
                  width: (tempTaskPositions[task.id]?.width ?? getWidth(task.startDate, task.endDate)) + 'px',
                  background: task.status === 'completed' ? '#10B981' : project.color,
                  cursor: draggingTask?.id === task.id ? 'grabbing' : 'grab'
                }"
                @mousedown.stop="startDrag($event, project, task)"
              >
                <span class="bar-label">{{ task.name }}</span>
              </div>

              <!-- 拖拽手柄 - 右侧 -->
              <div
                class="resize-handle resize-handle-right"
                @mousedown.stop="startResize($event, project, task, 'right')"
              ></div>
            </div>
          </div>
        </div>
      </div>

      <!-- 今日线 -->
      <div class="today-line" :style="{ left: todayPosition + 'px' }"></div>
    </div>

    <!-- 保存提示 -->
    <transition name="fade">
      <div v-if="saveStatus" class="save-indicator" :class="saveStatus">
        <t-loading v-if="saveStatus === 'saving'" size="small" text="保存中..." />
        <t-icon v-else-if="saveStatus === 'saved'" name="check-circle-filled" style="color: #10B981;" />
        <t-icon v-else-if="saveStatus === 'error'" name="error-circle-filled" style="color: #EF4444;" />
      </div>
    </transition>
  </div>
</template>

<script setup>
import { ref, computed, reactive, onMounted, onUnmounted } from 'vue'
import dayjs from 'dayjs'
import { ganttService, taskService } from '@/services/dataService'
import { AddIcon } from 'tdesign-icons-vue-next'

const dayWidth = ref(40)
const loading = ref(true)
const ganttData = ref([])
const dateRangeStart = ref(dayjs().subtract(7, 'day'))
const dateRangeEnd = ref(dayjs().add(30, 'day'))

// 拖拽相关状态
const ganttContainer = ref(null)
const draggingTask = ref(null) // { id, projectId, startDate, endDate }
const dragType = ref(null) // 'move' | 'left' | 'right'
const dragStartX = ref(0)
const dragOriginalLeft = ref(0)
const dragOriginalWidth = ref(0)
const tempTaskPositions = reactive({}) // 临时位置，用于实时预览

// 保存状态
const saveStatus = ref(null) // null | 'saving' | 'saved' | 'error'

// 防抖定时器
let debounceTimer = null

const zoomIn = () => { dayWidth.value = Math.min(80, dayWidth.value + 10) }
const zoomOut = () => { dayWidth.value = Math.max(20, dayWidth.value - 10) }

const today = dayjs()

// ============ 拖拽逻辑 ============

// 获取时间轴起点（用于计算位置）
const getTimelineStart = () => {
  return dateRangeStart.value
}

// 根据像素位置计算日期
const pixelToDate = (pixelX) => {
  const timelineStart = getTimelineStart()
  const relativeX = pixelX - 200 // 200 是任务名称列宽度
  const days = Math.round(relativeX / dayWidth.value)
  return timelineStart.add(days, 'day').format('YYYY-MM-DD')
}

// 根据日期计算像素位置
const dateToPixel = (date) => {
  const timelineStart = getTimelineStart()
  const days = dayjs(date).diff(timelineStart, 'day')
  return 200 + days * dayWidth.value
}

// 开始拖拽移动
const startDrag = (event, project, task) => {
  if (task.status === 'completed') return // 已完成任务不可拖拽

  event.preventDefault()
  draggingTask.value = { ...task, projectId: project.id }
  dragType.value = 'move'
  dragStartX.value = event.clientX
  dragOriginalLeft.value = getLeftPosition(task.startDate)
  dragOriginalWidth.value = getWidth(task.startDate, task.endDate)

  // 初始化临时位置
  tempTaskPositions[task.id] = {
    left: dragOriginalLeft.value,
    width: dragOriginalWidth.value
  }

  document.addEventListener('mousemove', onDragMove)
  document.addEventListener('mouseup', onDragEnd)
}

// 开始调整大小
const startResize = (event, project, task, direction) => {
  if (task.status === 'completed') return

  event.preventDefault()
  event.stopPropagation()
  draggingTask.value = { ...task, projectId: project.id }
  dragType.value = direction
  dragStartX.value = event.clientX
  dragOriginalLeft.value = getLeftPosition(task.startDate)
  dragOriginalWidth.value = getWidth(task.startDate, task.endDate)

  // 初始化临时位置
  tempTaskPositions[task.id] = {
    left: dragOriginalLeft.value,
    width: dragOriginalWidth.value
  }

  document.addEventListener('mousemove', onDragMove)
  document.addEventListener('mouseup', onDragEnd)
}

// 拖拽移动中
const onDragMove = (event) => {
  if (!draggingTask.value) return

  const task = draggingTask.value
  const deltaX = event.clientX - dragStartX.value

  if (dragType.value === 'move') {
    // 移动模式：左右平移
    tempTaskPositions[task.id] = {
      left: dragOriginalLeft.value + deltaX,
      width: dragOriginalWidth.value
    }
  } else if (dragType.value === 'left') {
    // 左侧调整：改变左边距和宽度
    const newLeft = Math.max(0, dragOriginalLeft.value + deltaX)
    const newWidth = dragOriginalWidth.value + (dragOriginalLeft.value - newLeft)
    if (newWidth >= dayWidth.value) { // 最小宽度为1天
      tempTaskPositions[task.id] = {
        left: newLeft,
        width: newWidth
      }
    }
  } else if (dragType.value === 'right') {
    // 右侧调整：只改变宽度
    const newWidth = Math.max(dayWidth.value, dragOriginalWidth.value + deltaX)
    tempTaskPositions[task.id] = {
      left: dragOriginalLeft.value,
      width: newWidth
    }
  }
}

// 拖拽结束
const onDragEnd = () => {
  if (!draggingTask.value) return

  const task = draggingTask.value
  const newPosition = tempTaskPositions[task.id]

  if (newPosition) {
    const originalStart = task.startDate
    const originalEnd = task.endDate

    // 计算新的开始和结束日期
    const newStartDate = pixelToDate(newPosition.left)
    const newEndDate = pixelToDate(newPosition.left + newPosition.width - 1)

    // 只有日期发生变化才调用API
    if (newStartDate !== originalStart || newEndDate !== originalEnd) {
      updateTaskDates(task.id, newStartDate, newEndDate)
    }
  }

  // 清理状态
  draggingTask.value = null
  dragType.value = null
  delete tempTaskPositions[task.id]

  document.removeEventListener('mousemove', onDragMove)
  document.removeEventListener('mouseup', onDragEnd)
}

// 防抖保存任务日期
const updateTaskDates = (taskId, startDate, endDate) => {
  // 先立即更新本地UI
  updateLocalTask(taskId, startDate, endDate)

  // 防抖调用API
  if (debounceTimer) {
    clearTimeout(debounceTimer)
  }

  saveStatus.value = 'saving'

  debounceTimer = setTimeout(async () => {
    try {
      await taskService.update(taskId, {
        planStartDate: startDate,
        planEndDate: endDate
      })
      saveStatus.value = 'saved'
      setTimeout(() => { saveStatus.value = null }, 2000)
    } catch (error) {
      console.error('更新任务时间失败:', error)
      saveStatus.value = 'error'
      setTimeout(() => { saveStatus.value = null }, 3000)
      // 回滚本地更改
      loadGanttData()
    }
  }, 500) // 500ms 防抖
}

// 更新本地任务数据
const updateLocalTask = (taskId, startDate, endDate) => {
  ganttData.value.forEach(project => {
    const task = project.tasks.find(t => t.id === taskId)
    if (task) {
      task.startDate = startDate
      task.endDate = endDate
    }
    // 同时更新项目的开始结束日期
    updateProjectDates(project)
  })
}

// 更新项目的开始结束日期
const updateProjectDates = (project) => {
  if (project.tasks.length === 0) return

  let minStart = project.tasks[0].startDate
  let maxEnd = project.tasks[0].endDate

  project.tasks.forEach(task => {
    if (dayjs(task.startDate).isBefore(minStart)) {
      minStart = task.startDate
    }
    if (dayjs(task.endDate).isAfter(maxEnd)) {
      maxEnd = task.endDate
    }
  })

  project.startDate = minStart
  project.endDate = maxEnd
}

// ============ 数据加载 ============

// 加载甘特图数据
const loadGanttData = async () => {
  try {
    loading.value = true
    const response = await ganttService.getData({ days: 60 })

    // 将扁平的任务列表转换为按项目分组的嵌套结构
    const projectMap = new Map()

    response.items.forEach(task => {
      if (!projectMap.has(task.projectId)) {
        projectMap.set(task.projectId, {
          id: task.projectId,
          name: task.projectName,
          color: task.projectColor || '#4A90D9',
          startDate: task.startDate,
          endDate: task.endDate,
          progress: 0,
          tasks: []
        })
      }

      const project = projectMap.get(task.projectId)
      project.tasks.push({
        id: task.id,
        name: task.title,
        startDate: task.startDate,
        endDate: task.endDate,
        status: task.status,
        progress: task.progress || 0
      })

      // 更新项目的开始/结束日期和进度
      if (task.startDate && (!project.startDate || dayjs(task.startDate).isBefore(project.startDate))) {
        project.startDate = task.startDate
      }
      if (task.endDate && (!project.endDate || dayjs(task.endDate).isAfter(project.endDate))) {
        project.endDate = task.endDate
      }
    })

    // 计算每个项目的总进度
    projectMap.forEach(project => {
      if (project.tasks.length > 0) {
        const totalProgress = project.tasks.reduce((sum, t) => sum + (t.progress || 0), 0)
        project.progress = Math.round(totalProgress / project.tasks.length)
      }
    })

    ganttData.value = Array.from(projectMap.values())
    dateRangeStart.value = dayjs(response.startDate)
    dateRangeEnd.value = dayjs(response.endDate)
  } catch (error) {
    console.error('加载甘特图数据失败:', error)
  } finally {
    loading.value = false
  }
}

onMounted(() => {
  loadGanttData()
})

onUnmounted(() => {
  if (debounceTimer) {
    clearTimeout(debounceTimer)
  }
  document.removeEventListener('mousemove', onDragMove)
  document.removeEventListener('mouseup', onDragEnd)
})

// ============ 工具函数 ============

const dateRange = computed(() => {
  const days = []
  let current = dateRangeStart.value
  while (current.isBefore(dateRangeEnd.value) || current.isSame(dateRangeEnd.value, 'day')) {
    days.push(current)
    current = current.add(1, 'day')
  }
  return days
})

const todayPosition = computed(() => {
  const daysSinceStart = today.diff(dateRangeStart.value, 'day')
  return 200 + daysSinceStart * dayWidth.value + dayWidth.value / 2
})

const getLeftPosition = (date) => {
  const startDate = dateRange.value[0]
  const currentDate = dayjs(date)
  const days = currentDate.diff(startDate, 'day')
  return 200 + days * dayWidth.value
}

const getWidth = (startDate, endDate) => {
  const start = dayjs(startDate)
  const end = dayjs(endDate)
  const days = end.diff(start, 'day') + 1
  return days * dayWidth.value
}

const isToday = (date) => dayjs(date).isSame(today, 'day')
const isWeekend = (date) => [0, 6].includes(dayjs(date).day())
const formatDay = (date) => dayjs(date).format('D')
const formatMonth = (date) => dayjs(date).format('MMM')
</script>

<style scoped>
.gantt-page { max-width: 100%; overflow-x: auto; }
.gantt-loading { display: flex; justify-content: center; align-items: center; min-height: 400px; background: var(--bg-container); border-radius: var(--radius-xl); }
.gantt-empty { display: flex; justify-content: center; align-items: center; min-height: 400px; background: var(--bg-container); border-radius: var(--radius-xl); }
.page-header { display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 24px; animation: fadeInUp 0.5s ease; }
.header-content { display: flex; flex-direction: column; gap: 4px; }
.page-title { font-size: 28px; font-weight: 700; color: var(--text-primary); margin: 0; }
.page-subtitle { font-size: 14px; color: var(--text-secondary); margin: 0; }
.header-actions { display: flex; gap: 8px; }

.gantt-container { background: var(--bg-container); border-radius: var(--radius-xl); border: 1px solid var(--border-color); overflow: hidden; position: relative; animation: fadeInUp 0.5s ease 0.1s backwards; }

.timeline-header { display: flex; border-bottom: 1px solid var(--border-color); background: var(--bg-page); }
.task-name-header { width: 200px; flex-shrink: 0; padding: 12px 16px; font-size: 13px; font-weight: 600; color: var(--text-secondary); border-right: 1px solid var(--border-color); }
.timeline-dates { display: flex; flex: 1; }

.date-cell { flex: 0 0 var(--day-width); padding: 8px 0; text-align: center; border-right: 1px solid var(--border-light); }
.date-cell.weekend { background: rgba(0,0,0,0.02); }
.date-cell.today { background: rgba(33, 150, 243, 0.1); }
.date-day { display: block; font-size: 12px; font-weight: 600; color: var(--text-primary); }
.date-month { display: block; font-size: 10px; color: var(--text-tertiary); }

.gantt-content { min-height: 400px; }
.project-row { border-bottom: 1px solid var(--border-color); }
.project-header { display: flex; height: 56px; background: var(--bg-page); }
.task-row { display: flex; height: 44px; border-top: 1px solid var(--border-light); animation: fadeIn 0.4s ease backwards; position: relative; }
.task-row:hover { background: var(--bg-hover); }

.task-name-cell { width: 200px; flex-shrink: 0; padding: 0 16px; display: flex; align-items: center; gap: 8px; border-right: 1px solid var(--border-color); }
.project-icon { width: 28px; height: 28px; border-radius: var(--radius-md); display: flex; align-items: center; justify-content: center; color: white; font-size: 12px; font-weight: 600; }
.project-name { font-size: 13px; font-weight: 600; color: var(--text-primary); }
.task-indent { width: 16px; }
.task-name { font-size: 13px; color: var(--text-secondary); }

.task-bar-wrapper { flex: 1; position: relative; padding: 8px 0; }

/* 任务条基础样式 */
.task-bar {
  position: absolute;
  height: 28px;
  border-radius: var(--radius-md);
  display: flex;
  align-items: center;
  padding: 0 8px;
  min-width: 40px;
  transition: box-shadow 0.2s ease;
  user-select: none;
}

.task-bar:hover {
  box-shadow: var(--shadow-md);
  z-index: 5;
}

/* 拖拽状态样式 */
.task-bar.dragging,
.task-bar.resizing {
  opacity: 0.9;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.3);
  z-index: 100;
  transform: scale(1.02);
}

/* 拖拽手柄 */
.resize-handle {
  position: absolute;
  top: 0;
  bottom: 0;
  width: 10px;
  cursor: ew-resize;
  z-index: 10;
  opacity: 0;
  transition: opacity 0.2s ease;
}

.resize-handle-left {
  left: 0;
  border-radius: var(--radius-md) 0 0 var(--radius-md);
  background: linear-gradient(to right, rgba(255,255,255,0.3), transparent);
}

.resize-handle-right {
  right: 0;
  border-radius: 0 var(--radius-md) var(--radius-md) 0;
  background: linear-gradient(to left, rgba(255,255,255,0.3), transparent);
}

.task-row:hover .resize-handle {
  opacity: 1;
}

.resize-handle:hover {
  background: rgba(255,255,255,0.5) !important;
}

.project-bar { height: 32px; opacity: 0.8; }
.bar-label { font-size: 11px; font-weight: 600; color: white; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }

.today-line { position: absolute; top: 0; bottom: 0; width: 2px; background: #EF4444; z-index: 10; pointer-events: none; }
.today-line::before { content: '今天'; position: absolute; top: 0; left: 50%; transform: translateX(-50%); font-size: 10px; font-weight: 600; color: #EF4444; background: var(--bg-container); padding: 2px 4px; border-radius: 4px; }

/* 保存状态提示 */
.save-indicator {
  position: fixed;
  bottom: 24px;
  right: 24px;
  background: var(--bg-container);
  border: 1px solid var(--border-color);
  border-radius: var(--radius-lg);
  padding: 12px 16px;
  display: flex;
  align-items: center;
  gap: 8px;
  box-shadow: var(--shadow-lg);
  z-index: 1000;
}

.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.3s ease, transform 0.3s ease;
}

.fade-enter-from,
.fade-leave-to {
  opacity: 0;
  transform: translateY(10px);
}
</style>
