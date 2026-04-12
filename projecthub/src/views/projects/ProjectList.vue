<template>
  <div class="project-list-page">
    <!-- 页面头部 -->
    <div class="page-header fade-in">
      <div class="page-header-left">
        <h2 class="page-title">项目列表</h2>
        <span class="page-subtitle">共 {{ filteredProjects.length }} 个项目</span>
      </div>
      <div class="page-header-right">
        <t-button theme="primary" @click="showCreateDialog = true">
          <template #icon>
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <line x1="12" y1="5" x2="12" y2="19"/><line x1="5" y1="12" x2="19" y2="12"/>
            </svg>
          </template>
          新建项目
        </t-button>
      </div>
    </div>

    <!-- 筛选栏 -->
    <div class="filter-bar fade-in stagger-1">
      <div class="filter-tabs">
        <button
          v-for="tab in statusTabs"
          :key="tab.value"
          class="filter-tab"
          :class="{ active: filterStatus === tab.value }"
          @click="filterStatus = tab.value"
        >
          {{ tab.label }}
          <span class="tab-count">{{ tab.count }}</span>
        </button>
      </div>
      <div class="filter-search">
        <t-input v-model="searchQuery" placeholder="搜索项目..." clearable>
          <template #prefix-icon>
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <circle cx="11" cy="11" r="8"/><path d="m21 21-4.35-4.35"/>
            </svg>
          </template>
        </t-input>
      </div>
    </div>

    <!-- 项目网格 -->
    <div class="project-grid">
      <div
        v-for="(project, index) in filteredProjects"
        :key="project.id"
        class="project-card fade-in-up"
        :style="{ animationDelay: `${0.08 * index}s` }"
        @click="goToProject(project.id)"
      >
        <div class="project-card-header">
          <div class="project-icon" :style="{ background: getProjectBgColor(project.type) }">
            {{ project.name.charAt(0).toUpperCase() }}
          </div>
          <div class="project-status">
            <t-tag :theme="getStatusTheme(project.status)" variant="light" size="small">
              {{ getStatusLabel(project.status) }}
            </t-tag>
          </div>
        </div>
        
        <div class="project-card-body">
          <h3 class="project-name">{{ project.name }}</h3>
          <p class="project-desc">{{ project.description || '暂无描述' }}</p>
          
          <div class="project-meta">
            <span class="meta-item">
              <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M22 19a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h5l2 3h9a2 2 0 0 1 2 2z"/>
              </svg>
              {{ getTypeLabel(project.type) }}
            </span>
            <span class="meta-item" v-if="project.customer">
              <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"/><circle cx="12" cy="7" r="4"/>
              </svg>
              {{ project.customer }}
            </span>
          </div>
        </div>
        
        <div class="project-card-footer">
          <div class="project-progress">
            <div class="progress-header">
              <span class="progress-label">进度</span>
              <span class="progress-value">{{ project.progress || 0 }}%</span>
            </div>
            <t-progress
              :percentage="project.progress || 0"
              :color="getProgressColor(project.progress || 0)"
              :show-text="false"
              theme="line"
              size="small"
            />
          </div>
          <div class="project-actions" @click.stop>
            <t-button variant="text" size="small" @click="editProject(project)">
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"/><path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"/>
              </svg>
            </t-button>
            <t-button variant="text" size="small" @click="deleteProject(project)">
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <polyline points="3 6 5 6 21 6"/><path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"/>
              </svg>
            </t-button>
          </div>
        </div>
        
        <div class="project-card-overlay">
          <span>查看详情</span>
        </div>
      </div>

      <!-- 创建项目卡片 -->
      <div class="project-card create-card fade-in-up" @click="showCreateDialog = true">
        <div class="create-content">
          <div class="create-icon">
            <svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <line x1="12" y1="5" x2="12" y2="19"/><line x1="5" y1="12" x2="19" y2="12"/>
            </svg>
          </div>
          <span>创建新项目</span>
        </div>
      </div>
    </div>

    <!-- 空状态 -->
    <div v-if="filteredProjects.length === 0 && !searchQuery" class="empty-state fade-in">
      <div class="empty-illustration">
        <svg width="120" height="120" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1">
          <path d="M22 19a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h5l2 3h9a2 2 0 0 1 2 2z"/>
        </svg>
      </div>
      <h3>开始你的第一个项目</h3>
      <p>创建项目来跟踪任务、管理时间线并达成目标</p>
      <t-button theme="primary" size="large" @click="showCreateDialog = true">
        <template #icon>
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <line x1="12" y1="5" x2="12" y2="19"/><line x1="5" y1="12" x2="19" y2="12"/>
          </svg>
        </template>
        创建项目
      </t-button>
    </div>

    <!-- 创建项目弹窗 -->
    <t-dialog
      v-model:visible="showCreateDialog"
      header="新建项目"
      :footer="false"
      width="520px"
    >
      <div class="form-group">
        <label>项目名称 <span class="required">*</span></label>
        <t-input v-model="newProject.name" placeholder="请输入项目名称" />
      </div>
      <div class="form-group">
        <label>项目描述</label>
        <t-textarea v-model="newProject.description" placeholder="简要描述项目目标" />
      </div>
      <div class="form-row">
        <div class="form-group">
          <label>项目类型 <span class="required">*</span></label>
          <t-select v-model="newProject.type" placeholder="选择类型" :options="typeOptions" />
        </div>
        <div class="form-group">
          <label>客户/来源</label>
          <t-input v-model="newProject.customer" placeholder="客户名称（可选）" />
        </div>
      </div>
      <div class="form-actions">
        <t-button variant="outline" @click="showCreateDialog = false">取消</t-button>
        <t-button theme="primary" @click="createProject">创建项目</t-button>
      </div>
    </t-dialog>

    <!-- 编辑项目弹窗 -->
    <t-dialog
      v-model:visible="showEditDialog"
      header="编辑项目"
      :footer="false"
      width="520px"
    >
      <div class="form-group">
        <label>项目名称 <span class="required">*</span></label>
        <t-input v-model="editForm.name" placeholder="请输入项目名称" />
      </div>
      <div class="form-group">
        <label>项目描述</label>
        <t-textarea v-model="editForm.description" placeholder="简要描述项目目标" />
      </div>
      <div class="form-row">
        <div class="form-group">
          <label>项目类型 <span class="required">*</span></label>
          <t-select v-model="editForm.type" placeholder="选择类型" :options="typeOptions" />
        </div>
        <div class="form-group">
          <label>客户/来源</label>
          <t-input v-model="editForm.customer" placeholder="客户名称（可选）" />
        </div>
      </div>
      <div class="form-group">
        <label>状态</label>
        <t-select v-model="editForm.status" placeholder="选择状态" :options="statusOptions" />
      </div>
      <div class="form-actions">
        <t-button variant="outline" @click="showEditDialog = false">取消</t-button>
        <t-button theme="primary" @click="saveProject">保存修改</t-button>
      </div>
    </t-dialog>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { MessagePlugin } from 'tdesign-vue-next'
