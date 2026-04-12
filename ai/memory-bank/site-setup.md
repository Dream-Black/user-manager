# 个人管理系统 - ProjectHub

## 项目概述

**项目名称**: ProjectHub（个人工作管理系统）
**项目类型**: 个人生产力工具 / SaaS风格应用
**核心定位**: 帮助个人管理项目任务、追踪时间历程、复盘总结经验，逐步扩展到资料、财务、人生规划等模块
**目标用户**: 自由职业者、斜杠青年、需要管理私单和项目的个人

---

## 一、MVP功能规格（第一阶段）

### 1.1 核心功能

#### 1.1.1 项目管理
- **项目创建**: 名称、描述、类型（工作/私单）、客户（可选）、开始日期、预计结束日期、状态（进行中/已完成/暂停/归档）
- **项目列表**: 卡片式展示，支持筛选（全部/工作/私单/已完成）和搜索
- **项目详情**: 包含基本信息、任务列表、时间线、复盘
- **项目进度**: 自动计算 = Σ(任务预估工时 × 任务进度%) / Σ(任务预估工时) × 100%

#### 1.1.2 任务管理
- **任务分类**: 开发、会议、文档、设计、调试、BUG（支持自定义分类增删）
- **任务创建**: 标题、描述、所属项目、分类、优先级（高/中/低）、预估工时、计划开始时间、计划结束时间
- **任务状态**:
  - 待办 → 进行中（记录实际开始时间）
  - 进行中 → 已完成（记录实际完成时间）
  - 进行中 → 延期（记录延期原因、延期后计划时间、保留原时间记录）
  - 已完成/延期 → 待办（用于处理计划外需求/变更）
- **任务进度**: 百分比进度（0-100%）
- **任务时间轴**: 每个任务有独立的时间轴，记录所有状态变更、延期、变更历史
- **计划外需求**: 任务完成后可"追加需求"，任务状态回归待办，追加的需求记录到任务日志
- **任务列表**: 按项目分组，支持拖拽排序（Alpine.js）
- **任务详情**: 弹窗展示完整信息+任务专属时间轴

#### 1.1.3 时间线
- **自动记录**: 项目创建、任务完成、任务延期、项目复盘、任务变更
- **里程碑类型**: 项目创建、任务完成、任务延期、任务变更、项目复盘
- **时间线展示**: 垂直时间轴，按时间倒序显示
- **历程积累**: 可视化展示项目从创建到完成的完整历程
- **时间筛选**: 按时间范围筛选

#### 1.1.4 复盘功能
- **随时复盘**: 项目进行中或完成后都可添加复盘
- **项目复盘**: 做得好的 / 需改进的 / 下一步行动
- **任务复盘**: 任务完成时可添加简短心得
- **复盘入时间轴**: 复盘自动添加到项目时间线中
- **经验沉淀**: 汇总展示所有复盘内容，支持按关键词搜索

#### 1.1.5 甘特图
- **默认视图**: 显示30天（过去7天 + 未来23天）
- **时间单位**: 默认天，可切换为小时
- **任务条显示**: 显示任务计划时间条，拖拽可调整
- **操作功能**: 点击任务条可编辑、查看详情
- **今日标记**: 明显标记今天的位置
- **周视图/日视图**: 切换不同粒度

#### 1.1.6 AI功能
- **DeepSeek接入**: 配置API Key即可使用
- **AI助手入口**: 侧边栏常驻，点击展开对话
- **基础问答**: 对项目、任务、复盘内容提问
- **智能建议**: 基于当前项目/任务状态给出建议

#### 1.1.7 消息提醒
- **每日提醒**: 每天8:30自动提醒
- **提醒内容**:
  - 今日待办任务列表
  - 多日任务进度（告诉今天应该完成多少/达到什么程度）
  - AI智能总结和建议
- **提醒方式**: 浏览器通知（需授权）
- **提醒配置**: 可设置提醒时间、开启/关闭

