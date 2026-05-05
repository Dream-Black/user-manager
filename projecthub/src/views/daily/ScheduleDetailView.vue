<template>
  <div class="schedule-detail-page">
    <div class="page-header">
      <div class="header-actions">
        <t-button variant="outline" @click="handleBack">
          <template #icon><ArrowLeftIcon /></template>
          返回
        </t-button>
        <t-button theme="primary" @click="handleSave">
          <template #icon><SaveIcon /></template>
          保存
        </t-button>
      </div>
    </div>

    <div class="detail-container">
      <div class="form-section">
        <div class="form-item">
          <t-form-item label="标题" required>
            <t-input v-model="formData.title" placeholder="请输入日程标题" />
          </t-form-item>
        </div>

        <div class="form-item">
          <t-form-item label="日程详情">
            <t-textarea
              v-model="formData.content"
              placeholder="请输入日程详情"
              :rows="4"
            />
          </t-form-item>
        </div>

        <div class="form-item">
          <t-form-item label="日期范围">
            <t-date-range-picker
              v-model="formData.dateRange"
              clearable
              separator="至"
              style="width: 100%"
              @change="handleDateRangeChange"
            />
          </t-form-item>
        </div>
      </div>

      <div class="settings-section">
        <div class="section-header">
          <h3 class="section-title">日程设置</h3>
        </div>

        <div class="form-item">
          <t-form-item label="重复模式">
            <t-radio-group v-model="formData.repeatMode" direction="horizontal">
              <t-radio value="week">按周</t-radio>
              <t-radio value="month">按月</t-radio>
            </t-radio-group>
          </t-form-item>
        </div>

        <div v-if="formData.repeatMode === 'week'" class="week-settings">
          <div class="form-item">
            <t-form-item label="每周重复（可多选）">
              <t-check-tag-group
                v-model="formData.weekDays"
                :options="weekDayOptions"
                multiple
                @change="handleRepeatDaysChange"
              />
            </t-form-item>
          </div>
          <div class="time-range">
            <t-form-item label="时间">
              <t-time-picker v-model="formData.startTime" placeholder="开始时间" format="HH:mm" />
              <span class="time-separator">至</span>
              <t-time-picker v-model="formData.endTime" placeholder="结束时间" format="HH:mm" />
            </t-form-item>
          </div>
        </div>

        <div v-if="formData.repeatMode === 'month'" class="month-settings">
          <div class="form-item">
            <t-form-item label="每月日期（可多选）">
              <t-check-tag-group
                v-model="formData.monthDays"
                :options="monthDayOptions"
                multiple
                @change="handleRepeatDaysChange"
              />
            </t-form-item>
          </div>
          <div class="time-range">
            <t-form-item label="时间">
              <t-time-picker v-model="formData.startTime" placeholder="开始时间" format="HH:mm" />
              <span class="time-separator">至</span>
              <t-time-picker v-model="formData.endTime" placeholder="结束时间" format="HH:mm" />
            </t-form-item>
          </div>
        </div>
      </div>

      <div class="calendar-section">
        <div class="section-header">
          <h3 class="section-title">日程预览</h3>
        </div>
        <t-calendar :key="calendarKey" v-model="calendarValue" mode="month">
          <template #cell="{ day, date }">
            <div 
              class="calendar-cell-content"
              :class="{ clickable: getEventsForDay(date) }"
              @click="handleCellClick(date)"
            >
              <span class="cell-day">{{ day }}</span>
              <div v-if="getEventsForDay(date)" class="event-list">
                <div 
                  class="event-item"
                  :class="getDayClass(date)"
                >
                  {{ formData.startTime }} - {{ formData.endTime }}
                  <br />
                  {{ getDayContent(date) || formData.title || '日程' }}
                </div>
              </div>
            </div>
          </template>
        </t-calendar>
      </div>
    </div>

    <t-dialog
      v-model:visible="showDialog"
      header="编辑日程"
      width="400px"
      :footer="null"
    >
      <div class="dialog-content">
        <div class="dialog-date">{{ dialogDateStr }}</div>
        
        <t-textarea
          v-model="dialogContent"
          placeholder="请输入当天的日程内容"
          :rows="3"
        />

        <div class="dialog-status">
          <t-button 
            theme="success" 
            variant="outline" 
            @click="handleComplete"
          >
            完成
          </t-button>
          <t-button 
            theme="danger" 
            variant="outline"
            @click="handleSkipClick"
          >
            跳过
          </t-button>
          <t-button 
            variant="outline" 
            @click="handleReset"
          >
            重置
          </t-button>
        </div>

        <div v-if="showSkipReason" class="skip-reason">
          <t-textarea
            v-model="skipReason"
            placeholder="请输入10字以内的跳过原因"
            :maxlength="10"
          />
          <div class="skip-actions">
            <t-button variant="outline" @click="cancelSkip">取消</t-button>
            <t-button theme="danger" @click="confirmSkip">确认跳过</t-button>
          </div>
        </div>

        <div class="dialog-actions">
          <t-button variant="outline" @click="handleCloseDialog">关闭</t-button>
        </div>
      </div>
    </t-dialog>
  </div>
