<template>
  <t-dialog
    :visible="visible"
    header="设置资源路径"
    :close-on-overlay-click="false"
    width="550px"
    @close="$emit('update:visible', false)"
  >
    <!-- 连接状态 -->
    <div class="proxy-status">
      <div class="status-header">
        <t-icon name="laptop" size="24px" />
        <span>本地代理服务</span>
      </div>
      
      <div class="status-body">
        <div v-if="checkingConnection" class="checking">
          <t-loading size="small" />
          <span>检测连接中...</span>
        </div>
        <div v-else-if="proxyConnected" class="connected">
          <t-icon name="check-circle" size="16px" />
          <span>已连接</span>
        </div>
        <div v-else class="disconnected">
          <t-icon name="error-circle" size="16px" />
          <span>未连接</span>
          <t-button size="small" variant="outline" @click="checkConnection">
            重试
          </t-button>
        </div>
      </div>
      
      <div v-if="!proxyConnected" class="status-tip">
        <t-icon name="info-circle" />
        <span>请确保 ProxyService.exe 已运行</span>
      </div>
    </div>

    <!-- 路径设置 -->
    <div class="path-setting">
      <div class="setting-header">
        <t-icon name="folder-open" size="24px" />
        <span>漫画资源路径</span>
      </div>
      
      <div class="setting-body">
        <div class="current-path" v-if="currentPath">
          <span class="label">当前路径：</span>
          <span class="path-text">{{ currentPath.path }}</span>
        </div>
        
        <t-input
          v-model="pathInput"
          placeholder="请输入漫画文件夹路径，如：D:/MyComics"
          :status="pathStatus"
        />
        
        <div v-if="pathValidation" class="validation-result">
          <div v-if="pathValidation.valid" class="valid">
            <t-icon name="check-circle" />
            <span>{{ pathValidation.message }}</span>
          </div>
          <div v-else class="invalid">
            <t-icon name="error-circle" />
            <span>{{ pathValidation.message }}</span>
          </div>
        </div>
      </div>
    </div>

    <template #footer>
      <t-button variant="outline" @click="$emit('update:visible', false)">
        取消
      </t-button>
      <t-button 
        theme="primary" 
        :loading="saving"
        :disabled="!pathInput.trim()"
        @click="handleSave"
      >
        保存配置
      </t-button>
    </template>
  </t-dialog>
</template>

<script setup lang="ts">
import { ref, watch, onMounted } from 'vue';
import { MessagePlugin } from 'tdesign-vue-next';
import * as api from '@/api/resources';
import type { ResourcePath } from '@/api/resources';

interface Props {
  visible: boolean;
  currentPath: ResourcePath | null;
}

interface Emits {
  (e: 'update:visible', visible: boolean): void;
  (e: 'save', path: string): void;
}

interface ValidationResult {
  valid: boolean;
  message: string;
}

const props = defineProps<Props>();
const emit = defineEmits<Emits>();

// 状态
const saving = ref(false);
const checkingConnection = ref(false);
const proxyConnected = ref(false);
const pathInput = ref('');
const pathValidation = ref<ValidationResult | null>(null);

// 路径状态
const pathStatus = ref<'default' | 'success' | 'error'>('default');

// 监听visible变化
watch(() => props.visible, (newVal) => {
  if (newVal) {
    pathInput.value = props.currentPath?.path || '';
    pathValidation.value = null;
    checkConnection();
  }
});

// 监听路径输入
watch(pathInput, async (newPath) => {
  if (!newPath.trim()) {
    pathValidation.value = null;
    pathStatus.value = 'default';
    return;
  }
  
  await validatePath(newPath);
});

// 检查代理连接
async function checkConnection() {
  checkingConnection.value = true;
  
  try {
    await api.getProxyHealth();
    proxyConnected.value = true;
  } catch {
    proxyConnected.value = false;
  } finally {
    checkingConnection.value = false;
  }
}

// 验证路径
async function validatePath(path: string) {
  // 基础验证：检查是否为空
  if (!path.trim()) {
    pathValidation.value = { valid: false, message: '请输入路径' };
    pathStatus.value = 'error';
    return;
  }
  
  // 格式验证
  if (!/^[a-zA-Z]:[/\\]/.test(path)) {
    pathValidation.value = { valid: false, message: '路径格式不正确，应为如 D:/ 或 C:/' };
    pathStatus.value = 'error';
    return;
  }
  
  // 如果代理已连接，尝试验证路径
  if (proxyConnected.value) {
    try {
      // 调用后端API验证路径
      await api.testResourcePath(path);
      pathValidation.value = { valid: true, message: '路径有效' };
      pathStatus.value = 'success';
    } catch {
      pathValidation.value = { valid: false, message: '无法访问该路径' };
      pathStatus.value = 'error';
    }
  } else {
    // 代理未连接，只做基础验证
    pathValidation.value = { valid: true, message: '路径格式正确' };
    pathStatus.value = 'success';
  }
}

// 保存
async function handleSave() {
  if (!pathInput.value.trim()) {
    MessagePlugin.warning('请输入路径');
    return;
  }
  
  if (pathValidation.value && !pathValidation.value.valid) {
    MessagePlugin.warning('请检查路径是否正确');
    return;
  }
  
  saving.value = true;
  
  try {
    // 添加到代理允许路径
    try {
      await api.addAllowedPath(pathInput.value);
    } catch {
      // 忽略代理添加失败
    }
    
    emit('save', pathInput.value);
  } finally {
    saving.value = false;
  }
}

onMounted(() => {
  if (props.visible) {
    checkConnection();
  }
});
</script>

<style scoped>
.proxy-status {
  padding: 16px;
  background: var(--td-bg-color-secondary-container);
  border-radius: 8px;
  margin-bottom: 20px;
}

.status-header,
.setting-header {
  display: flex;
  align-items: center;
  gap: 8px;
  font-weight: 500;
  margin-bottom: 12px;
}

.status-body {
  margin-left: 32px;
}

.checking,
.connected,
.disconnected {
  display: flex;
  align-items: center;
  gap: 8px;
}

.checking {
  color: var(--td-text-color-secondary);
}

.connected {
  color: var(--td-success-color);
}

.disconnected {
  color: var(--td-error-color);
}

.status-tip {
  display: flex;
  align-items: center;
  gap: 6px;
  margin-top: 12px;
  margin-left: 32px;
  padding: 8px 12px;
  background: var(--td-warning-color-轻量-10);
  border-radius: 4px;
  font-size: 12px;
  color: var(--td-warning-color);
}

.path-setting {
  padding: 16px;
  background: var(--td-bg-color-secondary-container);
  border-radius: 8px;
}

.setting-body {
  margin-left: 32px;
}

.current-path {
  margin-bottom: 12px;
}

.current-path .label {
  color: var(--td-text-color-secondary);
}

.current-path .path-text {
  font-family: monospace;
  background: var(--td-bg-color-container);
  padding: 2px 8px;
  border-radius: 4px;
}

.validation-result {
  margin-top: 12px;
  font-size: 12px;
}

.validation-result .valid {
  display: flex;
  align-items: center;
  gap: 6px;
  color: var(--td-success-color);
}

.validation-result .invalid {
  display: flex;
  align-items: center;
  gap: 6px;
  color: var(--td-error-color);
}
</style>
