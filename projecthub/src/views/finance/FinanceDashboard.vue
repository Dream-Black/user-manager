<template>
  <div class="finance-dashboard">
    <!-- 页面头部 -->
    <div class="page-header">
      <div class="header-content">
        <div class="welcome-section">
          <h1 class="page-title">财务概览</h1>
          <p class="page-subtitle">掌握您的财务状况，合理规划收支</p>
        </div>
        <div class="header-actions">
          <button class="btn btn-expense" @click="showExpenseForm = true">
            <i class="i-lucide-minus-circle"></i>
            记支出
          </button>
          <button class="btn btn-income" @click="showIncomeForm = true">
            <i class="i-lucide-plus-circle"></i>
            记收入
          </button>
        </div>
      </div>
    </div>

    <!-- 统计卡片 -->
    <section class="stats-section">
      <div class="stats-grid">
        <div class="stat-card" style="animationDelay: 0s">
          <div class="stat-icon danger">
            <TrendingDownIcon />
          </div>
          <div class="stat-info">
            <span class="stat-value text-danger">-¥{{ stats.monthExpense?.toLocaleString('zh-CN', { minimumFractionDigits: 2 }) || '0.00' }}</span>
            <span class="stat-label">本月支出</span>
          </div>
        </div>
        <div class="stat-card" style="animationDelay: 0.1s">
          <div class="stat-icon success">
            <TrendingUpIcon />
          </div>
          <div class="stat-info">
            <span class="stat-value text-success">+¥{{ stats.monthIncome?.toLocaleString('zh-CN', { minimumFractionDigits: 2 }) || '0.00' }}</span>
            <span class="stat-label">本月收入</span>
          </div>
        </div>
        <div class="stat-card" style="animationDelay: 0.2s">
          <div class="stat-icon primary">
            <WalletIcon />
          </div>
          <div class="stat-info">
            <span class="stat-value" :class="monthlyBalance >= 0 ? 'text-primary' : 'text-danger'">
              {{ monthlyBalance >= 0 ? '+' : '' }}¥{{ monthlyBalance.toLocaleString('zh-CN', { minimumFractionDigits: 2 }) }}
            </span>
            <span class="stat-label">本月结余</span>
          </div>
        </div>
        <div class="stat-card" style="animationDelay: 0.3s">
          <div class="stat-icon purple">
            <MoneyIcon />
          </div>
          <div class="stat-info">
            <span class="stat-value text-purple">{{ accountCount }}</span>
            <span class="stat-label">账户总数</span>
          </div>
        </div>
      </div>
    </section>

    <!-- 快捷操作 -->
    <section class="quick-actions-section">
      <div class="quick-actions-grid">
        <button class="quick-action-btn" @click="$router.push({ name: 'AccountManage' })">
          <div class="action-icon">
            <WalletIcon />
          </div>
          <span class="action-label">账户管理</span>
        </button>
        <button class="quick-action-btn" @click="showExpenseForm = true">
          <div class="action-icon">
            <TrendingDownIcon />
          </div>
          <span class="action-label">记支出</span>
        </button>
        <button class="quick-action-btn" @click="showIncomeForm = true">
          <div class="action-icon">
            <TrendingUpIcon />
          </div>
          <span class="action-label">记收入</span>
        </button>
        <button class="quick-action-btn" @click="showTransferForm = true">
          <div class="action-icon">
            <SwapIcon />
          </div>
          <span class="action-label">转账</span>
        </button>
        <button class="quick-action-btn" @click="$router.push({ name: 'StatsReport' })">
          <div class="action-icon">
            <ChartBarIcon />
          </div>
          <span class="action-label">看统计</span>
        </button>
      </div>
    </section>

    <!-- 图表区域 -->
    <section class="charts-section">
      <div class="charts-grid">
        <div class="chart-card">
          <div class="chart-header">
            <div>
              <h3 class="chart-title">本月支出分类</h3>
            </div>
          </div>
          <v-chart :option="pieOption" style="height: 300px" autoresize />
        </div>
        <div class="chart-card">
          <div class="chart-header">
            <div>
              <h3 class="chart-title">本月收入来源</h3>
            </div>
          </div>
          <v-chart :option="barOption" style="height: 300px" autoresize />
        </div>
      </div>
    </section>

    <!-- 最近交易记录 -->
    <section class="transactions-section">
      <div class="transactions-card">
        <div class="card-header">
          <h3 class="card-title">最近交易记录</h3>
        </div>
        <t-table
          :data="recentTransactions"
          :columns="transactionColumns"
          row-key="id"
          :loading="loading"
          :pagination="null"
          stripe
        >
          <template #date="{ row }">
            {{ formatDate(row.expenseDate || row.incomeDate) }}
          </template>
          <template #description="{ row }">
            {{ row.purpose || row.content || '收入' }}
          </template>
          <template #category="{ row }">
            <t-tag v-if="row.categoryName" theme="primary" variant="light">{{ row.categoryName }}</t-tag>
            <span v-else class="text-tertiary">-</span>
          </template>
          <template #amount="{ row }">
            <span :class="row.amount < 0 ? 'text-danger' : 'text-success'">
              {{ row.amount < 0 ? '-' : '+' }}¥{{ Math.abs(row.amount).toLocaleString('zh-CN', { minimumFractionDigits: 2 }) }}
            </span>
          </template>
        </t-table>
      </div>
    </section>

    <!-- 弹窗组件 -->
    <ExpenseForm v-model="showExpenseForm" @success="refreshData" />
    <IncomeForm v-model="showIncomeForm" @success="refreshData" />
    <TransferForm v-model="showTransferForm" @success="refreshData" />
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useFinanceStore } from '@/stores/finance'
import { financeExpenseApi, financeIncomeApi } from '@/api'
import { use } from 'echarts/core'
import { PieChart, BarChart } from 'echarts/charts'
import { TitleComponent, TooltipComponent, LegendComponent, GridComponent } from 'echarts/components'
import { CanvasRenderer } from 'echarts/renderers'
import VChart from 'vue-echarts'
import dayjs from 'dayjs'
import ExpenseForm from '@/components/finance/ExpenseForm.vue'
import IncomeForm from '@/components/finance/IncomeForm.vue'
import TransferForm from '@/components/finance/TransferForm.vue'
import { TrendingDownIcon, TrendingUpIcon, WalletIcon, MoneyIcon, SwapIcon, ChartBarIcon } from 'tdesign-icons-vue-next'

