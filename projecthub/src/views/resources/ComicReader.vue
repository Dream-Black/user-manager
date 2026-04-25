<template>
  <div
    class="comic-reader-page page-shell"
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
          <t-button theme="default" variant="text" @click="toggleFullscreen">
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
            />
          </div>
        </div>

        <div v-else class="page-container">
          <img
            v-if="currentPageData"
            :src="getPageUrl(currentPageData)"
            :alt="`${currentPageData.chapterName} 第${currentPageData.pageIndex + 1}页`"
            @error="handleImageError"
          />

          <div v-if="allPages.length === 0" class="loading-state">
            <t-loading size="large" text="加载中..." />
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

    <div v-if="loading" class="loading-overlay">
      <t-loading size="large" text="加载中..." />
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
const pageRef = ref<HTMLElement | null>(null);
const contentRef = ref<HTMLElement | null>(null);

// 数据
const comic = ref<Comic | null>(null);
const chapters = ref<any[]>([]);
const currentPageIndex = ref(0);

// 所有页面（连贯的跨章节页面列表）
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

// 获取页面URL（通过6789代理）
function getPageUrl(page: PageItem): string {
  const proxyUrl = 'http://localhost:6789/files/read';
  return `${proxyUrl}?path=${encodeURIComponent(page.fullPath)}`;
}

// 构建连贯的页面列表（跨章节）
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
  console.log('[ComicReader] 构建连贯页面列表:', pages.length, '页');
}

// 加载漫画数据
async function loadComic() {
  const comicId = Number(route.params.id);
  console.log('[ComicReader] comicId:', comicId);
  
  if (!comicId) {
    console.error('[ComicReader] 无效的 comicId');
    return;
  }
  
  loading.value = true;
  
  try {
    // 1. 获取漫画信息
    console.log('[ComicReader] 调用 api.getComic()');
    const comicResponse = await api.getComic(comicId);
    console.log('[ComicReader] getComic 响应:', comicResponse);
    comic.value = comicResponse.data;
    console.log('[ComicReader] 漫画信息:', comic.value);
    
    // 根据漫画类型决定默认视图
    viewMode.value = comic.value.type === 'scroll' ? 'scroll' : 'page';
    console.log('[ComicReader] 视图模式:', viewMode.value);
    
    // 2. 获取资源路径
    console.log('[ComicReader] 调用 api.getResourcePaths()');
    const pathResponse = await api.getResourcePaths(comic.value.resourcePathId);
    console.log('[ComicReader] getResourcePaths 响应:', pathResponse);
    
    const foundPath = pathResponse.data.items?.find((p: any) => p.type === 'comic' && p.isEnabled);
    if (!foundPath) {
      throw new Error('未找到资源路径');
    }
    console.log('[ComicReader] 资源路径:', foundPath);
    
    // 3. 调用6789扫描
    console.log('[ComicReader] 调用 api.scanComics()', { resourcePathId: foundPath.id });
    const scanResponse = await api.scanComics(foundPath.id);
    console.log('[ComicReader] scanComics 响应:', scanResponse);
    
    // 4. 用 folderName 匹配当前漫画
    const matchedComic = scanResponse.comics?.find((c: any) => c.name === comic.value.folderName);
    if (!matchedComic) {
      throw new Error(`未找到漫画文件夹: ${comic.value.folderName}`);
    }
    console.log('[ComicReader] 匹配的漫画:', matchedComic);
    
    // 5. 设置章节列表
    chapters.value = matchedComic.chapters?.map((ch: any, index: number) => ({
      ...ch,
      id: index,
      displayName: ch.name
    })) || [];
    console.log('[ComicReader] 章节列表:', chapters.value);
    
    // 6. 构建连贯页面列表
    buildAllPages();
    
    // 7. 恢复阅读进度
    restoreReadingProgress();
    
  } catch (error) {
    console.error('[ComicReader] 加载漫画失败:', error);
    MessagePlugin.error('加载漫画失败');
  } finally {
    loading.value = false;
  }
}

// 保存阅读进度
function saveReadingProgress() {
  if (!comic.value) return;
  localStorage.setItem(`comic-progress-${comic.value.id}`, String(currentPageIndex.value));
}

// 恢复阅读进度
function restoreReadingProgress() {
  if (!comic.value) return;
  const saved = localStorage.getItem(`comic-progress-${comic.value.id}`);
  if (saved) {
    currentPageIndex.value = parseInt(saved, 10);
    // 确保不越界
    if (currentPageIndex.value >= allPages.value.length) {
      currentPageIndex.value = 0;
    }
    console.log('[ComicReader] 恢复阅读进度:', currentPageIndex.value);
  }
}

// 页漫模式：翻页
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

// 点击内容区域（页漫模式）
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

// 键盘控制（页漫模式）
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

// 图片加载错误
function handleImageError(e: Event) {
  const img = e.target as HTMLImageElement;
  img.src = '/placeholder.png';
}

// 返回
function goBack() {
  router.push('/resources');
}

// 全屏切换（容器全屏）
function toggleFullscreen() {
  if (!pageRef.value) return;
  
  if (!document.fullscreenElement) {
    pageRef.value.requestFullscreen().then(() => {
      isFullscreen.value = true;
    }).catch((err) => {
      console.error('[ComicReader] 全屏失败:', err);
    });
  } else {
    document.exitFullscreen().then(() => {
      isFullscreen.value = false;
    }).catch((err) => {
      console.error('[ComicReader] 退出全屏失败:', err);
    });
  }
}

// 监听全屏状态变化
function handleFullscreenChange() {
  isFullscreen.value = !!document.fullscreenElement;
}

// 监听滚动（条漫模式，保存进度）
function handleScroll() {
  if (viewMode.value !== 'scroll' || !contentRef.value) return;
  // 可以实现懒加载或滚动保存进度
}

onMounted(() => {
  loadComic();
  
  // 添加滚动监听
  if (contentRef.value) {
    contentRef.value.addEventListener('scroll', handleScroll);
  }
  
  // 添加键盘监听
  window.addEventListener('keydown', handleKeydown);
  
  // 添加全屏状态监听
  document.addEventListener('fullscreenchange', handleFullscreenChange);
  
  // 恢复视图偏好
  const savedMode = localStorage.getItem('comic-view-mode');
  if (savedMode) {
    viewMode.value = savedMode as 'scroll' | 'page';
  }
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
  background: linear-gradient(180deg, var(--bg-page) 0%, #eef4ff 100%);
  padding: var(--space-6);
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

.page-item img {
  width: 100%;
  display: block;
}

.page-container {
  width: 100%;
  height: 100%;
  display: flex;
  justify-content: center;
  align-items: center;
  padding: var(--space-6);
}

.page-container img {
  max-width: 100%;
  max-height: 100%;
  object-fit: contain;
  border-radius: var(--radius-xl);
  box-shadow: var(--shadow-lg);
  background: white;
}

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
