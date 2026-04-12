import { defineStore } from 'pinia'
import { ref } from 'vue'
import { categoryApi } from '@/api'

export const useCategoryStore = defineStore('category', () => {
  const categories = ref([])
  const loading = ref(false)

  const categoryMap = ref({})

  async function fetchCategories() {
    loading.value = true
    try {
      categories.value = await categoryApi.list()
      // 构建分类映射
      categoryMap.value = {}
      categories.value.forEach(c => {
        categoryMap.value[c.name] = c
      })
    } finally {
      loading.value = false
    }
  }

  async function createCategory(data) {
    const category = await categoryApi.create(data)
    categories.value.push(category)
    categoryMap.value[category.name] = category
    return category
  }

  async function updateCategory(id, data) {
    const category = await categoryApi.update(id, data)
    const index = categories.value.findIndex(c => c.id === id)
    if (index !== -1) {
      categories.value[index] = category
      categoryMap.value[category.name] = category
    }
    return category
  }

  async function deleteCategory(id) {
    await categoryApi.delete(id)
    const category = categories.value.find(c => c.id === id)
    if (category) {
      delete categoryMap.value[category.name]
    }
    categories.value = categories.value.filter(c => c.id !== id)
  }

  function getCategoryByName(name) {
    return categoryMap.value[name]
  }

  return {
    categories,
    loading,
    categoryMap,
    fetchCategories,
    createCategory,
    updateCategory,
    deleteCategory,
    getCategoryByName
  }
})
