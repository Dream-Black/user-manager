<template>
  <t-dialog
    v-model:visible="visible"
    header="裁剪头像"
    width="500px"
    :footer="null"
    @close="handleClose"
  >
    <div class="avatar-cropper">
      <!-- 图片选择区 -->
      <div v-if="!imageSrc" class="upload-area" @click="triggerFileInput">
        <t-upload
          ref="uploadRef"
          :multiple="false"
          accept="image/*"
          :before-upload="handleFileSelect"
          :show-upload-list="false"
        >
          <template #trigger>
            <div class="upload-trigger">
              <UploadIcon class="upload-icon" />
              <span>点击选择图片</span>
              <span class="upload-hint">支持 JPG、PNG，建议正方形图片</span>
            </div>
          </template>
        </t-upload>
      </div>

      <!-- 裁剪区域 -->
      <div v-else class="crop-container">
        <div class="crop-wrapper" ref="cropWrapperRef">
          <img
            ref="imgRef"
            :src="imageSrc"
            class="crop-image"
            @load="onImageLoad"
            @error="onImageError"
          />
          <!-- 裁剪框 -->
          <div
            class="crop-box"
            :style="cropBoxStyle"
            @mousedown="startCropDrag"
          >
            <div class="crop-area">
              <!-- 裁剪预览 -->
              <div class="crop-preview" ref="previewRef"></div>
            </div>
            <!-- 边缘调整手柄 -->
            <div class="crop-handle crop-handle-n" @mousedown.stop="startResize($event, 'n')"></div>
            <div class="crop-handle crop-handle-s" @mousedown.stop="startResize($event, 's')"></div>
            <div class="crop-handle crop-handle-w" @mousedown.stop="startResize($event, 'w')"></div>
            <div class="crop-handle crop-handle-e" @mousedown.stop="startResize($event, 'e')"></div>
            <div class="crop-handle crop-handle-nw" @mousedown.stop="startResize($event, 'nw')"></div>
            <div class="crop-handle crop-handle-ne" @mousedown.stop="startResize($event, 'ne')"></div>
            <div class="crop-handle crop-handle-sw" @mousedown.stop="startResize($event, 'sw')"></div>
            <div class="crop-handle crop-handle-se" @mousedown.stop="startResize($event, 'se')"></div>
          </div>
          <!-- 半透明遮罩 -->
          <div class="crop-overlay" :style="overlayStyle"></div>
        </div>

        <!-- 裁剪信息 -->
        <div class="crop-info">
          <span class="crop-size">{{ outputSize }} × {{ outputSize }} 像素</span>
          <t-button variant="text" size="small" @click="resetCrop">重置</t-button>
        </div>
      </div>

      <!-- 操作按钮 -->
      <div class="crop-actions">
        <t-button variant="outline" @click="handleClose">取消</t-button>
        <t-button v-if="imageSrc" variant="outline" @click="reSelectImage">重新选择</t-button>
        <t-button theme="primary" :disabled="!imageSrc" :loading="processing" @click="handleConfirm">
          确认裁剪
        </t-button>
      </div>
    </div>
  </t-dialog>
</template>

<script setup>
import { ref, computed, watch, onMounted, onUnmounted, nextTick } from 'vue'
import { UploadIcon } from 'tdesign-icons-vue-next'
import { MessagePlugin } from 'tdesign-vue-next'
import { loadImage } from '@/utils/imageProcessor'

const props = defineProps({
  modelValue: Boolean,
  outputSize: {
    type: Number,
    default: 100
  }
})

const emit = defineEmits(['update:modelValue', 'confirm'])

const visible = computed({
  get: () => props.modelValue,
  set: (val) => emit('update:modelValue', val)
})

// Refs
const uploadRef = ref(null)
const cropWrapperRef = ref(null)
const imgRef = ref(null)
const previewRef = ref(null)

// State
const imageSrc = ref('')
const imageFile = ref(null)
const imageSize = ref({ width: 0, height: 0 })
const processing = ref(false)

// 裁剪框状态
const cropState = ref({
  x: 0,
  y: 0,
  width: 0,
  height: 0
})

