<template>
  <div class="dashboard">
    <!-- 快捷入口 -->
    <section class="quick-access">
      <div
        v-for="(item, index) in quickActions"
        :key="item.label"
        class="quick-item"
        :class="[`stagger-${index + 1}`]"
        @click="handleQuickAction(item.path)"
      >
        <div class="quick-icon" :style="{ background: item.bgColor }">
          <span v-html="item.icon"></span>
        </div>
        <span class="quick-label">{{ item.label }}</span>
      </div>
    </section>

    <!-- 统计卡片 -->
    <section class="stats-grid">
      <div
        v-for="(stat, index) in stats"
        :key="stat.label"
        class="stat-card fade-in-up"
        :class="[`stagger-${index + 1}`]"
      >
        <div class="stat-header">
          <span class="stat-label">{{ stat.label }}</span>
          <span class="stat-icon" :style="{ color: stat.color }" v-html="stat.icon"></span>
        </div>
        <div class="stat-value">{{ stat.value }}</div>
        <div class="stat-trend" :class="stat.trendUp ? 'up' : 'down'">
          <svg v-if="stat.trendUp" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <polyline points="23 6 13.5 15.5 8.5 10.5 1 18"/>
            <polyline points="17 6 23 6 23 12"/>
          </svg>
          <svg v-else width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <polyline points="23 18 13.5 8.5 8.5 13.5 1 6"/>
            <polyline points="17 18 23 18 23 12"/>
          </svg>
          <span>{{ stat.trend }}</span>
        </div>
      </div>
    </section>

    <!-- 双列布局 -->
    <div class="dashboard-grid">
      <!-- 进行中的项目 -->
      <div class="card fade-in-up stagger-3">
        <div class="card-header">
          <h3 class="card-title">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M22 19a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h5l2 3h9a2 2 0 0 1 2 2z"/>
            </svg>
            进行中的项目
          </h3>
          <router-link to="/projects" class="card-link">查看全部</router-link>
        </div>
        <div class="card-body">
          <div v-if="projects.length === 0" class="empty-state">
            <svg width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
              <path d="M22 19a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h5l2 3h9a2 2 0 0 1 2 2z"/>
            </svg>
            <p>暂无进行中的项目</p>
            <t-button theme="primary" variant="outline" @click="createProject">创建第一个项目</t-button>
          </div>
          <div v-else class="project-list">
            <div
              v-for="(project, index) in projects"
              :key="project.id"
              class="project-item"
              :style="{ animationDelay: `${0.1 * index}s` }"
              @click="goToProject(project.id)"
            >
              <div class="project-icon" :style="{ background: getProjectColor(project.type) }">
                {{ project.name.charAt(0) }}
              </div>
              <div class="project-info">
                <div class="project-name">{{ project.name }}</div>
                <div class="project-meta">
                  <span class="tag tag-gray">{{ getTypeLabel(project.type) }}</span>
                  <span class="project-date">{{ formatDate(project.createdAt) }}</span>
                </div>
              </div>
              <div class="project-progress">
                <t-progress
                  :percentage="project.progress || 0"
                  :color="getProgressColor(project.progress || 0)"
                  :show-text="false"
                  theme="line"
                  size="small"
                />
                <span class="progress-text">{{ project.progress || 0 }}%</span>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- 今日任务 -->
      <div class="card fade-in-up stagger-4">
        <div class="card-header">
          <h3 class="card-title">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M9 11l3 3L22 4"/>
              <path d="M21 12v7a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11"/>
            </svg>
            今日任务
          </h3>
          <router-link to="/tasks" class="card-link">查看全部</router-link>
        </div>
        <div class="card-body">
          <div v-if="todayTasks.length === 0" class="empty-state">
            <svg width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
              <path d="M9 11l3 3L22 4"/>
              <path d="M21 12v7a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11"/>
            </svg>
            <p>今日任务已完成</p>
          </div>
          <div v-else class="task-list">
            <div
              v-for="(task, index) in todayTasks"
              :key="task.id"
              class="task-item"
              :style="{ animationDelay: `${0.1 * index}s` }"
              @click="toggleTask(task)"
            >
              <t-checkbox
                :checked="task.status === 'completed'"
                @change="() => toggleTask(task)"
              />
              <div class="task-info">
                <span class="task-title" :class="{ completed: task.status === 'completed' }">
                  {{ task.title }}
                </span>
                <span class="task-project">{{ task.projectName }}</span>
              </div>
              <t-tag :theme="getPriorityTheme(task.priority)" variant="light" size="small">
                {{ getPriorityLabel(task.priority) }}
              </t-tag>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- 时间线预览 -->
    <div class="card fade-in-up stagger-5">
      <div class="card-header">
        <h3 class="card-title">
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <circle cx="12" cy="12" r="10"/>
            <polyline points="12 6 12 12 16 14"/>
          </svg>
          最近活动时间
        </h3>
        <router-link to="/timeline" class="card-link">查看全部</router-link>
      </div>
      <div class="card-body">
        <div v-if="recentActivities.length === 0" class="empty-state">
          <svg width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
            <circle cx="12" cy="12" r="10"/>
            <polyline points="12 6 12 12 16 14"/>
          </svg>
          <p>暂无活动记录</p>
        </div>
        <div v-else class="activity-timeline">
          <div
            v-for="(activity, index) in recentActivities"
            :key="activity.id"
            class="timeline-item"
            :style="{ animationDelay: `${0.1 * index}s` }"
          >
            <div class="timeline-dot" :style="{ background: activity.color }"></div>
            <div class="timeline-content">
              <div class="timeline-header">
                <span class="timeline-action">{{ activity.action }}</span>
                <span class="timeline-time">{{ formatTime(activity.createdAt) }}</span>
              </div>
              <p class="timeline-desc">{{ activity.details }}</p>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { MessagePlugin } from 'tdesign-vue-next'
