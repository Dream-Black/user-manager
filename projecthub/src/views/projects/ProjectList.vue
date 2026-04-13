<template>
  <div class="projects-page">
    <!-- 页面头部 -->
    <div class="page-header">
      <div class="header-content">
        <div class="header-info">
          <h1 class="page-title">项目管理</h1>
          <p class="page-subtitle">管理您的所有项目，随时掌握进度</p>
        </div>
        <button class="btn btn-primary" @click="showCreateModal = true">
          <svg viewBox="0 0 24 24" fill="none" width="16" height="16">
            <path d="M12 5v14M5 12h14" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
          </svg>
          新建项目
        </button>
      </div>
    </div>

    <!-- 统计概览 -->
    <div class="project-stats">
      <div class="stat-item">
        <span class="stat-dot active"></span>
        <span class="stat-label">进行中</span>
        <span class="stat-value">{{ activeProjects }}</span>
      </div>
      <div class="stat-item">
        <span class="stat-dot completed"></span>
        <span class="stat-label">已完成</span>
        <span class="stat-value">{{ completedProjects }}</span>
      </div>
      <div class="stat-divider"></div>
      <div class="stat-item">
        <span class="stat-number">{{ projects.length }}</span>
        <span class="stat-label">总项目数</span>
      </div>
    </div>

    <!-- 筛选栏 -->
    <div class="filter-bar">
      <div class="search-box">
        <svg viewBox="0 0 24 24" fill="none" width="18" height="18">
          <path d="M21 21L15 15M17 10C17 13.866 13.866 17 10 17C6.13401 17 3 13.866 3 10C3 6.13401 6.13401 3 10 3C13.866 3 17 6.13401 17 10Z" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
        </svg>
        <input v-model="searchQuery" type="text" placeholder="搜索项目..." class="filter-input" />
      </div>
      <div class="filter-actions">
        <select v-model="filterStatus" class="filter-select">
          <option value="">全部状态</option>
          <option value="active">进行中</option>
          <option value="completed">已完成</option>
          <option value="paused">已暂停</option>
        </select>
        <div class="view-toggle">
          <button class="view-btn" :class="{ active: viewMode === 'grid' }" @click="viewMode = 'grid'" title="网格视图">
            <svg viewBox="0 0 24 24" fill="none" width="18" height="18">
              <rect x="3" y="3" width="7" height="7" rx="1" stroke="currentColor" stroke-width="2"/>
              <rect x="14" y="3" width="7" height="7" rx="1" stroke="currentColor" stroke-width="2"/>
              <rect x="14" y="14" width="7" height="7" rx="1" stroke="currentColor" stroke-width="2"/>
              <rect x="3" y="14" width="7" height="7" rx="1" stroke="currentColor" stroke-width="2"/>
            </svg>
          </button>
          <button class="view-btn" :class="{ active: viewMode === 'list' }" @click="viewMode = 'list'" title="列表视图">
            <svg viewBox="0 0 24 24" fill="none" width="18" height="18">
              <path d="M8 6h13M8 12h13M8 18h13M3 6h.01M3 12h.01M3 18h.01" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
            </svg>
          </button>
        </div>
      </div>
    </div>

    <!-- 网格视图 -->
    <div v-if="viewMode === 'grid'" class="projects-grid">
      <div v-for="project in filteredProjects" :key="project.id" 
        class="project-card" 
        @click="$router.push(`/projects/${project.id}`)">
        <div class="project-header">
          <div class="project-icon" :style="{ background: project.color }">
            {{ project.name.charAt(0) }}
          </div>
          <span class="project-status" :class="project.status">{{ project.statusText }}</span>
        </div>
        <h3 class="project-name">{{ project.name }}</h3>
        <p class="project-desc">{{ project.description }}</p>
        <div class="project-progress">
          <div class="progress-info">
            <span>进度</span>
            <span class="progress-percent">{{ project.progress }}%</span>
          </div>
          <div class="progress-bar">
            <div class="progress-fill" :style="{ width: `${project.progress}%` }"></div>
          </div>
        </div>
        <div class="project-footer">
          <div class="project-meta">
            <svg viewBox="0 0 24 24" fill="none" width="14" height="14">
              <path d="M8 6h13M8 12h13M8 18h13M3 6h.01M3 12h.01M3 18h.01" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
            </svg>
            {{ project.tasks }} 任务
          </div>
          <div class="project-team">
            <div v-for="(member, idx) in project.members.slice(0, 3)" :key="idx" class="team-avatar">
              {{ member.charAt(0) }}
            </div>
            <span v-if="project.members.length > 3" class="team-more">+{{ project.members.length - 3 }}</span>
          </div>
        </div>
      </div>

      <!-- 新建项目卡片 -->
      <div class="project-card new-project" @click="showCreateModal = true">
        <div class="new-project-content">
          <div class="new-project-icon">
            <svg viewBox="0 0 24 24" fill="none" width="32" height="32">
              <path d="M12 5v14M5 12h14" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
            </svg>
          </div>
          <span class="new-project-text">创建新项目</span>
        </div>
      </div>
    </div>

    <!-- 列表视图 -->
    <div v-else class="projects-table">
      <table class="table">
        <thead>
          <tr>
            <th>项目名称</th>
            <th>状态</th>
            <th>进度</th>
            <th>任务</th>
            <th>团队</th>
            <th>截止日期</th>
            <th>操作</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="project in filteredProjects" :key="project.id" @click="$router.push(`/projects/${project.id}`)">
            <td>
              <div class="project-cell">
                <div class="project-icon-sm" :style="{ background: project.color }">
                  {{ project.name.charAt(0) }}
                </div>
                <div>
                  <span class="project-name-text">{{ project.name }}</span>
                  <span class="project-desc-text">{{ project.description }}</span>
                </div>
              </div>
            </td>
            <td>
              <span class="project-status" :class="project.status">{{ project.statusText }}</span>
            </td>
            <td>
              <div class="table-progress">
                <div class="progress-bar-sm">
                  <div class="progress-fill-sm" :style="{ width: `${project.progress}%` }"></div>
                </div>
                <span>{{ project.progress }}%</span>
              </div>
            </td>
            <td>{{ project.tasks }}</td>
            <td>
              <div class="team-avatars">
                <div v-for="(member, idx) in project.members.slice(0, 3)" :key="idx" class="team-avatar-sm">
                  {{ member.charAt(0) }}
                </div>
              </div>
            </td>
            <td>{{ project.dueDate }}</td>
            <td>
              <div class="action-btns">
                <button class="action-btn" @click.stop="$router.push(`/projects/${project.id}`)" title="查看">
                  <svg viewBox="0 0 24 24" fill="none" width="16" height="16">
                    <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z" stroke="currentColor" stroke-width="2"/>
                    <circle cx="12" cy="12" r="3" stroke="currentColor" stroke-width="2"/>
                  </svg>
                </button>
                <button class="action-btn" @click.stop="editProject(project)" title="编辑">
                  <svg viewBox="0 0 24 24" fill="none" width="16" height="16">
                    <path d="M11 4H4a2 2 0 00-2 2v14a2 2 0 002 2h14a2 2 0 002-2v-7" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                    <path d="M18.5 2.5a2.121 2.121 0 013 3L12 15l-4 1 1-4 9.5-9.5z" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                  </svg>
                </button>
                <button class="action-btn delete" @click.stop="deleteProject(project.id)" title="删除">
                  <svg viewBox="0 0 24 24" fill="none" width="16" height="16">
                    <path d="M3 6h18M19 6v14a2 2 0 01-2 2H7a2 2 0 01-2-2V6m3 0V4a2 2 0 012-2h4a2 2 0 012 2v2" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                  </svg>
                </button>
              </div>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- 空状态 -->
    <div v-if="filteredProjects.length === 0 && projects.length > 0" class="empty-state">
      <div class="empty-icon">
        <svg viewBox="0 0 24 24" fill="none" width="32" height="32">
          <path d="M21 21L15 15M17 10C17 13.866 13.866 17 10 17C6.13401 17 3 13.866 3 10C3 6.13401 6.13401 3 10 3C13.866 3 17 6.13401 17 10Z" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
        </svg>
      </div>
      <h3 class="empty-title">未找到匹配的项目</h3>
      <p class="empty-description">请尝试调整筛选条件或搜索关键字</p>
    </div>
  </div>
