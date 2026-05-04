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

const getStoredUser = () => {
  if (typeof window === 'undefined') return null
  const raw = window.localStorage.getItem('user')
  if (!raw) return null
  try {
    return JSON.parse(raw)
  } catch {
    return null
  }
}

const normalizeUser = (user) => {
  if (!user) return null
  return {
    id: user.id ?? user.Id ?? null,
    name: user.name ?? user.Name ?? '用户',
    email: user.email ?? user.Email ?? '',
    phone: user.phone ?? user.Phone ?? '',
    department: user.department ?? user.Department ?? '',
    role: user.role ?? user.Role ?? '',
    avatar: user.avatar ?? user.Avatar ?? '',
    theme: user.theme ?? user.Theme ?? 'light',
    density: user.density ?? user.Density ?? 'normal'
  }
}

const ensureUserLoaded = async () => {
  if (userLoaded.value || userLoading.value) return
  userLoading.value = true

  try {
    const storedUser = normalizeUser(getStoredUser())
    if (storedUser) {
      userState.value = storedUser
      return
    }
    userState.value = normalizeUser(await userService.getCurrent())
  } catch (error) {
    console.error('获取用户信息失败:', error)
    userState.value = null
  } finally {
    userLoaded.value = true
    userLoading.value = false
  }
}

export function clearAuthState() {
  if (typeof window !== 'undefined') {
    window.localStorage.removeItem('token')
    window.localStorage.removeItem('tokenExpiresAt')
    window.localStorage.removeItem('user')
  }
  userState.value = null
  userLoaded.value = false
}

export function setCurrentUser(user) {
  userState.value = normalizeUser(user)
  userLoaded.value = true
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
