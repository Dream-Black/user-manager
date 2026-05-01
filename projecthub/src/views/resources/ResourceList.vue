<template>
  <div class="resource-list-page">
    <!-- 顶部信息栏 -->
    <div class="info-bar">
      <div class="computer-info">
        <t-icon name="computer" />
        <span class="computer-name">{{ computer?.name || '加载中...' }}</span>
        <t-tag v-if="computer?.isOnline" theme="success" size="small">在线</t-tag>
        <t-tag v-else theme="default" size="small">离线</t-tag>
      </div>
      <div class="path-info">
        <t-icon name="folder-open" />
        <span class="path-text">{{ currentPath?.path || '未配置' }}</span>
        <t-button size="small" variant="outline" @click="showSettings = true">
          <template #icon><t-icon name="setting" /></template>
          设置
        </t-button>
        <t-button 
          size="small" 
          variant="outline" 
          @click="handleScan" 
          :loading="scanning"
        >
          <template #icon><t-icon name="refresh" /></template>
          扫描
        </t-button>
      </div>
    </div>

    <!-- 搜索和筛选 -->
    <div class="filter-bar">
      <t-input
        v-model="searchText"
        placeholder="搜索漫画..."
        clearable
        @enter="handleSearch"
      >
        <template #prefix-icon>
          <t-icon name="search" />
        </template>
      </t-input>
      
      <t-select
        v-model="filterType"
        placeholder="类型筛选"
        clearable
        style="width: 150px"
        @change="handleSearch"
      >
        <t-option value="manga" label="日漫" />
        <t-option value="comic" label="美漫" />
        <t-option value="novel" label="小说" />
        <t-option value="picture" label="图集" />
      </t-select>

      <t-select
        v-model="sortBy"
        placeholder="排序"
        style="width: 120px"
      >
        <t-option value="updated" label="更新时间" />
        <t-option value="name" label="名称" />
        <t-option value="created" label="添加时间" />
      </t-select>
    </div>

    <!-- 漫画列表 -->
    <div v-if="loading" class="loading-container">
      <t-loading size="large" text="加载中..." />
    </div>

    <div v-else-if="!currentPath" class="empty-container">
      <div class="empty-state">
        <t-icon name="folder-open" size="64px" />
        <p>尚未配置资源路径</p>
        <t-button theme="primary" @click="showSettings = true">配置路径</t-button>
      </div>
    </div>

    <div v-else-if="comics.length === 0" class="empty-container">
      <div class="empty-state">
        <t-icon name="image" size="64px" />
        <p>暂无漫画</p>
        <t-button theme="primary" @click="handleScan" :loading="scanning">扫描漫画</t-button>
      </div>
    </div>

    <div v-else class="comic-grid">
      <ComicCard
        v-for="comic in comics"
        :key="comic.id"
        :comic="comic"
        @click="goToComic(comic)"
        @edit="handleEdit"
        @delete="handleDelete"
      />
    </div>

    <!-- 分页 -->
    <div v-if="totalCount > pageSize" class="pagination">
      <t-pagination
        v-model="currentPage"
        :total="totalCount"
        :page-size="pageSize"
        @change="handlePageChange"
      />
    </div>

    <!-- 设置弹窗 -->
    <PathSettingDialog
      v-model:visible="showSettings"
      :current-path="currentPath"
      @save="handleSavePath"
    />

    <!-- 编辑弹窗 -->
    <ComicEditDialog
      v-model:visible="showEditDialog"
      :comic="selectedComic"
      @save="handleSaveComic"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { MessagePlugin } from 'tdesign-vue-next';
import ComicCard from './components/ComicCard.vue';
import ComicEditDialog from './components/ComicEditDialog.vue';
import PathSettingDialog from './components/PathSettingDialog.vue';
import * as api from '@/api/resources';
import type { Computer, ResourcePath, Comic } from '@/api/resources';

const router = useRouter();

// 状态
const loading = ref(false);
const scanning = ref(false);
const showSettings = ref(false);
const showEditDialog = ref(false);

// 数据
const computer = ref<Computer | null>(null);
const currentPath = ref<ResourcePath | null>(null);
const comics = ref<Comic[]>([]);
const selectedComic = ref<Comic | null>(null);

// 筛选和分页
const searchText = ref('');
const filterType = ref('');
const sortBy = ref('updated');
const currentPage = ref(1);
const pageSize = ref(20);
const totalCount = ref(0);

// 加载数据
async function loadData() {
  loading.value = true;
  try {
    // 1. 获取当前电脑
    try {
      const { data } = await api.getCurrentComputer();
      computer.value = data;
    } catch {
      // 电脑不存在，创建默认
      const { data } = await api.getCurrentComputer();
      computer.value = data;
    }

    // 2. 获取资源路径
    if (computer.value) {
      const { data } = await api.getResourcePaths(computer.value.id);
      currentPath.value = data.items.find((p: ResourcePath) => p.type === 'comic' && p.isEnabled) || null;
      
      // 3. 获取漫画列表
      if (currentPath.value) {
        await loadComics();
      }
    }
  } catch (error) {
    console.error('加载数据失败:', error);
    MessagePlugin.error('加载数据失败');
  } finally {
    loading.value = false;
  }
}