</template>

<script setup>
import { ref, computed } from 'vue'

const searchQuery = ref('')
const filterStatus = ref('')
const viewMode = ref('grid')
const showCreateModal = ref(false)

const projects = ref([
  { id: 1, name: 'AI助手', description: '智能对话系统开发', status: 'active', statusText: '进行中', progress: 75, tasks: 24, members: ['张三', '李四', '王五', '赵六'], dueDate: '2026-04-30', color: 'linear-gradient(135deg, #3B82F6, #60A5FA)' },
  { id: 2, name: '数据分析平台', description: '用户行为分析系统', status: 'active', statusText: '进行中', progress: 60, tasks: 18, members: ['张三', '李四'], dueDate: '2026-05-15', color: 'linear-gradient(135deg, #10B981, #34D399)' },
  { id: 3, name: '移动端APP', description: 'iOS/Android应用开发', status: 'active', statusText: '进行中', progress: 45, tasks: 32, members: ['王五', '赵六', '孙七'], dueDate: '2026-06-01', color: 'linear-gradient(135deg, #F59E0B, #FBBF24)' },
  { id: 4, name: '官网改版', description: '企业官网全新设计', status: 'completed', statusText: '已完成', progress: 100, tasks: 15, members: ['张三', '孙七'], dueDate: '2026-03-20', color: 'linear-gradient(135deg, #6366F1, #818CF8)' },
  { id: 5, name: '电商后台', description: '电商管理系统开发', status: 'paused', statusText: '已暂停', progress: 30, tasks: 8, members: ['李四', '赵六'], dueDate: '2026-05-30', color: 'linear-gradient(135deg, #EC4899, #F472B6)' },
  { id: 6, name: '内部办公系统', description: 'OA办公自动化系统', status: 'active', statusText: '进行中', progress: 55, tasks: 20, members: ['张三', '李四', '王五'], dueDate: '2026-06-15', color: 'linear-gradient(135deg, #14B8A6, #2DD4BF)' },
])