</template>

<script setup>
import { ref, watch } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { ArrowLeftIcon, SaveIcon } from 'tdesign-icons-vue-next'
import { scheduleApi } from '@/api'
import { MessagePlugin } from 'tdesign-vue-next'
import dayjs from 'dayjs'

const router = useRouter()
const route = useRoute()

const isEditing = ref(false)
const scheduleId = ref(null)

const formData = ref({
  title: '',
  content: '',
  dateRange: [],
  repeatMode: 'week',
  weekDays: ['1', '3', '5'],
  monthDays: [15],
  startTime: '09:00',
  endTime: '10:00',
  reminderEnabled: false
})

const calendarValue = ref(new Date())
const scheduleDaysMap = ref({})
const modifiedContents = ref({})

const showDialog = ref(false)
const showSkipReason = ref(false)
const dialogDate = ref(null)
const dialogDateStr = ref('')
const dialogContent = ref('')
const skipReason = ref('')

const weekDayOptions = [
  { value: '0', label: '周日' },
  { value: '1', label: '周一' },
  { value: '2', label: '周二' },
  { value: '3', label: '周三' },
  { value: '4', label: '周四' },
  { value: '5', label: '周五' },
  { value: '6', label: '周六' }
]

const monthDayOptions = Array.from({ length: 31 }, (_, i) => ({
  value: i + 1,
  label: `${i + 1}日`
}))

const getDateKey = (date) => {
  if (!date) return ''
  const d = dayjs(date)
  return `${d.year()}-${String(d.month() + 1).padStart(2, '0')}-${String(d.date()).padStart(2, '0')}`
}

const getEventsForDay = (currentDate) => {
  if (!currentDate) return false

  let weekMatch = false
  if (formData.value.repeatMode === 'week') {
    const dayOfWeek = String(dayjs(currentDate).day())
    weekMatch = formData.value.weekDays.includes(dayOfWeek)
  }

  let monthMatch = false
  if (formData.value.repeatMode === 'month') {
    const dayOfMonth = dayjs(currentDate).date()
    monthMatch = formData.value.monthDays.includes(dayOfMonth)
  }

  if (!weekMatch && !monthMatch) return false

  const range = formData.value.dateRange
  if (range && range.length === 2 && range[0] && range[1]) {
    const start = dayjs(range[0]).startOf('day')
    const end = dayjs(range[1]).endOf('day')
    const curr = dayjs(currentDate)
    if (curr.isBefore(start) || curr.isAfter(end)) return false
  }

  return true
}

const getDayContent = (date) => {
  const key = getDateKey(date)
  return modifiedContents.value[key] || scheduleDaysMap.value[key]?.content || ''
}

const getDayStatus = (date) => {
  const key = getDateKey(date)
  return scheduleDaysMap.value[key]?.status || 'pending'
}

const getDayClass = (date) => {
  const status = getDayStatus(date)
  if (status === 'completed') return 'event-completed'
  if (status === 'skipped') return 'event-skipped'
  return ''
}

const handleDateRangeChange = () => {
  updateCalendarKey()
}

const handleRepeatDaysChange = () => {
  updateCalendarKey()
}

let calendarKey = ref(0)
const updateCalendarKey = () => {
  calendarKey.value++
}

watch(() => [formData.value.repeatMode, JSON.stringify(formData.value.weekDays), JSON.stringify(formData.value.monthDays)], updateCalendarKey)

