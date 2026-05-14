<template>
  <div class="salary-manage p-6">
    <!-- 页面头部 -->
    <div class="page-header">
      <div class="header-content">
        <div class="header-info">
          <h1 class="page-title">工资管理</h1>
          <p class="page-subtitle">管理工资模板和工资录入记录</p>
        </div>
      </div>
    </div>

    <!-- Tab切换 -->
    <t-tabs v-model="activeTab">
      <t-tab-panel value="templates" label="模板管理">
        <div class="template-section">
          <!-- 新建模板 -->
          <div class="template-form-card">
            <div class="form-row">
              <t-input v-model="newTemplateTitle" placeholder="模板名称" class="template-input" />
              <t-button theme="primary" @click="createTemplate">
                <template #icon><i class="i-lucide-plus"></i></template>
                新建模板
              </t-button>
            </div>
          </div>

          <!-- 模板列表 -->
          <div v-if="templates.length === 0" class="empty-state">
            <div class="empty-icon">
              <i class="i-lucide-file-text"></i>
            </div>
            <h3 class="empty-title">暂无模板</h3>
            <p class="empty-description">创建您的第一个工资模板，方便快速录入工资</p>
          </div>

          <div v-for="tpl in templates" :key="tpl.id" class="template-card">
            <div class="template-header">
              <h3 class="template-name">{{ tpl.title }}</h3>
              <t-space>
                <t-button variant="text" size="small" @click="editTemplate(tpl)">
                  <template #icon><i class="i-lucide-pencil"></i></template>
                  编辑
                </t-button>
                <t-button theme="danger" variant="text" size="small" @click="deleteTemplate(tpl)">
                  <template #icon><i class="i-lucide-trash-2"></i></template>
                  删除
                </t-button>
              </t-space>
            </div>

            <!-- 模板子项 -->
            <div class="template-items">
              <div v-for="(item, idx) in tpl.templateItems" :key="item.id" class="template-item">
                <i class="i-lucide-grip-vertical item-drag-handle"></i>
                <span class="item-name">{{ item.name }}</span>
                <t-tag v-if="item.isActual" size="small" theme="success" variant="light">到手项</t-tag>
              </div>
              <div v-if="!tpl.templateItems?.length" class="empty-items">
                暂无子项，请编辑添加
              </div>
            </div>
          </div>
        </div>
      </t-tab-panel>

      <t-tab-panel value="salary" label="工资录入">
        <div class="salary-form-section">
          <div class="form-card">
            <t-form label-width="120px">
              <t-form-item label="选择年月">
                <div class="form-inline-items">
                  <t-date-picker
                    v-model="salaryForm.salaryDate"
                    mode="month"
                    allow-input
                    placeholder="选择年月"
                    class="date-picker"
                  />
                  <t-button theme="primary" variant="outline" @click="loadTemplateForInput">
                    <template #icon><i class="i-lucide-download"></i></template>
                    加载模板
                  </t-button>
                </div>
              </t-form-item>

              <t-form-item label="标题（公司）">
                <t-input v-model="salaryForm.content" placeholder="如：字节跳动" class="form-input" />
              </t-form-item>

              <!-- 工资明细 -->
              <template v-if="currentTemplateItems.length > 0">
                <t-divider>工资明细</t-divider>
                <t-form-item v-for="(item, idx) in currentTemplateItems" :key="item.id" :label="item.name">
                  <div class="detail-row">
                    <t-input-number
                      v-model="salaryForm.detailAmounts[idx]"
                      :min="0"
                      :decimal-places="2"
                      placeholder="金额"
                      class="amount-input"
                    />
                    <t-checkbox v-model="salaryForm.actualIndex" :value="idx" class="actual-checkbox">
                      实际到手项
                    </t-checkbox>
                  </div>
                </t-form-item>

                <t-form-item label="实际到手">
                  <span class="actual-amount">
                    ¥{{ actualAmount.toLocaleString('zh-CN', { minimumFractionDigits: 2 }) }}
                  </span>
                </t-form-item>
              </template>

              <t-form-item label="分配到账户">
                <div class="allocations">
                  <div v-for="(alloc, idx) in salaryForm.allocations" :key="idx" class="allocation-row">
                    <AccountSelect v-model="alloc.accountId" class="allocation-account" />
                    <t-input-number v-model="alloc.amount" :min="0" :decimal-places="2" placeholder="金额" class="allocation-amount" />
                    <t-button theme="danger" variant="text" @click="salaryForm.allocations.splice(idx, 1)">
                      <template #icon><i class="i-lucide-trash-2"></i></template>
                    </t-button>
                  </div>
                  <t-button variant="dashed" block @click="salaryForm.allocations.push({ accountId: undefined, amount: 0 })">
                    <template #icon><i class="i-lucide-plus"></i></template>
                    添加分配
                  </t-button>
                </div>
              </t-form-item>

              <t-form-item label="备注">
                <t-textarea v-model="salaryForm.remark" placeholder="备注信息" />
              </t-form-item>

              <t-form-item>
                <t-space>
                  <t-button theme="primary" @click="submitSalary">保存</t-button>
                  <t-button variant="outline" @click="resetSalaryForm">取消</t-button>
                </t-space>
              </t-form-item>
            </t-form>
          </div>

          <!-- 历史录入列表 -->
          <div class="history-card">
            <div class="card-header">
              <h3 class="card-title">历史录入</h3>
            </div>
            <t-table :data="salaryList" :columns="salaryColumns" row-key="id" :loading="loading" stripe>
              <template #salaryDate="{ row }">
                {{ formatDate(row.salaryDate) }}
              </template>
              <template #amount="{ row }">
                <span class="text-success font-bold">
                  ¥{{ row.amount?.toLocaleString('zh-CN', { minimumFractionDigits: 2 }) }}
                </span>
              </template>
              <template #operation="{ row }">
                <t-space>
                  <t-button variant="text" size="small">查看</t-button>
                  <t-button variant="text" size="small">编辑</t-button>
                </t-space>
              </template>
            </t-table>
          </div>
        </div>
      </t-tab-panel>
    </t-tabs>

    <!-- 模板表单弹窗 -->
    <SalaryTemplateForm
      v-model="showTemplateForm"
      :edit-data="editingTemplate"
      @success="loadTemplates"
    />
  </div>
