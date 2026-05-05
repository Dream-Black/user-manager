import { ref, readonly } from 'vue'

const reminders = ref([])
const isConnected = ref(false)

export function useScheduleSSEState() {
  return {
    reminders: readonly(reminders),
    isConnected: readonly(isConnected)
  }
}

export function useScheduleSSE() {
  let eventSource = null

  const showDesktopNotification = (data) => {
    const title = data.Title || data.title || '日程提醒'
    const startTime = data.StartTime || data.startTime
    const timeText = startTime ? new Date(startTime).toLocaleTimeString('zh-CN', { hour: '2-digit', minute: '2-digit' }) : ''
    // 优先显示日子内容，如果没有则显示日程内容或时间
    const dayContent = data.DayContent || data.dayContent
    const content = data.Content || data.content
    const body = dayContent || content || `${timeText} 开始`
    const scheduleId = data.ScheduleId || data.scheduleId

    if (window.localBridge?.showNotification) {
      window.localBridge.showNotification({
        title,
        body,
        scheduleId
      })
      console.log('[SSE] 显示桌面通知:', title, '日程ID:', scheduleId)
    } else {
      console.log('[SSE] 桌面通知不可用（localBridge.showNotification 未找到）')
    }
  }

  const connect = () => {
    if (eventSource) {
      eventSource.close()
    }

    const baseURL = window.location.origin
    const url = `${baseURL}/api/schedules/stream`

    console.log('[SSE] 连接到:', url)

    eventSource = new EventSource(url)

    eventSource.onopen = () => {
      console.log('[SSE] 连接已打开')
      isConnected.value = true
    }

    eventSource.onmessage = (event) => {
      console.log('[SSE] 收到消息:', event.data)
      try {
        const data = JSON.parse(event.data)
        const type = data.type || data.Type

        if (type === 'reminder') {
          const normalizedData = {
            type: type,
            scheduleId: data.scheduleId || data.ScheduleId,
            title: data.title || data.Title,
            reminderTime: data.reminderTime || data.ReminderTime,
            startTime: data.startTime || data.StartTime,
            endTime: data.endTime || data.EndTime,
            content: data.content || data.Content,
            dayContent: data.dayContent || data.DayContent,
            dayStatus: data.dayStatus || data.DayStatus
          }
          reminders.value.push(normalizedData)
          showDesktopNotification(normalizedData)
          console.log('[SSE] 提醒:', normalizedData)
        }
      } catch (error) {
        console.error('[SSE] 解析消息失败:', error)
      }
    }

    eventSource.onerror = (error) => {
      console.error('[SSE] 错误:', error)
      isConnected.value = false
    }
  }

  const disconnect = () => {
    if (eventSource) {
      eventSource.close()
      eventSource = null
      isConnected.value = false
      console.log('[SSE] 已断开')
    }
  }

  const clearReminders = () => {
    reminders.value = []
  }

  return {
    reminders,
    isConnected,
    connect,
    disconnect,
    clearReminders
  }
}
