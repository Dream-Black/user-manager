<template>
  <t-config-provider :globalConfig="globalConfig">
    <div class="app-layout" :class="{ 'sidebar-collapsed': sidebarCollapsed }">
      <Sidebar />
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
    </div>
  </t-config-provider>
</template>

<script setup>
import { computed } from 'vue'
import Sidebar from './components/layout/Sidebar.vue'
import Header from './components/layout/Header.vue'

const globalConfig = {
  theme: 'light'
}
</script>

<style>
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
