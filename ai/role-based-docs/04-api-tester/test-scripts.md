# 测试脚本说明

## 📁 脚本文件位置
```
scripts/
├── test-api.ps1           # API测试脚本（已存在）
├── test-backend.ps1      # 后端API测试
└── test-proxy.ps1        # 本地代理测试
```

---

## 🚀 快速测试

### 1. 启动所有服务
```powershell
# 启动后端
cd api
dotnet run

# 启动前端
cd projecthub
npm run dev

# 启动本地代理
cd proxy
python main.py
```

### 2. 运行API测试
```powershell
.\test-api.ps1
```

---

## 📋 测试脚本内容

### test-api.ps1 (已存在)

```powershell
# 测试API端点可用性
$baseUrl = "http://localhost:5000/api"

# 测试1: 获取当前电脑
Write-Host "测试: 获取当前电脑"
$response = Invoke-RestMethod -Uri "$baseUrl/computers/current" -Method GET -Headers @{"X-Computer-Hostname"="TEST-PC"}
Write-Host "结果: $($response | ConvertTo-Json)"

# 测试2: 创建资源路径
Write-Host "测试: 创建资源路径"
$body = @{
    computerId = 1
    type = "comic"
    path = "D:/TestComics/"
    isEnabled = $true
}
$response = Invoke-RestMethod -Uri "$baseUrl/resource-paths" -Method POST -ContentType "application/json" -Body ($body | ConvertTo-Json)
Write-Host "结果: $($response | ConvertTo-Json)"

# 测试3: 获取漫画列表
Write-Host "测试: 获取漫画列表"
$response = Invoke-RestMethod -Uri "$baseUrl/comics?computerId=1" -Method GET
Write-Host "结果: 漫画数量 = $($response.total)"
```

---

### test-proxy.ps1 (新建)

```powershell
# 本地代理服务测试
$proxyUrl = "http://localhost:6789"

Write-Host "=== 本地代理服务测试 ==="

# 测试1: 健康检查
Write-Host "`n[1/4] 测试健康检查..."
try {
    $response = Invoke-RestMethod -Uri "$proxyUrl/health" -Method GET -TimeoutSec 5
    Write-Host "✓ 健康检查通过: $($response.status)"
} catch {
    Write-Host "✗ 健康检查失败: $_"
}

# 测试2: 系统信息
Write-Host "`n[2/4] 测试系统信息..."
try {
    $response = Invoke-RestMethod -Uri "$proxyUrl/system/info" -Method GET -TimeoutSec 5
    Write-Host "✓ 系统信息: $($response.hostname)"
} catch {
    Write-Host "✗ 系统信息获取失败: $_"
}

# 测试3: 列出文件
Write-Host "`n[3/4] 测试文件列表..."
$testPath = "D:/TestComics"
try {
    $response = Invoke-RestMethod -Uri "$proxyUrl/files/list?path=$testPath" -Method GET -TimeoutSec 10
    Write-Host "✓ 文件列表: $($response.count) 个项目"
} catch {
    Write-Host "✗ 文件列表获取失败: $_"
}

# 测试4: 读取图片
Write-Host "`n[4/4] 测试图片读取..."
$imagePath = "D:/TestComics/Test/001.jpg"
try {
    $response = Invoke-WebRequest -Uri "$proxyUrl/files/read?path=$imagePath" -Method GET -TimeoutSec 10
    if ($response.StatusCode -eq 200) {
        Write-Host "✓ 图片读取成功: $($response.Content.Length) bytes"
    }
} catch {
    Write-Host "✗ 图片读取失败: $_"
}

Write-Host "`n=== 测试完成 ==="
```

---

### test-backend.ps1 (新建)

```powershell
# 后端API完整测试
$baseUrl = "http://localhost:5000/api"
$hostname = "TEST-HOST-$(Get-Random)"

Write-Host "=== 后端API完整测试 ==="
Write-Host "测试主机名: $hostname"