// 注册 echarts 组件
use([PieChart, BarChart, TitleComponent, TooltipComponent, LegendComponent, GridComponent, CanvasRenderer])

const financeStore = useFinanceStore()
const loading = ref(false)
const showExpenseForm = ref(false)
const showIncomeForm = ref(false)
const showTransferForm = ref(false)

const stats = ref({
  monthExpense: 0,
  monthIncome: 0
})

const recentTransactions = ref([])
const categoryStats = ref([])
const incomeStats = ref([])

const accountCount = computed(() => financeStore.accounts.length)
const monthlyBalance = computed(() => (stats.value.monthIncome || 0) - (stats.value.monthExpense || 0))

// 饼图配置
const pieOption = computed(() => ({
  tooltip: { trigger: 'item', formatter: '{b}: ¥{c} ({d}%)' },
  legend: { orient: 'vertical', right: 10, top: 'center' },
  series: [{
    type: 'pie',
    radius: ['40%', '70%'],
    center: ['40%', '50%'],
    data: categoryStats.value.map(cat => ({
      name: cat.name,
      value: cat.amount
    }))
  }]
}))

// 柱状图配置
const barOption = computed(() => ({
  tooltip: { trigger: 'axis' },
  xAxis: {
    type: 'category',
    data: incomeStats.value.map(item => item.name)
  },
  yAxis: { type: 'value' },
  series: [{
    type: 'bar',
    data: incomeStats.value.map(item => item.amount),
    itemStyle: { color: '#22c55e' }
  }]
}))

const transactionColumns = [
  { colKey: 'date', title: '日期', width: 120, cell: 'date' },
  { colKey: 'description', title: '描述', ellipsis: true, cell: 'description' },
  { colKey: 'category', title: '分类', width: 100, cell: 'category' },
  { colKey: 'amount', title: '金额', width: 150, cell: 'amount' }
]

const formatDate = (date) => dayjs(date).format('MM-DD')

