import { computed, ref } from 'vue'
import { userService } from '@/services/dataService'

const SIDEBAR_COLLAPSED_KEY = 'sidebarCollapsed'
const userState = ref(null)
const userLoading = ref(false)
const userLoaded = ref(false)
const sidebarCollapsed = ref(false)
let initialized = false

const readSidebarCollapsed = () => {
  if (typeof window === 'undefined') return false
  return window.localStorage.getItem(SIDEBAR_COLLAPSED_KEY) === 'true'
}

const writeSidebarCollapsed = (value) => {
  if (typeof window === 'undefined') return
  window.localStorage.setItem(SIDEBAR_COLLAPSED_KEY, String(value))
}

const ensureUserLoaded = async () => {
  if (userLoaded.value || userLoading.value) return
  userLoading.value = true

  try {
    userState.value = await userService.getCurrent()
  } catch (error) {
    console.error('获取用户信息失败:', error)
  } finally {
    userLoaded.value = true
    userLoading.value = false
  }
}

export function initLayoutState() {
  if (initialized) return
  initialized = true
  sidebarCollapsed.value = readSidebarCollapsed()
  ensureUserLoaded()
}

export function useLayoutState() {
  const user = computed(() => userState.value)
  const isSidebarCollapsed = computed(() => sidebarCollapsed.value)

  const toggleSidebarCollapsed = () => {
    sidebarCollapsed.value = !sidebarCollapsed.value
    writeSidebarCollapsed(sidebarCollapsed.value)
  }

  const refreshUser = async () => {
    userLoaded.value = false
    await ensureUserLoaded()
  }

  return {
    user,
    isSidebarCollapsed,
    toggleSidebarCollapsed,
    refreshUser,
  }
}