const loadSchedule = async (id) => {
  try {
    const data = await scheduleApi.get(id)
    formData.value.title = data.title || ''
    formData.value.content = data.content || ''
    formData.value.dateRange = data.startDate && data.endDate 
      ? [dayjs(data.startDate).toDate(), dayjs(data.endDate).toDate()] 
      : []
    formData.value.repeatMode = data.repeatMode || 'week'
    formData.value.startTime = data.startTime || '09:00'
    formData.value.endTime = data.endTime || '10:00'
    formData.value.reminderEnabled = data.reminderEnabled || false

    if (data.repeatMode === 'week') {
      try {
        formData.value.weekDays = JSON.parse(data.repeatDays || '[]')
      } catch {
        formData.value.weekDays = ['1', '3', '5']
      }
    } else {
      try {
        formData.value.monthDays = JSON.parse(data.repeatDays || '[15]').map(Number)
      } catch {
        formData.value.monthDays = [15]
      }
    }

    scheduleDaysMap.value = {}
    modifiedContents.value = {}

    const days = await scheduleApi.getDays(id)
    days.forEach(day => {
      const key = getDateKey(day.dayDate)
      scheduleDaysMap.value[key] = {
        id: day.id,
        content: day.content,
        status: day.status,
        skipReason: day.skipReason,
        dayDate: day.dayDate
      }
    })

    updateCalendarKey()
  } catch (error) {
    console.error('加载日程失败:', error)
    MessagePlugin.error('加载日程失败')
  }
}

const handleCellClick = (date) => {
  if (!getEventsForDay(date)) return

  dialogDate.value = date
  const d = dayjs(date)
  dialogDateStr.value = `${d.year()}年${d.month() + 1}月${d.date()}日`
  dialogContent.value = getDayContent(date)
  skipReason.value = ''
  showSkipReason.value = false
  showDialog.value = true
}

const handleCloseDialog = () => {
  const key = getDateKey(dialogDate.value)
  if (dialogContent.value !== (scheduleDaysMap.value[key]?.content || '')) {
    modifiedContents.value[key] = dialogContent.value
    updateCalendarKey()
  }
  showDialog.value = false
}

const handleComplete = async () => {
  const key = getDateKey(dialogDate.value)
  
  if (scheduleId.value) {
    try {
      const response = await scheduleApi.updateDayStatus(
        scheduleId.value,
        key,
        'completed',
        null
      )
      scheduleDaysMap.value[key] = {
        ...scheduleDaysMap.value[key],
        id: response.id,
        status: 'completed',
        content: dialogContent.value
      }
      if (modifiedContents.value[key]) {
        delete modifiedContents.value[key]
      }
      updateCalendarKey()
    } catch (error) {
      console.error('更新状态失败:', error)
      MessagePlugin.error('更新失败')
    }
  }
  
  showDialog.value = false
}

const handleSkipClick = () => {
  showSkipReason.value = true
}

const cancelSkip = () => {
  showSkipReason.value = false
  skipReason.value = ''
}

const confirmSkip = async () => {
  if (!skipReason.value.trim()) {
    MessagePlugin.warning('请输入跳过原因')
    return
  }
  if (skipReason.value.length > 10) {
    MessagePlugin.warning('跳过原因不能超过10字')
    return
  }

  const key = getDateKey(dialogDate.value)
  
  if (scheduleId.value) {
    try {
      const response = await scheduleApi.updateDayStatus(
        scheduleId.value,
        key,
        'skipped',
        skipReason.value
      )
      scheduleDaysMap.value[key] = {
        ...scheduleDaysMap.value[key],
        id: response.id,
        status: 'skipped',
        skipReason: skipReason.value,
        content: dialogContent.value
      }
      if (modifiedContents.value[key]) {
        delete modifiedContents.value[key]
      }
      updateCalendarKey()
    } catch (error) {
      console.error('更新状态失败:', error)
      MessagePlugin.error('更新失败')
    }
  }
  
  showSkipReason.value = false
  showDialog.value = false
}

const handleReset = async () => {
  const key = getDateKey(dialogDate.value)
  
  if (scheduleId.value) {
    try {
      const response = await scheduleApi.updateDayStatus(
        scheduleId.value,
        key,
        'pending',
        null
      )
      scheduleDaysMap.value[key] = {
        ...scheduleDaysMap.value[key],
        id: response.id,
        status: 'pending',
        skipReason: null,
        content: ''
      }
      if (modifiedContents.value[key]) {
        delete modifiedContents.value[key]
      }
      updateCalendarKey()
    } catch (error) {
      console.error('重置状态失败:', error)
      MessagePlugin.error('重置失败')
    }
  }
  
  showDialog.value = false
}

