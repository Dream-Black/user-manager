<template>
  <div
    class="comic-reader-page"
    ref="pageRef"
    @mousemove="showHeader = true"
    @mouseleave="showHeader = false"
  >
    <div class="reader-frame card-shell">
      <div class="reader-header" :class="{ visible: showHeader }">
        <div class="header-left">
          <t-button theme="default" variant="text" @click="goBack">
            <template #icon><t-icon name="return" /></template>
            返回
          </t-button>
        </div>

        <div class="header-center">
          <div class="reader-meta">
            <p class="section-kicker">资源管理</p>
            <span class="comic-title">{{ comic?.displayName || '加载中...' }}</span>
          </div>
        </div>

        <div class="header-right">
          <span class="page-info">{{ currentPageDisplay }} / {{ totalPageCount }}</span>
          
          <t-button theme="default" variant="text" @click="toggleRotation" :title="`当前: ${rotation}度 (点击切换)`">
            <template #icon>
              <t-icon name="refresh" />
            </template>
          </t-button>

          <t-button theme="default" variant="text" @click="toggleFullscreen" title="全屏">
            <template #icon>
              <t-icon :name="isFullscreen ? 'compress' : 'fullscreen'" />
            </template>
          </t-button>
        </div>
      </div>

      <div
        class="reader-content"
        ref="contentRef"
        @click="handleContentClick"
        @keydown="handleKeydown"
        tabindex="0"
      >
        <div v-if="viewMode === 'scroll'" class="scroll-container">
          <div
            v-for="(page, index) in allPages"
            :key="`${page.chapterIndex}-${index}`"
            class="page-item"
          >
            <img
              :src="getPageUrl(page)"
              :alt="`${page.chapterName} 第${index + 1}页`"
              @error="handleImageError"
              loading="lazy"
              :style="{ transform: `rotate(${rotation}deg)` }"
              :class="{ 'is-rotated-vertical': rotation === 90 || rotation === -90 }"
            />
          </div>
        </div>

        <div v-else class="page-container1">
          <img
            v-if="currentPageData"
            :src="getPageUrl(currentPageData)"
            :alt="`${currentPageData.chapterName} 第${currentPageData.pageIndex + 1}页`"
            @error="handleImageError"
            :style="{ transform: `rotate(${rotation}deg)` }"
            :class="{ 'is-rotated-vertical': rotation === 90 || rotation === -90 }"
          />

          <div v-if="allPages.length === 0" class="loading-state">
            <t-loading text="加载中..." />
          </div>
        </div>
      </div>

      <div v-if="viewMode === 'page' && showHeader" class="page-nav">
        <t-button
          variant="outline"
          :disabled="currentPageIndex <= 0"
          @click.stop="prevPage"
        >
          <template #icon><t-icon name="chevron-left" /></template>
          上一张
        </t-button>

        <t-button
          variant="outline"
          :disabled="currentPageIndex >= allPages.length - 1"
          @click.stop="nextPage"
        >
          下一张
          <template #icon><t-icon name="chevron-right" /></template>
        </t-button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted, watch } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import { MessagePlugin } from 'tdesign-vue-next';
import * as api from '@/api/resources';
import type { Comic } from '@/api/resources';

interface PageItem {
  chapterIndex: number;
  chapterName: string;
  chapterPath: string;
  pageIndex: number;
  filename: string;
  fullPath: string;
}

const router = useRouter();
const route = useRoute();

// 状态
const loading = ref(false);
const showHeader = ref(false);
const viewMode = ref<'scroll' | 'page'>('scroll');
const isFullscreen = ref(false);
const rotation = ref<number>(0); // 旋转状态支持 0, 90, -90
const pageRef = ref<HTMLElement | null>(null);
const contentRef = ref<HTMLElement | null>(null);

// 数据
const comic = ref<Comic | null>(null);
const chapters = ref<any[]>([]);
const currentPageIndex = ref(0);

// 所有页面
const allPages = ref<PageItem[]>([]);

// 计算属性
const totalPageCount = computed(() => allPages.value.length);

const currentPageDisplay = computed(() => {
  return currentPageIndex.value + 1;
});

const currentPageData = computed(() => {
  if (allPages.value.length === 0) return null;
  return allPages.value[currentPageIndex.value];
});

// 获取页面URL
function getPageUrl(page: PageItem): string {
  const proxyUrl = 'http://localhost:6789/files/read';
  return `${proxyUrl}?path=${encodeURIComponent(page.fullPath)}`;
}

// 构建页面列表
function buildAllPages() {
  const pages: PageItem[] = [];
  
  chapters.value.forEach((chapter, chapterIndex) => {
    const chapterImages = chapter.images || [];
    chapterImages.forEach((filename: string, pageIndex: number) => {
      pages.push({
        chapterIndex,
        chapterName: chapter.name,
        chapterPath: chapter.path,
        pageIndex,
        filename,
        fullPath: `${chapter.path}/${filename}`.replace(/\\/g, '/'),
      });
    });
  });
  
  allPages.value = pages;
}

