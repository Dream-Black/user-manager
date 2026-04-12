<template>
  <header class="app-header">
    <div class="header-left">
      <h1 class="header-title">{{ pageTitle }}</h1>
    </div>

    <!-- 搜索框 -->
    <div class="header-search">
      <span class="header-search-icon">
        <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
          <circle cx="11" cy="11" r="8"/>
          <path d="m21 21-4.35-4.35"/>
        </svg>
      </span>
      <input
        type="text"
        class="header-search-input"
        placeholder="搜索项目、任务..."
        v-model="searchQuery"
        @focus="handleSearchFocus"
        @blur="handleSearchBlur"
      />
    </div>

    <div class="header-right">
      <!-- 消息通知 -->
      <t-tooltip content="消息通知">
        <div class="header-action" @click="handleNotification">
          <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M18 8A6 6 0 0 0 6 8c0 7-3 9-3 9h18s-3-2-3-9"/>
            <path d="M13.73 21a2 2 0 0 1-3.46 0"/>
          </svg>
          <span class="header-action-badge"></span>
        </div>
      </t-tooltip>

      <!-- 帮助 -->
      <t-tooltip content="帮助文档">
        <div class="header-action">
          <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <circle cx="12" cy="12" r="10"/>
            <path d="M9.09 9a3 3 0 0 1 5.83 1c0 2-3 3-3 3"/>
            <line x1="12" y1="17" x2="12.01" y2="17"/>
          </svg>
        </div>
      </t-tooltip>

      <!-- 全屏 -->
      <t-tooltip :content="isFullscreen ? '退出全屏' : '全屏'">
        <div class="header-action" @click="toggleFullscreen">
          <svg v-if="!isFullscreen" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M8 3H5a2 2 0 0 0-2 2v3m18 0V5a2 2 0 0 0-2-2h-3m0 18h3a2 2 0 0 0 2-2v-3M3 16v3a2 2 0 0 0 2 2h3"/>
          </svg>
          <svg v-else width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M8 3v3a2 2 0 0 1-2 2H3m18 0h-3a2 2 0 0 1-2-2V3m0 18v-3a2 2 0 0 1 2-2h3M3 16h3a2 2 0 0 1 2 2v3"/>
          </svg>
        </div>
      </t-tooltip>

      <!-- 用户头像 -->
      <div class="header-user" @click="handleUserClick">
        <div class="header-avatar">P</div>
        <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
          <polyline points="6 9 12 15 18 9"/>
        </svg>
      </div>
    </div>
  </header>
</template>

<script setup>
import { ref, computed } from 'vue'
import { useRoute } from 'vue-router'

const route = useRoute()
const searchQuery = ref('')
const isFullscreen = ref(false)

const routeTitles = {
  '/': '工作台',
  '/projects': '项目列表',
  '/tasks': '任务管理',
  '/gantt': '甘特图',
  '/timeline': '时间线',
  '/review': '复盘总结',
  '/categories': '分类管理',
  '/ai': 'AI 助手',
  '/settings': '个人设置'
}

const pageTitle = computed(() => {
  return routeTitles[route.path] || 'ProjectHub'
})

const handleSearchFocus = () => {
  document.querySelector('.header-search-input')?.classList.add('focused')
}

const handleSearchBlur = () => {
  document.querySelector('.header-search-input')?.classList.remove('focused')
}

const handleNotification = () => {
  console.log('Notification clicked')
}

const handleUserClick = () => {
  console.log('User clicked')
}

const toggleFullscreen = () => {
  if (!document.fullscreenElement) {
    document.documentElement.requestFullscreen()
    isFullscreen.value = true
  } else {
    document.exitFullscreen()
    isFullscreen.value = false
  }
}
</script>

<style scoped>
/* 头部搜索框样式增强 */
.header-search-input {
  transition: all var(--transition-base);
}

.header-search-input:focus {
  width: 320px;
}

/* 用户下拉动画 */
.header-user {
  transition: all var(--transition-fast);
}

.header-user:hover svg {
  transform: rotate(180deg);
}

.header-user svg {
  transition: transform var(--transition-base);
  color: var(--text-tertiary);
}

/* 操作按钮悬停效果 */
.header-action {
  transition: all var(--transition-fast);
}

.header-action:hover {
  transform: scale(1.05);
}

.header-action svg {
  transition: transform var(--transition-bounce);
}

.header-action:hover svg {
  transform: rotate(10deg);
}
</style>
