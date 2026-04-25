<template>
  <div class="ai-page page-shell">
    <aside class="conv-sidebar card-shell">
      <div class="conv-header">
        <div>
          <p class="section-kicker">智能助手</p>
          <h3 class="section-title">对话列表</h3>
        </div>
        <button class="btn-new" @click="createNewConversation">
          <Plus :size="18" />
          <span>新建</span>
        </button>
      </div>
      <div class="conv-tools">
        <div class="search-shell">
          <Search :size="16" class="search-icon" />
          <input
            v-model="searchQuery"
            class="conv-search"
            placeholder="搜索对话标题或摘要..."
            @input="loadConversations"
          />
        </div>
      </div>
      <div class="conv-list">
        <div
          v-for="conv in conversations"
          :key="conv.id"
          class="conv-item"
          :class="{ active: conv.id === currentConvId }"
          @click="switchConversation(conv.id)"
        >
          <div class="conv-info">
            <span class="conv-name">{{ conv.title }}</span>
            <span class="conv-time">{{ formatTime(conv.updatedAt) }}</span>
            <div class="conv-badges">
              <span v-if="conv.isPinned" class="mini-badge">置顶</span>
              <span v-if="conv.isArchived" class="mini-badge muted">已归档</span>
            </div>
          </div>
          <div class="conv-actions">
            <button
              class="icon-action-btn"
              :class="{ active: conv.isPinned }"
              :title="conv.isPinned ? '取消置顶' : '置顶对话'"
              @click.stop="togglePin(conv)"
            >
              <PinIcon />
            </button>
            <button
              class="icon-action-btn"
              :class="{ active: conv.isArchived }"
              :title="conv.isArchived ? '取消归档' : '归档对话'"
              @click.stop="toggleArchive(conv)"
            >
              <FolderOffIcon />
            </button>
            <button
              class="icon-action-btn danger"
              title="删除对话"
              @click.stop="deleteConversation(conv.id)"
            >
              <Trash2 />
            </button>
          </div>
        </div>
        <div v-if="conversations.length === 0" class="conv-empty">
          暂无对话，点击上方按钮创建
        </div>
      </div>
    </aside>

    <main class="chat-main card-shell">
      <div class="chat-topbar">
        <div class="topbar-left">
          <p class="section-kicker">AI 工作台</p>
          <span class="current-title">{{ currentTitle }}</span>
        </div>
        <div class="topbar-right">
          <label class="deep-think-toggle">
            <t-switch v-model="deepThink" size="small" />
            <span class="toggle-label">深度思考</span>
          </label>
        </div>
      </div>

      <div class="chat-messages" ref="messagesContainer">
        <div v-if="messages.length === 0 && !streaming" class="chat-welcome">
          <div class="welcome-card">
            <div class="welcome-icon">
              <svg viewBox="0 0 48 48" fill="none" width="64" height="64">
                <rect width="48" height="48" rx="12" fill="url(#welcomeGrad)" />
                <path d="M16 24L20 28L32 20" stroke="white" stroke-width="3" stroke-linecap="round" stroke-linejoin="round" />
                <defs><linearGradient id="welcomeGrad" x1="0" y1="0" x2="48" y2="48"><stop stop-color="#3B82F6"/><stop offset="1" stop-color="#60A5FA"/></linearGradient></defs>
              </svg>
            </div>
            <h2>AI 工作助手</h2>
            <p>我可以帮你查询项目进度、今日任务、数据分析等</p>
            <div class="quick-prompts">
              <button v-for="p in quickPrompts" :key="p" class="prompt-chip" @click="sendQuickPrompt(p)">{{ p }}</button>
            </div>
          </div>
        </div>

        <div
          v-for="msg in messages"
          :key="msg._id"
          class="message-wrapper"
          :class="msg.role"
        >
          <div class="message-bubble">
            <div v-if="msg.filesJson && msg.filesJson.length" class="msg-attachments">
              <div v-for="att in msg.filesJson" :key="att.name" class="attachment-tag">
                <Paperclip :size="12" />
                <span>{{ att.name }}</span>
              </div>
            </div>

            <div v-if="msg.reasoningContent" class="reasoning-section">
              <details class="reasoning-details">
                <summary class="reasoning-summary">
                  <Brain :size="14" />
                  <span>思考过程</span>
                </summary>
                <div class="reasoning-content">{{ msg.reasoningContent }}</div>
              </details>
            </div>

            <div class="msg-content" v-html="renderMarkdown(msg.content || '')"></div>

            <div v-if="msg.toolCalls" class="tool-call-badge">
              <Database :size="12" />
              <span>已查询数据库</span>
            </div>

            <div v-if="msg.actionDraft" class="action-draft-card">
              <div class="action-draft-head">
                <div>
                  <span class="action-draft-type">{{ getKindLabel(msg.actionDraft.kind) }}</span>
                  <h4 class="action-draft-title">{{ msg.actionDraft.title }}</h4>
                </div>
                <button class="btn-mini action-confirm-btn" @click="openActionDraft(msg)">确认</button>
              </div>
              <div class="action-draft-preview">{{ msg.actionDraft.preview }}</div>
              <div class="action-draft-meta">
                <span class="mini-badge muted">{{ msg.actionDraft.mode === 'create' ? '新增' : '更新' }}</span>
                <span class="action-draft-target">{{ msg.actionDraft.targetLabel }}</span>
              </div>
            </div>

            <div v-if="msg.role === 'assistant'" class="message-toolbar">
              <button
                class="icon-action-btn"
                title="重新生成回复"
                @click="regenerateFromMessage(msg)"
              >
                <RefreshIcon />
              </button>
            </div>
            <div v-else-if="msg.role === 'user'" class="message-toolbar">
              <button
                class="icon-action-btn"
                title="编辑后重发"
                @click="startEditMessage(msg)"
              >
                <Edit1Icon />
              </button>
            </div>

            <span v-if="streaming && msg._id === streamingMsgId" class="typing-cursor">▊</span>
          </div>
        </div>

        <div v-if="streaming && messages.length === 0" class="message-wrapper assistant">
          <div class="message-bubble">
            <span class="thinking-dots">
              <span></span><span></span><span></span>
            </span>
          </div>
        </div>
      </div>

      <div class="chat-input-area">
        <div v-if="pendingActionVisible && pendingAction" class="action-confirm-panel">
          <div class="action-confirm-header">
            <div>
              <p class="section-kicker">待确认写入</p>
              <h4 class="action-confirm-title">{{ pendingAction.title }}</h4>
            </div>
            <button class="icon-action-btn danger" @click="cancelPendingAction"><X /></button>
          </div>
          <div class="action-confirm-body">
            <div class="action-confirm-diff">
              <div class="diff-column">
                <span class="diff-label">当前内容</span>
                <div class="diff-box muted">{{ pendingAction.before || '无' }}</div>
              </div>
              <div class="diff-column">
                <span class="diff-label">建议内容</span>
                <div class="diff-box">{{ pendingAction.after || '无' }}</div>
              </div>
            </div>
            <div class="action-confirm-meta">
              <span class="mini-badge">{{ getKindLabel(pendingAction.kind) }}</span>
              <span class="action-confirm-tip">先预览再确认执行，避免误写入</span>
            </div>
            <div v-if="actionFeedback" class="action-feedback" :class="actionFeedbackType">
              {{ actionFeedback }}
            </div>
          </div>
          <div class="action-confirm-footer">
            <button class="btn-secondary" :disabled="confirmingPendingAction" @click="cancelPendingAction">取消</button>
            <button class="btn-primary" :disabled="confirmingPendingAction" @click="confirmPendingAction">
              {{ confirmingPendingAction ? '确认中...' : '确认执行' }}
            </button>
          </div>
        </div>

        <div v-if="pendingAttachments.length" class="attachment-preview-bar">
          <div v-for="(att, idx) in pendingAttachments" :key="idx" class="att-preview-item">
            <span class="att-icon">
              <FileText v-if="isTextFile(att.name)" :size="14" />
              <Image v-else :size="14" />
            </span>
            <span class="att-name">{{ att.name }}</span>
            <button class="att-remove" @click="removeAttachment(idx)">
              <X :size="12" />
            </button>
          </div>
        </div>

        <div class="input-row">
          <label class="upload-btn" title="上传附件">
            <Paperclip :size="20" />
            <input
              type="file"
              hidden
              accept=".jpg,.jpeg,.png,.gif,.webp,.txt,.md,.json,.xml,.csv"
              multiple
              @change="handleFileSelect"
            />
          </label>
          <textarea
            ref="inputRef"
            v-model="inputText"
            class="chat-input"
            placeholder="输入消息，Enter 发送，Shift+Enter 换行..."
            rows="1"
            :disabled="streaming || !currentConvId"
            @keydown="handleKeydown"
            @input="autoResize"
          ></textarea>
          <button
            class="send-btn"
            :disabled="!inputText.trim() || streaming || !currentConvId"
            @click="sendMessage"
          >
            <Send :size="18" />
          </button>
        </div>
      </div>
    </main>
  </div>
