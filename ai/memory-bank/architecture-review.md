# ProjectHub 技术架构审查报告

**项目**: ProjectHub 个人工作管理系统
**审查日期**: 2026-04-13
**审查角色**: 高级架构师

---

## 一、技术选型评估

### 1.1 当前方案 vs 替代方案

| 维度 | 当前方案 (Laravel + Vue) | 替代方案 | 评估 |
|------|-------------------------|----------|------|
| **后端框架** | Laravel 10 | Spring Boot / NestJS / Express | Laravel 合适，生态完善 |
| **前端框架** | Vue 3 | React / Angular | Vue 合适，上手快 |
| **UI组件** | TDesign | Element Plus / Ant Design Vue | TDesign 合适，企业级 |
| **数据库** | MySQL | PostgreSQL | MySQL 合适，个人项目足够 |
| **部署复杂度** | 中 | 高（Spring Boot） | Laravel+Vue 平衡最好 |

### 1.2 选型结论

```
✅ Laravel + Vue 3 + TDesign + MySQL 是合适的组合
   - 学习成本低
   - 生态完善
   - 开发效率高
   - 适合快速迭代
```

---

## 二、架构设计

### 2.1 整体架构图

```
┌─────────────────────────────────────────────────────────────────────┐
│                            客户端层                                  │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐               │
│  │   Web App   │  │   浏览器     │  │   移动端    │               │
│  │  (Vue+TDesign)│  │  (通知)     │  │  (预留)     │               │
│  └──────┬──────┘  └──────┬──────┘  └──────┬──────┘               │
└─────────┼─────────────────┼─────────────────┼───────────────────────┘
          │                 │                 │
          ▼                 ▼                 ▼
┌─────────────────────────────────────────────────────────────────────┐
│                           API 网关层                                 │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │                     Laravel REST API                         │   │
│  │  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐      │   │
│  │  │ Projects │ │  Tasks   │ │ Timeline │ │ Settings │      │   │
│  │  │ Service  │ │ Service  │ │ Service  │ │ Service  │      │   │
│  │  └──────────┘ └──────────┘ └──────────┘ └──────────┘      │   │
│  │                                                             │   │
│  │  ┌──────────────────────────────────────────────────────┐   │   │
│  │  │                    DeepSeek Service                  │   │   │
│  │  └──────────────────────────────────────────────────────┘   │   │
│  └─────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────┘
          │
          ▼
┌─────────────────────────────────────────────────────────────────────┐
│                           数据层                                     │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐               │
│  │    MySQL    │  │    Redis    │  │   缓存层     │               │
│  │  (主数据)   │  │  (会话/缓存) │  │             │               │
│  └─────────────┘  └─────────────┘  └─────────────┘               │
└─────────────────────────────────────────────────────────────────────┘
```

### 2.2 前端架构（Vue 3 + TDesign）

```
frontend/
├── src/
│   ├── api/                    # API 请求层
│   │   ├── request.js          # Axios 实例 + 拦截器
│   │   ├── project.js         # 项目 API
│   │   ├── task.js            # 任务 API
│   │   └── ...
│   │
│   ├── components/            # 通用组件
│   │   ├── common/           # 通用业务组件
│   │   └── base/              # 基础 UI 组件
│   │
│   ├── composables/           # 组合式函数（Hooks）
│   │   ├── useProject.js      # 项目相关逻辑
│   │   ├── useTask.js        # 任务相关逻辑
│   │   ├── useTheme.js       # 主题切换
│   │   └── useNotification.js # 通知功能
│   │
│   ├── layouts/               # 布局组件
│   │
│   ├── pages/                 # 页面组件
│   │   ├── project/          # 项目模块
│   │   ├── task/             # 任务模块
│   │   ├── gantt/            # 甘特图模块
│   │   └── ...
│   │
│   ├── stores/                # Pinia 状态管理
│   │   ├── project.js
│   │   ├── task.js
│   │   └── settings.js
│   │
│   ├── router/                # 路由配置
│   ├── utils/                 # 工具函数
│   └── App.vue
```

