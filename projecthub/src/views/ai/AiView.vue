<template>
  <div class="ai-page">
    <div class="page-header">
      <div class="header-content">
        <h1 class="page-title">AI 助手</h1>
        <p class="page-subtitle">智能助手，助力高效工作</p>
      </div>
    </div>

    <div class="ai-container">
      <!-- 聊天区域 -->
      <div class="chat-area" ref="chatAreaRef">
        <div v-if="messages.length === 0" class="empty-state">
          <div class="ai-avatar">
            <AiIcon />
          </div>
          <h3>您好，我是AI助手</h3>
          <p>我可以帮您：</p>
          <ul>
            <li>分析项目进度和数据</li>
            <li>生成任务报告和总结</li>
            <li>提供工作建议和优化方案</li>
            <li>回答技术问题和知识查询</li>
          </ul>
        </div>

        <div v-else class="messages-list">
          <div
            v-for="(msg, index) in messages"
            :key="index"
            class="message"
            :class="msg.role"
            :style="{ animationDelay: `${index * 0.05}s` }"
          >
            <div class="message-avatar">
              <div v-if="msg.role === 'user'" class="user-avatar">P</div>
              <div v-else class="ai-avatar-sm"><AiIcon /></div>
            </div>
            <div class="message-content">
              <div class="message-bubble" :class="{ loading: msg.isLoading }">
                <span v-if="msg.isLoading" class="loading-dots">...</span>
                <span v-else>{{ msg.content }}</span>
              </div>
              <div class="message-time">{{ msg.time }}</div>
            </div>
          </div>
        </div>
      </div>

      <!-- 快捷提示 -->
      <div v-if="messages.length === 0" class="quick-prompts">
        <div class="prompt-label">试试这样问我：</div>
        <div class="prompts-grid">
          <div
            v-for="prompt in quickPrompts"
            :key="prompt.text"
            class="prompt-item"
            @click="sendQuickPrompt(prompt.text)"
          >
            <component :is="prompt.icon" class="prompt-icon" />
            <span>{{ prompt.text }}</span>
          </div>
        </div>
      </div>

      <!-- 输入区域 -->
      <div class="input-area">
        <t-input
          v-model="inputText"
          placeholder="输入您的问题..."
          @enter="handleSend"
        >
          <template #suffix>
            <div class="input-actions">
              <t-button variant="text" @click="handleSend" :disabled="!inputText.trim()">
                <template #icon><SendIcon /></template>
              </t-button>
            </div>
          </template>
        </t-input>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, nextTick, markRaw } from 'vue'
import { MessagePlugin } from 'tdesign-vue-next'
import { aiService } from '@/services/dataService'
import { projectService } from '@/services/dataService'
import { taskService } from '@/services/dataService'

const inputText = ref('')
const messages = ref([])
const chatAreaRef = ref(null)
const isLoading = ref(false)

const quickPrompts = [
  { text: '分析一下当前项目的进度', icon: markRaw(ChartIcon) },
  { text: '生成本周的工作总结', icon: markRaw(FileIcon) },
  { text: '优化任务分配方案', icon: markRaw(TaskIcon) },
  { text: '推荐下一个紧急任务', icon: markRaw(StarIcon) }
]

const handleSend = async () => {
  if (!inputText.value.trim() || isLoading.value) return

  const userMessage = {
    role: 'user',
    content: inputText.value,
    time: new Date().toLocaleTimeString('zh-CN', { hour: '2-digit', minute: '2-digit' })
  }
  messages.value.push(userMessage)
  inputText.value = ''

  await nextTick()
  scrollToBottom()

  // 添加加载状态
  isLoading.value = true
  const loadingMessage = {
    role: 'assistant',
    content: '思考中...',
    time: new Date().toLocaleTimeString('zh-CN', { hour: '2-digit', minute: '2-digit' }),
    isLoading: true
  }
  messages.value.push(loadingMessage)

  try {
    // 调用真实的 AI API
    const response = await aiService.chat({
      message: userMessage.content,
      context: await getContext()
    })

    // 替换加载消息为真实回复
    const lastIndex = messages.value.length - 1
    messages.value[lastIndex] = {
      role: 'assistant',
      content: response.message || response.response || response.content || '抱歉，我暂时无法回答这个问题。',
      time: new Date().toLocaleTimeString('zh-CN', { hour: '2-digit', minute: '2-digit' }),
      isLoading: false
    }
  } catch (error) {
    console.error('AI 响应失败:', error)
    
    // 替换为错误消息
    const lastIndex = messages.value.length - 1
    messages.value[lastIndex] = {
      role: 'assistant',
      content: '抱歉，服务暂时不可用，请稍后再试。',
      time: new Date().toLocaleTimeString('zh-CN', { hour: '2-digit', minute: '2-digit' }),
      isLoading: false
    }
  } finally {
    isLoading.value = false
    nextTick(() => scrollToBottom())
  }
}