</template>

<script setup>
import { ref, nextTick, onMounted, watch, computed } from 'vue'
import { Plus, Trash2, Send, Paperclip, X, FileText, Image, Brain, Database, Search } from 'lucide-vue-next'
import { Edit1Icon, RefreshIcon, PinIcon, FolderOffIcon } from 'tdesign-icons-vue-next'
import { aiService, projectService, taskService } from '@/services/dataService'
import { marked } from 'marked'
import hljs from 'highlight.js'
import 'highlight.js/styles/github-dark.css'

// ============ 状态 ============
const conversations = ref([])
const messages = ref([])
const currentConvId = ref(null)
const inputText = ref('')
const deepThink = ref(false)
const streaming = ref(false)
const streamingMsgId = ref(null)
const pendingAttachments = ref([])
const messagesContainer = ref(null)
const inputRef = ref(null)
const searchQuery = ref('')
const pendingAction = ref(null)
const pendingActionVisible = ref(false)
const editableMessageId = ref(null)
const editingMessage = ref(null)
const confirmingPendingAction = ref(false)
const actionFeedback = ref('')
const actionFeedbackType = ref('')

const currentTitle = computed(() => {
  const conv = conversations.value.find(c => c.id === currentConvId.value)
  return conv ? conv.title : '选择一个对话开始'
})

