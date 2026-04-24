<template>
  <div class="app-layout" :class="{ 'app-layout--blank': !showChrome }">
    <Sidebar v-if="showChrome" />
    <div class="main-wrapper" :class="{ 'sidebar-collapsed': showChrome && isSidebarCollapsed, 'main-wrapper--blank': !showChrome }">
      <Header v-if="showChrome" />
      <main class="main-content">
        <div class="page-shell">
          <router-view v-slot="{ Component }">
            <transition name="fade" mode="out-in">
              <component :is="Component" />
            </transition>
          </router-view>
        </div>
      </main>
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import Sidebar from './components/layout/Sidebar.vue'
import Header from './components/layout/Header.vue'
import { initLayoutState, useLayoutState } from './composables/useLayoutState'

initLayoutState()

const route = useRoute()
const { isSidebarCollapsed } = useLayoutState()

const noChromeRouteNames = new Set(['Login', 'Register'])
const noChromeRoutePaths = new Set(['/login', '/register', '/404'])

const showChrome = computed(() => {
  return !(noChromeRouteNames.has(route.name) || noChromeRoutePaths.has(route.path) || route.meta?.layout === 'blank')
})
</script>

<style>
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

.main-wrapper--blank {
  margin-left: 0;
}

.main-content {
  flex: 1;
  padding: 0;
  margin-top: var(--header-height);
  overflow-x: hidden;
}

.page-shell {
  width: 100%;
  max-width: 1440px;
  margin: 0 auto;
  padding: 24px;
  box-sizing: border-box;
}

.app-layout--blank .main-content {
  margin-top: 0;
}

.app-layout--blank .page-shell {
  max-width: none;
  min-height: 100vh;
  padding: 0;
}

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

::-webkit-scrollbar { width: 6px; height: 6px; }
::-webkit-scrollbar-track { background: transparent; }
::-webkit-scrollbar-thumb { background: var(--primary-light); border-radius: 3px; }
::-webkit-scrollbar-thumb:hover { background: var(--primary-color); }
::selection { background: var(--primary-light); color: var(--primary-dark); }
</style>
