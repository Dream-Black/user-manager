# 开发计划 - AI串行执行

## 📋 开发顺序

```
1. 后端开发 (ASP.NET Core)     → 约2小时
2. 本地代理服务 (Python)       → 约2小时
3. 前端开发 (Vue + TDesign)    → 约4小时
4. 自测验证                     → 约1小时
─────────────────────────────────
总计: 约9小时
```

---

## 🚀 启动命令

### 后端 (C# ASP.NET Core)
```bash
cd c:/Users/22618/Desktop/AI Claw/api
dotnet run
# 启动端口: 5000
```

### 本地代理 (Python FastAPI)
```bash
cd c:/Users/22618/Desktop/AI Claw/proxy
python main.py
# 启动端口: 6789
```

### 前端 (Vue)
```bash
cd c:/Users/22618/Desktop/AI Claw/projecthub
npm run dev
# 启动端口: 8081
```

---

## 📁 执行清单

### 阶段1: 后端开发 ✅ 已完成
- [x] 1.1 创建数据模型 (4个Model) ✅
- [x] 1.2 创建ComputersController ✅
- [x] 1.3 创建ResourcePathsController ✅
- [x] 1.4 创建ComicsController ✅
- [x] 1.5 创建ChaptersController ✅
- [x] 1.6 更新AppDbContext ✅
- [x] 1.7 数据库迁移 ✅

### 阶段2: 本地代理 ✅ 已完成
- [x] 2.1 创建proxy目录结构 ✅
- [x] 2.2 config.py 配置管理 ✅
- [x] 2.3 scanner.py 文件扫描 ✅
- [x] 2.4 main.py FastAPI服务 ✅
- [x] 2.5 打包脚本 build.bat ✅

### 阶段3: 前端开发 ✅ 已完成
- [x] 3.1 路由配置 ✅
- [x] 3.2 侧边栏导航 ✅
- [x] 3.3 imageCompress.ts 图片压缩 ✅
- [x] 3.4 api/resources.ts API封装 ✅
- [x] 3.5 ComicCard.vue 组件 ✅
- [x] 3.6 ResourceList.vue 列表页 ✅
- [x] 3.7 ComicEditDialog.vue 编辑弹窗 ✅
- [x] 3.8 PathSettingDialog.vue 设置弹窗 ✅
- [x] 3.9 ComicReader.vue 阅读页 ✅
- [x] 3.10 ChapterSelector.vue 章节选择器 ✅

### 阶段4: 自测验证
- [ ] 4.1 后端API测试
- [ ] 4.2 前端页面测试
- [ ] 4.3 联调测试

---

## 📝 详细文件清单

### 后端文件
```
api/
├── Models/
│   ├── Computer.cs
│   ├── ResourcePath.cs
│   ├── Comic.cs
│   └── ComicChapter.cs
├── Controllers/
│   ├── ComputersController.cs
│   ├── ResourcePathsController.cs
│   ├── ComicsController.cs
│   └── ChaptersController.cs
└── Data/
    └── AppDbContext.cs  # 修改
```

### 前端文件
```
projecthub/src/
├── api/
│   └── resources.js     # 新增
├── router/
│   └── index.js         # 修改
├── views/
│   └── resources/       # 新增目录
│       ├── ResourceLayout.vue
│       ├── ComicList.vue
│       ├── ComicReader.vue
│       └── components/
│           ├── ComicCard.vue
│           ├── ComicEditDialog.vue
│           ├── PathSettingDialog.vue
│           └── ChapterSelector.vue
├── components/
│   └── layout/
│       └── Sidebar.vue  # 修改
└── utils/
    └── imageCompress.js # 新增
```

### 代理文件
```
proxy/
├── main.py
├── config.py
├── scanner.py
└── build.bat
```

---

## ⚠️ 注意事项

1. **先启动后端** - 前端依赖API
2. **本地代理独立** - 不依赖后端，可单独测试
3. **数据库迁移** - 确保MySQL服务运行
4. **代理打包** - PyInstaller需要安装

---

## 🔗 相关文档

- 详细API设计: `ai/role-based-docs/01-backend-developer/api-design.md`
- 数据模型: `ai/role-based-docs/01-backend-developer/data-models.md`
- 组件规格: `ai/role-based-docs/02-frontend-developer/component-specs.md`
