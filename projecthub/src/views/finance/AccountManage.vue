<template>
  <div class="account-manage p-6">
    <!-- 标题栏 -->
    <div class="flex justify-between items-center mb-6">
      <h1 class="text-2xl font-bold">账户管理</h1>
    </div>

    <!-- 账户书本展示 -->
    <div class="book-container mb-6">
      <div class="book-wrapper">
        <div class="book-page" v-for="acc in accounts" :key="acc.id">
          <div class="book-page-inner">
            <div class="book-page-header">
              <span class="book-icon">{{ acc.icon || '💰' }}</span>
              <t-space>
                <t-button variant="text" size="small" @click="editAccount(acc)">
                  <template #icon><i class="i-lucide-pencil" /></template>
                </t-button>
                <t-button variant="text" size="small" theme="danger" @click="deleteAccount(acc)">
                  <template #icon><i class="i-lucide-trash-2" /></template>
                </t-button>
              </t-space>
            </div>
            <div class="book-page-content">
              <h3 class="book-account-name">{{ acc.name }}</h3>
              <div class="book-account-type">{{ acc.type || '其他' }}</div>
              <div class="book-account-balance" :class="acc.balance >= 0 ? 'positive' : 'negative'">
                ¥{{ acc.balance?.toLocaleString('zh-CN', { minimumFractionDigits: 2 }) }}
              </div>
              <div class="book-account-default">
                <t-checkbox
                  :checked="acc.isDefaultExpense"
                  @change="(val) => setDefaultAccount(acc.id, val)"
                >
                  默认支出账户
                </t-checkbox>
              </div>
            </div>
            <div class="book-page-footer">
              <t-button variant="outline" size="small" @click="openTransfer(acc)">
                <template #icon><i class="i-lucide-arrow-right-left" /></template>
                转账
              </t-button>
            </div>
          </div>
        </div>
        
        <!-- 新增账户卡片 -->
        <div class="book-page add-card" @click="showAccountForm = true">
          <div class="add-card-inner">
            <div class="add-icon">
              <i class="i-lucide-plus-circle text-4xl" />
            </div>
            <div class="add-text">新增账户</div>
          </div>
        </div>
      </div>
    </div>

    <!-- 转账记录 -->
    <t-card title="转账记录" hover-shadow>
      <t-table
        :data="transfers"
        :columns="transferColumns"
        row-key="id"
        :loading="loadingTransfers"
        :pagination="null"
        stripe
      >
        <template #date="{ row }">
          {{ formatDate(row.createdAt) }}
        </template>
        <template #fromAccount="{ row }">
          {{ row.fromAccountName || '-' }}
        </template>
        <template #toAccount="{ row }">
          {{ row.toAccountName || '-' }}
        </template>
        <template #amount="{ row }">
          <span class="font-bold">¥{{ row.amount?.toLocaleString('zh-CN', { minimumFractionDigits: 2 }) }}</span>
        </template>
      </t-table>
    </t-card>

    <!-- 账户表单弹窗 -->
    <t-dialog
      v-model:visible="showAccountForm"
      :header="editingAccount ? '编辑账户' : '新增账户'"
      width="500px"
      @confirm="submitAccount"
    >
      <t-form :data="accountForm" label-width="100px" ref="accountFormRef">
        <t-form-item label="账户名称" name="name">
          <t-input v-model="accountForm.name" placeholder="请输入账户名称" />
        </t-form-item>
        <t-form-item label="账户类型" name="type">
          <t-select v-model="accountForm.type" placeholder="请选择账户类型">
            <t-option value="现金" label="现金" />
            <t-option value="银行卡" label="银行卡" />
            <t-option value="支付宝" label="支付宝" />
            <t-option value="微信" label="微信" />
            <t-option value="其他" label="其他" />
          </t-select>
        </t-form-item>
        <t-form-item label="当前余额" name="balance">
          <t-input-number
            v-model="accountForm.balance"
            :decimal-places="2"
            placeholder="请输入当前余额"
            style="width: 200px"
          />
        </t-form-item>
        <t-form-item label="图标">
          <t-input v-model="accountForm.icon" placeholder="图标 emoji" style="width: 100px" />
        </t-form-item>
        <t-form-item label="设为默认">
          <t-checkbox v-model="accountForm.isDefaultExpense">默认支出账户</t-checkbox>
        </t-form-item>
      </t-form>
    </t-dialog>

    <!-- 转账弹窗 -->
    <TransferForm
      v-model="showTransferForm"
      :from-account-id="selectedAccountId"
      @success="loadData"
    />
  </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { MessagePlugin, DialogPlugin } from 'tdesign-vue-next'
