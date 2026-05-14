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
        <span v-if="getCategoryIcon(value)" class="text-base">{{ getCategoryIcon(value) }}</span>
        <span>{{ getCategoryName(value) }}</span>
      </div>
    </template>
    <template #option="{ option }">
      <div class="flex items-center gap-2">
        <span v-if="option.icon" class="text-base">{{ option.icon }}</span>
        <span :style="{ color: option.color }">{{ option.label }}</span>
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
    default: '请选择分类'
  }
})

const emit = defineEmits(['update:modelValue', 'change'])

const financeStore = useFinanceStore()
const loading = ref(false)
const selectedValue = ref(props.modelValue)

const options = computed(() => financeStore.categoryOptions)

const getCategoryIcon = (id) => {
  const cat = financeStore.categories.find(c => c.id === id)
  return cat?.icon || ''
}

const getCategoryName = (id) => {
  const cat = financeStore.categories.find(c => c.id === id)
  return cat?.name || ''
}

const handleChange = (value) => {
  emit('update:modelValue', value)
  emit('change', value)
}

watch(() => props.modelValue, (newVal) => {
  selectedValue.value = newVal
})

onMounted(async () => {
  if (financeStore.categories.length === 0) {
    loading.value = true
    await financeStore.fetchCategories()
    loading.value = false
  }
})
</script>
