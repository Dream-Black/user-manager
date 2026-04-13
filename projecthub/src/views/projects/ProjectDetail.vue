<template>
  <div class="project-detail-page">
    <!-- 项目头部 -->
    <div class="project-header" :style="{ background: project.gradient }">
      <div class="header-content">
        <div class="project-info">
          <div class="project-icon">{{ project.name.charAt(0) }}</div>
          <div class="project-text">
            <h1 class="project-name">{{ project.name }}</h1>
            <p class="project-description">{{ project.description }}</p>
          </div>
        </div>
        <div class="header-actions">
          <t-button variant="outline" theme="default">
            <template #icon><SettingIcon /></template>
            设置
          </t-button>
          <t-button theme="primary">
            <template #icon><AddIcon /></template>
            添加任务
          </t-button>
        </div>
      </div>
    </div>

    <!-- 项目统计 -->
    <div class="project-stats">
      <div class="stat-item">
        <span class="stat-value">{{ project.progress }}%</span>
        <span class="stat-label">完成度</span>
        <div class="stat-bar">
          <div class="stat-bar-fill" :style="{ width: project.progress + '%', background: project.color }"></div>
        </div>
      </div>
      <div class="stat-item">
        <span class="stat-value">{{ project.tasks }}</span>
        <span class="stat-label">总任务</span>
      </div>
      <div class="stat-item">
        <span class="stat-value">{{ project.completed }}</span>
        <span class="stat-label">已完成</span>
      </div>
      <div class="stat-item">
        <span class="stat-value">{{ project.memberCount }}</span>
        <span class="stat-label">成员</span>
      </div>
      <div class="stat-item">
        <span class="stat-value">{{ project.daysLeft }}</span>
        <span class="stat-label">剩余天数</span>
      </div>
    </div>

    <!-- 标签页 -->
    <t-tabs v-model="activeTab">
      <t-tab-panel value="tasks" label="任务列表">
        <div class="tasks-content">
          <div class="tasks-filters">
            <t-input placeholder="搜索任务..." style="width: 280px">
              <template #prefix-icon><SearchIcon /></template>
            </t-input>
            <t-select placeholder="状态筛选" style="width: 140px">
              <t-option value="all" label="全部" />
              <t-option value="pending" label="待开始" />
              <t-option value="in_progress" label="进行中" />
              <t-option value="completed" label="已完成" />
            </t-select>
          </div>

          <t-table :data="tasks" row-key="id" hover stripe>
            <t-table-column title="任务" width="40%">
              <template #default="{ row }">
                <div class="task-cell">
                  <t-checkbox :checked="row.status === 'completed'" />
                  <span :class="{ completed: row.status === 'completed' }">{{ row.title }}</span>
                </div>
              </template>
            </t-table-column>
            <t-table-column title="负责人" width="15%">
              <template #default="{ row }">
                <div class="assignee-cell">
                  <div class="avatar" :style="{ background: row.assigneeColor }">{{ row.assignee.charAt(0) }}</div>
                  {{ row.assignee }}
                </div>
              </template>
            </t-table-column>
            <t-table-column title="优先级" width="12%">
              <template #default="{ row }">
                <t-tag :type="getPriorityType(row.priority)" variant="light" size="small">{{ row.priorityText }}</t-tag>
              </template>
            </t-table-column>
            <t-table-column title="截止日期" width="15%">
              <template #default="{ row }">
                <span :class="{ overdue: isOverdue(row.dueDate) }">{{ row.dueDate }}</span>
              </template>
            </t-table-column>
            <t-table-column title="状态" width="12%">
              <template #default="{ row }">
                <t-tag :type="getStatusType(row.status)" variant="light">{{ row.statusText }}</t-tag>
              </template>
            </t-table-column>
            <t-table-column title="操作" width="10%" align="center">
              <template #default>
                <t-button variant="text" size="small"><EditIcon /></t-button>
              </template>
            </t-table-column>
          </t-table>
        </div>
      </t-tab-panel>

      <t-tab-panel value="members" label="团队成员">
        <div class="members-content">
          <div class="member-card" v-for="member in members" :key="member.id">
            <div class="member-avatar" :style="{ background: member.color }">{{ member.name.charAt(0) }}</div>
            <div class="member-info">
              <span class="member-name">{{ member.name }}</span>
              <span class="member-role">{{ member.role }}</span>
            </div>
            <t-tag variant="outline">{{ member.taskCount }} 任务</t-tag>
          </div>
        </div>
      </t-tab-panel>

      <t-tab-panel value="files" label="文件">
        <div class="files-content">
          <div class="file-item" v-for="file in files" :key="file.id">
            <div class="file-icon" :style="{ background: file.color }">
              <component :is="file.icon" />
            </div>
            <div class="file-info">
              <span class="file-name">{{ file.name }}</span>
              <span class="file-meta">{{ file.size }} · {{ file.uploader }} · {{ file.date }}</span>
            </div>
            <t-button variant="text" size="small"><DownloadIcon /></t-button>
          </div>
        </div>
      </t-tab-panel>
    </t-tabs>
  </div>
</template>

<script setup>
import { ref } from 'vue'
import { useRoute } from 'vue-router'
import { markRaw } from 'vue'

const route = useRoute()
const activeTab = ref('tasks')

const project = ref({
  id: 1,
  name: 'Website Redesign',
  description: '公司官网重构项目，提升用户体验和视觉效果',
  color: '#2196F3',
  gradient: 'linear-gradient(135deg, #2196F3 0%, #1976D2 100%)',
  progress: 75,
  tasks: 24,
  completed: 18,
  memberCount: 5,
  daysLeft: 15
})

