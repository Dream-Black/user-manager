import { defineStore } from 'pinia'
import { ref } from 'vue'
import { taskApi } from '@/api'

export const useTaskStore = defineStore('task', () => {
  const tasks = ref([])
  const currentTask = ref(null)
  const loading = ref(false)

  async function fetchTasksByProject(projectId) {
    loading.value = true
    try {
      tasks.value = await taskApi.listByProject(projectId)
    } finally {
      loading.value = false
    }
  }

  async function fetchTask(id) {
    loading.value = true
    try {
      currentTask.value = await taskApi.get(id)
      return currentTask.value
    } finally {
      loading.value = false
    }
  }

  async function createTask(data) {
    const task = await taskApi.create(data)
    tasks.value.push(task)
    return task
  }

  async function updateTask(id, data) {
    const task = await taskApi.update(id, data)
    const index = tasks.value.findIndex(t => t.id === id)
    if (index !== -1) {
      tasks.value[index] = { ...tasks.value[index], ...task }
    }
    if (currentTask.value?.id === id) {
      currentTask.value = { ...currentTask.value, ...task }
    }
    return task
  }

  async function deleteTask(id) {
    await taskApi.delete(id)
    tasks.value = tasks.value.filter(t => t.id !== id)
  }

  async function delayTask(id, reason, newPlanEndDate) {
    const result = await taskApi.delay(id, { reason, newPlanEndDate })
    const index = tasks.value.findIndex(t => t.id === id)
    if (index !== -1) {
      tasks.value[index] = { ...tasks.value[index], ...result.task }
    }
    return result
  }

  async function addExtraRequirement(id, description) {
    const result = await taskApi.addExtra(id, { description })
    const index = tasks.value.findIndex(t => t.id === id)
    if (index !== -1) {
      tasks.value[index] = { ...tasks.value[index], ...result.task }
    }
    return result
  }

  function clearTasks() {
    tasks.value = []
    currentTask.value = null
  }

  return {
    tasks,
    currentTask,
    loading,
    fetchTasksByProject,
    fetchTask,
    createTask,
    updateTask,
    deleteTask,
    delayTask,
    addExtraRequirement,
    clearTasks
  }
})