</template>

<script setup>
import { ref, computed, reactive, onMounted } from 'vue'
import { MessagePlugin, DialogPlugin } from 'tdesign-vue-next'
import { financeSalaryTemplateApi, financeIncomeApi } from '@/api'
import AccountSelect from '@/components/finance/AccountSelect.vue'
import SalaryTemplateForm from '@/components/finance/SalaryTemplateForm.vue'
import dayjs from 'dayjs'

const activeTab = ref('templates')
const templates = ref([])
const salaryList = ref([])
const loading = ref(false)
const showTemplateForm = ref(false)
const editingTemplate = ref(null)
const newTemplateTitle = ref('')

const salaryForm = reactive({
  salaryDate: new Date(),
  content: '',
  templateId: undefined,
  detailAmounts: [],
  actualIndex: -1,
  allocations: [],
  remark: ''
})

const currentTemplateItems = ref([])

const actualAmount = computed(() => {
  if (salaryForm.actualIndex >= 0) {
    return salaryForm.detailAmounts[salaryForm.actualIndex] || 0
  }
  return 0
})

const salaryColumns = [
  { colKey: 'salaryDate', title: '月份', width: 120, cell: 'salaryDate' },
  { colKey: 'content', title: '公司', width: 150 },
  { colKey: 'amount', title: '实际到手', width: 150, cell: 'amount' },
  { colKey: 'operation', title: '操作', width: 150, cell: 'operation' }
]

const formatDate = (date) => dayjs(date).format('YYYY年MM月')

const loadTemplates = async () => {
  try {
    const res = await financeSalaryTemplateApi.list()
    // API 返回 { success, data: [...] }，拦截器返回此对象
    templates.value = res?.data || res || []
  } catch (error) {
    MessagePlugin.error('加载模板失败')
  }
}

const createTemplate = async () => {
  if (!newTemplateTitle.value.trim()) {
    MessagePlugin.warning('请输入模板名称')
    return
  }
  try {
    await financeSalaryTemplateApi.create({ title: newTemplateTitle.value, templateItems: [] })
    MessagePlugin.success('创建成功')
    newTemplateTitle.value = ''
    loadTemplates()
  } catch (error) {
    MessagePlugin.error('创建失败')
  }
}

const editTemplate = (tpl) => {
  editingTemplate.value = tpl
  showTemplateForm.value = true
}

