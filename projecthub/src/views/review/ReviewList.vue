<template>
  <div class="review-page">
    <!-- 页面头部 -->
    <div class="page-header">
      <div class="header-content">
        <h1 class="page-title">复盘总结</h1>
        <p class="page-subtitle">记录项目经验，持续改进</p>
      </div>
      <div class="header-actions">
        <t-button theme="primary" @click="showCreateDialog = true">
          <template #icon><AddIcon /></template>
          新建复盘
        </t-button>
      </div>
    </div>

    <!-- 筛选 -->
    <div class="filter-bar">
      <t-input v-model="searchQuery" placeholder="搜索复盘..." clearable style="width: 280px">
        <template #prefix-icon><SearchIcon /></template>
      </t-input>
      <t-select v-model="filterType" placeholder="类型" clearable style="width: 140px">
        <t-option value="sprint" label="迭代复盘" />
        <t-option value="project" label="项目复盘" />
        <t-option value="weekly" label="周总结" />
        <t-option value="monthly" label="月总结" />
      </t-select>
    </div>

    <!-- 复盘列表 -->
    <div class="review-grid">
      <div
        v-for="(review, index) in filteredReviews"
        :key="review.id"
        class="review-card"
        :style="{ animationDelay: `${index * 0.05}s` }"
        @click="goToDetail(review)"
      >
        <div class="card-header" :style="{ background: review.gradient }">
          <div class="card-type">{{ review.typeText }}</div>
          <div class="card-date">{{ review.date }}</div>
        </div>

        <div class="card-body">
          <h3 class="review-title">{{ review.title }}</h3>
          <p class="review-summary">{{ review.summary }}</p>

          <div class="review-metrics">
            <div class="metric">
              <CheckCircleIcon class="metric-icon success" />
              <span class="metric-value">{{ review.goodCount }}</span>
              <span class="metric-label">做得好的</span>
            </div>
            <div class="metric">
              <ErrorIcon class="metric-icon warning" />
              <span class="metric-value">{{ review.improveCount }}</span>
              <span class="metric-label">需改进</span>
            </div>
            <div class="metric">
              <StarIcon class="metric-icon primary" />
              <span class="metric-value">{{ review.actionCount }}</span>
              <span class="metric-label">行动计划</span>
            </div>
          </div>

          <div class="card-footer">
            <div class="author">
              <div class="author-avatar" :style="{ background: review.authorColor }">
                {{ review.author.charAt(0) }}
              </div>
              <span class="author-name">{{ review.author }}</span>
            </div>
            <t-tag :type="getStatusType(review.status)" variant="light" size="small">
              {{ review.statusText }}
            </t-tag>
          </div>
        </div>
      </div>
    </div>

    <!-- 创建弹窗 -->
    <t-dialog v-model:visible="showCreateDialog" header="新建复盘" width="600px" :footer="null">
      <t-form ref="formRef" :data="formData" :rules="rules" label-align="top">
        <t-form-item label="标题" name="title">
          <t-input v-model="formData.title" placeholder="请输入复盘标题" />
        </t-form-item>

        <t-form-item label="类型" name="type">
          <t-select v-model="formData.type">
            <t-option value="sprint" label="迭代复盘" />
            <t-option value="project" label="项目复盘" />
            <t-option value="weekly" label="周总结" />
            <t-option value="monthly" label="月总结" />
          </t-select>
        </t-form-item>

        <t-form-item label="概述" name="summary">
          <t-textarea v-model="formData.summary" placeholder="简要描述本次复盘" />
        </t-form-item>

        <div class="form-actions">
          <t-button variant="outline" @click="showCreateDialog = false">取消</t-button>
          <t-button theme="primary" @click="handleSubmit">确定</t-button>
        </div>
      </t-form>
    </t-dialog>
  </div>
</template>

<script setup>
import { ref, computed } from 'vue'
import { useRouter } from 'vue-router'
import { MessagePlugin } from 'tdesign-vue-next'

const router = useRouter()

const searchQuery = ref('')
const filterType = ref('all')
const showCreateDialog = ref(false)
const formRef = ref(null)

const formData = ref({ title: '', type: 'sprint', summary: '' })
const rules = { title: [{ required: true, message: '请输入标题', trigger: 'blur' }] }

