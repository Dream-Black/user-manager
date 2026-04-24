<template>
  <aside class="sidebar" :class="{ collapsed: isCollapsed }">
    <div class="sidebar-header">
      <div class="logo-area" @click="router.push('/')">
        <svg class="logo-icon" viewBox="0 0 32 32" fill="none" aria-hidden="true">
          <rect width="32" height="32" rx="8" fill="url(#sidebarLogoGradient)" />
          <path d="M10 16L14 20L22 12" stroke="white" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round" />
          <defs>
            <linearGradient id="sidebarLogoGradient" x1="0" y1="0" x2="32" y2="32">
              <stop stop-color="#3B82F6" />
              <stop offset="1" stop-color="#60A5FA" />
            </linearGradient>
          </defs>
        </svg>
        <span v-if="!isCollapsed" class="logo-text">AI Claw</span>
      </div>
      <button class="collapse-btn" @click="toggleSidebarCollapsed" :title="isCollapsed ? '展开' : '收起'" :aria-label="isCollapsed ? '展开侧边栏' : '收起侧边栏'">
        <svg v-if="isCollapsed" viewBox="0 0 24 24" fill="none" width="20" height="20" aria-hidden="true">
          <path d="M9 18l6-6-6-6" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" />
        </svg>
        <svg v-else viewBox="0 0 24 24" fill="none" width="20" height="20" aria-hidden="true">
          <path d="M15 18l-6-6 6-6" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" />
        </svg>
      </button>
    </div>

    <div class="user-section">
      <img :src="user?.avatar || '/default-avatar.png'" :alt="user?.name || '用户头像'" class="user-avatar" />
      <div v-if="!isCollapsed" class="user-info">
        <span class="user-name">{{ user?.name || '加载中...' }}</span>
        <span class="user-role">{{ user?.role || user?.department || '用户' }}</span>
      </div>
    </div>

    <nav class="nav-section">
      <div v-for="group in navGroups" :key="group.title" class="nav-group">
        <div v-if="!isCollapsed" class="nav-group-title">{{ group.title }}</div>
        <router-link
          v-for="item in group.items"
          :key="item.path"
          :to="item.path"
          class="nav-link"
          :class="{ active: isActive(item.path) }"
          :title="isCollapsed ? item.title : ''"
        >
          <span class="nav-indicator"></span>
          <component :is="item.icon" class="nav-icon" aria-hidden="true" />
          <span v-if="!isCollapsed" class="nav-text">{{ item.title }}</span>
        </router-link>
      </div>
    </nav>

    <div class="sidebar-footer">
      <router-link to="/settings" class="nav-link" :class="{ active: isActive('/settings') }" :title="isCollapsed ? '设置' : ''">
        <span class="nav-indicator"></span>
        <span class="nav-icon" aria-hidden="true">
          <svg viewBox="0 0 24 24" fill="none" width="20" height="20">
            <path d="M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" />
            <circle cx="12" cy="12" r="3" stroke="currentColor" stroke-width="2" />
          </svg>
        </span>
        <span v-if="!isCollapsed" class="nav-text">设置</span>
      </router-link>
      <button class="nav-link nav-button" @click="handleLogout" :title="isCollapsed ? '退出' : ''" type="button">
        <span class="nav-indicator"></span>
        <span class="nav-icon" aria-hidden="true">
          <svg viewBox="0 0 24 24" fill="none" width="20" height="20">
            <path d="M9 21H5a2 2 0 01-2-2V5a2 2 0 012-2h4M16 17l5-5-5-5M21 12H9" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" />
          </svg>
        </span>
        <span v-if="!isCollapsed" class="nav-text">退出登录</span>
      </button>
    </div>
  </aside>
</template>

<script setup>
import { computed } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import {
  DashboardIcon,
  FolderOpenIcon,
  TaskIcon,
  TimeIcon,
  ChartRingIcon,
  RootListIcon,
  CheckCircleIcon,
  RobotIcon,
} from 'tdesign-icons-vue-next'
import { useLayoutState } from '@/composables/useLayoutState'

const route = useRoute()
const router = useRouter()
const { user, isSidebarCollapsed, toggleSidebarCollapsed } = useLayoutState()

const isCollapsed = computed(() => isSidebarCollapsed.value)

const navGroups = [
  {
    title: '主导航',
    items: [
      { path: '/', title: '仪表盘', icon: DashboardIcon },
      { path: '/projects', title: '项目管理', icon: FolderOpenIcon },
      { path: '/tasks', title: '任务中心', icon: TaskIcon },
      { path: '/timeline', title: '时间线', icon: TimeIcon },
      { path: '/gantt', title: '甘特图', icon: ChartRingIcon },
    ],
  },
  {
    title: '内容管理',
    items: [
      { path: '/resources', title: '资源管理', icon: RootListIcon },
      { path: '/review', title: '复盘总结', icon: CheckCircleIcon },
    ],
  },
  {
    title: '智能助手',
    items: [
      { path: '/ai', title: 'AI 助手', icon: RobotIcon },
    ],
  },
]