import dayjs from 'dayjs'
import relativeTime from 'dayjs/plugin/relativeTime'
import 'dayjs/locale/zh-cn'

dayjs.extend(relativeTime)
dayjs.locale('zh-cn')

const router = useRouter()

const projects = ref([])
const todayTasks = ref([])
const recentActivities = ref([])

const quickActions = [
  {
    label: '新建项目',
    path: '/projects?action=create',
    icon: '<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><line x1="12" y1="5" x2="12" y2="19"/><line x1="5" y1="12" x2="19" y2="12"/></svg>',
    bgColor: 'rgba(37, 99, 235, 0.1)'
  },
  {
    label: '创建任务',
    path: '/tasks?action=create',
    icon: '<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M9 11l3 3L22 4"/><path d="M21 12v7a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11"/></svg>',
    bgColor: 'rgba(16, 185, 129, 0.1)'
  },
  {
    label: 'AI 助手',
    path: '/ai',
    icon: '<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="12" cy="12" r="3"/><path d="M12 1v2m0 18v2M4.22 4.22l1.42 1.42m12.72 12.72l1.42 1.42M1 12h2m18 0h2M4.22 19.78l1.42-1.42M18.36 5.64l1.42-1.42"/></svg>',
    bgColor: 'rgba(139, 92, 246, 0.1)'
  },
  {
    label: '甘特图',
    path: '/gantt',
    icon: '<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><line x1="4" y1="6" x2="16" y2="6"/><line x1="8" y1="12" x2="20" y2="12"/><line x1="6" y1="18" x2="14" y2="18"/></svg>',
    bgColor: 'rgba(245, 158, 11, 0.1)'
  }
]

const stats = ref([
  {
    label: '项目总数',
    value: 0,
    icon: '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M22 19a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h5l2 3h9a2 2 0 0 1 2 2z"/></svg>',
    color: '#2563EB',
    trend: '+0 本月',
    trendUp: true
  },
  {
    label: '进行中',
    value: 0,
    icon: '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="12" cy="12" r="10"/><polyline points="12 6 12 12 16 14"/></svg>',
    color: '#F59E0B',
    trend: '+0 本周',
    trendUp: true
  },
  {
    label: '已完成',
    value: 0,
    icon: '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"/><polyline points="22 4 12 14.01 9 11.01"/></svg>',
    color: '#10B981',
    trend: '+0 本周',
    trendUp: true
  },
  {
    label: '延期任务',
    value: 0,
    icon: '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="12" cy="12" r="10"/><line x1="12" y1="8" x2="12" y2="12"/><line x1="12" y1="16" x2="12.01" y2="16"/></svg>',
    color: '#EF4444',
    trend: '-0',
    trendUp: false
  }
])

