<script setup lang="ts">
import { useRoute } from 'vue-router'

const route = useRoute()
</script>

<template>
  <div class="flex items-center justify-center py-20">
    <div class="text-center">
      <div class="text-4xl mb-4">🚧</div>
      <h2 class="text-xl font-bold text-gray-800 dark:text-gray-100 mb-2">
        {{ route.meta?.title || 'Page' }}
      </h2>
      <p class="text-gray-400 dark:text-gray-500">This page is under construction.</p>
    </div>
  </div>
</template>
