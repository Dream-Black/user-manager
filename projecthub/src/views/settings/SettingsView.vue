<template>
  <div class="settings-page">
    <div class="page-header">
      <div class="header-content">
        <h1 class="page-title">个人设置</h1>
        <p class="page-subtitle">管理您的账号偏好和系统配置</p>
      </div>
    </div>

    <div class="settings-container">
      <!-- 侧边导航 -->
      <div class="settings-nav">
        <div
          v-for="section in settingsSections"
          :key="section.key"
          class="nav-item"
          :class="{ active: activeSection === section.key }"
          @click="activeSection = section.key"
        >
          <component :is="section.icon" />
          <span>{{ section.label }}</span>
        </div>
      </div>

      <!-- 设置内容 -->
      <div class="settings-content">
        <!-- 个人信息 -->
        <div v-if="activeSection === 'profile'" class="settings-section">
          <h2 class="section-title">个人信息</h2>
          
          <!-- 头像上传 -->
          <div class="avatar-upload">
            <div class="avatar-preview" :style="avatarStyle">
              <img v-if="profileForm.avatar" :src="profileForm.avatar" alt="头像" />
              <span v-else>{{ profileForm.name?.charAt(0) || 'U' }}</span>
            </div>
            <div class="avatar-actions">
              <t-button variant="outline" size="small" @click="showAvatarCropper = true">
                <template #icon><UploadIcon /></template>
                更换头像
              </t-button>
              <p class="avatar-hint">支持 JPG、PNG，裁剪为 100×100 像素</p>
            </div>
          </div>

          <t-form ref="profileFormRef" :data="profileForm" :rules="profileRules" label-align="top">
            <t-form-item label="姓名" name="name">
              <t-input v-model="profileForm.name" placeholder="请输入姓名" />
            </t-form-item>
            <t-form-item label="邮箱" name="email">
              <t-input v-model="profileForm.email" placeholder="请输入邮箱" />
            </t-form-item>
            <t-form-item label="手机号" name="phone">
              <t-input v-model="profileForm.phone" placeholder="请输入手机号" />
            </t-form-item>
            <t-form-item label="部门" name="department">
              <t-select v-model="profileForm.department">
                <t-option value="tech" label="技术部" />
                <t-option value="product" label="产品部" />
                <t-option value="design" label="设计部" />
                <t-option value="operation" label="运营部" />
              </t-select>
            </t-form-item>
            <t-form-item label="职位" name="role">
              <t-input v-model="profileForm.role" placeholder="请输入职位" />
            </t-form-item>
          </t-form>

          <div class="section-actions">
            <t-button theme="primary" :loading="saving" @click="handleSaveProfile">保存修改</t-button>
          </div>
        </div>

        <!-- 账号安全 -->
        <div v-if="activeSection === 'security'" class="settings-section">
          <h2 class="section-title">账号安全</h2>
          <div class="security-item">
            <div class="security-info">
              <h4>登录密码</h4>
              <p>已设置密码登录，建议定期更换</p>
            </div>
            <t-button variant="outline" size="small">修改密码</t-button>
          </div>
          <div class="security-item">
            <div class="security-info">
              <h4>两步验证</h4>
              <p>启用后登录需要输入手机验证码</p>
            </div>
            <t-switch v-model="securityForm.twoFactor" @change="handleSecurityChange" />
          </div>
          <div class="security-item">
            <div class="security-info">
              <h4>登录通知</h4>
              <p>在新设备登录时发送邮件通知</p>
            </div>
            <t-switch v-model="securityForm.loginNotify" @change="handleSecurityChange" />
          </div>
        </div>

        <!-- 通知设置 -->
        <div v-if="activeSection === 'notifications'" class="settings-section">
          <h2 class="section-title">通知设置</h2>
          <div class="notification-group">
            <h4>任务通知</h4>
            <div class="notification-item">
              <div class="notification-info">
                <span class="notification-label">任务到期提醒</span>
                <span class="notification-desc">任务到期前发送通知</span>
              </div>
              <t-switch v-model="notificationForm.taskReminder" @change="handleNotificationChange" />
            </div>
            <div class="notification-item">
              <div class="notification-info">
                <span class="notification-label">任务分配通知</span>
                <span class="notification-desc">被分配新任务时发送通知</span>
              </div>
              <t-switch v-model="notificationForm.taskAssign" @change="handleNotificationChange" />
            </div>
          </div>
          <div class="notification-group">
            <h4>项目通知</h4>
            <div class="notification-item">
              <div class="notification-info">
                <span class="notification-label">项目更新通知</span>
                <span class="notification-desc">项目有新动态时发送通知</span>
              </div>
              <t-switch v-model="notificationForm.projectUpdate" @change="handleNotificationChange" />
            </div>
            <div class="notification-item">
              <div class="notification-info">
                <span class="notification-label">项目周报</span>
                <span class="notification-desc">每周一发送项目周报</span>
              </div>
              <t-switch v-model="notificationForm.weeklyReport" @change="handleNotificationChange" />
            </div>
          </div>
        </div>

        <!-- 外观设置 -->
        <div v-if="activeSection === 'appearance'" class="settings-section">
          <h2 class="section-title">外观设置</h2>
          <div class="theme-setting">
            <h4>主题模式</h4>
            <div class="theme-options">
              <div
                v-for="theme in themes"
                :key="theme.key"
                class="theme-option"
                :class="{ active: appearanceForm.theme === theme.key }"
                @click="setTheme(theme.key)"
              >
                <div class="theme-preview" :style="{ background: theme.preview }">
                  <CheckIcon v-if="appearanceForm.theme === theme.key" class="check-icon" />
                </div>
                <span>{{ theme.label }}</span>
              </div>
            </div>
          </div>
          <div class="density-setting">
            <h4>界面密度</h4>
            <t-radio-group v-model="appearanceForm.density" @change="handleAppearanceChange">
              <t-radio-button value="compact">紧凑</t-radio-button>
              <t-radio-button value="normal">默认</t-radio-button>
              <t-radio-button value="comfortable">宽松</t-radio-button>
            </t-radio-group>
          </div>
        </div>

        <!-- AI 设置 -->
        <div v-if="activeSection === 'ai'" class="settings-section">
          <h2 class="section-title">AI 助手设置</h2>
          <p class="section-desc">配置 DeepSeek API，启用 AI 助手功能（数据将加密存储）</p>

          <t-form :data="aiForm" label-align="top" class="ai-settings-form">
            <t-form-item label="DeepSeek API Key">
              <t-input
                v-model="aiForm.deepSeekApiKey"
                type="password"
                placeholder="sk-..."
                clearable
                :maxlength="200"
              />
              <template #help>
                <span class="form-help">在 <a href="https://platform.deepseek.com" target="_blank">DeepSeek 开放平台</a> 获取 API Key</span>
              </template>
            </t-form-item>

            <t-form-item label="默认模型">
              <t-radio-group v-model="aiForm.deepSeekModel">
                <t-radio-button value="deepseek-chat">deepseek-chat（快速响应）</t-radio-button>
                <t-radio-button value="deepseek-reasoner">deepseek-reasoner（深度思考）</t-radio-button>
              </t-radio-group>
            </t-form-item>
          </t-form>

          <div class="section-actions">
            <t-button theme="primary" :loading="aiSaving" @click="handleAiSettingsSave">
              保存设置
            </t-button>
          </div>
        </div>
      </div>
    </div>

    <!-- 头像裁剪弹窗 -->
    <AvatarCropper v-model="showAvatarCropper" :output-size="100" @confirm="handleAvatarCrop" />
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { MessagePlugin } from 'tdesign-vue-next'
import { markRaw } from 'vue'
import AvatarCropper from '@/components/settings/AvatarCropper.vue'
import { userService, userSettingsService, aiService } from '@/services/dataService'

