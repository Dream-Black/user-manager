# Add FinanceNav import to StatsReport.vue
$file = "C:\Users\22618\Desktop\AI Claw\projecthub\src\views\finance\StatsReport.vue"
$content = Get-Content $file -Encoding UTF8
$newContent = $content -replace "(import VChart from 'vue-echarts')", '$1' + "`nimport FinanceNav from ''@/components/finance/FinanceNav.vue'''
Set-Content $file -Value $newContent -Encoding UTF8

# Add FinanceNav import to SalaryManage.vue
$file2 = "C:\Users\22618\Desktop\AI Claw\projecthub\src\views\finance\SalaryManage.vue"
$content2 = Get-Content $file2 -Encoding UTF8
$newContent2 = $content2 -replace "(import SalaryTemplateForm from '@/components/finance/SalaryTemplateForm.vue')", '$1' + "`nimport FinanceNav from ''@/components/finance/FinanceNav.vue'''
Set-Content $file2 -Value $newContent2 -Encoding UTF8

Write-Host "Done"
