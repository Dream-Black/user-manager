<template>
  <div class="action-draft-card">
    <div class="action-draft-head">
      <div>
        <span class="action-draft-type">{{ getKindLabel(draft.kind) }}</span>
        <h4 class="action-draft-title">{{ draft.title }}</h4>
      </div>
      <div class="action-draft-actions">
        <button class="btn-mini action-cancel-btn" @click="$emit('cancel')">取消</button>
        <button class="btn-mini action-confirm-btn" @click="$emit('confirm')">确认</button>
      </div>
    </div>

    <div v-if="!draft.validation.ok" class="action-feedback error">
      草案缺少必要字段：{{ draft.validation.errors.join('、') }}
    </div>

    <div class="draft-diff-grid">
      <div class="diff-item">
        <span class="diff-label">目标</span>
        <div class="diff-value">{{ draft.targetLabel || '未识别' }}</div>
      </div>
      <div class="diff-item">
        <span class="diff-label">模式</span>
        <div class="diff-value">{{ draft.mode === 'create' ? '新增' : '更新' }}</div>
      </div>
      <div class="diff-item full-width">
        <span class="diff-label">变更前</span>
        <div class="diff-value muted">{{ draft.before || '无' }}</div>
      </div>
      <div class="diff-item full-width">
        <span class="diff-label">变更后</span>
        <div class="diff-value">{{ draft.after || '无' }}</div>
      </div>
      <div class="diff-item full-width">
        <span class="diff-label">预览</span>
        <div class="diff-value preview">{{ draft.preview }}</div>
      </div>
    </div>

    <div class="action-draft-meta">
      <span class="mini-badge muted">{{ draft.status || 'pending' }}</span>
      <span class="mini-badge muted">v{{ draft.schemaVersion || 1 }}</span>
      <span class="action-draft-target">{{ draft.expiresAt ? `过期：${draft.expiresAt}` : '' }}</span>
    </div>
  </div>
</template>

<script setup>
defineProps({
  draft: { type: Object, required: true }
})

defineEmits(['cancel', 'confirm'])

function getKindLabel(kind) {
  return ({ project: '项目', task: '任务', resource: '资源' }[kind] || kind || '草案')
}
</script>

<style scoped>
.action-draft-card {
  margin-top: 14px;
  padding: 16px;
  border: 1px solid rgba(59, 130, 246, 0.18);
  border-radius: 16px;
  background: linear-gradient(180deg, rgba(255,255,255,0.98) 0%, rgba(239,246,255,0.94) 100%);
  box-shadow: 0 10px 28px rgba(59,130,246,0.08);
}

.action-draft-head {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 12px;
}

.action-draft-type {
  display: inline-flex;
  align-items: center;
  padding: 3px 10px;
  border-radius: 999px;
  background: rgba(59,130,246,0.12);
  color: var(--primary-color);
  font-size: 11px;
  font-weight: 700;
}

.action-draft-title {
  margin: 8px 0 6px;
  font-size: 14px;
  font-weight: 700;
  color: var(--text-primary);
}

.action-draft-actions {
  display: flex;
  gap: 8px;
  flex-shrink: 0;
}

.btn-mini {
  padding: 5px 12px;
  border-radius: 999px;
  font-size: 12px;
  font-weight: 600;
  cursor: pointer;
  transition: all var(--transition-fast);
  border: 1px solid var(--border-light);
  background: #fff;
  color: var(--text-secondary);
}
.btn-mini:hover { background: var(--bg-color-secondary); }

.action-cancel-btn { color: var(--text-secondary); }
.action-confirm-btn {
  background: var(--gradient-primary);
  color: #fff;
  border-color: transparent;
  box-shadow: var(--shadow-sm);
}
.action-confirm-btn:hover {
  background: linear-gradient(135deg, #2563eb, #4f46e5);
  color: #fff;
}

.draft-diff-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 10px;
  margin-top: 12px;
}

.diff-item {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.diff-item.full-width { grid-column: 1 / -1; }

.diff-label {
  font-size: 12px;
  color: var(--text-secondary);
}

.diff-value {
  padding: 8px 10px;
  border: 1px solid var(--border-light);
  border-radius: 10px;
  background: #fff;
  color: var(--text-primary);
  font-size: 13px;
  line-height: 1.6;
  white-space: pre-wrap;
  word-break: break-word;
}

.diff-value.muted { color: var(--text-tertiary); background: #fafcff; }
.diff-value.preview { color: var(--text-secondary); }

.action-draft-meta {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 8px;
  margin-top: 12px;
}

.action-draft-target { font-size: 12px; color: var(--text-secondary); }

.mini-badge {
  display: inline-flex;
  align-items: center;
  padding: 2px 8px;
  border-radius: var(--radius-full);
  background: var(--primary-lighter);
  color: var(--primary-color);
  border: 1px solid var(--primary-light);
  font-size: 11px;
  font-weight: var(--font-weight-semibold);
}
.mini-badge.muted {
  background: var(--bg-color-secondary);
  color: var(--text-secondary);
  border-color: var(--border-light);
}

.action-feedback {
  margin-top: 10px;
  padding: 8px 12px;
  border-radius: 10px;
  font-size: 12px;
  line-height: 1.6;
  border: 1px solid transparent;
}
.action-feedback.error {
  background: #fef2f2;
  border-color: #fecaca;
  color: #b91c1c;
}
</style>
