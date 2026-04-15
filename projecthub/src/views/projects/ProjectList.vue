<template>
  <div class="projects-page">
    <!-- 页面头部 -->
    <div class="page-header">
      <div class="header-content">
        <div class="header-info">
          <h1 class="page-title">项目管理</h1>
          <p class="page-subtitle">管理您的所有项目，随时掌握进度</p>
        </div>
        <button class="btn btn-primary" @click="showCreateDialog = true">
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
        <t-select v-model="filterStatus" placeholder="筛选状态" clearable style="width: 140px">
          <t-option value="" label="全部状态" />
          <t-option value="in_progress" label="进行中" />
          <t-option value="completed" label="已完成" />
          <t-option value="overdue" label="已延期" />
        </t-select>
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
          <t-tag :theme="project.status === 'completed' ? 'success' : project.status === 'overdue' ? 'danger' : 'primary'" variant="light">
            {{ project.statusText }}
          </t-tag>
        </div>
        <h3 class="project-name">{{ project.name }}</h3>
        <p class="project-desc">{{ project.description }}</p>
        <div class="project-progress">
          <div class="progress-info">
            <span>进度</span>
            <span class="progress-percent">{{ project.progress }}%</span>
          </div>
          <div class="progress-bar">
            <div class="progress-fill" :class="{ completed: project.progress === 100 }" :style="{ width: `${project.progress}%` }"></div>
          </div>
        </div>
        <div class="project-footer">
          <div class="project-meta">
            <svg viewBox="0 0 24 24" fill="none" width="14" height="14">
              <path d="M8 6h13M8 12h13M8 18h13M3 6h.01M3 12h.01M3 18h.01" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
            </svg>
            {{ project.tasks }} 任务
          </div>
        </div>
      </div>

      <!-- 新建项目卡片 -->
      <div class="project-card new-project" @click="showCreateDialog = true">
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
              <t-tag :theme="project.status === 'completed' ? 'success' : project.status === 'overdue' ? 'danger' : 'primary'" variant="light">
                {{ project.statusText }}
              </t-tag>
            </td>
            <td>
              <div class="table-progress">
                <div class="progress-bar-sm">
                  <div class="progress-fill-sm" :class="{ completed: project.progress === 100 }" :style="{ width: `${project.progress}%` }"></div>
                </div>
                <span>{{ project.progress }}%</span>
              </div>
            </td>
            <td>{{ project.tasks }}</td>
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

    <!-- 新建项目弹窗 -->
    <t-dialog v-model:visible="showCreateDialog" header="创建新项目" :footer="false" width="500px" @close="showCreateDialog = false">
      <t-form :model="createForm" :rules="createRules" ref="formRef" @submit="handleCreate" label-width="100px">
        <t-form-item label="项目名称" name="name">
          <t-input v-model="createForm.name" placeholder="请输入项目名称" />
        </t-form-item>
        <t-form-item label="项目描述" name="description">
          <t-textarea v-model="createForm.description" placeholder="请输入项目描述" :rows="3" />
        </t-form-item>
        <t-form-item label="项目类型" name="type">
          <t-radio-group v-model="createForm.type">
            <t-radio value="web">Web应用</t-radio>
            <t-radio value="mobile">移动应用</t-radio>
            <t-radio value="desktop">桌面应用</t-radio>
            <t-radio value="ai">AI项目</t-radio>
            <t-radio value="other">其他</t-radio>
          </t-radio-group>
        </t-form-item>
        <t-form-item style="margin-top: 24px">
          <t-button type="submit" theme="primary" :loading="saving">创建项目</t-button>
          <t-button variant="outline" style="margin-left: 12px" @click="showCreateDialog = false">取消</t-button>
        </t-form-item>
      </t-form>
    </t-dialog>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { projectService } from '@/services/dataService'
import { MessagePlugin } from 'tdesign-vue-next'
import dayjs from 'dayjs'

const router = useRouter()

const searchQuery = ref('')
const filterStatus = ref('')
const viewMode = ref('grid')
const loading = ref(false)

