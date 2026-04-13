<template>
  <div class="categories-page">
    <!-- 页面头部 -->
    <div class="page-header">
      <div class="header-content">
        <h1 class="page-title">分类管理</h1>
        <p class="page-subtitle">整理项目、任务和文档的分类体系</p>
      </div>
      <div class="header-actions">
        <t-button theme="primary" @click="showCreateDialog = true">
          <template #icon><AddIcon /></template>
          新建分类
        </t-button>
      </div>
    </div>

    <!-- 分类统计 -->
    <div class="category-stats">
      <div class="stat-item">
        <span class="stat-value">{{ categories.length }}</span>
        <span class="stat-label">总分类数</span>
      </div>
      <div class="stat-divider"></div>
      <div class="stat-item">
        <span class="stat-value">{{ totalProjects }}</span>
        <span class="stat-label">关联项目</span>
      </div>
    </div>

    <!-- 分类网格 -->
    <div class="categories-grid">
      <div
        v-for="(category, index) in categories"
        :key="category.id"
        class="category-card"
        :style="{ animationDelay: `${index * 0.05}s` }"
      >
        <div class="card-header" :style="{ background: category.gradient }">
          <div class="card-icon">
            <component :is="category.iconComponent" />
          </div>
          <div class="card-actions" @click.stop>
            <t-dropdown trigger="click">
              <t-button variant="text" theme="default">
                <template #icon><MoreIcon /></template>
              </t-button>
              <template #dropdown>
                <t-dropdown-menu>
                  <t-dropdown-item @click="handleEdit(category)">编辑</t-dropdown-item>
                  <t-dropdown-item @click="handleMove(category, 'up')">上移</t-dropdown-item>
                  <t-dropdown-item @click="handleMove(category, 'down')">下移</t-dropdown-item>
                  <t-dropdown-item divider />
                  <t-dropdown-item theme="error" @click="handleDelete(category)">删除</t-dropdown-item>
                </t-dropdown-menu>
              </template>
            </t-dropdown>
          </div>
        </div>

        <div class="card-body">
          <h3 class="category-name">{{ category.name }}</h3>
          <p class="category-description">{{ category.description }}</p>

          <div class="category-stats-row">
            <div class="stat">
              <span class="stat-value">{{ category.projectCount }}</span>
              <span class="stat-label">项目</span>
            </div>
            <div class="stat">
              <span class="stat-value">{{ category.taskCount }}</span>
              <span class="stat-label">任务</span>
            </div>
            <div class="stat">
              <span class="stat-value">{{ category.fileCount }}</span>
              <span class="stat-label">文件</span>
            </div>
          </div>

          <div class="category-color">
            <span class="color-dot" :style="{ background: category.color }"></span>
            <span class="color-value">{{ category.color }}</span>
          </div>
        </div>
      </div>

      <!-- 添加卡片 -->
      <div class="add-card" @click="showCreateDialog = true">
        <div class="add-icon">
          <AddIcon />
        </div>
        <span>添加分类</span>
      </div>
    </div>

    <!-- 创建/编辑弹窗 -->
    <t-dialog
      v-model:visible="showCreateDialog"
      :header="editingCategory ? '编辑分类' : '新建分类'"
      width="500px"
      :footer="null"
    >
      <t-form ref="formRef" :data="formData" :rules="rules" label-align="top">
        <t-form-item label="分类名称" name="name">
          <t-input v-model="formData.name" placeholder="请输入分类名称" />
        </t-form-item>

        <t-form-item label="分类描述" name="description">
          <t-textarea v-model="formData.description" placeholder="请输入分类描述" />
        </t-form-item>

        <t-form-item label="选择图标" name="icon">
          <div class="icon-picker">
            <div
              v-for="icon in iconOptions"
              :key="icon.name"
              class="icon-option"
              :class="{ active: formData.icon === icon.name }"
              @click="formData.icon = icon.name"
            >
              <component :is="icon.component" />
            </div>
          </div>
        </t-form-item>

        <t-form-item label="选择颜色" name="color">
          <div class="color-picker">
            <div
              v-for="color in colorOptions"
              :key="color"
              class="color-option"
              :class="{ active: formData.color === color }"
              :style="{ background: color }"
              @click="formData.color = color"
            ></div>
          </div>
        </t-form-item>

        <div class="form-actions">
          <t-button variant="outline" @click="showCreateDialog = false">取消</t-button>
          <t-button theme="primary" @click="handleSubmit">确定</t-button>
        </div>
      </t-form>
    </t-dialog>

    <!-- 删除确认 -->
    <t-dialog
      v-model:visible="showDeleteDialog"
      header="删除分类"
      width="400px"
      :footer="null"
    >
      <div class="delete-confirm">
        <p class="delete-text">
          确定要删除分类 <strong>{{ deletingCategory?.name }}</strong> 吗？<br/>
          关联的项目不会被删除。
        </p>
        <div class="delete-actions">
          <t-button variant="outline" @click="showDeleteDialog = false">取消</t-button>
          <t-button theme="danger" @click="confirmDelete">确认删除</t-button>
        </div>
      </div>
    </t-dialog>
  </div>