### 1.2 个人设置

#### 1.2.1 DeepSeek设置
- **API Key**: 输入并保存DeepSeek API密钥
- **连接测试**: 验证Key是否有效
- **模型选择**: deepseek-chat / deepseek-coder

#### 1.2.2 主题设置
- **明亮主题**: 浅色背景，深色文字（默认）
- **深色主题**: 深色背景，浅色文字
- **跟随系统**: 自动跟随系统主题
- **一键切换**: 顶部快捷切换

#### 1.2.3 分类管理
- **任务分类**: 增删改查任务分类
- **默认分类**: 开发、会议、文档、设计、调试、BUG
- **分类图标**: 每个分类可设置图标和颜色

#### 1.2.4 工作时间设置
- **上班时间**: 如 09:00
- **午休开始**: 如 12:00
- **午休结束**: 如 13:30
- **下班时间**: 如 18:00
- **通勤时间**: 每日通勤耗时（小时）
- **目前职业**: 自由职业者 / 程序员 / 设计师 等
- **目前计划**: 简短描述当前的人生阶段/目标
- **目前公司**: 当前服务的公司（如果有）

---

## 二、技术架构

### 2.1 技术栈

| 层级 | 技术选型 | 说明 |
|------|----------|------|
| 后端框架 | Laravel 10+ | PHP主流框架，稳定可靠 |
| 前端框架 | Vue 3 + Vite | 现代化前端框架 |
| UI组件库 | TDesign Vue Next | 腾讯企业级UI组件库 |
| 数据库 | MySQL 8.0 | 本地数据库，root/chennan |
| HTTP客户端 | Axios | API调用 |
| 路由 | Vue Router | SPA路由 |
| 状态管理 | Pinia | Vue状态管理 |
| AI集成 | DeepSeek API | 对接DeepSeek大语言模型 |
| 甘特图 | 自绘/SVG | 基于Vue+TDesign自绘甘特图 |

### 2.2 数据库设计

```
projects
├── id (bigint, PK)
├── name (varchar 255)
├── description (text)
├── customer (varchar 255, nullable)        -- 客户名称
├── type (enum: work, freelance)
├── status (enum: active, completed, paused, archived)
├── progress (decimal 5,2, computed)        -- 计算字段：项目进度
├── started_at (timestamp)
├── ended_at (timestamp, nullable)
├── created_at (timestamp)
└── updated_at (timestamp)

tasks
├── id (bigint, PK)
├── project_id (bigint, FK)
├── title (varchar 255)
├── description (text, nullable)
├── category_id (bigint, FK, nullable)
├── priority (enum: high, medium, low)
├── status (enum: todo, in_progress, completed, delayed)
├── progress (int 0-100)                    -- 进度百分比
├── estimated_hours (decimal 8,2)           -- 预估工时（小时）
├── planned_start (datetime)                -- 计划开始时间
├── planned_end (datetime)                  -- 计划结束时间
├── actual_start (datetime, nullable)       -- 实际开始时间
├── actual_end (datetime, nullable)         -- 实际完成时间
├── sort_order (int)
├── created_at (timestamp)
└── updated_at (timestamp)

task_logs                                    -- 任务变更日志
├── id (bigint, PK)
├── task_id (bigint, FK)
├── type (enum: created, status_changed, delayed, requirement_added, progress_updated, edited)
├── description (text)
├── old_value (json, nullable)              -- 变更前的值
├── new_value (json, nullable)              -- 变更后的值
├── created_at (timestamp)
└── updated_at (timestamp)

task_delays                                  -- 延期记录
├── id (bigint, PK)
├── task_id (bigint, FK)
├── reason (text)                           -- 延期原因
├── original_planned_end (datetime)         -- 原计划完成时间
├── new_planned_end (datetime)               -- 新计划完成时间
├── created_at (timestamp)
└── updated_at (timestamp)

categories                                   -- 任务分类
├── id (bigint, PK)
├── name (varchar 100)                       -- 分类名称
├── icon (varchar 50)                        -- 图标类名
├── color (varchar 20)                      -- 颜色值
├── is_default (boolean)                     -- 是否系统默认
├── sort_order (int)
├── created_at (timestamp)
└── updated_at (timestamp)

milestones
├── id (bigint, PK)
├── project_id (bigint, FK)
├── task_id (bigint, FK, nullable)           -- 关联任务（如果有）
├── title (varchar 255)
├── type (enum: project_created, task_completed, task_delayed, task_changed, reflection_added, project_completed)
├── description (text, nullable)
├── occurred_at (timestamp)
├── created_at (timestamp)
└── updated_at (timestamp)

reflections
├── id (bigint, PK)
├── project_id (bigint, FK, nullable)
├── task_id (bigint, FK, nullable)
├── type (enum: project, task)
├── good_points (text)                      -- 做得好的
├── improvements (text)                       -- 需改进的
├── next_actions (text)                      -- 下一步行动
├── created_at (timestamp)
└── updated_at (timestamp)

settings
├── id (bigint, PK)
├── key (varchar 100, unique)
├── value (text, nullable)
└── updated_at (timestamp)

notifications                                -- 提醒记录
├── id (bigint, PK)
├── type (enum: daily_summary)
├── content (text)
├── sent_at (timestamp)
├── created_at (timestamp)
└── updated_at (timestamp)
```

