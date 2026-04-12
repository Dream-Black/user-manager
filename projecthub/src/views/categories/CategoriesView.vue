<template>
  <div class="categories-page">
    <div class="page-header fade-in">
      <h2 class="page-title">分类管理</h2>
      <t-button theme="primary" @click="showCreate = true">
        <template #icon><svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><line x1="12" y1="5" x2="12" y2="19"/><line x1="5" y1="12" x2="19" y2="12"/></svg></template>
        新增分类
      </t-button>
    </div>
    
    <div class="category-grid">
      <div v-for="(cat, index) in categories" :key="cat.id"
           class="category-card card fade-in-up"
           :style="{ animationDelay: `${0.06 * index}s`, '--cat-color': cat.color }">
        <div class="cat-icon" :style="{ background: cat.color + '20', color: cat.color }">
          <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M22 19a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h5l2 3h9a2 2 0 0 1 2 2z"/>
          </svg>
        </div>
        <div class="cat-info">
          <h3 class="cat-name">{{ cat.name }}</h3>
          <span class="cat-count">{{ cat.taskCount || 0 }} 个任务</span>
        </div>
        <t-tag v-if="cat.isSystem" variant="outline" size="small">系统</t-tag>
        <div class="cat-actions" v-if="!cat.isSystem">
          <t-button variant="text" size="small" @click="editCategory(cat)">
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"/><path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"/></svg>
          </t-button>
          <t-button variant="text" size="small" @click="deleteCategory(cat)">
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="3 6 5 6 21 6"/><path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"/></svg>
          </t-button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { MessagePlugin } from 'tdesign-vue-next'

const categories = ref([])
const showCreate = ref(false)

const fetchCategories = async () => {
  const res = await fetch('/api/categories')
  if (res.ok) categories.value = await res.json()
}

const editCategory = (cat) => console.log('Edit', cat)
const deleteCategory = async (cat) => {
  await fetch(`/api/categories/${cat.id}`, { method: 'DELETE' })
  categories.value = categories.value.filter(c => c.id !== cat.id)
  MessagePlugin.success('已删除')
}

onMounted(fetchCategories)
</script>

<style scoped>
.categories-page { animation: fadeIn 0.3s ease-out; }
.page-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: var(--space-6); }
.page-title { font-size: var(--font-size-2xl); font-weight: var(--font-weight-semibold); }
.category-grid { display: grid; grid-template-columns: repeat(3, 1fr); gap: var(--space-4); }
.category-card { display: flex; align-items: center; gap: var(--space-4); padding: var(--space-5); opacity: 0; transition: all 0.2s; }
.category-card:hover { transform: translateY(-2px); box-shadow: var(--shadow-md); }
.cat-icon { width: 48px; height: 48px; border-radius: var(--radius-xl); display: flex; align-items: center; justify-content: center; flex-shrink: 0; }
.cat-info { flex: 1; }
.cat-name { font-size: var(--font-size-base); font-weight: var(--font-weight-medium); margin-bottom: 2px; }
.cat-count { font-size: var(--font-size-xs); color: var(--text-tertiary); }
.cat-actions { display: flex; gap: var(--space-1); opacity: 0; transition: opacity 0.15s; }
.category-card:hover .cat-actions { opacity: 1; }
@media (max-width: 1000px) { .category-grid { grid-template-columns: repeat(2, 1fr); } }
@media (max-width: 600px) { .category-grid { grid-template-columns: 1fr; } }
</style>
