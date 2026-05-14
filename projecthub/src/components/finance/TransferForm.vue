<template>
  <t-dialog
    v-model:visible="visible"
    header="转账"
    width="500px"
    @confirm="handleSubmit"
    @close="handleClose"
  >
    <t-form ref="formRef" :data="formData" :rules="rules" label-width="100px">
      <t-form-item label="转出账户" name="fromAccountId">
        <AccountSelect v-model="formData.fromAccountId" placeholder="请选择转出账户" />
      </t-form-item>
      <t-form-item label="转入账户" name="toAccountId">
        <AccountSelect v-model="formData.toAccountId" placeholder="请选择转入账户" />
      </t-form-item>
      <t-form-item label="转账金额" name="amount">
        <t-input-number
          v-model="formData.amount"
          :min="0.01"
          :decimal-places="2"
          placeholder="请输入转账金额"
          style="width: 200px"
        />
      </t-form-item>
      <t-form-item label="备注">
        <t-textarea v-model="formData.remark" placeholder="备注信息（可选）" />
      </t-form-item>
    </t-form>
  </t-dialog>
</template>

<script setup>
import { ref, computed, watch, nextTick } from 'vue'
import { MessagePlugin } from 'tdesign-vue-next'
import { financeTransferApi } from '@/api'
import AccountSelect from './AccountSelect.vue'

const props = defineProps({
  modelValue: Boolean
})

const emit = defineEmits(['update:modelValue', 'success'])

const formRef = ref(null)

const visible = computed({
  get: () => props.modelValue,
  set: (val) => emit('update:modelValue', val)
})

watch(visible, (newVal) => {
  if (newVal) {
    nextTick(() => {
      formRef.value?.clearValidate()
    })
  }
})

const formData = ref({
  fromAccountId: undefined,
  toAccountId: undefined,
  amount: undefined,
  remark: ''
})

const rules = {
  fromAccountId: [{ required: true, message: '请选择转出账户' }],
  toAccountId: [
    { required: true, message: '请选择转入账户' },
    {
      validator: (val) => val !== formData.value.fromAccountId,
      message: '转出账户和转入账户不能相同'
    }
  ],
  amount: [{ required: true, message: '请输入转账金额' }]
}

const resetForm = () => {
  formData.value = {
    fromAccountId: undefined,
    toAccountId: undefined,
    amount: undefined,
    remark: ''
  }
}

const handleSubmit = async () => {
  const valid = await formRef.value?.validate()
  if (valid !== true) return

  if (formData.value.fromAccountId === formData.value.toAccountId) {
    MessagePlugin.warning('转出账户和转入账户不能相同')
    return
  }

  try {
    await financeTransferApi.create(formData.value)
    MessagePlugin.success('转账成功')
    emit('success')
    visible.value = false
    resetForm()
  } catch (error) {
    MessagePlugin.error(error.message || '转账失败')
  }
}

const handleClose = () => {
  resetForm()
}
</script>
