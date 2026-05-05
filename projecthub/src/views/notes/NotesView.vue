<template>
  <div class="notes-page">
    <div class="page-header">
      <div class="header-content">
        <h1 class="page-title">笔记</h1>
      </div>
      <div class="header-actions">
        <t-button theme="primary" @click="handleAddNote">
          <template #icon><AddIcon /></template>
          新增笔记
        </t-button>
      </div>
    </div>

    <div class="search-bar">
      <t-input
        v-model="searchQuery"
        placeholder="搜索笔记..."
        clearable
      >
        <template #prefix-icon>
          <SearchIcon />
        </template>
      </t-input>
    </div>

    <div class="filter-bar">
      <div class="filter-left">
        <t-check-tag-group
          v-model="selectedTags"
          :options="tagOptions"
          @change="handleTagChange"
        />
      </div>
      <div class="filter-right">
        <t-dropdown trigger="click">
          <t-button variant="outline" size="medium">
            <template #icon><UnfoldMoreIcon /></template>
            {{ sortOptions.find(o => o.value === sortBy)?.label || '排序' }}
          </t-button>
          <template #dropdown>
            <t-dropdown-menu>
              <t-dropdown-item
                v-for="option in sortOptions"
                :key="option.value"
                :value="option.value"
                :active="sortBy === option.value"
                @click="handleSortChange(option.value)"
              >
                {{ option.label }}
              </t-dropdown-item>
            </t-dropdown-menu>
          </template>
        </t-dropdown>
      </div>
    </div>

    <div class="notes-grid">
      <div
        v-for="note in filteredNotes"
        :key="note.id"
        class="note-card"
        @click="handleEditNote(note)"
      >
        <div class="note-card-header">
          <h3 class="note-title">{{ note.title }}</h3>
          <t-button variant="text" size="small" @click.stop="handleDeleteNote(note)">
            <template #icon><DeleteIcon /></template>
          </t-button>
        </div>
        <p class="note-content">{{ note.content }}</p>
        <div class="note-footer">
          <div class="note-tags">
            <t-tag
              v-for="tagId in note.tagIds"
              :key="tagId"
              :color="getTagColor(tagId)"
              variant="light"
              size="small"
            >
              {{ getTagLabel(tagId) }}
            </t-tag>
          </div>
          <span class="note-date">{{ note.updatedAt }}</span>
        </div>
      </div>

      <div v-if="filteredNotes.length === 0" class="notes-empty">
        暂无笔记
      </div>
    </div>

    <t-dialog
      v-model:visible="showDeleteDialog"
      header="删除笔记"
      width="400px"
      :footer="null"
    >
      <div class="delete-confirm">
        <p class="delete-text">确定要删除笔记 <strong>{{ deletingNote?.title }}</strong> 吗？</p>
        <div class="delete-actions">
          <t-button variant="outline" @click="showDeleteDialog = false">取消</t-button>
          <t-button theme="danger" @click="confirmDelete">确认删除</t-button>
        </div>
      </div>
    </t-dialog>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, watch } from 'vue'
import { useRouter } from 'vue-router'
import { MessagePlugin } from 'tdesign-vue-next'
import { AddIcon, SearchIcon, DeleteIcon, UnfoldMoreIcon } from 'tdesign-icons-vue-next'
import { noteService } from '@/services/dataService'

const router = useRouter()

const searchQuery = ref('')
const sortBy = ref('updatedAt')
const selectedTags = ref([])
const showDeleteDialog = ref(false)
const deletingNote = ref(null)

const sortOptions = [
  { value: 'updatedAt', label: '最新更新时间' },
  { value: 'title', label: '标题' },
  { value: 'tagCount', label: '标签' }
]

const tagOptions = ref([])

const tagColors = {
  work: '#0052d9',
  study: '#00a870',
  life: '#e37318',
  idea: '#834ec2'
}

const tagLabels = {
  work: '工作',
  study: '学习',
  life: '生活',
  idea: '想法'
}

const notes = ref([])

const loadTags = async () => {
  try {
    const tags = await noteService.getTags()
    tagOptions.value = tags.map(tag => ({
      value: tag,
      label: tagLabels[tag] || tag
    }))
  } catch (error) {
    console.error('加载标签失败:', error)
  }
}

const loadNotes = async () => {
  try {
    const params = {}
    if (selectedTags.value.length > 0) {
      params.tags = selectedTags.value.join(',')
    }
    params.sort = sortBy.value
    const data = await noteService.getAll(params)
    notes.value = data
  } catch (error) {
    console.error('加载笔记失败:', error)
    MessagePlugin.error('加载笔记失败')
  }
}

