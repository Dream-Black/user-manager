<template>
  <aside class="conv-sidebar card-shell">
    <div class="conv-header">
      <div>
        <p class="section-kicker">智能助手</p>
        <h3 class="section-title">对话列表</h3>
      </div>
      <button class="btn-new" @click="$emit('new-conversation')">
        <Plus :size="18" />
        <span>新建</span>
      </button>
    </div>

    <div class="conv-tools">
      <div class="search-shell">
        <Search :size="16" class="search-icon" />
        <input
          :value="searchQuery"
          class="conv-search"
          placeholder="搜索对话标题或摘要..."
          @input="$emit('update:searchQuery', $event.target.value); $emit('search')"
        />
      </div>
    </div>

    <div class="conv-list">
      <div
        v-for="conv in conversations"
        :key="conv.id"
        class="conv-item"
        :class="{ active: conv.id === currentConvId }"
        @click="$emit('switch', conv.id)"
      >
        <div class="conv-info">
          <span class="conv-name">{{ conv.title }}</span>
          <span class="conv-time">{{ formatTime(conv.updatedAt) }}</span>
          <div class="conv-badges">
            <span v-if="conv.isPinned" class="mini-badge">置顶</span>
            <span v-if="conv.isArchived" class="mini-badge muted">已归档</span>
          </div>
        </div>
        <div class="conv-actions">
          <button
            class="icon-action-btn"
            :class="{ active: conv.isPinned }"
            :title="conv.isPinned ? '取消置顶' : '置顶对话'"
            @click.stop="$emit('toggle-pin', conv)"
          >
            <PinIcon />
          </button>
          <button
            class="icon-action-btn"
            :class="{ active: conv.isArchived }"
            :title="conv.isArchived ? '取消归档' : '归档对话'"
            @click.stop="$emit('toggle-archive', conv)"
          >
            <FolderOffIcon />
          </button>
          <button
            class="icon-action-btn danger"
            title="删除对话"
            @click.stop="$emit('delete', conv.id)"
          >
            <Trash2 />
          </button>
        </div>
      </div>

      <div v-if="conversations.length === 0" class="conv-empty">
        暂无对话，点击上方按钮创建
      </div>
    </div>
  </aside>
</template>

<script setup>
import { Plus, Trash2, Search } from 'lucide-vue-next'
import { PinIcon, FolderOffIcon } from 'tdesign-icons-vue-next'

defineProps({
  conversations: { type: Array, default: () => [] },
  currentConvId: { type: [Number, String], default: null },
  searchQuery: { type: String, default: '' }
})

defineEmits([
  'new-conversation',
  'switch',
  'delete',
  'toggle-pin',
  'toggle-archive',
  'search',
  'update:searchQuery'
])

function formatTime(dateStr) {
  if (!dateStr) return ''
  const d = new Date(dateStr)
  const now = new Date()
  const diff = now - d
  if (diff < 60000) return '刚刚'
  if (diff < 3600000) return Math.floor(diff / 60000) + '分钟前'
  if (diff < 86400000) return Math.floor(diff / 3600000) + '小时前'
  const month = String(d.getMonth() + 1).padStart(2, '0')
  const day = String(d.getDate()).padStart(2, '0')
  return `${month}-${day}`
}
</script>

<style scoped>
.conv-sidebar {
  width: 300px;
  min-width: 300px;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.conv-header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: var(--space-3);
  padding: var(--space-5);
  border-bottom: 1px solid var(--border-light);
  background: linear-gradient(180deg, #fff 0%, #f8fbff 100%);
}

.section-kicker {
  font-size: var(--font-size-xs);
  color: var(--primary-color);
  font-weight: var(--font-weight-semibold);
  text-transform: uppercase;
  letter-spacing: 0.08em;
  margin-bottom: 4px;
}

.section-title {
  font-size: var(--font-size-lg);
  color: var(--text-primary);
  margin: 0;
}

.btn-new {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 8px 14px;
  border: 1px solid var(--primary-light);
  border-radius: var(--radius-full);
  background: var(--primary-lighter);
  color: var(--primary-color);
  font-size: var(--font-size-xs);
  font-weight: var(--font-weight-semibold);
  cursor: pointer;
  transition: all var(--transition-fast);
  flex-shrink: 0;
}
.btn-new:hover {
  background: var(--primary-color);
  color: white;
  transform: translateY(-1px);
  box-shadow: var(--shadow-md);
}

.conv-tools {
  padding: 12px var(--space-5) 0;
}

.search-shell {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 10px 12px;
  border: 1px solid var(--border-light);
  border-radius: var(--radius-lg);
  background: #fff;
  transition: all var(--transition-fast);
}
.search-shell:focus-within {
  border-color: var(--primary-color);
  box-shadow: 0 0 0 3px rgba(59, 130, 246, 0.1);
}
.search-icon { color: var(--text-tertiary); flex-shrink: 0; }
.conv-search {
  width: 100%;
  border: none;
  outline: none;
  font-size: var(--font-size-sm);
  color: var(--text-primary);
  background: transparent;
}
.conv-search::placeholder { color: var(--text-tertiary); }

.conv-list {
  flex: 1;
  overflow-y: auto;
  padding: var(--space-3) var(--space-3) var(--space-4);
}

.conv-item {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: var(--space-3);
  padding: var(--space-4);
  margin-bottom: var(--space-2);
  border-radius: var(--radius-lg);
  cursor: pointer;
  transition: all var(--transition-fast);
  border: 1px solid transparent;
}
.conv-item:hover {
  background: var(--primary-lighter);
  border-color: var(--primary-light);
}
.conv-item.active {
  background: linear-gradient(135deg, var(--primary-lighter) 0%, #ffffff 100%);
  border-color: var(--primary-light);
  box-shadow: 0 8px 24px rgba(59, 130, 246, 0.08);
}

.conv-info {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: 4px;
}
.conv-name {
  font-size: var(--font-size-sm);
  color: var(--text-primary);
  font-weight: var(--font-weight-medium);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}
.conv-time { font-size: var(--font-size-xs); color: var(--text-tertiary); }

.conv-badges {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
  margin-bottom: 2px;
}
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

.conv-actions {
  display: flex;
  align-items: center;
  gap: 6px;
  flex-shrink: 0;
}

.icon-action-btn {
  width: 30px;
  height: 30px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  border: 1px solid transparent;
  border-radius: var(--radius-base);
  background: transparent;
  color: var(--text-tertiary);
  cursor: pointer;
  transition: all var(--transition-fast);
}
.icon-action-btn :deep(svg) { width: 16px; height: 16px; }
.icon-action-btn:hover {
  background: var(--primary-lighter);
  border-color: var(--primary-light);
  color: var(--primary-color);
}
.icon-action-btn.active {
  background: rgba(59, 130, 246, 0.12);
  border-color: var(--primary-light);
  color: var(--primary-color);
}
.icon-action-btn.danger:hover {
  background: var(--error-lighter);
  border-color: var(--error-light);
  color: var(--error-color);
}

.conv-empty {
  text-align: center;
  color: var(--text-secondary);
  padding: var(--space-10) var(--space-4);
  font-size: var(--font-size-sm);
}

@media (max-width: 1200px) {
  .conv-sidebar { width: 280px; min-width: 280px; }
}
@media (max-width: 900px) {
  .conv-sidebar { width: 100%; min-width: 0; max-height: 320px; }
}
</style>