### 2.3 目录结构

```
app/
├── Http/
│   ├── Controllers/
│   │   ├── ProjectController.php
│   │   ├── TaskController.php
│   │   ├── TimelineController.php
│   │   ├── ReflectionController.php
│   │   ├── GanttController.php
│   │   ├── CategoryController.php
│   │   ├── SettingController.php
│   │   └── AiController.php
│   └── Livewire/
│       ├── Project/
│       │   ├── ProjectList.php
│       │   ├── ProjectCard.php
│       │   └── ProjectDetail.php
│       ├── Task/
│       │   ├── TaskList.php
│       │   ├── TaskItem.php
│       │   ├── TaskModal.php
│       │   ├── TaskDetail.php
│       │   └── TaskDelayModal.php
│       ├── Timeline/
│       │   └── TimelineView.php
│       ├── Gantt/
│       │   └── GanttChart.php
│       ├── Reflection/
│       │   ├── ReflectionForm.php
│       │   └── ReflectionList.php
│       ├── Category/
│       │   └── CategoryManager.php
│       ├── Setting/
│       │   └── SettingsPage.php
│       └── Ai/
│           └── AiChat.php
├── Models/
│   ├── Project.php
│   ├── Task.php
│   ├── TaskLog.php
│   ├── TaskDelay.php
│   ├── Category.php
│   ├── Milestone.php
│   ├── Reflection.php
│   ├── Setting.php
│   └── Notification.php
└── Services/
    ├── DeepSeekService.php
    └── NotificationService.php

resources/
└── views/
    ├── layouts/
    │   └── app.blade.php
    ├── livewire/
    │   └── [与上述对应]
    └── components/
        ├── sidebar.blade.php
        ├── header.blade.php
        ├── project-card.blade.php
        ├── task-item.blade.php
        ├── timeline-item.blade.php
        ├── gantt-chart.blade.php
        └── theme-toggle.blade.php

jobs/
└── SendDailyReminder.php                   -- 每日提醒任务
```

---

## 三、UI/UX设计

### 3.1 整体风格

- **设计语言**: Notion风格，简洁、专业、高效
- **配色方案**:

**明亮主题**:
- 背景色: `#f8f9fa`
- 卡片色: `#ffffff`
- 侧边栏: `#f1f3f5`
- 主文字: `#2d3436`
- 次要文字: `#636e72`
- 强调色: `#0984e3`
- 成功色: `#00b894`
- 警告色: `#fdcb6e`
- 危险色: `#d63031`

