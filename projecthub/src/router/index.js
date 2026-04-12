import { createRouter, createWebHistory } from 'vue-router'
import Dashboard from '../views/Dashboard.vue'
import ProjectList from '../views/projects/ProjectList.vue'
import ProjectDetail from '../views/projects/ProjectDetail.vue'
import TaskList from '../views/tasks/TaskList.vue'
import GanttView from '../views/gantt/GanttView.vue'
import TimelineView from '../views/timeline/TimelineView.vue'
import ReviewList from '../views/review/ReviewList.vue'
import CategoriesView from '../views/categories/CategoriesView.vue'
import AiView from '../views/ai/AiView.vue'
import SettingsView from '../views/settings/SettingsView.vue'

const routes = [
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
    path: '/timeline',
    name: 'Timeline',
    component: TimelineView,
    meta: { title: '时间线' }
  },
  {
    path: '/review',
    name: 'Review',
    component: ReviewList,
    meta: { title: '复盘总结' }
  },
  {
    path: '/categories',
    name: 'Categories',
    component: CategoriesView,
    meta: { title: '分类管理' }
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
  }
]

const router = createRouter({
  history: createWebHistory(),
  routes
})

export default router
