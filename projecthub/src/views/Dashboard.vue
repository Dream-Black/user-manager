<template>
  <div class="dashboard">
    <!-- 页面头部 -->
    <div class="page-header">
      <div class="header-content">
        <div class="welcome-section">
          <h1 class="page-title">欢迎回来，<span class="user-name">{{ userName }}</span> 👋</h1>
          <p class="page-subtitle">这里是您的工作概览，继续保持高效！</p>
        </div>
        <div class="header-actions">
          <button class="btn btn-secondary" @click="refreshData">
            <svg viewBox="0 0 24 24" fill="none" width="16" height="16">
              <path d="M23 4v6h-6M1 20v-6h6" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
              <path d="M3.51 9a9 9 0 0114.85-3.36L23 10M1 14l4.64 4.36A9 9 0 0020.49 15" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
            </svg>
            刷新数据
          </button>
          <button class="btn btn-primary" @click="$router.push('/projects/new')">
            <svg viewBox="0 0 24 24" fill="none" width="16" height="16">
              <path d="M12 5v14M5 12h14" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
            </svg>
            新建项目
          </button>
        </div>
      </div>
    </div>

    <!-- 统计卡片 -->
    <section class="stats-section">
      <div class="stats-grid">
        <div v-for="(stat, index) in stats" :key="stat.label" class="stat-card" :style="{ animationDelay: `${index * 0.1}s` }">
          <div class="stat-icon" :class="stat.color">
            <span v-html="stat.icon"></span>
          </div>
          <div class="stat-info">
            <span class="stat-value">{{ stat.value }}</span>
            <span class="stat-label">{{ stat.label }}</span>
          </div>
          <div class="stat-trend" :class="stat.trend > 0 ? 'up' : 'down'">
            <svg v-if="stat.trend > 0" viewBox="0 0 24 24" fill="none" width="14" height="14">
              <path d="M18 15l-6-6-6 6" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
            </svg>
            <svg v-else viewBox="0 0 24 24" fill="none" width="14" height="14">
              <path d="M6 9l6 6 6-6" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
            </svg>
            {{ Math.abs(stat.trend) }}%
          </div>
        </div>
      </div>
    </section>

    <!-- 图表区域 -->
    <section class="charts-section">
      <div class="charts-grid">
        <!-- 主图表 -->
        <div class="chart-card main-chart">
          <div class="chart-header">
            <div>
              <h3 class="chart-title">项目趋势</h3>
              <p class="chart-subtitle">近7天项目活动统计</p>
            </div>
            <div class="chart-actions">
              <button v-for="period in ['日', '周', '月']" :key="period" 
                class="chart-period-btn" 
                :class="{ active: selectedPeriod === period }"
                @click="selectedPeriod = period">
                {{ period }}
              </button>
            </div>
          </div>
          <div class="chart-container">
            <div class="chart-bars">
              <div v-for="(day, index) in chartData" :key="index" class="bar-wrapper">
                <div class="bar" :style="{ height: `${day.value}%` }"></div>
                <span class="bar-label">{{ day.label }}</span>
              </div>
            </div>
          </div>
        </div>

        <!-- 环形图 -->
        <div class="chart-card donut-chart">
          <div class="chart-header">
            <h3 class="chart-title">任务分布</h3>
          </div>
          <div class="donut-container">
            <svg class="donut" viewBox="0 0 100 100">
              <circle class="donut-ring" cx="50" cy="50" r="40" stroke-width="12"/>
              <circle class="donut-segment" cx="50" cy="50" r="40" stroke-width="12" 
                :stroke-dasharray="`${taskDistribution.completed} ${100 - taskDistribution.completed}`"
                stroke-dashoffset="25"/>
              <circle class="donut-segment completed" cx="50" cy="50" r="40" stroke-width="12" 
                :stroke-dasharray="`${taskDistribution.inProgress} ${100 - taskDistribution.inProgress}`"
                stroke-dashoffset="25"/>
              <circle class="donut-segment pending" cx="50" cy="50" r="40" stroke-width="12" 
                :stroke-dasharray="`${taskDistribution.pending} ${100 - taskDistribution.pending}`"
                stroke-dashoffset="25"/>
            </svg>
            <div class="donut-center">
              <span class="donut-value">{{ taskDistribution.total }}</span>
              <span class="donut-label">总任务</span>
            </div>
          </div>
          <div class="donut-legend">
            <div class="legend-item">
              <span class="legend-dot completed"></span>
              <span class="legend-text">已完成</span>
              <span class="legend-value">{{ taskDistribution.completedPercent }}%</span>
            </div>
            <div class="legend-item">
              <span class="legend-dot in-progress"></span>
              <span class="legend-text">进行中</span>
              <span class="legend-value">{{ taskDistribution.inProgressPercent }}%</span>
            </div>
            <div class="legend-item">
              <span class="legend-dot pending"></span>
              <span class="legend-text">待处理</span>
              <span class="legend-value">{{ taskDistribution.pendingPercent }}%</span>
            </div>
          </div>
        </div>
      </div>
    </section>

    <!-- 任务和项目 -->
    <section class="content-section">
      <div class="content-grid">
        <!-- 近期任务 -->
        <div class="tasks-card">
          <div class="card-header">
            <h3 class="card-title">近期任务</h3>
            <router-link to="/tasks" class="card-link">查看全部</router-link>
          </div>
          <div class="tasks-list">
            <div v-for="task in recentTasks" :key="task.id" class="task-item">
              <div class="task-checkbox" :class="{ checked: task.completed }">
                <svg v-if="task.completed" viewBox="0 0 24 24" fill="none" width="14" height="14">
                  <path d="M5 13l4 4L19 7" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                </svg>
              </div>
              <div class="task-content">
                <span class="task-title" :class="{ completed: task.completed }">{{ task.title }}</span>
                <div class="task-meta">
                  <span class="task-project">{{ task.project }}</span>
                  <span class="task-due" :class="{ overdue: isOverdue(task.due) }">{{ task.due }}</span>
                </div>
              </div>
              <span class="task-priority" :class="task.priority">{{ task.priorityText }}</span>
            </div>
          </div>
        </div>

        <!-- 近期项目 -->
        <div class="projects-card">
          <div class="card-header">
            <h3 class="card-title">近期项目</h3>
            <router-link to="/projects" class="card-link">查看全部</router-link>
          </div>
          <div class="projects-list">
            <div v-for="project in recentProjects" :key="project.id" class="project-item" @click="$router.push(`/projects/${project.id}`)">
              <div class="project-icon" :style="{ background: project.color }">
                {{ project.name.charAt(0) }}
              </div>
              <div class="project-info">
                <span class="project-name">{{ project.name }}</span>
                <span class="project-desc">{{ project.description }}</span>
              </div>
              <div class="project-progress">
                <div class="progress-bar">
                  <div class="progress-fill" :style="{ width: `${project.progress}%` }"></div>
                </div>
                <span class="progress-text">{{ project.progress }}%</span>
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>

    <!-- 快捷操作 -->
    <section class="quick-actions-section">
      <h3 class="section-title">快捷操作</h3>
      <div class="quick-actions-grid">
        <button v-for="(action, index) in quickActions" :key="action.label" 
          class="quick-action-btn" 
          :style="{ animationDelay: `${index * 0.05}s` }"
          @click="$router.push(action.path)">
          <span class="action-icon" v-html="action.icon"></span>
          <span class="action-label">{{ action.label }}</span>
        </button>
      </div>
    </section>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { projectService } from '@/services/dataService'
