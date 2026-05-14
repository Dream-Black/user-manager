<template>
  <div class="expense-detail p-6">
    <!-- 标题栏 -->
    <div class="flex justify-between items-center mb-6">
      <div class="flex items-center gap-2">
        <t-button variant="text" @click="$router.back()">
          <template #icon><i class="i-lucide-arrow-left" /></template>
        </t-button>
        <h1 class="text-2xl font-bold">支出详情</h1>
      </div>
      <div class="flex gap-2">
        <t-button theme="primary" variant="outline" @click="handleEdit">
          <template #icon><i class="i-lucide-pencil" /></template>
          编辑
        </t-button>
        <t-button theme="danger" variant="outline" @click="handleDelete">
          <template #icon><i class="i-lucide-trash-2" /></template>
          删除
        </t-button>
      </div>
    </div>

    <!-- 基本信息卡片 -->
    <t-card class="mb-4" hover-shadow>
      <t-descriptions :column="2" layout="horizontal" item-layout="horizontal">
        <t-descriptions-item label="金额">
          <span class="text-2xl font-bold text-red-500">
            -¥{{ expense.amount?.toLocaleString('zh-CN', { minimumFractionDigits: 2 }) }}
          </span>
        </t-descriptions-item>
        <t-descriptions-item label="用途">
          {{ expense.purpose }}
        </t-descriptions-item>
        <t-descriptions-item label="分类">
          <t-tag v-if="expense.categoryName" :color="expense.categoryColor" variant="light">
            {{ expense.categoryName }}
          </t-tag>
          <CategorySelect
            v-else
            v-model="selectedCategoryId"
            style="width: 150px"
            @change="handleCategoryChange"
          />
        </t-descriptions-item>
        <t-descriptions-item label="账户">
          {{ expense.accountName || '-' }}
        </t-descriptions-item>
        <t-descriptions-item label="记录时间">
          {{ formatDateTime(expense.expenseDate) }}
        </t-descriptions-item>
        <t-descriptions-item label="创建时间">
          {{ formatDateTime(expense.createdAt) }}
        </t-descriptions-item>
      </t-descriptions>
    </t-card>

    <!-- 清单子项（清单模式时显示） -->
    <t-card v-if="expense.type === 'list' && expense.items?.length > 0" class="mb-4" title="清单子项" hover-shadow>
      <t-table
        :data="expense.items"
        :columns="itemColumns"
        row-key="id"
        :pagination="null"
        stripe
      >
        <template #subtotal="{ row }">
          ¥{{ row.subtotal?.toLocaleString('zh-CN', { minimumFractionDigits: 2 }) }}
        </template>
      </t-table>
      <div class="mt-4 text-right font-bold">
        合计：¥{{ expense.amount?.toLocaleString('zh-CN', { minimumFractionDigits: 2 }) }}
      </div>
    </t-card>

    <!-- 备注 -->
    <t-card title="备注" hover-shadow>
      <div v-if="expense.remark" class="text-gray-700 whitespace-pre-wrap">{{ expense.remark }}</div>
      <span v-else class="text-gray-400">暂无备注</span>
    </t-card>

    <!-- 编辑表单弹窗 -->
    <ExpenseForm v-model="showForm" :edit-data="expense" @success="loadData" />
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { MessagePlugin, DialogPlugin } from 'tdesign-vue-next'
import { financeExpenseApi, financeCategoryApi } from '@/api'
import dayjs from 'dayjs'
import CategorySelect from '@/components/finance/CategorySelect.vue'
import ExpenseForm from '@/components/finance/ExpenseForm.vue'

const route = useRoute()
const router = useRouter()

const expense = ref({})
const showForm = ref(false)
const selectedCategoryId = ref(undefined)

const itemColumns = [
  { colKey: 'name', title: '名称', width: 200 },
  { colKey: 'quantity', title: '数量', width: 100 },
  { colKey: 'unit', title: '单位', width: 80 },
  { colKey: 'unitPrice', title: '单价', width: 120, cell: { render: (h, { row }) => `¥${row.unitPrice?.toLocaleString('zh-CN', { minimumFractionDigits: 2 })}` } },
  { colKey: 'subtotal', title: '小计', width: 150, cell: 'subtotal' }
]

const formatDateTime = (date) => {
  if (!date) return '-'
  return dayjs(date).format('YYYY-MM-DD HH:mm')
}

const loadData = async () => {
  try {
    const res = await financeExpenseApi.get(route.params.id)
    // API 返回 { success, data: {...} }，拦截器返回此对象
    const data = res?.data || res
    expense.value = data
    selectedCategoryId.value = data.categoryId
  } catch (error) {
    MessagePlugin.error('加载数据失败')
    router.back()
  }
}

const handleCategoryChange = async (categoryId) => {
  try {
    await financeExpenseApi.update(expense.value.id, { ...expense.value, categoryId })
    MessagePlugin.success('分类更新成功')
    loadData()
  } catch (error) {
    MessagePlugin.error('分类更新失败')
  }
}

const handleEdit = () => {
  showForm.value = true
}

const handleDelete = () => {
  const dialog = DialogPlugin.confirm({
    header: '确认删除',
    body: `确定要删除"${expense.value.purpose}"这条支出记录吗？`,
    onConfirm: async () => {
      try {
        await financeExpenseApi.delete(expense.value.id)
        MessagePlugin.success('删除成功')
        router.back()
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
