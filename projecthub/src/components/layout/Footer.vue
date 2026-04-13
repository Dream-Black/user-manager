<template>
  <footer class="app-footer">
    <div class="footer-content">
      <span class="version">版本 {{ versionInfo.version }}</span>
      <span class="divider">|</span>
      <span class="build-time">更新于 {{ formattedDate }}</span>
    </div>
  </footer>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'

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

// 格式化日期为北京时间 (UTC+8)，格式：YYYY-MM-DD HH:mm:ss
const formattedDate = computed(() => {
  const dateStr = versionInfo.value.buildDate || versionInfo.value.buildTime
  if (!dateStr || dateStr === '-') return '-'
  
  const date = new Date(dateStr)
  // 转换为北京时间 (UTC+8)
  const beijingDate = new Date(date.getTime() + 8 * 60 * 60 * 1000)
  const year = beijingDate.getUTCFullYear()
  const month = String(beijingDate.getUTCMonth() + 1).padStart(2, '0')
  const day = String(beijingDate.getUTCDate()).padStart(2, '0')
  const hours = String(beijingDate.getUTCHours()).padStart(2, '0')
  const minutes = String(beijingDate.getUTCMinutes()).padStart(2, '0')
  const seconds = String(beijingDate.getUTCSeconds()).padStart(2, '0')
  return `${year}-${month}-${day} ${hours}:${minutes}:${seconds}`
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
