# 组件规格说明

## 📦 组件目录
```
projecthub/src/views/resources/components/
├── ComicCard.vue           # 漫画卡片
├── ComicEditDialog.vue    # 编辑弹窗
├── PathSettingDialog.vue   # 路径设置弹窗
└── ChapterSelector.vue     # 章节选择器
```

---

## 1. ComicCard 漫画卡片

### 组件位置
`projecthub/src/views/resources/components/ComicCard.vue`

### Props
```typescript
interface Props {
  comic: {
    id: number;
    displayName: string;
    folderName: string;
    type: string;         // manga/comic/novel/picture
    thumbnailBase64: string;
    chapterCount: number;
  };
  onEdit: (comic: Comic) => void;
  onDelete: (id: number) => void;
}
```

### 显示效果
```
┌──────────────────────────┐
│                          │
│      [封面图片]          │
│      16:9比例             │
│                          │
├──────────────────────────┤
│ 咒术回战                 │
│ 第1-5话                  │
│                          │
│              [⋮ 更多]    │
└──────────────────────────┘
```

### 类型标签颜色
| 类型 | 颜色 | 说明 |
|------|------|------|
| manga | #0052D9 | 日漫（蓝色） |
| comic | #FF9800 | 美漫（橙色） |
| novel | #4CAF50 | 小说（绿色） |
| picture | #9C27B0 | 图集（紫色） |

### 事件
| 事件名 | 参数 | 说明 |
|--------|------|------|
| click | comic | 点击卡片，进入阅读页 |
| edit | comic | 点击编辑 |
| delete | id | 点击删除 |

### 状态
| 状态 | 表现 |
|------|------|
| 默认 | 正常显示封面和标题 |
| 加载中 | 显示loading骨架屏 |
| 无封面 | 显示默认占位图（类型对应图标） |
| 悬停 | 显示菜单按钮 |

---

## 2. ComicEditDialog 编辑弹窗

### 组件位置
`projecthub/src/views/resources/components/ComicEditDialog.vue`

### Props
```typescript
interface Props {
  visible: boolean;
  comic: Comic | null;     // null表示新增模式
  onClose: () => void;
  onSave: (data: ComicEditData) => Promise<void>;
}
```

### 表单字段
| 字段 | 类型 | 验证 | 说明 |
|------|------|------|------|
| displayName | input | 必填，1-100字符 | 显示名称 |
| type | select | 必填 | 漫画类型 |
| thumbnail | file | 可选，<5MB | 封面图片 |

### 弹窗布局
```
┌─────────────────────────────────────┐
│ 编辑漫画                        ✕  │
├─────────────────────────────────────┤
│                                     │
│   ┌─────────────────────────┐      │
│   │                         │      │
│   │    [封面预览]           │      │
│   │    [点击上传]           │      │
│   │                         │      │
│   └─────────────────────────┘      │
│                                     │
│   名称: [________________]        │
│                                     │
│   类型: [漫画类型      ▼]         │
│                                     │
│                                     │
├─────────────────────────────────────┤
│              [取消]  [保存]         │
└─────────────────────────────────────┘
```

### 图片压缩流程
1. 用户选择图片 → File对象
2. 调用 `imageCompress.js` 工具
3. 压缩到200KB以内
4. 转为Base64
5. 保存时提交到后端

### 事件
| 事件名 | 参数 | 说明 |
|--------|------|------|
| update:visible | visible | 控制弹窗显示/隐藏 |
| save | ComicEditData | 保存数据 |

---

## 3. PathSettingDialog 路径设置弹窗

### 组件位置
`projecthub/src/views/resources/components/PathSettingDialog.vue`

### Props
```typescript
interface Props {
  visible: boolean;
  currentPath: string;
  onClose: () => void;
  onSave: (path: string) => Promise<void>;
}
```

### 弹窗布局
```
┌─────────────────────────────────────┐
│ 设置资源路径                     ✕  │
├─────────────────────────────────────┤
│                                     │
│  📁 漫画文件夹                      │
│                                     │
│  当前路径: D:/MyComics/           │
│                                     │
│  [选择文件夹]  [清除]              │
│                                     │
│  ✓ 路径已存在                      │
│  ✓ 包含 10 个漫画文件夹            │
│                                     │
├─────────────────────────────────────┤
│              [取消]  [保存]         │
└─────────────────────────────────────┘
```

### 功能说明
1. **选择文件夹**：调用系统文件选择器
2. **路径验证**：验证路径是否存在且可读
3. **预览扫描**：显示该路径下检测到的漫画数量
4. **保存配置**：调用API保存路径

### 状态
| 状态 | 表现 |
|------|------|
| 未选择 | 显示"请选择文件夹" |
| 路径无效 | 显示红色警告 |
| 路径有效 | 显示绿色对勾 + 扫描预览 |
| 保存中 | 按钮显示loading |

---

## 4. ChapterSelector 章节选择器

### 组件位置
`projecthub/src/views/resources/components/ChapterSelector.vue`

### Props
```typescript
interface Props {
  chapters: Chapter[];
  currentChapterId: number;
  onChange: (chapterId: number) => void;
}
```

### 显示效果
```
┌──────────────────────────────────────────────────┐
│ 第1话 ▼                                          │
├──────────────────────────────────────────────────┤
│ 第1话 ✓（当前）                                  │
│ 第2话                                            │
│ 第3话                                            │
│ ...                                              │
└──────────────────────────────────────────────────┘
```

### 交互说明
1. 点击当前章节 → 展开下拉列表
2. 上下键或鼠标选择章节
3. 选择后 → 关闭下拉，触发onChange
4. 自动滚动到可视区域

---

## 5. 图片压缩工具

### 工具位置
`projecthub/src/utils/imageCompress.js`

### 使用方法
```javascript
import { compressImage } from '@/utils/imageCompress';

// 压缩图片
const result = await compressImage(file, {
  maxWidth: 800,        // 最大宽度
  maxSize: 200 * 1024,   // 最大200KB
  quality: 0.8          // 初始质量
});

// 返回结果
// result.base64 - 压缩后的Base64字符串
// result.width - 实际宽度
// result.height - 实际高度
```

### 压缩算法
1. 缩放图片到目标宽度（保持比例）
2. 转Canvas绘制
3. 调整quality直到文件大小 < maxSize
4. 输出Base64

---

## 6. API调用封装

### API位置
`projecthub/src/api/resources.js`

### API列表
```javascript
// 电脑相关
export function getCurrentComputer() { ... }
export function updateComputer(id, data) { ... }
export function sendHeartbeat(id) { ... }

// 资源路径相关
export function getResourcePaths(computerId) { ... }
export function createResourcePath(data) { ... }
export function updateResourcePath(id, data) { ... }
export function deleteResourcePath(id) { ... }

// 漫画相关
export function getComics(params) { ... }
export function getComic(id) { ... }
export function updateComic(id, data) { ... }
export function deleteComic(id) { ... }
export function uploadThumbnail(id, base64) { ... }
export function scanComics(resourcePathId) { ... }

// 章节相关
export function getChapters(comicId) { ... }
export function getChapterPages(chapterId) { ... }
```

### 请求拦截
所有API请求自动添加请求头：
```javascript
headers: {
  'X-Computer-Hostname': getComputerHostName()
}
```
