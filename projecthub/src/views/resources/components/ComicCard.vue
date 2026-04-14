<template>
  <div class="comic-card" @click="$emit('click', comic)">
    <div class="card-thumbnail">
      <img 
        v-if="comic.thumbnailBase64" 
        :src="comic.thumbnailBase64" 
        :alt="comic.displayName"
      />
      <div v-else class="thumbnail-placeholder">
        <t-icon :name="typeIcon" size="48px" />
      </div>
      
      <!-- 类型标签 -->
      <div class="type-badge" :style="{ backgroundColor: typeColor }">
        {{ typeLabel }}
      </div>
      
      <!-- 操作菜单 -->
      <div class="card-actions" @click.stop>
        <t-dropdown 
          :popup-props="{ placement: 'bottom-right' }"
          trigger="click"
        >
          <t-button variant="text" shape="square" size="small">
            <t-icon name="more" />
          </t-button>
          <template #dropdown>
            <t-dropdown-menu>
              <t-dropdown-item @click="$emit('edit', comic)">
                <t-icon name="edit" />
                <span>编辑</span>
              </t-dropdown-item>
              <t-dropdown-item theme="danger" @click="handleDelete">
                <t-icon name="delete" />
                <span>删除</span>
              </t-dropdown-item>
            </t-dropdown-menu>
          </template>
        </t-dropdown>
      </div>
    </div>
    
    <div class="card-info">
      <div class="comic-name" :title="comic.displayName">
        {{ comic.displayName }}
      </div>
      <div class="comic-meta">
        {{ chapterCount }}
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { DialogPlugin } from 'tdesign-vue-next';
import type { Comic } from '@/api/resources';

interface Props {
  comic: Comic;
}

interface Emits {
  (e: 'click', comic: Comic): void;
  (e: 'edit', comic: Comic): void;
  (e: 'delete', comic: Comic): void;
}

const props = defineProps<Props>();
const emit = defineEmits<Emits>();

// 类型配置
const typeConfig = {
  manga: { label: '日漫', color: '#0052D9', icon: 'book-open' },
  comic: { label: '美漫', color: '#FF9800', icon: 'book' },
  novel: { label: '小说', color: '#4CAF50', icon: 'book-opened' },
  picture: { label: '图集', color: '#9C27B0', icon: 'image' },
};

const typeLabel = computed(() => typeConfig[props.comic.type]?.label || '未知');
const typeColor = computed(() => typeConfig[props.comic.type]?.color || '#999');
const typeIcon = computed(() => typeConfig[props.comic.type]?.icon || 'image');
const chapterCount = computed(() => {
  const count = props.comic.chapterCount || 0;
  if (count === 0) return '暂无章节';
  return `${count}章节`;
});

function handleDelete() {
  const dialog = DialogPlugin.confirm({
    header: '确认删除',
    body: `确定要删除"${props.comic.displayName}"吗？此操作不可恢复。`,
    theme: 'warning',
    confirmBtn: '删除',
    cancelBtn: '取消',
    onConfirm: () => {
      emit('delete', props.comic);
      dialog.destroy();
    },
  });
}
</script>

<style scoped>
.comic-card {
  cursor: pointer;
  border-radius: 8px;
  overflow: hidden;
  background: var(--td-bg-color-container);
  transition: all 0.2s ease;
}

.comic-card:hover {
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
}

.card-thumbnail {
  position: relative;
  aspect-ratio: 3 / 4;
  background: var(--td-bg-color-secondary-container);
  overflow: hidden;
}

.card-thumbnail img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.thumbnail-placeholder {
  width: 100%;
  height: 100%;
  display: flex;
  justify-content: center;
  align-items: center;
  color: var(--td-text-color-disabled);
}

.type-badge {
  position: absolute;
  top: 8px;
  left: 8px;
  padding: 2px 8px;
  border-radius: 4px;
  font-size: 12px;
  color: #fff;
  font-weight: 500;
}

.card-actions {
  position: absolute;
  top: 8px;
  right: 8px;
  opacity: 0;
  transition: opacity 0.2s;
}

.comic-card:hover .card-actions {
  opacity: 1;
}

.card-actions :deep(.t-button) {
  background: rgba(0, 0, 0, 0.5);
  color: #fff;
}

.card-actions :deep(.t-button:hover) {
  background: rgba(0, 0, 0, 0.7);
}

.card-info {
  padding: 12px;
}

.comic-name {
  font-weight: 500;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  margin-bottom: 4px;
}

.comic-meta {
  font-size: 12px;
  color: var(--td-text-color-secondary);
}
</style>
