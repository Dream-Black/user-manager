<template>
  <div class="expense-list p-6">
    <!-- 标题栏 -->
    <div class="flex justify-between items-center mb-6">
      <h1 class="text-2xl font-bold">支出记录</h1>
      <t-button theme="primary" @click="showForm = true">
        <template #icon><i class="i-lucide-plus" /></template>
        记支出
      </t-button>
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
          <div class="text-sm text-gray-600 mb-1">分类</div>
          <CategorySelect v-model="filter.categoryId" placeholder="全部分类" style="width: 150px" />
        </t-space>
        <t-space direction="vertical" size="small">
          <div class="text-sm text-gray-600 mb-1">关键词搜索</div>
          <t-input v-model="filter.keyword" placeholder="搜索用途..." clearable style="width: 200px">
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

    <!-- 支出列表 -->
    <t-card hover-shadow>
      <t-table
        :data="expenses"
        :columns="columns"
        row-key="id"
        :loading="loading"
        :pagination="pagination"
        @page-change="handlePageChange"
        stripe
        hover
      >
        <template #date="{ row }">
          {{ formatDate(row.expenseDate) }}
        </template>
        <template #purpose="{ row }">
          <div class="flex items-center gap-2">
            <span>{{ row.purpose }}</span>
            <t-tag v-if="row.type === 'list'" size="small" theme="warning" variant="light">清单</t-tag>
          </div>
        </template>
        <template #category="{ row }">
          <t-tag v-if="row.categoryName" :color="row.categoryColor" variant="light">{{ row.categoryName }}</t-tag>
          <span v-else class="text-gray-400">未分类</span>
        </template>
        <template #amount="{ row }">
          <span class="font-bold text-red-500">
            -¥{{ row.amount.toLocaleString('zh-CN', { minimumFractionDigits: 2 }) }}
          </span>
        </template>
        <template #account="{ row }">
          {{ row.accountName || '-' }}
        </template>
        <template #operation="{ row }">
          <t-space>
            <t-button variant="text" size="small" @click="viewDetail(row)">
              详情
            </t-button>
            <t-button variant="text" size="small" theme="danger" @click="handleDelete(row)">
              删除
            </t-button>
          </t-space>
        </template>
      </t-table>
    </t-card>

    <!-- 支出表单弹窗 -->
    <ExpenseForm v-model="showForm" @success="loadData" />
  </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { MessagePlugin, DialogPlugin } from 'tdesign-vue-next'
import { financeExpenseApi } from '@/api'
import dayjs from 'dayjs'
import CategorySelect from '@/components/finance/CategorySelect.vue'
import ExpenseForm from '@/components/finance/ExpenseForm.vue'

const router = useRouter()
const loading = ref(false)
const expenses = ref([])
const showForm = ref(false)

const filter = reactive({
  dateRange: [],
  categoryId: undefined,
  keyword: ''
})

const pagination = reactive({
  current: 1,
  pageSize: 20,
  total: 0,
  showJumper: true,
  showSizer: true,
  pageSizeOptions: [10, 20, 50]
})

const columns = [
  { colKey: 'expenseDate', title: '日期', width: 120, cell: 'date' },
  { colKey: 'purpose', title: '用途/商品', ellipsis: true, cell: 'purpose' },
  { colKey: 'category', title: '分类', width: 120, cell: 'category' },
  { colKey: 'amount', title: '金额', width: 150, cell: 'amount' },
  { colKey: 'account', title: '账户', width: 120, cell: 'account' },
  { colKey: 'operation', title: '操作', width: 150, cell: 'operation', fixed: 'right' }
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
    if (filter.categoryId) params.categoryId = filter.categoryId
    if (filter.keyword) params.keyword = filter.keyword

    const res = await financeExpenseApi.list(params)
    // API 返回 { success, data: [...], total }，拦截器返回此对象
    const data = res?.data || res
    expenses.value = data?.items || data || []
    pagination.total = res?.total || data?.length || 0
  } catch (error) {
    MessagePlugin.error('加载数据失败')
  } finally {
    loading.value = false
  }
}

const resetFilter = () => {
  filter.dateRange = []
  filter.categoryId = undefined
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
  router.push({ name: 'ExpenseDetail', params: { id: row.id } })
}

const handleDelete = (row) => {
  const dialog = DialogPlugin.confirm({
    header: '确认删除',
    body: `确定要删除"${row.purpose}"这条支出记录吗？`,
    onConfirm: async () => {
      try {
        await financeExpenseApi.delete(row.id)
        MessagePlugin.success('删除成功')
        loadData()
      } catch (error) {
        MessagePlugin.error('删除失败')
      }
      dialog.hide()
    }
  })
}

onMounted(() => {
  loadData()
})
</script>