const handleBack = () => {
  router.push('/schedule')
}

const handleSave = async () => {
  if (!formData.value.title) {
    MessagePlugin.warning('请输入日程标题')
    return
  }

  try {
    const startDate = formData.value.dateRange[0] ? dayjs(formData.value.dateRange[0]) : dayjs()
    const endDate = formData.value.dateRange[1] ? dayjs(formData.value.dateRange[1]) : dayjs()

    const data = {
      title: formData.value.title,
      content: formData.value.content,
      startDate: startDate.format('YYYY-MM-DD'),
      endDate: endDate.format('YYYY-MM-DD'),
      repeatMode: formData.value.repeatMode,
      repeatDays: formData.value.repeatMode === 'week' 
        ? JSON.stringify(formData.value.weekDays) 
        : JSON.stringify(formData.value.monthDays),
      startTime: formData.value.startTime,
      endTime: formData.value.endTime,
      reminderEnabled: formData.value.reminderEnabled
    }

    if (isEditing.value && scheduleId.value) {
      await scheduleApi.update(scheduleId.value, data)
    } else {
      const result = await scheduleApi.create(data)
      scheduleId.value = result.id
      isEditing.value = true
    }

    await scheduleApi.generateDays(scheduleId.value, {
      startDate: startDate.toDate(),
      endDate: endDate.toDate(),
      repeatMode: formData.value.repeatMode,
      repeatDays: data.repeatDays
    })

    for (const [key, content] of Object.entries(modifiedContents.value)) {
      await scheduleApi.updateDayContent(scheduleId.value, key, content)
    }

    modifiedContents.value = {}
    MessagePlugin.success('保存成功')
  } catch (error) {
    console.error('保存失败:', error)
    MessagePlugin.error('保存失败')
  }
}

const init = async () => {
  const id = route.params.id
  if (id && id !== 'new') {
    scheduleId.value = parseInt(id)
    isEditing.value = true
    await loadSchedule(scheduleId.value)
  }
}

init()
</script>

<style scoped>
.schedule-detail-page {
  margin: 0 auto;
}

.page-header {
  display: flex;
  justify-content: flex-start;
  align-items: center;
  margin-bottom: 24px;
  gap: 12px;
  animation: fadeInUp 0.5s ease;
}

.header-actions {
  display: flex;
  justify-content: space-between;
  gap: 12px;
  width: 100%;
}

.detail-container {
  background: var(--bg-container);
  border-radius: var(--radius-xl);
  padding: 24px;
  animation: fadeInUp 0.5s ease 0.1s backwards;
}

.form-section,
.settings-section,
.calendar-section {
  margin-bottom: 24px;
}

.section-header {
  margin-bottom: 16px;
}

.section-title {
  font-size: 16px;
  font-weight: 600;
  color: var(--text-primary);
  margin: 0;
}

.form-item {
  margin-bottom: 16px;
}

.time-range {
  display: flex;
  align-items: center;
  gap: 12px;
}

.time-separator {
  color: var(--text-secondary);
}

.week-settings,
.month-settings {
  padding-left: 24px;
}

.calendar-cell-content {
  width: 100%;
  height: 100%;
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.calendar-cell-content.clickable {
  cursor: pointer;
}

.calendar-cell-content.clickable:hover {
  background: var(--primary-lighter);
  border-radius: 4px;
}

.cell-day {
  font-size: 12px;
  color: var(--text-secondary);
}

.event-list {
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.event-item {
  font-size: 10px;
  color: var(--primary-color);
  background: var(--primary-lighter);
  padding: 2px 4px;
  border-radius: 4px;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.event-item.event-completed {
  color: #00a870;
  background: #e8f8f0;
}

.event-item.event-skipped {
  color: #e34d59;
  background: #fef1f2;
}

.dialog-content {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.dialog-date {
  font-size: 14px;
  font-weight: 600;
  color: var(--text-primary);
}

.dialog-status {
  display: flex;
  gap: 8px;
}

.skip-reason {
  display: flex;
  flex-direction: column;
  gap: 8px;
  padding-top: 8px;
  border-top: 1px solid var(--border-light);
}

.skip-actions {
  display: flex;
  gap: 8px;
  justify-content: flex-end;
}

.dialog-actions {
  display: flex;
  gap: 8px;
  justify-content: flex-end;
  padding-top: 8px;
  border-top: 1px solid var(--border-light);
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
</style>