<template>
  <header class="header">
    <div class="header-left">
      <div class="logo">
        <svg class="logo-icon" viewBox="0 0 32 32" fill="none" aria-hidden="true">
          <rect width="32" height="32" rx="8" fill="url(#logoGradient)" />
          <path d="M10 16L14 20L22 12" stroke="white" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round" />
          <defs>
            <linearGradient id="logoGradient" x1="0" y1="0" x2="32" y2="32">
              <stop stop-color="#3B82F6" />
              <stop offset="1" stop-color="#60A5FA" />
            </linearGradient>
          </defs>
        </svg>
        <span class="logo-text">AI Claw</span>
      </div>

      <div class="search-wrapper">
        <svg class="search-icon" viewBox="0 0 24 24" fill="none" aria-hidden="true">
          <path d="M21 21L15 15M17 10C17 13.866 13.866 17 10 17C6.13401 17 3 13.866 3 10C3 6.13401 6.13401 3 10 3C13.866 3 17 6.13401 17 10Z" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" />
        </svg>
        <input v-model="searchQuery" type="text" class="search-input" placeholder="搜索项目、任务..." />
        <span class="search-shortcut">⌘K</span>
      </div>
    </div>

    <div class="header-right">
      <button class="icon-btn" title="新建" @click="router.push('/projects/new')" type="button">
        <svg viewBox="0 0 24 24" fill="none" width="20" height="20" aria-hidden="true">
          <path d="M12 5V19M5 12H19" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" />
        </svg>
      </button>

      <button class="icon-btn notification-btn" title="通知" @click="showNotifications = !showNotifications" type="button">
        <svg viewBox="0 0 24 24" fill="none" width="20" height="20" aria-hidden="true">
          <path d="M18 8A6 6 0 1 0 6 8c0 7-3 9-3 9h18s-3-2-3-9M13.73 21a2 2 0 0 1-3.46 0" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" />
        </svg>
        <span v-if="unreadCount > 0" class="notification-badge">{{ unreadCount }}</span>
      </button>

      <div ref="userDropdownRef" class="user-dropdown" @click="showUserMenu = !showUserMenu">
        <img :src="user?.avatar || '/default-avatar.png'" :alt="user?.name || '用户头像'" class="user-avatar" />
        <div class="user-info">
          <span class="user-name">{{ user?.name || '加载中...' }}</span>
          <span class="user-role">{{ user?.role || user?.department || '用户' }}</span>
        </div>
        <svg class="dropdown-arrow" viewBox="0 0 24 24" fill="none" width="16" height="16" aria-hidden="true">
          <path d="M6 9l6 6 6-6" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" />
        </svg>

        <div v-if="showUserMenu" ref="userMenuRef" class="dropdown-menu">
          <div class="dropdown-header">
            <img :src="user?.avatar || '/default-avatar.png'" :alt="user?.name || '用户头像'" class="user-avatar-lg" />
            <div>
              <div class="dropdown-user-name">{{ user?.name || '访客用户' }}</div>
              <div class="dropdown-user-email">{{ user?.email || '' }}</div>
            </div>
          </div>
          <div class="dropdown-divider"></div>
          <button class="dropdown-item" type="button" @click="router.push('/settings')">
            <svg viewBox="0 0 24 24" fill="none" width="16" height="16" aria-hidden="true">
              <path d="M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" />
              <circle cx="12" cy="12" r="3" stroke="currentColor" stroke-width="2" />
            </svg>
            设置
          </button>
          <button class="dropdown-item" type="button" @click="router.push('/profile')">
            <svg viewBox="0 0 24 24" fill="none" width="16" height="16" aria-hidden="true">
              <path d="M20 21v-2a4 4 0 00-4-4H8a4 4 0 00-4 4v2M12 11a4 4 0 100-8 4 4 0 000 8z" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" />
            </svg>
            个人资料
          </button>
          <div class="dropdown-divider"></div>
          <button class="dropdown-item logout" type="button" @click="handleLogout">
            <svg viewBox="0 0 24 24" fill="none" width="16" height="16" aria-hidden="true">
              <path d="M9 21H5a2 2 0 01-2-2V5a2 2 0 012-2h4M16 17l5-5-5-5M21 12H9" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" />
            </svg>
            退出登录
          </button>
        </div>
      </div>
    </div>

    <div v-if="showNotifications" ref="notificationsPanelRef" class="notifications-panel">
      <div class="panel-header">
        <h3>通知</h3>
        <button class="mark-read-btn" @click="markAllRead" type="button">全部已读</button>
      </div>
      <div class="notifications-list">
        <div v-for="notification in notifications" :key="notification.id" class="notification-item" :class="{ unread: !notification.read }">
          <div class="notification-icon" :class="notification.type">
            <svg v-if="notification.type === 'project'" viewBox="0 0 24 24" fill="none" width="18" height="18" aria-hidden="true">
              <path d="M3 7v10a2 2 0 002 2h14a2 2 0 002-2V9a2 2 0 00-2-2h-6l-2-2H5a2 2 0 00-2 2z" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" />
            </svg>
            <svg v-else-if="notification.type === 'task'" viewBox="0 0 24 24" fill="none" width="18" height="18" aria-hidden="true">
              <path d="M9 11l3 3L22 4M21 12v7a2 2 0 01-2 2H5a2 2 0 01-2-2V5a2 2 0 012-2h11" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" />
            </svg>
            <svg v-else viewBox="0 0 24 24" fill="none" width="18" height="18" aria-hidden="true">
              <path d="M18 8A6 6 0 006 8c0 7-3 9-3 9h18s-3-2-3-9M13.73 21a2 2 0 01-3.46 0" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" />
            </svg>
          </div>
          <div class="notification-content">
            <p class="notification-text">{{ notification.message }}</p>
            <span class="notification-time">{{ notification.time }}</span>
          </div>
        </div>
        <div v-if="notifications.length === 0" class="empty-notifications">
          <svg viewBox="0 0 24 24" fill="none" width="32" height="32" aria-hidden="true">
            <path d="M18 8A6 6 0 006 8c0 7-3 9-3 9h18s-3-2-3-9M13.73 21a2 2 0 01-3.46 0" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" />
          </svg>
          <p>暂无通知</p>
        </div>
      </div>
    </div>
  </header>
