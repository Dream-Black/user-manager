<template>
  <div
    class="message-wrapper"
    :class="msg.role"
    :data-msg-id="msg._id"
  >
    <div class="message-bubble">
      <!-- 附件标签 -->
      <div v-if="msg.filesJson && msg.filesJson.length" class="msg-attachments">
        <div v-for="att in msg.filesJson" :key="att.name" class="attachment-tag">
          <Paperclip :size="12" />
          <span>{{ att.name }}</span>
        </div>
      </div>

      <!-- 思考过程 -->
      <div v-if="msg.reasoningContent" class="reasoning-section">
        <details class="reasoning-details" open>
          <summary class="reasoning-summary">
            <Brain :size="14" />
            <span>思考过程</span>
          </summary>
          <div class="reasoning-content">{{ msg.reasoningContent }}</div>
        </details>
      </div>

      <!-- 正文 -->
      <div class="msg-content" v-html="renderMarkdown(msg.content || '')"></div>

      <!-- 工具调用标记 -->
      <div v-if="msg.toolCalls" class="tool-call-badge">
        <Database :size="12" />
        <span>已查询数据库</span>
      </div>

      <!-- 操作草案 -->
      <ActionDraftCard
        v-if="msg.actionDraft"
        :draft="msg.actionDraft"
        @cancel="$emit('cancel-draft', msg)"
        @confirm="$emit('confirm-draft', msg)"
      />

      <!-- 消息工具栏 -->
      <div v-if="msg.role === 'assistant'" class="message-toolbar">
        <button class="icon-action-btn" title="重新生成回复" @click="$emit('regenerate', msg)">
          <RefreshIcon />
        </button>
      </div>
      <div v-else-if="msg.role === 'user'" class="message-toolbar">
        <button class="icon-action-btn" title="编辑后重发" @click="$emit('edit', msg)">
          <Edit1Icon />
        </button>
      </div>

      <!-- 流式光标 -->
      <span v-if="isStreaming" class="typing-cursor">▊</span>
    </div>
  </div>
</template>

<script setup>
import { Paperclip, Brain, Database } from 'lucide-vue-next'
import { Edit1Icon, RefreshIcon } from 'tdesign-icons-vue-next'
import { marked } from 'marked'
import hljs from 'highlight.js'
import 'highlight.js/styles/github-dark.css'
import ActionDraftCard from './ActionDraftCard.vue'

marked.setOptions({
  breaks: true,
  gfm: true,
  highlight: (code, lang) => {
    if (lang && hljs.getLanguage(lang)) return hljs.highlight(code, { language: lang }).value
    return hljs.highlightAuto(code).value
  }
})

defineProps({
  msg: { type: Object, required: true },
  isStreaming: { type: Boolean, default: false }
})

defineEmits(['edit', 'regenerate', 'cancel-draft', 'confirm-draft'])

function renderMarkdown(text) {
  if (!text) return ''
  return marked.parse(text)
}
</script>

<style scoped>
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
.msg-content :deep(code) { font-family: var(--font-family-mono); font-size: 12px; }
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
.icon-action-btn :deep(svg) { width: 16px; height: 16px; }
.icon-action-btn:hover {
  color: var(--primary-color);
  border-color: var(--primary-light);
  background: var(--primary-lighter);
  transform: translateY(-1px);
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

@media (max-width: 900px) {
  .message-bubble { max-width: 100%; }
}
</style>