# ===== Computer API =====
Write-Host "`n--- Computer API ---"

# TC-COM-001: 获取当前电脑
Write-Host "`n[COM-001] 获取当前电脑..."
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/computers/current" -Method GET -Headers @{"X-Computer-Hostname"=$hostname}
    if ($response.hostName -eq $hostname) {
        Write-Host "✓ PASS: 自动创建新电脑"
        $computerId = $response.id
    }
} catch {
    Write-Host "✗ FAIL: $_"
}

# TC-COM-006: 发送心跳
Write-Host "`n[COM-006] 发送心跳..."
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/computers/$computerId/heartbeat" -Method POST
    Write-Host "✓ PASS: 心跳成功"
} catch {
    Write-Host "✗ FAIL: $_"
}

# ===== ResourcePath API =====
Write-Host "`n--- ResourcePath API ---"

# TC-PATH-002: 创建资源路径
Write-Host "`n[PATH-002] 创建资源路径..."
try {
    $body = @{
        computerId = $computerId
        type = "comic"
        path = "D:/TestComics/"
        isEnabled = $true
    }
    $response = Invoke-RestMethod -Uri "$baseUrl/resource-paths" -Method POST -ContentType "application/json" -Body ($body | ConvertTo-Json)
    Write-Host "✓ PASS: 创建成功"
    $pathId = $response.id
} catch {
    Write-Host "✗ FAIL: $_"
}

# TC-PATH-005: 更新资源路径
Write-Host "`n[PATH-005] 更新资源路径..."
try {
    $body = @{ isEnabled = $false }
    Invoke-RestMethod -Uri "$baseUrl/resource-paths/$pathId" -Method PUT -ContentType "application/json" -Body ($body | ConvertTo-Json)
    Write-Host "✓ PASS: 更新成功"
} catch {
    Write-Host "✗ FAIL: $_"
}

# ===== Comic API =====
Write-Host "`n--- Comic API ---"

# TC-COMIC-001: 获取漫画列表
Write-Host "`n[COMIC-001] 获取漫画列表..."
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/comics?computerId=$computerId" -Method GET
    Write-Host "✓ PASS: 获取成功，总数=$($response.total)"
} catch {
    Write-Host "✗ FAIL: $_"
}

Write-Host "`n=== 测试完成 ==="
```

---

## 🧪 手动测试清单

### API测试检查表

#### 计算机管理
- [ ] 获取当前电脑（新建）
- [ ] 获取当前电脑（已有）
- [ ] 获取所有电脑列表
- [ ] 创建电脑
- [ ] 更新电脑
- [ ] 发送心跳

#### 资源路径管理
- [ ] 获取路径列表
- [ ] 创建路径（正常）
- [ ] 创建路径（路径格式错误）
- [ ] 创建路径（重复类型）
- [ ] 更新路径
- [ ] 删除路径（级联删除）

#### 漫画管理
- [ ] 获取漫画列表
- [ ] 获取漫画列表（筛选）
- [ ] 获取漫画列表（搜索）
- [ ] 获取漫画详情
- [ ] 更新漫画
- [ ] 删除漫画
- [ ] 上传封面
- [ ] 扫描文件夹

#### 章节管理
- [ ] 获取章节列表
- [ ] 获取章节列表（排序）
- [ ] 获取章节页面

---

## 📊 测试报告模板

```markdown
## API测试报告

### 测试信息
- 测试时间: 2026-04-14 20:00
- 测试人员: [姓名]
- 测试环境: localhost

### 测试结果汇总
| 模块 | 通过 | 失败 | 阻塞 |
|------|------|------|------|
| Computer API | - | - | - |
| ResourcePath API | - | - | - |
| Comic API | - | - | - |
| Chapter API | - | - | - |
| **总计** | - | - | - |

### 问题列表
| ID | 模块 | 描述 | 严重程度 |
|----|------|------|----------|
| 1 | - | - | - |

### 结论
[通过/不通过]
```
