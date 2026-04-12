# ProjectHub 开发任务清单 v2.0

## 规格摘要

**原始需求**: 个人管理系统MVP，包含项目任务管理、时间线、甘特图、复盘、AI助手、每日提醒、主题切换
**技术栈**: Laravel 10 + Livewire 3 + FluxUI + Tailwind CSS + Alpine.js
**目标时间线**: 11-13天完成MVP

---

## Phase 1: 项目基础功能

---

#### [ ] 任务1：Laravel项目初始化
**描述**: 创建Laravel项目，配置Livewire 3、FluxUI、Tailwind CSS
**验收标准**:
- `php artisan serve` 正常运行
- Tailwind CSS构建正常
- FluxUI组件可正常使用

**要创建/编辑的文件**:
- `composer.json` (添加依赖)
- `tailwind.config.js`
- `resources/css/app.css`
- `vite.config.js`

---

#### [ ] 任务2：数据库迁移（基础表）
**描述**: 创建projects、tasks、milestones、reflections、settings基础表
**验收标准**:
- `php artisan migrate` 执行成功
- 数据表创建正确
- 外键关系正确

**要创建的文件**:
- `database/migrations/xxxx_create_projects_table.php` (+customer字段)
- `database/migrations/xxxx_create_tasks_table.php` (+预估工时、计划时间、实际时间、进度)
- `database/migrations/xxxx_create_milestones_table.php`
- `database/migrations/xxxx_create_reflections_table.php`
- `database/migrations/xxxx_create_settings_table.php`

---

#### [ ] 任务3：数据库迁移（新表）
**描述**: 创建categories、task_logs、task_delays、notifications表
**验收标准**:
- `php artisan migrate` 执行成功
- 新表创建正确

**要创建的文件**:
- `database/migrations/xxxx_create_categories_table.php`
- `database/migrations/xxxx_create_task_logs_table.php`
- `database/migrations/xxxx_create_task_delays_table.php`
- `database/migrations/xxxx_create_notifications_table.php`

---

#### [ ] 任务4：Eloquent模型创建
**描述**: 创建所有模型及关系
**验收标准**:
- 模型关系正确
- fillable属性正确
- 类型转换正确
- 进度计算方法存在

**要创建的文件**:
- `app/Models/Project.php` (+progress属性, +customer字段)
- `app/Models/Task.php` (+时间字段, +进度)
- `app/Models/Category.php`
- `app/Models/TaskLog.php`
- `app/Models/TaskDelay.php`
- `app/Models/Milestone.php`
- `app/Models/Reflection.php`
- `app/Models/Setting.php`
- `app/Models/Notification.php`

---

#### [ ] 任务5：分类种子数据
**描述**: 创建默认任务分类（开发、会议、文档、设计、调试、BUG）
**验收标准**:
- Seeder执行后有6个默认分类
- 分类有图标和颜色

**要创建的文件**:
- `database/seeders/CategorySeeder.php`

---

#### [ ] 任务6：基础布局组件（+主题支持）
**描述**: 创建主布局（Header、Sidebar、Main Content），支持亮/暗主题
**验收标准**:
- 侧边栏导航正常（项目/甘特图/时间线/复盘/分类/AI/设置）
- 主题切换按钮存在
- 主题状态保存和恢复
- 响应式布局正常

**要创建的文件**:
- `resources/views/layouts/app.blade.php` (主题CSS变量)
- `resources/views/components/sidebar.blade.php`
- `resources/views/components/header.blade.php`
- `resources/views/components/theme-toggle.blade.php`

**组件**: flux:sidebar, flux:button, Alpine.js theme store

---

#### [ ] 任务7：项目列表页面
**描述**: 创建项目卡片列表，支持筛选和搜索，显示进度
**验收标准**:
- 项目以卡片形式展示，显示进度条
- 支持筛选：全部/工作/私单/已完成
- 支持搜索项目名称
- 点击卡片进入项目详情

**要创建的文件**:
- `app/Http/Livewire/Project/ProjectList.php`
- `resources/views/livewire/project/project-list.blade.php`
- `resources/views/components/project-card.blade.php`

---

#### [ ] 任务8：创建/编辑项目
**描述**: 创建项目表单弹窗，支持新建和编辑，包含客户字段
**验收标准**:
- 弹窗表单包含：名称、描述、类型、客户、日期
- 客户字段可为空
- 日期选择器正常工作

**要创建的文件**:
- `app/Http/Livewire/Project/ProjectModal.php`
- `resources/views/livewire/project/project-modal.blade.php`

