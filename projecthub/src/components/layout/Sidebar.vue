<template>
  <aside class="sidebar" :class="{ collapsed: isCollapsed }">
    <!-- Logo区域 -->
    <div class="sidebar-header">
      <div class="logo-area" @click="$router.push('/')">
        <svg class="logo-icon" viewBox="0 0 32 32" fill="none">
          <rect width="32" height="32" rx="8" fill="url(#sidebarLogoGradient)"/>
          <path d="M10 16L14 20L22 12" stroke="white" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"/>
          <defs>
            <linearGradient id="sidebarLogoGradient" x1="0" y1="0" x2="32" y2="32">
              <stop stop-color="#3B82F6"/>
              <stop offset="1" stop-color="#60A5FA"/>
            </linearGradient>
          </defs>
        </svg>
        <span v-if="!isCollapsed" class="logo-text">AI Claw</span>
      </div>
      <button class="collapse-btn" @click="toggleCollapse" :title="isCollapsed ? '展开' : '收起'">
        <svg v-if="isCollapsed" viewBox="0 0 24 24" fill="none" width="20" height="20">
          <path d="M9 18l6-6-6-6" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
        </svg>
        <svg v-else viewBox="0 0 24 24" fill="none" width="20" height="20">
          <path d="M15 18l-6-6 6-6" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
        </svg>
      </button>
    </div>

    <!-- 用户信息 -->
    <div class="user-section">
      <img :src="user?.avatar || ''" :alt="user?.name" class="user-avatar" />
      <div v-if="!isCollapsed" class="user-info">
        <span class="user-name">{{ user?.name || '访客' }}</span>
        <span class="user-role">{{ user?.role || '管理员' }}</span>
      </div>
    </div>

    <!-- 导航菜单 -->
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
          <span class="nav-icon" v-html="item.icon"></span>
          <span v-if="!isCollapsed" class="nav-text">{{ item.title }}</span>
          <span v-if="!isCollapsed && item.badge" class="nav-badge">{{ item.badge }}</span>
        </router-link>
      </div>
    </nav>

    <!-- 底部操作 -->
    <div class="sidebar-footer">
      <router-link to="/settings" class="nav-link" :class="{ active: isActive('/settings') }" :title="isCollapsed ? '设置' : ''">
        <span class="nav-indicator"></span>
        <span class="nav-icon">
          <svg viewBox="0 0 24 24" fill="none" width="20" height="20">
            <path d="M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
            <circle cx="12" cy="12" r="3" stroke="currentColor" stroke-width="2"/>
          </svg>
        </span>
        <span v-if="!isCollapsed" class="nav-text">设置</span>
      </router-link>
      <a class="nav-link" @click="handleLogout" :title="isCollapsed ? '退出' : ''">
        <span class="nav-indicator"></span>
        <span class="nav-icon">
          <svg viewBox="0 0 24 24" fill="none" width="20" height="20">
            <path d="M9 21H5a2 2 0 01-2-2V5a2 2 0 012-2h4M16 17l5-5-5-5M21 12H9" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
          </svg>
        </span>
        <span v-if="!isCollapsed" class="nav-text">退出登录</span>
      </a>
    </div>
  </aside>
</template>

