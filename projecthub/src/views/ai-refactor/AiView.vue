<template>
  <div class="ai-page">
    <!-- 左侧对话列表 -->
    <ConvSidebar
      :conversations="conversations"
      :current-conv-id="currentConvId"
      :search-query="searchQuery"
      @update:search-query="searchQuery = $event"
      @search="loadConversations"
      @new-conversation="onNewConversation"
      @switch="onSwitchConversation"
      @delete="onDeleteConversation"
      @toggle-pin="togglePin"
      @toggle-archive="toggleArchive"
    />

    <!-- 右侧聊天区域 -->
    <main class="chat-main card-shell">
      <!-- 顶栏 -->
      <ChatTopbar
        :current-title="currentTitle"
        v-model:selected-model="selectedModel"
        v-model:deep-think="deepThink"
        :model-options="modelOptions"
        :balance-text="balanceText"
        :balance-class="balanceClass"
        :balance-tooltip="balanceTooltip"
      />

      <!-- 消息区域 -->
      <MessageList
        ref="messageListRef"
        :messages="messages"
        :streaming="streaming"
        :streaming-msg-id="streamingMsgId"
        :terminal-result="terminalResult"
        :terminal-result-collapsed="terminalResultCollapsed"
        :quick-prompts="quickPrompts"
        @quick-prompt="onQuickPrompt"
        @edit-message="startEditMessage"
        @regenerate="regenerateFromMessage"
        @cancel-draft="cancelDraft"
        @confirm-draft="openActionDraft"
        @update:terminal-result-collapsed="terminalResultCollapsed = $event"
      />

      <!-- 输入区域 -->
      <ChatInputArea
        ref="inputAreaRef"
        v-model="inputText"
        :pending-attachments="pendingAttachments"
        :disabled="streaming || !currentConvId"
        @send="onSendMessage"
        @file-select="handleFileSelect"
        @remove-attachment="removeAttachment"
      />
    </main>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, onUnmounted, watch, nextTick } from 'vue'
import 'highlight.js/styles/github-dark.css'

// 子组件
import ConvSidebar from './components/ConvSidebar.vue'
import ChatTopbar from './components/ChatTopbar.vue'
import MessageList from './components/MessageList.vue'
import ChatInputArea from './components/ChatInputArea.vue'

// composables
import { useConversations } from './composables/useConversations'
import { useMessages } from './composables/useMessages'
import { useBalance } from './composables/useBalance'
import { useTerminal } from './composables/useTerminal'

// ---- DOM refs ----
const messageListRef = ref(null)
const inputAreaRef = ref(null)

// 通过 MessageList 组件暴露的 containerRef 访问滚动容器
const messagesContainer = computed(() => messageListRef.value?.containerRef)

// ---- 模型配置 ----
const selectedModel = ref('deepseek-v4-flash')
const deepThink = ref(false)
const modelOptions = [
  { label: 'V4 Flash', value: 'deepseek-v4-flash' },
  { label: 'V4 Pro', value: 'deepseek-v4-pro' }
]

const quickPrompts = [
  '今天该干什么？',
  '帮我分析项目进度',
  '查看所有项目状态',
  '我有多少待办任务？',
  '生成本周工作总结'
]

// ---- composables ----
const {
  conversations, currentConvId, searchQuery, currentTitle,
  loadConversations, createNewConversation, deleteConversation,
  togglePin, toggleArchive
} = useConversations()

const {
  balanceText, balanceClass, balanceTooltip,
  startPolling, stopPolling
} = useBalance()

// messages composable 需要 messagesContainer 和 checkAndExecuteTerminalCommand
// 先声明 messages composable，terminal composable 依赖它的 messages/currentConvId
// 所以先初始化 messages composable，再传给 terminal

const {
  messages, streaming, streamingMsgId,
  pendingAttachments, inputText, editingMessage,
  renderMarkdown, getKindLabel,
  scrollToBottom, scrollToMessage,
  loadMessages, reloadMessages, sendMessage,
  isTextFile, handleFileSelect, removeAttachment,
  startEditMessage, regenerateFromMessage,
  openActionDraft, cancelDraft, cancelPendingAction, confirmPendingAction,
  pendingAction, pendingActionVisible, editableMessageId,
  confirmingPendingAction, actionFeedback, actionFeedbackType
} = useMessages({
  currentConvId,
  messagesContainer,
  // 先用占位，useTerminal 初始化后再覆盖
  checkAndExecuteTerminalCommand: null
})

const {
  terminalResult,
  terminalResultCollapsed,
  continueStreaming,
  checkAndExecuteTerminalCommand
} = useTerminal({
  messages,
  currentConvId,
  scrollToBottom,
  reloadMessages
})

// 将 checkAndExecuteTerminalCommand 注入到 messages composable（workaround：直接在 sendMessage 调用时传入）

// ---- 初始化 ----
onMounted(async () => {
  await loadConversations()
  if (conversations.value.length > 0) {
    await onSwitchConversation(conversations.value[0].id)
  }
  startPolling()
})

onUnmounted(() => {
  stopPolling()
})

// ---- 事件处理 ----
async function onNewConversation() {
  const conv = await createNewConversation()
  if (conv) await onSwitchConversation(conv.id)
}

async function onSwitchConversation(id) {
  currentConvId.value = id
  await loadMessages(id)
}

async function onDeleteConversation(id) {
  const deleted = await deleteConversation(id)
  if (deleted && currentConvId.value === id) {
    currentConvId.value = null
    messages.value = []
    if (conversations.value.length > 0) {
      await onSwitchConversation(conversations.value[0].id)
    }
  }
}

async function onQuickPrompt(prompt) {
  if (!currentConvId.value) {
    const conv = await createNewConversation()
    if (!conv) return
    await onSwitchConversation(conv.id)
  }
  inputText.value = prompt
  await onSendMessage()
}

async function onSendMessage() {
  // 发送前重置输入框高度
  inputAreaRef.value?.resetHeight()
  await sendMessage({
    deepThink: deepThink.value,
    selectedModel: selectedModel.value,
    onConvsReload: loadConversations,
    checkAndExecuteTerminalCommand
  })
}
</script>

<style scoped>
.ai-page {
  display: flex;
  gap: var(--space-6);
  max-height: calc(100vh - 200px);
}

.card-shell {
  background: var(--bg-card-solid);
  border: 1px solid var(--border-light);
  border-radius: var(--radius-2xl);
  box-shadow: var(--shadow-card);
}

.chat-main {
  flex: 1;
  display: flex;
  flex-direction: column;
  width: 0;
  overflow: hidden;
}

@media (max-width: 1200px) {
  .ai-page { gap: var(--space-4); }
}

@media (max-width: 900px) {
  .ai-page {
    flex-direction: column;
    height: auto;
    min-height: calc(100vh - var(--header-height, 64px));
  }
  .chat-main { min-height: 70vh; }
}
</style>