const quickPrompts = [
  '今天该干什么？',
  '帮我分析项目进度',
  '查看所有项目状态',
  '我有多少待办任务？',
  '生成本周工作总结'
]

// 配置 marked
marked.setOptions({
  breaks: true,
  gfm: true,
  highlight: (code, lang) => {
    if (lang && hljs.getLanguage(lang)) {
      return hljs.highlight(code, { language: lang }).value
    }
    return hljs.highlightAuto(code).value
  }
})

// ============ 初始化 ============
onMounted(async () => {
  await loadConversations()
  if (conversations.value.length > 0) {
    await switchConversation(conversations.value[0].id)
  }
})

// ============ 对话管理 ============
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
    await switchConversation(conv.id)
  } catch (e) {
    console.error('创建对话失败:', e)
  }
}

async function switchConversation(id) {
  currentConvId.value = id
  try {
    const msgs = await aiService.getConversationMessages(id)
    console.log('[AI] switchConversation loaded', { conversationId: id, count: msgs?.length || 0, raw: msgs })
    messages.value = (msgs || []).map((m, i) => {
      const draft = m.role === 'assistant' ? extractDraftMessage(m) : null
      console.log('[AI] message mapped', {
        id: m.id,
        role: m.role,
        hasAttachments: !!m.attachments,
        hasFilesJson: !!m.filesJson,
        hasToolCalls: !!m.toolCalls,
        hasDraft: !!draft,
        draft
      })
      return {
        ...m,
        _id: `${m.id}_${i}`,
        filesJson: m.filesJson,
        attachments: m.attachments,
        toolCalls: m.toolCalls ? safeParse(m.toolCalls) : null,
        actionDraft: draft
      }
    })
    syncLatestDraft()
    await nextTick()
    scrollToBottom()
  } catch (e) {
    console.error('加载消息失败:', e)
    messages.value = []
  }
}

async function deleteConversation(id) {
  try {
    await aiService.deleteConversation(id)
    conversations.value = conversations.value.filter(c => c.id !== id)
    if (currentConvId.value === id) {
      currentConvId.value = null
      messages.value = []
      if (conversations.value.length > 0) {
        await switchConversation(conversations.value[0].id)
      }
    }
  } catch (e) {
    console.error('删除对话失败:', e)
  }
}

// ============ 消息发送 ============
function sendQuickPrompt(prompt) {
  if (!currentConvId.value) {
    createNewConversation().then(() => {
      inputText.value = prompt
      sendMessage()
    })
  } else {
    inputText.value = prompt
    sendMessage()
  }
}

function handleKeydown(e) {
  if (e.key === 'Enter' && !e.shiftKey) {
    e.preventDefault()
    sendMessage()
  }
}

function autoResize() {
  const el = inputRef.value
  if (!el) return
  el.style.height = 'auto'
  el.style.height = Math.min(el.scrollHeight, 120) + 'px'
}

async function sendMessage() {
  const text = inputText.value.trim()
  if (!text || streaming.value || !currentConvId.value) return

  const userFiles = [...pendingAttachments.value]
  const isEditing = !!editingMessage.value?.id

  if (isEditing) {
    try {
      await aiService.updateMessage(currentConvId.value, editingMessage.value.id, { content: text })
      editingMessage.value = null
    } catch (e) {
      console.error('编辑消息失败:', e)
      return
    }
  } else {
    // 添加用户消息到本地
    const userMsg = {
      _id: 'user_' + Date.now(),
      role: 'user',
      content: text,
      filesJson: [...userFiles],
      reasoningContent: null,
      toolCalls: null
    }
    messages.value.push(userMsg)
  }

  inputText.value = ''
  if (inputRef.value) {
    inputRef.value.style.height = 'auto'
  }

  const attachments = [...pendingAttachments.value]
  pendingAttachments.value = []

  await nextTick()
  scrollToBottom()

  // 创建 AI 占位消息
  const aiMsgId = 'ai_' + Date.now()
  const aiMsg = {
    _id: aiMsgId,
    role: 'assistant',
    content: '',
    reasoningContent: '',
    toolCalls: null,
    filesJson: null
  }
  messages.value.push(aiMsg)
  streaming.value = true
  streamingMsgId.value = aiMsgId

  try {
    const response = await aiService.chatStream(
      currentConvId.value,
      text,
      deepThink.value,
      userFiles.length > 0 ? userFiles.map(a => ({ name: a.name, type: a.type })) : null
    )

    if (!response.ok) {
      throw new Error(`HTTP ${response.status}`)
    }

    const reader = response.body.getReader()
    const decoder = new TextDecoder()
    let buffer = ''

    while (true) {
      const { done, value } = await reader.read()
      if (done) break

      buffer += decoder.decode(value, { stream: true })
      let eventEnd
      while ((eventEnd = buffer.indexOf('\n\n')) !== -1) {
        const rawEvent = buffer.slice(0, eventEnd).trim()
        buffer = buffer.slice(eventEnd + 2)
        if (!rawEvent) continue

        const dataLine = rawEvent
          .split(/\r?\n/)
          .find(line => line.startsWith('data: '))
        if (!dataLine) continue

        const dataStr = dataLine.substring(6).trim()
        if (!dataStr) continue

        const data = safeParse(dataStr)
        if (!data) continue

        const msg = messages.value.find(m => m._id === aiMsgId)
        if (!msg) continue

        switch (data.type) {
          case 'content':
            msg.content += data.content
            break
          case 'reasoning':
            msg.reasoningContent += data.content
            break
          case 'tool_call':
            msg.content += '\n\n🔍 *正在查询数据库...*'
            break
          case 'tool_result':
            msg.content = msg.content.replace(/\n\n🔍 \*正在查询数据库...\*/g, '')
            break
          case 'error':
            msg.content = data.content
            break
          case 'done':
            break
        }
        await nextTick()
        scrollToBottom()
      }
    }
  } catch (e) {
    const msg = messages.value.find(m => m._id === aiMsgId)
    if (msg) {
      msg.content = `❌ 请求失败: ${e?.message || '未知错误'}`
    }
    console.error('流式聊天失败:', e)
  } finally {
    streaming.value = false
    streamingMsgId.value = null

    // 清理 AI 消息中的工具调用标记
    const msg = messages.value.find(m => m._id === aiMsgId)
    if (msg) {
      msg.content = msg.content.replace(/\n\n🔍 \*正在查询数据库...\*/g, '')
    }

    // 重新加载消息以获取完整的服务端数据
    if (currentConvId.value) {
      await reloadMessages()
    }
  }
}