const deleteTemplate = (tpl) => {
  const dialog = DialogPlugin.confirm({
    header: '确认删除',
    body: `确定要删除模板"${tpl.title}"吗？删除后不可恢复。`,
    theme: 'warning',
    onConfirm: async () => {
      try {
        await financeSalaryTemplateApi.delete(tpl.id)
        MessagePlugin.success('删除成功')
        loadTemplates()
      } catch (e) {
        MessagePlugin.error('删除失败')
      } finally {
        dialog.hide()
      }
    },
  })
}

const loadTemplateForInput = async () => {
  if (!salaryForm.salaryDate) {
    MessagePlugin.warning('请先选择年月')
    return
  }
  // 加载第一个模板作为默认
  if (templates.value.length > 0) {
    try {
      const res = await financeSalaryTemplateApi.get(templates.value[0].id)
      // API 返回 { success, data: {...} }，拦截器返回此对象
      const tpl = res?.data || res
      currentTemplateItems.value = tpl.templateItems || []
      salaryForm.templateId = tpl.id
      salaryForm.detailAmounts = (tpl.templateItems || []).map(() => 0)
    } catch (error) {
      MessagePlugin.error('加载模板失败')
    }
  }
}

const submitSalary = async () => {
  if (!salaryForm.templateId) {
    MessagePlugin.warning('请先加载模板')
    return
  }
  if (salaryForm.actualIndex < 0) {
    MessagePlugin.warning('请标记实际到手项')
    return
  }
  try {
    const data = {
      type: 'salary',
      amount: actualAmount.value,
      content: salaryForm.content,
      incomeDate: salaryForm.salaryDate,
      salaryDetail: {
        templateId: salaryForm.templateId,
        salaryDate: salaryForm.salaryDate,
        actualItemId: currentTemplateItems.value[salaryForm.actualIndex]?.id,
        detailItems: currentTemplateItems.value.map((item, idx) => ({
          templateItemId: item.id,
          amount: salaryForm.detailAmounts[idx] || 0
        }))
      },
      incomeAccounts: salaryForm.allocations.filter(a => a.accountId && a.amount > 0)
    }
    await financeIncomeApi.create(data)
    MessagePlugin.success('保存成功')
    resetSalaryForm()
    loadSalaryList()
  } catch (error) {
    MessagePlugin.error('保存失败')
  }
}

const resetSalaryForm = () => {
  salaryForm.salaryDate = new Date()
  salaryForm.content = ''
  salaryForm.templateId = undefined
  salaryForm.detailAmounts = []
  salaryForm.actualIndex = -1
  salaryForm.allocations = []
  salaryForm.remark = ''
  currentTemplateItems.value = []
}

const loadSalaryList = async () => {
  loading.value = true
  try {
    const res = await financeIncomeApi.list({ type: 'salary' })
    // API 返回 { success, data: [...] }，拦截器返回此对象
    const data = res?.data || res
    salaryList.value = data?.items || data || []
  } catch (error) {
    console.error('加载工资列表失败', error)
  } finally {
    loading.value = false
  }
}

onMounted(() => {
  loadTemplates()
  loadSalaryList()
})
</script>

<style scoped>
.salary-manage {
  padding: var(--space-6);
  max-width: var(--content-max-width);
  margin: 0 auto;
  animation: fadeIn 0.5s ease;
}

/* 页面头部 */
.page-header {
  margin-bottom: var(--space-6);
}

.header-content {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: var(--space-6);
}

.header-info {
  animation: fadeInUp 0.6s ease;
}

.page-title {
  font-size: var(--font-size-2xl);
  font-weight: var(--font-weight-bold);
  color: var(--text-primary);
  margin-bottom: var(--space-2);
}

.page-subtitle {
  font-size: var(--font-size-sm);
  color: var(--text-secondary);
}

/* 模板区域 */
.template-section {
  animation: fadeInUp 0.6s ease 0.1s backwards;
}

.template-form-card {
  background: var(--bg-card-solid);
  border-radius: var(--radius-xl);
  padding: var(--space-5);
  border: 1px solid var(--border-light);
  box-shadow: var(--shadow-card);
  margin-bottom: var(--space-5);
}

.form-row {
  display: flex;
  gap: var(--space-3);
  align-items: center;
}

.template-input {
  flex: 1;
  max-width: 400px;
}

/* 模板卡片 */
.template-card {
  background: var(--bg-card-solid);
  border-radius: var(--radius-xl);
  padding: var(--space-5);
  border: 1px solid var(--border-light);
  box-shadow: var(--shadow-card);
  margin-bottom: var(--space-4);
  transition: all var(--transition-normal);
  animation: cardEnter 0.5s ease backwards;
}