// 拖拽状态
const isDragging = ref(false)
const isResizing = ref(false)
const resizeDirection = ref('')
const dragStart = ref({ x: 0, y: 0 })
const cropStart = ref({ x: 0, y: 0, width: 0, height: 0 })

// 计算样式
const cropBoxStyle = computed(() => ({
  left: cropState.value.x + 'px',
  top: cropState.value.y + 'px',
  width: cropState.value.width + 'px',
  height: cropState.value.height + 'px'
}))

const overlayStyle = computed(() => {
  const { x, y, width, height } = cropState.value
  return {
    clipPath: `polygon(
      0% 0%, 100% 0%, 100% 100%, 0% 100%,
      0% 0%, 0% ${y}px, ${x}px ${y}px, ${x}px ${y + height}px, ${x + width}px ${y + height}px, ${x + width}px ${y}px, 0% ${y}px,
      100% 0%, 100% ${y}px, ${x + width}px ${y}px, ${x + width}px ${y + height}px, 100% ${y + height}px, 100% 100%, 0% 100%
    )`
  }
})

// 图片加载完成
const onImageLoad = async () => {
  if (!imgRef.value || !cropWrapperRef.value) return

  const img = imgRef.value
  const wrapper = cropWrapperRef.value
  const wrapperRect = wrapper.getBoundingClientRect()

  // 计算图片在容器中的实际尺寸和位置
  const scale = Math.min(
    wrapperRect.width / img.naturalWidth,
    wrapperRect.height / img.naturalHeight
  )

  const scaledWidth = img.naturalWidth * scale
  const scaledHeight = img.naturalHeight * scale
  const offsetX = (wrapperRect.width - scaledWidth) / 2
  const offsetY = (wrapperRect.height - scaledHeight) / 2

  // 保存原始图片尺寸
  imageSize.value = {
    width: img.naturalWidth,
    height: img.naturalHeight
  }

  // 设置裁剪框为正方形，取较小的边
  const cropSize = Math.min(scaledWidth, scaledHeight)
  const centerX = offsetX + scaledWidth / 2 - cropSize / 2
  const centerY = offsetY + scaledHeight / 2 - cropSize / 2

  cropState.value = {
    x: centerX,
    y: centerY,
    width: cropSize,
    height: cropSize
  }

  // 更新图片样式
  img.style.width = scaledWidth + 'px'
  img.style.height = scaledHeight + 'px'
  img.style.position = 'absolute'
  img.style.left = offsetX + 'px'
  img.style.top = offsetY + 'px'
}

const onImageError = () => {
  console.error('图片加载失败')
}

// 文件选择
const handleFileSelect = async (file) => {
  if (!file) return

  const validTypes = ['image/jpeg', 'image/png', 'image/gif', 'image/webp']
  if (!validTypes.includes(file.raw.type)) {
    MessagePlugin.warning('请选择图片文件（JPG、PNG、GIF、WebP）')
    return false
  }

  imageFile.value = file.raw
  const reader = new FileReader()
  reader.onload = (e) => {
    imageSrc.value = e.target.result
  }
  reader.readAsDataURL(file.raw)

  return false // 阻止默认上传
}

// 触发文件选择
const triggerFileInput = () => {
  const input = uploadRef.value?.$el?.querySelector('input[type="file"]')
  input?.click()
}

// 重新选择图片
const reSelectImage = () => {
  imageSrc.value = ''
  imageFile.value = null
  triggerFileInput()
}

// 重置裁剪
const resetCrop = () => {
  onImageLoad()
}

// 开始拖拽裁剪框
const startCropDrag = (e) => {
  if (isResizing.value) return

  isDragging.value = true
  dragStart.value = { x: e.clientX, y: e.clientY }
  cropStart.value = { ...cropState.value }

  document.addEventListener('mousemove', onCropDrag)
  document.addEventListener('mouseup', stopCropDrag)
}