async function reloadMessages() {
  try {
    const msgs = await aiService.getConversationMessages(currentConvId.value)
    messages.value = (msgs || []).map((m, i) => {
      const draft = m.role === 'assistant' ? extractDraftMessage(m) : null
      return {
        ...m,
        _id: `${m.id}_${i}`,
        filesJson: m.filesJson,
        attachments: m.attachments,
        toolCalls: m.toolCalls ? safeParse(m.toolCalls) : null,
        actionDraft: draft
      }
    })
    syncLatestDraft()
    await nextTick()
    scrollToBottom()
    await loadConversations()
  } catch (e) {
    console.error('重新加载消息失败:', e)
  }
}

// ============ 附件处理 ============
function handleFileSelect(e) {
  const files = Array.from(e.target.files)
  for (const file of files) {
    if (file.size > 10 * 1024 * 1024) {
      console.error(`文件 ${file.name} 超过 10MB 限制`)
      continue
    }
    pendingAttachments.value.push({
      name: file.name,
      size: file.size,
      type: getFileType(file.name),
      file
    })
  }
  e.target.value = ''
}

function removeAttachment(idx) {
  pendingAttachments.value.splice(idx, 1)
}

function isTextFile(name) {
  const ext = name.split('.').pop()?.toLowerCase()
  return ['txt', 'md', 'json', 'xml', 'csv', 'log'].includes(ext)
}

function getFileType(name) {
  const ext = name.split('.').pop()?.toLowerCase()
  const imageExts = ['jpg', 'jpeg', 'png', 'gif', 'webp']
  return imageExts.includes(ext) ? 'image' : 'text'
}