// 新建项目弹窗
const showCreateDialog = ref(false)
const saving = ref(false)
const formRef = ref(null)
const createForm = ref({
  name: '',
  description: '',
  type: 'web'
})
const createRules = {
  name: [{ required: true, message: '请输入项目名称', trigger: 'input' }]
}

// 创建项目
const handleCreate = async () => {
  saving.value = true
  try {
    const res = await projectService.create({
      name: createForm.value.name,
      description: createForm.value.description,
      type: createForm.value.type
    })
    showCreateDialog.value = false
    createForm.value = { name: '', description: '', type: 'web' }
    await loadProjects()
    MessagePlugin.success('项目创建成功')
  } catch (error) {
    console.error('创建项目失败:', error)
    MessagePlugin.error('创建失败，请重试')
  } finally {
    saving.value = false
  }
}

// 项目颜色映射
const projectColors = [
  'linear-gradient(135deg, #3B82F6, #60A5FA)',
  'linear-gradient(135deg, #10B981, #34D399)',
  'linear-gradient(135deg, #F59E0B, #FBBF24)',
  'linear-gradient(135deg, #6366F1, #818CF8)',
  'linear-gradient(135deg, #EC4899, #F472B6)',
  'linear-gradient(135deg, #14B8A6, #2DD4BF)'
]

const projects = ref([])

const activeProjects = computed(() => projects.value.filter(p => p.status === 'active' || p.status === 'in_progress').length)
const completedProjects = computed(() => projects.value.filter(p => p.status === 'completed').length)

// 状态文本映射
const getStatusText = (status) => {
  const map = {
    'in_progress': '进行中',
    'completed': '已完成',
    'overdue': '已延期'
  }
  return map[status] || status
}

// 加载项目数据
const loadProjects = async () => {
  loading.value = true
  try {
    const data = await projectService.getAll({
      status: filterStatus.value || undefined,
      search: searchQuery.value || undefined
    })
    
    projects.value = data.map((p, index) => ({
      id: p.id,
      name: p.name,
      description: p.description || p.customer || '暂无描述',
      status: p.status,
      statusText: getStatusText(p.status),
      progress: p.progress || 0,
      tasks: p.taskCount || p.tasks?.length || 0,
      completedTasks: p.completedTaskCount || 0,
      dueDate: p.completedAt ? dayjs(p.completedAt).format('YYYY年MM月DD日') : '未设置',
      color: projectColors[index % projectColors.length],
      createdAt: p.createdAt,
      updatedAt: p.updatedAt
    }))
  } catch (error) {
    console.error('加载项目失败:', error)
  } finally {
    loading.value = false
  }
}

const filteredProjects = computed(() => {
  return projects.value.filter(project => {
    const matchSearch = project.name.toLowerCase().includes(searchQuery.value.toLowerCase()) ||
                        project.description.toLowerCase().includes(searchQuery.value.toLowerCase()) ||
                        (project.customer && project.customer.toLowerCase().includes(searchQuery.value.toLowerCase()))
    const matchStatus = !filterStatus.value || project.status === filterStatus.value
    return matchSearch && matchStatus
  })
})

const editProject = (project) => {
  console.log('Edit project:', project)
}

const deleteProject = async (id) => {
  if (confirm('确定要删除这个项目吗？')) {
    try {
      await projectService.delete(id)
      projects.value = projects.value.filter(p => p.id !== id)
    } catch (error) {
      console.error('删除项目失败:', error)
      alert('删除失败，请重试')
    }
  }
}

// 监听筛选条件变化
const handleSearch = () => {
  loadProjects()
}

const handleStatusChange = () => {
  loadProjects()
}

// 组件挂载时加载数据
onMounted(() => {
  loadProjects()
})
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
  transition: width 0.8s ease, background 0.3s ease;
}

.progress-fill.completed {
  background: linear-gradient(90deg, #10B981, #34D399);
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
  transition: width 0.8s ease, background 0.3s ease;
}

.progress-fill-sm.completed {
  background: linear-gradient(90deg, #10B981, #34D399);
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