const reviews = ref([
  {
    id: 1, title: 'Website Redesign 项目复盘', summary: '本次复盘总结了官网重构项目中的经验教训',
    type: 'project', typeText: '项目复盘', date: '2026-04-10',
    status: 'completed', statusText: '已完成',
    goodCount: 5, improveCount: 3, actionCount: 4,
    author: '张三', authorColor: '#2196F3',
    gradient: 'linear-gradient(135deg, #2196F3 0%, #1976D2 100%)'
  },
  {
    id: 2, title: 'Sprint 23 迭代复盘', summary: '本迭代完成了用户中心模块的开发',
    type: 'sprint', typeText: '迭代复盘', date: '2026-04-08',
    status: 'in_progress', statusText: '进行中',
    goodCount: 3, improveCount: 2, actionCount: 3,
    author: '李四', authorColor: '#4CAF50',
    gradient: 'linear-gradient(135deg, #4CAF50 0%, #388E3C 100%)'
  },
  {
    id: 3, title: '4月第一周工作总结', summary: '本周完成了API接口开发和文档编写',
    type: 'weekly', typeText: '周总结', date: '2026-04-05',
    status: 'completed', statusText: '已完成',
    goodCount: 4, improveCount: 1, actionCount: 2,
    author: '王五', authorColor: '#FF9800',
    gradient: 'linear-gradient(135deg, #FF9800 0%, #F57C00 100%)'
  }
])

const filteredReviews = computed(() => {
  return reviews.value.filter(r => {
    const matchSearch = !searchQuery.value || r.title.includes(searchQuery.value)
    const matchType = filterType.value === 'all' || r.type === filterType.value
    return matchSearch && matchType
  })
})

const getStatusType = (status) => {
  const types = { completed: 'success', in_progress: 'primary', pending: 'default' }
  return types[status] || 'default'
}

const goToDetail = (review) => {
  console.log('查看详情:', review)
}

const handleSubmit = async () => {
  const result = await formRef.value.validate()
  if (result === true) {
    reviews.value.unshift({
      id: Date.now(),
      ...formData.value,
      typeText: formData.value.type === 'sprint' ? '迭代复盘' :
                formData.value.type === 'project' ? '项目复盘' :
                formData.value.type === 'weekly' ? '周总结' : '月总结',
      date: new Date().toISOString().split('T')[0],
      status: 'in_progress', statusText: '进行中',
      goodCount: 0, improveCount: 0, actionCount: 0,
      author: '当前用户', authorColor: '#9C27B0',
      gradient: 'linear-gradient(135deg, #9C27B0 0%, #7B1FA2 100%)'
    })
    showCreateDialog.value = false
    MessagePlugin.success('复盘已创建')
  }
}

import { AddIcon, SearchIcon, CheckCircleIcon, ErrorIcon, StarIcon } from 'tdesign-icons-vue-next'
</script>

<style scoped>
.review-page { max-width: 1600px; margin: 0 auto; }
.page-header { display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 24px; animation: fadeInUp 0.5s ease; }
.header-content { display: flex; flex-direction: column; gap: 4px; }
.page-title { font-size: 28px; font-weight: 700; color: var(--text-primary); margin: 0; }
.page-subtitle { font-size: 14px; color: var(--text-secondary); margin: 0; }
.filter-bar { display: flex; gap: 12px; margin-bottom: 24px; animation: fadeInUp 0.5s ease 0.1s backwards; }

.review-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(340px, 1fr)); gap: 20px; }

.review-card {
  background: var(--bg-container);
  border-radius: var(--radius-xl);
  overflow: hidden;
  border: 1px solid var(--border-color);
  cursor: pointer;
  transition: all var(--transition-base);
  animation: cardEnter 0.6s ease backwards;
}
.review-card:hover { transform: translateY(-4px); box-shadow: var(--shadow-lg); }

.card-header {
  padding: 16px 20px;
  display: flex;
  justify-content: space-between;
  align-items: center;
}
.card-type { font-size: 12px; font-weight: 600; color: rgba(255,255,255,0.9); }
.card-date { font-size: 12px; color: rgba(255,255,255,0.7); }

.card-body { padding: 20px; }
.review-title { font-size: 16px; font-weight: 600; color: var(--text-primary); margin: 0 0 8px 0; }
.review-summary { font-size: 13px; color: var(--text-secondary); margin: 0 0 16px 0; line-height: 1.5; }

.review-metrics { display: flex; gap: 16px; padding: 16px 0; border-top: 1px solid var(--border-light); border-bottom: 1px solid var(--border-light); margin-bottom: 16px; }
.metric { flex: 1; text-align: center; }
.metric-icon { width: 20px; height: 20px; margin-bottom: 4px; }
.metric-icon.success { color: #10B981; }
.metric-icon.warning { color: #FF9800; }
.metric-icon.primary { color: #2196F3; }
.metric-value { display: block; font-size: 20px; font-weight: 700; color: var(--text-primary); }
.metric-label { font-size: 11px; color: var(--text-tertiary); }

.card-footer { display: flex; justify-content: space-between; align-items: center; }
.author { display: flex; align-items: center; gap: 8px; }
.author-avatar { width: 28px; height: 28px; border-radius: var(--radius-full); display: flex; align-items: center; justify-content: center; color: white; font-size: 11px; font-weight: 600; }
.author-name { font-size: 13px; color: var(--text-secondary); }

.form-actions { display: flex; justify-content: flex-end; gap: 12px; margin-top: 24px; padding-top: 24px; border-top: 1px solid var(--border-light); }
</style>
