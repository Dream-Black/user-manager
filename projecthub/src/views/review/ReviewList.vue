<template>
  <div class="review-page">
    <div class="page-header fade-in">
      <h2 class="page-title">复盘总结</h2>
      <t-button theme="primary" @click="showCreate = true">
        <template #icon><svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><line x1="12" y1="5" x2="12" y2="19"/><line x1="5" y1="12" x2="19" y2="12"/></svg></template>
        新增复盘
      </t-button>
    </div>
    
    <div class="review-grid">
      <div v-for="(review, index) in reviews" :key="review.id" 
           class="review-card card fade-in-up"
           :style="{ animationDelay: `${0.08 * index}s` }">
        <div class="review-header">
          <h3 class="review-title">{{ review.title || '复盘记录 ' + review.id }}</h3>
          <span class="review-date">{{ formatDate(review.createdAt) }}</span>
        </div>
        <div class="review-content">
          <div class="review-item">
            <span class="review-label">
              <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M14 9V5a3 3 0 0 0-3-3l-4 9v11h11.28a2 2 0 0 0 2-1.7l1.38-9a2 2 0 0 0-2-2.3zM7 22H4a2 2 0 0 1-2-2v-7a2 2 0 0 1 2-2h3"/></svg>
              做得好的
            </span>
            <p>{{ review.goodPoints || review.good || '暂无记录' }}</p>
          </div>
          <div class="review-item">
            <span class="review-label">
              <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="12" cy="12" r="10"/><line x1="12" y1="8" x2="12" y2="12"/><line x1="12" y1="16" x2="12.01" y2="16"/></svg>
              需要改进的
            </span>
            <p>{{ review.improvements || review.improve || '暂无记录' }}</p>
          </div>
          <div class="review-item">
            <span class="review-label">
              <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="9 11 12 14 22 4"/><path d="M21 12v7a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11"/></svg>
              下一步行动
            </span>
            <p>{{ review.nextActions || review.nextSteps || '暂无记录' }}</p>
          </div>
        </div>
      </div>
    </div>
    
    <div v-if="reviews.length === 0" class="empty-state fade-in">
      <svg width="64" height="64" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
        <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/>
        <polyline points="14 2 14 8 20 8"/>
        <line x1="16" y1="13" x2="8" y2="13"/>
        <line x1="16" y1="17" x2="8" y2="17"/>
      </svg>
      <p>暂无复盘记录</p>
      <t-button theme="primary" variant="outline" @click="showCreate = true">创建复盘</t-button>
    </div>

    <!-- 创建复盘弹窗 -->
    <t-dialog
      v-model:visible="showCreate"
      header="新增复盘"
      :footer="false"
      width="520px"
    >
      <div class="form-group">
        <label>复盘标题</label>
        <t-input v-model="newReview.title" placeholder="给这次复盘起个标题" />
      </div>
      <div class="form-group">
        <label>
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M14 9V5a3 3 0 0 0-3-3l-4 9v11h11.28a2 2 0 0 0 2-1.7l1.38-9a2 2 0 0 0-2-2.3zM7 22H4a2 2 0 0 1-2-2v-7a2 2 0 0 1 2-2h3"/></svg>
          做得好的
        </label>
        <t-textarea v-model="newReview.goodPoints" placeholder="总结这次做得好的地方" />
      </div>
      <div class="form-group">
        <label>
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="12" cy="12" r="10"/><line x1="12" y1="8" x2="12" y2="12"/><line x1="12" y1="16" x2="12.01" y2="16"/></svg>
          需要改进的
        </label>
        <t-textarea v-model="newReview.improvements" placeholder="反思需要改进的地方" />
      </div>
      <div class="form-group">
        <label>
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="9 11 12 14 22 4"/><path d="M21 12v7a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11"/></svg>
          下一步行动
        </label>
        <t-textarea v-model="newReview.nextActions" placeholder="制定下一步行动计划" />
      </div>
      <div class="form-actions">
        <t-button variant="outline" @click="showCreate = false">取消</t-button>
        <t-button theme="primary" @click="createReview">创建复盘</t-button>
      </div>
    </t-dialog>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { MessagePlugin } from 'tdesign-vue-next'
import dayjs from 'dayjs'

const reviews = ref([])
const showCreate = ref(false)

const newReview = ref({
  title: '',
  goodPoints: '',
  improvements: '',
  nextActions: ''
})

const fetchReviews = async () => {
  try {
    const res = await fetch('/api/reviews')
    if (res.ok) {
      reviews.value = await res.json()
    }
  } catch (error) {
    console.error('Failed to fetch reviews:', error)
  }
}

const createReview = async () => {
  if (!newReview.value.goodPoints && !newReview.value.improvements && !newReview.value.nextActions) {
    MessagePlugin.warning('请至少填写一项内容')
    return
  }

  try {
    const res = await fetch('/api/reviews', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        projectId: 1, // 默认项目
        title: newReview.value.title || '复盘记录',
        goodPoints: newReview.value.goodPoints,
        improvements: newReview.value.improvements,
        nextActions: newReview.value.nextActions
      })
    })
    if (res.ok) {
      const review = await res.json()
      reviews.value.unshift(review)
      showCreate.value = false
      newReview.value = { title: '', goodPoints: '', improvements: '', nextActions: '' }
      MessagePlugin.success('复盘创建成功')
    }
  } catch (error) {
    MessagePlugin.error('创建失败')
  }
}

const formatDate = (d) => dayjs(d).format('YYYY-MM-DD')

onMounted(fetchReviews)
</script>

<style scoped>
.review-page { animation: fadeIn 0.3s ease-out; }
.page-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: var(--space-6); }
.page-title { font-size: var(--font-size-2xl); font-weight: var(--font-weight-semibold); }
.review-grid { display: grid; grid-template-columns: repeat(2, 1fr); gap: var(--space-5); }
.review-card { padding: var(--space-5); opacity: 0; transition: all 0.2s; }
.review-card:hover { transform: translateY(-2px); box-shadow: var(--shadow-md); }
.review-header { display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: var(--space-4); padding-bottom: var(--space-3); border-bottom: 1px solid var(--border-light); }
.review-title { font-size: var(--font-size-lg); font-weight: var(--font-weight-semibold); color: var(--text-primary); }
.review-date { font-size: var(--font-size-xs); color: var(--text-tertiary); flex-shrink: 0; }
.review-content { display: flex; flex-direction: column; gap: var(--space-4); }
.review-item span { display: flex; align-items: center; gap: var(--space-2); font-size: var(--font-size-xs); font-weight: var(--font-weight-medium); color: var(--text-tertiary); margin-bottom: var(--space-2); }
.review-item p { font-size: var(--font-size-sm); color: var(--text-secondary); line-height: 1.6; min-height: 40px; }
.empty-state { display: flex; flex-direction: column; align-items: center; padding: var(--space-12); color: var(--text-tertiary); }
.empty-state svg { margin-bottom: var(--space-4); opacity: 0.4; }
.empty-state p { margin-bottom: var(--space-4); }
.form-group { margin-bottom: var(--space-4); }
.form-group label { display: flex; align-items: center; gap: var(--space-2); font-size: var(--font-size-sm); font-weight: var(--font-weight-medium); color: var(--text-primary); margin-bottom: var(--space-2); }
.form-actions { display: flex; justify-content: flex-end; gap: var(--space-3); margin-top: var(--space-6); }
@media (max-width: 900px) { .review-grid { grid-template-columns: 1fr; } }
</style>
