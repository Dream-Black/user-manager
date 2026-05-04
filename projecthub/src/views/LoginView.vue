<template>
  <div class="login-page">
    <div class="login-panel">
      <div class="hero">
        <div class="brand-mark">ProjectHub</div>
        <h1>欢迎回来</h1>
        <p>使用邮箱和密码登录，继续你的工作流。</p>
        <ul>
          <li>2 小时登录有效期</li>
          <li>RSA 公钥加密传输</li>
          <li>自动保存登录状态</li>
        </ul>
      </div>

      <div class="form-card">
        <h2>登录账号</h2>
        <p class="subtext">请输入邮箱和密码</p>

        <t-form :data="form" class="login-form" @submit.prevent>
          <t-form-item label="邮箱" name="email">
            <t-input v-model="form.email" placeholder="name@example.com" clearable />
          </t-form-item>
          <t-form-item label="密码" name="password">
            <t-input v-model="form.password" type="password" placeholder="请输入密码" clearable />
          </t-form-item>
          <div v-if="errorMessage" class="error-banner">{{ errorMessage }}</div>
          <t-button theme="primary" block :loading="loading" @click="handleLogin">登录</t-button>
        </t-form>
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
.login-page {
  min-height: 100vh;
  display: grid;
  place-items: center;
  padding: 24px;
  background:
    radial-gradient(circle at top left, rgba(74, 144, 217, 0.18), transparent 36%),
    radial-gradient(circle at bottom right, rgba(103, 203, 171, 0.14), transparent 32%),
    linear-gradient(135deg, #f8fbff 0%, #eef4fb 100%);
}
.login-panel {
  width: min(980px, 100%);
  display: grid;
  grid-template-columns: 1.1fr 0.9fr;
  gap: 24px;
}
.hero,
.form-card {
  background: rgba(255, 255, 255, 0.9);
  backdrop-filter: blur(16px);
  border: 1px solid rgba(255, 255, 255, 0.7);
  border-radius: 24px;
  box-shadow: 0 24px 80px rgba(74, 144, 217, 0.12);
}
.hero { padding: 40px; }
.brand-mark {
  display: inline-flex;
  padding: 8px 14px;
  border-radius: 999px;
  background: #ebf4ff;
  color: #4a90d9;
  font-weight: 700;
  font-size: 13px;
  letter-spacing: 0.02em;
}
.hero h1 { margin: 18px 0 10px; font-size: 40px; line-height: 1.1; color: #1a202c; }
.hero p { margin: 0 0 22px; color: #718096; font-size: 16px; }
.hero ul { margin: 0; padding-left: 20px; color: #4a5568; line-height: 1.9; }
.form-card { padding: 36px; align-self: center; }
.form-card h2 { margin: 0; font-size: 24px; color: #1a202c; }
.subtext { margin: 8px 0 24px; color: #718096; }
.error-banner {
  margin-bottom: 16px;
  padding: 10px 12px;
  border-radius: 12px;
  background: #fff5f5;
  color: #c53030;
  font-size: 14px;
}
@media (max-width: 860px) {
  .login-panel { grid-template-columns: 1fr; }
  .hero h1 { font-size: 32px; }
}
</style>
