<template>
  <div class="login-page">
    <ThreeBackground />
    <div class="login-container">
      <!-- Logo + 标题区域 -->
      <div class="brand-section">
        <div class="logo-wrapper">
          <div class="logo-icon">
            <svg viewBox="0 0 40 40" fill="none" xmlns="http://www.w3.org/2000/svg">
              <path d="M20 4L36 12V28L20 36L4 28V12L20 4Z" stroke="url(#logoGrad)" stroke-width="2" fill="none"/>
              <path d="M20 12L28 16V24L20 28L12 24V16L20 12Z" fill="url(#logoGrad)" opacity="0.6"/>
              <defs>
                <linearGradient id="logoGrad" x1="4" y1="4" x2="36" y2="36">
                  <stop offset="0%" stop-color="#4a90d9"/>
                  <stop offset="100%" stop-color="#7B61FF"/>
                </linearGradient>
              </defs>
            </svg>
          </div>
          <span class="brand-name">Dream Hub</span>
        </div>
        <h1 class="hero-title">欢迎回来</h1>
        <p class="hero-subtitle">登录以继续你的工作空间</p>
      </div>

      <!-- 登录表单卡片 -->
      <div class="form-card">
        <t-form :data="form" class="login-form" @submit.prevent="handleLogin" label-align="top">
          <t-form-item name="email">
            <t-input
              v-model="form.email"
              placeholder="邮箱"
              clearable
              size="large"
            >
              <template #prefix-icon>
                <t-icon name="mail" />
              </template>
            </t-input>
          </t-form-item>

          <t-form-item name="password">
            <t-input
              v-model="form.password"
              type="password"
              placeholder="密码"
              clearable
              size="large"
              @enter="handleLogin"
            >
              <template #prefix-icon>
                <t-icon name="lock-on" />
              </template>
            </t-input>
          </t-form-item>

          <div v-if="errorMessage" class="error-message">
            <t-icon name="error-circle" />
            {{ errorMessage }}
          </div>

          <t-button
            theme="primary"
            block
            size="large"
            :loading="loading"
            @click="handleLogin"
            class="submit-btn"
          >
            登录
          </t-button>
        </t-form>
      </div>

      <!-- 底部信息 -->
      <div class="footer-info">
        <span>痴 · 梦 · 个 · 人 · 管 · 理 · 系 · 统</span>
      </div>
    </div>
  </div>
</template>