const activeSection = ref('profile')
const profileFormRef = ref(null)
const showAvatarCropper = ref(false)
const saving = ref(false)
const aiSaving = ref(false)

const settingsSections = [
  { key: 'profile', label: '个人信息', icon: markRaw(UserIcon) },
  { key: 'security', label: '账号安全', icon: markRaw(LockOnIcon) },
  { key: 'notifications', label: '通知设置', icon: markRaw(NotificationIcon) },
  { key: 'appearance', label: '外观设置', icon: markRaw(PaletteIcon) },
  { key: 'ai', label: 'AI 设置', icon: markRaw(RobotIcon) }
]

const themes = [
  { key: 'light', label: '浅色', preview: 'linear-gradient(135deg, #fff 50%, #f5f7fa 50%)' },
  { key: 'dark', label: '深色', preview: 'linear-gradient(135deg, #1f2937 50%, #111827 50%)' },
  { key: 'auto', label: '自动', preview: 'linear-gradient(135deg, #fff 50%, #1f2937 50%)' }
]

// 头像样式
const avatarStyle = computed(() => {
  if (profileForm.value.avatar) {
    return {
      background: 'transparent',
      color: 'transparent'
    }
  }
  return {}
})

// 表单数据
const profileForm = ref({
  name: '',
  email: '',
  phone: '',
  department: '',
  role: '',
  avatar: ''
})