// 获取数据
const fetchData = async () => {
  try {
    // 获取项目
    const projectRes = await fetch('/api/projects')
    if (projectRes.ok) {
      const data = await projectRes.json()
      projects.value = data.slice(0, 5)
      stats.value[0].value = data.length
      stats.value[1].value = data.filter(p => p.status === 'in_progress').length
      stats.value[2].value = data.filter(p => p.status === 'completed').length
    }

    // 获取今日任务
    const taskRes = await fetch('/api/tasks')
    if (taskRes.ok) {
      const tasks = await taskRes.json()
      const today = dayjs().format('YYYY-MM-DD')
      todayTasks.value = tasks
        .filter(t => dayjs(t.dueDate).format('YYYY-MM-DD') === today)
        .slice(0, 8)
      stats.value[3].value = tasks.filter(t => t.status === 'overdue').length
    }

    // 获取最近活动
    const timelineRes = await fetch('/api/timelines/recent?limit=5')
    if (timelineRes.ok) {
      recentActivities.value = await timelineRes.json()
    }
  } catch (error) {
    console.error('Failed to fetch data:', error)
  }
}

const handleQuickAction = (path) => {
  router.push(path)
}

const createProject = () => {
  router.push('/projects?action=create')
}

const goToProject = (id) => {
  router.push(`/projects/${id}`)
}

