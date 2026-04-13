import { createApp } from 'vue'
import { createPinia } from 'pinia'
import router from './router'
import TDesign from 'tdesign-vue-next'
import App from './App.vue'
import 'tdesign-vue-next/es/style/index.css'

// 导入自定义设计系统
import './styles/design-system.css'
import './styles/tdesign-overrides.css'

const app = createApp(App)
const pinia = createPinia()

app.use(pinia)
app.use(router)
app.use(TDesign)
app.mount('#app')