</template>

<script setup>
import { ref, computed, markRaw } from 'vue'
import { MessagePlugin } from 'tdesign-vue-next'

// 状态
const showCreateDialog = ref(false)
const showDeleteDialog = ref(false)
const editingCategory = ref(null)
const deletingCategory = ref(null)
const formRef = ref(null)

// 图标选项
const iconOptions = [
  { name: 'folder', component: markRaw(FolderIcon) },
  { name: 'code', component: markRaw(CodeIcon) },
  { name: 'design', component: markRaw(PaletteIcon) },
  { name: 'chart', component: markRaw(ChartIcon) },
  { name: 'document', component: markRaw(FileIcon) },
  { name: 'image', component: markRaw(ImageIcon) },
  { name: 'video', component: markRaw(VideoIcon) },
  { name: 'music', component: markRaw(MusicIcon) }
]

// 颜色选项
const colorOptions = [
  '#2196F3', '#4CAF50', '#FF9800', '#9C27B0',
  '#00BCD4', '#E91E63', '#3F51B5', '#FF5722',
  '#795548', '#607D8B'
]

// 分类数据
const categories = ref([
  {
    id: 1, name: '前端开发', description: 'Web前端相关项目',
    icon: 'code', iconComponent: markRaw(CodeIcon),
    color: '#2196F3', gradient: 'linear-gradient(135deg, #2196F3 0%, #1976D2 100%)',
    projectCount: 5, taskCount: 24, fileCount: 18
  },
  {
    id: 2, name: '后端服务', description: '服务端和API开发',
    icon: 'server', iconComponent: markRaw(ServerIcon),
    color: '#4CAF50', gradient: 'linear-gradient(135deg, #4CAF50 0%, #388E3C 100%)',
    projectCount: 3, taskCount: 15, fileCount: 12
  },
  {
    id: 3, name: '移动应用', description: 'iOS和Android开发',
    icon: 'mobile', iconComponent: markRaw(MobileIcon),
    color: '#FF9800', gradient: 'linear-gradient(135deg, #FF9800 0%, #F57C00 100%)',
    projectCount: 2, taskCount: 12, fileCount: 8
  },
  {
    id: 4, name: 'UI/UX设计', description: '界面设计和用户体验',
    icon: 'design', iconComponent: markRaw(PaletteIcon),
    color: '#9C27B0', gradient: 'linear-gradient(135deg, #9C27B0 0%, #7B1FA2 100%)',
    projectCount: 4, taskCount: 18, fileCount: 45
  },
  {
    id: 5, name: '数据分析', description: '数据处理和分析',
    icon: 'chart', iconComponent: markRaw(ChartIcon),
    color: '#00BCD4', gradient: 'linear-gradient(135deg, #00BCD4 0%, #0097A7 100%)',
    projectCount: 2, taskCount: 8, fileCount: 22
  },
  {
    id: 6, name: '文档资料', description: '项目文档和教程',
    icon: 'document', iconComponent: markRaw(FileIcon),
    color: '#607D8B', gradient: 'linear-gradient(135deg, #607D8B 0%, #455A64 100%)',
    projectCount: 0, taskCount: 0, fileCount: 56
  }
])