// 加载漫画列表
async function loadComics() {
  if (!currentPath.value) return;
  
  try {
    const { data } = await api.getComics({
      resourcePathId: currentPath.value.id,
      type: filterType.value || undefined,
      search: searchText.value || undefined,
      page: currentPage.value,
      pageSize: pageSize.value,
    });
    
    comics.value = data.items || data || [];
    totalCount.value = data.total || comics.value.length;
  } catch (error) {
    console.error('加载漫画失败:', error);
  }
}

// 扫描
async function handleScan() {
  if (!currentPath.value) {
    showSettings.value = true;
    return;
  }
  
  scanning.value = true;
  try {
    // 1. 调用6789扫描获取原始数据（含 chapters）
    const scanResult = await api.scanComics(currentPath.value.id);
    console.log('[handleScan] 扫描结果:', scanResult);
    
    // 2. 保存到后端数据库
    if (scanResult.comics.length > 0) {
      await api.batchSaveComics(currentPath.value.id, scanResult.comics);
    }
    
    // 3. 刷新列表
    await loadComics();
    MessagePlugin.success(`扫描完成：共 ${scanResult.totalCount} 部漫画`);
  } catch (error) {
    console.error('扫描失败:', error);
    MessagePlugin.error('扫描失败，请检查代理服务是否运行');
  } finally {
    scanning.value = false;
  }
}

// 搜索
function handleSearch() {
  currentPage.value = 1;
  loadComics();
}

// 分页
function handlePageChange(pageInfo: { current: number; pageSize: number }) {
  currentPage.value = pageInfo.current;
  pageSize.value = pageInfo.pageSize;
  loadComics();
}

// 进入漫画详情
function goToComic(comic: Comic) {
  router.push(`/resources/comics/${comic.id}`);
}

// 编辑
function handleEdit(comic: Comic) {
  selectedComic.value = comic;
  showEditDialog.value = true;
}

// 保存编辑
async function handleSaveComic(data: { displayName: string; type: string; thumbnail?: string }) {
  if (!selectedComic.value) return;
  
  try {
    await api.updateComic(selectedComic.value.id, {
      displayName: data.displayName,
      type: data.type,
    });
    
    if (data.thumbnail) {
      await api.uploadThumbnail(selectedComic.value.id, data.thumbnail);
    }
    
    await loadComics();
    showEditDialog.value = false;
    MessagePlugin.success('保存成功');
  } catch (error) {
    console.error('保存失败:', error);
    MessagePlugin.error('保存失败');
  }
}

// 删除
async function handleDelete(comic: Comic) {
  try {
    await api.deleteComic(comic.id);
    await loadComics();
    MessagePlugin.success('删除成功');
  } catch (error) {
    console.error('删除失败:', error);
    MessagePlugin.error('删除失败');
  }
}

// 保存路径
async function handleSavePath(path: string) {
  if (!computer.value) return;
  
  try {
    if (currentPath.value) {
      await api.updateResourcePath(currentPath.value.id, { path });
    } else {
      const { data } = await api.createResourcePath({
        computerId: computer.value.id,
        type: 'comic',
        path,
      });
      currentPath.value = data;
    }
    
    showSettings.value = false;
    await loadComics();
    MessagePlugin.success('路径保存成功');
  } catch (error) {
    console.error('保存路径失败:', error);
    MessagePlugin.error('保存路径失败');
  }
}

onMounted(() => {
  loadData();
});
</script>

<style scoped>
.resource-list-page {
  padding: 24px;
  max-width: 1400px;
  margin: 0 auto;
}

.info-bar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 16px 20px;
  background: var(--td-bg-color-container);
  border-radius: 8px;
  margin-bottom: 16px;
  gap: 24px;
}

.computer-info,
.path-info {
  display: flex;
  align-items: center;
  gap: 8px;
}

.computer-name {
  font-weight: 500;
}

.path-text {
  color: var(--td-text-color-secondary);
  max-width: 400px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.filter-bar {
  display: flex;
  gap: 12px;
  margin-bottom: 20px;
}

.loading-container,
.empty-container {
  display: flex;
  justify-content: center;
  align-items: center;
  min-height: 300px;
}

.empty-state {
  text-align: center;
  color: var(--td-text-color-secondary);
}

.empty-state .t-icon {
  margin-bottom: 16px;
  color: var(--td-text-color-disabled);
}

.empty-state p {
  margin-bottom: 16px;
}

.comic-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(180px, 1fr));
  gap: 20px;
}

.pagination {
  display: flex;
  justify-content: center;
  margin-top: 24px;
}

@media (max-width: 768px) {
  .resource-list-page {
    padding: 16px;
  }
  
  .info-bar {
    flex-direction: column;
    align-items: flex-start;
  }
  
  .comic-grid {
    grid-template-columns: repeat(auto-fill, minmax(140px, 1fr));
    gap: 12px;
  }
}
</style>