import { taskService } from '@/services/dataService'
import dayjs from 'dayjs'

const userName = '用户'
const loading = ref(false)
const selectedPeriod = ref('周')

// 项目颜色映射
const projectColors = [
  'linear-gradient(135deg, #3B82F6, #60A5FA)',
  'linear-gradient(135deg, #10B981, #34D399)',
  'linear-gradient(135deg, #F59E0B, #FBBF24)',
  'linear-gradient(135deg, #6366F1, #818CF8)',
  'linear-gradient(135deg, #EC4899, #F472B6)',
  'linear-gradient(135deg, #8B5CF6, #A78BFA)'
]

const stats = ref([
  { 
    label: '总项目数', 
    value: '0', 
    icon: '<svg viewBox="0 0 24 24" fill="none" width="24" height="24"><path d="M3 7v10a2 2 0 002 2h14a2 2 0 002-2V9a2 2 0 00-2-2h-6l-2-2H5a2 2 0 00-2 2z" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/></svg>',
    color: 'primary',
    trend: 0
  },
  { 
    label: '进行中任务', 
    value: '0', 
    icon: '<svg viewBox="0 0 24 24" fill="none" width="24" height="24"><path d="M9 11l3 3L22 4M21 12v7a2 2 0 01-2 2H5a2 2 0 01-2-2V5a2 2 0 012-2h11" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/></svg>',
    color: 'success',
    trend: 0
  },
  { 
    label: '已完成任务', 
    value: '0', 
    icon: '<svg viewBox="0 0 24 24" fill="none" width="24" height="24"><path d="M22 11.08V12a10 10 0 11-5.93-9.14" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/><path d="M22 4L12 14.01l-3-3" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/></svg>',
    color: 'warning',
    trend: 0
  },
  { 
    label: '本周新增', 
    value: '0', 
    icon: '<svg viewBox="0 0 24 24" fill="none" width="24" height="24"><path d="M12 5v14M5 12h14" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/></svg>',
    color: 'info',
    trend: 0
  }
])