import dayjs from 'dayjs'

const router = useRouter()
const projects = ref([])
const filterStatus = ref('all')
const searchQuery = ref('')
const showCreateDialog = ref(false)
const showEditDialog = ref(false)

const newProject = ref({
  name: '',
  description: '',
  type: 'web',
  customer: ''
})

const editForm = ref({
  id: null,
  name: '',
  description: '',
  type: 'web',
  customer: '',
  status: 'in_progress'
})

const typeOptions = [
  { value: 'web', label: 'Web 应用' },
  { value: 'mobile', label: '移动端应用' },
  { value: 'design', label: '设计项目' },
  { value: 'other', label: '其他' }
]

const statusOptions = [
  { value: 'in_progress', label: '进行中' },
  { value: 'completed', label: '已完成' },
  { value: 'paused', label: '已暂停' }
]

const statusTabs = computed(() => [
  { label: '全部', value: 'all', count: projects.value.length },
  { label: '进行中', value: 'in_progress', count: projects.value.filter(p => p.status === 'in_progress').length },
  { label: '已完成', value: 'completed', count: projects.value.filter(p => p.status === 'completed').length },
  { label: '已暂停', value: 'paused', count: projects.value.filter(p => p.status === 'paused').length }
])

const filteredProjects = computed(() => {
  let result = projects.value
  
  if (filterStatus.value !== 'all') {
    result = result.filter(p => p.status === filterStatus.value)
  }
  
  if (searchQuery.value) {
    const query = searchQuery.value.toLowerCase()
    result = result.filter(p => 
      p.name.toLowerCase().includes(query) ||
      p.description?.toLowerCase().includes(query) ||
      p.customer?.toLowerCase().includes(query)
    )
  }
  
  return result
})

const fetchProjects = async () => {
  try {
    const res = await fetch('/api/projects')
    if (res.ok) {
      projects.value = await res.json()
    }
  } catch (error) {
    console.error('Failed to fetch projects:', error)
  }
}