// 表单数据
const formData = ref({
  name: '',
  description: '',
  icon: 'folder',
  color: '#2196F3'
})

// 规则
const rules = {
  name: [{ required: true, message: '请输入分类名称', trigger: 'blur' }]
}

// 统计
const totalProjects = computed(() => {
  return categories.value.reduce((sum, c) => sum + c.projectCount, 0)
})

// 编辑
const handleEdit = (category) => {
  editingCategory.value = category
  formData.value = { ...category }
  showCreateDialog.value = true
}

// 删除
const handleDelete = (category) => {
  deletingCategory.value = category
  showDeleteDialog.value = true
}

// 确认删除
const confirmDelete = () => {
  categories.value = categories.value.filter(c => c.id !== deletingCategory.value.id)
  showDeleteDialog.value = false
  MessagePlugin.success('分类已删除')
}

// 移动
const handleMove = (category, direction) => {
  const index = categories.value.findIndex(c => c.id === category.id)
  const newIndex = direction === 'up' ? index - 1 : index + 1
  if (newIndex >= 0 && newIndex < categories.value.length) {
    const temp = categories.value[index]
    categories.value[index] = categories.value[newIndex]
    categories.value[newIndex] = temp
  }
}

// 提交
const handleSubmit = async () => {
  const result = await formRef.value.validate()
  if (result === true) {
    const selectedIcon = iconOptions.find(i => i.name === formData.value.icon)

    if (editingCategory.value) {
      const index = categories.value.findIndex(c => c.id === editingCategory.value.id)
      if (index !== -1) {
        categories.value[index] = {
          ...categories.value[index],
          ...formData.value,
          gradient: `linear-gradient(135deg, ${formData.value.color} 0%, ${formData.value.color}dd 100%)`,
          iconComponent: selectedIcon?.component || markRaw(FolderIcon)
        }
      }
      MessagePlugin.success('分类已更新')
    } else {
      categories.value.unshift({
        id: Date.now(),
        ...formData.value,
        gradient: `linear-gradient(135deg, ${formData.value.color} 0%, ${formData.value.color}dd 100%)`,
        iconComponent: selectedIcon?.component || markRaw(FolderIcon),
        projectCount: 0,
        taskCount: 0,
        fileCount: 0
      })
      MessagePlugin.success('分类已创建')
    }
    showCreateDialog.value = false
    editingCategory.value = null
    formData.value = { name: '', description: '', icon: 'folder', color: '#2196F3' }
  }
}

// 图标
import {
  AddIcon, MoreIcon, FolderIcon, CodeIcon, PaletteIcon,
  ChartIcon, FileIcon, ImageIcon, VideoIcon, MusicIcon,
  ServerIcon, MobileIcon
} from 'tdesign-icons-vue-next'
</script>

<style scoped>
.categories-page {
  max-width: 1600px;
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

.page-subtitle {
  font-size: 14px;
  color: var(--text-secondary);
  margin: 0;
}

/* 统计 */
.category-stats {
  display: flex;
  align-items: center;
  gap: 24px;
  padding: 20px 24px;
  background: var(--bg-container);
  border-radius: var(--radius-xl);
  margin-bottom: 24px;
  border: 1px solid var(--border-color);
  animation: fadeInUp 0.5s ease 0.1s backwards;
}

.stat-item {
  display: flex;
  flex-direction: column;
}

.stat-item .stat-value {
  font-size: 24px;
  font-weight: 700;
  color: var(--text-primary);
}

.stat-item .stat-label {
  font-size: 13px;
  color: var(--text-secondary);
}

.stat-divider {
  width: 1px;
  height: 40px;
  background: var(--border-color);
}

/* 网格 */
.categories-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
  gap: 20px;
}

.category-card {
  background: var(--bg-container);
  border-radius: var(--radius-xl);
  overflow: hidden;
  border: 1px solid var(--border-color);
  transition: all var(--transition-base);
  animation: cardEnter 0.6s ease backwards;
}

