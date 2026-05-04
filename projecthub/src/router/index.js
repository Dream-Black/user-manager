import { createRouter, createWebHistory } from 'vue-router'
import Dashboard from '../views/Dashboard.vue'
import ProjectList from '../views/projects/ProjectList.vue'
import ProjectDetail from '../views/projects/ProjectDetail.vue'
import TaskList from '../views/tasks/TaskList.vue'
import GanttView from '../views/gantt/GanttView.vue'
import AiView from '../views/ai-refactor/AiView.vue'
import SettingsView from '../views/settings/SettingsView.vue'
import LoginView from '../views/LoginView.vue'

const routes = [
  { path: '/login', name: 'Login', component: LoginView, meta: { layout: 'blank', title: '登录' } },
  { path: '/review', redirect: '/' },
  { path: '/review/:pathMatch(.*)*', redirect: '/' },
  { path: '/timeline', redirect: '/' },
  { path: '/timeline/:pathMatch(.*)*', redirect: '/' },
  {
    path: '/',
    name: 'Dashboard',
    component: Dashboard,
    meta: { title: '工作台', requiresAuth: true }
  },
  {
    path: '/projects',
    name: 'ProjectList',
    component: ProjectList,
    meta: { title: '项目列表', requiresAuth: true }
  },
  {
    path: '/projects/:id',
    name: 'ProjectDetail',
    component: ProjectDetail,
    meta: { title: '项目详情', requiresAuth: true }
  },
  {
    path: '/tasks',
    name: 'Tasks',
    component: TaskList,
    meta: { title: '任务管理', requiresAuth: true }
  },
  {
    path: '/gantt',
    name: 'Gantt',
    component: GanttView,
    meta: { title: '甘特图', requiresAuth: true }
  },
  {
    path: '/ai',
    name: 'AI',
    component: AiView,
    meta: { title: 'AI 助手', requiresAuth: true }
  },
  {
    path: '/settings',
    name: 'Settings',
    component: SettingsView,
    meta: { title: '个人设置', requiresAuth: true }
  },
  {
    path: '/resources',
    name: 'Resources',
    component: () => import('../views/resources/ResourceList.vue'),
    meta: { title: '资源管理', requiresAuth: true }
  },
  {
    path: '/resources/comics/:id',
    name: 'ComicReader',
    component: () => import('../views/resources/ComicReader.vue'),
    meta: { title: '漫画阅读', requiresAuth: true }
  }
]

const router = createRouter({
  history: createWebHistory(),
  routes
})

router.beforeEach((to, from, next) => {
  const token = localStorage.getItem('token')
  const expiresAt = Number(localStorage.getItem('tokenExpiresAt') || 0)
  const isLoggedIn = Boolean(token) && (!expiresAt || Date.now() <= expiresAt)

  if (to.meta.requiresAuth && !isLoggedIn) {
    next({ path: '/login', query: { redirect: to.fullPath } })
    return
  }

  if (to.path === '/login' && isLoggedIn) {
    next('/')
    return
  }

  next()
})

export default router
