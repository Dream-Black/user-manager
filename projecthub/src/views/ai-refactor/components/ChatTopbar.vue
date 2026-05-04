<template>
  <div class="chat-topbar">
    <div class="topbar-left">
      <p class="section-kicker">AI 工作台</p>
      <span class="current-title">{{ currentTitle }}</span>
    </div>
    <div class="topbar-right">
      <t-select
        :model-value="selectedModel"
        :options="modelOptions"
        size="small"
        style="width: 130px"
        placeholder="选择模型"
        @change="$emit('update:selectedModel', $event)"
      />
      <label class="deep-think-toggle">
        <t-switch
          :model-value="deepThink"
          size="small"
          @change="$emit('update:deepThink', $event)"
        />
        <span class="toggle-label">深度思考</span>
      </label>
      <div class="balance-badge" :class="balanceClass" :title="balanceTooltip">
        <span class="balance-dot"></span>
        <span class="balance-text">{{ balanceText }}</span>
      </div>
    </div>
  </div>
</template>

<script setup>
defineProps({
  currentTitle: { type: String, default: '选择一个对话开始' },
  selectedModel: { type: String, default: 'deepseek-v4-flash' },
  deepThink: { type: Boolean, default: false },
  modelOptions: { type: Array, default: () => [] },
  balanceText: { type: String, default: '' },
  balanceClass: { type: String, default: '' },
  balanceTooltip: { type: String, default: '' }
})

defineEmits(['update:selectedModel', 'update:deepThink'])
</script>

<style scoped>
.chat-topbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: var(--space-5) var(--space-6);
  border-bottom: 1px solid var(--border-light);
  background: rgba(255, 255, 255, 0.86);
  backdrop-filter: blur(16px);
  flex-shrink: 0;
}

.topbar-left, .topbar-right {
  display: flex;
  align-items: center;
  gap: var(--space-4);
}

.section-kicker {
  font-size: var(--font-size-xs);
  color: var(--primary-color);
  font-weight: var(--font-weight-semibold);
  text-transform: uppercase;
  letter-spacing: 0.08em;
  margin: 0 0 2px;
}

.current-title {
  font-size: var(--font-size-base);
  font-weight: var(--font-weight-semibold);
  color: var(--text-primary);
}

.deep-think-toggle {
  display: flex;
  align-items: center;
  gap: 8px;
  cursor: pointer;
}
.toggle-label {
  font-size: var(--font-size-sm);
  color: var(--text-secondary);
}

.balance-badge {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 4px 10px;
  border-radius: var(--radius-full);
  background: var(--bg-color-secondary);
  border: 1px solid var(--border-light);
  cursor: default;
  transition: all var(--transition-fast);
}
.balance-dot {
  width: 7px;
  height: 7px;
  border-radius: 50%;
  flex-shrink: 0;
}
.balance-text {
  font-size: var(--font-size-xs);
  font-weight: var(--font-weight-medium);
  white-space: nowrap;
}

.balance-badge.ok .balance-dot { background: var(--success-color); box-shadow: 0 0 6px rgba(16,185,129,0.5); }
.balance-badge.ok .balance-text { color: var(--success-color); }
.balance-badge.warning .balance-dot { background: var(--warning-color); box-shadow: 0 0 6px rgba(245,158,11,0.5); animation: dot-pulse 1.4s infinite; }
.balance-badge.warning .balance-text { color: var(--warning-color); }
.balance-badge.error .balance-dot { background: var(--text-tertiary); }
.balance-badge.error .balance-text { color: var(--text-tertiary); }
.balance-badge.loading .balance-dot { background: var(--primary-color); animation: dot-pulse 1.4s infinite; }
.balance-badge.loading .balance-text { color: var(--primary-color); }

@keyframes dot-pulse {
  0%, 80%, 100% { transform: scale(0.6); opacity: 0.4; }
  40% { transform: scale(1); opacity: 1; }
}
</style>
