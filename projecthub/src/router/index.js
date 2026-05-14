import { createRouter, createWebHistory } from 'vue-router'
import Dashboard from '../views/Dashboard.vue'
import ProjectList from '../views/projects/ProjectList.vue'
import ProjectDetail from '../views/projects/ProjectDetail.vue'
import TaskList from '../views/tasks/TaskList.vue'
import GanttView from '../views/gantt/GanttView.vue'
import ScheduleManagementView from '../views/daily/ScheduleManagementView.vue'
import ScheduleDetailView from '../views/daily/ScheduleDetailView.vue'
import AiView from '../views/ai-refactor/AiView.vue'
import SettingsView from '../views/settings/SettingsView.vue'
import LoginView from '../views/LoginView.vue'
import NotesView from '../views/notes/NotesView.vue'
import NoteDetailView from '../views/notes/NoteDetailView.vue'

// 财务模块视图
import FinanceDashboard from '../views/finance/FinanceDashboard.vue'
import ExpenseList from '../views/finance/ExpenseList.vue'
import ExpenseDetail from '../views/finance/ExpenseDetail.vue'
import IncomeList from '../views/finance/IncomeList.vue'
import SalaryManage from '../views/finance/SalaryManage.vue'
import AccountManage from '../views/finance/AccountManage.vue'
import StatsReport from '../views/finance/StatsReport.vue'
import ProductPriceTrend from '../views/finance/ProductPriceTrend.vue'
import InvestmentPlaceholder from '../views/finance/InvestmentPlaceholder.vue'

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
    path: '/schedule',
    name: 'Schedule',
    component: ScheduleManagementView,
    meta: { title: '日程管理', requiresAuth: true }
  },
  {
    path: '/schedule/:id',
    name: 'ScheduleDetail',
    component: ScheduleDetailView,
    meta: { title: '日程详情', requiresAuth: true }
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
    path: '/notes',
    name: 'Notes',
    component: NotesView,
    meta: { title: '笔记', requiresAuth: true }
  },
  {
    path: '/notes/:id',
    name: 'NoteDetail',
    component: NoteDetailView,
    meta: { title: '笔记详情', requiresAuth: true }
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
  },
  // 财务模块路由（扁平化结构）
  {
    path: '/finance-manager/overview',
    name: 'FinanceDashboard',
    component: FinanceDashboard,
    meta: { title: '财务概览', requiresAuth: true }
  },
  {
    path: '/finance-manager/expenses',
    name: 'ExpenseList',
    component: ExpenseList,
    meta: { title: '支出记录', requiresAuth: true }
  },
  {
    path: '/finance-manager/expenses/:id',
    name: 'ExpenseDetail',
    component: ExpenseDetail,
    meta: { title: '支出详情', requiresAuth: true }
  },
  {
    path: '/finance-manager/income',
    name: 'IncomeList',
    component: IncomeList,
    meta: { title: '收入记录', requiresAuth: true }
  },
  {
    path: '/finance-manager/salary',
    name: 'SalaryManage',
    component: SalaryManage,
    meta: { title: '工资管理', requiresAuth: true }
  },
  {
    path: '/finance-manager/accounts',
    name: 'AccountManage',
    component: AccountManage,
    meta: { title: '账户管理', requiresAuth: true }
  },
  {
    path: '/finance-manager/stats',
    name: 'StatsReport',
    component: StatsReport,
    meta: { title: '统计报表', requiresAuth: true }
  },
  {
    path: '/finance-manager/product/:name',
    name: 'ProductPriceTrend',
    component: ProductPriceTrend,
    meta: { title: '商品价格趋势', requiresAuth: true }
  },
  {
    path: '/finance-manager/investment',
    name: 'InvestmentPlaceholder',
    component: InvestmentPlaceholder,
    meta: { title: '理财', requiresAuth: true }
  },
  {
    path: '/finance',
    redirect: '/finance-manager/overview'
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