### 2.3 后端架构（Laravel）

```
app/
├── Http/
│   ├── Controllers/
│   │   └── Api/
│   │       ├── ProjectController.php
│   │       ├── TaskController.php
│   │       └── ...
│   │
│   ├── Requests/             # 表单请求（验证）
│   │   ├── StoreProjectRequest.php
│   │   ├── UpdateProjectRequest.php
│   │   └── ...
│   │
│   ├── Resources/            # API 资源（格式化响应）
│   │   ├── ProjectResource.php
│   │   ├── TaskResource.php
│   │   └── ...
│   │
│   └── Middleware/           # 中间件
│
├── Models/                    # Eloquent 模型
│
├── Services/                  # 业务逻辑层
│   ├── ProjectService.php
│   ├── TaskService.php
│   ├── TimelineService.php
│   ├── DeepSeekService.php
│   └── NotificationService.php
│
├── Repositories/             # 数据访问层（可选）
│   ├── ProjectRepository.php
│   └── TaskRepository.php
│
└── Exceptions/                # 自定义异常
    └── Handler.php
```

---

## 三、API 设计

### 3.1 RESTful API 规范

#### 项目接口

| 方法 | 路径 | 说明 |
|------|------|------|
| GET | /api/projects | 列表（支持筛选、分页） |
| GET | /api/projects/{id} | 详情 |
| POST | /api/projects | 创建 |
| PUT | /api/projects/{id} | 更新 |
| DELETE | /api/projects/{id} | 删除 |
| GET | /api/projects/{id}/tasks | 获取项目下的任务 |
| GET | /api/projects/{id}/timeline | 获取项目时间线 |

#### 任务接口

| 方法 | 路径 | 说明 |
|------|------|------|
| GET | /api/tasks | 列表 |
| GET | /api/tasks/{id} | 详情 |
| POST | /api/tasks | 创建 |
| PUT | /api/tasks/{id} | 更新 |
| DELETE | /api/tasks/{id} | 删除 |
| POST | /api/tasks/{id}/start | 开始任务（记录实际开始时间） |
| POST | /api/tasks/{id}/complete | 完成 |
| POST | /api/tasks/{id}/delay | 延期 |
| POST | /api/tasks/{id}/add-requirement | 追加需求 |
| GET | /api/tasks/{id}/logs | 获取任务日志 |

### 3.2 API 响应格式

```json
// 成功响应
{
  "success": true,
  "data": { ... },
  "message": "操作成功"
}

// 分页响应
{
  "success": true,
  "data": {
    "items": [...],
    "pagination": {
      "total": 100,
      "page": 1,
      "page_size": 20
    }
  }
}

// 错误响应
{
  "success": false,
  "message": "错误信息",
  "errors": {
    "field": ["验证错误1", "验证错误2"]
  }
}
```

---

## 四、数据库设计（优化版）

### 4.1 表结构

#### projects 表
```sql
CREATE TABLE projects (
    id BIGINT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
    name VARCHAR(255) NOT NULL COMMENT '项目名称',
    description TEXT COMMENT '项目描述',
    customer VARCHAR(255) COMMENT '客户名称',
    type ENUM('work', 'freelance') DEFAULT 'work' COMMENT '类型：工作/私单',
    status ENUM('active', 'completed', 'paused', 'archived') DEFAULT 'active' COMMENT '状态',
    started_at TIMESTAMP NULL COMMENT '开始时间',
    ended_at TIMESTAMP NULL COMMENT '结束时间',
    created_at TIMESTAMP NULL,
    updated_at TIMESTAMP NULL,
    INDEX idx_status (status),
    INDEX idx_type (type),
    INDEX idx_started_at (started_at)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
```

