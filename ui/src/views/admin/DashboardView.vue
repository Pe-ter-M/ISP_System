<script setup lang="ts">
import { useAuthStore } from '@/stores/auth.store'

const auth = useAuthStore()
</script>

<template>
  <div>
    <h1 class="text-2xl font-bold text-gray-800 dark:text-gray-100 mb-2">Welcome, {{ auth.userName }}</h1>
    <p class="text-gray-500 dark:text-gray-400 mb-6">You are logged in as <strong>{{ auth.userRole }}</strong></p>

    <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
      <div class="bg-white dark:bg-gray-900 rounded-xl p-6 shadow-sm border border-gray-100 dark:border-gray-800">
        <p class="text-sm text-gray-400 dark:text-gray-500 mb-1">Email</p>
        <p class="font-semibold text-gray-800 dark:text-gray-100">{{ auth.userEmail }}</p>
      </div>
      <div class="bg-white dark:bg-gray-900 rounded-xl p-6 shadow-sm border border-gray-100 dark:border-gray-800">
        <p class="text-sm text-gray-400 dark:text-gray-500 mb-1">Role</p>
        <p class="font-semibold text-gray-800 dark:text-gray-100">{{ auth.userRole }}</p>
      </div>
      <div class="bg-white dark:bg-gray-900 rounded-xl p-6 shadow-sm border border-gray-100 dark:border-gray-800">
        <p class="text-sm text-gray-400 dark:text-gray-500 mb-1">Permissions</p>
        <p class="font-semibold text-gray-800 dark:text-gray-100">{{ auth.userPermissions.length }}</p>
      </div>
    </div>
  </div>
</template>
