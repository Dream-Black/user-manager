<template>
  <div class="stats-report p-6">
    <!-- 标题栏 -->
    <h1 class="text-2xl font-bold mb-6">统计报表</h1>

    <!-- 时间范围选择 -->
    <t-card class="mb-4" hover-shadow>
      <div class="flex flex-wrap gap-4 items-center">
        <t-space>
          <t-button :theme="quickRange === 'month' ? 'primary' : 'default'" variant="outline" @click="setQuickRange('month')">本月</t-button>
          <t-button :theme="quickRange === '3month' ? 'primary' : 'default'" variant="outline" @click="setQuickRange('3month')">近3月</t-button>
          <t-button :theme="quickRange === '6month' ? 'primary' : 'default'" variant="outline" @click="setQuickRange('6month')">近6月</t-button>
          <t-button :theme="quickRange === 'year' ? 'primary' : 'default'" variant="outline" @click="setQuickRange('year')">近1年</t-button>
          <t-button :theme="quickRange === 'custom' ? 'primary' : 'default'" variant="outline" @click="quickRange = 'custom'">自定义</t-button>
        </t-space>
        <t-date-range-picker
          v-if="quickRange === 'custom'"
          v-model="dateRange"
          allow-input
          style="width: 280px"
          @change="loadData"
        />
      </div>
    </t-card>

    <!-- 汇总卡片 -->
    <div class="grid grid-cols-3 gap-4 mb-6">
      <t-card hover-shadow>
        <div class="text-center">
          <div class="text-gray-500 text-sm mb-1">支出</div>
          <div class="text-2xl font-bold text-red-500">
            -¥{{ summary.expense?.toLocaleString('zh-CN', { minimumFractionDigits: 2 }) || '0.00' }}
          </div>
        </div>
      </t-card>
      <t-card hover-shadow>
        <div class="text-center">
          <div class="text-gray-500 text-sm mb-1">收入</div>
          <div class="text-2xl font-bold text-green-500">
            +¥{{ summary.income?.toLocaleString('zh-CN', { minimumFractionDigits: 2 }) || '0.00' }}
          </div>
        </div>
      </t-card>
      <t-card hover-shadow>
        <div class="text-center">
          <div class="text-gray-500 text-sm mb-1">结余</div>
          <div class="text-2xl font-bold" :class="summary.balance >= 0 ? 'text-blue-500' : 'text-red-500'">
            {{ summary.balance >= 0 ? '+' : '' }}¥{{ summary.balance?.toLocaleString('zh-CN', { minimumFractionDigits: 2 }) || '0.00' }}
          </div>
        </div>
      </t-card>
    </div>

    <!-- Tab切换 -->
    <t-tabs v-model="activeTab">
      <!-- 支出分析 -->
      <t-tab-panel value="expense" label="支出分析">
        <div class="grid grid-cols-2 gap-4 mt-4">
          <t-card title="支出分类占比" hover-shadow>
            <v-chart :option="pieOption" style="height: 350px" autoresize />
          </t-card>
          <t-card title="分类明细" hover-shadow>
            <t-table :data="categoryList" :columns="categoryColumns" row-key="name" :pagination="null" stripe>
              <template #amount="{ row }">
                <span class="font-bold">¥{{ row.amount?.toLocaleString('zh-CN', { minimumFractionDigits: 2 }) }}</span>
              </template>
              <template #percent="{ row }">
                {{ ((row.amount / summary.expense) * 100).toFixed(1) }}%
              </template>
              <template #action="{ row }">
                <t-button variant="text" size="small" @click="drillDown(row)">查看明细</t-button>
              </template>
            </t-table>
          </t-card>
        </div>
      </t-tab-panel>

      <!-- 收入分析 -->
      <t-tab-panel value="income" label="收入分析">
        <div class="grid grid-cols-2 gap-4 mt-4">
          <t-card title="收入来源构成" hover-shadow>
            <v-chart :option="incomeBarOption" style="height: 350px" autoresize />
          </t-card>
          <t-card title="月存款趋势" hover-shadow>
            <v-chart :option="savingsLineOption" style="height: 350px" autoresize />
          </t-card>
        </div>
      </t-tab-panel>

      <!-- 账户分析 -->
      <t-tab-panel value="account" label="账户分析">
        <div class="mt-4">
          <t-card title="账户余额变化曲线" hover-shadow>
            <v-chart :option="balanceLineOption" style="height: 400px" autoresize />
          </t-card>
        </div>
      </t-tab-panel>
    </t-tabs>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { MessagePlugin } from 'tdesign-vue-next'
import { financeExpenseApi, financeIncomeApi, financeTransferApi } from '@/api'
import { use } from 'echarts/core'
import { PieChart, BarChart, LineChart } from 'echarts/charts'
import { TitleComponent, TooltipComponent, LegendComponent, GridComponent } from 'echarts/components'
import { CanvasRenderer } from 'echarts/renderers'
import VChart from 'vue-echarts'
import dayjs from 'dayjs'

// 注册 echarts 组件
use([PieChart, BarChart, LineChart, TitleComponent, TooltipComponent, LegendComponent, GridComponent, CanvasRenderer])