#### tasks 表
```sql
CREATE TABLE tasks (
    id BIGINT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
    project_id BIGINT UNSIGNED NOT NULL COMMENT '所属项目',
    title VARCHAR(255) NOT NULL COMMENT '任务标题',
    description TEXT COMMENT '任务描述',
    category_id BIGINT UNSIGNED NULL COMMENT '分类',
    priority ENUM('high', 'medium', 'low') DEFAULT 'medium' COMMENT '优先级',
    status ENUM('todo', 'in_progress', 'completed', 'delayed') DEFAULT 'todo' COMMENT '状态',
    progress INT DEFAULT 0 COMMENT '进度百分比 0-100',
    estimated_hours DECIMAL(8,2) DEFAULT 0 COMMENT '预估工时（小时）',
    planned_start DATETIME NULL COMMENT '计划开始时间',
    planned_end DATETIME NULL COMMENT '计划结束时间',
    actual_start DATETIME NULL COMMENT '实际开始时间',
    actual_end DATETIME NULL COMMENT '实际完成时间',
    sort_order INT DEFAULT 0 COMMENT '排序',
    created_at TIMESTAMP NULL,
    updated_at TIMESTAMP NULL,
    FOREIGN KEY (project_id) REFERENCES projects(id) ON DELETE CASCADE,
    FOREIGN KEY (category_id) REFERENCES categories(id) ON DELETE SET NULL,
    INDEX idx_project_id (project_id),
    INDEX idx_status (status),
    INDEX idx_planned_start (planned_start),
    INDEX idx_planned_end (planned_end)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
```

#### task_logs 表（任务变更日志）
```sql
CREATE TABLE task_logs (
    id BIGINT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
    task_id BIGINT UNSIGNED NOT NULL,
    type ENUM('created', 'status_changed', 'delayed', 'requirement_added', 'progress_updated', 'edited') NOT NULL,
    description TEXT COMMENT '变更描述',
    old_value JSON COMMENT '变更前的值',
    new_value JSON COMMENT '变更后的值',
    created_at TIMESTAMP NULL,
    FOREIGN KEY (task_id) REFERENCES tasks(id) ON DELETE CASCADE,
    INDEX idx_task_id (task_id),
    INDEX idx_created_at (created_at)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
```

#### task_delays 表（延期记录）
```sql
CREATE TABLE task_delays (
    id BIGINT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
    task_id BIGINT UNSIGNED NOT NULL,
    reason TEXT NOT NULL COMMENT '延期原因',
    original_planned_end DATETIME NOT NULL COMMENT '原计划完成时间',
    new_planned_end DATETIME NOT NULL COMMENT '新计划完成时间',
    created_at TIMESTAMP NULL,
    FOREIGN KEY (task_id) REFERENCES tasks(id) ON DELETE CASCADE,
    INDEX idx_task_id (task_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
```

#### categories 表（任务分类）
```sql
CREATE TABLE categories (
    id BIGINT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
    name VARCHAR(100) NOT NULL COMMENT '分类名称',
    icon VARCHAR(50) DEFAULT 'folder' COMMENT '图标',
    color VARCHAR(20) DEFAULT '#0052d9' COMMENT '颜色',
    is_default TINYINT(1) DEFAULT 0 COMMENT '是否系统默认',
    sort_order INT DEFAULT 0 COMMENT '排序',
    created_at TIMESTAMP NULL,
    updated_at TIMESTAMP NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
```

#### milestones 表（里程碑）
```sql
CREATE TABLE milestones (
    id BIGINT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
    project_id BIGINT UNSIGNED NOT NULL,
    task_id BIGINT UNSIGNED NULL,
    title VARCHAR(255) NOT NULL COMMENT '标题',
    type ENUM('project_created', 'task_completed', 'task_delayed', 'task_changed', 'reflection_added', 'project_completed') NOT NULL,
    description TEXT COMMENT '描述',
    occurred_at TIMESTAMP NOT NULL COMMENT '发生时间',
    created_at TIMESTAMP NULL,
    FOREIGN KEY (project_id) REFERENCES projects(id) ON DELETE CASCADE,
    FOREIGN KEY (task_id) REFERENCES tasks(id) ON DELETE SET NULL,
    INDEX idx_project_id (project_id),
    INDEX idx_occurred_at (occurred_at)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
```