const chartData = ref([
  { label: '周一', value: 0 },
  { label: '周二', value: 0 },
  { label: '周三', value: 0 },
  { label: '周四', value: 0 },
  { label: '周五', value: 0 },
  { label: '周六', value: 0 },
  { label: '周日', value: 0 }
])

const taskDistribution = ref({
  completed: 0,
  completedPercent: 0,
  inProgress: 0,
  inProgressPercent: 0,
  pending: 0,
  pendingPercent: 0,
  total: 0
})

const recentTasks = ref([])

const recentProjects = ref([])

const quickActions = ref([
  { label: '创建任务', icon: '<svg viewBox="0 0 24 24" fill="none" width="20" height="20"><path d="M12 5v14M5 12h14" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/></svg>', path: '/tasks/new' },
  { label: '项目列表', icon: '<svg viewBox="0 0 24 24" fill="none" width="20" height="20"><path d="M3 7v10a2 2 0 002 2h14a2 2 0 002-2V9a2 2 0 00-2-2h-6l-2-2H5a2 2 0 00-2 2z" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/></svg>', path: '/projects' },
  { label: 'AI对话', icon: '<svg viewBox="0 0 24 24" fill="none" width="20" height="20"><path d="M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/></svg>', path: '/ai' },
  { label: '导出报告', icon: '<svg viewBox="0 0 24 24" fill="none" width="20" height="20"><path d="M21 15v4a2 2 0 01-2 2H5a2 2 0 01-2-2v-4M7 10l5 5 5-5M12 15V3" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/></svg>', path: '/reports' },
  { label: '任务中心', icon: '<svg viewBox="0 0 24 24" fill="none" width="20" height="20"><path d="M9 11l3 3L22 4M21 12v7a2 2 0 01-2 2H5a2 2 0 01-2-2V5a2 2 0 012-2h11" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/></svg>', path: '/tasks' },
  { label: '设置', icon: '<svg viewBox="0 0 24 24" fill="none" width="20" height="20"><path d="M12 15a3 3 0 100-6 3 3 0 000 6z" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/><path d="M19.4 15a1.65 1.65 0 00.33 1.82l.06.06a2 2 0 010 2.83 2 2 0 01-2.83 0l-.06-.06a1.65 1.65 0 00-1.82-.33 1.65 1.65 0 00-1 1.51V21a2 2 0 01-2 2 2 2 0 01-2-2v-.09A1.65 1.65 0 009 19.4a1.65 1.65 0 00-1.82.33l-.06.06a2 2 0 01-2.83 0 2 2 0 010-2.83l.06-.06a1.65 1.65 0 00.33-1.82 1.65 1.65 0 00-1.51-1H3a2 2 0 01-2-2 2 2 0 012-2h.09A1.65 1.65 0 004.6 9a1.65 1.65 0 00-.33-1.82l-.06-.06a2 2 0 010-2.83 2 2 0 012.83 0l.06.06a1.65 1.65 0 001.82.33H9a1.65 1.65 0 001-1.51V3a2 2 0 012-2 2 2 0 012 2v.09a1.65 1.65 0 001 1.51 1.65 1.65 0 001.82-.33l.06-.06a2 2 0 012.83 0 2 2 0 010 2.83l-.06.06a1.65 1.65 0 00-.33 1.82V9a1.65 1.65 0 001.51 1H21a2 2 0 012 2 2 2 0 01-2 2h-.09a1.65 1.65 0 00-1.51 1z" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/></svg>', path: '/settings' }
])