<script setup>
import { ref, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'

const route = useRoute()
const router = useRouter()

const isCollapsed = ref(false)

const user = ref({
  name: '张三',
  role: '产品经理',
  avatar: ''
})

const navGroups = ref([
  {
    title: '主导航',
    items: [
      { 
        path: '/', 
        title: '仪表盘', 
        icon: '<svg viewBox="0 0 24 24" fill="none" width="20" height="20"><rect x="3" y="3" width="7" height="7" rx="1" stroke="currentColor" stroke-width="2"/><rect x="14" y="3" width="7" height="7" rx="1" stroke="currentColor" stroke-width="2"/><rect x="14" y="14" width="7" height="7" rx="1" stroke="currentColor" stroke-width="2"/><rect x="3" y="14" width="7" height="7" rx="1" stroke="currentColor" stroke-width="2"/></svg>'
      },
      { 
        path: '/projects', 
        title: '项目管理', 
        icon: '<svg viewBox="0 0 24 24" fill="none" width="20" height="20"><path d="M3 7v10a2 2 0 002 2h14a2 2 0 002-2V9a2 2 0 00-2-2h-6l-2-2H5a2 2 0 00-2 2z" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/></svg>',
        badge: '8'
      },
      { 
        path: '/tasks', 
        title: '任务中心', 
        icon: '<svg viewBox="0 0 24 24" fill="none" width="20" height="20"><path d="M9 11l3 3L22 4M21 12v7a2 2 0 01-2 2H5a2 2 0 01-2-2V5a2 2 0 012-2h11" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/></svg>',
        badge: '12'
      },
      { 
        path: '/timeline', 
        title: '时间线', 
        icon: '<svg viewBox="0 0 24 24" fill="none" width="20" height="20"><path d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/></svg>'
      },
      { 
        path: '/gantt', 
        title: '甘特图', 
        icon: '<svg viewBox="0 0 24 24" fill="none" width="20" height="20"><path d="M8 6h10M8 12h10M8 18h6M4 6h.01M4 12h.01M4 18h.01" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/></svg>'
      },
    ]
  },
  {
    title: '内容管理',
    items: [
      { 
        path: '/categories', 
        title: '分类管理', 
        icon: '<svg viewBox="0 0 24 24" fill="none" width="20" height="20"><path d="M4 6h16M4 12h16M4 18h16" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/></svg>'
      },
      { 
        path: '/review', 
        title: '审核中心', 
        icon: '<svg viewBox="0 0 24 24" fill="none" width="20" height="20"><path d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/></svg>',
        badge: '3'
      },
    ]
  },
  {
    title: '智能助手',
    items: [
      { 
        path: '/ai', 
        title: 'AI 助手', 
        icon: '<svg viewBox="0 0 24 24" fill="none" width="20" height="20"><path d="M12 2a10 10 0 100 20 10 10 0 000-20zm0 2a8 8 0 110 16 8 8 0 010-16zm-1 5v6l4 2" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/></svg>'
      },
    ]
  }
])

const toggleCollapse = () => {
  isCollapsed.value = !isCollapsed.value
  localStorage.setItem('sidebarCollapsed', isCollapsed.value)
}

const isActive = (path) => {
  if (path === '/') {
    return route.path === '/'
  }
  return route.path.startsWith(path)
}

const handleLogout = () => {
  // 实际项目中调用登出API
  router.push('/login')
}

// 恢复折叠状态
const savedCollapsed = localStorage.getItem('sidebarCollapsed')
if (savedCollapsed !== null) {
  isCollapsed.value = savedCollapsed === 'true'
}
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
  transition: all 0.35s cubic-bezier(0.4, 0, 0.2, 1);
  z-index: 100;
  overflow: hidden;
}

.sidebar.collapsed {
  width: var(--sidebar-collapsed);
}

/* 头部 */
.sidebar-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: var(--space-4) var(--space-4);
  border-bottom: 1px solid var(--border-light);
  height: var(--header-height);
}

.logo-area {
  display: flex;
  align-items: center;
  gap: var(--space-3);
  cursor: pointer;
  transition: opacity var(--transition-fast);
}

.logo-area:hover {
  opacity: 0.8;
}

.logo-icon {
  width: 36px;
  height: 36px;
  flex-shrink: 0;
}

.logo-text {
  font-size: var(--font-size-lg);
  font-weight: var(--font-weight-bold);
  background: var(--gradient-primary);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
  white-space: nowrap;
}

.collapse-btn {
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
  flex-shrink: 0;
}

.collapse-btn:hover {
  background: var(--primary-lighter);
  color: var(--primary-color);
}

