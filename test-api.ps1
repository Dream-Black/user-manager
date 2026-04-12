$headers = @{"Content-Type" = "application/json"}

Write-Host "=== 1. 测试创建项目 ===" -ForegroundColor Cyan
$body = @{
    name = "测试项目A"
    description = "这是一个测试项目"
    type = "web"
    status = "in_progress"
} | ConvertTo-Json

try {
    $result = Invoke-RestMethod -Uri "http://localhost:5000/api/projects" -Method POST -Headers $headers -Body $body
    Write-Host "✅ 创建成功: ID=$($result.id), Name=$($result.name)" -ForegroundColor Green
    $projectId = $result.id
} catch {
    Write-Host "❌ 创建失败: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "=== 2. 测试获取项目列表 ===" -ForegroundColor Cyan
try {
    $projects = Invoke-RestMethod -Uri "http://localhost:5000/api/projects" -Method GET
    Write-Host "✅ 获取成功: 共 $($projects.Count) 个项目" -ForegroundColor Green
    $projects | ForEach-Object { Write-Host "   - $($_.name) (ID=$($_.id))" }
} catch {
    Write-Host "❌ 获取失败: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "=== 3. 测试创建任务 ===" -ForegroundColor Cyan
$taskBody = @{
    title = "测试任务1"
    description = "这是测试任务描述"
    projectId = $projectId
    status = "todo"
    priority = "medium"
    estimatedHours = 8
} | ConvertTo-Json

try {
    $task = Invoke-RestMethod -Uri "http://localhost:5000/api/tasks" -Method POST -Headers $headers -Body $taskBody
    Write-Host "✅ 创建成功: ID=$($task.id), Title=$($task.title)" -ForegroundColor Green
    $taskId = $task.id
} catch {
    Write-Host "❌ 创建失败: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "=== 4. 测试获取任务列表 ===" -ForegroundColor Cyan
try {
    $tasks = Invoke-RestMethod -Uri "http://localhost:5000/api/tasks" -Method GET
    Write-Host "✅ 获取成功: 共 $($tasks.Count) 个任务" -ForegroundColor Green
} catch {
    Write-Host "❌ 获取失败: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "=== 5. 测试更新任务 ===" -ForegroundColor Cyan
$updateBody = @{
    status = "in_progress"
} | ConvertTo-Json

try {
    $updated = Invoke-RestMethod -Uri "http://localhost:5000/api/tasks/$taskId" -Method PUT -Headers $headers -Body $updateBody
    Write-Host "✅ 更新成功: Status=$($updated.status)" -ForegroundColor Green
} catch {
    Write-Host "❌ 更新失败: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "=== 6. 测试分类 API ===" -ForegroundColor Cyan
try {
    $categories = Invoke-RestMethod -Uri "http://localhost:5000/api/categories" -Method GET
    Write-Host "✅ 获取成功: 共 $($categories.Count) 个分类" -ForegroundColor Green
} catch {
    Write-Host "❌ 获取失败: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "=== 7. 测试时间线 API ===" -ForegroundColor Cyan
try {
    $timelines = Invoke-RestMethod -Uri "http://localhost:5000/api/timelines/recent?limit=5" -Method GET
    Write-Host "✅ 获取成功: 共 $($timelines.Count) 条记录" -ForegroundColor Green
} catch {
    Write-Host "❌ 获取失败: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "=== 8. 测试设置 API ===" -ForegroundColor Cyan
try {
    $settings = Invoke-RestMethod -Uri "http://localhost:5000/api/settings" -Method GET
    Write-Host "✅ 获取成功: Theme=$($settings.theme), Language=$($settings.language)" -ForegroundColor Green
} catch {
    Write-Host "❌ 获取失败: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "=== 全部 API 测试完成 ===" -ForegroundColor Cyan
