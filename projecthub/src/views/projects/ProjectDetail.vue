<template>
  <div class="project-detail">
    <div class="page-header fade-in">
      <t-button variant="text" @click="$router.push('/projects')">
        <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
          <line x1="19" y1="12" x2="5" y2="12"/><polyline points="12 19 5 12 12 5"/>
        </svg>
        返回列表
      </t-button>
    </div>
    
    <div v-if="project" class="project-content">
      <div class="project-header card fade-in-up">
        <div class="project-icon" :style="{ background: getProjectBgColor(project.type) }">
          {{ project.name?.charAt(0).toUpperCase() }}
        </div>
        <div class="project-info">
          <h1 class="project-name">{{ project.name }}</h1>
          <p class="project-desc">{{ project.description || '暂无描述' }}</p>
          <div class="project-tags">
            <t-tag :theme="getStatusTheme(project.status)" variant="light">
              {{ getStatusLabel(project.status) }}
            </t-tag>
            <t-tag variant="outline">{{ getTypeLabel(project.type) }}</t-tag>
            <span class="project-date">创建于 {{ formatDate(project.createdAt) }}</span>
          </div>
        </div>
        <div class="project-progress-card">
          <div class="progress-ring">
            <svg viewBox="0 0 100 100">
              <circle cx="50" cy="50" r="45" fill="none" stroke="var(--gray-200)" stroke-width="8"/>
              <circle cx="50" cy="50" r="45" fill="none" stroke="var(--primary-500)" stroke-width="8"
                :stroke-dasharray="`${(project.progress || 0) * 2.83} 283`"
                stroke-linecap="round"
                transform="rotate(-90 50 50)"/>
            </svg>
            <span class="progress-value">{{ project.progress || 0 }}%</span>
          </div>
          <span class="progress-label">项目进度</span>
        </div>
      </div>
      
      <div class="tasks-section card fade-in-up stagger-1">
        <div class="card-header">
          <h3 class="card-title">任务列表</h3>
          <t-button theme="primary" size="small" @click="showAddTask = true">
            <template #icon><svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><line x1="12" y1="5" x2="12" y2="19"/><line x1="5" y1="12" x2="19" y2="12"/></svg></template>
            添加任务
          </t-button>
        </div>
        <div class="card-body">
          <div v-if="tasks.length === 0" class="empty-state">
            <p>暂无任务</p>
          </div>
          <div v-else class="task-list">
            <div v-for="task in tasks" :key="task.id" class="task-item">
              <t-checkbox :checked="task.status === 'completed'" @change="toggleTask(task)"/>
              <span class="task-title" :class="{ completed: task.status === 'completed' }">{{ task.title }}</span>
              <t-tag :theme="getPriorityTheme(task.priority)" variant="light" size="small">
                {{ getPriorityLabel(task.priority) }}
              </t-tag>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { MessagePlugin } from 'tdesign-vue-next'
import dayjs from 'dayjs'

const route = useRoute()
const project = ref(null)
const tasks = ref([])
const showAddTask = ref(false)

const fetchData = async () => {
  const id = route.params.id
  const [projectRes, tasksRes] = await Promise.all([
    fetch(`/api/projects/${id}`),
    fetch(`/api/tasks?projectId=${id}`)
  ])
  if (projectRes.ok) project.value = await projectRes.json()
  if (tasksRes.ok) tasks.value = await tasksRes.json()
}

const toggleTask = async (task) => {
  const newStatus = task.status === 'completed' ? 'pending' : 'completed'
  await fetch(`/api/tasks/${task.id}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ ...task, status: newStatus })
  })
  task.status = newStatus
}

const formatDate = (date) => dayjs(date).format('YYYY-MM-DD')
const getProjectBgColor = (type) => ({ web: 'rgba(37,99,235,0.1)', mobile: 'rgba(16,185,129,0.1)', design: 'rgba(139,92,246,0.1)', other: 'rgba(107,114,128,0.1)' })[type] || 'rgba(107,114,128,0.1)'
const getStatusTheme = (s) => ({ in_progress: 'primary', completed: 'success', paused: 'warning' })[s] || 'default'
const getStatusLabel = (s) => ({ in_progress: '进行中', completed: '已完成', paused: '已暂停' })[s] || '未知'
const getTypeLabel = (t) => ({ web: 'Web', mobile: '移动端', design: '设计', other: '其他' })[t] || '其他'
const getPriorityTheme = (p) => ({ high: 'danger', medium: 'warning', low: 'success' })[p] || 'default'
const getPriorityLabel = (p) => ({ high: '高', medium: '中', low: '低' })[p] || '普通'

onMounted(fetchData)
</script>

<style scoped>
.project-detail { animation: fadeIn 0.3s ease-out; }
.page-header { margin-bottom: var(--space-5); }
.project-content { display: flex; flex-direction: column; gap: var(--space-5); }
.project-header { display: flex; align-items: flex-start; gap: var(--space-5); padding: var(--space-6); }
.project-icon { width: 72px; height: 72px; border-radius: var(--radius-xl); display: flex; align-items: center; justify-content: center; font-size: 28px; font-weight: bold; color: var(--primary-600); flex-shrink: 0; transition: transform 0.3s; }
.project-header:hover .project-icon { transform: scale(1.1) rotate(5deg); }
.project-info { flex: 1; }
.project-name { font-size: var(--font-size-2xl); font-weight: var(--font-weight-bold); margin-bottom: var(--space-2); }
.project-desc { color: var(--text-secondary); margin-bottom: var(--space-3); }
.project-tags { display: flex; align-items: center; gap: var(--space-2); }
.project-date { font-size: var(--font-size-xs); color: var(--text-tertiary); margin-left: var(--space-2); }
.project-progress-card { text-align: center; }
.progress-ring { position: relative; width: 100px; height: 100px; margin: 0 auto var(--space-2); }
.progress-ring svg { width: 100%; height: 100%; }
.progress-ring circle:last-child { transition: stroke-dasharray 1s ease-out; }
.progress-value { position: absolute; inset: 0; display: flex; align-items: center; justify-content: center; font-size: var(--font-size-xl); font-weight: var(--font-weight-bold); }
.progress-label { font-size: var(--font-size-xs); color: var(--text-tertiary); }
.task-list { display: flex; flex-direction: column; gap: var(--space-2); }
.task-item { display: flex; align-items: center; gap: var(--space-3); padding: var(--space-2); border-radius: var(--radius-lg); transition: background 0.15s; }
.task-item:hover { background: var(--gray-50); }
.task-title { flex: 1; font-size: var(--font-size-sm); }
.task-title.completed { text-decoration: line-through; color: var(--text-tertiary); }
.empty-state { text-align: center; padding: var(--space-8); color: var(--text-tertiary); }
</style>