const toggleTask = async (task) => {
  try {
    await fetch(`/api/tasks/${task.id}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        ...task,
        status: task.status === 'completed' ? 'pending' : 'completed'
      })
    })
    task.status = task.status === 'completed' ? 'pending' : 'completed'
    MessagePlugin.success(task.status === 'completed' ? '任务已完成' : '任务已重置')
  } catch (error) {
    MessagePlugin.error('操作失败')
  }
}

const formatDate = (date) => {
  return dayjs(date).format('MM/DD')
}

const formatTime = (date) => {
  return dayjs(date).fromNow()
}

const getProjectColor = (type) => {
  const colors = {
    'web': 'rgba(37, 99, 235, 0.1)',
    'mobile': 'rgba(16, 185, 129, 0.1)',
    'design': 'rgba(139, 92, 246, 0.1)',
    'other': 'rgba(107, 114, 128, 0.1)'
  }
  return colors[type] || colors.other
}

const getTypeLabel = (type) => {
  const labels = { 'web': 'Web', 'mobile': '移动端', 'design': '设计', 'other': '其他' }
  return labels[type] || '其他'
}

const getProgressColor = (progress) => {
  if (progress < 30) return '#EF4444'
  if (progress < 70) return '#F59E0B'
  return '#10B981'
}

const getPriorityTheme = (priority) => {
  const themes = { 'high': 'danger', 'medium': 'warning', 'low': 'success' }
  return themes[priority] || 'default'
}

const getPriorityLabel = (priority) => {
  const labels = { 'high': '高', 'medium': '中', 'low': '低' }
  return labels[priority] || '普通'
}

onMounted(() => {
  fetchData()
})
</script>

<style scoped>
.dashboard {
  animation: fadeIn 0.4s ease-out;
}

@keyframes fadeIn {
  from { opacity: 0; }
  to { opacity: 1; }
}

/* 快捷入口 */
.quick-access {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: var(--space-4);
  margin-bottom: var(--space-6);
}

.quick-item {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: var(--space-3);
  padding: var(--space-5);
  background: var(--bg-secondary);
  border-radius: var(--radius-xl);
  border: 1px solid var(--border-light);
  cursor: pointer;
  transition: all var(--transition-base);
}

.quick-item:hover {
  transform: translateY(-4px);
  box-shadow: var(--shadow-lg);
  border-color: var(--primary-200);
}

.quick-item:hover .quick-icon {
  transform: scale(1.1) rotate(5deg);
}

.quick-icon {
  width: 56px;
  height: 56px;
  border-radius: var(--radius-xl);
  display: flex;
  align-items: center;
  justify-content: center;
  color: var(--primary-500);
  transition: transform var(--transition-bounce);
}

.quick-label {
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  color: var(--text-primary);
}

/* 统计卡片 */
.stats-grid {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: var(--space-5);
  margin-bottom: var(--space-6);
}

.stat-card {
  background: var(--bg-secondary);
  border-radius: var(--radius-xl);
  border: 1px solid var(--border-light);
  padding: var(--space-5);
  transition: all var(--transition-base);
  opacity: 0;
  transform: translateY(16px);
}

.stat-card:hover {
  transform: translateY(-2px);
  box-shadow: var(--shadow-md);
}

.stat-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: var(--space-3);
}

.stat-label {
  font-size: var(--font-size-sm);
  color: var(--text-secondary);
}

.stat-value {
  font-size: var(--font-size-4xl);
  font-weight: var(--font-weight-bold);
  color: var(--text-primary);
  line-height: 1;
  margin-bottom: var(--space-2);
}

.stat-trend {
  display: flex;
  align-items: center;
  gap: var(--space-1);
  font-size: var(--font-size-xs);
  color: var(--text-tertiary);
}

.stat-trend.up {
  color: var(--success-500);
}

.stat-trend.down {
  color: var(--error-500);
}

/* 双列布局 */
.dashboard-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: var(--space-5);
  margin-bottom: var(--space-5);
}

/* 项目列表 */
.project-list {
  display: flex;
  flex-direction: column;
  gap: var(--space-2);
}

.project-item {
  display: flex;
  align-items: center;
  gap: var(--space-3);
  padding: var(--space-3);
  border-radius: var(--radius-lg);
  cursor: pointer;
  transition: all var(--transition-fast);
  animation: slideInRight 0.3s ease-out forwards;
  opacity: 0;
}

@keyframes slideInRight {
  from {
    opacity: 0;
    transform: translateX(-10px);
  }
  to {
    opacity: 1;
    transform: translateX(0);
  }
}

.project-item:hover {
  background: var(--gray-50);
}

.project-icon {
  width: 40px;
  height: 40px;
  border-radius: var(--radius-lg);
  display: flex;
  align-items: center;
  justify-content: center;
  color: var(--primary-600);
  font-weight: var(--font-weight-semibold);
  font-size: var(--font-size-lg);
}

.project-info {
  flex: 1;
  min-width: 0;
}

.project-name {
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  color: var(--text-primary);
  margin-bottom: 4px;
}

.project-meta {
  display: flex;
  align-items: center;
  gap: var(--space-2);
}

.project-date {
  font-size: var(--font-size-xs);
  color: var(--text-tertiary);
}

.project-progress {
  width: 100px;
  display: flex;
  align-items: center;
  gap: var(--space-2);
}

.progress-text {
  font-size: var(--font-size-xs);
  color: var(--text-tertiary);
  width: 32px;
  text-align: right;
}

/* 任务列表 */
.task-list {
  display: flex;
  flex-direction: column;
  gap: var(--space-1);
}

.task-item {
  display: flex;
  align-items: center;
  gap: var(--space-3);
  padding: var(--space-3);
  border-radius: var(--radius-lg);
  cursor: pointer;
  transition: all var(--transition-fast);
  animation: fadeIn 0.3s ease-out forwards;
  opacity: 0;
}

.task-item:hover {
  background: var(--gray-50);
}

.task-info {
  flex: 1;
  min-width: 0;
}

.task-title {
  display: block;
  font-size: var(--font-size-sm);
  color: var(--text-primary);
  margin-bottom: 2px;
}

.task-title.completed {
  text-decoration: line-through;
  color: var(--text-tertiary);
}

.task-project {
  font-size: var(--font-size-xs);
  color: var(--text-tertiary);
}

/* 时间线 */
.activity-timeline {
  display: flex;
  flex-direction: column;
}

.timeline-item {
  display: flex;
  gap: var(--space-4);
  padding: var(--space-3) 0;
  border-bottom: 1px solid var(--border-light);
  animation: fadeInUp 0.3s ease-out forwards;
  opacity: 0;
}

.timeline-item:last-child {
  border-bottom: none;
}

.timeline-dot {
  width: 10px;
  height: 10px;
  border-radius: 50%;
  flex-shrink: 0;
  margin-top: 5px;
}

.timeline-content {
  flex: 1;
}

.timeline-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 4px;
}

.timeline-action {
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  color: var(--text-primary);
}

.timeline-time {
  font-size: var(--font-size-xs);
  color: var(--text-tertiary);
}

.timeline-desc {
  font-size: var(--font-size-sm);
  color: var(--text-secondary);
}

/* 空状态 */
.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: var(--space-8) var(--space-4);
  text-align: center;
  color: var(--text-tertiary);
}

.empty-state svg {
  margin-bottom: var(--space-4);
  opacity: 0.5;
}

.empty-state p {
  margin-bottom: var(--space-4);
}

/* 卡片链接 */
.card-link {
  font-size: var(--font-size-sm);
  color: var(--primary-500);
  text-decoration: none;
  transition: color var(--transition-fast);
}

.card-link:hover {
  color: var(--primary-600);
}

/* 响应式 */
@media (max-width: 1200px) {
  .stats-grid {
    grid-template-columns: repeat(2, 1fr);
  }
  .quick-access {
    grid-template-columns: repeat(2, 1fr);
  }
}

@media (max-width: 768px) {
  .dashboard-grid {
    grid-template-columns: 1fr;
  }
  .stats-grid {
    grid-template-columns: 1fr;
  }
}
</style>
