<template>
  <div class="timeline-page">
    <div class="page-header fade-in">
      <h2 class="page-title">时间线</h2>
      <span class="page-subtitle">项目活动记录</span>
    </div>
    
    <div class="timeline-container">
      <div v-for="(item, index) in timeline" :key="item.id" 
           class="timeline-item fade-in-up"
           :style="{ animationDelay: `${0.1 * index}s` }">
        <div class="timeline-dot" :style="{ background: item.color }"></div>
        <div class="timeline-content card">
          <div class="timeline-header">
            <span class="timeline-action">{{ item.action }}</span>
            <span class="timeline-time">{{ formatTime(item.createdAt) }}</span>
          </div>
          <p class="timeline-desc">{{ item.details }}</p>
        </div>
      </div>
      
      <div v-if="timeline.length === 0" class="empty-state fade-in">
        <svg width="64" height="64" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
          <circle cx="12" cy="12" r="10"/><polyline points="12 6 12 12 16 14"/>
        </svg>
        <p>暂无活动记录</p>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import dayjs from 'dayjs'
import relativeTime from 'dayjs/plugin/relativeTime'
dayjs.extend(relativeTime)

const timeline = ref([])

const fetchTimeline = async () => {
  const res = await fetch('/api/timelines/recent?limit=20')
  if (res.ok) timeline.value = await res.json()
}

const formatTime = (date) => dayjs(date).fromNow()
const colors = ['#2563EB', '#10B981', '#F59E0B', '#EF4444', '#8B5CF6']
const getColor = (i) => colors[i % colors.length]

onMounted(fetchTimeline)
</script>

<style scoped>
.timeline-page { animation: fadeIn 0.3s ease-out; }
.page-header { margin-bottom: var(--space-6); }
.page-title { font-size: var(--font-size-2xl); font-weight: var(--font-weight-semibold); margin-bottom: var(--space-1); }
.page-subtitle { font-size: var(--font-size-sm); color: var(--text-tertiary); }
.timeline-container { position: relative; padding-left: 40px; }
.timeline-container::before { content: ''; position: absolute; left: 15px; top: 0; bottom: 0; width: 2px; background: var(--border-light); }
.timeline-item { position: relative; margin-bottom: var(--space-4); opacity: 0; }
.timeline-dot { position: absolute; left: -33px; top: 20px; width: 12px; height: 12px; border-radius: 50%; border: 3px solid var(--bg-primary); box-shadow: 0 0 0 2px currentColor; }
.timeline-content { padding: var(--space-4); }
.timeline-header { display: flex; justify-content: space-between; margin-bottom: var(--space-2); }
.timeline-action { font-weight: var(--font-weight-medium); color: var(--text-primary); }
.timeline-time { font-size: var(--font-size-xs); color: var(--text-tertiary); }
.timeline-desc { color: var(--text-secondary); font-size: var(--font-size-sm); }
.empty-state { display: flex; flex-direction: column; align-items: center; padding: var(--space-12); color: var(--text-tertiary); }
.empty-state svg { margin-bottom: var(--space-4); opacity: 0.4; }
</style>