**深色主题**:
- 背景色: `#1a1a2e`
- 卡片色: `#16213e`
- 侧边栏: `#0f0f23`
- 主文字: `#dfe6e9`
- 次要文字: `#b2bec3`
- 强调色: `#74b9ff`
- 成功色: `#55efc4`
- 警告色: `#ffeaa7`
- 危险色: `#ff7675`

- **圆角**: 8px-12px，柔和但不过度
- **阴影**: 轻阴影，卡片悬停时加深
- **动效**: 微妙的过渡动画（200-300ms），提升体验但不分散注意力

### 3.2 页面布局

```
┌─────────────────────────────────────────────────────────┐
│  Header: Logo + 搜索 + 主题切换 + AI助手 + 设置        │
├────────┬────────────────────────────────────────────────┤
│        │                                                │
│  Side  │           Main Content Area                   │
│  bar   │    (项目列表 / 项目详情 / 甘特图 / etc)      │
│        │                                                │
│ - 项目  │                                                │
│ - 甘特图│                                                │
│ - 时间线│                                                │
│ - 复盘  │                                                │
│ - 分类  │                                                │
│ - AI   │                                                │
│ - 设置  │                                                │
│        │                                                │
└────────┴────────────────────────────────────────────────┘
```

### 3.3 响应式策略

- **桌面端 (>1024px)**: 完整侧边栏 + 主内容区
- **平板端 (768-1024px)**: 可折叠侧边栏
- **移动端 (<768px)**: 底部导航 + 全屏内容

---

## 四、AI功能设计（DeepSeek集成）

### 4.1 配置方式

- **设置页面**: 输入DeepSeek API Key
- **密钥安全**: 加密存储在数据库settings表中
- **可用模型**: deepseek-chat, deepseek-coder

### 4.2 AI助手交互

```
┌─────────────────────┐
│  AI助手             │
├─────────────────────┤
│ [对话历史...]       │
│                     │
├─────────────────────┤
│ [输入框]       [发送]│
└─────────────────────┘
```

### 4.3 AI能力范围

1. **问答模式**: 用户提问，AI回答
2. **项目建议**: "我应该如何推进这个项目？"
3. **任务拆分**: "帮我把这个大任务拆成小任务"
4. **复盘总结**: "帮我总结这个项目的经验"
5. **每日提醒总结**: AI生成今日工作建议

---

## 五、消息提醒设计

### 5.1 每日提醒机制

- **触发时间**: 每天 8:30（可配置）
- **触发方式**: Laravel Scheduled Task / 前端定时器
- **浏览器通知**: 使用 Notification API，需用户授权

### 5.2 提醒内容生成

```
📋 今日工作提醒

⏰ 今日待办（3项）
1. [开发] 用户登录功能 - 高优先级
2. [BUG] 修复支付回调异常 - 高优先级
3. [文档] 编写接口文档 - 中优先级

📊 正在进行中（2项）
• 项目管理系统开发 (35%) - 预计还需 16h
  → 今日目标：完成用户模块，争取达到50%

🎯 AI建议
"根据你的任务安排，今天建议集中处理高优先级任务。
'用户登录功能'是当前项目的关键路径任务，
建议上午优先完成。'修复支付回调异常'是线上问题，
优先级很高，建议下午尽快处理。"

祝你今天高效满满！💪
```

### 5.3 提醒触发逻辑

1. 获取用户设置的工作时间
2. 查询今日计划任务（planned_start = 今天）
3. 查询进行中任务，计算今日目标
4. 组装提醒内容
5. 调用DeepSeek生成AI建议
6. 发送浏览器通知
7. 记录到 notifications 表

---

## 六、质量要求