const profileRules = {
  name: [{ required: true, message: '请输入姓名', trigger: 'blur' }],
  email: [
    { required: true, message: '请输入邮箱', trigger: 'blur' },
    { type: 'email', message: '请输入正确的邮箱格式', trigger: 'blur' }
  ]
}

const securityForm = ref({
  twoFactor: false,
  loginNotify: true
})

const notificationForm = ref({
  taskReminder: true,
  taskAssign: true,
  projectUpdate: true,
  weeklyReport: false
})

const appearanceForm = ref({
  theme: 'light',
  density: 'normal'
})

const aiForm = ref({
  deepSeekApiKey: '',
  deepSeekModel: 'deepseek-chat'
})

// 监听系统主题变化
const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)')

const updateSystemTheme = () => {
  if (appearanceForm.value.theme === 'auto') {
    const isDark = mediaQuery.matches
    document.documentElement.setAttribute('data-theme', isDark ? 'dark' : 'light')
  }
}

mediaQuery.addEventListener('change', updateSystemTheme)

// 加载用户信息
const loadUserProfile = async () => {
  try {
    const user = await userService.getCurrent()
    profileForm.value = {
      name: user.name || '',
      email: user.email || '',
      phone: user.phone || '',
      department: user.department || '',
      role: user.role || '',
      avatar: user.avatar || ''
    }
  } catch (error) {
    console.error('加载用户信息失败:', error)
    // 使用默认数据
    profileForm.value = {
      name: 'ProjectHub User',
      email: 'user@projecthub.com',
      phone: '138****8888',
      department: 'tech',
      role: '项目经理',
      avatar: ''
    }
  }
}

// 保存个人信息
const handleSaveProfile = async () => {
  const result = await profileFormRef.value.validate()
  if (result !== true) return

  saving.value = true
  try {
    await userService.update({
      name: profileForm.value.name,
      email: profileForm.value.email,
      phone: profileForm.value.phone,
      department: profileForm.value.department,
      role: profileForm.value.role
    })
    MessagePlugin.success('个人信息已保存')
  } catch (error) {
    console.error('保存失败:', error)
    MessagePlugin.error('保存失败，请重试')
  } finally {
    saving.value = false
  }
}

// 头像裁剪完成
const handleAvatarCrop = async ({ base64 }) => {
  try {
    await userService.uploadAvatar(base64)
    profileForm.value.avatar = base64
    MessagePlugin.success('头像已更新')
  } catch (error) {
    console.error('上传头像失败:', error)
    MessagePlugin.error('上传失败，请重试')
  }
}

