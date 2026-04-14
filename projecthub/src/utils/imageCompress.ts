/**
 * 图片压缩工具
 * 将图片压缩到指定大小以内，并转为Base64
 */

interface CompressOptions {
  maxWidth?: number;      // 最大宽度，默认800
  maxHeight?: number;     // 最大高度，默认1200
  maxSize?: number;       // 最大文件大小（字节），默认200KB
  quality?: number;       // 初始质量，0-1，默认0.8
  outputType?: 'base64' | 'blob';  // 输出类型，默认base64
}

interface CompressResult {
  base64: string;         // 压缩后的Base64字符串
  width: number;          // 实际宽度
  height: number;        // 实际高度
  size: number;          // 实际文件大小
  originalSize: number;   // 原始文件大小
}

/**
 * 压缩图片
 * @param file - 图片文件
 * @param options - 压缩选项
 * @returns 压缩结果
 */
export async function compressImage(
  file: File,
  options: CompressOptions = {}
): Promise<CompressResult> {
  const {
    maxWidth = 800,
    maxHeight = 1200,
    maxSize = 200 * 1024, // 200KB
    quality = 0.8,
    outputType = 'base64',
  } = options;

  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    
    reader.onload = (e) => {
      const img = new Image();
      
      img.onload = () => {
        // 计算缩放后的尺寸
        let { width, height } = img;
        
        if (width > maxWidth) {
          height = (height * maxWidth) / width;
          width = maxWidth;
        }
        
        if (height > maxHeight) {
          width = (width * maxHeight) / height;
          height = maxHeight;
        }
        
        // 创建Canvas
        const canvas = document.createElement('canvas');
        canvas.width = width;
        canvas.height = height;
        
        const ctx = canvas.getContext('2d');
        if (!ctx) {
          reject(new Error('无法获取Canvas上下文'));
          return;
        }
        
        // 绘制图片
        ctx.drawImage(img, 0, 0, width, height);
        
        // 迭代压缩直到文件大小符合要求
        let currentQuality = quality;
        let result: string;
        
        const tryCompress = () => {
          result = canvas.toDataURL('image/jpeg', currentQuality);
          const size = result.length - result.indexOf(',') - 1;
          
          if (size > maxSize && currentQuality > 0.1) {
            currentQuality -= 0.1;
            return tryCompress();
          }
          
          return result;
        };
        
        const base64 = tryCompress();
        
        resolve({
          base64,
          width,
          height,
          size: base64.length - base64.indexOf(',') - 1,
          originalSize: file.size,
        });
      };
      
      img.onerror = () => reject(new Error('图片加载失败'));
      img.src = e.target?.result as string;
    };
    
    reader.onerror = () => reject(new Error('文件读取失败'));
    reader.readAsDataURL(file);
  });
}

/**
 * 将Base64转为Blob
 */
export function base64ToBlob(base64: string): Blob {
  const parts = base64.split(',');
  const mime = parts[0].match(/:(.*?);/)?.[1] || 'image/jpeg';
  const byteString = atob(parts[1]);
  const ab = new ArrayBuffer(byteString.length);
  const ia = new Uint8Array(ab);
  
  for (let i = 0; i < byteString.length; i++) {
    ia[i] = byteString.charCodeAt(i);
  }
  
  return new Blob([ab], { type: mime });
}

/**
 * 从文件创建预览URL
 */
export function createPreviewUrl(file: File): string {
  return URL.createObjectURL(file);
}

/**
 * 释放预览URL
 */
export function revokePreviewUrl(url: string): void {
  URL.revokeObjectURL(url);
}

/**
 * 验证图片文件
 */
export function validateImageFile(file: File): { valid: boolean; error?: string } {
  const validTypes = ['image/jpeg', 'image/png', 'image/gif', 'image/webp'];
  
  if (!validTypes.includes(file.type)) {
    return { valid: false, error: '只支持 JPG、PNG、GIF、WebP 格式' };
  }
  
  if (file.size > 5 * 1024 * 1024) {
    return { valid: false, error: '图片大小不能超过 5MB' };
  }
  
  return { valid: true };
}
