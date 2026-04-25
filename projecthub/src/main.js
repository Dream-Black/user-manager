import { createApp } from 'vue'
import { createPinia } from 'pinia'
import router from './router'
import TDesign from 'tdesign-vue-next'
import TDesignChat from '@tdesign-vue-next/chat'
import App from './App.vue'
import 'tdesign-vue-next/es/style/index.css'
import '@tdesign-vue-next/chat/es/style/index.css'

// 导入自定义设计系统
import './styles/design-system.css'
import './styles/tdesign-overrides.css'
import { remoteLogger } from './services/logger'

if (import.meta.env.DEV) {
  remoteLogger.install()
}

const app = createApp(App)
const pinia = createPinia()

app.use(pinia)
app.use(router)
app.use(TDesign)
app.use(TDesignChat)
app.mount('#app')