---

#### [ ] 任务9：项目详情页面
**描述**: 项目详情页，包含基本信息、任务列表、时间线入口、复盘入口
**验收标准**:
- 显示项目名称、描述、客户、状态、进度
- 关联的任务列表
- 时间线入口按钮
- 复盘入口按钮
- 项目编辑按钮

**要创建的文件**:
- `app/Http/Livewire/Project/ProjectDetail.php`
- `resources/views/livewire/project/project-detail.blade.php`

---

## Phase 2: 任务管理功能

---

#### [ ] 任务10：分类管理页面
**描述**: 分类的增删改查管理
**验收标准**:
- 显示所有分类列表
- 可添加新分类（名称、图标、颜色）
- 可编辑分类
- 可删除非系统分类
- 默认分类不可删除

**要创建的文件**:
- `app/Http/Livewire/Category/CategoryManager.php`
- `resources/views/livewire/category/category-manager.blade.php`

---

#### [ ] 任务11：任务列表组件
**描述**: 按项目显示任务列表，支持拖拽排序，显示所有字段
**验收标准**:
- 任务以列表形式展示
- 显示：标题、分类（图标+颜色）、优先级、状态、进度%、预估工时、计划时间
- 支持拖拽排序
- 优先级颜色标识（高/中/低）

**要创建的文件**:
- `app/Http/Livewire/Task/TaskList.php`
- `resources/views/livewire/task/task-list.blade.php`
- `resources/views/components/task-item.blade.php`

---

#### [ ] 任务12：创建/编辑任务
**描述**: 任务创建和编辑表单，包含所有字段
**验收标准**:
- 弹窗表单包含：标题、描述、分类、优先级、预估工时、计划开始/结束时间
- 分类下拉选择
- 日期时间选择器正常工作
- 保存后任务列表刷新

**要创建的文件**:
- `app/Http/Livewire/Task/TaskModal.php`
- `resources/views/livewire/task/task-modal.blade.php`

---

#### [ ] 任务13：任务状态切换（+时间记录）
**描述**: 点击切换任务状态，自动记录时间
**验收标准**:
- 待办 → 进行中：记录 actual_start
- 进行中 → 已完成：记录 actual_end
- 状态变更记录到 task_logs
- 已完成任务显示特殊样式

**要编辑的文件**:
- `app/Http/Livewire/Task/TaskList.php`

---

#### [ ] 任务14：任务延期功能
**描述**: 任务延期，记录原因和新计划时间
**验收标准**:
- 延期弹窗：输入延期原因、新的计划结束时间
- 延期记录保存到 task_delays 表
- 原计划时间保留到 task_delays.original_planned_end
- 状态变更为 delayed
- 延期记录显示在任务时间轴

**要创建的文件**:
- `app/Http/Livewire/Task/TaskDelayModal.php`
- `resources/views/livewire/task/task-delay-modal.blade.php`

---

#### [ ] 任务15：任务详情弹窗（+时间轴）
**描述**: 任务详情弹窗，显示完整信息和独立时间轴
**验收标准**:
- 显示所有任务字段
- 显示任务专属时间轴（所有变更记录）
- 延期记录单独展示
- 可编辑、可删除

**要创建的文件**:
- `app/Http/Livewire/Task/TaskDetail.php`
- `resources/views/livewire/task/task-detail.blade.php`

---

#### [ ] 任务16：任务进度更新
**描述**: 任务进度百分比更新
**验收标准**:
- 在任务列表可直接修改进度
- 或在详情弹窗中修改
- 进度变更记录到 task_logs
- 项目总进度自动重新计算

---

#### [ ] 任务17：计划外需求追加
**描述**: 任务完成后可追加新需求，任务状态回归待办
**验收标准**:
- 已完成任务有"追加需求"按钮
- 点击后输入追加内容
- 追加内容记录到 task_logs (type=requirement_added)
- 任务状态回归待办
- 追加记录显示在任务时间轴

**要创建的文件**:
- `app/Http/Livewire/Task/TaskAddRequirementModal.php`
- `resources/views/livewire/task/task-add-requirement-modal.blade.php`

---

## Phase 3: 甘特图

---

#### [ ] 任务18：甘特图页面
**描述**: 创建甘特图视图，默认显示30天
**验收标准**:
- 甘特图画布，默认显示30天（过去7天+未来23天）
- 左侧任务列表（标题、分类）
- 右侧时间轴（任务条）
- 任务条显示计划时间范围
- 今日标记线
- 可切换天/小时视图

