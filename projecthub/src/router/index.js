import { createRouter, createWebHistory } from 'vue-router'
import Dashboard from '../views/Dashboard.vue'
import ProjectList from '../views/projects/ProjectList.vue'
import ProjectDetail from '../views/projects/ProjectDetail.vue'
import TaskList from '../views/tasks/TaskList.vue'
import GanttView from '../views/gantt/GanttView.vue'
import AiView from '../views/ai/AiView.vue'
import SettingsView from '../views/settings/SettingsView.vue'

const routes = [
  { path: '/review', redirect: '/' },
  { path: '/review/:pathMatch(.*)*', redirect: '/' },
  { path: '/timeline', redirect: '/' },
  { path: '/timeline/:pathMatch(.*)*', redirect: '/' },
  {
    path: '/',
    name: 'Dashboard',
    component: Dashboard,
    meta: { title: '工作台' }
  },
  {
    path: '/projects',
    name: 'ProjectList',
    component: ProjectList,
    meta: { title: '项目列表' }
  },
  {
    path: '/projects/:id',
    name: 'ProjectDetail',
    component: ProjectDetail,
    meta: { title: '项目详情' }
  },
  {
    path: '/tasks',
    name: 'Tasks',
    component: TaskList,
    meta: { title: '任务管理' }
  },
  {
    path: '/gantt',
    name: 'Gantt',
    component: GanttView,
    meta: { title: '甘特图' }
  },
  {
    path: '/ai',
    name: 'AI',
    component: AiView,
    meta: { title: 'AI 助手' }
  },
  {
    path: '/settings',
    name: 'Settings',
    component: SettingsView,
    meta: { title: '个人设置' }
  },
  {
    path: '/resources',
    name: 'Resources',
    component: () => import('../views/resources/ResourceList.vue'),
    meta: { title: '资源管理' }
  },
  {
    path: '/resources/comics/:id',
    name: 'ComicReader',
    component: () => import('../views/resources/ComicReader.vue'),
    meta: { title: '漫画阅读' }
  }
]

const router = createRouter({
  history: createWebHistory(),
  routes
})

export default router