#### reflections 表（复盘）
```sql
CREATE TABLE reflections (
    id BIGINT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
    project_id BIGINT UNSIGNED NULL,
    task_id BIGINT UNSIGNED NULL,
    type ENUM('project', 'task') NOT NULL,
    good_points TEXT COMMENT '做得好的',
    improvements TEXT COMMENT '需改进的',
    next_actions TEXT COMMENT '下一步行动',
    created_at TIMESTAMP NULL,
    updated_at TIMESTAMP NULL,
    FOREIGN KEY (project_id) REFERENCES projects(id) ON DELETE SET NULL,
    FOREIGN KEY (task_id) REFERENCES tasks(id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
```

#### settings 表（设置）
```sql
CREATE TABLE settings (
    id BIGINT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
    `key` VARCHAR(100) NOT NULL UNIQUE,
    value TEXT NULL,
    updated_at TIMESTAMP NULL,
    INDEX idx_key (`key`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
```

### 4.2 项目进度计算

```php
// Project 模型中的计算属性
public function getProgressAttribute(): float
{
    $totalHours = $this->tasks->sum('estimated_hours');

    if ($totalHours == 0) {
        return 0.00;
    }

    $completedHours = $this->tasks->sum(function ($task) {
        return $task->estimated_hours * $task->progress / 100;
    });

    return round($completedHours / $totalHours * 100, 2);
}
```

---

## 五、甘特图实现方案

### 5.1 技术方案对比

| 方案 | 优点 | 缺点 | 推荐度 |
|------|------|------|--------|
| **自绘 SVG** | 完全可控，灵活 | 开发量大 | ⭐⭐⭐⭐ |
| **DHTMLX Gantt** | 功能强大 | 收费，学习曲线 | ⭐⭐⭐ |
| **vue-ganttastic** | Vue 友好 | 功能有限 | ⭐⭐⭐ |
| **自绘 Canvas** | 性能好 | 复杂 | ⭐⭐⭐ |

### 5.2 推荐方案：Vue 3 + SVG 自绘

```vue
<!-- GanttChart.vue 结构 -->
<template>
  <div class="gantt-container">
    <!-- 左侧：任务列表 -->
    <div class="gantt-sidebar">
      <div class="gantt-header">任务</div>
      <div class="task-list">
        <TaskRow v-for="task in tasks" :key="task.id" :task="task" />
      </div>
    </div>

    <!-- 右侧：时间轴 -->
    <div class="gantt-timeline" ref="timelineRef">
      <!-- 时间刻度 -->
      <div class="timeline-header">
        <TimeScale :start="startDate" :end="endDate" :unit="unit" />
      </div>

      <!-- 任务条 -->
      <div class="timeline-body">
        <GanttRow v-for="task in tasks" :key="task.id">
          <GanttBar
            :task="task"
            :start="startDate"
            :unit="unit"
            @drag-end="onDragEnd"
          />
        </GanttRow>

        <!-- 今日线 -->
        <TodayLine :start="startDate" :unit="unit" />
      </div>
    </div>
  </div>
</template>
```

### 5.3 甘特图 API 返回格式

```json
{
  "success": true,
  "data": {
    "tasks": [
      {
        "id": 1,
        "title": "用户登录功能",
        "planned_start": "2026-04-13",
        "planned_end": "2026-04-15",
        "progress": 50,
        "color": "#0052d9"
      }
    ],
    "date_range": {
      "start": "2026-04-06",
      "end": "2026-05-06"
    },
    "today": "2026-04-13"
  }
}
```