const isActive = (path) => (path === '/' ? route.path === '/' : route.path.startsWith(path))
const handleLogout = () => router.push('/login')
</script>

<style scoped>
.sidebar {
  position: fixed;
  left: 0;
  top: 0;
  bottom: 0;
  width: var(--sidebar-width);
  background: var(--gradient-sidebar);
  border-right: 1px solid var(--border-light);
  display: flex;
  flex-direction: column;
  transition: width 0.35s cubic-bezier(0.4, 0, 0.2, 1);
  z-index: 100;
  overflow: hidden;
}

.sidebar.collapsed { width: var(--sidebar-collapsed); }
.sidebar-header { display: flex; align-items: center; justify-content: space-between; padding: var(--space-4) var(--space-4); border-bottom: 1px solid var(--border-light); height: var(--header-height); position: relative; }
.logo-area { display: flex; align-items: center; gap: var(--space-3); cursor: pointer; transition: opacity var(--transition-fast); }
.logo-area:hover { opacity: 0.8; }
.logo-icon { width: 36px; height: 36px; flex-shrink: 0; }
.logo-text { font-size: var(--font-size-lg); font-weight: var(--font-weight-bold); background: var(--gradient-primary); -webkit-background-clip: text; -webkit-text-fill-color: transparent; background-clip: text; white-space: nowrap; }
.collapse-btn { width: 32px; height: 32px; display: flex; align-items: center; justify-content: center; border: none; border-radius: var(--radius-base); background: transparent; color: var(--text-secondary); cursor: pointer; transition: all var(--transition-fast); flex-shrink: 0; }
.collapse-btn:hover { background: var(--primary-lighter); color: var(--primary-color); }
.user-section { display: flex; align-items: center; gap: var(--space-3); padding: var(--space-4); margin: var(--space-3) var(--space-3); border-radius: var(--radius-xl); background: var(--primary-lighter); transition: background var(--transition-fast); }
.user-avatar { width: 40px; height: 40px; border-radius: var(--radius-full); object-fit: cover; flex-shrink: 0; border: 2px solid white; background: var(--gradient-primary); }
.user-info { display: flex; flex-direction: column; overflow: hidden; }
.user-name { font-size: var(--font-size-sm); font-weight: var(--font-weight-semibold); color: var(--text-primary); white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
.user-role { font-size: var(--font-size-xs); color: var(--text-secondary); white-space: nowrap; }
.nav-section { flex: 1; overflow-y: auto; overflow-x: hidden; padding: var(--space-2) var(--space-3); }
.nav-group { margin-bottom: var(--space-4); }
.nav-group-title { font-size: var(--font-size-xs); font-weight: var(--font-weight-semibold); color: var(--text-tertiary); text-transform: uppercase; letter-spacing: 0.05em; padding: var(--space-2) var(--space-3); margin-bottom: var(--space-1); white-space: nowrap; }
.nav-link { position: relative; display: flex; align-items: center; gap: var(--space-3); padding: var(--space-3) var(--space-3); margin-bottom: 2px; border-radius: var(--radius-lg); color: var(--text-secondary); text-decoration: none; cursor: pointer; transition: all var(--transition-fast); white-space: nowrap; width: 100%; border: none; background: transparent; text-align: left; }
.nav-button { font: inherit; }
.nav-indicator { position: absolute; left: -4px; width: 4px; height: 24px; background: var(--gradient-primary); border-radius: 0 var(--radius-sm) var(--radius-sm) 0; opacity: 0; transition: all var(--transition-fast); }
.nav-link:hover { color: var(--primary-color); background: var(--primary-lighter); }
.nav-link.active { color: var(--primary-color); background: var(--primary-lighter); font-weight: var(--font-weight-semibold); box-shadow: 0 2px 8px rgba(59, 130, 246, 0.15); }
.nav-link.active .nav-indicator { opacity: 1; }
.nav-icon { width: 20px; height: 20px; display: inline-flex; align-items: center; justify-content: center; flex-shrink: 0; }
.nav-icon :deep(svg) { width: 20px; height: 20px; }
.nav-text { font-size: var(--font-size-sm); flex: 1; }
.sidebar-footer { padding: var(--space-3); border-top: 1px solid var(--border-light); }
.sidebar.collapsed .sidebar-header { justify-content: center; padding: var(--space-4) var(--space-2); }
.sidebar.collapsed .logo-area { justify-content: center; }
.sidebar.collapsed .collapse-btn { position: absolute; right: -12px; background: var(--bg-card-solid); box-shadow: var(--shadow-sm); border: 1px solid var(--border-light); }
.sidebar.collapsed .user-section { justify-content: center; padding: var(--space-3); margin: var(--space-2); }
.sidebar.collapsed .nav-section { padding: var(--space-2); }
.sidebar.collapsed .nav-link { justify-content: center; padding: var(--space-3); }
.sidebar.collapsed .nav-group-title { display: none; }
.sidebar.collapsed .sidebar-footer { padding: var(--space-2); }
</style>