import { financeAccountApi, financeTransferApi } from '@/api'
import dayjs from 'dayjs'
import TransferForm from '@/components/finance/TransferForm.vue'

const accounts = ref([])
const transfers = ref([])
const loadingTransfers = ref(false)
const showAccountForm = ref(false)
const showTransferForm = ref(false)
const editingAccount = ref(null)
const selectedAccountId = ref(undefined)
const accountFormRef = ref(null)

const accountForm = reactive({
  name: '',
  type: '其他',
  balance: 0,
  icon: '',
  isDefaultExpense: false
})

const transferColumns = [
  { colKey: 'createdAt', title: '日期', width: 120, cell: 'date' },
  { colKey: 'fromAccount', title: '转出', width: 120, cell: 'fromAccount' },
  { colKey: 'toAccount', title: '转入', width: 120, cell: 'toAccount' },
  { colKey: 'amount', title: '金额', width: 150, cell: 'amount' },
  { colKey: 'remark', title: '备注', ellipsis: true }
]

const formatDate = (date) => dayjs(date).format('YYYY-MM-DD')

const loadData = async () => {
  try {
    const res = await financeAccountApi.list()
    // API 返回 { success, data: [...] }，拦截器返回此对象
    accounts.value = res?.data || res || []
  } catch (error) {
    MessagePlugin.error('加载账户失败')
  }
  loadTransfers()
}

const loadTransfers = async () => {
  loadingTransfers.value = true
  try {
    const res = await financeTransferApi.list()
    // API 返回 { success, data: [...] }，拦截器返回此对象
    const data = res?.data || res
    transfers.value = data?.items || data || []
  } catch (error) {
    console.error('加载转账记录失败', error)
  } finally {
    loadingTransfers.value = false
  }
}

const resetAccountForm = () => {
  accountForm.name = ''
  accountForm.type = '其他'
  accountForm.balance = 0
  accountForm.icon = ''
  accountForm.isDefaultExpense = false
  editingAccount.value = null
}

const editAccount = (acc) => {
  editingAccount.value = acc
  accountForm.name = acc.name
  accountForm.type = acc.type || '其他'
  accountForm.balance = acc.balance
  accountForm.icon = acc.icon || ''
  accountForm.isDefaultExpense = acc.isDefaultExpense
  showAccountForm.value = true
}

const submitAccount = async () => {
  try {
    const data = { ...accountForm }
    if (editingAccount.value) {
      await financeAccountApi.update(editingAccount.value.id, data)
      MessagePlugin.success('更新成功')
    } else {
      await financeAccountApi.create(data)
      MessagePlugin.success('创建成功')
    }
    showAccountForm.value = false
    resetAccountForm()
    loadData()
  } catch (error) {
    MessagePlugin.error('操作失败')
  }
}

const deleteAccount = (acc) => {
  const dialog = DialogPlugin.confirm({
    header: '确认删除',
    body: `确定要删除账户"${acc.name}"吗？`,
    onConfirm: async () => {
      try {
        await financeAccountApi.delete(acc.id)
        MessagePlugin.success('删除成功')
        loadData()
      } catch (error) {
        MessagePlugin.error('删除失败')
      }
      dialog.hide()
    }
  })
}

const setDefaultAccount = async (id, val) => {
  if (val) {
    try {
      await financeAccountApi.setDefault(id)
      MessagePlugin.success('设置成功')
      loadData()
    } catch (error) {
      MessagePlugin.error('设置失败')
    }
  }
}

const openTransfer = (acc) => {
  selectedAccountId.value = acc.id
  showTransferForm.value = true
}

onMounted(() => {
  loadData()
})
</script>

