import { ref } from 'vue'
import { terminalService } from '@/services/dataService'
import { aiService } from '@/services/dataService'

/**
 * 终端执行相关逻辑
 * @param {Object} options
 * @param {import('vue').Ref} options.messages - 消息列表 ref
 * @param {import('vue').Ref} options.currentConvId - 当前对话 id ref
 * @param {Function} options.scrollToBottom - 滚动到底部的函数
 * @param {Function} options.reloadMessages - 重新加载消息的函数
 */
export function useTerminal({ messages, currentConvId, scrollToBottom, reloadMessages }) {
  const terminalExecutionInProgress = ref(false)
  const pendingTerminalCommand = ref(null)
  const terminalResult = ref(null)
  const terminalResultCollapsed = ref(true)
  const continueStreaming = ref(false)
  const continueStreamingMsgId = ref(null)
  const continueContentBuffer = ref('')

  function safeParse(str) {
    try { return JSON.parse(str) }
    catch { return null }
  }

  async function checkAndExecuteTerminalCommand(text) {
    if (!terminalService.isDesktopEnv()) return false
    const detection = terminalService.detectTerminalCommand(text)
    if (detection.hasCommand) {
      pendingTerminalCommand.value = detection.command
      await executeTerminalCommand(detection.command)
      return true
    }
    return false
  }

  async function executeTerminalCommand(command) {
    try {
      terminalExecutionInProgress.value = true
      const result = await terminalService.executeCommand(command)
      await sendTerminalResultToAI(command, result)
    } catch (e) {
      console.error('[Terminal] 执行命令失败:', e)
      await sendTerminalResultToAI(command, { success: false, error: e.message })
    } finally {
      terminalExecutionInProgress.value = false
      pendingTerminalCommand.value = null
    }
  }

  async function sendTerminalResultToAI(command, result) {
    terminalResult.value = {
      command,
      success: result.success,
      stdout: result.stdout || '',
      stderr: result.stderr || '',
      error: result.error || ''
    }
    terminalResultCollapsed.value = false

    const toolResultText = `
命令: ${command}
成功: ${result.success}
${result.stdout ? `标准输出:\n${result.stdout}` : ''}
${result.stderr ? `错误输出:\n${result.stderr}` : ''}
${result.error ? `错误信息:\n${result.error}` : ''}
    `.trim()

    await scrollToBottom()

    try {
      continueStreaming.value = true
      continueContentBuffer.value = ''

      const aiMsgId = 'continue_' + Date.now()
      continueStreamingMsgId.value = aiMsgId

      messages.value.push({
        _id: aiMsgId,
        role: 'assistant',
        content: '',
        reasoningContent: '',
        toolCalls: null,
        filesJson: null
      })

      const response = await aiService.continueChat(currentConvId.value, toolResultText)
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
          const data = safeParse(dataLine.substring(6).trim())
          if (!data) continue

          const idx = messages.value.findIndex(m => m._id === aiMsgId)
          if (idx === -1) continue

          switch (data.type) {
            case 'content':
              continueContentBuffer.value += data.content
              messages.value[idx].content = continueContentBuffer.value
              await scrollToBottom()
              break
            case 'reasoning':
              messages.value[idx].reasoningContent =
                (messages.value[idx].reasoningContent || '') + data.content
              break
            case 'error':
              messages.value[idx].content = `❌ ${data.content}`
              break
          }
        }
      }
    } catch (e) {
      console.error('[Terminal] 继续对话失败:', e)
      const errorIdx = messages.value.findIndex(m => m._id === continueStreamingMsgId.value)
      if (errorIdx !== -1) {
        messages.value[errorIdx].content = `❌ 继续对话失败: ${e.message}`
      }
    } finally {
      if (continueStreaming.value === 'terminal') return

      const terminalText = continueContentBuffer.value
      continueStreaming.value = false
      continueStreamingMsgId.value = null
      continueContentBuffer.value = ''
      terminalResult.value = null

      if (terminalText && await checkAndExecuteTerminalCommand(terminalText)) {
        continueStreaming.value = 'terminal'
        return
      }

      await reloadMessages()
    }
  }

  return {
    terminalExecutionInProgress,
    pendingTerminalCommand,
    terminalResult,
    terminalResultCollapsed,
    continueStreaming,
    checkAndExecuteTerminalCommand
  }
}
