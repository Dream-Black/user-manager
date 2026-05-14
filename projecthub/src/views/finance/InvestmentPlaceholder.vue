<template>
  <div class="investment-placeholder min-h-screen flex items-center justify-center">
    <div class="text-center">
      <div class="text-8xl mb-6">🚧</div>
      <h1 class="text-3xl font-bold text-gray-700 mb-4">理财功能 敬请期待</h1>
      <p class="text-gray-500 mb-8">股票、基金、债券等投资管理即将上线</p>
      <t-button theme="primary" @click="$router.back()">
        ← 返回上一页
      </t-button>
    </div>
  </div>
</template>

<script setup>
</script>

<style scoped>
.investment-placeholder {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  min-height: 100vh;
}
</style>
