<template>
  <div class="schedule-page">
    <div class="page-header">
      <div class="header-content">
        <h1 class="page-title">日程管理</h1>
      </div>
      <div class="header-actions">
        <t-button theme="default" variant="outline" @click="handleTestNotification">
          测试通知
        </t-button>
        <t-button theme="primary" @click="handleAddSchedule">
          <template #icon><AddIcon /></template>
          添加日程
        </t-button>
      </div>
    </div>

    <div class="search-bar">
      <t-input
        v-model="searchQuery"
        placeholder="搜索日程..."
        clearable
      >
        <template #prefix-icon>
          <SearchIcon />
        </template>
      </t-input>
    </div>

    <div class="schedule-grid">
      <div
        v-for="schedule in filteredSchedules"
        :key="schedule.id"
        class="schedule-card"
        @click="handleEditSchedule(schedule)"
      >
        <div class="schedule-card-header">
          <h3 class="schedule-title">{{ schedule.title }}</h3>
          <t-button variant="text" size="small" @click.stop="handleDeleteSchedule(schedule)">
            <template #icon><DeleteIcon /></template>
          </t-button>
        </div>
        <p class="schedule-content">{{ schedule.content }}</p>
        <div class="schedule-footer">
          <span class="schedule-date">{{ formatDate(schedule.startDate) }} - {{ formatDate(schedule.endDate) }}</span>
          <t-switch 
            :model-value="schedule.reminderEnabled" 
            size="small" 
            @click.stop
            @change="(val) => handleReminderToggle(schedule, val)"
          />
        </div>
      </div>

      <div v-if="filteredSchedules.length === 0" class="schedule-empty">
        暂无日程
      </div>
    </div>

    <t-dialog
      v-model:visible="showDeleteDialog"
      header="删除日程"
      width="400px"
      :footer="null"
    >
      <div class="delete-confirm">
        <p class="delete-text">确定要删除日程 <strong>{{ deletingSchedule?.title }}</strong> 吗？</p>
        <div class="delete-actions">
          <t-button variant="outline" @click="showDeleteDialog = false">取消</t-button>
          <t-button theme="danger" @click="confirmDelete">确认删除</t-button>
        </div>
      </div>
    </t-dialog>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { AddIcon, SearchIcon, DeleteIcon } from 'tdesign-icons-vue-next'
import { scheduleApi } from '@/api'
import { useScheduleSSEState } from '@/composables/useScheduleSSE'
import { MessagePlugin } from 'tdesign-vue-next'

const router = useRouter()
const { reminders, isConnected } = useScheduleSSEState()

const searchQuery = ref('')
const showDeleteDialog = ref(false)
const deletingSchedule = ref(null)
const schedules = ref([])

const filteredSchedules = computed(() => {
  if (!searchQuery.value) return schedules.value
  const query = searchQuery.value.toLowerCase()
  return schedules.value.filter(s =>
    s.title.toLowerCase().includes(query) ||
    (s.content && s.content.toLowerCase().includes(query))
  )
})

