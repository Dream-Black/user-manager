<template>
  <t-dialog
    v-model:visible="visible"
    :header="formData.type === 'salary' ? '记工资收入' : '记零散收入'"
    width="650px"
    @confirm="handleSubmit"
    @close="handleClose"
  >
    <t-form ref="formRef" :data="formData" :rules="rules" label-width="110px">
      <!-- 收入类型 -->
      <t-form-item label="收入类型" name="type">
        <t-radio-group v-model="formData.type" variant="default-filled">
          <t-radio-button value="salary">工资收入</t-radio-button>
          <t-radio-button value="misc">零散收入</t-radio-button>
        </t-radio-group>
      </t-form-item>

      <!-- 零散收入 -->
      <template v-if="formData.type === 'misc'">
        <t-form-item label="金额" name="amount">
          <t-input-number
            v-model="formData.amount"
            :min="0.01"
            :decimal-places="2"
            placeholder="请输入金额"
            style="width: 200px"
          />
        </t-form-item>
        <t-form-item label="内容" name="content">
          <t-input v-model="formData.content" placeholder="请输入收入内容" clearable />
        </t-form-item>
        <t-form-item label="所属项目">
          <t-select v-model="formData.projectId" placeholder="请选择项目（可选）" clearable filterable>
            <t-option v-for="p in projects" :key="p.id" :value="p.id" :label="p.name" />
          </t-select>
        </t-form-item>
        <t-form-item label="收入日期" name="incomeDate">
          <t-date-picker v-model="formData.incomeDate" allow-input clearable />
        </t-form-item>
        <t-form-item label="分配到账户">
          <div class="w-full">
            <div v-for="(alloc, idx) in formData.allocations" :key="idx" class="flex gap-2 mb-2">
              <AccountSelect v-model="alloc.accountId" class="flex-1" />
              <t-input-number
                v-model="alloc.amount"
                :min="0"
                :decimal-places="2"
                placeholder="金额"
                class="w-36"
              />
              <t-button theme="danger" variant="text" @click="removeAllocation(idx)">
                <template #icon><i class="i-lucide-trash-2" /></template>
              </t-button>
            </div>
            <t-button variant="dashed" block @click="addAllocation">
              <template #icon><i class="i-lucide-plus" /></template>
              添加分配
            </t-button>
          </div>
        </t-form-item>
        <t-form-item label="备注">
          <t-textarea v-model="formData.remark" placeholder="备注信息" />
        </t-form-item>
      </template>

      <!-- 工资收入 -->
      <template v-if="formData.type === 'salary'">
        <t-form-item label="选择模板" name="templateId">
          <t-select v-model="formData.templateId" placeholder="请选择工资模板" @change="loadTemplate">
            <t-option v-for="t in templates" :key="t.id" :value="t.id" :label="t.title" />
          </t-select>
        </t-form-item>
        <t-form-item label="工资月份" name="salaryDate">
          <t-date-picker
            v-model="formData.salaryDate"
            mode="month"
            allow-input
            clearable
            placeholder="选择年月"
          />
        </t-form-item>
        <t-form-item label="标题（公司名）">
          <t-input v-model="formData.content" placeholder="如：字节跳动" clearable />
        </t-form-item>

        <!-- 工资明细项 -->
        <template v-if="templateItems.length > 0">
          <t-divider>工资明细</t-divider>
          <t-form-item v-for="(item, idx) in templateItems" :key="idx" :label="item.name">
            <t-input-number
              v-model="formData.detailItems[idx]"
              :min="0"
              :decimal-places="2"
              placeholder="金额"
              style="width: 200px"
            />
            <t-checkbox v-model="formData.actualItemIndex" :value="idx" class="ml-4">
              实际到手项
            </t-checkbox>
          </t-form-item>
          <t-form-item label="实际到手">
            <span class="font-bold text-green-600">
              ¥{{ actualAmount.toLocaleString('zh-CN', { minimumFractionDigits: 2 }) }}
            </span>
          </t-form-item>
        </template>

        <t-form-item label="分配到账户">
          <div class="w-full">
            <div v-for="(alloc, idx) in formData.allocations" :key="idx" class="flex gap-2 mb-2">
              <AccountSelect v-model="alloc.accountId" class="flex-1" />
              <t-input-number
                v-model="alloc.amount"
                :min="0"
                :decimal-places="2"
                placeholder="金额"
                class="w-36"
              />
              <t-button theme="danger" variant="text" @click="removeAllocation(idx)">
                <template #icon><i class="i-lucide-trash-2" /></template>
              </t-button>
            </div>
            <t-button variant="dashed" block @click="addAllocation">
              <template #icon><i class="i-lucide-plus" /></template>
              添加分配
            </t-button>
          </div>
        </t-form-item>

        <t-form-item label="备注">
          <t-textarea v-model="formData.remark" placeholder="备注信息" />
        </t-form-item>
      </template>
    </t-form>
  </t-dialog>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { MessagePlugin } from 'tdesign-vue-next'