const refreshData = async () => {
  loading.value = true
  try {
    const now = dayjs()
    const startOfMonth = now.startOf('month').format('YYYY-MM-DD')
    const endOfMonth = now.endOf('month').format('YYYY-MM-DD')

    const [accounts, expensesRes, incomesRes] = await Promise.all([
      financeStore.fetchAccounts(),
      financeExpenseApi.list({ startDate: startOfMonth, endDate: endOfMonth }),
      financeIncomeApi.list({ startDate: startOfMonth, endDate: endOfMonth })
    ])

    // API 返回 { success, data: [...] }，提取 data
    const expenses = expensesRes?.data || expensesRes || []
    const incomes = incomesRes?.data || incomesRes || []

    // 计算统计数据
    stats.value = {
      monthExpense: expenses.reduce((sum, e) => sum + e.amount, 0),
      monthIncome: incomes.reduce((sum, i) => sum + i.amount, 0)
    }

    // 本地按分类聚合数据来做饼图
    const categoryMap = {}
    expenses.forEach(e => {
      const catName = e.categoryName || '未分类'
      categoryMap[catName] = (categoryMap[catName] || 0) + e.amount
    })
    categoryStats.value = Object.entries(categoryMap).map(([name, amount]) => ({ name, amount }))

    // 收入统计
    const incomeMap = {}
    incomes.forEach(inc => {
      const key = inc.content || '其他'
      incomeMap[key] = (incomeMap[key] || 0) + inc.amount
    })
    incomeStats.value = Object.entries(incomeMap).map(([name, amount]) => ({ name, amount }))

    // 最近交易（合并支出和收入）
    const transactions = [
      ...expenses.map(e => ({ ...e, sortDate: e.expenseDate })),
      ...incomes.map(i => ({ ...i, sortDate: i.incomeDate }))
    ]
    recentTransactions.value = transactions
      .sort((a, b) => new Date(b.sortDate) - new Date(a.sortDate))
      .slice(0, 10)

  } catch (error) {
    console.error('加载数据失败:', error)
  } finally {
    loading.value = false
  }
}

onMounted(() => {
  refreshData()
})
</script>

<style scoped>
.finance-dashboard {
  padding: var(--space-6);
  max-width: var(--content-max-width);
  margin: 0 auto;
  animation: fadeIn 0.5s ease;
}

/* 页面头部 */
.page-header {
  margin-bottom: var(--space-8);
}

.header-content {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: var(--space-6);
}

.welcome-section {
  animation: fadeInUp 0.6s ease;
}

.page-title {
  font-size: var(--font-size-2xl);
  font-weight: var(--font-weight-bold);
  color: var(--text-primary);
  margin-bottom: var(--space-2);
}

.page-subtitle {
  font-size: var(--font-size-sm);
  color: var(--text-secondary);
}

.header-actions {
  display: flex;
  gap: var(--space-3);
  animation: fadeInUp 0.6s ease 0.1s backwards;
}

/* 按钮样式 */
.btn {
  display: inline-flex;
  align-items: center;
  gap: var(--space-2);
  padding: var(--space-3) var(--space-5);
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  border: none;
  border-radius: var(--radius-lg);
  cursor: pointer;
  transition: all var(--transition-normal);
}