const activeProjects = computed(() => projects.value.filter(p => p.status === 'active').length)
const completedProjects = computed(() => projects.value.filter(p => p.status === 'completed').length)

const filteredProjects = computed(() => {
  return projects.value.filter(project => {
    const matchSearch = project.name.toLowerCase().includes(searchQuery.value.toLowerCase()) ||
                        project.description.toLowerCase().includes(searchQuery.value.toLowerCase())
    const matchStatus = !filterStatus.value || project.status === filterStatus.value
    return matchSearch && matchStatus
  })
})

const editProject = (project) => {
  console.log('Edit project:', project)
}

const deleteProject = (id) => {
  if (confirm('确定要删除这个项目吗？')) {
    projects.value = projects.value.filter(p => p.id !== id)
  }
}
</script>

<style scoped>
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

/* 统计概览 */
.project-stats {
  display: flex;
  align-items: center;
  gap: var(--space-6);
  padding: var(--space-4) var(--space-5);
  background: var(--bg-card-solid);
  border-radius: var(--radius-xl);
  margin-bottom: var(--space-5);
  border: 1px solid var(--border-light);
  box-shadow: var(--shadow-card);
}

.stat-item {
  display: flex;
  align-items: center;
  gap: var(--space-2);
}

.stat-dot {
  width: 8px;
  height: 8px;
  border-radius: var(--radius-full);
}

.stat-dot.active {
  background: var(--primary-color);
}

.stat-dot.completed {
  background: var(--success-color);
}

.stat-label {
  font-size: var(--font-size-sm);
  color: var(--text-secondary);
}

.stat-value {
  font-size: var(--font-size-lg);
  font-weight: var(--font-weight-bold);
  color: var(--text-primary);
}

.stat-number {
  font-size: var(--font-size-lg);
  font-weight: var(--font-weight-bold);
  color: var(--primary-color);
}

.stat-divider {
  width: 1px;
  height: 24px;
  background: var(--border-color);
}

/* 筛选栏 */
.filter-bar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--space-4);
  margin-bottom: var(--space-5);
}

.search-box {
  display: flex;
  align-items: center;
  gap: var(--space-3);
  padding: var(--space-3) var(--space-4);
  background: var(--bg-card-solid);
  border: 1px solid var(--border-color);
  border-radius: var(--radius-xl);
  flex: 1;
  max-width: 400px;
  transition: all var(--transition-fast);
}

.search-box:focus-within {
  border-color: var(--primary-color);
  box-shadow: 0 0 0 3px rgba(59, 130, 246, 0.1);
}

.search-box svg {
  color: var(--text-tertiary);
  flex-shrink: 0;
}

.filter-input {
  flex: 1;
  border: none;
  background: transparent;
  font-size: var(--font-size-sm);
  color: var(--text-primary);
  outline: none;
}

.filter-input::placeholder {
  color: var(--text-tertiary);
}

.filter-actions {
  display: flex;
  align-items: center;
  gap: var(--space-3);
}

