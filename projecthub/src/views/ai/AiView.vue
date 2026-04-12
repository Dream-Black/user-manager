<template>
  <div class="ai-page">
    <div class="page-header fade-in">
      <h2 class="page-title">AI 助手</h2>
      <span class="page-subtitle">智能问答与任务分析</span>
    </div>
    
    <div class="ai-container card fade-in-up">
      <div class="chat-messages" ref="messagesContainer">
        <div v-for="(msg, index) in messages" :key="index" 
             class="message" :class="msg.role">
          <div class="message-avatar">
            <svg v-if="msg.role === 'assistant'" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <circle cx="12" cy="12" r="3"/><path d="M12 1v2m0 18v2M4.22 4.22l1.42 1.42m12.72 12.72l1.42 1.42M1 12h2m18 0h2M4.22 19.78l1.42-1.42M18.36 5.64l1.42-1.42"/>
            </svg>
            <span v-else>U</span>
          </div>
          <div class="message-content">{{ msg.content }}</div>
        </div>
        
        <div v-if="isTyping" class="message assistant">
          <div class="message-avatar">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <circle cx="12" cy="12" r="3"/><path d="M12 1v2m0 18v2M4.22 4.22l1.42 1.42m12.72 12.72l1.42 1.42M1 12h2m18 0h2M4.22 19.78l1.42-1.42M18.36 5.64l1.42-1.42"/>
            </svg>
          </div>
          <div class="message-content typing">
            <span></span><span></span><span></span>
          </div>
        </div>
      </div>
      
      <div class="chat-input">
        <t-input v-model="inputMessage" placeholder="输入问题..." @enter="sendMessage" />
        <t-button theme="primary" :disabled="!inputMessage || isTyping" @click="sendMessage">
          <template #icon><svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><line x1="22" y1="2" x2="11" y2="13"/><polygon points="22 2 15 22 11 13 2 9 22 2"/></svg></template>
          发送
        </t-button>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, nextTick } from 'vue'

const messages = ref([
  { role: 'assistant', content: '你好！我是 ProjectHub AI 助手，可以帮你分析项目、解答问题或提供建议。有什么可以帮你的吗？' }
])
const inputMessage = ref('')
const isTyping = ref(false)
const messagesContainer = ref(null)

const sendMessage = async () => {
  if (!inputMessage.value || isTyping.value) return
  
  messages.value.push({ role: 'user', content: inputMessage.value })
  const query = inputMessage.value
  inputMessage.value = ''
  isTyping.value = true
  
  await nextTick()
  messagesContainer.value.scrollTop = messagesContainer.value.scrollHeight
  
  // 调用 AI 接口
  try {
    const res = await fetch('/api/ai/chat', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ message: query })
    })
    const data = await res.json()
    messages.value.push({ role: 'assistant', content: data.response || '抱歉，我暂时无法回答这个问题。' })
  } catch {
    messages.value.push({ role: 'assistant', content: '抱歉，发生了错误。请稍后再试。' })
  }
  
  isTyping.value = false
  await nextTick()
  messagesContainer.value.scrollTop = messagesContainer.value.scrollHeight
}
</script>

<style scoped>
.ai-page { animation: fadeIn 0.3s ease-out; height: calc(100vh - 140px); display: flex; flex-direction: column; }
.page-header { margin-bottom: var(--space-5); }
.page-title { font-size: var(--font-size-2xl); font-weight: var(--font-weight-semibold); margin-bottom: var(--space-1); }
.page-subtitle { font-size: var(--font-size-sm); color: var(--text-tertiary); }
.ai-container { flex: 1; display: flex; flex-direction: column; min-height: 0; opacity: 0; }
.chat-messages { flex: 1; overflow-y: auto; padding: var(--space-5); }
.message { display: flex; gap: var(--space-3); margin-bottom: var(--space-4); animation: fadeIn 0.3s; }
.message.user { flex-direction: row-reverse; }
.message-avatar { width: 36px; height: 36px; border-radius: 50%; background: var(--primary-100); color: var(--primary-600); display: flex; align-items: center; justify-content: center; flex-shrink: 0; font-weight: medium; }
.message.user .message-avatar { background: var(--gray-100); color: var(--gray-600); }
.message-content { max-width: 70%; padding: var(--space-3) var(--space-4); background: var(--gray-50); border-radius: var(--radius-xl); font-size: var(--font-size-sm); line-height: 1.6; }
.message.user .message-content { background: var(--primary-500); color: white; }
.message-content.typing { display: flex; gap: 4px; padding: var(--space-4); }
.message-content.typing span { width: 8px; height: 8px; background: var(--text-tertiary); border-radius: 50%; animation: bounce 1.4s infinite ease-in-out; }
.message-content.typing span:nth-child(1) { animation-delay: 0s; }
.message-content.typing span:nth-child(2) { animation-delay: 0.2s; }
.message-content.typing span:nth-child(3) { animation-delay: 0.4s; }
@keyframes bounce { 0%, 80%, 100% { transform: scale(0); } 40% { transform: scale(1); } }
.chat-input { display: flex; gap: var(--space-3); padding: var(--space-4); border-top: 1px solid var(--border-light); }
.chat-input .t-input { flex: 1; }
</style>
