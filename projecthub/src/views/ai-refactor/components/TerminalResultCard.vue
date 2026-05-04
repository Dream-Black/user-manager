<template>
  <div class="terminal-result-card">
    <details :open="!collapsed">
      <summary
        class="terminal-result-header"
        @click="$emit('update:collapsed', !collapsed)"
      >
        <Terminal :size="16" />
        <span>终端执行结果</span>
        <span class="terminal-command">{{ result.command }}</span>
        <span v-if="result.success" class="terminal-badge success">成功</span>
        <span v-else class="terminal-badge error">失败</span>
      </summary>
      <div class="terminal-result-content">
        <div v-if="result.stdout" class="terminal-stdout">
          <div class="terminal-label">输出：</div>
          <pre class="terminal-output">{{ result.stdout }}</pre>
        </div>
        <div v-if="result.stderr" class="terminal-stderr">
          <div class="terminal-label">错误：</div>
          <pre class="terminal-output error">{{ result.stderr }}</pre>
        </div>
        <div v-if="result.error" class="terminal-error">
          <div class="terminal-label">异常：</div>
          <pre class="terminal-output error">{{ result.error }}</pre>
        </div>
      </div>
    </details>
  </div>
</template>

<script setup>
import { Terminal } from 'lucide-vue-next'

defineProps({
  result: { type: Object, required: true },
  collapsed: { type: Boolean, default: true }
})

defineEmits(['update:collapsed'])
</script>

<style scoped>
.terminal-result-card {
  margin: var(--space-4) 0;
  padding: 0 var(--space-4);
  max-width: min(760px, 80%);
  align-self: flex-start;
}
.terminal-result-card details {
  background: #1e1e1e;
  border-radius: var(--radius-lg);
  overflow: hidden;
}
.terminal-result-header {
  display: flex;
  align-items: center;
  gap: var(--space-2);
  padding: var(--space-3) var(--space-4);
  background: #2d2d2d;
  color: #e0e0e0;
  cursor: pointer;
  font-size: var(--font-size-sm);
  list-style: none;
}
.terminal-result-header::-webkit-details-marker { display: none; }
.terminal-command {
  color: #4fc3f7;
  font-family: var(--font-family-mono);
  font-size: 12px;
}
.terminal-badge {
  padding: 2px 8px;
  border-radius: var(--radius-full);
  font-size: 11px;
  font-weight: var(--font-weight-semibold);
}
.terminal-badge.success { background: rgba(16,185,129,0.2); color: #10b981; }
.terminal-badge.error { background: rgba(239,68,68,0.2); color: #ef4444; }

.terminal-result-content { padding: var(--space-4); color: #e0e0e0; font-size: var(--font-size-sm); }
.terminal-label { color: #888; margin-bottom: 4px; font-size: 12px; }
.terminal-output {
  margin: 0;
  padding: var(--space-3);
  background: #0d0d0d;
  border-radius: var(--radius-base);
  font-family: var(--font-family-mono);
  font-size: 12px;
  white-space: pre-wrap;
  word-break: break-all;
  color: #10b981;
}
.terminal-output.error { color: #ef4444; }
</style>
