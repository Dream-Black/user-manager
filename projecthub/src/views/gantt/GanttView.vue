<template>
  <div class="gantt-page" :style="{ '--task-name-column-width': `${LEFT_COLUMN_WIDTH}px` }">
    <div class="page-header">
      <div class="header-content">
        <h1 class="page-title">甘特图</h1>
        <p class="page-subtitle">可视化项目进度和时间安排</p>
      </div>
    </div>

    <div v-if="loading" class="gantt-loading">
      <t-loading size="large" text="加载甘特图数据..." />
    </div>

    <div v-else-if="ganttData.length === 0" class="gantt-empty">
      <t-empty title="暂无甘特图数据" description="请先创建项目或添加任务" />
    </div>

    <div v-else class="gantt-container">
      <div class="gantt-toolbar">
        <h2 class="gantt-title">甘特图</h2>
        <t-date-range-picker
          v-model="selectedRange"
          enable-time-picker
          clearable
          format="YYYY-MM-DD HH:mm:ss"
          separator="至"
          class="gantt-range-picker"
          @change="handleRangeChange"
        />
      </div>

      <div class="gantt-grid">
        <div class="gantt-fixed-column">
          <div class="gantt-fixed-header">任务名称</div>
          <div class="gantt-fixed-body">
            <div v-for="(project, pIndex) in ganttData" :key="project.id" class="project-row">
              <div class="project-header fixed-row-cell">
                <div class="task-name-cell">
                  <div class="project-icon" :style="{ background: project.color }">{{ project.name.charAt(0) }}</div>
                  <span class="project-name">{{ project.name }}</span>
                </div>
              </div>
              <div
                v-for="(task, tIndex) in project.tasks"
                :key="task.id"
                class="task-row fixed-row-cell"
                :style="{ animationDelay: `${(pIndex * 3 + tIndex) * 0.05}s` }"
              >
                <div class="task-name-cell">
                  <span class="task-indent"></span>
                  <span class="task-name">{{ task.name }}</span>
                </div>
              </div>
            </div>
          </div>
        </div>

        <div class="gantt-scroll-area" ref="ganttContainer">
          <div class="gantt-scroll-inner" :style="{ width: timelineWidth + 'px' }">
            <div class="timeline-header">
              <div class="timeline-dates-wrapper" :style="{ '--day-width': dayWidth + 'px', width: timelineContentWidth + 'px' }">
                <div class="timeline-months">
                  <template v-for="(group, index) in monthGroups" :key="index">
                    <div class="month-cell" :style="{ width: (group.days * dayWidth) + 'px' }">{{ group.month }}</div>
                  </template>
                </div>
                <div class="timeline-dates">
                  <div v-for="date in dateRange" :key="date" class="date-cell" :class="{ today: isToday(date), weekend: isWeekend(date) }">
                    <span class="date-weekday">{{ formatWeekday(date) }}</span>
                    <span class="date-day">{{ dayjs(date).date() }}</span>
                  </div>
                </div>
              </div>
            </div>

            <div class="gantt-content">
              <div v-for="(project, pIndex) in ganttData" :key="project.id" class="project-row">
                <div class="project-header timeline-row-cell">
                  <div class="task-bar-wrapper" :style="{ '--day-width': dayWidth + 'px', width: timelineContentWidth + 'px' }">
                    <div class="task-bar project-bar" :style="{ left: getLeftPosition(project.startDate) + 'px', width: getWidth(project.startDate, project.endDate) + 'px', background: project.color }">
                      <div class="task-progress" :style="{ width: (project.progress || 0) + '%' }"></div>
                      <span class="bar-label">{{ project.progress }}%</span>
                    </div>
                  </div>
                </div>

                <div v-for="(task, tIndex) in project.tasks" :key="task.id" class="task-row timeline-row-cell" :style="{ animationDelay: `${(pIndex * 3 + tIndex) * 0.05}s` }">
                  <div class="task-bar-wrapper" :style="{ '--day-width': dayWidth + 'px', width: timelineContentWidth + 'px' }">
                    <div
                      class="task-bar"
                      :class="{
                        completed: task.status === 'completed',
                        overdue: isOverdue(task),
                        dragging: draggingTask?.id === task.id && dragType === 'move',
                        'resizing-left': draggingTask?.id === task.id && dragType === 'left',
                        'resizing-right': draggingTask?.id === task.id && dragType === 'right'
                      }"
                      :style="{
                        left: (tempTaskPositions[task.id]?.left ?? getLeftPosition(task.startDate)) + 'px',
                        width: (tempTaskPositions[task.id]?.width ?? getWidth(task.startDate, task.endDate)) + 'px',
                        background: getTaskBarColor(task, project.color)
                      }"
                      @mousedown.stop="onTaskBarMouseDown($event, project, task)"
                    >
                      <div class="task-progress" :style="{ width: (task.progress || 0) + '%', background: getProgressColor(task) }"></div>
                      <div class="resize-handle resize-handle-left" :class="{ active: draggingTask?.id === task.id && dragType === 'left' }" @mousedown.stop="onResizeHandleMouseDown($event, project, task, 'left')">
                        <span class="resize-arrow">&#x2194;</span>
                      </div>
                      <span class="bar-label">
                        {{ task.name }}
                        <span v-if="task.progress > 0" class="progress-text">({{ task.progress }}%)</span>
                      </span>
                      <div class="resize-handle resize-handle-right" :class="{ active: draggingTask?.id === task.id && dragType === 'right' }" @mousedown.stop="onResizeHandleMouseDown($event, project, task, 'right')">
                        <span class="resize-arrow">&#x2194;</span>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
              <div class="today-line" :style="{ left: todayPosition + 'px' }"></div>
            </div>
          </div>
        </div>
      </div>
    </div>

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
import { ref, computed, reactive, onMounted, onUnmounted, watch } from 'vue'
import dayjs from 'dayjs'
import { ganttService, taskService } from '@/services/dataService'