---

## 六、AI 集成方案（DeepSeek）

### 6.1 服务设计

```php
class DeepSeekService
{
    private string $apiKey;
    private string $model;
    private string $baseUrl = 'https://api.deepseek.com';

    public function __construct()
    {
        $this->apiKey = config('services.deepseek.api_key');
        $this->model = config('services.deepseek.model', 'deepseek-chat');
    }

    public function chat(string $message, array $context = []): string
    {
        $prompt = $this->buildPrompt($message, $context);

        $response = Http::timeout(30)->post(
            "{$this->baseUrl}/chat/completions",
            [
                'model' => $this->model,
                'messages' => [
                    ['role' => 'system', 'content' => $this->getSystemPrompt()],
                    ['role' => 'user', 'content' => $prompt],
                ],
                'temperature' => 0.7,
            ]
        );

        return $response['choices'][0]['message']['content'];
    }

    private function buildPrompt(string $message, array $context): string
    {
        $contextStr = '';
        if (!empty($context['project'])) {
            $contextStr .= "当前项目：{$context['project']->name}\n";
        }
        if (!empty($context['tasks'])) {
            $contextStr .= "相关任务：\n";
            foreach ($context['tasks'] as $task) {
                $contextStr .= "- {$task->title} ({$task->status})\n";
            }
        }
        return "【上下文】\n{$contextStr}\n\n【问题】\n{$message}";
    }

    private function getSystemPrompt(): string
    {
        return "你是一个专业的个人效率助手，擅长项目管理、任务规划的建议。
请用简洁专业的语言回答，如果涉及具体任务，给出可操作的建议。";
    }
}
```

### 6.2 每日提醒生成

```php
public function generateDailySummary(): array
{
    // 1. 获取今日任务
    $todayTasks = Task::whereDate('planned_start', today())
        ->orWhere(function ($q) {
            $q->where('status', 'in_progress')
              ->whereDate('planned_end', today());
        })
        ->get();

    // 2. 获取进行中任务
    $inProgressTasks = Task::where('status', 'in_progress')
        ->with('project')
        ->get();

    // 3. 构建提示
    $prompt = $this->buildDailySummaryPrompt($todayTasks, $inProgressTasks);

    // 4. 调用 AI
    $suggestion = $this->chat($prompt);

    return [
        'today_tasks' => $todayTasks,
        'in_progress' => $inProgressTasks,
        'ai_suggestion' => $suggestion,
    ];
}
```

### 6.3 安全考虑

- API Key 加密存储（使用 Laravel 的 env 加密）
- 请求超时限制（30秒）
- 错误重试机制
- 频率限制（防止滥用）

---

## 七、通知系统设计

### 7.1 浏览器通知流程

```
┌──────────────┐     ┌──────────────┐     ┌──────────────┐
│   前端定时器  │────▶│  检查提醒时间 │────▶│  获取提醒内容 │
│  (每分钟检查) │     │  (8:30 ±1min) │     │              │
└──────────────┘     └──────────────┘     └──────┬───────┘
                                                 │
                                                 ▼
┌──────────────┐     ┌──────────────┐     ┌──────────────┐
│   显示通知   │◀────│  API 获取数据 │◀────│  AI 生成建议 │
└──────────────┘     └──────────────┘     └──────────────┘
```

### 7.2 前端实现

```typescript
// composables/useNotification.ts
export function useNotification() {
  const checkPermission = async () => {
    if (!('Notification' in window)) {
      return false
    }

    if (Notification.permission === 'granted') {
      return true
    }

    if (Notification.permission !== 'denied') {
      const permission = await Notification.requestPermission()
      return permission === 'granted'
    }

    return false
  }

  const sendNotification = async (title: string, body: string) => {
    const hasPermission = await checkPermission()
    if (!hasPermission) return

    new Notification(title, {
      body,
      icon: '/logo.png',
    })
  }

  return { checkPermission, sendNotification }
}
```