// 加载数据
const loadDashboardData = async () => {
  loading.value = true
  try {
    // 并行获取项目和任务数据
    const [projects, tasks] = await Promise.all([
      projectService.getAll(),
      taskService.getAll()
    ])

    // 更新统计卡片
    const totalProjects = projects.length || 0
    const totalTasks = tasks.length || 0
    const completedTasks = tasks.filter(t => t.status === 'completed').length || 0
    const inProgressTasks = tasks.filter(t => t.status === 'in_progress').length || 0
    const pendingTasks = tasks.filter(t => t.status === 'todo').length || 0

    // 本周新增项目
    const weekStart = dayjs().startOf('week')
    const weekProjects = projects.filter(p => dayjs(p.createdAt).isAfter(weekStart)).length || 0

    stats.value = [
      { 
        label: '总项目数', 
        value: totalProjects.toString(), 
        icon: '<svg viewBox="0 0 24 24" fill="none" width="24" height="24"><path d="M3 7v10a2 2 0 002 2h14a2 2 0 002-2V9a2 2 0 00-2-2h-6l-2-2H5a2 2 0 00-2 2z" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/></svg>',
        color: 'primary',
        trend: weekProjects
      },
      { 
        label: '进行中任务', 
        value: inProgressTasks.toString(), 
        icon: '<svg viewBox="0 0 24 24" fill="none" width="24" height="24"><path d="M9 11l3 3L22 4M21 12v7a2 2 0 01-2 2H5a2 2 0 01-2-2V5a2 2 0 012-2h11" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/></svg>',
        color: 'success',
        trend: 0
      },
      { 
        label: '已完成任务', 
        value: completedTasks.toString(), 
        icon: '<svg viewBox="0 0 24 24" fill="none" width="24" height="24"><path d="M22 11.08V12a10 10 0 11-5.93-9.14" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/><path d="M22 4L12 14.01l-3-3" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/></svg>',
        color: 'warning',
        trend: 0
      },
      { 
        label: '本周新增', 
        value: weekProjects.toString(), 
        icon: '<svg viewBox="0 0 24 24" fill="none" width="24" height="24"><path d="M12 5v14M5 12h14" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/></svg>',
        color: 'info',
        trend: 0
      }
    ]

    // 更新任务分布
    const total = totalTasks || 1
    taskDistribution.value = {
      completed: Math.round((completedTasks / total) * 100),
      completedPercent: Math.round((completedTasks / total) * 100),
      inProgress: Math.round((inProgressTasks / total) * 100),
      inProgressPercent: Math.round((inProgressTasks / total) * 100),
      pending: Math.round((pendingTasks / total) * 100),
      pendingPercent: Math.round((pendingTasks / total) * 100),
      total: totalTasks
    }

    // 更新近期任务（最多5个）
    recentTasks.value = tasks.slice(0, 5).map(t => ({
      id: t.id,
      title: t.title,
      project: t.projectName || '未分配',
      due: formatDueDate(t.dueDate),
      priority: t.priority || 'medium',
      priorityText: getPriorityText(t.priority),
      completed: t.status === 'completed'
    }))

    // 更新近期项目（最多4个）
    recentProjects.value = projects.slice(0, 4).map((p, index) => ({
      id: p.id,
      name: p.name,
      description: p.customer || p.description || '暂无描述',
      progress: p.progress || 0,
      color: projectColors[index % projectColors.length]
    }))

    // 生成图表数据（基于任务更新时间）
    updateChartData(tasks)

  } catch (error) {
    console.error('加载仪表盘数据失败:', error)
  } finally {
    loading.value = false
  }
}

// 格式化日期
const formatDueDate = (date) => {
  if (!date) return '未设置'
  const d = dayjs(date)
  const now = dayjs()
  const diffDays = d.diff(now, 'day')
  
  if (diffDays < 0) return '已过期'
  if (diffDays === 0) return '今天'
  if (diffDays === 1) return '明天'
  if (diffDays < 7) return `${diffDays}天后`
  return d.format('MM-DD')
}

// 获取优先级文本
const getPriorityText = (priority) => {
  const map = { high: '高', medium: '中', low: '低' }
  return map[priority] || '中'
}

// 更新图表数据
const updateChartData = (tasks) => {
  const days = ['周一', '周二', '周三', '周四', '周五', '周六', '周日']
  const today = dayjs()
  
  chartData.value = days.map((label, index) => {
    const dayStart = today.startOf('week').add(index, 'day')
    const dayEnd = dayStart.endOf('day')
    const count = tasks.filter(t => {
      const updated = dayjs(t.updatedAt)
      return updated.isAfter(dayStart) && updated.isBefore(dayEnd)
    }).length
    return {
      label,
      value: Math.min(count * 10, 100) // 最多100%
    }
  })
}