const onCropDrag = (e) => {
  if (!isDragging.value || !cropWrapperRef.value) return

  const wrapper = cropWrapperRef.value
  const wrapperRect = wrapper.getBoundingClientRect()

  const deltaX = e.clientX - dragStart.value.x
  const deltaY = e.clientY - dragStart.value.y

  let newX = cropStart.value.x + deltaX
  let newY = cropStart.value.y + deltaY

  // 边界限制
  newX = Math.max(0, Math.min(newX, wrapperRect.width - cropState.value.width))
  newY = Math.max(0, Math.min(newY, wrapperRect.height - cropState.value.height))

  cropState.value.x = newX
  cropState.value.y = newY
}

const stopCropDrag = () => {
  isDragging.value = false
  document.removeEventListener('mousemove', onCropDrag)
  document.removeEventListener('mouseup', stopCropDrag)
}

// 开始调整裁剪框大小
const startResize = (e, direction) => {
  isResizing.value = true
  resizeDirection.value = direction
  dragStart.value = { x: e.clientX, y: e.clientY }
  cropStart.value = { ...cropState.value }

  document.addEventListener('mousemove', onResize)
  document.addEventListener('mouseup', stopResize)
}

const onResize = (e) => {
  if (!isResizing.value || !cropWrapperRef.value) return

  const wrapper = cropWrapperRef.value
  const wrapperRect = wrapper.getBoundingClientRect()

  const deltaX = e.clientX - dragStart.value.x
  const deltaY = e.clientY - dragStart.value.y

  let { x, y, width, height } = cropStart.value

  const minSize = 50

  switch (resizeDirection.value) {
    case 'e':
      width = Math.max(minSize, Math.min(width + deltaX, wrapperRect.width - x))
      break
    case 'w':
      const newWidthW = width - deltaX
      if (newWidthW >= minSize && x + deltaX >= 0) {
        width = newWidthW
        x += deltaX
      }
      break
    case 's':
      height = Math.max(minSize, Math.min(height + deltaY, wrapperRect.height - y))
      break
    case 'n':
      const newHeightN = height - deltaY
      if (newHeightN >= minSize && y + deltaY >= 0) {
        height = newHeightN
        y += deltaY
      }
      break
    case 'se':
      width = Math.max(minSize, Math.min(width + deltaX, wrapperRect.width - x))
      height = Math.max(minSize, Math.min(height + deltaY, wrapperRect.height - y))
      break
    case 'sw':
      const newWidthSW = width - deltaX
      if (newWidthSW >= minSize && x + deltaX >= 0) {
        width = newWidthSW
        x += deltaX
      }
      height = Math.max(minSize, Math.min(height + deltaY, wrapperRect.height - y))
      break
    case 'ne':
      width = Math.max(minSize, Math.min(width + deltaX, wrapperRect.width - x))
      const newHeightNE = height - deltaY
      if (newHeightNE >= minSize && y + deltaY >= 0) {
        height = newHeightNE
        y += deltaY
      }
      break
    case 'nw':
      const newWidthNW = width - deltaX
      if (newWidthNW >= minSize && x + deltaX >= 0) {
        width = newWidthNW
        x += deltaX
      }
      const newHeightNW = height - deltaY
      if (newHeightNW >= minSize && y + deltaY >= 0) {
        height = newHeightNW
        y += deltaY
      }
      break
  }

  // 保持正方形
  const size = Math.max(width, height)
  width = size
  height = size

  cropState.value = { x, y, width, height }
}

const stopResize = () => {
  isResizing.value = false
  resizeDirection.value = ''
  document.removeEventListener('mousemove', onResize)
  document.removeEventListener('mouseup', stopResize)
}