const router = useRouter()
const loading = ref(false)
const activeTab = ref('expense')
const quickRange = ref('month')
const dateRange = ref([])

const summary = ref({ expense: 0, income: 0, balance: 0 })
const categoryList = ref([])
const incomeList = ref([])
const balanceTrend = ref([])
const monthlySavings = ref([])

// 饼图配置
const pieOption = computed(() => ({
  tooltip: { trigger: 'item', formatter: '{b}: ¥{c} ({d}%)' },
  legend: { orient: 'vertical', right: 10, top: 'center' },
  series: [{
    type: 'pie',
    radius: ['40%', '70%'],
    center: ['40%', '50%'],
    data: categoryList.value.map(cat => ({ name: cat.name, value: cat.amount }))
  }]
}))

// 收入柱状图
const incomeBarOption = computed(() => ({
  tooltip: { trigger: 'axis' },
  xAxis: { type: 'category', data: incomeList.value.map(i => i.name) },
  yAxis: { type: 'value' },
  series: [{
    type: 'bar',
    data: incomeList.value.map(i => i.amount),
    itemStyle: { color: '#22c55e' }
  }]
}))

// 余额变化折线图
const balanceLineOption = computed(() => ({
  tooltip: { trigger: 'axis' },
  legend: { data: ['余额趋势'] },
  xAxis: { type: 'category', data: balanceTrend.value.map(b => b.date) },
  yAxis: { type: 'value' },
  series: [{
    name: '余额',
    type: 'line',
    data: balanceTrend.value.map(b => b.balance),
    smooth: true
  }]
}))

// 月存款折线图
const savingsLineOption = computed(() => ({
  tooltip: { trigger: 'axis' },
  xAxis: { type: 'category', data: monthlySavings.value.map(m => m.month) },
  yAxis: { type: 'value' },
  series: [{
    name: '月存款',
    type: 'line',
    data: monthlySavings.value.map(m => m.savings),
    smooth: true,
    itemStyle: { color: '#3b82f6' }
  }]
}))

const categoryColumns = [
  { colKey: 'name', title: '分类', width: 120 },
  { colKey: 'amount', title: '金额', width: 150, cell: 'amount' },
  { colKey: 'percent', title: '占比', width: 100, cell: 'percent' },
  { colKey: 'action', title: '操作', width: 120, cell: 'action' }
]

const setQuickRange = (type) => {
  quickRange.value = type
  const now = dayjs()
  switch (type) {
    case 'month':
      dateRange.value = [now.startOf('month').format('YYYY-MM-DD'), now.endOf('month').format('YYYY-MM-DD')]
      break
    case '3month':
      dateRange.value = [now.subtract(3, 'month').startOf('month').format('YYYY-MM-DD'), now.format('YYYY-MM-DD')]
      break
    case '6month':
      dateRange.value = [now.subtract(6, 'month').startOf('month').format('YYYY-MM-DD'), now.format('YYYY-MM-DD')]
      break
    case 'year':
      dateRange.value = [now.subtract(1, 'year').format('YYYY-MM-DD'), now.format('YYYY-MM-DD')]
      break
  }
  loadData()
}

const loadData = async () => {
  if (!dateRange.value || dateRange.value.length !== 2) return
  loading.value = true
  try {
    const params = { startDate: dateRange.value[0], endDate: dateRange.value[1] }

    const [expensesRes, incomesRes, balanceRes, savingsRes] = await Promise.all([
      financeExpenseApi.list(params),
      financeIncomeApi.list(params),
      financeTransferApi.balanceTrend(params),
      financeTransferApi.monthlySavings(params)
    ])

    // API 返回 { success, data: [...] }，提取 data
    const expenses = expensesRes?.data || expensesRes || []
    const incomes = incomesRes?.data || incomesRes || []
    balanceTrend.value = balanceRes?.data || balanceRes || []
    monthlySavings.value = savingsRes?.data || savingsRes || []

    // 支出统计 - 本地按分类聚合
    const categoryMap = {}
    expenses.forEach(e => {
      const catName = e.categoryName || '未分类'
      categoryMap[catName] = (categoryMap[catName] || 0) + e.amount
    })
    categoryList.value = Object.entries(categoryMap).map(([name, amount]) => ({ name, amount }))
    summary.value.expense = categoryList.value.reduce((sum, c) => sum + c.amount, 0)

    // 收入统计 - 本地按来源聚合
    const incomeMap = {}
    incomes.forEach(inc => {
      const key = inc.content || '其他'
      incomeMap[key] = (incomeMap[key] || 0) + inc.amount
    })
    incomeList.value = Object.entries(incomeMap).map(([name, amount]) => ({ name, amount }))
    summary.value.income = incomeList.value.reduce((sum, i) => sum + i.amount, 0)

    summary.value.balance = summary.value.income - summary.value.expense
  } catch (error) {
    MessagePlugin.error('加载数据失败')
  } finally {
    loading.value = false
  }
}

const drillDown = (cat) => {
  router.push({
    name: 'ExpenseList',
    query: { categoryId: cat.id }
  })
}

onMounted(() => {
  setQuickRange('month')
})
</script>
