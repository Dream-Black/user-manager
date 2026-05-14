import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { financeAccountApi, financeCategoryApi } from '@/api'

export const useFinanceStore = defineStore('finance', () => {
  // 状态
  const accounts = ref([])
  const categories = ref([])
  const currentAccount = ref(null)
  const loading = ref(false)

  // 计算属性
  const defaultAccount = computed(() =>
    accounts.value.find(acc => acc.isDefaultExpense)
  )

  const totalBalance = computed(() =>
    accounts.value.reduce((sum, acc) => sum + acc.balance, 0)
  )

  const accountOptions = computed(() =>
    accounts.value.map(acc => ({
      value: acc.id,
      label: `${acc.name} (¥${acc.balance.toLocaleString('zh-CN', { minimumFractionDigits: 2 })})`,
      icon: acc.icon,
      color: acc.color
    }))
  )

  const categoryOptions = computed(() =>
    categories.value.map(cat => ({
      value: cat.id,
      label: cat.name,
      icon: cat.icon,
      color: cat.color
    }))
  )

  // 方法
  async function fetchAccounts() {
    loading.value = true
    try {
      const res = await financeAccountApi.list()
      // API 返回 { success, data: [...] }，拦截器返回此对象，需提取 data
      const data = res?.data || res || []
      accounts.value = data
      // 设置当前默认账户
      const defaultAcc = data.find(acc => acc.isDefaultExpense)
      if (defaultAcc) {
        currentAccount.value = defaultAcc
      }
      return data
    } finally {
      loading.value = false
    }
  }

  async function fetchCategories() {
    try {
      const res = await financeCategoryApi.list()
      // API 返回 { success, data: [...] }，拦截器返回此对象，需提取 data
      const data = res?.data || res || []
      categories.value = data
      return data
    } catch (error) {
      console.error('获取分类失败:', error)
      return []
    }
  }

  async function refetch() {
    await Promise.all([fetchAccounts(), fetchCategories()])
  }

  function setCurrentAccount(account) {
    currentAccount.value = account
  }

  return {
    accounts,
    categories,
    currentAccount,
    loading,
    defaultAccount,
    totalBalance,
    accountOptions,
    categoryOptions,
    fetchAccounts,
    fetchCategories,
    refetch,
    setCurrentAccount
  }
})