.template-card:hover {
  transform: translateY(-2px);
  box-shadow: var(--shadow-lg);
}

.template-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: var(--space-4);
}

.template-name {
  font-size: var(--font-size-base);
  font-weight: var(--font-weight-semibold);
  color: var(--text-primary);
}

.template-items {
  background: var(--bg-color-secondary);
  padding: var(--space-4);
  border-radius: var(--radius-lg);
}

.template-item {
  display: flex;
  align-items: center;
  gap: var(--space-3);
  padding: var(--space-2) 0;
}

.item-drag-handle {
  color: var(--text-tertiary);
  cursor: move;
  font-size: var(--font-size-sm);
}

.item-name {
  flex: 1;
  font-size: var(--font-size-sm);
  color: var(--text-primary);
}

.empty-items {
  font-size: var(--font-size-sm);
  color: var(--text-tertiary);
  padding: var(--space-3) 0;
  text-align: center;
}

/* 空状态 */
.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: var(--space-12);
  background: var(--bg-card-solid);
  border-radius: var(--radius-xl);
  border: 1px solid var(--border-light);
  text-align: center;
  animation: cardEnter 0.5s ease backwards;
}

.empty-icon {
  width: 80px;
  height: 80px;
  border-radius: var(--radius-full);
  background: var(--primary-lighter);
  display: flex;
  align-items: center;
  justify-content: center;
  margin-bottom: var(--space-4);
  color: var(--primary-color);
  font-size: 32px;
}

.empty-title {
  font-size: var(--font-size-lg);
  font-weight: var(--font-weight-semibold);
  color: var(--text-primary);
  margin-bottom: var(--space-2);
}

.empty-description {
  font-size: var(--font-size-sm);
  color: var(--text-secondary);
}

/* 工资表单区域 */
.salary-form-section {
  animation: fadeInUp 0.6s ease 0.1s backwards;
}

.form-card {
  background: var(--bg-card-solid);
  border-radius: var(--radius-xl);
  padding: var(--space-5);
  border: 1px solid var(--border-light);
  box-shadow: var(--shadow-card);
  margin-bottom: var(--space-5);
}

.form-inline-items {
  display: flex;
  gap: var(--space-3);
  align-items: center;
}

.date-picker {
  width: 200px;
}

.form-input {
  max-width: 400px;
}

.detail-row {
  display: flex;
  align-items: center;
  gap: var(--space-4);
}

.amount-input {
  width: 200px;
}

.actual-checkbox {
  white-space: nowrap;
}

.actual-amount {
  font-size: var(--font-size-lg);
  font-weight: var(--font-weight-bold);
  color: var(--success-color);
}

.allocations {
  width: 100%;
}

.allocation-row {
  display: flex;
  gap: var(--space-2);
  margin-bottom: var(--space-2);
  align-items: center;
}

.allocation-account {
  flex: 1;
}

.allocation-amount {
  width: 150px;
}

/* 历史记录卡片 */
.history-card {
  background: var(--bg-card-solid);
  border-radius: var(--radius-xl);
  padding: var(--space-5);
  border: 1px solid var(--border-light);
  box-shadow: var(--shadow-card);
  animation: cardEnter 0.6s ease 0.2s backwards;
}

.card-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: var(--space-5);
}

.card-title {
  font-size: var(--font-size-base);
  font-weight: var(--font-weight-semibold);
  color: var(--text-primary);
}

.text-success {
  color: var(--success-color);
}

.font-bold {
  font-weight: var(--font-weight-bold);
}

/* 动画 */
@keyframes fadeIn {
  from {
    opacity: 0;
  }
  to {
    opacity: 1;
  }
}

@keyframes fadeInUp {
  from {
    opacity: 0;
    transform: translateY(20px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

@keyframes cardEnter {
  from {
    opacity: 0;
    transform: translateY(20px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

/* 响应式 */
@media (max-width: 768px) {
  .salary-manage {
    padding: var(--space-4);
  }
  
  .form-row {
    flex-direction: column;
    align-items: stretch;
  }
  
  .template-input {
    max-width: none;
  }
  
  .form-inline-items {
    flex-direction: column;
    align-items: stretch;
  }
  
  .detail-row {
    flex-direction: column;
    align-items: flex-start;
  }
  
  .allocation-row {
    flex-direction: column;
    align-items: stretch;
  }
}
</style>