<script setup>
import { reactive, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import JSEncrypt from 'jsencrypt'
import { authService } from '../services/authService'
import { setCurrentUser } from '../composables/useLayoutState'
import ThreeBackground from '../components/ThreeBackground.vue'

const router = useRouter()
const route = useRoute()
const loading = ref(false)
const errorMessage = ref('')
const form = reactive({ email: '', password: '' })

const encryptPassword = async (password) => {
  const keyResponse = await authService.getPublicKey()
  const publicKeyPem = keyResponse.publicKey || keyResponse
  const encryptor = new JSEncrypt()
  encryptor.setPublicKey(publicKeyPem)
  const encrypted = encryptor.encrypt(password)

  if (!encrypted) {
    throw new Error('密码加密失败')
  }

  return encrypted
}

const handleLogin = async () => {
  if (!form.email || !form.password) {
    errorMessage.value = '请输入邮箱和密码'
    return
  }

  loading.value = true
  errorMessage.value = ''
  try {
    const encryptedPassword = await encryptPassword(form.password)
    const res = await authService.login(form.email, encryptedPassword)
    localStorage.setItem('token', res.token)
    localStorage.setItem('tokenExpiresAt', String(Date.now() + 2 * 60 * 60 * 1000))
    const normalizedUser = {
      id: res.user.id ?? res.user.Id,
      name: res.user.name ?? res.user.Name,
      email: res.user.email ?? res.user.Email,
      phone: res.user.phone ?? res.user.Phone,
      department: res.user.department ?? res.user.Department,
      role: res.user.role ?? res.user.Role,
      avatar: res.user.avatar ?? res.user.Avatar,
      theme: res.user.theme ?? res.user.Theme,
      density: res.user.density ?? res.user.Density
    }
    localStorage.setItem('user', JSON.stringify(normalizedUser))
    setCurrentUser(normalizedUser)
    const redirect = route.query.redirect || '/'
    await router.push(redirect)
  } catch (error) {
    errorMessage.value = error?.response?.data?.message || '登录失败，请检查邮箱和密码'
  } finally {
    loading.value = false
  }
}
</script>

<style scoped>
/* ========== 页面容器 ========== */
.login-page {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 24px;
  position: relative;
  font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
}

/* ========== 主容器 ========== */
.login-container {
  width: 100%;
  max-width: 400px;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 32px;
  animation: fadeIn 0.6s ease-out;
  position: relative;
  z-index: 1;
}

@keyframes fadeIn {
  from {
    opacity: 0;
    transform: translateY(10px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

/* ========== 品牌区域 ========== */
.brand-section {
  text-align: center;
}

.logo-wrapper {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 10px;
  margin-bottom: 20px;
}

.logo-icon {
  width: 36px;
  height: 36px;
}

.logo-icon svg {
  width: 100%;
  height: 100%;
  filter: drop-shadow(0 2px 4px rgba(74, 144, 217, 0.2));
}

.brand-name {
  font-size: 22px;
  font-weight: 700;
  background: linear-gradient(135deg, #4a90d9 0%, #7B61FF 100%);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
  letter-spacing: 0.02em;
}

.hero-title {
  font-size: 28px;
  font-weight: 700;
  color: #1a202c;
  margin: 0 0 8px 0;
  letter-spacing: -0.02em;
}

.hero-subtitle {
  font-size: 15px;
  color: #718096;
  margin: 0;
}

/* ========== 表单卡片 ========== */
.form-card {
  width: 100%;
  padding: 36px;
  background: rgba(255, 255, 255, 0.85);
  backdrop-filter: blur(10px);
  -webkit-backdrop-filter: blur(10px);
  border: 1px solid rgba(255, 255, 255, 0.6);
  border-radius: 16px;
  box-shadow:
    0 8px 32px rgba(74, 144, 217, 0.08),
    0 2px 8px rgba(0, 0, 0, 0.04);
}

/* 隐藏表单项标签 */
:deep(.t-form__label) {
  display: none !important;
}

/* 表单项间距 */
:deep(.t-form__item) {
  margin-bottom: 16px;
}

/* 输入框样式 */
:deep(.t-input) {
  background: #f7f8fa !important;
  border: 1.5px solid #e2e8f0 !important;
  border-radius: 10px !important;
  transition: all 0.2s ease;
  height: 48px !important;
}

:deep(.t-input:hover) {
  border-color: #cbd5e0 !important;
}

:deep(.t-input--focused) {
  background: #ffffff !important;
  border-color: #4a90d9 !important;
  box-shadow: 0 0 0 3px rgba(74, 144, 217, 0.1) !important;
}

/* 处理浏览器自动填充样式 */
:deep(.t-input__inner:-webkit-autofill),
:deep(.t-input__inner:-webkit-autofill:hover),
:deep(.t-input__inner:-webkit-autofill:focus) {
  -webkit-box-shadow: 0 0 0 1000px #f7f8fa inset !important;
  -webkit-text-fill-color: #1a202c !important;
  border-color: #4a90d9 !important;
  transition: background-color 5000s ease-in-out 0s;
}

:deep(.t-input__inner) {
  color: #1a202c !important;
  font-size: 15px;
  height: 48px !important;
}

:deep(.t-input__inner::placeholder) {
  color: #a0aec0 !important;
}

:deep(.t-icon) {
  color: #a0aec0 !important;
}

/* ========== 错误提示 ========== */
.error-message {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 10px 14px;
  margin-bottom: 16px;
  background: #fff5f5;
  border: 1px solid #fed7d7;
  border-radius: 10px;
  color: #e53e3e;
  font-size: 14px;
}

.error-message :deep(.t-icon) {
  color: #e53e3e !important;
  flex-shrink: 0;
}

/* ========== 提交按钮 ========== */
.submit-btn {
  height: 48px !important;
  font-size: 16px !important;
  font-weight: 600 !important;
  border-radius: 10px !important;
  background: linear-gradient(135deg, #4a90d9 0%, #5a7bd5 100%) !important;
  border: none !important;
  color: #ffffff !important;
  transition: all 0.2s ease !important;
  margin-top: 32px !important;
}

.submit-btn:hover {
  background: linear-gradient(135deg, #3a80c9 0%, #4a6bc5 100%) !important;
  box-shadow: 0 4px 12px rgba(74, 144, 217, 0.3);
  transform: translateY(-1px);
}

.submit-btn:active {
  transform: translateY(0);
  box-shadow: none;
}

/* ========== 底部信息 ========== */
.footer-info {
  text-align: center;
  color: #a0aec0;
  font-size: 13px;
  letter-spacing: 0.02em;
}

/* ========== 响应式适配 ========== */
@media (max-width: 480px) {
  .login-container {
    gap: 24px;
  }

  .form-card {
    padding: 28px 24px;
    border-radius: 14px;
  }

  .hero-title {
    font-size: 24px;
  }

  .brand-name {
    font-size: 20px;
  }
}
</style>
