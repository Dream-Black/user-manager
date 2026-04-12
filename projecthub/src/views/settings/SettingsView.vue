<template>
  <div class="settings-page">
    <div class="page-header fade-in">
      <h2 class="page-title">个人设置</h2>
    </div>
    
    <div class="settings-grid">
      <div class="settings-section card fade-in-up">
        <h3 class="section-title">
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <circle cx="12" cy="12" r="5"/><path d="M12 1v2m0 18v2M4.22 4.22l1.42 1.42m12.72 12.72l1.42 1.42M1 12h2m18 0h2M4.22 19.78l1.42-1.42M18.36 5.64l1.42-1.42"/>
          </svg>
          外观设置
        </h3>
        <div class="setting-item">
          <div class="setting-info">
            <span class="setting-label">主题模式</span>
            <span class="setting-desc">选择界面显示主题</span>
          </div>
          <t-select v-model="settings.themeMode" :options="themeOptions" />
        </div>
      </div>
      
      <div class="settings-section card fade-in-up stagger-1">
        <h3 class="section-title">
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <circle cx="12" cy="12" r="3"/><path d="M12 1v2m0 18v2M4.22 4.22l1.42 1.42m12.72 12.72l1.42 1.42M1 12h2m18 0h2M4.22 19.78l1.42-1.42M18.36 5.64l1.42-1.42"/>
          </svg>
          AI 配置
        </h3>
        <div class="setting-item">
          <div class="setting-info">
            <span class="setting-label">DeepSeek API Key</span>
            <span class="setting-desc">用于 AI 助手功能</span>
          </div>
          <t-input v-model="settings.deepseekApiKey" type="password" placeholder="sk-..." style="width: 300px" />
        </div>
        <div class="setting-item">
          <div class="setting-info">
            <span class="setting-label">AI 模型</span>
          </div>
          <t-select v-model="settings.deepseekModel" :options="modelOptions" />
        </div>
      </div>
      
      <div class="settings-section card fade-in-up stagger-2">
        <h3 class="section-title">
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <circle cx="12" cy="12" r="10"/><polyline points="12 6 12 12 16 14"/>
          </svg>
          工作时间
        </h3>
        <div class="setting-row">
          <div class="setting-item">
            <span class="setting-label">上班时间</span>
            <t-input v-model="settings.workStartTime" placeholder="09:00:00" />
          </div>
          <div class="setting-item">
            <span class="setting-label">下班时间</span>
            <t-input v-model="settings.workEndTime" placeholder="18:00:00" />
          </div>
        </div>
      </div>
      
      <div class="settings-section card fade-in-up stagger-3">
        <h3 class="section-title">
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"/><circle cx="12" cy="7" r="4"/>
          </svg>
          个人资料
        </h3>
        <div class="setting-item">
          <span class="setting-label">当前职位</span>
          <t-input v-model="settings.currentJob" placeholder="如：项目经理" />
        </div>
        <div class="setting-item">
          <span class="setting-label">公司名称</span>
          <t-input v-model="settings.currentCompany" placeholder="如：XX科技" />
        </div>
      </div>

      <div class="settings-section card fade-in-up stagger-4">
        <h3 class="section-title">
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M18 8A6 6 0 0 0 6 8c0 7-3 9-3 9h18s-3-2-3-9"/><path d="M13.73 21a2 2 0 0 1-3.46 0"/>
          </svg>
          每日提醒
        </h3>
        <div class="setting-item">
          <div class="setting-info">
            <span class="setting-label">启用每日提醒</span>
            <span class="setting-desc">每天固定时间提醒查看任务</span>
          </div>
          <t-switch v-model="settings.reminderEnabled" />
        </div>
        <div class="setting-item" v-if="settings.reminderEnabled">
          <span class="setting-label">提醒时间</span>
          <t-input v-model="settings.reminderTime" placeholder="09:00:00" />
        </div>
      </div>
    </div>
    
    <div class="settings-footer fade-in-up stagger-5">
      <t-button theme="primary" size="large" @click="saveSettings">
        <template #icon>
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M19 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11l5 5v11a2 2 0 0 1-2 2z"/><polyline points="17 21 17 13 7 13 7 21"/><polyline points="7 3 7 8 15 8"/>
          </svg>
        </template>
        保存设置
      </t-button>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { MessagePlugin } from 'tdesign-vue-next'

const settings = ref({
  themeMode: 'light',
  deepseekApiKey: '',
  deepseekModel: 'deepseek-chat',
  workStartTime: '09:00:00',
  workEndTime: '18:00:00',
  currentJob: '',
  currentCompany: '',
  reminderEnabled: false,
  reminderTime: '09:00:00'
})

const themeOptions = [
  { value: 'light', label: '浅色模式' },
  { value: 'dark', label: '深色模式' },
  { value: 'auto', label: '跟随系统' }
]

const modelOptions = [
  { value: 'deepseek-chat', label: 'DeepSeek Chat' },
  { value: 'deepseek-coder', label: 'DeepSeek Coder' }
]

const fetchSettings = async () => {
  try {
    const res = await fetch('/api/settings')
    if (res.ok) {
      const data = await res.json()
      settings.value = {
        themeMode: data.themeMode || 'light',
        deepseekApiKey: data.deepseekApiKey || '',
        deepseekModel: data.deepseekModel || 'deepseek-chat',
        workStartTime: data.workStartTime || '09:00:00',
        workEndTime: data.workEndTime || '18:00:00',
        currentJob: data.currentJob || '',
        currentCompany: data.currentCompany || '',
        reminderEnabled: data.reminderEnabled || false,
        reminderTime: data.reminderTime || '09:00:00'
      }
    }
  } catch (error) {
    console.error('Failed to fetch settings:', error)
  }
}

const saveSettings = async () => {
  try {
    const res = await fetch('/api/settings', {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(settings.value)
    })
    if (res.ok) {
      MessagePlugin.success('设置已保存')
    }
  } catch (error) {
    MessagePlugin.error('保存失败')
  }
}

onMounted(fetchSettings)
</script>

<style scoped>
.settings-page { animation: fadeIn 0.3s ease-out; }
.page-header { margin-bottom: var(--space-6); }
.page-title { font-size: var(--font-size-2xl); font-weight: var(--font-weight-semibold); }
.settings-grid { display: grid; grid-template-columns: repeat(2, 1fr); gap: var(--space-5); margin-bottom: var(--space-6); }
.settings-section { padding: var(--space-5); opacity: 0; }
.section-title { display: flex; align-items: center; gap: var(--space-2); font-size: var(--font-size-base); font-weight: var(--font-weight-semibold); margin-bottom: var(--space-4); padding-bottom: var(--space-3); border-bottom: 1px solid var(--border-light); color: var(--text-primary); }
.setting-item { display: flex; justify-content: space-between; align-items: center; padding: var(--space-3) 0; }
.setting-info { flex: 1; }
.setting-label { display: block; font-size: var(--font-size-sm); font-weight: var(--font-weight-medium); margin-bottom: 2px; color: var(--text-primary); }
.setting-desc { font-size: var(--font-size-xs); color: var(--text-tertiary); }
.setting-row { display: grid; grid-template-columns: 1fr 1fr; gap: var(--space-4); }
.settings-footer { display: flex; justify-content: flex-end; }
@media (max-width: 900px) { .settings-grid { grid-template-columns: 1fr; } }
</style>