.btn-expense {
  background: var(--danger-lighter, #fef2f2);
  color: var(--danger-color, #ef4444);
  border: 1px solid var(--danger-light, #fecaca);
}

.btn-expense:hover {
  background: var(--danger-color, #ef4444);
  color: white;
  transform: translateY(-2px);
  box-shadow: var(--shadow-md);
}

.btn-income {
  background: var(--success-lighter, #f0fdf4);
  color: var(--success-color, #22c55e);
  border: 1px solid var(--success-light, #bbf7d0);
}

.btn-income:hover {
  background: var(--success-color, #22c55e);
  color: white;
  transform: translateY(-2px);
  box-shadow: var(--shadow-md);
}

/* 统计卡片 */
.stats-section {
  margin-bottom: var(--space-8);
}

.stats-grid {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: var(--space-5);
}

.stat-card {
  background: var(--bg-card-solid);
  border-radius: var(--radius-xl);
  padding: var(--space-5);
  display: flex;
  align-items: flex-start;
  gap: var(--space-4);
  border: 1px solid var(--border-light);
  box-shadow: var(--shadow-card);
  animation: cardEnter 0.6s ease backwards;
  transition: all var(--transition-normal);
}

.stat-card:hover {
  transform: translateY(-4px);
  box-shadow: var(--shadow-lg);
  border-color: var(--primary-light);
}

.stat-icon {
  width: 48px;
  height: 48px;
  border-radius: var(--radius-lg);
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  font-size: 24px;
}

.stat-icon i {
  font-size: 24px;
}

.stat-icon.primary {
  background: var(--primary-lighter);
  color: var(--primary-color);
}

.stat-icon.success {
  background: var(--success-lighter);
  color: var(--success-color);
}

.stat-icon.danger {
  background: var(--danger-lighter, #fef2f2);
  color: var(--danger-color, #ef4444);
}

.stat-icon.purple {
  background: #f3e8ff;
  color: #a855f7;
}

.stat-info {
  flex: 1;
  display: flex;
  flex-direction: column;
}

.stat-value {
  font-size: var(--font-size-2xl);
  font-weight: var(--font-weight-bold);
  color: var(--text-primary);
  line-height: 1.2;
}

.stat-label {
  font-size: var(--font-size-sm);
  color: var(--text-secondary);
  margin-top: var(--space-1);
}

.text-primary {
  color: var(--primary-color);
}

.text-success {
  color: var(--success-color);
}

.text-danger {
  color: var(--danger-color, #ef4444);
}

.text-purple {
  color: #a855f7;
}

/* 快捷操作 */
.quick-actions-section {
  margin-bottom: var(--space-8);
  animation: fadeInUp 0.6s ease 0.3s backwards;
}

.quick-actions-grid {
  display: grid;
  grid-template-columns: repeat(5, 1fr);
  gap: var(--space-4);
}

.quick-action-btn {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: var(--space-3);
  padding: var(--space-5) var(--space-4);
  background: var(--bg-card-solid);
  border: 1px solid var(--border-light);
  border-radius: var(--radius-xl);
  cursor: pointer;
  transition: all var(--transition-normal);
  animation: cardEnter 0.5s ease backwards;
}

.quick-action-btn:hover {
  background: var(--gradient-primary);
  color: white;
  border-color: transparent;
  transform: translateY(-4px);
  box-shadow: var(--shadow-lg);
}

.quick-action-btn:hover .action-icon {
  background: rgba(255, 255, 255, 0.2);
  color: white;
}

.quick-action-btn:hover .action-label {
  color: white;
}

.action-icon {
  width: 40px;
  height: 40px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: var(--radius-lg);
  background: var(--primary-lighter);
  color: var(--primary-color);
  transition: all var(--transition-fast);
  font-size: 20px;
}

.action-icon i {
  font-size: 20px;
}

.action-label {
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  color: var(--text-primary);
  transition: color var(--transition-fast);
}

/* 图表区域 */
.charts-section {
  margin-bottom: var(--space-8);
}

.charts-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: var(--space-5);
}

.chart-card {
  background: var(--bg-card-solid);
  border-radius: var(--radius-xl);
  padding: var(--space-5);
  border: 1px solid var(--border-light);
  box-shadow: var(--shadow-card);
  animation: cardEnter 0.6s ease 0.2s backwards;
}

.chart-header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  margin-bottom: var(--space-5);
}

.chart-title {
  font-size: var(--font-size-base);
  font-weight: var(--font-weight-semibold);
  color: var(--text-primary);
}

/* 交易记录 */
.transactions-section {
  margin-bottom: var(--space-8);
}

.transactions-card {
  background: var(--bg-card-solid);
  border-radius: var(--radius-xl);
  padding: var(--space-5);
  border: 1px solid var(--border-light);
  box-shadow: var(--shadow-card);
  animation: cardEnter 0.6s ease 0.3s backwards;
}

.card-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: var(--space-5);
}

.card-title {
  font-size: var(--font-size-base);
  font-weight: var(--font-weight-semibold);
  color: var(--text-primary);
}

.text-tertiary {
  color: var(--text-tertiary);
}

/* 动画 */
@keyframes fadeIn {
  from {
    opacity: 0;
  }
  to {
    opacity: 1;
  }
}

@keyframes fadeInUp {
  from {
    opacity: 0;
    transform: translateY(20px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

@keyframes cardEnter {
  from {
    opacity: 0;
    transform: translateY(20px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

/* 响应式 */
@media (max-width: 1200px) {
  .stats-grid {
    grid-template-columns: repeat(2, 1fr);
  }
  
  .charts-grid {
    grid-template-columns: 1fr;
  }
  
  .quick-actions-grid {
    grid-template-columns: repeat(3, 1fr);
  }
}

@media (max-width: 768px) {
  .finance-dashboard {
    padding: var(--space-4);
  }
  
  .header-content {
    flex-direction: column;
  }
  
  .header-actions {
    width: 100%;
  }
  
  .stats-grid {
    grid-template-columns: 1fr;
  }
  
  .quick-actions-grid {
    grid-template-columns: repeat(2, 1fr);
  }
}
</style>
