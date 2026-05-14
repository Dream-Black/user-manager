<template>
  <t-dialog
    v-model:visible="visible"
    :header="isEdit ? '编辑支出' : '记支出'"
    width="600px"
    @confirm="handleSubmit"
    @close="handleClose"
  >
    <t-form ref="formRef" :data="formData" :rules="rules" label-width="100px">
      <!-- 模式切换 -->
      <t-form-item v-if="!isEdit" label="记录模式">
        <t-radio-group v-model="mode" variant="default-filled">
          <t-radio-button value="simple">简单模式</t-radio-button>
          <t-radio-button value="list">清单模式</t-radio-button>
        </t-radio-group>
      </t-form-item>

      <!-- 简单模式 -->
      <template v-if="mode === 'simple'">
        <t-form-item label="金额" name="amount">
          <t-input-number
            v-model="formData.amount"
            :min="0.01"
            :decimal-places="2"
            placeholder="请输入金额"
            style="width: 200px"
          />
        </t-form-item>
        <t-form-item label="用途" name="purpose">
          <t-input v-model="formData.purpose" placeholder="请输入用途" clearable />
        </t-form-item>
        <t-form-item label="分类">
          <CategorySelect v-model="formData.categoryId" />
        </t-form-item>
        <t-form-item label="支出日期" name="expenseDate">
          <t-date-picker
            v-model="formData.expenseDate"
            enable-time-picker
            allow-input
            clearable
          />
        </t-form-item>
        <t-form-item label="备注">
          <t-textarea v-model="formData.remark" placeholder="备注信息" />
        </t-form-item>
      </template>

      <!-- 清单模式 -->
      <template v-if="mode === 'list'">
        <t-form-item label="用途描述" name="purpose">
          <t-input v-model="formData.purpose" placeholder="如：聚餐、购物" clearable />
        </t-form-item>
        <t-form-item label="清单子项">
          <div class="w-full">
            <div v-for="(item, index) in formData.items" :key="index" class="flex gap-2 mb-2">
              <t-input v-model="item.name" placeholder="名称" class="flex-1" />
              <t-input-number v-model="item.quantity" :min="1" placeholder="数量" class="w-24" />
              <t-input v-model="item.unit" placeholder="单位" class="w-20" />
              <t-input-number
                v-model="item.unitPrice"
                :min="0"
                :decimal-places="2"
                placeholder="单价"
                class="w-32"
              />
              <span class="inline-flex items-center text-sm text-gray-600">
                ¥{{ (item.quantity * item.unitPrice).toFixed(2) }}
              </span>
              <t-button theme="danger" variant="text" @click="removeItem(index)">
                <template #icon><i class="i-lucide-trash-2" /></template>
              </t-button>
            </div>
            <t-button variant="dashed" block @click="addItem">
              <template #icon><i class="i-lucide-plus" /></template>
              添加子项
            </t-button>
            <div class="mt-2 text-right font-bold">
              合计：¥{{ totalAmount.toFixed(2) }}
            </div>
          </div>
        </t-form-item>
        <t-form-item label="支出日期" name="expenseDate">
          <t-date-picker
            v-model="formData.expenseDate"
            enable-time-picker
            allow-input
            clearable
          />
        </t-form-item>
        <t-form-item label="备注">
          <t-textarea v-model="formData.remark" placeholder="备注信息" />
        </t-form-item>
      </template>
    </t-form>
  </t-dialog>
</template>

<script setup>
import { ref, computed, watch } from 'vue'
import { MessagePlugin } from 'tdesign-vue-next'
import { financeExpenseApi } from '@/api'
import { useFinanceStore } from '@/stores/finance'
import CategorySelect from './CategorySelect.vue'

const props = defineProps({
  modelValue: Boolean,
  editData: Object
})

const emit = defineEmits(['update:modelValue', 'success'])

const financeStore = useFinanceStore()
const formRef = ref(null)
const mode = ref('simple')
const isEdit = computed(() => !!props.editData)

const visible = computed({
  get: () => props.modelValue,
  set: (val) => emit('update:modelValue', val)
})

const formData = ref({
  amount: undefined,
  purpose: '',
  categoryId: undefined,
  expenseDate: new Date(),
  remark: '',
  items: []
})

const rules = {
  amount: [{ required: true, message: '请输入金额' }],
  purpose: [{ required: true, message: '请输入用途' }],
  expenseDate: [{ required: true, message: '请选择支出日期' }]
}

const totalAmount = computed(() => {
  return formData.value.items.reduce((sum, item) => {
    return sum + (item.quantity || 0) * (item.unitPrice || 0)
  }, 0)
})

const addItem = () => {
  formData.value.items.push({
    name: '',
    quantity: 1,
    unit: '个',
    unitPrice: 0
  })
}

const removeItem = (index) => {
  formData.value.items.splice(index, 1)
}

const resetForm = () => {
  formData.value = {
    amount: undefined,
    purpose: '',
    categoryId: undefined,
    expenseDate: new Date(),
    remark: '',
    items: []
  }
  mode.value = 'simple'
}

const handleSubmit = async () => {
  const valid = await formRef.value?.validate()
  if (valid !== true) return

  try {
    const data = { ...formData.value }
    if (mode.value === 'list') {
      data.type = 'list'
      data.amount = totalAmount.value
      data.items = formData.value.items.map((item, idx) => ({
        ...item,
        subtotal: item.quantity * item.unitPrice,
        sortOrder: idx
      }))
    } else {
      data.type = 'simple'
    }

    // 获取默认账户
    const defaultAcc = financeStore.defaultAccount
    if (defaultAcc) {
      data.accountId = defaultAcc.id
    }

    if (isEdit.value) {
      await financeExpenseApi.update(props.editData.id, data)
      MessagePlugin.success('更新成功')
    } else {
      await financeExpenseApi.create(data)
      MessagePlugin.success('记录成功')
    }

    emit('success')
    visible.value = false
    resetForm()
  } catch (error) {
    MessagePlugin.error(error.message || '操作失败')
  }
}

const handleClose = () => {
  resetForm()
}

// 编辑模式时填充数据
watch(() => props.editData, (val) => {
  if (val) {
    formData.value = {
      amount: val.amount,
      purpose: val.purpose,
      categoryId: val.categoryId,
      expenseDate: new Date(val.expenseDate),
      remark: val.remark || '',
      items: val.items || []
    }
    mode.value = val.type || 'simple'
  }
}, { immediate: true })
</script>