### 6.1 功能要求
- [ ] 所有FluxUI组件仅使用支持的属性
- [ ] 无后台进程命令（不使用`&`）
- [ ] 移动端响应式设计
- [ ] 表单验证完整
- [ ] 图片使用 Unsplash 或 https://picsum.photos/
- [ ] 包含Playwright截图测试

### 6.2 性能要求
- 页面加载 < 2秒
- 交互响应 < 500ms
- 数据库查询优化（索引）

### 6.3 安全要求
- 所有输入转义防XSS
- CSRF保护
- API Key加密存储

---

## 七、开发时间线

| 阶段 | 内容 | 预计时间 |
|------|------|----------|
| Phase 1 | 项目基础功能（+客户字段、进度计算） | 1天 |
| Phase 2 | 任务管理（+时间追踪、延期、分类） | 2天 |
| Phase 3 | 任务时间轴 + 计划外需求 | 1天 |
| Phase 4 | 甘特图页面 | 1.5天 |
| Phase 5 | 时间线（+复盘入时间轴） | 0.5天 |
| Phase 6 | 主题切换（亮/暗） | 0.5天 |
| Phase 7 | DeepSeek集成 + AI助手 | 1天 |
| Phase 8 | 每日提醒 + AI总结 | 1天 |
| Phase 9 | 个人设置 + 分类管理 | 0.5天 |
| Phase 10 | UI优化 + 响应式 | 1天 |
| Phase 11 | 测试 + Bug修复 | 1-2天 |

**MVP总预计**: 11-13天

---

## 八、后续扩展预留

在数据库和架构设计时预留扩展空间：

1. **资料管理模块**: 添加 `materials` 表
2. **财务管理模块**: 添加 `finances`, `accounts` 表
3. **人生规划模块**: 添加 `goals`, `milestones_life` 表
4. **多语言支持**: Laravel localization

---

## 九、验收标准

### 9.1 功能验收
- [ ] 可以创建、编辑、删除项目（包含客户字段）
- [ ] 项目进度自动计算
- [ ] 可以创建、编辑、删除任务（包含预估工时、计划时间）
- [ ] 任务状态切换时自动记录实际时间
- [ ] 任务可以延期，记录延期原因
- [ ] 任务有独立的时间轴
- [ ] 任务可以追加计划外需求
- [ ] 任务分类可管理（增删改查）
- [ ] 甘特图显示30天视图
- [ ] 甘特图可调整任务时间
- [ ] 甘特图时间单位可切换（天/小时）
- [ ] 复盘可随时添加
- [ ] 复盘显示在项目时间轴
- [ ] 主题可切换（亮/暗）
- [ ] AI助手可以对话
- [ ] 每日提醒正常工作
- [ ] 个人设置完整保存
- [ ] DeepSeek API Key可以配置

### 9.2 视觉验收
- [ ] Notion风格简洁界面
- [ ] 侧边栏导航正常
- [ ] 移动端布局正常
- [ ] 动画过渡流畅
- [ ] 深色主题正常显示

---

## 十、任务状态流转图

```
                    ┌─────────────┐
                    │    待办      │
                    └──────┬──────┘
                           │ 点击"开始"
                           │ 记录 actual_start
                           ▼
                    ┌─────────────┐
            ┌───────│   进行中    │───────┐
            │       └──────┬──────┘       │
            │              │              │
    追加需求 │       点击"完成"      点击"延期"│
            │              │              │
            │              ▼              ▼
            │       ┌─────────────┐ ┌─────────────┐
            │       │   已完成   │ │   延期     │
            │       └─────────────┘ └──────┬────┘
            │                              │ 新的计划时间
            │                              │
            │         ┌───────────────────┘
            │         │
            │         ▼
            │   回到"待办"（重新开始）
            │
            │  追加需求记录到 task_logs
            │
            ▼
    任务追加需求
```

---

**文档版本**: v2.0
**更新日期**: 2026-04-13
**负责人**: ProjectHub PM