const createProject = async () => {
  if (!newProject.value.name) {
    MessagePlugin.warning('请输入项目名称')
    return
  }
  if (!newProject.value.type) {
    MessagePlugin.warning('请选择项目类型')
    return
  }
  
  try {
    const res = await fetch('/api/projects', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        ...newProject.value,
        status: 'in_progress'
      })
    })
    
    if (res.ok) {
      const project = await res.json()
      projects.value.unshift(project)
      showCreateDialog.value = false
      newProject.value = { name: '', description: '', type: 'web', customer: '' }
      MessagePlugin.success('项目创建成功')
    }
  } catch (error) {
    MessagePlugin.error('创建失败')
  }
}

const goToProject = (id) => {
  router.push(`/projects/${id}`)
}

const editProject = (project) => {
  editForm.value = {
    id: project.id,
    name: project.name,
    description: project.description || '',
    type: project.type,
    customer: project.customer || '',
    status: project.status
  }
  showEditDialog.value = true
}

const saveProject = async () => {
  if (!editForm.value.name) {
    MessagePlugin.warning('请输入项目名称')
    return
  }
  
  try {
    const res = await fetch(`/api/projects/${editForm.value.id}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        name: editForm.value.name,
        description: editForm.value.description,
        type: editForm.value.type,
        customer: editForm.value.customer,
        status: editForm.value.status
      })
    })
    
    if (res.ok) {
      const updated = await res.json()
      const index = projects.value.findIndex(p => p.id === updated.id)
      if (index !== -1) {
        projects.value[index] = { ...projects.value[index], ...updated }
      }
      showEditDialog.value = false
      MessagePlugin.success('项目已更新')
    }
  } catch (error) {
    MessagePlugin.error('保存失败')
  }
}

const deleteProject = async (project) => {
  try {
    await fetch(`/api/projects/${project.id}`, { method: 'DELETE' })
    projects.value = projects.value.filter(p => p.id !== project.id)
    MessagePlugin.success('项目已删除')
  } catch (error) {
    MessagePlugin.error('删除失败')
  }
}

const getProjectBgColor = (type) => {
  const colors = {
    'web': 'rgba(37, 99, 235, 0.1)',
    'mobile': 'rgba(16, 185, 129, 0.1)',
    'design': 'rgba(139, 92, 246, 0.1)',
    'other': 'rgba(107, 114, 128, 0.1)'
  }
  return colors[type] || colors.other
}

const getTypeLabel = (type) => {
  return { web: 'Web 应用', mobile: '移动端', design: '设计', other: '其他' }[type] || '其他'
}

const getStatusTheme = (status) => {
  return { in_progress: 'primary', completed: 'success', paused: 'warning' }[status] || 'default'
}

const getStatusLabel = (status) => {
  return { in_progress: '进行中', completed: '已完成', paused: '已暂停' }[status] || '未知'
}

const getProgressColor = (progress) => {
  if (progress < 30) return '#EF4444'
  if (progress < 70) return '#F59E0B'
  return '#10B981'
}

onMounted(() => {
  fetchProjects()
})
</script>

<style scoped>
.project-list-page {
  animation: fadeIn 0.3s ease-out;
}

.page-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: var(--space-5);
}

.page-header-left {
  display: flex;
  align-items: baseline;
  gap: var(--space-3);
}

.page-title {
  font-size: var(--font-size-2xl);
  font-weight: var(--font-weight-semibold);
  color: var(--text-primary);
}

.page-subtitle {
  font-size: var(--font-size-sm);
  color: var(--text-tertiary);
}

/* 筛选栏 */
.filter-bar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: var(--space-3) var(--space-4);
  background: var(--bg-secondary);
  border-radius: var(--radius-xl);
  border: 1px solid var(--border-light);
  margin-bottom: var(--space-6);
}

.filter-tabs {
  display: flex;
  gap: var(--space-1);
}

.filter-tab {
  display: flex;
  align-items: center;
  gap: var(--space-2);
  padding: var(--space-2) var(--space-3);
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  color: var(--text-secondary);
  border-radius: var(--radius-lg);
  transition: all var(--transition-fast);
}

.filter-tab:hover {
  background: var(--gray-100);
  color: var(--text-primary);
}

.filter-tab.active {
  background: var(--primary-50);
  color: var(--primary-600);
}

.tab-count {
  font-size: var(--font-size-xs);
  background: var(--gray-100);
  padding: 2px 6px;
  border-radius: var(--radius-full);
}

.filter-tab.active .tab-count {
  background: var(--primary-100);
  color: var(--primary-600);
}

.filter-search {
  width: 240px;
}

/* 项目网格 */
.project-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: var(--space-5);
}

.project-card {
  position: relative;
  background: var(--bg-secondary);
  border-radius: var(--radius-xl);
  border: 1px solid var(--border-light);
  overflow: hidden;
  cursor: pointer;
  transition: all var(--transition-base);
  opacity: 0;
}

.project-card:hover {
  border-color: var(--primary-200);
  box-shadow: var(--shadow-lg);
  transform: translateY(-4px);
}

.project-card:hover .project-card-overlay {
  opacity: 1;
}

.project-card-overlay {
  position: absolute;
  inset: 0;
  background: rgba(37, 99, 235, 0.9);
  display: flex;
  align-items: center;
  justify-content: center;
  opacity: 0;
  transition: opacity var(--transition-base);
}

.project-card-overlay span {
  color: white;
  font-weight: var(--font-weight-medium);
  font-size: var(--font-size-base);
}

.project-card-header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  padding: var(--space-5);
  border-bottom: 1px solid var(--border-light);
}

.project-icon {
  width: 48px;
  height: 48px;
  border-radius: var(--radius-xl);
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: var(--font-size-xl);
  font-weight: var(--font-weight-bold);
  color: var(--primary-600);
  transition: transform var(--transition-bounce);
}

.project-card:hover .project-icon {
  transform: scale(1.1) rotate(5deg);
}

.project-card-body {
  padding: var(--space-5);
}

.project-name {
  font-size: var(--font-size-lg);
  font-weight: var(--font-weight-semibold);
  color: var(--text-primary);
  margin-bottom: var(--space-2);
}

.project-desc {
  font-size: var(--font-size-sm);
  color: var(--text-secondary);
  line-height: var(--line-height-relaxed);
  margin-bottom: var(--space-4);
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
}

.project-meta {
  display: flex;
  gap: var(--space-4);
}

.meta-item {
  display: flex;
  align-items: center;
  gap: var(--space-1);
  font-size: var(--font-size-xs);
  color: var(--text-tertiary);
}

.project-card-footer {
  display: flex;
  align-items: flex-end;
  justify-content: space-between;
  padding: var(--space-4) var(--space-5);
  background: var(--gray-50);
  border-top: 1px solid var(--border-light);
}

.project-progress {
  flex: 1;
  margin-right: var(--space-4);
}

.progress-header {
  display: flex;
  justify-content: space-between;
  margin-bottom: var(--space-2);
}

.progress-label {
  font-size: var(--font-size-xs);
  color: var(--text-tertiary);
}

.progress-value {
  font-size: var(--font-size-xs);
  font-weight: var(--font-weight-medium);
  color: var(--text-primary);
}

.project-actions {
  display: flex;
  gap: var(--space-1);
  opacity: 0;
  transition: opacity var(--transition-fast);
}

.project-card:hover .project-actions {
  opacity: 1;
}

/* 创建卡片 */
.create-card {
  display: flex;
  align-items: center;
  justify-content: center;
  min-height: 240px;
  border-style: dashed;
  border-color: var(--gray-300);
  background: transparent;
}

.create-card:hover {
  border-color: var(--primary-400);
  background: var(--primary-50);
  transform: none;
  box-shadow: none;
}

.create-card:hover .create-icon {
  transform: scale(1.1);
  background: var(--primary-500);
  color: white;
}

.create-content {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: var(--space-3);
  color: var(--text-tertiary);
}

.create-icon {
  width: 64px;
  height: 64px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  background: var(--gray-100);
  transition: all var(--transition-bounce);
}

.create-content span {
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
}

/* 空状态 */
.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: var(--space-12);
  text-align: center;
}

.empty-illustration {
  margin-bottom: var(--space-6);
  color: var(--gray-300);
}

.empty-state h3 {
  font-size: var(--font-size-xl);
  font-weight: var(--font-weight-semibold);
  color: var(--text-primary);
  margin-bottom: var(--space-2);
}

.empty-state p {
  font-size: var(--font-size-sm);
  color: var(--text-tertiary);
  margin-bottom: var(--space-6);
}

/* 表单 */
.form-group {
  margin-bottom: var(--space-4);
}

.form-group label {
  display: block;
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  color: var(--text-primary);
  margin-bottom: var(--space-2);
}

.required {
  color: var(--error-500);
}

.form-row {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: var(--space-4);
}

.form-actions {
  display: flex;
  justify-content: flex-end;
  gap: var(--space-3);
  margin-top: var(--space-6);
}

/* 响应式 */
@media (max-width: 1200px) {
  .project-grid {
    grid-template-columns: repeat(2, 1fr);
  }
}

@media (max-width: 768px) {
  .project-grid {
    grid-template-columns: 1fr;
  }
  .filter-bar {
    flex-direction: column;
    gap: var(--space-3);
  }
  .filter-search {
    width: 100%;
  }
}
</style>