// 确认裁剪
const handleConfirm = async () => {
  if (!imageFile.value || !imgRef.value) return

  processing.value = true

  try {
    // 计算裁剪区域（相对于原始图片）
    const img = imgRef.value
    const wrapper = cropWrapperRef.value
    const wrapperRect = wrapper.getBoundingClientRect()

    // 图片缩放比例
    const scaleX = img.naturalWidth / parseFloat(img.style.width)
    const scaleY = img.naturalHeight / parseFloat(img.style.height)

    // 裁剪区域（相对于原始图片）
    const cropX = cropState.value.x * scaleX
    const cropY = cropState.value.y * scaleY
    const cropWidth = cropState.value.width * scaleX
    const cropHeight = cropState.value.height * scaleY

    // 创建canvas进行裁剪
    const canvas = document.createElement('canvas')
    const targetSize = props.outputSize
    canvas.width = targetSize
    canvas.height = targetSize

    const ctx = canvas.getContext('2d')

    // 加载原图
    const originalImg = await loadImage(imageFile.value)

    // 绘制裁剪区域
    ctx.drawImage(
      originalImg,
      cropX, cropY, cropWidth, cropHeight,
      0, 0, targetSize, targetSize
    )

    // 转为base64
    const base64 = canvas.toDataURL('image/jpeg', 0.8)

    emit('confirm', {
      base64,
      file: imageFile.value,
      width: targetSize,
      height: targetSize
    })

    handleClose()
  } catch (error) {
    console.error('裁剪失败:', error)
    MessagePlugin.error('裁剪失败，请重试')
  } finally {
    processing.value = false
  }
}

// 关闭弹窗
const handleClose = () => {
  visible.value = false
  imageSrc.value = ''
  imageFile.value = null
  cropState.value = { x: 0, y: 0, width: 0, height: 0 }
}

// 清理事件监听
onUnmounted(() => {
  document.removeEventListener('mousemove', onCropDrag)
  document.removeEventListener('mouseup', stopCropDrag)
  document.removeEventListener('mousemove', onResize)
  document.removeEventListener('mouseup', stopResize)
})
</script>

<style scoped>
.avatar-cropper {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.upload-area {
  padding: 20px;
}

.upload-trigger {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 40px;
  border: 2px dashed var(--border-color);
  border-radius: var(--radius-lg);
  cursor: pointer;
  transition: all 0.2s;
}

.upload-trigger:hover {
  border-color: var(--primary-color);
  background: var(--bg-hover);
}

.upload-icon {
  width: 48px;
  height: 48px;
  color: var(--text-tertiary);
  margin-bottom: 12px;
}

.upload-trigger span {
  color: var(--text-secondary);
  font-size: 14px;
}

.upload-hint {
  font-size: 12px !important;
  color: var(--text-tertiary) !important;
  margin-top: 8px;
}

.crop-container {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.crop-wrapper {
  position: relative;
  width: 100%;
  height: 300px;
  background: #1a1a1a;
  border-radius: var(--radius-lg);
  overflow: hidden;
}

.crop-image {
  max-width: 100%;
  max-height: 100%;
  user-select: none;
}

.crop-box {
  position: absolute;
  border: 2px solid white;
  box-shadow: 0 0 0 9999px rgba(0, 0, 0, 0.5);
  cursor: move;
  z-index: 10;
}

.crop-area {
  width: 100%;
  height: 100%;
  position: relative;
}

.crop-preview {
  width: 100%;
  height: 100%;
  /* 裁剪区域预览 */
}

.crop-overlay {
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(0, 0, 0, 0.5);
  pointer-events: none;
}

/* 裁剪手柄 */
.crop-handle {
  position: absolute;
  width: 10px;
  height: 10px;
  background: white;
  border: 1px solid #333;
  border-radius: 2px;
}

.crop-handle-n { top: -5px; left: 50%; transform: translateX(-50%); cursor: n-resize; }
.crop-handle-s { bottom: -5px; left: 50%; transform: translateX(-50%); cursor: s-resize; }
.crop-handle-w { left: -5px; top: 50%; transform: translateY(-50%); cursor: w-resize; }
.crop-handle-e { right: -5px; top: 50%; transform: translateY(-50%); cursor: e-resize; }
.crop-handle-nw { top: -5px; left: -5px; cursor: nw-resize; }
.crop-handle-ne { top: -5px; right: -5px; cursor: ne-resize; }
.crop-handle-sw { bottom: -5px; left: -5px; cursor: sw-resize; }
.crop-handle-se { bottom: -5px; right: -5px; cursor: se-resize; }

.crop-info {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 0 4px;
}

.crop-size {
  font-size: 12px;
  color: var(--text-tertiary);
}

.crop-actions {
  display: flex;
  justify-content: flex-end;
  gap: 12px;
  padding-top: 16px;
  border-top: 1px solid var(--border-light);
}
</style>
