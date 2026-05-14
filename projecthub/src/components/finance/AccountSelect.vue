<template>
  <t-select
    v-model="selectedValue"
    :options="options"
    :placeholder="placeholder"
    :loading="loading"
    filterable
    clearable
    @change="handleChange"
  >
    <template #valueDisplay="{ value }">
      <div v-if="value" class="flex items-center gap-2">
        <span v-if="getAccountIcon(value)" class="text-base">{{ getAccountIcon(value) }}</span>
        <span>{{ getAccountName(value) }}</span>
        <span class="text-gray-400 text-sm">¥{{ getAccountBalance(value)?.toLocaleString() }}</span>
      </div>
    </template>
    <template #option="{ option }">
      <div class="flex items-center gap-2 justify-between w-full">
        <div class="flex items-center gap-2">
          <span v-if="option.icon" class="text-base">{{ option.icon }}</span>
          <span>{{ option.label }}</span>
        </div>
        <span class="text-gray-500 text-sm">¥{{ getBalanceById(option.value)?.toLocaleString() }}</span>
      </div>
    </template>
  </t-select>
</template>

<script setup>
import { ref, computed, onMounted, watch } from 'vue'
import { useFinanceStore } from '@/stores/finance'

const props = defineProps({
  modelValue: [Number, String],
  placeholder: {
    type: String,
    default: '请选择账户'
  }
})

const emit = defineEmits(['update:modelValue', 'change'])

const financeStore = useFinanceStore()
const loading = ref(false)
const selectedValue = ref(props.modelValue)

const options = computed(() => financeStore.accountOptions)

const getAccountIcon = (id) => {
  const acc = financeStore.accounts.find(a => a.id === id)
  return acc?.icon || ''
}

const getAccountName = (id) => {
  const acc = financeStore.accounts.find(a => a.id === id)
  return acc?.name || ''
}

const getAccountBalance = (id) => {
  const acc = financeStore.accounts.find(a => a.id === id)
  return acc?.balance || 0
}

const getBalanceById = (id) => {
  const acc = financeStore.accounts.find(a => a.id === id)
  return acc?.balance || 0
}

const handleChange = (value) => {
  emit('update:modelValue', value)
  emit('change', value)
}

watch(() => props.modelValue, (newVal) => {
  selectedValue.value = newVal
})

onMounted(async () => {
  if (financeStore.accounts.length === 0) {
    loading.value = true
    await financeStore.fetchAccounts()
    loading.value = false
  }
})
</script>
