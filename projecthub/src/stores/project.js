import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { projectApi, taskApi } from '@/api'

export const useProjectStore = defineStore('project', () => {
  const projects = ref([])
  const currentProject = ref(null)
  const loading = ref(false)
  const filter = ref({ type: 'all', status: 'all', search: '' })

  const filteredProjects = computed(() => {
    return projects.value.filter(p => {
      if (filter.value.type !== 'all' && p.type !== filter.value.type) return false
      if (filter.value.status !== 'all' && p.status !== filter.value.status) return false
      if (filter.value.search && !p.name.toLowerCase().includes(filter.value.search.toLowerCase())) return false
      return true
    })
  })

  const stats = computed(() => ({
    total: projects.value.length,
    active: projects.value.filter(p => p.status === 'active').length,
    completed: projects.value.filter(p => p.status === 'completed').length
  }))

  async function fetchProjects() {
    loading.value = true
    try {
      const data = await projectApi.list()
      projects.value = data
    } finally {
      loading.value = false
    }
  }

  async function fetchProject(id) {
    loading.value = true
    try {
      currentProject.value = await projectApi.get(id)
      return currentProject.value
    } finally {
      loading.value = false
    }
  }

  async function createProject(data) {
    const project = await projectApi.create(data)
    projects.value.unshift(project)
    return project
  }

  async function updateProject(id, data) {
    const project = await projectApi.update(id, data)
    const index = projects.value.findIndex(p => p.id === id)
    if (index !== -1) {
      projects.value[index] = { ...projects.value[index], ...project }
    }
    if (currentProject.value?.id === id) {
      currentProject.value = { ...currentProject.value, ...project }
    }
    return project
  }

  async function deleteProject(id) {
    await projectApi.delete(id)
    projects.value = projects.value.filter(p => p.id !== id)
    if (currentProject.value?.id === id) {
      currentProject.value = null
    }
  }

  function setFilter(newFilter) {
    filter.value = { ...filter.value, ...newFilter }
  }

  return {
    projects,
    currentProject,
    loading,
    filter,
    filteredProjects,
    stats,
    fetchProjects,
    fetchProject,
    createProject,
    updateProject,
    deleteProject,
    setFilter
  }
})