const dayWidth = ref(40)
const loading = ref(true)
const ganttData = ref([])
const LEFT_COLUMN_WIDTH = 280
const DEFAULT_RANGE_BEFORE = 7
const DEFAULT_RANGE_AFTER = 30
const DEFAULT_LOAD_DAYS = 60
const MIN_RANGE_DAYS = 1

const today = dayjs().startOf('day')
const createDefaultRange = () => ({
  start: dayjs().startOf('day').subtract(DEFAULT_RANGE_BEFORE, 'day').format('YYYY-MM-DD HH:mm:ss'),
  end: dayjs().startOf('day').add(DEFAULT_RANGE_AFTER, 'day').format('YYYY-MM-DD HH:mm:ss'),
})

const { start: initialStart, end: initialEnd } = createDefaultRange()
const dateRangeStart = ref(dayjs(initialStart).startOf('day'))
const dateRangeEnd = ref(dayjs(initialEnd).startOf('day'))
const selectedRange = ref([initialStart, initialEnd])

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

// ============ 拖拽逻辑 ============

// 获取时间轴起点
const getTimelineStart = () => dateRangeStart.value
const getTaskNameColumnWidth = () => `${LEFT_COLUMN_WIDTH}px`
const timelineContentWidth = computed(() => dateRange.value.length * dayWidth.value)
const timelineWidth = computed(() => timelineContentWidth.value)

// 任务条鼠标按下 - 中间区域移动任务
const onTaskBarMouseDown = (event, project, task) => {
  if (task.status === 'completed') return
  startMove(event, project, task)
}

// 边缘调整区域鼠标按下 - 调整任务大小
const onResizeHandleMouseDown = (event, project, task, direction) => {
  if (task.status === 'completed') return
  event.preventDefault()
  startResize(event, project, task, direction)
}

// 开始拖拽移动
const startMove = (event, project, task) => {
  event.preventDefault()
  draggingTask.value = { ...task, projectId: project.id }
  dragType.value = 'move'
  dragStartX.value = event.clientX
  dragOriginalLeft.value = getLeftPosition(task.startDate)
  dragOriginalWidth.value = getWidth(task.startDate, task.endDate)

  tempTaskPositions[task.id] = {
    left: dragOriginalLeft.value,
    width: dragOriginalWidth.value
  }

  document.addEventListener('mousemove', onDragMove)
  document.addEventListener('mouseup', onDragEnd)
}