---

## 八、安全设计

### 8.1 后端表单验证

```php
// app/Http/Requests/StoreTaskRequest.php
class StoreTaskRequest extends FormRequest
{
    public function rules(): array
    {
        return [
            'title' => 'required|string|max:255',
            'project_id' => 'required|exists:projects,id',
            'category_id' => 'nullable|exists:categories,id',
            'priority' => 'in:high,medium,low',
            'estimated_hours' => 'numeric|min:0|max:999',
            'planned_start' => 'date',
            'planned_end' => 'date|after_or_equal:planned_start',
            'progress' => 'integer|min:0|max:100',
        ];
    }
}
```

### 8.2 前端请求拦截器

```typescript
// api/request.ts
const request = axios.create()

request.interceptors.request.use((config) => {
  // 添加 CSRF Token
  const token = document.querySelector('meta[name="csrf-token"]')?.getAttribute('content')
  if (token) {
    config.headers['X-CSRF-TOKEN'] = token
  }
  return config
})

request.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      router.push('/login')
    }
    return Promise.reject(error)
  }
)
```

---

## 九、性能优化

### 9.1 数据库优化

```php
// 1. 查询优化 - 使用 with() 预加载
$tasks = Task::with(['project', 'category'])
    ->where('project_id', $projectId)
    ->orderBy('sort_order')
    ->get();

// 2. 索引 - 复合索引
Schema::table('tasks', function (Blueprint $table) {
    $table->index(['project_id', 'status', 'sort_order']);
});

// 3. 分页
$tasks = Task::paginate(20);

// 4. 缓存
Cache::remember("project:{$id}:tasks", 3600, function () {
    return Task::where('project_id', $id)->get();
});
```

### 9.2 前端优化

| 优化点 | 方案 |
|--------|------|
| 代码分割 | Vue Router 懒加载 |
| 组件懒加载 | defineAsyncComponent |
| 请求缓存 | Pinia 缓存 |
| 虚拟滚动 | 甘特图长列表 |

---

## 十、可扩展性设计

### 10.1 模块化架构

```
┌─────────────────────────────────────────────────────────────┐
│                      核心模块（MVP）                         │
│  ┌─────────┐ ┌─────────┐ ┌─────────┐ ┌─────────┐           │
│  │ Projects │ │  Tasks  │ │   AI    │ │Settings │           │
│  └─────────┘ └─────────┘ └─────────┘ └─────────┘           │
├─────────────────────────────────────────────────────────────┤
│                      扩展模块（未来）                        │
│  ┌─────────┐ ┌─────────┐ ┌─────────┐                      │
│  │Materials │ │ Finance │ │  Goals  │                      │
│  │ (资料)  │ │ (财务)  │ │ (规划)  │                      │
│  └─────────┘ └─────────┘ └─────────┘                      │
└─────────────────────────────────────────────────────────────┘
```

### 10.2 数据库预留

```sql
-- 资料管理表（预留）
CREATE TABLE materials (
    id BIGINT PRIMARY KEY,
    user_id BIGINT NOT NULL,
    type ENUM('image', 'video', 'document', 'comic', 'novel') NOT NULL,
    title VARCHAR(255) NOT NULL,
    path VARCHAR(500) NOT NULL,
    metadata JSON,
    created_at TIMESTAMP,
    updated_at TIMESTAMP
);

-- 财务管理表（预留）
CREATE TABLE finances (
    id BIGINT PRIMARY KEY,
    user_id BIGINT NOT NULL,
    type ENUM('income', 'expense') NOT NULL,
    category VARCHAR(100),
    amount DECIMAL(10,2) NOT NULL,
    description TEXT,
    happened_at DATE NOT NULL,
    created_at TIMESTAMP,
    updated_at TIMESTAMP
);

-- 人生规划表（预留）
CREATE TABLE goals (
    id BIGINT PRIMARY KEY,
    user_id BIGINT NOT NULL,
    title VARCHAR(255) NOT NULL,
    description TEXT,
    target_date DATE,
    status ENUM('planning', 'in_progress', 'completed', 'abandoned'),
    created_at TIMESTAMP,
    updated_at TIMESTAMP
);
```