const refreshData = () => {
  loadDashboardData()
}

const isOverdue = (due) => {
  return due === '已过期'
}

// 组件挂载时加载数据
onMounted(() => {
  loadDashboardData()
})
</script>

<style scoped>
.dashboard {
  padding: var(--space-6);
  max-width: var(--content-max-width);
  margin: 0 auto;
  animation: fadeIn 0.5s ease;
}

/* 页面头部 */
.page-header {
  margin-bottom: var(--space-8);
}

.header-content {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: var(--space-6);
}

.welcome-section {
  animation: fadeInUp 0.6s ease;
}

.page-title {
  font-size: var(--font-size-2xl);
  font-weight: var(--font-weight-bold);
  color: var(--text-primary);
  margin-bottom: var(--space-2);
}

.user-name {
  background: var(--gradient-primary);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
}

.page-subtitle {
  font-size: var(--font-size-sm);
  color: var(--text-secondary);
}

.header-actions {
  display: flex;
  gap: var(--space-3);
  animation: fadeInUp 0.6s ease 0.1s backwards;
}

/* 统计卡片 */
.stats-section {
  margin-bottom: var(--space-8);
}

.stats-grid {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: var(--space-5);
}

.stat-card {
  background: var(--bg-card-solid);
  border-radius: var(--radius-xl);
  padding: var(--space-5);
  display: flex;
  align-items: flex-start;
  gap: var(--space-4);
  border: 1px solid var(--border-light);
  box-shadow: var(--shadow-card);
  animation: cardEnter 0.6s ease backwards;
  transition: all var(--transition-normal);
}

.stat-card:hover {
  transform: translateY(-4px);
  box-shadow: var(--shadow-lg);
  border-color: var(--primary-light);
}

.stat-icon {
  width: 48px;
  height: 48px;
  border-radius: var(--radius-lg);
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

.stat-icon.primary {
  background: var(--primary-lighter);
  color: var(--primary-color);
}

.stat-icon.success {
  background: var(--success-lighter);
  color: var(--success-color);
}

.stat-icon.warning {
  background: var(--warning-lighter);
  color: var(--warning-color);
}

.stat-icon.info {
  background: var(--info-lighter);
  color: var(--info-color);
}

.stat-info {
  flex: 1;
  display: flex;
  flex-direction: column;
}

.stat-value {
  font-size: var(--font-size-2xl);
  font-weight: var(--font-weight-bold);
  color: var(--text-primary);
  line-height: 1.2;
}

.stat-label {
  font-size: var(--font-size-sm);
  color: var(--text-secondary);
  margin-top: var(--space-1);
}

.stat-trend {
  display: flex;
  align-items: center;
  gap: 2px;
  font-size: var(--font-size-xs);
  font-weight: var(--font-weight-medium);
  padding: var(--space-1) var(--space-2);
  border-radius: var(--radius-full);
}

.stat-trend.up {
  color: var(--success-color);
  background: var(--success-lighter);
}

.stat-trend.down {
  color: var(--error-color);
  background: var(--error-lighter);
}

/* 图表区域 */
.charts-section {
  margin-bottom: var(--space-8);
}

.charts-grid {
  display: grid;
  grid-template-columns: 2fr 1fr;
  gap: var(--space-5);
}

.chart-card {
  background: var(--bg-card-solid);
  border-radius: var(--radius-xl);
  padding: var(--space-5);
  border: 1px solid var(--border-light);
  box-shadow: var(--shadow-card);
  animation: cardEnter 0.6s ease 0.2s backwards;
}

.chart-header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  margin-bottom: var(--space-5);
}

.chart-title {
  font-size: var(--font-size-base);
  font-weight: var(--font-weight-semibold);
  color: var(--text-primary);
  margin-bottom: var(--space-1);
}

.chart-subtitle {
  font-size: var(--font-size-xs);
  color: var(--text-tertiary);
}

.chart-actions {
  display: flex;
  gap: var(--space-1);
  background: var(--bg-color-secondary);
  padding: var(--space-1);
  border-radius: var(--radius-base);
}

