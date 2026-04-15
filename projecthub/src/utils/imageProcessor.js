/**
 * 图片处理工具 - 压缩和裁剪
 * 将图片压缩并裁剪为指定尺寸（默认100x100）
 */

/**
 * 压缩并裁剪图片为指定尺寸
 * @param {File|Blob|string} input - 图片文件、Blob或DataURL
 * @param {Object} options - 配置选项
 * @param {number} options.width - 目标宽度，默认100
 * @param {number} options.height - 目标高度，默认100
 * @param {number} options.quality - 输出质量0-1，默认0.8
 * @returns {Promise<{dataUrl: string, blob: Blob}>}
 */
export async function processImage(input, options = {}) {
  const { width = 100, height = 100, quality = 0.8 } = options

  // 获取图片元素
  const img = await loadImage(input)

  // 创建Canvas
  const canvas = document.createElement('canvas')
  canvas.width = width
  canvas.height = height

  const ctx = canvas.getContext('2d')

  // 绘制裁剪后的图片（居中填充）
  ctx.drawImage(img, 0, 0, width, height)

  // 转换为DataURL和Blob
  const dataUrl = canvas.toDataURL('image/jpeg', quality)
  const blob = await dataUrlToBlob(dataUrl)

  return { dataUrl, blob }
}

/**
 * 加载图片
 * @param {File|Blob|string} input
 * @returns {Promise<HTMLImageElement>}
 */
export function loadImage(input) {
  return new Promise((resolve, reject) => {
    const img = new Image()
    img.crossOrigin = 'anonymous'

    img.onload = () => resolve(img)
    img.onerror = reject

    if (input instanceof File || input instanceof Blob) {
      img.src = URL.createObjectURL(input)
    } else {
      img.src = input
    }
  })
}

/**
 * DataURL转Blob
 * @param {string} dataUrl
 * @returns {Promise<Blob>}
 */
export function dataUrlToBlob(dataUrl) {
  return new Promise((resolve) => {
    fetch(dataUrl)
      .then(res => res.blob())
      .then(resolve)
  })
}

/**
 * Blob转DataURL
 * @param {Blob} blob
 * @returns {Promise<string>}
 */
export function blobToDataUrl(blob) {
  return new Promise((resolve, reject) => {
    const reader = new FileReader()
    reader.onload = () => resolve(reader.result)
    reader.onerror = reject
    reader.readAsDataURL(blob)
  })
}

/**
 * 压缩图片（保持比例）
 * @param {File|Blob} file
 * @param {number} maxWidth - 最大宽度
 * @param {number} maxHeight - 最大高度
 * @param {number} quality - 质量0-1
 * @returns {Promise<string>} - DataURL
 */
export async function compressImage(file, maxWidth = 800, maxHeight = 800, quality = 0.8) {
  const img = await loadImage(file)

  let { width, height } = img

  // 计算缩放比例
  if (width > maxWidth || height > maxHeight) {
    const ratio = Math.min(maxWidth / width, maxHeight / height)
    width = Math.round(width * ratio)
    height = Math.round(height * ratio)
  }

  const canvas = document.createElement('canvas')
  canvas.width = width
  canvas.height = height

  const ctx = canvas.getContext('2d')
  ctx.drawImage(img, 0, 0, width, height)

  return canvas.toDataURL('image/jpeg', quality)
}

/**
 * 获取图片原始尺寸
 * @param {File|Blob|string} input
 * @returns {Promise<{width: number, height: number}>}
 */
export function getImageSize(input) {
  return new Promise((resolve, reject) => {
    const img = new Image()
    img.crossOrigin = 'anonymous'

    img.onload = () => {
      resolve({ width: img.naturalWidth, height: img.naturalHeight })
    }
    img.onerror = reject

    if (input instanceof File || input instanceof Blob) {
      img.src = URL.createObjectURL(input)
    } else {
      img.src = input
    }
  })
}

/**
 * 裁剪图片（支持选择区域）
 * @param {File|Blob|string} input - 源图片
 * @param {Object} cropArea - 裁剪区域 { x, y, width, height }
 * @param {number} outputWidth - 输出宽度
 * @param {number} outputHeight - 输出高度
 * @param {number} quality - 质量
 * @returns {Promise<{dataUrl: string, blob: Blob}>}
 */
export async function cropImage(input, cropArea, outputWidth = 100, outputHeight = 100, quality = 0.8) {
  const img = await loadImage(input)

  const canvas = document.createElement('canvas')
  canvas.width = outputWidth
  canvas.height = outputHeight

  const ctx = canvas.getContext('2d')

  // 绘制裁剪区域
  ctx.drawImage(
    img,
    cropArea.x,
    cropArea.y,
    cropArea.width,
    cropArea.height,
    0,
    0,
    outputWidth,
    outputHeight
  )

  const dataUrl = canvas.toDataURL('image/jpeg', quality)
  const blob = await dataUrlToBlob(dataUrl)

  return { dataUrl, blob }
}

export default {
  processImage,
  loadImage,
  dataUrlToBlob,
  blobToDataUrl,
  compressImage,
  getImageSize,
  cropImage
}
