<template>
  <div class="chapter-selector">
    <t-select
      :value="currentChapterId"
      :options="chapterOptions"
      placeholder="选择章节"
      @change="handleChange"
      :popup-props="{ placement: 'bottom-left', overlayInnerStyle: { maxHeight: '300px' } }"
    >
      <template #value>
        <span v-if="currentChapter" class="current-chapter">
          {{ currentChapter.displayName }}
        </span>
        <span v-else class="placeholder">选择章节</span>
      </template>
      <template #panel>
        <t-virtual-list style="max-height: 280px">
          <div
            v-for="chapter in chapters"
            :key="chapter.id"
            class="chapter-option"
            :class="{ active: chapter.id === currentChapterId }"
            @click="handleSelect(chapter.id)"
          >
            <span class="chapter-name">{{ chapter.displayName }}</span>
            <t-icon v-if="chapter.id === currentChapterId" name="check" />
          </div>
        </t-virtual-list>
      </template>
    </t-select>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import type { ComicChapter } from '@/api/resources';

interface Props {
  chapters: ComicChapter[];
  currentChapterId: number | null;
}

interface Emits {
  (e: 'change', chapterId: number): void;
}

const props = defineProps<Props>();
const emit = defineEmits<Emits>();

// 当前章节
const currentChapter = computed(() => {
  return props.chapters.find(c => c.id === props.currentChapterId);
});

// 章节选项
const chapterOptions = computed(() => {
  return props.chapters.map(chapter => ({
    value: chapter.id,
    label: chapter.displayName,
  }));
});

// 选择
function handleChange(value: number) {
  emit('change', value);
}

// 点击选择
function handleSelect(chapterId: number) {
  emit('change', chapterId);
}
</script>

<style scoped>
.chapter-selector {
  min-width: 200px;
}

.current-chapter {
  font-weight: 500;
}

.placeholder {
  color: var(--td-text-color-placeholder);
}

.chapter-option {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 10px 12px;
  cursor: pointer;
  transition: background-color 0.2s;
}

.chapter-option:hover {
  background-color: var(--td-bg-color-container-hover);
}

.chapter-option.active {
  background-color: var(--td-brand-color-轻量-10);
  color: var(--td-brand-color);
}

.chapter-name {
  flex: 1;
}
</style>
