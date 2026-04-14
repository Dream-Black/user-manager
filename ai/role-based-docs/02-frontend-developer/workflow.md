# 前端工作流程

## 📋 开发流程

```
┌─────────────────────────────────────────────────────────────────┐
│                         开发流程                                │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  1. 环境准备                                                    │
│     ├── 安装依赖: npm install                                   │
│     ├── 启动开发: npm run dev                                  │
│     └── 确保后端API运行                                          │
│                                                                 │
│  2. 任务开发                                                    │
│     ├── 阅读需求文档                                             │
│     ├── 创建分支: git checkout -b feature/resource-management   │
│     ├── 按组件顺序开发                                          │
│     └── 提交代码: git commit                                   │
│                                                                 │
│  3. 自测验证                                                    │
│     ├── 功能测试                                                 │
│     ├── 样式检查                                                 │
│     └── 响应式测试                                               │
│                                                                 │
│  4. 代码提交                                                    │
│     ├── 推送分支: git push                                     │
│     └── 提PR合并到main                                          │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## 🎯 组件开发顺序

建议按以下顺序开发，确保依赖关系正确：

### 第一批（基础组件）
1. **图片压缩工具** `utils/imageCompress.js`
   - 后续所有功能依赖此工具
   - 独立工具类，无依赖

2. **API封装** `api/resources.js`
   - 提供数据请求能力
   - 后续页面依赖此模块

### 第二批（核心页面）
3. **路由配置** `router/index.js`
   - 资源模块路由注册
   - 路由守卫配置

4. **侧边栏修改** `components/layout/Sidebar.vue`
   - 添加资源管理入口
   - 高亮当前菜单

5. **ResourceLayout** `views/resources/ResourceLayout.vue`
   - 页面容器
   - 面包屑导航
   - 布局结构

### 第三批（列表页）
6. **ComicCard组件** `components/ComicCard.vue`
   - 漫画卡片展示
   - 独立组件

7. **漫画列表页** `views/resources/ComicList.vue`
   - 依赖ComicCard
   - 整合搜索、筛选

8. **路径设置组件** `components/PathSettingDialog.vue`
   - 弹窗组件
   - 配置入口

### 第四批（阅读页）
9. **编辑弹窗** `components/ComicEditDialog.vue`
   - 漫画信息编辑
   - 封面上传

10. **章节选择器** `components/ChapterSelector.vue`
    - 下拉组件

11. **漫画阅读页** `views/resources/ComicReader.vue`
    - 整合所有阅读功能
    - 模式切换

---

## 🔧 开发规范

### 1. 命名规范
| 类型 | 规范 | 示例 |
|------|------|------|
| 组件文件 | PascalCase | `ComicCard.vue` |
| JS/TS文件 | camelCase | `imageCompress.js` |
| CSS类名 | kebab-case | `.comic-card` |
| 路由路径 | kebab-case | `/resources/comics` |
| API函数 | camelCase | `getComics` |

### 2. 代码风格
- 使用Vue 3 Composition API
- 使用 `<script setup>` 语法
- Props使用TypeScript接口定义
- 样式使用TDesign主题变量

### 3. 图片处理
```javascript
// ✅ 正确：使用压缩工具
const base64 = await compressImage(file);

// ✅ 正确：拼接代理地址
const imageUrl = `${LOCAL_PROXY_URL}/files/read?path=${encodeURIComponent(path)}`;

// ❌ 错误：直接使用本地路径
const imageUrl = `file://${path}`;
```

### 4. 错误处理
```javascript
// ✅ 正确：try-catch + 用户提示
try {
  await saveComic(data);
  Message.success('保存成功');
} catch (error) {
  Message.error('保存失败：' + error.message);
}

// ❌ 错误：直接抛出错误
throw new Error('Save failed');
```

---

## 🧪 测试检查清单

### 功能测试
- [ ] 漫画列表加载
- [ ] 漫画搜索
- [ ] 漫画类型筛选
- [ ] 漫画编辑保存
- [ ] 封面上传压缩
- [ ] 漫画删除
- [ ] 扫描功能
- [ ] 章节切换
- [ ] 阅读模式切换
- [ ] 翻页功能

### 异常测试
- [ ] 网络断开提示
- [ ] 本地代理未启动提示
- [ ] 路径不存在提示
- [ ] 图片加载失败占位
- [ ] 表单验证提示

### 响应式测试
- [ ] 手机端 (< 768px)
- [ ] 平板端 (768px - 1024px)
- [ ] 桌面端 (> 1024px)

---

## 🐛 常见问题

### Q1: 图片无法显示
**原因**: 本地代理服务未启动
**解决**: 确保 `ProxyService.exe` 正在运行

### Q2: 封面上传失败
**原因**: 图片未压缩，Base64过长
**解决**: 检查 `imageCompress.js` 是否正确执行

### Q3: 扫描结果为空
**原因**: 资源路径配置错误
**解决**: 检查路径是否存在，格式是否正确

### Q4: 章节图片顺序混乱
**原因**: 文件系统排序不一致
**解决**: 后端按文件名自然排序，前端按sortOrder排序

---

## 📞 协作说明

### 与后端协作
1. **接口约定**：提前约定API格式
2. **接口文档**：后端提供Swagger文档
3. **Mock数据**：后端未完成时使用Mock

### 与UI设计协作
1. **设计稿**：UI提供Figma设计稿
2. **组件规格**：参考 `component-specs.md`
3. **反馈问题**：及时反馈实现难度

### 与测试协作
1. **自测报告**：提交前完成自测
2. **Bug反馈**：及时修复测试反馈的问题
3. **回归测试**：修复后确保不破坏其他功能