**要创建的文件**:
- `app/Http/Livewire/Gantt/GanttChart.php`
- `resources/views/livewire/gantt/gantt-chart.blade.php`

**技术**: 纯CSS/SVG绘制甘特图，或使用 simple-gantt 库

---

#### [ ] 任务19：甘特图交互
**描述**: 甘特图的任务操作
**验收标准**:
- 点击任务条显示详情
- 拖拽任务条边缘可调整计划时间
- 保存调整后的时间到数据库
- 时间单位切换（天/小时）

---

## Phase 4: 时间线

---

#### [ ] 任务20：里程碑自动记录
**描述**: 各种事件自动生成里程碑
**验收标准**:
- 创建项目时生成 milestone
- 任务完成时生成 milestone
- 任务延期时生成 milestone
- 项目复盘时生成 milestone
- 项目完成时生成 milestone

---

#### [ ] 任务21：时间线展示页面
**描述**: 垂直时间轴展示所有里程碑
**验收标准**:
- 垂直时间轴设计
- 按时间倒序排列
- 显示里程碑图标、标题、时间、内容
- 按项目筛选
- 复盘内容显示在时间线中

**要创建的文件**:
- `app/Http/Livewire/Timeline/TimelineView.php`
- `resources/views/livewire/timeline/timeline-view.blade.php`

---

## Phase 5: 复盘功能

---

#### [ ] 任务22：项目复盘表单
**描述**: 项目复盘表单，随时可添加
**验收标准**:
- 三个输入框：做得好的、需改进的、下一步行动
- 引导式模板提示
- 提交后保存到 reflections 表
- 自动生成 milestone

**要创建的文件**:
- `app/Http/Livewire/Reflection/ReflectionForm.php`
- `resources/views/livewire/reflection/reflection-form.blade.php`

---

#### [ ] 任务23：任务复盘功能
**描述**: 任务完成时可添加简短复盘
**验收标准**:
- 任务完成时可添加复盘
- 简化版复盘表单
- 关联任务ID保存

---

#### [ ] 任务24：复盘列表页面
**描述**: 展示所有复盘历史
**验收标准**:
- 卡片式展示复盘列表
- 显示关联项目/任务名称
- 显示复盘时间和摘要
- 支持搜索复盘内容

**要创建的文件**:
- `app/Http/Livewire/Reflection/ReflectionList.php`
- `resources/views/livewire/reflection/reflection-list.blade.php`

---

## Phase 6: 主题切换

---

#### [ ] 任务25：主题切换功能
**描述**: 实现亮/暗主题切换
**验收标准**:
- 顶部有主题切换按钮
- 点击切换明亮/深色主题
- 主题状态保存到 localStorage
- 页面刷新后保持主题
- 所有页面组件都支持主题

**要编辑的文件**:
- `resources/views/layouts/app.blade.php`
- `resources/views/components/theme-toggle.blade.php`
- Tailwind CSS 暗黑模式配置

---

## Phase 7: AI功能（DeepSeek集成）

---

#### [ ] 任务26：DeepSeek服务类
**描述**: 创建DeepSeek API调用服务
**验收标准**:
- DeepSeekService类可实例化
- 支持 chat completions API
- 异常处理完善
- 响应格式统一

**要创建的文件**:
- `app/Services/DeepSeekService.php`

---

#### [ ] 任务27：设置页面（基础）
**描述**: 添加DeepSeek API Key配置
**验收标准**:
- 设置页面可访问
- API Key输入框（密码类型）
- 保存后加密存储
- 验证Key有效性
- 模型选择

**要创建的文件**:
- `app/Http/Livewire/Setting/SettingsPage.php`
- `resources/views/livewire/setting/settings-page.blade.php`

---

#### [ ] 任务28：AI助手对话界面
**描述**: 侧边栏AI助手组件
**验收标准**:
- 点击侧边栏AI图标展开
- 对话窗口显示历史消息
- 输入框可发送消息
- 显示AI回复

**要创建的文件**:
- `app/Http/Livewire/Ai/AiChat.php`
- `resources/views/livewire/ai/ai-chat.blade.php`

---

#### [ ] 任务29：AI能力集成
**描述**: 将AI对话与项目/任务关联
**验收标准**:
- 当前上下文信息传递给AI
- 支持询问项目建议
- 支持询问任务拆分
- AI回复格式化显示

---

## Phase 8: 每日提醒

---

#### [ ] 任务30：每日提醒服务
**描述**: 创建每日提醒生成服务
**验收标准**:
- 获取今日待办任务
- 获取进行中任务及进度
- 计算今日目标
- 调用DeepSeek生成AI建议
- 组装完整提醒内容

