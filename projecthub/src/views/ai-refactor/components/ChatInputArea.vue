<template>
  <div class="chat-input-area">
    <!-- 附件预览栏 -->
    <div v-if="pendingAttachments.length" class="attachment-preview-bar">
      <div v-for="(att, idx) in pendingAttachments" :key="idx" class="att-preview-item">
        <span class="att-icon">
          <FileText v-if="isTextFile(att.name)" :size="14" />
          <Image v-else :size="14" />
        </span>
        <span class="att-name">{{ att.name }}</span>
        <button class="att-remove" @click="$emit('remove-attachment', idx)">
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
          @change="$emit('file-select', $event)"
        />
      </label>

      <textarea
        ref="inputRef"
        :value="modelValue"
        class="chat-input"
        placeholder="输入消息，Enter 发送，Shift+Enter 换行..."
        rows="1"
        :disabled="disabled"
        @input="onInput"
        @keydown="handleKeydown"
      ></textarea>

      <button
        class="send-btn"
        :disabled="!modelValue.trim() || disabled"
        @click="$emit('send')"
      >
        <Send :size="18" />
      </button>
    </div>
  </div>
</template>

<script setup>
import { ref } from 'vue'
import { Paperclip, X, FileText, Image, Send } from 'lucide-vue-next'

const props = defineProps({
  modelValue: { type: String, default: '' },
  pendingAttachments: { type: Array, default: () => [] },
  disabled: { type: Boolean, default: false }
})

const emit = defineEmits([
  'update:modelValue',
  'send',
  'file-select',
  'remove-attachment'
])

const inputRef = ref(null)

function onInput(e) {
  emit('update:modelValue', e.target.value)
  autoResize()
}

function handleKeydown(e) {
  if (e.key === 'Enter' && !e.shiftKey) {
    e.preventDefault()
    emit('send')
  }
}

function autoResize() {
  const el = inputRef.value
  if (!el) return
  el.style.height = 'auto'
  el.style.height = Math.min(el.scrollHeight, 120) + 'px'
}

function resetHeight() {
  const el = inputRef.value
  if (el) el.style.height = 'auto'
}

function isTextFile(name) {
  const ext = name.split('.').pop()?.toLowerCase()
  return ['txt', 'md', 'json', 'xml', 'csv', 'log'].includes(ext)
}

function focus() {
  inputRef.value?.focus()
}

defineExpose({ resetHeight, focus })
</script>

<style scoped>
.chat-input-area {
  padding: var(--space-5) var(--space-6);
  border-top: 1px solid var(--border-light);
  background: rgba(255,255,255,0.9);
  backdrop-filter: blur(16px);
  flex-shrink: 0;
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
.send-btn:disabled { opacity: 0.45; cursor: not-allowed; box-shadow: none; }
</style>
