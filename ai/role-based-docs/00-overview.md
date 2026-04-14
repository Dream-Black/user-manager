# 资源管理模块开发计划 - 角色文档总览

## 📋 项目信息
- **项目名称**：AI Claw / ProjectHub
- **模块名称**：资源管理模块
- **开发周期**：约22小时（26个任务）
- **最后更新**：2026-04-14

---

## 👥 角色分工

| 角色 | 负责人 | 核心职责 | 任务数量 |
|------|--------|----------|----------|
| [后端开发](./01-backend-developer/) | - | API、数据库、数据模型 | 8个任务 |
| [前端开发](./02-frontend-developer/) | - | Vue页面、组件、交互 | 10个任务 |
| [UI设计师](./03-ui-designer/) | - | 界面设计、规范输出 | 设计交付物 |
| [API测试](./04-api-tester/) | - | 接口验证、联调测试 | 测试用例 |

---

## 📁 文档目录结构

```
ai/role-based-docs/
├── 00-overview.md                    # 本文档（总览）
├── 01-backend-developer/              # 后端开发文档
│   ├── README.md                      # 后端开发指南
│   ├── data-models.md                 # 数据模型设计
│   ├── api-design.md                  # API接口设计
│   └── database-migration.md           # 数据库迁移指南
├── 02-frontend-developer/             # 前端开发文档
│   ├── README.md                      # 前端开发指南
│   ├── page-design.md                 # 页面设计需求
│   ├── component-specs.md             # 组件规格说明
│   └── workflow.md                    # 前端工作流程
├── 03-ui-designer/                    # UI设计师文档
│   ├── README.md                      # 设计要求说明
│   ├── color-typography.md             # 色彩与字体规范
│   ├── layout-wireframes.md            # 布局线框图
│   └── component-designs.md            # 组件设计要求
└── 04-api-tester/                     # API测试文档
    ├── README.md                      # 测试指南
    ├── api-test-cases.md              # 测试用例
    └── test-scripts.md                # 测试脚本
```

---

## 🎯 开发阶段

### 阶段一：后端开发（8个任务，约6小时）
1. 创建数据模型
2. 实现ComputersController
3. 实现ResourcePathsController
4. 实现ComicsController
5. 实现ChaptersController
6. 更新AppDbContext
7. 数据库迁移
8. 前端API封装

### 阶段二：前端开发（10个任务，约10小时）
1. 路由配置
2. 侧边栏导航
3. ResourceLayout布局
4. 漫画列表页
5. ComicCard组件
6. 图片压缩工具
7. 编辑弹窗
8. 漫画阅读页
9. 路径设置组件
10. 章节选择器

### 阶段三：本地代理服务（6个任务，约5小时）
1. 项目结构搭建
2. 配置管理模块
3. 文件扫描模块
4. FastAPI主服务
5. 配置引导流程
6. PyInstaller打包

### 阶段四：测试联调（2个任务，约1小时）
1. 前后端联调测试
2. E2E端到端测试

---

## 📌 技术栈

| 层级 | 技术 | 说明 |
|------|------|------|
| 前端框架 | Vue 3 + TypeScript + Vite | 现有项目 |
| UI组件库 | TDesign | 腾讯企业级UI |
| 后端框架 | ASP.NET Core 9 | 现有项目 |
| ORM | Entity Framework Core | 现有项目 |
| 数据库 | MySQL 8.0 | 现有配置 |
| 本地代理 | Python FastAPI | 新增服务 |

---

## 🔗 相关文档

- [架构设计评审](../memory-bank/architecture-review.md)
- [项目记忆](../memory-bank/site-setup.md)
- [色彩系统](../design/color-system.md)
- [交互原型](../design/interaction-prototype.md)
- [UI风格指南](../design/ui-style-guide.md)