const tasks = ref([
  { id: 1, title: '完成首页UI设计', assignee: '张三', assigneeColor: '#2196F3', priority: 'high', priorityText: '高', dueDate: '2026-04-15', status: 'completed', statusText: '已完成' },
  { id: 2, title: '开发登录注册功能', assignee: '李四', assigneeColor: '#4CAF50', priority: 'high', priorityText: '高', dueDate: '2026-04-18', status: 'in_progress', statusText: '进行中' },
  { id: 3, title: '响应式布局适配', assignee: '王五', assigneeColor: '#FF9800', priority: 'medium', priorityText: '中', dueDate: '2026-04-20', status: 'in_progress', statusText: '进行中' },
  { id: 4, title: 'SEO优化', assignee: '赵六', assigneeColor: '#9C27B0', priority: 'low', priorityText: '低', dueDate: '2026-04-25', status: 'pending', statusText: '待开始' }
])

const members = ref([
  { id: 1, name: '张三', role: '项目经理', color: '#2196F3', taskCount: 8 },
  { id: 2, name: '李四', role: '前端工程师', color: '#4CAF50', taskCount: 6 },
  { id: 3, name: '王五', role: 'UI设计师', color: '#FF9800', taskCount: 5 },
  { id: 4, name: '赵六', role: '后端工程师', color: '#9C27B0', taskCount: 5 }
])

const files = ref([
  { id: 1, name: '项目需求文档.pdf', size: '2.3MB', uploader: '张三', date: '2026-04-10', color: '#EF4444', icon: markRaw(FileIcon) },
  { id: 2, name: 'UI设计稿.fig', size: '15.6MB', uploader: '王五', date: '2026-04-12', color: '#9C27B0', icon: markRaw(ImageIcon) },
  { id: 3, name: '技术方案.docx', size: '1.2MB', uploader: '李四', date: '2026-04-08', color: '#2196F3', icon: markRaw(FileIcon) }
])

const getPriorityType = (p) => ({ high: 'danger', medium: 'warning', low: 'primary' }[p] || 'default')
const getStatusType = (s) => ({ completed: 'success', in_progress: 'primary', pending: 'default' }[s] || 'default')
const isOverdue = (date) => new Date(date) < new Date()

import { AddIcon, SettingIcon, SearchIcon, EditIcon, DownloadIcon, FileIcon, ImageIcon } from 'tdesign-icons-vue-next'
</script>

<style scoped>
.project-detail-page { }
.project-header { padding: 32px; border-radius: var(--radius-xl); margin-bottom: 24px; animation: fadeInUp 0.5s ease; }
.header-content { display: flex; justify-content: space-between; align-items: flex-start; }
.project-info { display: flex; gap: 20px; }
.project-icon { width: 64px; height: 64px; border-radius: var(--radius-xl); background: rgba(255,255,255,0.2); display: flex; align-items: center; justify-content: center; color: white; font-size: 28px; font-weight: 700; backdrop-filter: blur(4px); }
.project-text h1 { font-size: 24px; font-weight: 700; color: white; margin: 0 0 8px 0; }
.project-text p { font-size: 14px; color: rgba(255,255,255,0.8); margin: 0; }
.header-actions { display: flex; gap: 12px; }

.project-stats { display: flex; gap: 32px; padding: 24px; background: var(--bg-container); border-radius: var(--radius-xl); border: 1px solid var(--border-color); margin-bottom: 24px; animation: fadeInUp 0.5s ease 0.1s backwards; }
.stat-item { text-align: center; }
.stat-value { display: block; font-size: 28px; font-weight: 700; color: var(--text-primary); }
.stat-label { font-size: 12px; color: var(--text-tertiary); }
.stat-bar { width: 80px; height: 4px; background: var(--border-color); border-radius: 2px; margin-top: 8px; overflow: hidden; }
.stat-bar-fill { height: 100%; border-radius: 2px; transition: width 0.8s ease; }

.tasks-content, .members-content, .files-content { padding-top: 20px; }
.tasks-filters { display: flex; gap: 12px; margin-bottom: 16px; }

.task-cell { display: flex; align-items: center; gap: 12px; }
.task-cell .completed { text-decoration: line-through; color: var(--text-tertiary); }
.assignee-cell { display: flex; align-items: center; gap: 8px; }
.avatar { width: 28px; height: 28px; border-radius: var(--radius-full); display: flex; align-items: center; justify-content: center; color: white; font-size: 11px; font-weight: 600; }
.overdue { color: var(--error-color); }

.member-card { display: flex; align-items: center; gap: 16px; padding: 16px; background: var(--bg-page); border-radius: var(--radius-lg); margin-bottom: 12px; }
.member-avatar { width: 48px; height: 48px; border-radius: var(--radius-full); display: flex; align-items: center; justify-content: center; color: white; font-size: 18px; font-weight: 600; }
.member-info { flex: 1; }
.member-name { display: block; font-weight: 600; color: var(--text-primary); }
.member-role { font-size: 12px; color: var(--text-tertiary); }

.file-item { display: flex; align-items: center; gap: 16px; padding: 12px; background: var(--bg-page); border-radius: var(--radius-lg); margin-bottom: 8px; }
.file-icon { width: 40px; height: 40px; border-radius: var(--radius-md); display: flex; align-items: center; justify-content: center; color: white; }
.file-info { flex: 1; }
.file-name { display: block; font-weight: 500; color: var(--text-primary); }
.file-meta { font-size: 12px; color: var(--text-tertiary); }
</style>