const formatDate = (dateStr) => {
  if (!dateStr) return ''
  const date = new Date(dateStr)
  return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`
}

const loadSchedules = async () => {
  try {
    const data = await scheduleApi.list()
    schedules.value = data.map(s => ({
      id: s.id,
      title: s.title,
      content: s.content,
      startDate: s.startDate,
      endDate: s.endDate,
      repeatMode: s.repeatMode,
      repeatDays: s.repeatDays,
      startTime: s.startTime,
      endTime: s.endTime,
      reminderEnabled: s.reminderEnabled
    }))
  } catch (error) {
    console.error('加载日程失败:', error)
    MessagePlugin.error('加载日程失败')
  }
}

const handleAddSchedule = () => {
  router.push('/schedule/new')
}

const handleEditSchedule = (schedule) => {
  router.push(`/schedule/${schedule.id}`)
}

const handleDeleteSchedule = (schedule) => {
  deletingSchedule.value = schedule
  showDeleteDialog.value = true
}

const confirmDelete = async () => {
  try {
    await scheduleApi.delete(deletingSchedule.value.id)
    schedules.value = schedules.value.filter(s => s.id !== deletingSchedule.value.id)
    showDeleteDialog.value = false
    MessagePlugin.success('删除成功')
  } catch (error) {
    console.error('删除日程失败:', error)
    MessagePlugin.error('删除失败')
  }
}

const handleReminderToggle = async (schedule, enabled) => {
  try {
    await scheduleApi.update(schedule.id, { reminderEnabled: enabled })
    schedule.reminderEnabled = enabled
    MessagePlugin.success(enabled ? '已开启提醒' : '已关闭提醒')
  } catch (error) {
    console.error('更新提醒失败:', error)
    MessagePlugin.error('更新失败')
  }
}

const handleTestNotification = () => {
  if (window.localBridge?.showNotification) {
    window.localBridge.showNotification({
      title: '测试日程',
      body: '这是一条测试通知消息',
      scheduleId: 1
    })
    MessagePlugin.success('通知已发送')
  } else {
    MessagePlugin.warning('桌面通知仅在桌面端可用')
  }
}

onMounted(() => {
  loadSchedules()
})
</script>

<style scoped>
.schedule-page {
  margin: 0 auto;
}

.page-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: 24px;
  animation: fadeInUp 0.5s ease;
}

.header-content {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.page-title {
  font-size: 28px;
  font-weight: 700;
  color: var(--text-primary);
  margin: 0;
}

.search-bar {
  margin-bottom: 16px;
  animation: fadeInUp 0.5s ease 0.05s backwards;
}

.search-bar :deep(.t-input) {
  max-width: 400px;
}

.reminder-notification {
  margin-bottom: 16px;
  animation: fadeInUp 0.5s ease 0.08s backwards;
}

.schedule-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(320px, 1fr));
  gap: 16px;
  animation: fadeInUp 0.5s ease 0.1s backwards;
}

.schedule-card {
  background: var(--bg-card-solid);
  border-radius: var(--radius-xl);
  padding: 20px;
  border: 1px solid var(--border-light);
  cursor: pointer;
  transition: all var(--transition-normal);
  box-shadow: var(--shadow-sm);
  display: flex;
  flex-direction: column;
  min-height: 160px;
}

.schedule-card:hover {
  border-color: var(--primary-color);
  box-shadow: var(--shadow-md);
  transform: translateY(-2px);
}

.schedule-card-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: 12px;
}

.schedule-title {
  font-size: 16px;
  font-weight: 600;
  color: var(--text-primary);
  margin: 0;
  flex: 1;
  line-height: 1.4;
}

.schedule-content {
  font-size: 13px;
  color: var(--text-secondary);
  margin: 0 0 12px 0;
  line-height: 1.6;
  flex: 1;
  display: -webkit-box;
  -webkit-line-clamp: 3;
  -webkit-box-orient: vertical;
  overflow: hidden;
}

.schedule-footer {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-top: auto;
  padding-top: 12px;
  border-top: 1px solid var(--border-light);
}

.schedule-date {
  font-size: 12px;
  color: var(--text-tertiary);
  white-space: nowrap;
}

.schedule-empty {
  grid-column: 1 / -1;
  text-align: center;
  padding: 60px 20px;
  color: var(--text-tertiary);
  font-size: 14px;
}

.delete-confirm {
  text-align: center;
}

.delete-text {
  font-size: 14px;
  color: var(--text-secondary);
  margin: 0 0 24px 0;
}

.delete-actions {
  display: flex;
  justify-content: center;
  gap: 12px;
}

@keyframes fadeInUp {
  from {
    opacity: 0;
    transform: translateY(10px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

@media (max-width: 768px) {
  .schedule-grid {
    grid-template-columns: 1fr;
  }
}
</style>