// 开始调整大小
const startResize = (event, project, task, direction) => {
  event.preventDefault()
  event.stopPropagation()
  draggingTask.value = { ...task, projectId: project.id }
  dragType.value = direction
  dragStartX.value = event.clientX
  dragOriginalLeft.value = getLeftPosition(task.startDate)
  dragOriginalWidth.value = getWidth(task.startDate, task.endDate)

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
    // 移动模式：左右平移，限制不能超出最左侧边界
    const newLeft = Math.max(0, dragOriginalLeft.value + deltaX)
    tempTaskPositions[task.id] = {
      left: newLeft,
      width: dragOriginalWidth.value
    }
  } else if (dragType.value === 'left') {
    // 左侧调整：改变左边距和宽度
    let newLeft = dragOriginalLeft.value + deltaX
    let newWidth = dragOriginalWidth.value + (dragOriginalLeft.value - newLeft)

    // 限制最小宽度为1天
    if (newWidth < dayWidth.value) {
      newWidth = dayWidth.value
      newLeft = dragOriginalLeft.value + dragOriginalWidth.value - dayWidth.value
    }
    
    // 限制不能超出时间轴左侧起点
    if (newLeft < 0) {
      newLeft = 0
      newWidth = dragOriginalLeft.value + dragOriginalWidth.value
    }

    tempTaskPositions[task.id] = {
      left: newLeft,
      width: newWidth
    }
  } else if (dragType.value === 'right') {
    // 右侧调整：只改变宽度，固定左侧。最小宽度1天
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

    const startDays = Math.round(newPosition.left / dayWidth.value)
    const durationDays = Math.max(1, Math.round(newPosition.width / dayWidth.value))

    const newStartDate = getTimelineStart().add(startDays, 'day').format('YYYY-MM-DD')
    const newEndDate = getTimelineStart().add(startDays + durationDays - 1, 'day').format('YYYY-MM-DD')

    if (newStartDate !== originalStart || newEndDate !== originalEnd) {
      updateTaskDates(task.id, newStartDate, newEndDate)
    } else {
      delete tempTaskPositions[task.id]
    }
  }

  draggingTask.value = null
  dragType.value = null

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
  }, 500)
}

