<template>
  <t-dialog
    v-model:visible="visible"
    :header="isEdit ? '编辑模板' : '新建工资模板'"
    width="600px"
    @confirm="handleSubmit"
    @close="handleClose"
  >
    <t-form ref="formRef" :data="formData" :rules="rules" label-width="100px">
      <t-form-item label="模板名称" name="title">
        <t-input v-model="formData.title" placeholder="如：默认工资模板" clearable />
      </t-form-item>
      <t-form-item label="备注">
        <t-textarea v-model="formData.remark" placeholder="备注信息（可选）" />
      </t-form-item>

      <t-divider>模板子项</t-divider>

      <div class="mb-4">
        <div
          v-for="(item, index) in formData.items"
          :key="index"
          class="flex items-center gap-2 mb-2 p-2 bg-gray-50 rounded"
        >
          <span class="text-gray-400 cursor-move">≡</span>
          <t-input v-model="item.name" placeholder="子项名称" class="flex-1" />
          <t-checkbox v-model="item.isActual" class="whitespace-nowrap">
            实际到手项
          </t-checkbox>
          <t-button theme="danger" variant="text" size="small" @click="removeItem(index)">
            <template #icon><i class="i-lucide-trash-2" /></template>
          </t-button>
        </div>
        <t-button variant="dashed" block @click="addItem">
          <template #icon><i class="i-lucide-plus" /></template>
          添加子项
        </t-button>
      </div>
    </t-form>
  </t-dialog>
</template>

<script setup>
import { ref, computed, watch } from 'vue'
import { MessagePlugin } from 'tdesign-vue-next'
import { financeSalaryTemplateApi } from '@/api'

const props = defineProps({
  modelValue: Boolean,
  editData: Object
})

const emit = defineEmits(['update:modelValue', 'success'])

const formRef = ref(null)
const isEdit = computed(() => !!props.editData)

const visible = computed({
  get: () => props.modelValue,
  set: (val) => emit('update:modelValue', val)
})

const formData = ref({
  title: '',
  remark: '',
  items: []
})

const rules = {
  title: [{ required: true, message: '请输入模板名称' }]
}

const addItem = () => {
  formData.value.items.push({
    name: '',
    sortOrder: formData.value.items.length,
    isActual: false
  })
}

const removeItem = (index) => {
  formData.value.items.splice(index, 1)
  // 重新排序
  formData.value.items.forEach((item, idx) => {
    item.sortOrder = idx
  })
}

const resetForm = () => {
  formData.value = {
    title: '',
    remark: '',
    items: []
  }
}

const handleSubmit = async () => {
  const valid = await formRef.value?.validate()
  if (valid !== true) return

  // 验证子项
  const hasEmptyName = formData.value.items.some(item => !item.name.trim())
  if (hasEmptyName) {
    MessagePlugin.warning('请填写所有子项名称')
    return
  }

  try {
    const data = {
      title: formData.value.title,
      remark: formData.value.remark,
      templateItems: formData.value.items.map((item, idx) => ({
        name: item.name,
        sortOrder: idx
      }))
    }

    if (isEdit.value) {
      await financeSalaryTemplateApi.update(props.editData.id, data)
      MessagePlugin.success('更新成功')
    } else {
      await financeSalaryTemplateApi.create(data)
      MessagePlugin.success('创建成功')
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

// 编辑模式填充数据
watch(() => props.editData, (val) => {
  if (val) {
    formData.value = {
      title: val.title,
      remark: val.remark || '',
      items: (val.templateItems || []).map(item => ({
        name: item.name,
        sortOrder: item.sortOrder,
        isActual: false // 实际到手项在录入时标记
      }))
    }
  }
}, { immediate: true })
</script>