.chart-period-btn {
  padding: var(--space-2) var(--space-3);
  font-size: var(--font-size-xs);
  font-weight: var(--font-weight-medium);
  color: var(--text-secondary);
  background: transparent;
  border: none;
  border-radius: var(--radius-sm);
  cursor: pointer;
  transition: all var(--transition-fast);
}

.chart-period-btn:hover {
  color: var(--text-primary);
}

.chart-period-btn.active {
  background: var(--bg-card-solid);
  color: var(--primary-color);
  box-shadow: var(--shadow-xs);
}

/* 柱状图 */
.chart-container {
  height: 200px;
}

.chart-bars {
  display: flex;
  align-items: flex-end;
  justify-content: space-between;
  height: 100%;
  gap: var(--space-3);
}

.bar-wrapper {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  height: 100%;
}

.bar {
  width: 100%;
  max-width: 40px;
  background: var(--gradient-primary);
  border-radius: var(--radius-sm) var(--radius-sm) 0 0;
  transition: all 0.8s cubic-bezier(0.4, 0, 0.2, 1);
  position: relative;
}

.bar:hover {
  filter: brightness(1.1);
  transform: scaleX(1.1);
}

.bar-label {
  font-size: var(--font-size-xs);
  color: var(--text-tertiary);
  margin-top: var(--space-2);
}

/* 环形图 */
.donut-container {
  position: relative;
  width: 160px;
  height: 160px;
  margin: 0 auto var(--space-5);
}

.donut {
  width: 100%;
  height: 100%;
  transform: rotate(-90deg);
}

.donut-ring {
  fill: none;
  stroke: var(--bg-color-secondary);
}

.donut-segment {
  fill: none;
  stroke: var(--primary-color);
  stroke-dasharray: 0 100;
  transition: stroke-dasharray 1s ease;
}

.donut-segment.completed {
  stroke: var(--success-color);
}

.donut-segment.pending {
  stroke: var(--warning-color);
}

.donut-center {
  position: absolute;
  top: 50%;
  left: 50%;
  transform: translate(-50%, -50%);
  text-align: center;
}

.donut-value {
  display: block;
  font-size: var(--font-size-2xl);
  font-weight: var(--font-weight-bold);
  color: var(--text-primary);
}

.donut-label {
  font-size: var(--font-size-xs);
  color: var(--text-tertiary);
}

.donut-legend {
  display: flex;
  flex-direction: column;
  gap: var(--space-3);
}

.legend-item {
  display: flex;
  align-items: center;
  gap: var(--space-3);
}

.legend-dot {
  width: 10px;
  height: 10px;
  border-radius: var(--radius-full);
}

.legend-dot.completed {
  background: var(--success-color);
}

.legend-dot.in-progress {
  background: var(--primary-color);
}

.legend-dot.pending {
  background: var(--warning-color);
}

.legend-text {
  flex: 1;
  font-size: var(--font-size-sm);
  color: var(--text-secondary);
}

.legend-value {
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  color: var(--text-primary);
}

/* 任务和项目 */
.content-section {
  margin-bottom: var(--space-8);
}

.content-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: var(--space-5);
}

.tasks-card,
.projects-card {
  background: var(--bg-card-solid);
  border-radius: var(--radius-xl);
  padding: var(--space-5);
  border: 1px solid var(--border-light);
  box-shadow: var(--shadow-card);
  animation: cardEnter 0.6s ease 0.3s backwards;
}

.card-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: var(--space-5);
}

.card-title {
  font-size: var(--font-size-base);
  font-weight: var(--font-weight-semibold);
  color: var(--text-primary);
}

.card-link {
  font-size: var(--font-size-sm);
  color: var(--primary-color);
  text-decoration: none;
  transition: color var(--transition-fast);
}

.card-link:hover {
  color: var(--primary-hover);
  text-decoration: underline;
}

/* 任务列表 */
.tasks-list {
  display: flex;
  flex-direction: column;
  gap: var(--space-3);
}

.task-item {
  display: flex;
  align-items: center;
  gap: var(--space-3);
  padding: var(--space-3);
  border-radius: var(--radius-lg);
  transition: background var(--transition-fast);
}

.task-item:hover {
  background: var(--bg-color-secondary);
}

.task-checkbox {
  width: 22px;
  height: 22px;
  border: 2px solid var(--border-color);
  border-radius: var(--radius-sm);
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  transition: all var(--transition-fast);
  flex-shrink: 0;
}

.task-checkbox:hover {
  border-color: var(--primary-color);
}

