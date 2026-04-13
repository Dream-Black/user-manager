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

    <div class="gantt-container">
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
              <div
                class="task-bar"
                :class="{ completed: task.status === 'completed' }"
                :style="{
                  left: getLeftPosition(task.startDate) + 'px',
                  width: getWidth(task.startDate, task.endDate) + 'px',
                  background: task.status === 'completed' ? '#10B981' : project.color
                }"
              >
                <span class="bar-label">{{ task.name }}</span>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- 今日线 -->
      <div class="today-line" :style="{ left: todayPosition + 'px' }"></div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed } from 'vue'
import dayjs from 'dayjs'

const dayWidth = ref(40)
const zoomIn = () => { dayWidth.value = Math.min(80, dayWidth.value + 10) }
const zoomOut = () => { dayWidth.value = Math.max(20, dayWidth.value - 10) }

const today = dayjs()

const dateRange = computed(() => {
  const start = today.subtract(7, 'day')
  const end = today.add(30, 'day')
  const days = []
  let current = start
  while (current.isBefore(end) || current.isSame(end, 'day')) {
    days.push(current)
    current = current.add(1, 'day')
  }
  return days
})

const todayPosition = computed(() => {
  const daysSinceStart = today.diff(dateRange.value[0], 'day')
  return 200 + daysSinceStart * dayWidth.value + dayWidth.value / 2
})

const ganttData = ref([
  {
    id: 1, name: 'Website Redesign', color: '#2196F3',
    startDate: '2026-04-01', endDate: '2026-04-30', progress: 75,
    tasks: [
      { id: 11, name: '需求分析', startDate: '2026-04-01', endDate: '2026-04-05', status: 'completed' },
      { id: 12, name: 'UI设计', startDate: '2026-04-06', endDate: '2026-04-15', status: 'completed' },
      { id: 13, name: '前端开发', startDate: '2026-04-16', endDate: '2026-04-25', status: 'in_progress' },
      { id: 14, name: '测试上线', startDate: '2026-04-26', endDate: '2026-04-30', status: 'pending' }
    ]
  },
  {
    id: 2, name: 'App Development', color: '#4CAF50',
    startDate: '2026-04-10', endDate: '2026-06-30', progress: 45,
    tasks: [
      { id: 21, name: '技术选型', startDate: '2026-04-10', endDate: '2026-04-15', status: 'completed' },
      { id: 22, name: '原型设计', startDate: '2026-04-16', endDate: '2026-04-30', status: 'completed' },
      { id: 23, name: 'iOS开发', startDate: '2026-05-01', endDate: '2026-06-15', status: 'in_progress' },
      { id: 24, name: 'Android开发', startDate: '2026-05-15', endDate: '2026-06-20', status: 'pending' }
    ]
  },
  {
    id: 3, name: 'API Integration', color: '#FF9800',
    startDate: '2026-04-05', endDate: '2026-04-25', progress: 90,
    tasks: [
      { id: 31, name: '接口设计', startDate: '2026-04-05', endDate: '2026-04-10', status: 'completed' },
      { id: 32, name: '开发实现', startDate: '2026-04-11', endDate: '2026-04-20', status: 'completed' },
      { id: 33, name: '联调测试', startDate: '2026-04-21', endDate: '2026-04-25', status: 'in_progress' }
    ]
  }
])

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

import { AddIcon } from 'tdesign-icons-vue-next'
</script>

<style scoped>
.gantt-page { max-width: 100%; overflow-x: auto; }
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
.task-row { display: flex; height: 44px; border-top: 1px solid var(--border-light); animation: fadeIn 0.4s ease backwards; }
.task-row:hover { background: var(--bg-hover); }

.task-name-cell { width: 200px; flex-shrink: 0; padding: 0 16px; display: flex; align-items: center; gap: 8px; border-right: 1px solid var(--border-color); }
.project-icon { width: 28px; height: 28px; border-radius: var(--radius-md); display: flex; align-items: center; justify-content: center; color: white; font-size: 12px; font-weight: 600; }
.project-name { font-size: 13px; font-weight: 600; color: var(--text-primary); }
.task-indent { width: 16px; }
.task-name { font-size: 13px; color: var(--text-secondary); }

.task-bar-wrapper { flex: 1; position: relative; padding: 8px 0; }
.task-bar { position: absolute; height: 28px; border-radius: var(--radius-md); display: flex; align-items: center; padding: 0 8px; min-width: 40px; transition: all var(--transition-fast); }
.task-bar:hover { transform: scaleY(1.1); box-shadow: var(--shadow-md); }
.project-bar { height: 32px; opacity: 0.8; }
.bar-label { font-size: 11px; font-weight: 600; color: white; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }

.today-line { position: absolute; top: 0; bottom: 0; width: 2px; background: #EF4444; z-index: 10; pointer-events: none; }
.today-line::before { content: '今天'; position: absolute; top: 0; left: 50%; transform: translateX(-50%); font-size: 10px; font-weight: 600; color: #EF4444; background: var(--bg-container); padding: 2px 4px; border-radius: 4px; }
</style>