</template>

<script setup>
import { ref, computed, onMounted, onBeforeUnmount } from 'vue'
import { useRouter } from 'vue-router'
import { useLayoutState } from '@/composables/useLayoutState'

const router = useRouter()
const { user } = useLayoutState()

const searchQuery = ref('')
const showNotifications = ref(false)
const showUserMenu = ref(false)
const userDropdownRef = ref(null)
const userMenuRef = ref(null)
const notificationsPanelRef = ref(null)

const notifications = ref([
  { id: 1, type: 'project', message: '新项目 "AI助手" 已被创建', time: '5分钟前', read: false },
  { id: 2, type: 'task', message: '任务 "设计登录页面" 已完成', time: '1小时前', read: false },
  { id: 3, type: 'system', message: '系统将于今晚23:00进行维护', time: '2小时前', read: true },
])

const unreadCount = computed(() => notifications.value.filter((n) => !n.read).length)
const markAllRead = () => notifications.value.forEach((n) => { n.read = true })
const handleLogout = () => router.push('/login')

const handleDocumentClick = (event) => {
  const target = event.target
  if (userDropdownRef.value && !userDropdownRef.value.contains(target)) {
    showUserMenu.value = false
  }
  if (notificationsPanelRef.value && !notificationsPanelRef.value.contains(target)) {
    if (!target.closest?.('.notification-btn')) {
      showNotifications.value = false
    }
  }
}

onMounted(() => {
  document.addEventListener('click', handleDocumentClick)
})

onBeforeUnmount(() => {
  document.removeEventListener('click', handleDocumentClick)
})
</script>