---

## 十一、任务状态流转

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

## 十二、开发规范

### 12.1 代码风格

- **PHP**: PSR-12 标准
- **Vue**: Composition API + `<script setup>`
- **命名**: 
  - 控制器: `ProjectController`
  - 模型: `Project`
  - 组件: `PascalCase`
  - API: `camelCase`

### 12.2 Git 规范

```
feat: 新功能
fix: 修复bug
docs: 文档更新
style: 代码格式
refactor: 重构
test: 测试
chore: 构建/工具
```

### 12.3 提交信息格式

```
feat(project): 添加项目创建功能
fix(task): 修复任务状态切换bug
docs(api): 更新API文档
```

---

## 十三、测试策略

### 13.1 测试分层

```
┌─────────────────────────────────────────┐
│           E2E 测试（Playwright）          │
│   完整的用户流程，截图验证                │
├─────────────────────────────────────────┤
│           Feature 测试                   │
│   接口功能、边界条件、错误处理            │
├─────────────────────────────────────────┤
│           单元测试                       │
│   Service 层业务逻辑                     │
└─────────────────────────────────────────┘
```

### 13.2 关键测试用例

```php
// tests/Feature/TaskTest.php

public function test_task_progress_affects_project_progress()
{
    $project = Project::factory()->create();
    $task1 = Task::factory()->create([
        'project_id' => $project->id,
        'estimated_hours' => 10,
        'progress' => 50,
    ]);
    $task2 = Task::factory()->create([
        'project_id' => $project->id,
        'estimated_hours' => 10,
        'progress' => 100,
    ]);

    // 10*50% + 10*100% = 15 / 20 = 75%
    $this->assertEquals(75.00, $project->fresh()->progress);
}

public function test_task_start_records_actual_start_time()
{
    $task = Task::factory()->create([
        'status' => 'todo',
        'actual_start' => null,
    ]);

    $response = $this->postJson("/api/tasks/{$task->id}/start");

    $response->assertOk();
    $this->assertNotNull($task->fresh()->actual_start);
}
```

---

## 十四、风险评估

| 风险 | 影响 | 应对措施 |
|------|------|----------|
| 甘特图开发量大 | 中 | 使用成熟库如 DHTMLX Gantt |
| AI 响应不稳定 | 中 | 超时处理 + 降级方案 |
| 浏览器通知兼容 | 低 | 检测兼容性 + 降级到页面提示 |
| 性能瓶颈 | 中 | 提前做性能测试 |
| 数据迁移复杂度 | 中 | 设计时考虑扩展字段 |

---

## 十五、总结

### 15.1 架构优势

1. **分层清晰**: 前端（Vue）/ 后端（Laravel）/ 数据层（MySQL）
2. **可维护性高**: 模块化设计，职责分离
3. **可扩展性强**: 预留扩展模块，表结构支持
4. **安全性好**: 表单验证、CSRF、SQL注入防护
5. **性能优化**: 索引、分页、缓存策略

### 15.2 技术栈确认

| 层级 | 技术选型 |
|------|----------|
| 后端 | Laravel 10+ |
| 前端 | Vue 3 + Vite + TDesign |
| 数据库 | MySQL 8.0 |
| AI | DeepSeek API |

### 15.3 下一步行动

1. ✅ 技术架构审查完成
2. ⏳ 开始 Phase 1：项目初始化
3. ⏳ 完成数据库迁移
4. ⏳ 开发基础 API
5. ⏳ 开发前端基础组件

---

**审查完成**
**状态**: 就绪，可以开始开发