import { financeIncomeApi, financeSalaryTemplateApi, projectApi } from '@/api'
import AccountSelect from './AccountSelect.vue'

const props = defineProps({
  modelValue: Boolean
})

const emit = defineEmits(['update:modelValue', 'success'])

const formRef = ref(null)
const templates = ref([])
const projects = ref([])
const templateItems = ref([])

const visible = computed({
  get: () => props.modelValue,
  set: (val) => emit('update:modelValue', val)
})

const formData = ref({
  type: 'misc',
  amount: undefined,
  content: '',
  projectId: undefined,
  incomeDate: new Date(),
  salaryDate: new Date(),
  templateId: undefined,
  detailItems: [],
  actualItemIndex: -1,
  allocations: [],
  remark: ''
})

const rules = {
  amount: [{ required: true, message: '请输入金额' }],
  content: [{ required: true, message: '请输入内容' }],
  incomeDate: [{ required: true, message: '请选择日期' }],
  templateId: [{ required: true, message: '请选择模板' }],
  salaryDate: [{ required: true, message: '请选择月份' }]
}

const actualAmount = computed(() => {
  if (formData.value.actualItemIndex >= 0) {
    return formData.value.detailItems[formData.value.actualItemIndex] || 0
  }
  return 0
})

const addAllocation = () => {
  formData.value.allocations.push({ accountId: undefined, amount: 0 })
}

const removeAllocation = (idx) => {
  formData.value.allocations.splice(idx, 1)
}

const loadTemplate = async (templateId) => {
  if (!templateId) {
    templateItems.value = []
    formData.value.detailItems = []
    return
  }
  try {
    const res = await financeSalaryTemplateApi.get(templateId)
    // API 返回 { success, data: {...} }，拦截器返回此对象
    const template = res?.data || res
    templateItems.value = template.templateItems || []
    formData.value.detailItems = (template.templateItems || []).map(() => 0)
  } catch (error) {
    MessagePlugin.error('加载模板失败')
  }
}

const resetForm = () => {
  formData.value = {
    type: 'misc',
    amount: undefined,
    content: '',
    projectId: undefined,
    incomeDate: new Date(),
    salaryDate: new Date(),
    templateId: undefined,
    detailItems: [],
    actualItemIndex: -1,
    allocations: [],
    remark: ''
  }
  templateItems.value = []
}

const handleSubmit = async () => {
  const valid = await formRef.value?.validate()
  if (valid !== true) return

  try {
    const data = { ...formData.value }

    // 处理工资收入
    if (data.type === 'salary') {
      data.amount = actualAmount.value
      data.salaryDetail = {
        templateId: data.templateId,
        salaryDate: data.salaryDate,
        actualItemId: templateItems.value[data.actualItemIndex]?.id,
        remark: data.remark,
        detailItems: templateItems.value.map((item, idx) => ({
          templateItemId: item.id,
          amount: data.detailItems[idx] || 0
        }))
      }
      delete data.detailItems
      delete data.actualItemIndex
    }

    // 处理收入分配
    if (data.allocations.length > 0) {
      data.incomeAccounts = data.allocations.filter(a => a.accountId && a.amount > 0)
    }
    delete data.allocations

    await financeIncomeApi.create(data)
    MessagePlugin.success('记录成功')
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

onMounted(async () => {
  try {
    const [templateRes, projectRes] = await Promise.all([
      financeSalaryTemplateApi.list(),
      projectApi.list()
    ])
    // API 返回 { success, data: [...] }，拦截器返回此对象
    templates.value = templateRes?.data || templateRes || []
    projects.value = projectRes?.data || projectRes || []
  } catch (error) {
    console.error('加载数据失败:', error)
  }
})
</script>