/* 用户区域 */
.user-section {
  display: flex;
  align-items: center;
  gap: var(--space-3);
  padding: var(--space-4);
  margin: var(--space-3) var(--space-3);
  border-radius: var(--radius-xl);
  background: var(--primary-lighter);
  transition: background var(--transition-fast);
}

.user-section:hover {
  background: var(--primary-light);
}

.user-avatar {
  width: 40px;
  height: 40px;
  border-radius: var(--radius-full);
  background: var(--gradient-primary);
  display: flex;
  align-items: center;
  justify-content: center;
  color: white;
  font-weight: 600;
  font-size: 14px;
  flex-shrink: 0;
  border: 2px solid white;
}

.user-info {
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.user-name {
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-semibold);
  color: var(--text-primary);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.user-role {
  font-size: var(--font-size-xs);
  color: var(--text-secondary);
  white-space: nowrap;
}

/* 导航 */
.nav-section {
  flex: 1;
  overflow-y: auto;
  overflow-x: hidden;
  padding: var(--space-2) var(--space-3);
}

.nav-group {
  margin-bottom: var(--space-4);
}

.nav-group-title {
  font-size: var(--font-size-xs);
  font-weight: var(--font-weight-semibold);
  color: var(--text-tertiary);
  text-transform: uppercase;
  letter-spacing: 0.05em;
  padding: var(--space-2) var(--space-3);
  margin-bottom: var(--space-1);
  white-space: nowrap;
}

.nav-link {
  position: relative;
  display: flex;
  align-items: center;
  gap: var(--space-3);
  padding: var(--space-3) var(--space-3);
  margin-bottom: 2px;
  border-radius: var(--radius-lg);
  color: var(--text-secondary);
  text-decoration: none;
  cursor: pointer;
  transition: all var(--transition-fast);
  white-space: nowrap;
}

.nav-indicator {
  position: absolute;
  left: -4px;
  width: 4px;
  height: 24px;
  background: var(--gradient-primary);
  border-radius: 0 var(--radius-sm) var(--radius-sm) 0;
  opacity: 0;
  transition: all var(--transition-fast);
}

.nav-link:hover {
  color: var(--primary-color);
  background: var(--primary-lighter);
}

.nav-link.active {
  color: var(--primary-color);
  background: var(--primary-lighter);
  font-weight: var(--font-weight-semibold);
  box-shadow: 0 2px 8px rgba(59, 130, 246, 0.15);
}

.nav-link.active .nav-indicator {
  opacity: 1;
}

.nav-icon {
  width: 20px;
  height: 20px;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

.nav-text {
  font-size: var(--font-size-sm);
  flex: 1;
}

.nav-badge {
  font-size: var(--font-size-xs);
  font-weight: var(--font-weight-semibold);
  padding: 2px 8px;
  border-radius: var(--radius-full);
  background: var(--gradient-primary);
  color: white;
  animation: badgePulse 2s ease-in-out infinite;
}

/* 底部 */
.sidebar-footer {
  padding: var(--space-3);
  border-top: 1px solid var(--border-light);
}

/* 折叠状态下 */
.sidebar.collapsed .sidebar-header {
  justify-content: center;
  padding: var(--space-4) var(--space-2);
}

.sidebar.collapsed .logo-area {
  justify-content: center;
}

.sidebar.collapsed .collapse-btn {
  position: absolute;
  right: -12px;
  background: var(--bg-card-solid);
  box-shadow: var(--shadow-sm);
  border: 1px solid var(--border-light);
}

.sidebar.collapsed .user-section {
  justify-content: center;
  padding: var(--space-3);
  margin: var(--space-2);
}

.sidebar.collapsed .nav-section {
  padding: var(--space-2);
}

.sidebar.collapsed .nav-link {
  justify-content: center;
  padding: var(--space-3);
}

.sidebar.collapsed .nav-group-title {
  display: none;
}

.sidebar.collapsed .sidebar-footer {
  padding: var(--space-2);
}
</style>