<style scoped>
.header { position: sticky; top: 0; z-index: 100; display: flex; align-items: center; justify-content: space-between; gap: var(--space-6); padding: 0 var(--space-6); background: var(--bg-card-solid); border-bottom: 1px solid var(--border-light); backdrop-filter: blur(20px); height: var(--header-height); box-shadow: var(--shadow-sm); }
.header-left { display: flex; align-items: center; gap: var(--space-6); flex: 1; min-width: 0; }
.logo { display: flex; align-items: center; gap: var(--space-3); text-decoration: none; flex-shrink: 0; }
.logo-icon { width: 36px; height: 36px; flex-shrink: 0; }
.logo-text { font-size: var(--font-size-lg); font-weight: var(--font-weight-bold); background: var(--gradient-primary); -webkit-background-clip: text; -webkit-text-fill-color: transparent; background-clip: text; white-space: nowrap; }
.search-wrapper { position: relative; display: flex; align-items: center; max-width: 400px; flex: 1; min-width: 0; }
.search-icon { position: absolute; left: 14px; width: 18px; height: 18px; color: var(--text-tertiary); pointer-events: none; transition: color var(--transition-fast); }
.search-input { width: 100%; padding: var(--space-3) var(--space-4); padding-left: 42px; padding-right: 60px; font-size: var(--font-size-sm); border: 1px solid var(--border-color); border-radius: var(--radius-xl); background: var(--bg-color-secondary); color: var(--text-primary); transition: all var(--transition-fast); }
.search-input::placeholder { color: var(--text-tertiary); }
.search-input:hover, .search-input:focus { border-color: var(--primary-color); background: white; }
.search-input:focus { outline: none; box-shadow: 0 0 0 3px rgba(59, 130, 246, 0.1); }
.search-wrapper:focus-within .search-icon { color: var(--primary-color); }
.search-shortcut { position: absolute; right: 12px; padding: 4px 8px; font-size: var(--font-size-xs); font-weight: 500; color: var(--text-tertiary); background: var(--bg-color-secondary); border: 1px solid var(--border-color); border-radius: var(--radius-sm); }
.header-right { display: flex; align-items: center; gap: var(--space-2); flex-shrink: 0; }
.icon-btn { position: relative; width: 40px; height: 40px; display: flex; align-items: center; justify-content: center; border: none; border-radius: var(--radius-lg); background: transparent; color: var(--text-secondary); cursor: pointer; transition: all var(--transition-fast); }
.icon-btn:hover { background: var(--primary-lighter); color: var(--primary-color); }
.notification-badge { position: absolute; top: 6px; right: 6px; min-width: 18px; height: 18px; padding: 0 5px; font-size: 11px; font-weight: 600; color: white; background: var(--error-color); border-radius: var(--radius-full); display: flex; align-items: center; justify-content: center; animation: badgePulse 2s ease-in-out infinite; }
.user-dropdown { position: relative; display: flex; align-items: center; gap: var(--space-3); padding: var(--space-2) var(--space-3); margin-left: var(--space-2); border-radius: var(--radius-xl); cursor: pointer; transition: all var(--transition-fast); }
.user-dropdown:hover { background: var(--primary-lighter); }
.user-avatar { width: 36px; height: 36px; border-radius: var(--radius-full); object-fit: cover; background: var(--gradient-primary); flex-shrink: 0; border: 2px solid var(--primary-light); transition: border-color var(--transition-fast); }
.user-dropdown:hover .user-avatar { border-color: var(--primary-color); }
.user-info { display: flex; flex-direction: column; min-width: 0; }
.user-name { font-size: var(--font-size-sm); font-weight: var(--font-weight-medium); color: var(--text-primary); line-height: 1.2; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
.user-role { font-size: var(--font-size-xs); color: var(--text-tertiary); line-height: 1.2; white-space: nowrap; }
.dropdown-arrow { width: 16px; height: 16px; color: var(--text-tertiary); transition: transform var(--transition-fast); }
.user-dropdown:hover .dropdown-arrow { transform: rotate(180deg); color: var(--primary-color); }
.dropdown-menu { position: absolute; top: calc(100% + 8px); right: 0; width: 240px; background: var(--bg-card-solid); border: 1px solid var(--border-light); border-radius: var(--radius-xl); box-shadow: var(--shadow-xl); animation: fadeInDown 0.2s ease; z-index: 200; overflow: hidden; }
.dropdown-header { display: flex; align-items: center; gap: var(--space-3); padding: var(--space-4); }
.user-avatar-lg { width: 44px; height: 44px; border-radius: var(--radius-full); object-fit: cover; background: var(--gradient-primary); border: 2px solid var(--primary-light); }
.dropdown-user-name { font-size: var(--font-size-sm); font-weight: var(--font-weight-semibold); color: var(--text-primary); }
.dropdown-user-email { font-size: var(--font-size-xs); color: var(--text-tertiary); }
.dropdown-divider { height: 1px; background: var(--border-color); margin: var(--space-2) 0; }
.dropdown-item { width: 100%; display: flex; align-items: center; gap: var(--space-3); padding: var(--space-3) var(--space-4); font-size: var(--font-size-sm); color: var(--text-secondary); cursor: pointer; transition: all var(--transition-fast); text-decoration: none; background: transparent; border: none; text-align: left; }
.dropdown-item:hover { background: var(--primary-lighter); color: var(--primary-color); }
.dropdown-item.logout:hover { background: var(--error-lighter); color: var(--error-color); }
.notifications-panel { position: absolute; top: calc(100% + 8px); right: 100px; width: 360px; background: var(--bg-card-solid); border: 1px solid var(--border-light); border-radius: var(--radius-xl); box-shadow: var(--shadow-xl); animation: fadeInDown 0.2s ease; z-index: 200; overflow: hidden; }
.panel-header { display: flex; align-items: center; justify-content: space-between; padding: var(--space-4) var(--space-5); border-bottom: 1px solid var(--border-color); }
.panel-header h3 { font-size: var(--font-size-base); font-weight: var(--font-weight-semibold); color: var(--text-primary); }
.mark-read-btn { font-size: var(--font-size-xs); color: var(--primary-color); background: none; border: none; cursor: pointer; transition: color var(--transition-fast); }
.mark-read-btn:hover { color: var(--primary-hover); text-decoration: underline; }
.notifications-list { max-height: 400px; overflow-y: auto; }
.notification-item { display: flex; align-items: flex-start; gap: var(--space-3); padding: var(--space-4) var(--space-5); transition: background var(--transition-fast); }
.notification-item:hover { background: var(--bg-hover); }
.notification-item.unread { background: var(--primary-lighter); }
.notification-icon { width: 40px; height: 40px; border-radius: var(--radius-lg); display: flex; align-items: center; justify-content: center; flex-shrink: 0; }
.notification-icon.project { background: var(--primary-lighter); color: var(--primary-color); }
.notification-icon.task { background: var(--success-lighter); color: var(--success-color); }
.notification-icon.system { background: var(--warning-lighter); color: var(--warning-color); }
.notification-content { flex: 1; min-width: 0; }
.notification-text { font-size: var(--font-size-sm); color: var(--text-primary); line-height: 1.5; margin-bottom: var(--space-1); }
.notification-time { font-size: var(--font-size-xs); color: var(--text-tertiary); }
.empty-notifications { display: flex; flex-direction: column; align-items: center; justify-content: center; padding: var(--space-10); color: var(--text-tertiary); }
.empty-notifications svg { margin-bottom: var(--space-3); opacity: 0.5; }

@media (max-width: 1024px) {
  .logo-text,
  .user-info,
  .search-shortcut { display: none; }
  .search-wrapper { max-width: 280px; }
  .notifications-panel { right: 0; width: min(360px, calc(100vw - 24px)); }
}

@media (max-width: 768px) {
  .header { padding: 0 var(--space-3); gap: var(--space-3); }
  .search-wrapper { display: none; }
  .notifications-panel { width: calc(100vw - 24px); }
}
</style>
