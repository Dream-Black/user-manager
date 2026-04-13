<template>
  <div class="app-layout">
    <Sidebar v-if="showSidebar" />
    <div class="main-wrapper" :class="{ 'sidebar-collapsed': isSidebarCollapsed }">
      <Header v-if="showHeader" />
      <main class="main-content">
        <router-view v-slot="{ Component }">
          <transition name="fade" mode="out-in">
            <component :is="Component" />
          </transition>
        </router-view>
      </main>
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import Sidebar from './components/layout/Sidebar.vue'
import Header from './components/layout/Header.vue'

const route = useRoute()

const showSidebar = computed(() => {
  // 登录页等特殊页面不显示侧边栏
  const noSidebarRoutes = ['/login', '/register', '/404']
  return !noSidebarRoutes.includes(route.path)
})

const showHeader = computed(() => {
  const noHeaderRoutes = ['/login', '/register', '/404']
  return !noHeaderRoutes.includes(route.path)
})

const isSidebarCollapsed = computed(() => {
  return localStorage.getItem('sidebarCollapsed') === 'true'
})
</script>

<style>
/* 全局样式 */
.app-layout {
  display: flex;
  min-height: 100vh;
  background: var(--bg-primary);
}

.main-wrapper {
  flex: 1;
  display: flex;
  flex-direction: column;
  min-height: 100vh;
  margin-left: var(--sidebar-width);
  transition: margin-left 0.35s cubic-bezier(0.4, 0, 0.2, 1);
}

.main-wrapper.sidebar-collapsed {
  margin-left: var(--sidebar-collapsed);
}

.main-content {
  flex: 1;
  padding: 0;
  margin-top: var(--header-height);
  overflow-x: hidden;
}

/* 页面切换动画 */
.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.3s ease, transform 0.3s ease;
}

.fade-enter-from {
  opacity: 0;
  transform: translateY(10px);
}

.fade-leave-to {
  opacity: 0;
  transform: translateY(-10px);
}

/* 滚动条 */
::-webkit-scrollbar {
  width: 6px;
  height: 6px;
}

::-webkit-scrollbar-track {
  background: transparent;
}

::-webkit-scrollbar-thumb {
  background: var(--primary-light);
  border-radius: 3px;
}

::-webkit-scrollbar-thumb:hover {
  background: var(--primary-color);
}

/* 选中文字 */
::selection {
  background: var(--primary-light);
  color: var(--primary-dark);
}
</style>
