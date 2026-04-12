import { defineStore } from 'pinia'
import { ref } from 'vue'
import { settingsApi } from '@/api'

export const useSettingsStore = defineStore('settings', () => {
  const settings = ref({
    deepSeekApiKey: '',
    deepSeekModel: 'deepseek-chat',
    workStartTime: '09:00:00',
    workEndTime: '18:00:00',
    lunchBreakStart: null,
    lunchBreakEnd: null,
    commuteHours: 1,
    currentJob: '',
    currentCompany: '',
    currentPlan: '',
    reminderTime: '08:30:00',
    reminderEnabled: true,
    themeMode: 'light'
  })
  const loading = ref(false)

  async function fetchSettings() {
    loading.value = true
    try {
      const data = await settingsApi.get()
      settings.value = data
    } finally {
      loading.value = false
    }
  }

  async function updateSettings(data) {
    const result = await settingsApi.update(data)
    settings.value = { ...settings.value, ...result }
    return result
  }

  return {
    settings,
    loading,
    fetchSettings,
    updateSettings
  }
})
