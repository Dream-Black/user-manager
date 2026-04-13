<template>
  <t-config-provider :globalConfig="globalConfig">
    <div class="app-layout" :class="{ 'sidebar-collapsed': sidebarCollapsed }">
      <Sidebar />
      <div class="app-body">
        <main class="app-main">
          <Header />
          <div class="app-content">
            <router-view v-slot="{ Component, route }">
              <transition name="page" mode="out-in">
                <component :is="Component" :key="route.path" />
              </transition>
            </router-view>
          </div>
        </main>
        <Footer />
      </div>
    </div>
  </t-config-provider>
</template>

<script setup>
import { computed } from 'vue'
import Sidebar from './components/layout/Sidebar.vue'
import Header from './components/layout/Header.vue'
import Footer from './components/layout/Footer.vue'

const globalConfig = {
  theme: 'light'
}
</script>

<style>
.app-layout {
  display: flex;
  min-height: 100vh;
}

.app-body {
  flex: 1;
  display: flex;
  flex-direction: column;
  min-height: 100vh;
  overflow: hidden;
}

.app-main {
  flex: 1;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.app-content {
  flex: 1;
  padding: 24px;
  overflow-y: auto;
  background: var(--td-bg-color-page);
}

/* 页面切换动画 */
.page-enter-active,
.page-leave-active {
  transition: all 0.25s ease-out;
}

.page-enter-from {
  opacity: 0;
  transform: translateY(12px);
}

.page-leave-to {
  opacity: 0;
  transform: translateY(-12px);
}
</style>