// 切换旋转状态 (0 -> 90 -> -90 -> 0)
function toggleRotation() {
  if (rotation.value === 0) {
    rotation.value = 90;
  } else if (rotation.value === 90) {
    rotation.value = -90;
  } else {
    rotation.value = 0;
  }
  localStorage.setItem('comic-rotation', String(rotation.value));
}

// 加载漫画数据
async function loadComic() {
  const comicId = Number(route.params.id);
  
  if (!comicId) return;
  
  loading.value = true;
  
  try {
    const comicResponse = await api.getComic(comicId);
    comic.value = comicResponse.data;
    
    viewMode.value = comic.value!.type === 'scroll' ? 'scroll' : 'page';
    
    const pathResponse = await api.getResourcePaths(comic.value!.resourcePathId);
    const foundPath = pathResponse.data.items?.find((p: any) => p.type === 'comic' && p.isEnabled);
    if (!foundPath) throw new Error('未找到资源路径');
    
    const scanResponse = await api.scanComics(foundPath.id);
    const matchedComic = scanResponse.comics?.find((c: any) => c.name === comic.value!.folderName);
    if (!matchedComic) throw new Error(`未找到漫画文件夹: ${comic.value!.folderName}`);
    
    chapters.value = matchedComic.chapters?.map((ch: any, index: number) => ({
      ...ch,
      id: index,
      displayName: ch.name
    })) || [];
    
    buildAllPages();
    restoreReadingProgress();
    
  } catch (error) {
    console.error('[ComicReader] 加载漫画失败:', error);
    MessagePlugin.error('加载漫画失败');
  } finally {
    loading.value = false;
  }
}

// 保存与恢复进度
function saveReadingProgress() {
  if (!comic.value) return;
  localStorage.setItem(`comic-progress-${comic.value.id}`, String(currentPageIndex.value));
}

function restoreReadingProgress() {
  if (!comic.value) return;
  const saved = localStorage.getItem(`comic-progress-${comic.value.id}`);
  if (saved) {
    currentPageIndex.value = parseInt(saved, 10);
    if (currentPageIndex.value >= allPages.value.length) {
      currentPageIndex.value = 0;
    }
  }
}

// 翻页逻辑
function nextPage() {
  if (currentPageIndex.value < allPages.value.length - 1) {
    currentPageIndex.value++;
    saveReadingProgress();
  }
}

function prevPage() {
  if (currentPageIndex.value > 0) {
    currentPageIndex.value--;
    saveReadingProgress();
  }
}

function handleContentClick(e: MouseEvent) {
  if (viewMode.value !== 'page') return;
  
  const target = e.currentTarget as HTMLElement;
  const { clientWidth } = target;
  const clickX = e.clientX - target.getBoundingClientRect().left;
  
  if (clickX < clientWidth / 3) {
    prevPage();
  } else if (clickX > (clientWidth * 2) / 3) {
    nextPage();
  }
}

function handleKeydown(e: KeyboardEvent) {
  if (viewMode.value !== 'page') return;
  
  switch (e.key) {
    case 'ArrowRight':
    case ' ':
      e.preventDefault();
      nextPage();
      break;
    case 'ArrowLeft':
      e.preventDefault();
      prevPage();
      break;
  }
}

function handleImageError(e: Event) {
  const img = e.target as HTMLImageElement;
  img.src = '/placeholder.png';
}

function goBack() {
  router.push('/resources');
}

function toggleFullscreen() {
  if (!pageRef.value) return;
  
  if (!document.fullscreenElement) {
    pageRef.value.requestFullscreen().then(() => {
      isFullscreen.value = true;
    }).catch((err) => console.error('[ComicReader] 全屏失败:', err));
  } else {
    document.exitFullscreen().then(() => {
      isFullscreen.value = false;
    }).catch((err) => console.error('[ComicReader] 退出全屏失败:', err));
  }
}

function handleFullscreenChange() {
  isFullscreen.value = !!document.fullscreenElement;
}

function handleScroll() {
  if (viewMode.value !== 'scroll' || !contentRef.value) return;
}

onMounted(() => {
  loadComic();
  
  if (contentRef.value) {
    contentRef.value.addEventListener('scroll', handleScroll);
  }
  
  window.addEventListener('keydown', handleKeydown);
  document.addEventListener('fullscreenchange', handleFullscreenChange);
  
  // 恢复视图偏好
  const savedMode = localStorage.getItem('comic-view-mode');
  if (savedMode) viewMode.value = savedMode as 'scroll' | 'page';

  // 恢复旋转偏好
  const savedRotation = localStorage.getItem('comic-rotation');
  if (savedRotation) rotation.value = Number(savedRotation);
});

onUnmounted(() => {
  window.removeEventListener('keydown', handleKeydown);
  document.removeEventListener('fullscreenchange', handleFullscreenChange);
});

watch(viewMode, (newMode) => {
  localStorage.setItem('comic-view-mode', newMode);
});
</script>

