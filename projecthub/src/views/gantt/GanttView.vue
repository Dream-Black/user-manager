<template>
  <div class="gantt-page">
    <div class="page-header fade-in">
      <div class="page-header-left">
        <h2 class="page-title">甘特图</h2>
        <span class="page-subtitle">可视化任务时间线</span>
      </div>
      <div class="page-header-right">
        <t-button variant="outline" @click="prevWeek">
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="15 18 9 12 15 6"/></svg>
        </t-button>
        <t-button variant="outline" @click="goToday">今天</t-button>
        <t-button variant="outline" @click="nextWeek">
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="9 18 15 12 9 6"/></svg>
        </t-button>
      </div>
    </div>
    
    <div class="gantt-container card fade-in-up">
      <div class="gantt-timeline">
        <div class="timeline-header">
          <div class="timeline-weeks">
            <div v-for="week in weeks" :key="week.label" class="week-cell" :style="{ width: `${week.days * cellWidth}px` }">
              {{ week.label }}
            </div>
          </div>
          <div class="timeline-days">
            <div v-for="day in days" :key="day" class="day-cell" :style="{ width: `${cellWidth}px` }">
              {{ day }}
            </div>
          </div>
        </div>
        
        <div class="gantt-body">
          <div v-for="task in tasks" :key="task.id" class="gantt-row">
            <div class="task-label">{{ task.title }}</div>
            <div class="task-bar-container">
              <div 
                class="task-bar" 
                :style="getBarStyle(task)"
                @mousedown="startDrag($event, task)"
              >
                <div class="bar-progress" :style="{ width: `${task.progress || 0}%` }"></div>
              </div>
            </div>
          </div>
          
          <div v-if="tasks.length === 0" class="empty-state">
            <p>暂无任务数据</p>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'

const tasks = ref([])
const cellWidth = 40
const startDate = ref(new Date())

const days = computed(() => {
  const result = []
  for (let i = 0; i < 30; i++) {
    const d = new Date(startDate.value)
    d.setDate(d.getDate() + i)
    result.push(d.getDate())
  }
  return result
})

const weeks = computed(() => {
  const result = []
  for (let i = 0; i < 5; i++) {
    const d = new Date(startDate.value)
    d.setDate(d.getDate() + i * 7)
    result.push({ label: `${d.getMonth() + 1}月第${Math.ceil(d.getDate() / 7)}周`, days: 7 })
  }
  return result
})

const fetchTasks = async () => {
  const res = await fetch('/api/tasks')
  if (res.ok) tasks.value = await res.json()
}

const getBarStyle = (task) => {
  const start = new Date(task.startDate || task.createdAt)
  const end = new Date(task.dueDate || startDate.value)
  const diff = Math.max(0, (end - start) / (1000 * 60 * 60 * 24))
  const offset = Math.max(0, (start - new Date(startDate.value)) / (1000 * 60 * 60 * 24))
  
  return {
    left: `${offset * cellWidth}px`,
    width: `${Math.max(diff * cellWidth, 20)}px`,
    background: getPriorityColor(task.priority)
  }
}

const getPriorityColor = (priority) => {
  return { high: '#EF4444', medium: '#F59E0B', low: '#10B981' }[priority] || '#3B82F6'
}

const prevWeek = () => {
  const d = new Date(startDate.value)
  d.setDate(d.getDate() - 7)
  startDate.value = d
}

const nextWeek = () => {
  const d = new Date(startDate.value)
  d.setDate(d.getDate() + 7)
  startDate.value = d
}

const goToday = () => {
  startDate.value = new Date()
}

const startDrag = (e, task) => {
  console.log('Drag start', task)
}

onMounted(fetchTasks)
</script>

<style scoped>
.gantt-page { animation: fadeIn 0.3s ease-out; }
.page-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: var(--space-5); }
.page-header-left { display: flex; align-items: baseline; gap: var(--space-3); }
.page-title { font-size: var(--font-size-2xl); font-weight: var(--font-weight-semibold); }
.page-subtitle { font-size: var(--font-size-sm); color: var(--text-tertiary); }
.page-header-right { display: flex; gap: var(--space-2); }
.gantt-container { overflow-x: auto; }
.gantt-timeline { min-width: 100%; }
.timeline-header { border-bottom: 1px solid var(--border-light); }
.timeline-weeks, .timeline-days { display: flex; }
.week-cell, .day-cell { display: flex; align-items: center; justify-content: center; height: 32px; font-size: var(--font-size-xs); color: var(--text-tertiary); border-right: 1px solid var(--border-light); }
.week-cell { font-weight: var(--font-weight-medium); color: var(--text-secondary); }
.day-cell { height: 24px; font-size: 11px; }
.gantt-body { padding: var(--space-3) 0; }
.gantt-row { display: flex; align-items: center; height: 44px; border-bottom: 1px solid var(--border-light); transition: background 0.15s; }
.gantt-row:hover { background: var(--gray-50); }
.task-label { width: 180px; padding: 0 var(--space-4); font-size: var(--font-size-sm); flex-shrink: 0; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.task-bar-container { flex: 1; position: relative; height: 100%; }
.task-bar { position: absolute; height: 24px; border-radius: var(--radius-full); cursor: pointer; transition: transform 0.2s, box-shadow 0.2s; display: flex; align-items: center; overflow: hidden; }
.task-bar:hover { transform: scaleY(1.2); box-shadow: 0 2px 8px rgba(0,0,0,0.15); }
.bar-progress { height: 100%; background: rgba(255,255,255,0.3); transition: width 0.3s; }
.empty-state { text-align: center; padding: var(--space-8); color: var(--text-tertiary); }
</style>
