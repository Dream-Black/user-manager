<template>
  <div class="product-price-trend p-6">
    <!-- 标题栏 -->
    <div class="flex items-center gap-2 mb-6">
      <t-button variant="text" @click="$router.back()">
        <template #icon><i class="i-lucide-arrow-left" /></template>
      </t-button>
      <h1 class="text-2xl font-bold">商品价格趋势：{{ productName }}</h1>
    </div>

    <!-- 商品搜索 -->
    <t-card class="mb-4" hover-shadow>
      <div class="flex gap-2">
        <t-input
          v-model="searchKeyword"
          placeholder="搜索商品名称..."
          class="flex-1"
          clearable
          @enter="searchProduct"
        >
          <template #suffixIcon>
            <i class="i-lucide-search cursor-pointer" @click="searchProduct" />
          </template>
        </t-input>
        <t-button theme="primary" @click="searchProduct">搜索</t-button>
      </div>
      <!-- 搜索结果下拉 -->
      <div v-if="searchResults.length > 0" class="mt-2 border rounded max-h-40 overflow-y-auto">
        <div
          v-for="item in searchResults"
          :key="item.id"
          class="p-2 hover:bg-gray-100 cursor-pointer"
          @click="selectProduct(item)"
        >
          {{ item.purpose }}
        </div>
      </div>
    </t-card>

    <!-- 价格趋势图 -->
    <t-card v-if="priceHistory.length > 0" class="mb-4" title="价格趋势图" hover-shadow>
      <v-chart :option="lineOption" style="height: 350px" autoresize />
    </t-card>

    <!-- 历史购买记录 -->
    <t-card v-if="priceHistory.length > 0" class="mb-4" title="历史购买记录" hover-shadow>
      <t-table :data="priceHistory" :columns="historyColumns" row-key="id" :pagination="null" stripe>
        <template #price="{ row }">
          <span class="font-bold">¥{{ row.amount?.toLocaleString('zh-CN', { minimumFractionDigits: 2 }) }}</span>
        </template>
      </t-table>
    </t-card>

    <!-- 价格统计 -->
    <t-card v-if="priceStats.avg > 0" title="价格统计" hover-shadow>
      <div class="grid grid-cols-3 gap-4 text-center">
        <div>
          <div class="text-gray-500 text-sm mb-1">平均价格</div>
          <div class="text-xl font-bold">¥{{ priceStats.avg?.toLocaleString('zh-CN', { minimumFractionDigits: 2 }) }}</div>
        </div>
        <div>
          <div class="text-gray-500 text-sm mb-1">最高价格</div>
          <div class="text-xl font-bold text-red-500">¥{{ priceStats.max?.toLocaleString('zh-CN', { minimumFractionDigits: 2 }) }}</div>
        </div>
        <div>
          <div class="text-gray-500 text-sm mb-1">最低价格</div>
          <div class="text-xl font-bold text-green-500">¥{{ priceStats.min?.toLocaleString('zh-CN', { minimumFractionDigits: 2 }) }}</div>
        </div>
      </div>
      <div class="mt-4 text-center">
        <t-tag :theme="priceLevel.theme" size="large">{{ priceLevel.text }}</t-tag>
      </div>
    </t-card>

    <!-- 占位提示 -->
    <div v-if="priceHistory.length === 0 && !loading" class="text-center text-gray-400 mt-8">
      请搜索商品查看价格趋势
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { MessagePlugin } from 'tdesign-vue-next'
import { financeExpenseApi } from '@/api'
import { use } from 'echarts/core'
import { LineChart } from 'echarts/charts'
import { TitleComponent, TooltipComponent, GridComponent, MarkLineComponent } from 'echarts/components'
import { CanvasRenderer } from 'echarts/renderers'
import VChart from 'vue-echarts'
import dayjs from 'dayjs'

// 注册 echarts 组件
use([LineChart, TitleComponent, TooltipComponent, GridComponent, MarkLineComponent, CanvasRenderer])

const route = useRoute()
const loading = ref(false)
const productName = ref('')
const searchKeyword = ref('')
const searchResults = ref([])
const priceHistory = ref([])

// 折线图配置
const lineOption = computed(() => ({
  tooltip: { trigger: 'axis' },
  xAxis: {
    type: 'category',
    data: priceHistory.value.map(h => dayjs(h.expenseDate).format('MM-DD'))
  },
  yAxis: { type: 'value', name: '价格(¥)' },
  series: [{
    type: 'line',
    data: priceHistory.value.map(h => h.amount),
    smooth: true,
    itemStyle: { color: '#3b82f6' },
    areaStyle: { color: 'rgba(59, 130, 246, 0.1)' }
  }]
}))

const historyColumns = [
  { colKey: 'expenseDate', title: '日期', width: 120, cell: { render: (h, { row }) => dayjs(row.expenseDate).format('YYYY-MM-DD') } },
  { colKey: 'amount', title: '购买价格', width: 150, cell: 'price' },
  { colKey: 'purpose', title: '用途/备注', ellipsis: true },
  { colKey: 'accountName', title: '账户', width: 100 }
]

const priceStats = computed(() => {
  if (priceHistory.value.length === 0) return { avg: 0, max: 0, min: 0 }
  const amounts = priceHistory.value.map(h => h.amount)
  const sum = amounts.reduce((a, b) => a + b, 0)
  return {
    avg: sum / amounts.length,
    max: Math.max(...amounts),
    min: Math.min(...amounts)
  }
})

const priceLevel = computed(() => {
  if (priceHistory.value.length === 0) return { text: '', theme: 'default' }
  const current = priceHistory.value[0]?.amount || 0
  const avg = priceStats.value.avg
  if (current < avg * 0.9) return { text: '当前价格：偏低', theme: 'success' }
  if (current > avg * 1.1) return { text: '当前价格：偏高', theme: 'danger' }
  return { text: '当前价格：正常', theme: 'primary' }
})

const searchProduct = async () => {
  if (!searchKeyword.value.trim()) return
  try {
    const res = await financeExpenseApi.list({ keyword: searchKeyword.value })
    // API 返回 { success, data: [...] }，拦截器返回此对象
    const data = res?.data || res || []
    // 按用途分组去重
    const map = {}
    data.forEach(item => {
      if (!map[item.purpose]) map[item.purpose] = item
    })
    searchResults.value = Object.values(map).slice(0, 10)
  } catch (error) {
    MessagePlugin.error('搜索失败')
  }
}

const selectProduct = async (item) => {
  productName.value = item.purpose
  searchKeyword.value = item.purpose
  searchResults.value = []
  await loadPriceHistory(item.purpose)
}

const loadPriceHistory = async (productName) => {
  loading.value = true
  try {
    const res = await financeExpenseApi.list({ keyword: productName })
    // API 返回 { success, data: [...] }，拦截器返回此对象
    const data = res?.data || res || []
    // 筛选相同用途的记录
    priceHistory.value = data.filter(item => item.purpose === productName)
  } catch (error) {
    MessagePlugin.error('加载价格历史失败')
  } finally {
    loading.value = false
  }
}

onMounted(() => {
  // 从路由参数获取商品名称
  if (route.params.name) {
    productName.value = route.params.name
    searchKeyword.value = route.params.name
    loadPriceHistory(route.params.name)
  }
})
</script>