.filter-select {
  padding: var(--space-3) var(--space-4);
  font-size: var(--font-size-sm);
  border: 1px solid var(--border-color);
  border-radius: var(--radius-base);
  background: var(--bg-card-solid);
  color: var(--text-primary);
  cursor: pointer;
  outline: none;
  transition: all var(--transition-fast);
}

.filter-select:focus {
  border-color: var(--primary-color);
}

.view-toggle {
  display: flex;
  background: var(--bg-color-secondary);
  padding: var(--space-1);
  border-radius: var(--radius-base);
}

.view-btn {
  width: 36px;
  height: 36px;
  display: flex;
  align-items: center;
  justify-content: center;
  border: none;
  border-radius: var(--radius-sm);
  background: transparent;
  color: var(--text-secondary);
  cursor: pointer;
  transition: all var(--transition-fast);
}

.view-btn:hover {
  color: var(--text-primary);
}

.view-btn.active {
  background: var(--bg-card-solid);
  color: var(--primary-color);
  box-shadow: var(--shadow-xs);
}

/* 项目网格 */
.projects-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(320px, 1fr));
  gap: var(--space-5);
}

.project-card {
  background: var(--bg-card-solid);
  border-radius: var(--radius-xl);
  padding: var(--space-5);
  border: 1px solid var(--border-light);
  box-shadow: var(--shadow-card);
  cursor: pointer;
  transition: all var(--transition-normal);
  animation: cardEnter 0.5s ease backwards;
}

.project-card:hover {
  transform: translateY(-4px);
  box-shadow: var(--shadow-lg);
  border-color: var(--primary-light);
}

.project-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: var(--space-4);
}

.project-icon {
  width: 48px;
  height: 48px;
  border-radius: var(--radius-lg);
  display: flex;
  align-items: center;
  justify-content: center;
  color: white;
  font-weight: var(--font-weight-bold);
  font-size: var(--font-size-lg);
}

.project-status {
  font-size: var(--font-size-xs);
  font-weight: var(--font-weight-medium);
  padding: var(--space-1) var(--space-3);
  border-radius: var(--radius-full);
}

.project-status.active {
  background: var(--primary-lighter);
  color: var(--primary-color);
}

.project-status.completed {
  background: var(--success-lighter);
  color: var(--success-color);
}

.project-status.paused {
  background: var(--warning-lighter);
  color: var(--warning-color);
}

.project-name {
  font-size: var(--font-size-base);
  font-weight: var(--font-weight-semibold);
  color: var(--text-primary);
  margin-bottom: var(--space-2);
}

.project-desc {
  font-size: var(--font-size-sm);
  color: var(--text-secondary);
  margin-bottom: var(--space-4);
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
}

.project-progress {
  margin-bottom: var(--space-4);
}

.progress-info {
  display: flex;
  justify-content: space-between;
  font-size: var(--font-size-xs);
  color: var(--text-tertiary);
  margin-bottom: var(--space-2);
}

.progress-percent {
  font-weight: var(--font-weight-medium);
  color: var(--primary-color);
}

.progress-bar {
  height: 6px;
  background: var(--bg-color-secondary);
  border-radius: var(--radius-full);
  overflow: hidden;
}

.progress-fill {
  height: 100%;
  background: var(--gradient-primary);
  border-radius: var(--radius-full);
  transition: width 0.8s ease;
}

.project-footer {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding-top: var(--space-4);
  border-top: 1px solid var(--border-color);
}

.project-meta {
  display: flex;
  align-items: center;
  gap: var(--space-2);
  font-size: var(--font-size-xs);
  color: var(--text-tertiary);
}

.project-team {
  display: flex;
  align-items: center;
}

.team-avatar {
  width: 28px;
  height: 28px;
  border-radius: var(--radius-full);
  background: var(--gradient-primary);
  display: flex;
  align-items: center;
  justify-content: center;
  color: white;
  font-size: var(--font-size-xs);
  font-weight: var(--font-weight-medium);
  margin-left: -8px;
  border: 2px solid var(--bg-card-solid);
}

.team-avatar:first-child {
  margin-left: 0;
}

.team-more {
  margin-left: var(--space-2);
  font-size: var(--font-size-xs);
  color: var(--text-tertiary);
}

/* 新建项目卡片 */
.new-project {
  display: flex;
  align-items: center;
  justify-content: center;
  border: 2px dashed var(--border-color);
  background: transparent;
  min-height: 280px;
}

.new-project:hover {
  border-color: var(--primary-color);
  background: var(--primary-lighter);
}

.new-project-content {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: var(--space-3);
}

