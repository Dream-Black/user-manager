<template>
  <div class="chat-welcome">
    <div class="welcome-card">
      <div class="welcome-icon">
        <svg viewBox="0 0 48 48" fill="none" width="64" height="64">
          <rect width="48" height="48" rx="12" fill="url(#welcomeGrad)" />
          <path d="M16 24L20 28L32 20" stroke="white" stroke-width="3" stroke-linecap="round" stroke-linejoin="round" />
          <defs>
            <linearGradient id="welcomeGrad" x1="0" y1="0" x2="48" y2="48">
              <stop stop-color="#3B82F6" />
              <stop offset="1" stop-color="#60A5FA" />
            </linearGradient>
          </defs>
        </svg>
      </div>
      <h2>AI 工作助手</h2>
      <p>我可以帮你查询项目进度、今日任务、数据分析等</p>
      <div class="quick-prompts">
        <button
          v-for="p in quickPrompts"
          :key="p"
          class="prompt-chip"
          @click="$emit('quick-prompt', p)"
        >{{ p }}</button>
      </div>
    </div>
  </div>
</template>

<script setup>
defineProps({
  quickPrompts: {
    type: Array,
    default: () => [
      '今天该干什么？',
      '帮我分析项目进度',
      '查看所有项目状态',
      '我有多少待办任务？',
      '生成本周工作总结'
    ]
  }
})

defineEmits(['quick-prompt'])
</script>

<style scoped>
.chat-welcome {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  text-align: center;
  padding: var(--space-8) 0;
}

.welcome-card {
  max-width: 680px;
  width: 100%;
  padding: var(--space-8);
  border-radius: var(--radius-2xl);
  background: rgba(255, 255, 255, 0.85);
  border: 1px solid var(--border-light);
  box-shadow: var(--shadow-lg);
}

.welcome-icon { margin-bottom: var(--space-4); }

.welcome-card h2 {
  font-size: var(--font-size-2xl);
  font-weight: var(--font-weight-bold);
  color: var(--text-primary);
  margin: 0 0 var(--space-2);
}
.welcome-card p {
  font-size: var(--font-size-sm);
  color: var(--text-secondary);
  margin: 0;
}

.quick-prompts {
  display: flex;
  flex-wrap: wrap;
  gap: var(--space-2);
  justify-content: center;
  margin-top: var(--space-5);
}

.prompt-chip {
  padding: 8px 14px;
  border: 1px solid var(--border-light);
  border-radius: var(--radius-full);
  background: #fff;
  color: var(--text-primary);
  font-size: var(--font-size-sm);
  cursor: pointer;
  transition: all var(--transition-fast);
}
.prompt-chip:hover {
  background: var(--primary-lighter);
  border-color: var(--primary-light);
  color: var(--primary-color);
  transform: translateY(-1px);
}
</style>