// ============ 工具方法 ============
function renderMarkdown(text) {
  if (!text) return ''
  return marked.parse(text)
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

function scrollToBottom() {
  nextTick(() => {
    const el = messagesContainer.value
    if (el) el.scrollTop = el.scrollHeight
  })
}

async function toggleArchive(conv) {
  try {
    await aiService.archiveConversation(conv.id, !conv.isArchived)
    await loadConversations()
  } catch (e) {
    console.error('归档失败:', e)
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

function openActionDraft(message) {
  console.log('[AI] openActionDraft', { id: message?.id, actionDraft: message?.actionDraft })
  pendingAction.value = message?.actionDraft || null
  pendingActionVisible.value = !!pendingAction.value
  editableMessageId.value = message?.id || null
}

function normalizeDraft(raw) {
  const obj = typeof raw === 'string' ? safeParse(raw) : raw
  if (!obj || typeof obj !== 'object') return null
  const payload = obj.payload || {}
  return {
    kind: obj.kind || 'unknown',
    mode: obj.mode || 'create',
    title: obj.title || '待确认操作',
    targetLabel: obj.targetLabel || '',
    before: obj.before || '',
    after: obj.after || '',
    preview: obj.preview || '',
    payload
  }
}

function getKindLabel(kind) {
  return ({ project: '项目', task: '任务', resource: '资源' }[kind] || kind || '草案')
}

function extractDraftMessage(message) {
  const raw = message?.attachments || message?.Attachments
  if (!raw) {
    console.log('[AI] extractDraftMessage no draft attachments', { id: message?.id, role: message?.role })
    return null
  }
  const draft = normalizeDraft(raw)
  console.log('[AI] extractDraftMessage parsed', { id: message?.id, role: message?.role, draft })
  return draft && draft.kind !== 'unknown' ? draft : null
}

function syncLatestDraft() {
  const latest = [...messages.value].reverse().find(m => m.actionDraft)
  console.log('[AI] syncLatestDraft', { latestId: latest?.id, draft: latest?.actionDraft })
  if (!latest?.actionDraft) {
    pendingAction.value = null
    pendingActionVisible.value = false
    editableMessageId.value = null
    return
  }
  pendingAction.value = latest.actionDraft
  pendingActionVisible.value = true
  editableMessageId.value = latest.id || null
  clearActionFeedback()
}

function setActionFeedback(message, type = 'success') {
  actionFeedback.value = message
  actionFeedbackType.value = type
}

function clearActionFeedback() {
  actionFeedback.value = ''
  actionFeedbackType.value = ''
}

function cancelPendingAction() {
  pendingAction.value = null
  pendingActionVisible.value = false
  editableMessageId.value = null
  confirmingPendingAction.value = false
  clearActionFeedback()
}

async function startEditMessage(msg) {
  if (!msg?.id || currentConvId.value == null) return
  editingMessage.value = msg
  inputText.value = msg?.content || ''
  await nextTick()
  autoResize()
  inputRef.value?.focus()
}

async function confirmPendingAction() {
  console.log('[AI] confirmPendingAction clicked', { currentConvId: currentConvId.value, editableMessageId: editableMessageId.value, pendingAction: pendingAction.value })
  if (!pendingAction.value || !currentConvId.value || !editableMessageId.value || confirmingPendingAction.value) return

  confirmingPendingAction.value = true
  clearActionFeedback()

  try {
    const res = await aiService.confirmDraft(currentConvId.value, editableMessageId.value)
    console.log('[AI] confirmDraft response', res)
    setActionFeedback(`已确认执行：${res?.kind || pendingAction.value.kind} - ${res?.action || 'completed'}`, 'success')
    pendingAction.value = null
    pendingActionVisible.value = false
    editableMessageId.value = null
    await reloadMessages()
  } catch (e) {
    console.error('确认写入失败:', e)
    setActionFeedback(e?.message ? `确认失败：${e.message}` : '确认失败，请稍后重试', 'error')
  } finally {
    confirmingPendingAction.value = false
  }
}

async function regenerateFromMessage(msg) {
  console.log('[AI] regenerateFromMessage', { id: msg?.id, role: msg?.role, currentConvId: currentConvId.value })
  if (!msg?.id || msg.role !== 'assistant' || !currentConvId.value) return
  try {
    await aiService.regenerateMessage(currentConvId.value, msg.id)
    await reloadMessages()
  } catch (e) {
    console.error('重新生成失败:', e)
  }
}

function safeParse(str) {
  try { return JSON.parse(str) }
  catch { return null }
}
</script>

<style scoped>
.ai-page {
  display: flex;
  gap: var(--space-6);
  min-height: calc(100vh - var(--header-height, 64px));
  padding: var(--space-6);
  background: linear-gradient(180deg, var(--bg-page) 0%, #eef4ff 100%);
  overflow: hidden;
}

.page-shell {
  animation: fadeInUp 0.45s ease;
}

.card-shell {
  background: var(--bg-card-solid);
  border: 1px solid var(--border-light);
  border-radius: var(--radius-2xl);
  box-shadow: var(--shadow-card);
}

.section-kicker {
  font-size: var(--font-size-xs);
  color: var(--primary-color);
  font-weight: var(--font-weight-semibold);
  text-transform: uppercase;
  letter-spacing: 0.08em;
  margin-bottom: 4px;
}

.section-title {
  font-size: var(--font-size-lg);
  color: var(--text-primary);
  margin: 0;
}

.conv-sidebar {
  width: 300px;
  min-width: 300px;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.conv-header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: var(--space-3);
  padding: var(--space-5);
  border-bottom: 1px solid var(--border-light);
  background: linear-gradient(180deg, #fff 0%, #f8fbff 100%);
}

.conv-tools {
  padding: 12px var(--space-5) 0;
}

.search-shell {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 10px 12px;
  border: 1px solid var(--border-light);
  border-radius: var(--radius-lg);
  background: #fff;
  transition: all var(--transition-fast);
}
.search-shell:focus-within {
  border-color: var(--primary-color);
  box-shadow: 0 0 0 3px rgba(59, 130, 246, 0.1);
}
.search-icon {
  color: var(--text-tertiary);
  flex-shrink: 0;
}
.conv-search {
  width: 100%;
  border: none;
  outline: none;
  font-size: var(--font-size-sm);
  color: var(--text-primary);
  background: transparent;
}
.conv-search::placeholder {
  color: var(--text-tertiary);
}

.btn-new {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 8px 14px;
  border: 1px solid var(--primary-light);
  border-radius: var(--radius-full);
  background: var(--primary-lighter);
  color: var(--primary-color);
  font-size: var(--font-size-xs);
  font-weight: var(--font-weight-semibold);
  cursor: pointer;
  transition: all var(--transition-fast);
  flex-shrink: 0;
}
.btn-new:hover {
  background: var(--primary-color);
  color: white;
  transform: translateY(-1px);
  box-shadow: var(--shadow-md);
}

.conv-list {
  flex: 1;
  overflow-y: auto;
  padding: var(--space-3) var(--space-3) var(--space-4);
}

.conv-item {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: var(--space-3);
  padding: var(--space-4);
  margin-bottom: var(--space-2);
  border-radius: var(--radius-lg);
  cursor: pointer;
  transition: all var(--transition-fast);
  border: 1px solid transparent;
}
.conv-item:hover {
  background: var(--primary-lighter);
  border-color: var(--primary-light);
}
.conv-item.active {
  background: linear-gradient(135deg, var(--primary-lighter) 0%, #ffffff 100%);
  border-color: var(--primary-light);
  box-shadow: 0 8px 24px rgba(59, 130, 246, 0.08);
}

.conv-info {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: 4px;
}
.conv-name {
  font-size: var(--font-size-sm);
  color: var(--text-primary);
  font-weight: var(--font-weight-medium);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}
.conv-time {
  font-size: var(--font-size-xs);
  color: var(--text-tertiary);
}

.conv-badges {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
  margin-top: 2px;
}
.mini-badge {
  display: inline-flex;
  align-items: center;
  padding: 2px 8px;
  border-radius: var(--radius-full);
  background: var(--primary-lighter);
  color: var(--primary-color);
  border: 1px solid var(--primary-light);
  font-size: 11px;
  font-weight: var(--font-weight-semibold);
}
.mini-badge.muted {
  background: var(--bg-color-secondary);
  color: var(--text-secondary);
  border-color: var(--border-light);
}

.conv-actions {
  display: flex;
  align-items: center;
  gap: 6px;
  flex-shrink: 0;
}

.icon-action-btn {
  width: 30px;
  height: 30px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  border: 1px solid transparent;
  border-radius: var(--radius-base);
  background: transparent;
  color: var(--text-tertiary);
  cursor: pointer;
  transition: all var(--transition-fast);
}

.icon-action-btn :deep(svg) {
  width: 16px;
  height: 16px;
}

.icon-action-btn:hover {
  background: var(--primary-lighter);
  border-color: var(--primary-light);
  color: var(--primary-color);
}

.icon-action-btn.active {
  background: rgba(59, 130, 246, 0.12);
  border-color: var(--primary-light);
  color: var(--primary-color);
}

.icon-action-btn.danger:hover {
  background: var(--error-lighter);
  border-color: var(--error-light);
  color: var(--error-color);
}

.conv-empty {
  text-align: center;
  color: var(--text-secondary);
  padding: var(--space-10) var(--space-4);
  font-size: var(--font-size-sm);
}

.chat-main {
  flex: 1;
  display: flex;
  flex-direction: column;
  min-width: 0;
  overflow: hidden;
}

.chat-topbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: var(--space-5) var(--space-6);
  border-bottom: 1px solid var(--border-light);
  background: rgba(255, 255, 255, 0.86);
  backdrop-filter: blur(16px);
}

.current-title {
  font-size: var(--font-size-base);
  font-weight: var(--font-weight-semibold);
  color: var(--text-primary);
}

.deep-think-toggle {
  display: flex;
  align-items: center;
  gap: 8px;
  cursor: pointer;
}
.toggle-label {
  font-size: var(--font-size-sm);
  color: var(--text-secondary);
}

.chat-messages {
  flex: 1;
  overflow-y: auto;
  padding: var(--space-6);
  display: flex;
  flex-direction: column;
  gap: var(--space-4);
  background: linear-gradient(180deg, rgba(255,255,255,0.45) 0%, rgba(239,246,255,0.55) 100%);
}

.chat-welcome {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  text-align: center;
  padding: var(--space-8) 0;
}

.welcome-card {
  max-width: 680px;
  width: 100%;
  padding: var(--space-8);
  border-radius: var(--radius-2xl);
  background: rgba(255,255,255,0.85);
  border: 1px solid var(--border-light);
  box-shadow: var(--shadow-lg);
}

.welcome-icon { margin-bottom: var(--space-4); }
.chat-welcome h2 {
  font-size: var(--font-size-2xl);
  font-weight: var(--font-weight-bold);
  color: var(--text-primary);
  margin: 0 0 var(--space-2);
}
.chat-welcome p {
  font-size: var(--font-size-sm);
  color: var(--text-secondary);
  margin: 0;
}
.quick-prompts {
  display: flex;
  flex-wrap: wrap;
  gap: var(--space-2);
  justify-content: center;
  margin-top: var(--space-5);
}
.prompt-chip {
  padding: 8px 14px;
  border: 1px solid var(--border-light);
  border-radius: var(--radius-full);
  background: #fff;
  color: var(--text-primary);
  font-size: var(--font-size-sm);
  cursor: pointer;
  transition: all var(--transition-fast);
}
.prompt-chip:hover {
  background: var(--primary-lighter);
  border-color: var(--primary-light);
  color: var(--primary-color);
  transform: translateY(-1px);
}

.message-wrapper { display: flex; }
.message-wrapper.user { justify-content: flex-end; }
.message-wrapper.assistant { justify-content: flex-start; }

.message-bubble {
  max-width: min(760px, 80%);
  padding: var(--space-4) var(--space-5);
  border-radius: var(--radius-xl);
  position: relative;
  box-shadow: var(--shadow-sm);
}
.message-wrapper.user .message-bubble {
  background: var(--gradient-primary);
  color: white;
  border-bottom-right-radius: 6px;
}
.message-wrapper.assistant .message-bubble {
  background: white;
  color: var(--text-primary);
  border: 1px solid var(--border-light);
  border-bottom-left-radius: 6px;
}

.msg-content {
  font-size: var(--font-size-sm);
  line-height: 1.75;
  word-break: break-word;
}
.msg-content :deep(p) { margin: 0.5em 0; }
.msg-content :deep(pre) {
  background: #0f172a;
  color: #e2e8f0;
  border-radius: var(--radius-lg);
  padding: 14px;
  overflow-x: auto;
  margin: 12px 0;
}
.msg-content :deep(code) {
  font-family: var(--font-family-mono);
  font-size: 12px;
}
.msg-content :deep(ul), .msg-content :deep(ol) { padding-left: 20px; }

.msg-attachments {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
  margin-bottom: var(--space-2);
}
.attachment-tag {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  padding: 4px 10px;
  border-radius: var(--radius-full);
  background: var(--primary-lighter);
  color: var(--primary-color);
  font-size: var(--font-size-xs);
}

.reasoning-section { margin-bottom: var(--space-2); }
.reasoning-details { cursor: pointer; }
.reasoning-summary {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: var(--font-size-xs);
  color: var(--text-secondary);
  padding: 4px 0;
  list-style: none;
}
.reasoning-summary::-webkit-details-marker { display: none; }
.reasoning-content {
  margin-top: 8px;
  padding: 12px 14px;
  background: var(--secondary-lighter);
  border-left: 3px solid var(--secondary-color);
  border-radius: 0 var(--radius-lg) var(--radius-lg) 0;
  font-size: var(--font-size-sm);
  color: #4f46e5;
  line-height: 1.65;
  white-space: pre-wrap;
}

.tool-call-badge {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  margin-top: 8px;
  padding: 4px 10px;
  border-radius: var(--radius-full);
  background: var(--success-lighter);
  color: var(--success-color);
  font-size: var(--font-size-xs);
}

.message-toolbar {
  display: flex;
  justify-content: flex-end;
  margin-top: 10px;
}

.icon-action-btn {
  width: 32px;
  height: 32px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  border: 1px solid var(--border-light);
  border-radius: 50%;
  background: rgba(255,255,255,0.92);
  color: var(--text-secondary);
  cursor: pointer;
  transition: all var(--transition-fast);
}

.icon-action-btn :deep(svg) {
  width: 16px;
  height: 16px;
}

.icon-action-btn:hover {
  color: var(--primary-color);
  border-color: var(--primary-light);
  background: var(--primary-lighter);
  transform: translateY(-1px);
}

.icon-action-btn.active {
  color: var(--primary-color);
  border-color: var(--primary-light);
  background: rgba(59, 130, 246, 0.12);
}

.icon-action-btn.danger {
  color: var(--error-color);
}

.icon-action-btn.danger:hover {
  color: var(--error-color);
  border-color: var(--error-light);
  background: var(--error-lighter);
}

.action-draft-card {
  margin-top: 14px;
  padding: 16px;
  border: 1px solid rgba(59, 130, 246, 0.18);
  border-radius: 16px;
  background: linear-gradient(180deg, rgba(255,255,255,0.98) 0%, rgba(239,246,255,0.94) 100%);
  box-shadow: 0 10px 28px rgba(59,130,246,0.08);
}

.action-draft-head {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 12px;
}

.action-draft-type {
  display: inline-flex;
  align-items: center;
  padding: 3px 10px;
  border-radius: 999px;
  background: rgba(59,130,246,0.12);
  color: var(--primary-color);
  font-size: 11px;
  font-weight: 700;
}

.action-draft-title {
  margin: 8px 0 6px;
  font-size: 14px;
  font-weight: 700;
  color: var(--text-primary);
}

.action-draft-preview {
  color: var(--text-secondary);
  font-size: 13px;
  line-height: 1.7;
  white-space: pre-wrap;
}

.action-draft-meta {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 8px;
  margin-top: 12px;
}

.action-draft-target {
  font-size: 12px;
  color: var(--text-secondary);
}

.action-confirm-btn {
  background: var(--gradient-primary);
  color: #fff;
  border-color: transparent;
  box-shadow: var(--shadow-sm);
}

.action-confirm-btn:hover {
  background: linear-gradient(135deg, #2563eb, #4f46e5);
  color: #fff;
}

.action-confirm-panel {
  margin-bottom: 12px;
  padding: 16px;
  border: 1px solid rgba(59, 130, 246, 0.2);
  border-radius: 18px;
  background: #fff;
  box-shadow: 0 12px 30px rgba(15, 23, 42, 0.08);
}

.action-confirm-title {
  margin: 4px 0 0;
  font-size: 15px;
  color: var(--text-primary);
}

.action-confirm-body {
  margin-top: 12px;
}

.action-confirm-diff {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 12px;
}

.diff-column {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.diff-label {
  font-size: 12px;
  color: var(--text-secondary);
}

.diff-box {
  min-height: 84px;
  padding: 12px;
  border: 1px solid var(--border-light);
  border-radius: 12px;
  background: linear-gradient(180deg, #fff, #f8fbff);
  color: var(--text-primary);
  font-size: 13px;
  line-height: 1.6;
  white-space: pre-wrap;
}

.diff-box.muted {
  color: var(--text-tertiary);
  background: #fafcff;
}

.action-confirm-meta {
  display: flex;
  align-items: center;
  gap: 10px;
  flex-wrap: wrap;
  margin-top: 12px;
}

.action-confirm-tip {
  font-size: 12px;
  color: var(--text-tertiary);
}

.action-feedback {
  margin-top: 12px;
  padding: 10px 12px;
  border-radius: 12px;
  font-size: 12px;
  line-height: 1.6;
  border: 1px solid transparent;
}
.action-feedback.success {
  background: #eff6ff;
  border-color: #bfdbfe;
  color: #1d4ed8;
}
.action-feedback.error {
  background: #fef2f2;
  border-color: #fecaca;
  color: #b91c1c;
}
.action-confirm-footer {
  display: flex;
  justify-content: flex-end;
  gap: 10px;
  margin-top: 14px;
}

.btn-secondary, .btn-primary {
  padding: 8px 14px;
  border-radius: 999px;
  border: 1px solid var(--border-light);
  cursor: pointer;
  transition: all var(--transition-fast);
}

.btn-secondary {
  background: #fff;
  color: var(--text-secondary);
}

.btn-secondary:disabled,
.btn-primary:disabled {
  opacity: 0.6;
  cursor: not-allowed;
  box-shadow: none;
  transform: none;
}

.btn-secondary:hover {
  background: var(--bg-color-secondary);
}

.btn-primary {
  background: var(--gradient-primary);
  color: #fff;
  border-color: transparent;
}

.btn-primary:hover {
  box-shadow: var(--shadow-md);
}

.typing-cursor {
  animation: blink 1s infinite;
  color: var(--primary-color);
  font-weight: bold;
}
@keyframes blink {
  0%, 50% { opacity: 1; }
  51%, 100% { opacity: 0; }
}

.thinking-dots {
  display: flex;
  gap: 4px;
  padding: 4px 0;
}
.thinking-dots span {
  width: 6px;
  height: 6px;
  border-radius: 50%;
  background: var(--text-tertiary);
  animation: dot-pulse 1.4s infinite;
}
.thinking-dots span:nth-child(2) { animation-delay: 0.2s; }
.thinking-dots span:nth-child(3) { animation-delay: 0.4s; }
@keyframes dot-pulse {
  0%, 80%, 100% { transform: scale(0.6); opacity: 0.4; }
  40% { transform: scale(1); opacity: 1; }
}

.chat-input-area {
  padding: var(--space-5) var(--space-6);
  border-top: 1px solid var(--border-light);
  background: rgba(255,255,255,0.9);
  backdrop-filter: blur(16px);
}

.attachment-preview-bar {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
  margin-bottom: 10px;
}
.att-preview-item {
  display: flex;
  align-items: center;
  gap: 4px;
  padding: 4px 10px;
  background: var(--primary-lighter);
  border: 1px solid var(--primary-light);
  border-radius: var(--radius-full);
  font-size: var(--font-size-xs);
  color: var(--primary-color);
}
.att-icon { display: flex; }
.att-name { max-width: 120px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.att-remove {
  display: flex;
  border: none;
  background: transparent;
  color: var(--text-tertiary);
  cursor: pointer;
  padding: 2px;
}
.att-remove:hover { color: var(--error-color); }

.input-row {
  display: flex;
  align-items: flex-end;
  gap: 10px;
}

.upload-btn {
  width: 42px;
  height: 42px;
  display: flex;
  align-items: center;
  justify-content: center;
  border: 1px solid var(--border-light);
  border-radius: var(--radius-lg);
  background: white;
  color: var(--text-secondary);
  cursor: pointer;
  transition: all var(--transition-fast);
  flex-shrink: 0;
}
.upload-btn:hover {
  color: var(--primary-color);
  border-color: var(--primary-light);
  background: var(--primary-lighter);
}

.chat-input {
  flex: 1;
  padding: 12px 16px;
  border: 1px solid var(--border-light);
  border-radius: var(--radius-lg);
  background: white;
  color: var(--text-primary);
  font-size: var(--font-size-sm);
  font-family: inherit;
  resize: none;
  outline: none;
  line-height: 1.5;
  max-height: 140px;
  transition: all var(--transition-fast);
}
.chat-input:focus {
  border-color: var(--primary-color);
  box-shadow: 0 0 0 3px rgba(59, 130, 246, 0.1);
}
.chat-input::placeholder { color: var(--text-tertiary); }

.send-btn {
  width: 42px;
  height: 42px;
  display: flex;
  align-items: center;
  justify-content: center;
  border: none;
  border-radius: var(--radius-lg);
  background: var(--gradient-primary);
  color: white;
  cursor: pointer;
  transition: all var(--transition-fast);
  flex-shrink: 0;
}
.send-btn:hover:not(:disabled) {
  transform: translateY(-1px);
  box-shadow: var(--shadow-md);
}
.send-btn:disabled {
  opacity: 0.45;
  cursor: not-allowed;
  box-shadow: none;
}

@media (max-width: 1200px) {
  .ai-page {
    gap: var(--space-4);
    padding: var(--space-4);
  }

  .conv-sidebar {
    width: 280px;
    min-width: 280px;
  }
}

@media (max-width: 900px) {
  .ai-page {
    flex-direction: column;
    height: auto;
    min-height: calc(100vh - var(--header-height, 64px));
  }

  .conv-sidebar {
    width: 100%;
    min-width: 0;
    max-height: 320px;
  }

  .chat-main {
    min-height: 70vh;
  }

  .message-bubble {
    max-width: 100%;
  }
}
</style>