// 设置主题
const setTheme = async (theme) => {
  appearanceForm.value.theme = theme
  
  // 应用主题到 DOM
  if (theme === 'auto') {
    const isDark = window.matchMedia('(prefers-color-scheme: dark)').matches
    document.documentElement.setAttribute('data-theme', isDark ? 'dark' : 'light')
  } else {
    document.documentElement.setAttribute('data-theme', theme)
  }
  
  // 保存到本地
  localStorage.setItem('theme', theme)
  
  // 保存到后端
  await handleAppearanceChange()
}

// 设置变更处理
const handleSecurityChange = () => {
  // TODO: 调用API保存安全设置
  MessagePlugin.success('安全设置已保存')
}

const handleNotificationChange = () => {
  // TODO: 调用API保存通知设置
  MessagePlugin.success('通知设置已保存')
}

const handleAppearanceChange = async () => {
  try {
    // 保存到后端
    await userSettingsService.update({
      theme: appearanceForm.value.theme,
      density: appearanceForm.value.density
    })
    MessagePlugin.success('外观设置已保存')
  } catch (error) {
    console.error('保存外观设置失败:', error)
    // 即使保存失败，本地已生效
  }
}

// 图标
import { markRaw as vueMarkRaw } from 'vue'
import { UserIcon, LockOnIcon, NotificationIcon, PaletteIcon, CheckIcon, UploadIcon, RobotIcon } from 'tdesign-icons-vue-next'

// 初始化主题
const initTheme = () => {
  const savedTheme = localStorage.getItem('theme')
  if (savedTheme) {
    appearanceForm.value.theme = savedTheme
  }
  applyTheme(appearanceForm.value.theme)
}

onMounted(async () => {
  await loadUserProfile()
  initTheme()
  await loadUserSettings()
  await loadAiSettings()
})

// 应用主题
const applyTheme = (theme) => {
  if (theme === 'auto') {
    const isDark = window.matchMedia('(prefers-color-scheme: dark)').matches
    document.documentElement.setAttribute('data-theme', isDark ? 'dark' : 'light')
  } else {
    document.documentElement.setAttribute('data-theme', theme)
  }
}

// 加载用户设置
const loadUserSettings = async () => {
  try {
    const settings = await userSettingsService.get()
    if (settings.theme) {
      appearanceForm.value.theme = settings.theme
      localStorage.setItem('theme', settings.theme)
      applyTheme(settings.theme)
    }
    if (settings.density) {
      appearanceForm.value.density = settings.density
    }
  } catch (error) {
    console.error('加载用户设置失败:', error)
  }
}

// 加载 AI 设置
const loadAiSettings = async () => {
  try {
    const settings = await aiService.getSettings()
    aiForm.value.deepSeekApiKey = settings.deepSeekApiKey || ''
    aiForm.value.deepSeekModel = settings.deepSeekModel || 'deepseek-chat'
  } catch (error) {
    console.error('加载 AI 设置失败:', error)
  }
}

// 保存 AI 设置
const handleAiSettingsSave = async () => {
  aiSaving.value = true
  try {
    await aiService.updateSettings({
      deepSeekApiKey: aiForm.value.deepSeekApiKey,
      deepSeekModel: aiForm.value.deepSeekModel
    })
    MessagePlugin.success('AI 设置已保存')
  } catch (error) {
    console.error('保存 AI 设置失败:', error)
    MessagePlugin.error('保存失败，请重试')
  } finally {
    aiSaving.value = false
  }
}
</script>

<style scoped>
.settings-page { max-width: 1200px; margin: 0 auto; }
.page-header { margin-bottom: 24px; animation: fadeInUp 0.5s ease; }
.header-content { display: flex; flex-direction: column; gap: 4px; }
.page-title { font-size: 28px; font-weight: 700; color: var(--text-primary); margin: 0; }
.page-subtitle { font-size: 14px; color: var(--text-secondary); margin: 0; }

