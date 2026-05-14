<template>
  <div class="finance-nav">
    <router-link
      v-for="item in navItems"
      :key="item.path"
      :to="item.path"
      class="nav-tab"
      :class="{ active: isActive(item.path) }"
    >
      <i :class="item.icon" />
      <span>{{ item.label }}</span>
    </router-link>
  </div>
</template>

<script setup>
import { useRoute } from 'vue-router'

const route = useRoute()

const navItems = [
  { path: '/finance', label: '概览', icon: 'i-lucide-wallet' },
  { path: '/finance/expenses', label: '支出', icon: 'i-lucide-minus-circle' },
  { path: '/finance/income', label: '收入', icon: 'i-lucide-plus-circle' },
  { path: '/finance/accounts', label: '账户', icon: 'i-lucide-landmark' },
  { path: '/finance/stats', label: '统计', icon: 'i-lucide-bar-chart-3' },
  { path: '/finance/income/salary', label: '工资', icon: 'i-lucide-calculator' },
]

const isActive = (path) => {
  if (path === '/finance') return route.path === '/finance'
  return route.path.startsWith(path)
}
</script>

<style scoped>
.finance-nav {
  display: flex;
  gap: 4px;
  margin-bottom: var(--space-6);
  padding: var(--space-1);
  background: var(--bg-secondary, #f8fafc);
  border-radius: var(--radius-lg);
  overflow-x: auto;
}

.nav-tab {
  display: inline-flex;
  align-items: center;
  gap: var(--space-2);
  padding: var(--space-2) var(--space-4);
  border-radius: var(--radius-md);
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  color: var(--text-secondary);
  text-decoration: none;
  white-space: nowrap;
  transition: all var(--transition-fast);
}

.nav-tab:hover {
  background: var(--primary-lighter, #eff6ff);
  color: var(--primary-color, #3b82f6);
}

.nav-tab.active {
  background: var(--primary-color, #3b82f6);
  color: white;
  box-shadow: var(--shadow-sm);
}

.nav-tab i {
  font-size: 16px;
}
</style>