.new-project-icon {
  width: 64px;
  height: 64px;
  border-radius: var(--radius-full);
  background: var(--primary-lighter);
  display: flex;
  align-items: center;
  justify-content: center;
  color: var(--primary-color);
  transition: all var(--transition-fast);
}

.new-project:hover .new-project-icon {
  background: var(--primary-color);
  color: white;
}

.new-project-text {
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  color: var(--text-secondary);
}

.new-project:hover .new-project-text {
  color: var(--primary-color);
}

/* 表格视图 */
.projects-table {
  background: var(--bg-card-solid);
  border-radius: var(--radius-xl);
  overflow: hidden;
  border: 1px solid var(--border-light);
  box-shadow: var(--shadow-card);
}

.table {
  width: 100%;
  border-collapse: collapse;
}

.table th {
  padding: var(--space-4);
  text-align: left;
  font-size: var(--font-size-xs);
  font-weight: var(--font-weight-semibold);
  color: var(--text-tertiary);
  text-transform: uppercase;
  letter-spacing: 0.05em;
  background: var(--bg-color-secondary);
  border-bottom: 1px solid var(--border-color);
}

.table td {
  padding: var(--space-4);
  font-size: var(--font-size-sm);
  color: var(--text-primary);
  border-bottom: 1px solid var(--border-color);
}

.table tbody tr {
  cursor: pointer;
  transition: background var(--transition-fast);
}

.table tbody tr:hover {
  background: var(--primary-lighter);
}

.project-cell {
  display: flex;
  align-items: center;
  gap: var(--space-3);
}

.project-icon-sm {
  width: 36px;
  height: 36px;
  border-radius: var(--radius-base);
  display: flex;
  align-items: center;
  justify-content: center;
  color: white;
  font-weight: var(--font-weight-semibold);
  font-size: var(--font-size-sm);
  flex-shrink: 0;
}

.project-name-text {
  display: block;
  font-weight: var(--font-weight-medium);
}

.project-desc-text {
  display: block;
  font-size: var(--font-size-xs);
  color: var(--text-tertiary);
}

.table-progress {
  display: flex;
  align-items: center;
  gap: var(--space-2);
}

.progress-bar-sm {
  width: 60px;
  height: 4px;
  background: var(--bg-color-secondary);
  border-radius: var(--radius-full);
  overflow: hidden;
}

.progress-fill-sm {
  height: 100%;
  background: var(--gradient-primary);
  border-radius: var(--radius-full);
}

.team-avatars {
  display: flex;
}

.team-avatar-sm {
  width: 28px;
  height: 28px;
  border-radius: var(--radius-full);
  background: var(--gradient-primary);
  display: flex;
  align-items: center;
  justify-content: center;
  color: white;
  font-size: var(--font-size-xs);
  font-weight: var(--font-weight-medium);
  margin-left: -8px;
  border: 2px solid white;
}

.team-avatar-sm:first-child {
  margin-left: 0;
}

.action-btns {
  display: flex;
  gap: var(--space-1);
}

.action-btn {
  width: 32px;
  height: 32px;
  display: flex;
  align-items: center;
  justify-content: center;
  border: none;
  border-radius: var(--radius-base);
  background: transparent;
  color: var(--text-secondary);
  cursor: pointer;
  transition: all var(--transition-fast);
}

.action-btn:hover {
  background: var(--primary-lighter);
  color: var(--primary-color);
}

.action-btn.delete:hover {
  background: var(--error-lighter);
  color: var(--error-color);
}

/* 空状态 */
.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: var(--space-12);
  background: var(--bg-card-solid);
  border-radius: var(--radius-xl);
  border: 1px solid var(--border-light);
  text-align: center;
}

.empty-icon {
  width: 80px;
  height: 80px;
  border-radius: var(--radius-full);
  background: var(--primary-lighter);
  display: flex;
  align-items: center;
  justify-content: center;
  margin-bottom: var(--space-4);
  color: var(--primary-color);
}

.empty-title {
  font-size: var(--font-size-lg);
  font-weight: var(--font-weight-semibold);
  color: var(--text-primary);
  margin-bottom: var(--space-2);
}

.empty-description {
  font-size: var(--font-size-sm);
  color: var(--text-secondary);
}

/* 响应式 */
@media (max-width: 768px) {
  .filter-bar {
    flex-direction: column;
    align-items: stretch;
  }
  
  .search-box {
    max-width: none;
  }
  
  .filter-actions {
    justify-content: space-between;
  }
  
  .projects-grid {
    grid-template-columns: 1fr;
  }
}
</style>
