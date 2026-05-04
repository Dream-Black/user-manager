<template>
  <div ref="containerRef" class="chat-messages">
    <!-- 欢迎页 -->
    <ChatWelcome
      v-if="messages.length === 0 && !streaming"
      :quick-prompts="quickPrompts"
      @quick-prompt="$emit('quick-prompt', $event)"
    />

    <!-- 消息列表 -->
    <MessageBubble
      v-for="msg in messages"
      :key="msg._id"
      :msg="msg"
      :is-streaming="streaming && msg._id === streamingMsgId"
      @edit="$emit('edit-message', $event)"
      @regenerate="$emit('regenerate', $event)"
      @cancel-draft="$emit('cancel-draft', $event)"
      @confirm-draft="$emit('confirm-draft', $event)"
    />

    <!-- 空白流式占位（刚发消息、AI 还没开始回复时） -->
    <div v-if="streaming && messages.length === 0" class="message-wrapper assistant">
      <div class="message-bubble">
        <span class="thinking-dots">
          <span></span><span></span><span></span>
        </span>
      </div>
    </div>

    <!-- 终端执行结果 -->
    <TerminalResultCard
      v-if="terminalResult"
      :result="terminalResult"
      :collapsed="terminalResultCollapsed"
      @update:collapsed="$emit('update:terminalResultCollapsed', $event)"
    />
  </div>
</template>

<script setup>
import { ref } from 'vue'
import MessageBubble from './MessageBubble.vue'
import ChatWelcome from './ChatWelcome.vue'
import TerminalResultCard from './TerminalResultCard.vue'

defineProps({
  messages: { type: Array, default: () => [] },
  streaming: { type: Boolean, default: false },
  streamingMsgId: { type: String, default: null },
  terminalResult: { type: Object, default: null },
  terminalResultCollapsed: { type: Boolean, default: true },
  quickPrompts: { type: Array, default: () => [] }
})

defineEmits([
  'quick-prompt',
  'edit-message',
  'regenerate',
  'cancel-draft',
  'confirm-draft',
  'update:terminalResultCollapsed'
])

const containerRef = ref(null)

defineExpose({ containerRef })
</script>

<style scoped>
.chat-messages {
  flex: 1;
  overflow-y: auto;
  padding: var(--space-6);
  display: flex;
  flex-direction: column;
  gap: var(--space-4);
  background: linear-gradient(180deg, rgba(255,255,255,0.45) 0%, rgba(239,246,255,0.55) 100%);
  position: relative;
}

/* 流式占位气泡 */
.message-wrapper { display: flex; }
.message-wrapper.assistant { justify-content: flex-start; }

.message-bubble {
  max-width: min(760px, 80%);
  padding: var(--space-4) var(--space-5);
  border-radius: var(--radius-xl);
  background: white;
  color: var(--text-primary);
  border: 1px solid var(--border-light);
  border-bottom-left-radius: 6px;
  box-shadow: var(--shadow-sm);
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
</style>