.settings-container { display: flex; gap: 24px; background: var(--bg-card-solid); border-radius: var(--radius-xl); border: 1px solid var(--border-light); overflow: hidden; animation: fadeInUp 0.5s ease 0.1s backwards; box-shadow: var(--shadow-card); }

.settings-nav { width: 220px; padding: 20px; border-right: 1px solid var(--border-light); flex-shrink: 0; background: var(--bg-color-secondary); }
.nav-item { display: flex; align-items: center; gap: 12px; padding: 12px 16px; border-radius: var(--radius-lg); cursor: pointer; color: var(--text-secondary); font-size: 14px; transition: all var(--transition-fast); margin-bottom: 4px; }
.nav-item:hover { background: var(--primary-lighter); color: var(--primary-color); }
.nav-item.active { background: var(--primary-lighter); color: var(--primary-color); font-weight: 600; }
.nav-item svg { width: 18px; height: 18px; }

.settings-content { flex: 1; padding: 32px; overflow-y: auto; }
.settings-section { max-width: 600px; }
.section-title { font-size: 18px; font-weight: 600; color: var(--text-primary); margin: 0 0 24px 0; }

.avatar-upload { display: flex; align-items: center; gap: 20px; margin-bottom: 24px; }
.avatar-preview { width: 80px; height: 80px; border-radius: var(--radius-full); background: var(--gradient-primary); display: flex; align-items: center; justify-content: center; color: white; font-size: 28px; font-weight: 600; box-shadow: var(--shadow-glow); overflow: hidden; flex-shrink: 0; }
.avatar-preview img { width: 100%; height: 100%; object-fit: cover; }
.avatar-actions { display: flex; flex-direction: column; gap: 4px; }
.avatar-hint { font-size: 12px; color: var(--text-tertiary); margin: 0; }

.section-actions { margin-top: 24px; padding-top: 24px; border-top: 1px solid var(--border-light); }

.security-item { display: flex; justify-content: space-between; align-items: center; padding: 16px 0; border-bottom: 1px solid var(--border-light); }
.security-info h4 { font-size: 14px; font-weight: 500; color: var(--text-primary); margin: 0 0 4px 0; }
.security-info p { font-size: 12px; color: var(--text-tertiary); margin: 0; }

.notification-group { margin-bottom: 24px; }
.notification-group h4 { font-size: 14px; font-weight: 500; color: var(--text-primary); margin: 0 0 16px 0; }
.notification-item { display: flex; justify-content: space-between; align-items: center; padding: 12px 0; border-bottom: 1px solid var(--border-light); }
.notification-label { font-size: 14px; color: var(--text-primary); }
.notification-desc { font-size: 12px; color: var(--text-tertiary); display: block; }

.theme-setting { margin-bottom: 24px; }
.theme-setting h4 { font-size: 14px; font-weight: 500; color: var(--text-primary); margin: 0 0 16px 0; }
.theme-options { display: flex; gap: 12px; }
.theme-option { cursor: pointer; text-align: center; }
.theme-option.active span { color: var(--primary-color); font-weight: 500; }
.theme-preview { width: 80px; height: 56px; border-radius: var(--radius-lg); border: 2px solid var(--border-light); display: flex; align-items: center; justify-content: center; margin-bottom: 8px; transition: all var(--transition-fast); }
.theme-option.active .theme-preview { border-color: var(--primary-color); border-width: 3px; }
.check-icon { color: white; }
.theme-option span { font-size: 12px; color: var(--text-secondary); }

.density-setting h4 { font-size: 14px; font-weight: 500; color: var(--text-primary); margin: 0 0 16px 0; }

.section-desc { font-size: 13px; color: var(--text-secondary); margin: 0 0 20px 0; }
.form-help { font-size: 12px; color: var(--text-tertiary); }
.form-help a { color: var(--primary-color); }
.ai-settings-form { max-width: 500px; }
</style>