// 更新本地任务数据
const updateLocalTask = (taskId, startDate, endDate) => {
  ganttData.value.forEach(project => {
    const task = project.tasks.find(t => t.id === taskId)
    if (task) {
      task.startDate = startDate
      task.endDate = endDate
      // 清除临时位置，直接渲染真实位置
      delete tempTaskPositions[task.id]
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

const normalizeTask = (task) => ({
  id: task.id,
  name: task.title,
  startDate: task.planStartDate,
  endDate: task.planEndDate,
  status: task.status,
  progress: task.progress || 0,
  estimatedHours: task.estimatedHours || 0,
})

const recalculateProjectRange = (project) => {
  if (project.tasks.length === 0) return

  project.startDate = project.tasks.reduce((earliest, task) => (
    !earliest || dayjs(task.startDate).isBefore(earliest) ? task.startDate : earliest
  ), null)

  project.endDate = project.tasks.reduce((latest, task) => (
    !latest || dayjs(task.endDate).isAfter(latest) ? task.endDate : latest
  ), null)
}

const calculateProjectProgress = (project) => {
  if (project.tasks.length === 0) return 0

  const totalHours = project.tasks.reduce((sum, task) => sum + (task.estimatedHours || 0), 0)
  if (totalHours > 0) {
    const weightedProgress = project.tasks.reduce((sum, task) => sum + ((task.estimatedHours || 0) * (task.progress || 0) / 100), 0)
    return Math.round((weightedProgress / totalHours) * 100)
  }

  const totalProgress = project.tasks.reduce((sum, task) => sum + (task.progress || 0), 0)
  return Math.round(totalProgress / project.tasks.length)
}

const buildGanttProjects = (items) => {
  const projectMap = new Map()

  items.forEach((task) => {
    if (!projectMap.has(task.projectId)) {
      projectMap.set(task.projectId, {
        id: task.projectId,
        name: task.projectName,
        color: task.projectColor || '#4A90D9',
        startDate: task.planStartDate,
        endDate: task.planEndDate,
        progress: 0,
        tasks: [],
      })
    }

    const project = projectMap.get(task.projectId)
    project.tasks.push(normalizeTask(task))
    recalculateProjectRange(project)
  })

  projectMap.forEach((project) => {
    project.progress = calculateProjectProgress(project)
  })

  return Array.from(projectMap.values())
}

const applyTimelineRange = (range) => {
  if (!range || range.length !== 2 || !range[0] || !range[1]) return

  const start = dayjs(range[0]).startOf('day')
  const end = dayjs(range[1]).startOf('day')

  if (end.isBefore(start)) return

  const diffDays = end.diff(start, 'day')
  if (diffDays < MIN_RANGE_DAYS - 1) {
    return
  }

  dateRangeStart.value = start
  dateRangeEnd.value = end
  selectedRange.value = [start.format('YYYY-MM-DD HH:mm:ss'), end.format('YYYY-MM-DD HH:mm:ss')]
}

const handleRangeChange = (value) => {
  if (!value || value.length !== 2) return
  applyTimelineRange(value)
}

const syncSelectedRange = () => {
  selectedRange.value = [
    dateRangeStart.value.format('YYYY-MM-DD HH:mm:ss'),
    dateRangeEnd.value.format('YYYY-MM-DD HH:mm:ss'),
  ]
}

const loadGanttData = async () => {
  try {
    loading.value = true
    const response = await ganttService.getData({ days: DEFAULT_LOAD_DAYS })
    ganttData.value = buildGanttProjects(response.items || [])

    if (ganttData.value.length === 0) {
      const { start, end } = createDefaultRange()
      applyTimelineRange([start, end])
      return
    }

    const allDates = ganttData.value.flatMap((project) => [project.startDate, project.endDate].filter(Boolean))
    if (allDates.length > 0) {
      const earliest = allDates.reduce((min, date) => (dayjs(date).isBefore(min) ? date : min), allDates[0])
      const latest = allDates.reduce((max, date) => (dayjs(date).isAfter(max) ? date : max), allDates[0])
      applyTimelineRange([
        dayjs(earliest).startOf('day').subtract(DEFAULT_RANGE_BEFORE, 'day').format('YYYY-MM-DD HH:mm:ss'),
        dayjs(latest).startOf('day').add(DEFAULT_RANGE_AFTER, 'day').format('YYYY-MM-DD HH:mm:ss'),
      ])
    }
  } catch (error) {
    console.error('加载甘特图数据失败:', error)
  } finally {
    loading.value = false
  }
}

onMounted(() => {
  loadGanttData()
})

watch([dateRangeStart, dateRangeEnd], syncSelectedRange, { immediate: true })

onUnmounted(() => {
  if (debounceTimer) {
    clearTimeout(debounceTimer)
  }
  document.removeEventListener('mousemove', onDragMove)
  document.removeEventListener('mouseup', onDragEnd)
})

// ============ 工具函数 ============

// 判断任务是否延期
const isOverdue = (task) => {
  if (task.status === 'completed') return false
  if (!task.endDate) return false
  return dayjs(task.endDate).isBefore(today, 'day')
}

// 根据任务状态获取颜色
const getTaskBarColor = (task, projectColor) => {
  if (task.status === 'completed') return '#10B981' // 完成青色
  if (isOverdue(task)) return '#EF4444' // 延期红色
  if (task.status === 'in_progress' || task.status === 'pending') return '#4A90D9' // 进行中蓝色
  return '#9CA3AF' // 未开始灰色
}

// 获取进度条颜色（半透明白色）
const getProgressColor = (task) => {
  return 'rgba(255, 255, 255, 0.4)'
}

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
  return Math.max(0, daysSinceStart * dayWidth.value + dayWidth.value / 2)
})

const getLeftPosition = (date) => {
  const startDate = dateRangeStart.value
  const currentDate = dayjs(date).startOf('day')
  const days = currentDate.diff(startDate, 'day')
  return Math.max(0, days * dayWidth.value)
}

const getWidth = (startDate, endDate) => {
  const start = dayjs(startDate).startOf('day')
  const end = dayjs(endDate).startOf('day')
  const timelineStart = dateRangeStart.value
  const timelineEnd = dateRangeEnd.value

  const effectiveStart = start.isBefore(timelineStart) ? timelineStart : start
  const effectiveEnd = end.isAfter(timelineEnd) ? timelineEnd : end

  if (effectiveStart.isAfter(timelineEnd) || effectiveEnd.isBefore(timelineStart)) {
    return 0
  }

  const days = effectiveEnd.diff(effectiveStart, 'day') + 1
  return Math.max(0, days * dayWidth.value)
}

const isToday = (date) => dayjs(date).isSame(today, 'day')
const isWeekend = (date) => [0, 6].includes(dayjs(date).day())
const formatWeekday = (date) => {
  const weekdays = ['日', '一', '二', '三', '四', '五', '六']
  return weekdays[dayjs(date).day()]
}

// 月份分组
const monthGroups = computed(() => {
  const groups = []
  let currentMonth = ''
  let currentDays = 0

  dateRange.value.forEach((date) => {
    const monthStr = `${dayjs(date).month() + 1}月`
    if (monthStr !== currentMonth) {
      if (currentMonth) {
        groups.push({ month: currentMonth, days: currentDays })
      }
      currentMonth = monthStr
      currentDays = 1
    } else {
      currentDays++
    }
  })

  if (currentMonth) {
    groups.push({ month: currentMonth, days: currentDays })
  }

  return groups
})
</script>

<style scoped>
.gantt-page { max-width: 100%; overflow: hidden; }
.gantt-loading { display: flex; justify-content: center; align-items: center; min-height: 400px; background: var(--bg-container); border-radius: var(--radius-xl); }
.gantt-empty { display: flex; justify-content: center; align-items: center; min-height: 400px; background: var(--bg-container); border-radius: var(--radius-xl); }

.gantt-container,
.gantt-loading,
.gantt-empty {
  max-width: 1440px;
  margin: 0 auto;
}

.gantt-loading,
.gantt-empty {
  padding: 24px;
  box-sizing: border-box;
}
.page-header { display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 24px; animation: fadeInUp 0.5s ease; }
.header-content { display: flex; flex-direction: column; gap: 4px; }
.page-title { font-size: 28px; font-weight: 700; color: var(--text-primary); margin: 0; }
.page-subtitle { font-size: 14px; color: var(--text-secondary); margin: 0; }
.header-actions { display: flex; gap: 8px; }

.gantt-container { background: var(--bg-container); border-radius: var(--radius-xl); border: 1px solid var(--border-color); animation: fadeInUp 0.5s ease 0.1s backwards; display: flex; flex-direction: column; gap: 16px; padding: 32px; box-sizing: border-box; }
.gantt-toolbar { display: flex; align-items: center; justify-content: space-between; gap: 16px; }
.toolbar-title { display: flex; flex-direction: column; gap: 4px; }
.toolbar-title h2 { margin: 0; font-size: 16px; font-weight: 600; color: var(--text-primary); }
.toolbar-title span { font-size: 12px; color: var(--text-secondary); }
.gantt-grid { display: grid; grid-template-columns: var(--task-name-column-width, 280px) minmax(0, 1fr); align-items: start; min-height: 400px; border: 1px solid var(--border-color); border-radius: var(--radius-lg); overflow: hidden; }
.gantt-fixed-column { position: relative; z-index: 5; background: var(--bg-container); border-right: 1px solid var(--border-color); overflow: hidden; }
.gantt-fixed-header { height: 75px; padding: 12px 16px; display: flex; align-items: center; font-size: 13px; font-weight: 600; color: var(--text-secondary); border-bottom: 1px solid var(--border-color); box-sizing: border-box; background: var(--bg-page); }
.gantt-fixed-body { position: relative; }
.fixed-row-cell { background: var(--bg-container); }
.gantt-scroll-area { position: relative; overflow-x: auto; overflow-y: auto; min-height: 400px; background: var(--bg-container); scrollbar-gutter: stable both-edges; margin-left: -6px; }
.gantt-scroll-inner { position: relative; min-width: max-content; }
.timeline-header { display: flex; border-bottom: 1px solid var(--border-color); background: var(--bg-page); min-width: max-content; position: sticky; top: 0; z-index: 8; }
.timeline-dates-wrapper { display: flex; flex-direction: column; flex: 1; min-width: max-content; }
.timeline-months { display: flex; border-bottom: 1px solid var(--border-light); }
.month-cell { flex-shrink: 0; padding: 6px 0; text-align: center; font-size: 12px; font-weight: 600; color: var(--text-primary); border-right: 1px solid var(--border-light); box-sizing: border-box; }
.timeline-dates { display: flex; flex: 1; }
.date-cell { flex: 0 0 var(--day-width); padding: 4px 0; text-align: center; border-right: 1px solid var(--border-light); box-sizing: border-box; }
.date-cell.weekend { background: rgba(0,0,0,0.02); }
.date-cell.today { background: rgba(33, 150, 243, 0.1); }
.date-weekday { display: block; font-size: 10px; color: var(--text-tertiary); }
.date-day { display: block; font-size: 12px; font-weight: 600; color: var(--text-primary); }

.gantt-content { min-height: 400px; min-width: max-content; position: relative; }
.project-row { border-bottom: 1px solid var(--border-color); }
.project-header { display: flex; height: 56px; background: var(--bg-page); }
.task-row { display: flex; height: 44px; border-top: 1px solid var(--border-light); animation: fadeIn 0.4s ease backwards; position: relative; }
.task-row:hover { background: var(--bg-hover); }

.task-name-cell { width: var(--task-name-column-width, 280px); flex-shrink: 0; padding: 0 16px; display: flex; align-items: center; gap: 8px; border-right: 1px solid var(--border-color); box-sizing: border-box; position: sticky; left: 0; z-index: 6; background: var(--bg-container); }
.project-icon { width: 28px; height: 28px; border-radius: var(--radius-md); display: flex; align-items: center; justify-content: center; color: white; font-size: 12px; font-weight: 600; }
.project-name { font-size: 13px; font-weight: 600; color: var(--text-primary); }
.task-indent { width: 16px; }
.task-name { font-size: 13px; color: var(--text-secondary); }

.task-bar-wrapper { position: relative; padding: 8px 0; min-width: max-content; }

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
  cursor: grab;
  z-index: 1;
  overflow: hidden;
}

/* 进度填充条 */
.task-progress {
  position: absolute;
  left: 0;
  top: 0;
  bottom: 0;
  z-index: -1;
  border-radius: var(--radius-md);
}

/* 进度文字 */
.progress-text {
  margin-left: 4px;
  font-size: 10px;
  opacity: 0.9;
}

/* 边缘调整区域 */
.resize-handle {
  position: absolute;
  top: 0;
  bottom: 0;
  width: 10px;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: ew-resize;
  opacity: 0;
  transition: opacity 0.15s ease;
  z-index: 2;
}

.resize-handle-left {
  left: 0;
  border-radius: var(--radius-md) 0 0 var(--radius-md);
  background: linear-gradient(to right, rgba(0,0,0,0.15), transparent);
}

.resize-handle-right {
  right: 0;
  border-radius: 0 var(--radius-md) var(--radius-md) 0;
  background: linear-gradient(to left, rgba(0,0,0,0.15), transparent);
}

.resize-arrow {
  font-size: 10px;
  color: white;
  font-weight: bold;
  text-shadow: 0 1px 2px rgba(0,0,0,0.3);
}

/* 悬停时显示边缘手柄 */
.task-bar:hover {
  box-shadow: var(--shadow-md);
}

.task-bar:hover .resize-handle {
  opacity: 1;
}

/* 拖拽状态样式 */
.task-bar.dragging {
  cursor: grabbing;
  opacity: 0.9;
}

.task-bar.resizing-left,
.task-bar.resizing-right {
  cursor: ew-resize;
  opacity: 0.95;
}

/* 拖拽模式下的边缘样式 */
.resize-handle.active {
  opacity: 1 !important;
  background: rgba(255,255,255,0.4);
}

.project-bar { height: 32px; opacity: 0.8; }
.bar-label { font-size: 11px; font-weight: 600; color: white; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }

.today-line { position: absolute; top: 0; bottom: 0; width: 2px; background: #EF4444; z-index: 3; pointer-events: none; }
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