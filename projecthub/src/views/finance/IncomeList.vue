<template>
  <div class="income-list p-6">
    <!-- 标题栏 -->
    <div class="flex justify-between items-center mb-6">
      <h1 class="text-2xl font-bold">收入记录</h1>
      <div class="flex gap-2">
        <t-button theme="primary" @click="showSalaryForm = true">
          <template #icon><i class="i-lucide-plus" /></template>
          记工资
        </t-button>
        <t-button theme="success" @click="showIncomeForm = true">
          <template #icon><i class="i-lucide-plus-circle" /></template>
          记零散收入
        </t-button>
      </div>
    </div>

    <!-- 筛选栏 -->
    <t-card class="mb-4" hover-shadow>
      <div class="flex flex-wrap gap-4 items-center">
        <t-space direction="vertical" size="small">
          <div class="text-sm text-gray-600 mb-1">日期范围</div>
          <t-date-range-picker
            v-model="filter.dateRange"
            allow-input
            clearable
            style="width: 280px"
          />
        </t-space>
        <t-space direction="vertical" size="small">
          <div class="text-sm text-gray-600 mb-1">类型</div>
          <t-select v-model="filter.type" placeholder="全部类型" clearable style="width: 150px">
            <t-option value="salary" label="工资" />
            <t-option value="misc" label="零散" />
          </t-select>
        </t-space>
        <t-space direction="vertical" size="small">
          <div class="text-sm text-gray-600 mb-1">搜索</div>
          <t-input v-model="filter.keyword" placeholder="搜索内容..." clearable style="width: 200px">
            <template #suffixIcon>
              <i class="i-lucide-search cursor-pointer" @click="loadData" />
            </template>
          </t-input>
        </t-space>
        <t-space>
          <t-button theme="primary" variant="outline" @click="loadData">
            <template #icon><i class="i-lucide-search" /></template>
            搜索
          </t-button>
          <t-button variant="outline" @click="resetFilter">
            <template #icon><i class="i-lucide-rotate-ccw" /></template>
            重置
          </t-button>
        </t-space>
      </div>
    </t-card>

    <!-- 收入列表 -->
    <t-card hover-shadow>
      <t-table
        :data="incomes"
        :columns="columns"
        row-key="id"
        :loading="loading"
        :pagination="pagination"
        @page-change="handlePageChange"
        stripe
        hover
      >
        <template #date="{ row }">
          {{ formatDate(row.incomeDate) }}
        </template>
        <template #type="{ row }">
          <t-tag :theme="row.type === 'salary' ? 'primary' : 'success'" variant="light">
            {{ row.type === 'salary' ? '工资' : '零散' }}
          </t-tag>
        </template>
        <template #content="{ row }">
          <div class="flex items-center gap-2">
            <span>{{ row.content }}</span>
            <t-tag v-if="row.projectName" size="small" theme="default" variant="light">{{ row.projectName }}</t-tag>
          </div>
        </template>
        <template #amount="{ row }">
          <span class="font-bold text-green-500">
            +¥{{ row.amount.toLocaleString('zh-CN', { minimumFractionDigits: 2 }) }}
          </span>
        </template>
        <template #operation="{ row }">
          <t-button variant="text" size="small" @click="viewDetail(row)">
            详情
          </t-button>
        </template>
      </t-table>
    </t-card>

    <!-- 表单弹窗 -->
    <IncomeForm v-model="showIncomeForm" @success="loadData" />
  </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { MessagePlugin } from 'tdesign-vue-next'
import { financeIncomeApi } from '@/api'
import dayjs from 'dayjs'
import IncomeForm from '@/components/finance/IncomeForm.vue'

const router = useRouter()
const loading = ref(false)
const incomes = ref([])
const showIncomeForm = ref(false)
const showSalaryForm = ref(false)

const filter = reactive({
  dateRange: [],
  type: undefined,
  keyword: ''
})

const pagination = reactive({
  current: 1,
  pageSize: 20,
  total: 0
})

const columns = [
  { colKey: 'incomeDate', title: '日期', width: 120, cell: 'date' },
  { colKey: 'type', title: '类型', width: 100, cell: 'type' },
  { colKey: 'content', title: '内容/公司', ellipsis: true, cell: 'content' },
  { colKey: 'amount', title: '金额', width: 150, cell: 'amount' },
  { colKey: 'operation', title: '操作', width: 100, cell: 'operation', fixed: 'right' }
]

const formatDate = (date) => dayjs(date).format('YYYY-MM-DD')

const loadData = async () => {
  loading.value = true
  try {
    const params = {
      page: pagination.current,
      pageSize: pagination.pageSize
    }
    if (filter.dateRange?.length === 2) {
      params.startDate = filter.dateRange[0]
      params.endDate = filter.dateRange[1]
    }
    if (filter.type) params.type = filter.type
    if (filter.keyword) params.keyword = filter.keyword

    const res = await financeIncomeApi.list(params)
    // API 返回 { success, data: [...] }，拦截器返回此对象
    const data = res?.data || res
    incomes.value = data?.items || data || []
    pagination.total = res?.total || data?.length || 0
  } catch (error) {
    MessagePlugin.error('加载数据失败')
  } finally {
    loading.value = false
  }
}

const resetFilter = () => {
  filter.dateRange = []
  filter.type = undefined
  filter.keyword = ''
  pagination.current = 1
  loadData()
}

const handlePageChange = (pageInfo) => {
  pagination.current = pageInfo.current
  pagination.pageSize = pageInfo.pageSize
  loadData()
}

const viewDetail = (row) => {
  // 可以跳转到详情页，暂用弹窗显示
  MessagePlugin.info(`收入详情：${row.content}，金额：¥${row.amount}`)
}
</script>
