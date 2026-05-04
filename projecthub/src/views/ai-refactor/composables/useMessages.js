import { ref, nextTick } from 'vue'
import { aiService } from '@/services/dataService'
import { marked } from 'marked'
import hljs from 'highlight.js'

// 配置 marked（全局一次即可）
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

function safeParse(str) {
  try { return JSON.parse(str) }
  catch { return null }
}

// ---- draft 相关工具 ----
function validateDraftSchema(kind, mode, payload) {
  const errors = []
  const requireString = (key, label) => {
    if (!payload?.[key] || String(payload[key]).trim() === '') errors.push(label)
  }
  const requireNumber = (key, label) => {
    const v = Number(payload?.[key])
    if (!Number.isFinite(v) || v <= 0) errors.push(label)
  }
  const signature = `${kind}.${mode}`
  if (signature === 'project.create') requireString('name', '项目名称')
  else if (signature === 'project.update') { requireNumber('id', '项目ID'); requireString('name', '项目名称') }
  else if (signature === 'task.create') { requireString('title', '任务标题'); requireNumber('projectId', '项目ID') }
  else if (signature === 'task.update') { requireNumber('id', '任务ID'); requireString('title', '任务标题') }
  else if (signature === 'resource.create') { requireNumber('computerId', '电脑ID'); requireString('path', '资源路径') }
  else if (signature === 'resource.update') { requireNumber('id', '资源ID'); requireString('path', '资源路径') }
  return { ok: errors.length === 0, errors }
}

function normalizeDraft(raw) {
  const obj = typeof raw === 'string' ? safeParse(raw) : raw
  if (!obj || typeof obj !== 'object') return null
  const payload = obj.payload || {}
  return {
    id: obj.id || `${obj.kind || 'unknown'}-${Date.now()}`,
    schemaVersion: obj.schemaVersion || 1,
    kind: obj.kind || 'unknown',
    mode: obj.mode || 'create',
    title: obj.title || '待确认操作',
    targetLabel: obj.targetLabel || '',
    before: obj.before || '',
    after: obj.after || '',
    preview: obj.preview || '',
    status: obj.status || 'pending',
    createdAt: obj.createdAt || null,
    expiresAt: obj.expiresAt || null,
    payload,
    summary: obj.summary || '',
    validation: validateDraftSchema(obj.kind || 'unknown', obj.mode || 'create', payload)
  }
}

function extractDraftMessage(message) {
  const raw = message?.attachments || message?.Attachments
  if (!raw) return null
  const draft = normalizeDraft(raw)
  return draft && draft.kind !== 'unknown' ? draft : null
}

function mapMessages(msgs) {
  return (msgs || []).map((m, i) => ({
    ...m,
    _id: `${m.id}_${i}`,
    filesJson: m.filesJson,
    attachments: m.attachments,
    toolCalls: m.toolCalls ? safeParse(m.toolCalls) : null,
    actionDraft: m.role === 'assistant' ? extractDraftMessage(m) : null
  }))
}

/**
 * 消息管理 composable
 * @param {Object} opts
 * @param {import('vue').Ref} opts.currentConvId
 * @param {import('vue').Ref} opts.messagesContainer
 * @param {Function} opts.checkAndExecuteTerminalCommand
 */