**要创建的文件**:
- `app/Services/NotificationService.php`
- `app/Jobs/SendDailyReminder.php`

---

#### [ ] 任务31：浏览器通知
**描述**: 实现浏览器通知
**验收标准**:
- 用户授权后发送浏览器通知
- 通知内容包含今日任务和AI建议
- 通知点击可打开项目页面

**要编辑的文件**:
- `resources/views/layouts/app.blade.php` (Service Worker)
- 前端通知组件

---

#### [ ] 任务32：提醒配置
**描述**: 提醒时间配置和开关
**验收标准**:
- 设置中可配置提醒时间
- 可开启/关闭每日提醒
- 保存配置

---

## Phase 9: 个人设置

---

#### [ ] 任务33：个人设置页面
**描述**: 完整个人设置
**验收标准**:
- 上班时间、午休开始、午休结束、下班时间
- 通勤时间
- 目前职业
- 目前计划
- 目前公司
- 所有设置保存到 settings 表

---

## Phase 10: UI优化与测试

---

#### [ ] 任务34：响应式布局优化
**描述**: 确保移动端布局正常
**验收标准**:
- 平板端侧边栏可折叠
- 移动端底部导航
- 甘特图在移动端可滚动
- 弹窗全屏显示

---

#### [ ] 任务35：动画过渡优化
**描述**: 添加页面过渡动画
**验收标准**:
- 页面切换过渡效果
- 弹窗开启动画
- 按钮悬停效果
- 状态切换动画

---

#### [ ] 任务36：Playwright截图测试
**描述**: 添加自动化截图测试
**验收标准**:
- 项目列表页面截图
- 项目详情页面截图
- 任务列表页面截图
- 时间线页面截图
- 甘特图页面截图
- AI助手页面截图
- 深色主题截图

**命令**: `./qa-playwright-capture.sh http://localhost:8000 public/qa-screenshots`

---

#### [ ] 任务37：功能测试与Bug修复
**描述**: 全面测试所有功能，修复发现的问题
**验收标准**:
- 所有FluxUI组件正常工作
- 所有表单验证有效
- 数据库操作正常
- 无console错误
- 主题切换正常

---

## 项目进度计算公式

```
项目进度 = Σ(任务预估工时 × 任务进度%) / Σ(任务预估工时) × 100%
```

**示例**:
- 任务A: 预估8h, 完成50% → 4h
- 任务B: 预估4h, 完成100% → 4h
- 任务C: 预估8h, 未开始 → 0h
- 总进度 = (4 + 4 + 0) / (8 + 4 + 8) × 100% = 40%

---

## 任务状态流转

```
待办 ──[开始]──→ 进行中 ──[完成]──→ 已完成
  │                   │
  │              [延期]│
  │                   ↓
  │              延期 ──[重新开始]──→ 待办
  │
  └─────────────[追加需求]──→ 待办 (追加记录)
```

---

## 质量检查清单

- [ ] 所有FluxUI组件仅使用支持的属性
- [ ] 任何命令中无后台进程 - 永远不要附加`&`
- [ ] 无服务器启动命令 - 假设开发服务器正在运行
- [ ] 移动端响应式设计正常
- [ ] 表单功能验证完整
- [ ] 图片使用 Unsplash 或 picsum.photos
- [ ] 包含Playwright截图测试
- [ ] CSRF保护启用
- [ ] XSS防护正确转义
- [ ] 主题切换正常
- [ ] 时间记录准确

---

## 技术备注

**开发环境要求**:
- PHP 8.1+
- Node.js 18+
- MySQL 8.0
- Composer 2.x

**关键依赖**:
- Laravel 10+
- Livewire 3
- FluxUI (via Composer)
- Tailwind CSS 3
- Alpine.js

**特殊说明**:
- DeepSeek API Key需用户自行配置
- 数据库连接配置在 .env 文件
- 浏览器通知需要 HTTPS 或 localhost

**时间线期望**:
- Phase 1: 1天
- Phase 2: 2天
- Phase 3: 1天
- Phase 4: 1.5天
- Phase 5: 0.5天
- Phase 6: 0.5天
- Phase 7: 1天
- Phase 8: 1天
- Phase 9: 0.5天
- Phase 10: 2-3天
- **总计**: 11-13天（含Buffer）

---

**任务清单版本**: v2.0
**更新日期**: 2026-04-13
**基于规格**: ai/memory-bank/site-setup.md v2.0
