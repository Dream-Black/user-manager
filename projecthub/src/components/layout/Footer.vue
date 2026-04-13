<template>
  <footer class="app-footer">
    <div class="footer-content">
      <span class="version">版本 {{ versionInfo.version }}</span>
      <span class="divider">|</span>
      <span class="build-time">更新于 {{ versionInfo.buildDate }}</span>
    </div>
  </footer>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'

interface VersionInfo {
  version: string
  buildTime: string
  buildDate: string
  commit: string
}

const versionInfo = ref<VersionInfo>({
  version: '1.0.0',
  buildTime: '',
  buildDate: '-',
  commit: ''
})

onMounted(async () => {
  try {
    const response = await fetch('/version.json')
    if (response.ok) {
      versionInfo.value = await response.json()
    }
  } catch (error) {
    console.warn('无法加载版本信息:', error)
  }
})
</script>

<style scoped>
.app-footer {
  padding: 12px 24px;
  background: var(--td-bg-color-container);
  border-top: 1px solid var(--td-border-level-1-color);
  text-align: center;
}

.footer-content {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 12px;
  font-size: 12px;
  color: var(--td-text-color-secondary);
}

.version {
  font-weight: 500;
  color: var(--td-brand-color);
}

.divider {
  opacity: 0.5;
}

.build-time {
  opacity: 0.8;
}
</style>