export function useMessages({ currentConvId, messagesContainer, checkAndExecuteTerminalCommand }) {
  const messages = ref([])
  const streaming = ref(false)
  const streamingMsgId = ref(null)
  const pendingAttachments = ref([])
  const inputText = ref('')
  const editingMessage = ref(null)

  // action draft 状态
  const pendingAction = ref(null)
  const pendingActionVisible = ref(false)
  const editableMessageId = ref(null)
  const confirmingPendingAction = ref(false)
  const actionFeedback = ref('')
  const actionFeedbackType = ref('')

  function renderMarkdown(text) {
    if (!text) return ''
    return marked.parse(text)
  }

  function getKindLabel(kind) {
    return ({ project: '项目', task: '任务', resource: '资源' }[kind] || kind || '草案')
  }

  function scrollToBottom() {
    return nextTick(() => {
      const el = messagesContainer.value
      if (el) el.scrollTop = el.scrollHeight
    })
  }

  function scrollToMessage(msgId) {
    return nextTick(() => {
      const el = messagesContainer.value
      if (!el || !msgId) return
      const targetEl = el.querySelector(`[data-msg-id="${msgId}"]`)
      if (targetEl) {
        const containerRect = el.getBoundingClientRect()
        const targetRect = targetEl.getBoundingClientRect()
        el.scrollTop = el.scrollTop + (targetRect.top - containerRect.top) - 16
      }
    })
  }

  async function loadMessages(convId) {
    try {
      const msgs = await aiService.getConversationMessages(convId)
      messages.value = mapMessages(msgs)
      syncLatestDraft()
      await nextTick()
      scrollToBottom()
    } catch (e) {
      console.error('加载消息失败:', e)
      messages.value = []
    }
  }

  async function reloadMessages() {
    if (!currentConvId.value) return
    try {
      const msgs = await aiService.getConversationMessages(currentConvId.value)
      messages.value = mapMessages(msgs)
      syncLatestDraft()
      await nextTick()
      scrollToBottom()
    } catch (e) {
      console.error('重新加载消息失败:', e)
    }
  }

  function syncLatestDraft() {
    const now = new Date()
    const latest = [...messages.value].reverse().find(m => {
      const draft = m.actionDraft
      if (!draft || draft.status !== 'pending') return false
      if (draft.expiresAt && new Date(draft.expiresAt) < now) return false
      return true
    })
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

  // ---- 发送消息 ----
  async function sendMessage({ deepThink, selectedModel, onConvsReload, checkAndExecuteTerminalCommand: runtimeCheck } = {}) {
    // 运行时传入的优先，构造时传入的做回退
    const terminalCheck = runtimeCheck ?? checkAndExecuteTerminalCommand
    const text = inputText.value.trim()
    if (!text || streaming.value || !currentConvId.value) return

    const userFiles = [...pendingAttachments.value]
    const isEditing = !!editingMessage.value?.id

    let targetMsgId = null

    if (isEditing) {
      const editingMsgIndex = messages.value.findIndex(m => m._id === editingMessage.value._id)
      if (editingMsgIndex === -1) return

      targetMsgId = editingMessage.value._id
      messages.value = messages.value.slice(0, editingMsgIndex + 1)
      messages.value[editingMsgIndex] = {
        ...messages.value[editingMsgIndex],
        content: text,
        filesJson: [...userFiles]
      }
      try {
        await aiService.updateMessage(currentConvId.value, editingMessage.value.id, { content: text })
        editingMessage.value = null
      } catch (e) {
        console.error('编辑消息失败:', e)
        return
      }
    } else {
      const userMsgId = 'user_' + Date.now()
      targetMsgId = userMsgId
      messages.value.push({
        _id: userMsgId,
        role: 'user',
        content: text,
        filesJson: [...userFiles],
        reasoningContent: null,
        toolCalls: null
      })
    }

    inputText.value = ''
    pendingAttachments.value = []
    scrollToMessage(targetMsgId)

    const aiMsgId = 'ai_' + Date.now()
    messages.value.push({
      _id: aiMsgId,
      role: 'assistant',
      content: '',
      reasoningContent: '',
      toolCalls: null,
      filesJson: null
    })
    streaming.value = true
    streamingMsgId.value = aiMsgId

    let contentBuffer = ''
    let reasoningBuffer = ''
    let hasStartedContent = false

    const flushUpdate = () => {
      const idx = messages.value.findIndex(m => m._id === aiMsgId)
      if (idx === -1) return
      const updated = { ...messages.value[idx], content: contentBuffer, reasoningContent: reasoningBuffer }
      messages.value.splice(idx, 1, updated)
    }

    try {
      const response = await aiService.chatStream(
        currentConvId.value,
        text,
        deepThink,
        userFiles.length > 0 ? userFiles.map(a => ({ name: a.name, type: a.type })) : null,
        selectedModel
      )

      if (!response.ok) throw new Error(`HTTP ${response.status}`)

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

          const dataLine = rawEvent.split(/\r?\n/).find(l => l.startsWith('data: '))
          if (!dataLine) continue
          const dataStr = dataLine.substring(6).trim()
          if (!dataStr) continue
          const data = safeParse(dataStr)
          if (!data) continue

          switch (data.type) {
            case 'content':
              contentBuffer += data.content
              flushUpdate()
              if (!hasStartedContent && contentBuffer.trim()) {
                hasStartedContent = true
                await nextTick()
                const el = messagesContainer.value
                if (el) {
                  const detailsEl = el.querySelector(`[data-msg-id="${aiMsgId}"] .reasoning-details`)
                  if (detailsEl) detailsEl.removeAttribute('open')
                }
                scrollToMessage(targetMsgId)
              }
              break
            case 'reasoning':
              reasoningBuffer += data.content
              flushUpdate()
              break
            case 'tool_call':
              contentBuffer += '\n\n🔍 *正在查询数据库...*'
              flushUpdate()
              break
            case 'tool_result':
              contentBuffer = contentBuffer.replace(/\n\n🔍 \*正在查询数据库...\*/g, '')
              flushUpdate()
              break
            case 'error':
              contentBuffer = data.content
              flushUpdate()
              break
            case 'done':
              if (terminalCheck && await terminalCheck(contentBuffer)) {
                return
              }
              break
          }
        }
      }
    } catch (e) {
      const msg = messages.value.find(m => m._id === aiMsgId)
      if (msg) msg.content = `❌ 请求失败: ${e?.message || '未知错误'}`
      console.error('流式聊天失败:', e)
    } finally {
      streaming.value = false
      streamingMsgId.value = null

      const msg = messages.value.find(m => m._id === aiMsgId)
      if (msg) {
        msg.content = msg.content.replace(/\n\n🔍 \*正在查询数据库...\*/g, '')
        if (terminalCheck && await terminalCheck(msg.content)) return
      }

      if (currentConvId.value) {
        await reloadMessages()
        onConvsReload?.()
      }
    }
  }

  // ---- 附件 ----
  function isTextFile(name) {
    const ext = name.split('.').pop()?.toLowerCase()
    return ['txt', 'md', 'json', 'xml', 'csv', 'log'].includes(ext)
  }

  function getFileType(name) {
    const ext = name.split('.').pop()?.toLowerCase()
    return ['jpg', 'jpeg', 'png', 'gif', 'webp'].includes(ext) ? 'image' : 'text'
  }

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

  // ---- 消息操作 ----
  async function startEditMessage(msg) {
    if (!msg?.id || currentConvId.value == null) return
    editingMessage.value = msg
    inputText.value = msg?.content || ''
  }

  async function regenerateFromMessage(msg) {
    if (!msg?.id || msg.role !== 'assistant' || !currentConvId.value) return
    try {
      await aiService.regenerateMessage(currentConvId.value, msg.id)
      await reloadMessages()
    } catch (e) {
      console.error('重新生成失败:', e)
    }
  }

  // ---- draft 操作 ----
  function setActionFeedback(message, type = 'success') {
    actionFeedback.value = message
    actionFeedbackType.value = type
  }

  function clearActionFeedback() {
    actionFeedback.value = ''
    actionFeedbackType.value = ''
  }

  function openActionDraft(message) {
    if (message?.actionDraft?.validation && !message.actionDraft.validation.ok) {
      setActionFeedback(`草案缺少必要字段：${message.actionDraft.validation.errors.join('、')}`, 'error')
      return
    }
    pendingAction.value = message?.actionDraft || null
    pendingActionVisible.value = !!pendingAction.value
    editableMessageId.value = message?.id || null
  }

  async function cancelDraft(message) {
    if (!message?.id || !currentConvId.value) return
    try {
      await aiService.cancelDraft(currentConvId.value, message.id)
      if (pendingAction.value && editableMessageId.value === message.id) {
        cancelPendingAction()
      }
      await reloadMessages()
    } catch (e) {
      console.error('取消草案失败:', e)
    }
  }

  function cancelPendingAction() {
    if (pendingAction.value) pendingAction.value.status = 'cancelled'
    pendingAction.value = null
    pendingActionVisible.value = false
    editableMessageId.value = null
    confirmingPendingAction.value = false
    clearActionFeedback()
  }

  async function confirmPendingAction() {
    if (!pendingAction.value || !currentConvId.value || !editableMessageId.value || confirmingPendingAction.value) return
    if (pendingAction.value.expiresAt && new Date(pendingAction.value.expiresAt) < new Date()) {
      setActionFeedback('草案已过期，请重新生成', 'error')
      return
    }
    confirmingPendingAction.value = true
    clearActionFeedback()
    try {
      const res = await aiService.confirmDraft(currentConvId.value, editableMessageId.value)
      setActionFeedback(`已确认执行：${res?.kind || pendingAction.value.kind} - ${res?.action || 'completed'}`, 'success')
      pendingAction.value.status = 'confirmed'
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

  return {
    messages,
    streaming,
    streamingMsgId,
    pendingAttachments,
    inputText,
    editingMessage,
    pendingAction,
    pendingActionVisible,
    editableMessageId,
    confirmingPendingAction,
    actionFeedback,
    actionFeedbackType,
    renderMarkdown,
    getKindLabel,
    scrollToBottom,
    scrollToMessage,
    loadMessages,
    reloadMessages,
    sendMessage,
    isTextFile,
    getFileType,
    handleFileSelect,
    removeAttachment,
    startEditMessage,
    regenerateFromMessage,
    openActionDraft,
    cancelDraft,
    cancelPendingAction,
    confirmPendingAction,
    setActionFeedback,
    clearActionFeedback
  }
}