<style scoped>
.comic-reader-page {
  position: relative;
  width: 100%;
  min-height: calc(100vh - var(--header-height, 64px));
  overflow: hidden;
}

.page-shell {
  animation: fadeInUp 0.45s ease;
}

.card-shell {
  background: rgba(255,255,255,0.92);
  border: 1px solid var(--border-light);
  border-radius: var(--radius-2xl);
  box-shadow: var(--shadow-card);
}

.reader-frame {
  position: relative;
  width: 100%;
  height: calc(100vh - var(--header-height, 64px) - var(--space-12));
  overflow: hidden;
}

.reader-header {
  position: sticky;
  top: 0;
  z-index: 10;
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: var(--space-4);
  padding: var(--space-4) var(--space-5);
  background: rgba(255, 255, 255, 0.9);
  border-bottom: 1px solid var(--border-light);
  opacity: 0.96;
  transition: opacity 0.25s ease, transform 0.25s ease;
}

.reader-header.visible {
  opacity: 1;
}

.header-left,
.header-right {
  display: flex;
  align-items: center;
  gap: 8px;
}

.header-center {
  flex: 1;
  display: flex;
  justify-content: center;
}

.reader-meta {
  display: flex;
  flex-direction: column;
  align-items: center;
}

.section-kicker {
  font-size: var(--font-size-xs);
  color: var(--primary-color);
  font-weight: var(--font-weight-semibold);
  text-transform: uppercase;
  letter-spacing: 0.08em;
  margin-bottom: 2px;
}

.comic-title {
  font-size: var(--font-size-base);
  font-weight: var(--font-weight-semibold);
  color: var(--text-primary);
  max-width: 420px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.page-info {
  font-size: var(--font-size-sm);
  color: var(--text-secondary);
  padding: 6px 12px;
  background: var(--primary-lighter);
  border: 1px solid var(--primary-light);
  border-radius: var(--radius-full);
}

.reader-content {
  width: 100%;
  height: calc(100% - 72px);
  outline: none;
  overflow: auto;
}

.scroll-container {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: var(--space-4);
  padding: var(--space-6) 0 var(--space-8);
}

.page-item {
  width: min(720px, 82vw);
  background: white;
  border: 1px solid var(--border-light);
  border-radius: var(--radius-xl);
  box-shadow: var(--shadow-sm);
  overflow: hidden;
}

/* 统一加上过渡动画 */
.page-item img {
  width: 100%;
  display: block;
  transition: transform 0.3s cubic-bezier(0.4, 0, 0.2, 1);
}

/* ---------- 核心调整：页漫模式与旋转样式 ---------- */
.page-container1 {
  width: 100%;
  height: 100%;
  display: flex;
  justify-content: center;
  align-items: center;
  /* 开启容器查询，为旋转后的尺寸计算提供基准 */
  container-type: size; 
  overflow: hidden;
}

.page-container1 img {
  max-width: 100%;
  max-height: 100%;
  object-fit: contain;
  box-shadow: var(--shadow-lg);
  background: white;
  /* 动画过渡变得更加丝滑 */
  transition: transform 0.3s cubic-bezier(0.4, 0, 0.2, 1), 
              max-width 0.3s ease, 
              max-height 0.3s ease;
}

/* 专门针对页漫模式，90度或-90度时的尺寸约束（防止溢出并撑满） */
.page-container1 img.is-rotated-vertical {
  /* 旋转后，宽变高，高变宽，依托容器查询动态反转限制 */
  max-width: calc(100vh - 150px);
  max-height: 100vw;
  /* 现代浏览器直接精准贴合 */
  max-width: 100cqh;
  max-height: 100cqw;
}
/* ------------------------------------------------ */

.loading-state {
  color: var(--text-secondary);
}

.page-nav {
  position: absolute;
  bottom: var(--space-5);
  left: 50%;
  transform: translateX(-50%);
  display: flex;
  gap: 12px;
  z-index: 100;
  padding: 10px;
  background: rgba(255,255,255,0.85);
  backdrop-filter: blur(12px);
  border: 1px solid var(--border-light);
  border-radius: var(--radius-full);
  box-shadow: var(--shadow-md);
}

.loading-overlay {
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  display: flex;
  justify-content: center;
  align-items: center;
  background: rgba(255, 255, 255, 0.6);
  z-index: 200;
  backdrop-filter: blur(6px);
}

@media (max-width: 768px) {
  .comic-reader-page {
    padding: var(--space-3);
  }

  .reader-frame {
    height: calc(100vh - var(--header-height, 64px) - var(--space-6));
  }

  .reader-header {
    flex-wrap: wrap;
    padding: var(--space-3) var(--space-4);
  }

  .comic-title {
    max-width: 220px;
    font-size: var(--font-size-sm);
  }

  .page-nav {
    bottom: var(--space-3);
    gap: 8px;
    padding: 8px;
  }

  .page-item {
    width: 92vw;
  }
}
</style>