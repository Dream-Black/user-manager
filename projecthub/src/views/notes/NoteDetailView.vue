<template>
  <div class="note-detail-page">
    <div class="page-header">
      <t-button variant="text" @click="handleBack">
        <template #icon><ArrowLeftIcon /></template>
        返回
      </t-button>
    </div>

    <div class="note-content-wrapper">
      <div class="note-title-section">
        <t-input
          v-model="noteTitle"
          placeholder="笔记标题"
          size="large"
          borderless
          class="note-title-input"
          @change="handleTitleChange"
        />
      </div>

      <div class="note-tags-section">
        <div class="tags-container">
          <t-tag
            v-for="tag in noteTags"
            :key="tag.value"
            :color="getTagColor(tag.value)"
            variant="light"
            closable
            @close="handleRemoveTag(tag.value)"
          >
            {{ tag.label }}
          </t-tag>
          <t-input
            v-model="newTagText"
            placeholder="添加标签"
            size="small"
            class="tag-input"
            @enter="handleAddTag"
          />
        </div>
      </div>

      <div class="note-body-section">
        <t-textarea
          v-model="noteBody"
          placeholder="开始写笔记..."
          borderless
          autosize
          class="note-body-input"
          @change="handleBodyChange"
        />
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ArrowLeftIcon } from 'tdesign-icons-vue-next'
import { noteService } from '@/services/dataService'

const route = useRoute()
const router = useRouter()

const noteId = ref(null)
const noteTitle = ref('')
const noteBody = ref('')
const noteTags = ref([])
const newTagText = ref('')

let saveTimer = null

const tagOptions = [
  { value: 'work', label: '工作' },
  { value: 'study', label: '学习' },
  { value: 'life', label: '生活' },
  { value: 'idea', label: '想法' }
]

const tagColors = {
  work: '#0052d9',
  study: '#00a870',
  life: '#e37318',
  idea: '#834ec2'
}

const getTagColor = (tagId) => {
  return tagColors[tagId] || '#0052d9'
}

const loadNote = async () => {
  noteId.value = Number(route.params.id)
  try {
    const note = await noteService.getById(noteId.value)
    noteTitle.value = note.title
    noteBody.value = note.content || ''
    noteTags.value = (note.tagIds || []).map(tagId => {
      const tag = tagOptions.find(t => t.value === tagId)
      return tag || { value: tagId, label: tagId }
    })
  } catch (error) {
    console.error('加载笔记失败:', error)
  }
}

const saveNote = async () => {
  if (!noteId.value) return

  try {
    await noteService.update(noteId.value, {
      title: noteTitle.value,
      content: noteBody.value,
      tagIds: noteTags.value.map(t => t.value)
    })
  } catch (error) {
    console.error('保存笔记失败:', error)
  }
}

const debouncedSave = () => {
  if (saveTimer) {
    clearTimeout(saveTimer)
  }
  saveTimer = setTimeout(() => {
    saveNote()
  }, 500)
}

const handleBack = () => {
  if (saveTimer) {
    clearTimeout(saveTimer)
    saveNote()
  }
  router.push('/notes')
}

const handleTitleChange = () => {
  debouncedSave()
}

const handleBodyChange = () => {
  debouncedSave()
}

const handleAddTag = async () => {
  if (!newTagText.value.trim()) return

  const existingTag = tagOptions.find(
    t => t.label === newTagText.value.trim() || t.value === newTagText.value.trim()
  )

  if (existingTag) {
    const exists = noteTags.value.some(t => t.value === existingTag.value)
    if (exists) {
      return
    }
    noteTags.value.push(existingTag)
  } else {
    const newValue = newTagText.value.trim()
    noteTags.value.push({ value: newValue, label: newValue })
  }

  newTagText.value = ''
  await saveNote()
}

const handleRemoveTag = async (tagValue) => {
  noteTags.value = noteTags.value.filter(t => t.value !== tagValue)
  await saveNote()
}

onMounted(() => {
  loadNote()
})
</script>

<style scoped>
.note-detail-page {
  max-width: 900px;
  margin: 0 auto;
  padding: 24px;
}

.page-header {
  margin-bottom: 24px;
}

.note-content-wrapper {
  background: var(--bg-card-solid);
  border-radius: var(--radius-xl);
  padding: 32px;
  border: 1px solid var(--border-light);
}

.note-title-section {
  margin-bottom: 20px;
}

.note-title-input {
  font-size: 24px;
  font-weight: 600;
}

.note-title-input :deep(.t-input) {
  font-size: 24px;
  font-weight: 600;
  border: none;
  background: transparent;
}

.note-title-input :deep(.t-input__inner) {
  font-size: 24px;
  font-weight: 600;
}

.note-title-input :deep(.t-input__wrapper) {
  border: none;
  background: transparent;
  box-shadow: none;
}

.note-tags-section {
  margin-bottom: 24px;
  padding-bottom: 20px;
  border-bottom: 1px solid var(--border-light);
}

.tags-container {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  align-items: center;
}

.tag-input {
  width: 100px;
}

.tag-input :deep(.t-input) {
  border: none;
  background: transparent;
  box-shadow: none;
}

.tag-input :deep(.t-input__wrapper) {
  border: none;
  background: transparent;
  box-shadow: none;
}

.note-body-section {
  min-height: 400px;
}

.note-body-input {
  min-height: 400px;
  font-size: 15px;
  line-height: 1.8;
}

.note-body-input :deep(.t-textarea) {
  border: none;
  background: transparent;
}

.note-body-input :deep(.t-textarea__wrapper) {
  border: none;
  background: transparent;
  box-shadow: none;
}

.note-body-input :deep(.t-textarea__inner) {
  min-height: 400px;
  font-size: 15px;
  line-height: 1.8;
  resize: none;
  border: none;
  background: transparent;
}
</style>
