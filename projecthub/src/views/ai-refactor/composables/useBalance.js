import { ref, computed, onMounted, onUnmounted } from 'vue'
import { aiService } from '@/services/dataService'

export function useBalance() {
  const balanceData = ref({ hasApiKey: false, isAvailable: false, totalBalance: 0 })
  const balanceLoading = ref(false)
  let balanceTimer = null

  const balanceText = computed(() => {
    if (balanceLoading.value) return '查询中...'
    if (!balanceData.value.hasApiKey) return '未配置'
    if (!balanceData.value.isAvailable) return '不可用'
    const b = balanceData.value.totalBalance
    if (b >= 1) return `¥${b.toFixed(2)}`
    return `¥${b.toFixed(4)}`
  })

  const balanceClass = computed(() => {
    if (balanceLoading.value) return 'loading'
    if (!balanceData.value.hasApiKey || !balanceData.value.isAvailable) return 'error'
    if (balanceData.value.totalBalance < 1) return 'warning'
    return 'ok'
  })

  const balanceTooltip = computed(() => {
    if (balanceLoading.value) return '正在查询余额...'
    if (!balanceData.value.hasApiKey) return '请在设置中配置 DeepSeek API Key'
    if (!balanceData.value.isAvailable) return '余额不足或不可用'
    return `DeepSeek 余额：¥${balanceData.value.totalBalance}`
  })

  async function fetchBalance() {
    balanceLoading.value = true
    try {
      const data = await aiService.getBalance()
      balanceData.value = data || { hasApiKey: false, isAvailable: false, totalBalance: 0 }
    } catch (e) {
      console.error('获取余额失败:', e)
      balanceData.value = { hasApiKey: false, isAvailable: false, totalBalance: 0 }
    } finally {
      balanceLoading.value = false
    }
  }

  function startPolling() {
    fetchBalance()
    balanceTimer = setInterval(fetchBalance, 10000)
  }

  function stopPolling() {
    if (balanceTimer) {
      clearInterval(balanceTimer)
      balanceTimer = null
    }
  }

  return {
    balanceData,
    balanceLoading,
    balanceText,
    balanceClass,
    balanceTooltip,
    fetchBalance,
    startPolling,
    stopPolling
  }
}