<style scoped>
.book-container {
  overflow-x: auto;
  overflow-y: hidden;
  padding: 20px 10px;
  margin: 0 -24px;
  padding-left: 24px;
  padding-right: 24px;
}

.book-container::-webkit-scrollbar {
  height: 8px;
}

.book-container::-webkit-scrollbar-track {
  background: #f1f1f1;
  border-radius: 4px;
}

.book-container::-webkit-scrollbar-thumb {
  background: #888;
  border-radius: 4px;
}

.book-container::-webkit-scrollbar-thumb:hover {
  background: #555;
}

.book-wrapper {
  display: flex;
  gap: 16px;
  min-width: min-content;
  padding-bottom: 10px;
}

.book-page {
  flex-shrink: 0;
  width: 240px;
  perspective: 1000px;
}

.book-page-inner {
  background: linear-gradient(135deg, #ffffff 0%, #f8f9fa 100%);
  border-radius: 8px;
  padding: 20px;
  min-height: 320px;
  box-shadow: 
    0 2px 8px rgba(0, 0, 0, 0.1),
    2px 0 0 #e8e8e8,
    4px 0 0 #d0d0d0,
    inset 0 0 30px rgba(0, 0, 0, 0.02);
  border: 1px solid #e0e0e0;
  display: flex;
  flex-direction: column;
  transition: all 0.3s ease;
  position: relative;
  overflow: hidden;
}

.book-page-inner::before {
  content: '';
  position: absolute;
  top: 0;
  right: 0;
  width: 3px;
  height: 100%;
  background: linear-gradient(to right, rgba(0,0,0,0.05), rgba(0,0,0,0.1));
}

.book-page:hover .book-page-inner {
  transform: translateY(-4px) rotateY(-2deg);
  box-shadow: 
    0 8px 24px rgba(0, 0, 0, 0.15),
    3px 0 0 #e8e8e8,
    6px 0 0 #d0d0d0;
}

.book-page-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: 16px;
  padding-bottom: 12px;
  border-bottom: 1px dashed #e0e0e0;
}

.book-icon {
  font-size: 2rem;
}

.book-page-content {
  flex: 1;
  display: flex;
  flex-direction: column;
  justify-content: center;
  align-items: center;
  text-align: center;
}

.book-account-name {
  font-size: 1.25rem;
  font-weight: 700;
  color: #1a1a1a;
  margin: 0 0 8px 0;
}

.book-account-type {
  font-size: 0.875rem;
  color: #666;
  margin-bottom: 16px;
}

.book-account-balance {
  font-size: 1.75rem;
  font-weight: 700;
  margin: 8px 0;
}

.book-account-balance.positive {
  color: #22c55e;
}

.book-account-balance.negative {
  color: #ef4444;
}

.book-account-default {
  margin-top: 12px;
}

.book-page-footer {
  margin-top: 16px;
  padding-top: 16px;
  border-top: 1px dashed #e0e0e0;
  display: flex;
  justify-content: center;
}

.add-card {
  cursor: pointer;
}

.add-card-inner {
  background: linear-gradient(135deg, #f8f9fa 0%, #e9ecef 100%);
  border-radius: 8px;
  padding: 20px;
  min-height: 320px;
  box-shadow: 
    0 2px 8px rgba(0, 0, 0, 0.08),
    2px 0 0 #d0d0d0,
    4px 0 0 #b0b0b0;
  border: 2px dashed #c0c0c0;
  display: flex;
  flex-direction: column;
  justify-content: center;
  align-items: center;
  transition: all 0.3s ease;
}

.add-card:hover .add-card-inner {
  transform: translateY(-4px);
  border-color: #007bff;
  background: linear-gradient(135deg, #e8f4ff 0%, #d4e9ff 100%);
}

.add-icon {
  color: #888;
  margin-bottom: 16px;
  transition: color 0.3s ease;
}

.add-card:hover .add-icon {
  color: #007bff;
}

.add-text {
  font-size: 1rem;
  font-weight: 600;
  color: #666;
  transition: color 0.3s ease;
}

.add-card:hover .add-text {
  color: #007bff;
}
</style>