// 获取上下文数据用于 AI 分析
const getContext = async () => {
  try {
    const [projects, tasks] = await Promise.all([
      projectService.getAll(),
      taskService.getAll()
    ])
    
    return {
      projects: projects.slice(0, 5),
      tasks: tasks.slice(0, 10),
      totalProjects: projects.length,
      totalTasks: tasks.length,
      completedTasks: tasks.filter(t => t.status === 'completed').length
    }
  } catch (error) {
    console.error('获取上下文失败:', error)
    return null
  }
}

const sendQuickPrompt = (text) => {
  inputText.value = text
  handleSend()
}

const scrollToBottom = () => {
  if (chatAreaRef.value) {
    chatAreaRef.value.scrollTop = chatAreaRef.value.scrollHeight
  }
}

import { AiIcon, SendIcon, ChartIcon, FileIcon, TaskIcon, StarIcon } from 'tdesign-icons-vue-next'
</script>

<style scoped>
.ai-page { max-width: 900px; margin: 0 auto; height: calc(100vh - 160px); display: flex; flex-direction: column; }
.page-header { margin-bottom: 24px; animation: fadeInUp 0.5s ease; }
.header-content { display: flex; flex-direction: column; gap: 4px; }
.page-title { font-size: 28px; font-weight: 700; color: var(--text-primary); margin: 0; }
.page-subtitle { font-size: 14px; color: var(--text-secondary); margin: 0; }

.ai-container { flex: 1; display: flex; flex-direction: column; background: var(--bg-card-solid); border-radius: var(--radius-xl); border: 1px solid var(--border-light); overflow: hidden; animation: fadeInUp 0.5s ease 0.1s backwards; box-shadow: var(--shadow-card); }

.chat-area { flex: 1; overflow-y: auto; padding: 24px; }

.empty-state { display: flex; flex-direction: column; align-items: center; justify-content: center; height: 100%; text-align: center; }
.ai-avatar { width: 80px; height: 80px; border-radius: var(--radius-full); background: var(--gradient-primary); display: flex; align-items: center; justify-content: center; color: white; margin-bottom: 20px; animation: float 3s ease-in-out infinite; box-shadow: var(--shadow-glow); }
.ai-avatar svg { width: 40px; height: 40px; }
.empty-state h3 { font-size: 20px; font-weight: 600; color: var(--text-primary); margin: 0 0 12px 0; }
.empty-state p { font-size: 14px; color: var(--text-secondary); margin: 0 0 16px 0; }
.empty-state ul { list-style: none; padding: 0; margin: 0; text-align: left; }
.empty-state li { font-size: 14px; color: var(--text-secondary); margin-bottom: 8px; padding-left: 20px; position: relative; }
.empty-state li::before { content: '✓'; position: absolute; left: 0; color: var(--success-color); }

.messages-list { display: flex; flex-direction: column; gap: 20px; }

.message { display: flex; gap: 12px; animation: fadeInUp 0.4s ease backwards; }
.message.user { flex-direction: row-reverse; }
.message.user .message-content { align-items: flex-end; }
.message.user .message-bubble { background: var(--gradient-primary); color: white; border-radius: 16px 16px 4px 16px; }

.message-avatar { flex-shrink: 0; }
.user-avatar { width: 36px; height: 36px; border-radius: var(--radius-full); background: var(--gradient-primary); display: flex; align-items: center; justify-content: center; color: white; font-weight: 600; font-size: 14px; }
.ai-avatar-sm { width: 36px; height: 36px; border-radius: var(--radius-full); background: var(--primary-lighter); display: flex; align-items: center; justify-content: center; color: var(--primary-color); }
.ai-avatar-sm svg { width: 20px; height: 20px; }

.message-content { max-width: 70%; }
.message-bubble { padding: 12px 16px; background: var(--bg-page); border-radius: 16px 16px 16px 4px; font-size: 14px; line-height: 1.6; color: var(--text-primary); }
.message-time { font-size: 11px; color: var(--text-tertiary); margin-top: 4px; }
.loading-dots { animation: pulse 1.5s infinite; }
@keyframes pulse { 0%, 100% { opacity: 0.3; } 50% { opacity: 1; } }

.quick-prompts { padding: 0 24px 20px; }
.prompt-label { font-size: 12px; color: var(--text-tertiary); margin-bottom: 12px; }
.prompts-grid { display: grid; grid-template-columns: repeat(2, 1fr); gap: 12px; }
.prompt-item { display: flex; align-items: center; gap: 10px; padding: 12px 16px; background: var(--bg-page); border-radius: var(--radius-lg); cursor: pointer; transition: all var(--transition-fast); }
.prompt-item:hover { background: var(--primary-lighter); transform: translateY(-2px); }
.prompt-icon { width: 18px; height: 18px; color: var(--primary-color); }
.prompt-item span { font-size: 13px; color: var(--text-secondary); }

.input-area { padding: 16px 24px; border-top: 1px solid var(--border-light); background: var(--bg-color-secondary); }
.input-actions { display: flex; gap: 4px; }
</style>