const filteredNotes = computed(() => {
  let result = [...notes.value]

  if (searchQuery.value) {
    const query = searchQuery.value.toLowerCase()
    result = result.filter(note =>
      note.title.toLowerCase().includes(query) ||
      note.content.toLowerCase().includes(query)
    )
  }

  return result
})

const getTagLabel = (tagId) => {
  return tagLabels[tagId] || tagId
}

const getTagColor = (tagId) => {
  return tagColors[tagId] || '#0052d9'
}

const handleAddNote = async () => {
  try {
    const newNote = await noteService.create({
      title: '未命名笔记',
      content: '',
      tagIds: []
    })
    router.push(`/notes/${newNote.id}`)
  } catch (error) {
    console.error('创建笔记失败:', error)
    MessagePlugin.error('创建笔记失败')
  }
}

const handleEditNote = (note) => {
  router.push(`/notes/${note.id}`)
}

const handleDeleteNote = (note) => {
  deletingNote.value = note
  showDeleteDialog.value = true
}

const confirmDelete = async () => {
  try {
    await noteService.delete(deletingNote.value.id)
    notes.value = notes.value.filter(n => n.id !== deletingNote.value.id)
    showDeleteDialog.value = false
    MessagePlugin.success('笔记已删除')
  } catch (error) {
    console.error('删除笔记失败:', error)
    MessagePlugin.error('删除失败')
  }
}

const handleSortChange = (value) => {
  sortBy.value = value
  loadNotes()
}

const handleTagChange = () => {
  loadNotes()
}

watch(selectedTags, () => {
  loadNotes()
}, { deep: true })

onMounted(() => {
  loadTags()
  loadNotes()
})
</script>

<style scoped>
.notes-page {
  margin: 0 auto;
}

.page-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: 24px;
  animation: fadeInUp 0.5s ease;
}

.header-content {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.page-title {
  font-size: 28px;
  font-weight: 700;
  color: var(--text-primary);
  margin: 0;
}

.search-bar {
  margin-bottom: 16px;
  animation: fadeInUp 0.5s ease 0.05s backwards;
}

.search-bar :deep(.t-input) {
  max-width: 400px;
}

.filter-bar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 16px;
  margin-bottom: 20px;
  animation: fadeInUp 0.5s ease 0.1s backwards;
  flex-wrap: wrap;
}

.filter-left {
  display: flex;
  gap: 8px;
  flex-wrap: wrap;
}

.filter-right {
  display: flex;
  gap: 8px;
}

.notes-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(320px, 1fr));
  gap: 16px;
  animation: fadeInUp 0.5s ease 0.15s backwards;
}

.note-card {
  background: var(--bg-card-solid);
  border-radius: var(--radius-xl);
  padding: 20px;
  border: 1px solid var(--border-light);
  cursor: pointer;
  transition: all var(--transition-normal);
  box-shadow: var(--shadow-sm);
  display: flex;
  flex-direction: column;
  min-height: 160px;
}

.note-card:hover {
  border-color: var(--primary-color);
  box-shadow: var(--shadow-md);
  transform: translateY(-2px);
}

.note-card-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: 12px;
}

.note-title {
  font-size: 16px;
  font-weight: 600;
  color: var(--text-primary);
  margin: 0;
  flex: 1;
  line-height: 1.4;
}

.note-content {
  font-size: 13px;
  color: var(--text-secondary);
  margin: 0 0 12px 0;
  line-height: 1.6;
  flex: 1;
  display: -webkit-box;
  -webkit-line-clamp: 3;
  -webkit-box-orient: vertical;
  overflow: hidden;
}

.note-footer {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-top: auto;
  padding-top: 12px;
  border-top: 1px solid var(--border-light);
}

.note-tags {
  display: flex;
  gap: 6px;
  flex-wrap: wrap;
}

.note-date {
  font-size: 12px;
  color: var(--text-tertiary);
  white-space: nowrap;
}

.notes-empty {
  grid-column: 1 / -1;
  text-align: center;
  padding: 60px 20px;
  color: var(--text-tertiary);
  font-size: 14px;
}

.delete-confirm {
  text-align: center;
}

.delete-text {
  font-size: 14px;
  color: var(--text-secondary);
  margin: 0 0 24px 0;
}

.delete-actions {
  display: flex;
  justify-content: center;
  gap: 12px;
}

@keyframes fadeInUp {
  from {
    opacity: 0;
    transform: translateY(10px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

@media (max-width: 768px) {
  .filter-bar {
    flex-direction: column;
    align-items: stretch;
  }

  .filter-right {
    justify-content: flex-end;
  }

  .notes-grid {
    grid-template-columns: 1fr;
  }
}
</style>