.category-card:hover {
  transform: translateY(-4px);
  box-shadow: var(--shadow-lg);
  border-color: transparent;
}

.card-header {
  position: relative;
  height: 80px;
  padding: 16px;
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
}

.card-icon {
  width: 48px;
  height: 48px;
  border-radius: var(--radius-lg);
  background: rgba(255, 255, 255, 0.2);
  display: flex;
  align-items: center;
  justify-content: center;
  color: white;
  backdrop-filter: blur(4px);
}

.card-icon svg {
  width: 24px;
  height: 24px;
}

.card-body {
  padding: 20px;
}

.category-name {
  font-size: 18px;
  font-weight: 600;
  color: var(--text-primary);
  margin: 0 0 8px 0;
}

.category-description {
  font-size: 13px;
  color: var(--text-secondary);
  margin: 0 0 16px 0;
  line-height: 1.5;
}

.category-stats-row {
  display: flex;
  gap: 16px;
  padding: 16px 0;
  border-top: 1px solid var(--border-light);
  border-bottom: 1px solid var(--border-light);
  margin-bottom: 16px;
}

.stat {
  flex: 1;
  text-align: center;
}

.stat .stat-value {
  display: block;
  font-size: 20px;
  font-weight: 700;
  color: var(--text-primary);
}

.stat .stat-label {
  font-size: 12px;
  color: var(--text-tertiary);
}

.category-color {
  display: flex;
  align-items: center;
  gap: 8px;
}

.color-dot {
  width: 12px;
  height: 12px;
  border-radius: var(--radius-full);
}

.color-value {
  font-size: 12px;
  font-family: var(--font-family-mono);
  color: var(--text-tertiary);
}

/* 添加卡片 */
.add-card {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 12px;
  min-height: 260px;
  background: var(--bg-page);
  border: 2px dashed var(--border-color);
  border-radius: var(--radius-xl);
  cursor: pointer;
  transition: all var(--transition-fast);
}

.add-card:hover {
  border-color: var(--brand-color);
  background: var(--brand-color-light);
}

.add-icon {
  width: 56px;
  height: 56px;
  border-radius: var(--radius-full);
  background: var(--bg-container);
  display: flex;
  align-items: center;
  justify-content: center;
  color: var(--brand-color);
  transition: all var(--transition-bounce);
}

.add-card:hover .add-icon {
  transform: scale(1.1) rotate(90deg);
}

.add-card span {
  font-size: 14px;
  font-weight: 500;
  color: var(--text-secondary);
}

/* 图标选择 */
.icon-picker {
  display: grid;
  grid-template-columns: repeat(8, 1fr);
  gap: 8px;
}

.icon-option {
  width: 40px;
  height: 40px;
  border-radius: var(--radius-md);
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  transition: all var(--transition-fast);
  background: var(--bg-page);
  color: var(--text-secondary);
}

.icon-option:hover {
  background: var(--brand-color-light);
  color: var(--brand-color);
}

.icon-option.active {
  background: var(--brand-color);
  color: white;
}

.icon-option svg {
  width: 20px;
  height: 20px;
}

/* 颜色选择 */
.color-picker {
  display: flex;
  gap: 8px;
  flex-wrap: wrap;
}

.color-option {
  width: 32px;
  height: 32px;
  border-radius: var(--radius-md);
  cursor: pointer;
  transition: all var(--transition-fast);
  border: 2px solid transparent;
}

.color-option:hover {
  transform: scale(1.15);
}

.color-option.active {
  border-color: var(--text-primary);
  box-shadow: 0 0 0 2px var(--bg-container);
}

/* 表单 */
.form-actions {
  display: flex;
  justify-content: flex-end;
  gap: 12px;
  margin-top: 24px;
  padding-top: 24px;
  border-top: 1px solid var(--border-light);
}

/* 删除确认 */
.delete-confirm {
  text-align: center;
}

.delete-text {
  font-size: 14px;
  color: var(--text-secondary);
  line-height: 1.6;
  margin: 0 0 24px 0;
}

.delete-actions {
  display: flex;
  justify-content: center;
  gap: 12px;
}
</style>
