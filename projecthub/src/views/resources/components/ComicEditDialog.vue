<template>
  <t-dialog
    :visible="visible"
    header="编辑漫画"
    :close-on-overlay-click="false"
    :close-on-esc-keydown="false"
    width="500px"
    @close="$emit('update:visible', false)"
  >
    <t-form :data="formData" :rules="rules" ref="formRef">
      <!-- 封面预览 -->
      <div class="thumbnail-preview">
        <img 
          v-if="thumbnailPreview" 
          :src="thumbnailPreview" 
          alt="封面预览" 
        />
        <div v-else-if="comic?.thumbnailBase64" class="current-thumbnail">
          <img :src="comic.thumbnailBase64" alt="当前封面" />
          <div class="thumbnail-label">当前封面</div>
        </div>
        <div v-else class="no-thumbnail">
          <t-icon name="image" size="48px" />
          <span>暂无封面</span>
        </div>
        
        <div class="thumbnail-actions">
          <t-upload
            ref="uploadRef"
            :files="uploadFiles"
            :handle-change="handleFileChange"
            accept="image/*"
            :size-limit="{ size: 5, unit: 'MB' }"
            :tips="thumbnailPreview ? '已选择新图片' : '支持 JPG、PNG，最大 5MB'"
            @fail="handleUploadFail"
          >
            <t-button variant="outline" size="small">
              <template #icon><t-icon name="upload" /></template>
              {{ thumbnailPreview ? '更换封面' : '上传封面' }}
            </t-button>
          </t-upload>
        </div>
      </div>

      <!-- 名称 -->
      <t-form-item label="名称" name="displayName">
        <t-input 
          v-model="formData.displayName" 
          placeholder="请输入漫画名称"
          :maxlength="100"
          show-word-limit
        />
      </t-form-item>

      <!-- 类型 -->
      <t-form-item label="类型" name="type">
        <t-select v-model="formData.type" placeholder="请选择类型">
          <t-option value="manga" label="日漫" />
          <t-option value="comic" label="美漫" />
          <t-option value="novel" label="小说" />
          <t-option value="picture" label="图集" />
        </t-select>
      </t-form-item>
    </t-form>

    <template #footer>
      <t-button variant="outline" @click="handleCancel">取消</t-button>
      <t-button theme="primary" :loading="saving" @click="handleSave">保存</t-button>
    </template>
  </t-dialog>
</template>

<script setup lang="ts">
import { ref, watch, computed } from 'vue';
import { MessagePlugin } from 'tdesign-vue-next';
import type { FormRules } from 'tdesign-vue-next';
import { compressImage } from '@/utils/imageCompress';
import type { Comic } from '@/api/resources';

interface Props {
  visible: boolean;
  comic: Comic | null;
}

interface Emits {
  (e: 'update:visible', visible: boolean): void;
  (e: 'save', data: { displayName: string; type: string; thumbnail?: string }): void;
}

const props = defineProps<Props>();
const emit = defineEmits<Emits>();

// 状态
const saving = ref(false);
const uploadFiles = ref([]);
const thumbnailPreview = ref<string | null>(null);
const originalThumbnail = ref<string | null>(null);
const formRef = ref();

// 表单数据
const formData = ref({
  displayName: '',
  type: 'manga' as 'manga' | 'comic' | 'novel' | 'picture',
});

// 表单规则
const rules: FormRules = {
  displayName: [
    { required: true, message: '请输入漫画名称', trigger: 'blur' },
    { min: 1, max: 100, message: '名称长度在 1-100 个字符', trigger: 'blur' },
  ],
  type: [
    { required: true, message: '请选择类型', trigger: 'change' },
  ],
};

// 是否已修改
const hasChanges = computed(() => {
  if (!props.comic) return false;
  return (
    formData.value.displayName !== props.comic.displayName ||
    formData.value.type !== props.comic.type ||
    thumbnailPreview.value !== null
  );
});

// 监听visible变化，初始化表单
watch(() => props.visible, (newVal) => {
  if (newVal && props.comic) {
    formData.value = {
      displayName: props.comic.displayName,
      type: props.comic.type as 'manga' | 'comic' | 'novel' | 'picture',
    };
    thumbnailPreview.value = null;
    originalThumbnail.value = props.comic.thumbnailBase64 || null;
    uploadFiles.value = [];
  }
});

// 文件选择
async function handleFileChange(files: File[]) {
  if (files.length === 0) return;
  
  const file = files[0];
  
  // 压缩并预览
  try {
    const result = await compressImage(file, {
      maxWidth: 800,
      maxSize: 200 * 1024,
    });
    thumbnailPreview.value = result.base64;
    uploadFiles.value = [{ name: file.name, status: 'success' }];
  } catch (error) {
    console.error('图片压缩失败:', error);
    MessagePlugin.error('图片压缩失败');
  }
}

// 上传失败
function handleUploadFail({ file }: { file: File }) {
  MessagePlugin.error(`文件 ${file.name} 上传失败`);
}

// 取消
function handleCancel() {
  if (hasChanges.value) {
    const dialog = window.confirm('有未保存的更改，确定要关闭吗？');
    if (!dialog) return;
  }
  emit('update:visible', false);
}

// 保存
async function handleSave() {
  try {
    await formRef.value.validate();
  } catch {
    return;
  }
  
  saving.value = true;
  
  try {
    emit('save', {
      displayName: formData.value.displayName,
      type: formData.value.type,
      thumbnail: thumbnailPreview.value || undefined,
    });
  } finally {
    saving.value = false;
  }
}
</script>

<style scoped>
.thumbnail-preview {
  display: flex;
  flex-direction: column;
  align-items: center;
  margin-bottom: 24px;
}

.thumbnail-preview img {
  width: 160px;
  height: 213px;
  object-fit: cover;
  border-radius: 8px;
  margin-bottom: 12px;
}

.current-thumbnail {
  position: relative;
  margin-bottom: 12px;
}

.thumbnail-label {
  position: absolute;
  bottom: 0;
  left: 0;
  right: 0;
  padding: 4px;
  background: rgba(0, 0, 0, 0.6);
  color: #fff;
  font-size: 12px;
  text-align: center;
  border-radius: 0 0 8px 8px;
}

.no-thumbnail {
  width: 160px;
  height: 213px;
  display: flex;
  flex-direction: column;
  justify-content: center;
  align-items: center;
  background: var(--td-bg-color-secondary-container);
  border-radius: 8px;
  margin-bottom: 12px;
  color: var(--td-text-color-disabled);
  gap: 8px;
}

.thumbnail-actions {
  margin-top: 8px;
}

.t-form :deep(.t-form__item) {
  margin-bottom: 20px;
}
</style>
