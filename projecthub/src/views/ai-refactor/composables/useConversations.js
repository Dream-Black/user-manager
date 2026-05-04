import { ref, computed } from 'vue'
import { aiService } from '@/services/dataService'

export function useConversations() {
  const conversations = ref([])
  const currentConvId = ref(null)
  const searchQuery = ref('')

  const currentTitle = computed(() => {
    const conv = conversations.value.find(c => c.id === currentConvId.value)
    return conv ? conv.title : '选择一个对话开始'
  })

  async function loadConversations() {
    try {
      const data = await aiService.getConversations({ search: searchQuery.value || undefined })
      conversations.value = data || []
    } catch (e) {
      console.error('加载对话列表失败:', e)
    }
  }

  async function createNewConversation() {
    try {
      const conv = await aiService.createConversation('新对话')
      conversations.value.unshift(conv)
      return conv
    } catch (e) {
      console.error('创建对话失败:', e)
      return null
    }
  }

  async function deleteConversation(id) {
    try {
      await aiService.deleteConversation(id)
      conversations.value = conversations.value.filter(c => c.id !== id)
      return true
    } catch (e) {
      console.error('删除对话失败:', e)
      return false
    }
  }

  async function togglePin(conv) {
    try {
      await aiService.pinConversation(conv.id, !conv.isPinned)
      await loadConversations()
    } catch (e) {
      console.error('置顶失败:', e)
    }
  }

  async function toggleArchive(conv) {
    try {
      await aiService.archiveConversation(conv.id, !conv.isArchived)
      await loadConversations()
    } catch (e) {
      console.error('归档失败:', e)
    }
  }

  function formatTime(dateStr) {
    if (!dateStr) return ''
    const d = new Date(dateStr)
    const now = new Date()
    const diff = now - d
    if (diff < 60000) return '刚刚'
    if (diff < 3600000) return Math.floor(diff / 60000) + '分钟前'
    if (diff < 86400000) return Math.floor(diff / 3600000) + '小时前'
    const month = String(d.getMonth() + 1).padStart(2, '0')
    const day = String(d.getDate()).padStart(2, '0')
    return `${month}-${day}`
  }

  return {
    conversations,
    currentConvId,
    searchQuery,
    currentTitle,
    loadConversations,
    createNewConversation,
    deleteConversation,
    togglePin,
    toggleArchive,
    formatTime
  }
}