.task-checkbox.checked {
  background: var(--success-color);
  border-color: var(--success-color);
  color: white;
}

.task-content {
  flex: 1;
  min-width: 0;
}

.task-title {
  display: block;
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  color: var(--text-primary);
  margin-bottom: 2px;
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

.task-project,
.task-due {
  font-size: var(--font-size-xs);
  color: var(--text-tertiary);
}

.task-due.overdue {
  color: var(--error-color);
}

.task-priority {
  font-size: var(--font-size-xs);
  font-weight: var(--font-weight-medium);
  padding: 2px 8px;
  border-radius: var(--radius-full);
  flex-shrink: 0;
}

.task-priority.high {
  background: var(--error-lighter);
  color: var(--error-color);
}

.task-priority.medium {
  background: var(--warning-lighter);
  color: var(--warning-color);
}

.task-priority.low {
  background: var(--primary-lighter);
  color: var(--primary-color);
}

/* 项目列表 */
.projects-list {
  display: flex;
  flex-direction: column;
  gap: var(--space-3);
}

.project-item {
  display: flex;
  align-items: center;
  gap: var(--space-3);
  padding: var(--space-3);
  border-radius: var(--radius-lg);
  cursor: pointer;
  transition: all var(--transition-fast);
}

.project-item:hover {
  background: var(--bg-color-secondary);
}

.project-icon {
  width: 40px;
  height: 40px;
  border-radius: var(--radius-lg);
  display: flex;
  align-items: center;
  justify-content: center;
  color: white;
  font-weight: var(--font-weight-bold);
  font-size: var(--font-size-base);
  flex-shrink: 0;
}

.project-info {
  flex: 1;
  min-width: 0;
}

.project-name {
  display: block;
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  color: var(--text-primary);
  margin-bottom: 2px;
}

.project-desc {
  font-size: var(--font-size-xs);
  color: var(--text-tertiary);
}

.project-progress {
  width: 100px;
  flex-shrink: 0;
}

.progress-bar {
  height: 6px;
  background: var(--bg-color-secondary);
  border-radius: var(--radius-full);
  overflow: hidden;
  margin-bottom: 4px;
}

.progress-fill {
  height: 100%;
  background: var(--gradient-primary);
  border-radius: var(--radius-full);
  transition: width 0.8s ease;
}

.progress-text {
  font-size: var(--font-size-xs);
  color: var(--text-tertiary);
}

/* 快捷操作 */
.quick-actions-section {
  animation: fadeInUp 0.6s ease 0.4s backwards;
}

.section-title {
  font-size: var(--font-size-base);
  font-weight: var(--font-weight-semibold);
  color: var(--text-primary);
  margin-bottom: var(--space-5);
}

.quick-actions-grid {
  display: grid;
  grid-template-columns: repeat(6, 1fr);
  gap: var(--space-4);
}

.quick-action-btn {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: var(--space-3);
  padding: var(--space-5) var(--space-4);
  background: var(--bg-card-solid);
  border: 1px solid var(--border-light);
  border-radius: var(--radius-xl);
  cursor: pointer;
  transition: all var(--transition-normal);
  animation: cardEnter 0.5s ease backwards;
}

.quick-action-btn:hover {
  background: var(--gradient-primary);
  color: white;
  border-color: transparent;
  transform: translateY(-4px);
  box-shadow: var(--shadow-lg);
}

.quick-action-btn:hover .action-icon {
  color: white;
}

.action-icon {
  width: 40px;
  height: 40px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: var(--radius-lg);
  background: var(--primary-lighter);
  color: var(--primary-color);
  transition: all var(--transition-fast);
}

.action-label {
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  color: var(--text-primary);
  transition: color var(--transition-fast);
}

/* 响应式 */
@media (max-width: 1200px) {
  .stats-grid {
    grid-template-columns: repeat(2, 1fr);
  }
  
  .charts-grid {
    grid-template-columns: 1fr;
  }
  
  .quick-actions-grid {
    grid-template-columns: repeat(3, 1fr);
  }
}

@media (max-width: 768px) {
  .dashboard {
    padding: var(--space-4);
  }
  
  .header-content {
    flex-direction: column;
  }
  
  .header-actions {
    width: 100%;
  }
  
  .header-actions .btn {
    flex: 1;
  }
  
  .content-grid {
    grid-template-columns: 1fr;
  }
  
  .quick-actions-grid {
    grid-template-columns: repeat(2, 1fr);
  }
}
</style>
