<script setup lang="ts">
import { useAuthStore } from '@/stores/auth.store'

const auth = useAuthStore()
</script>

<template>
  <div>
    <h1 class="text-2xl font-bold text-gray-800 dark:text-gray-100 mb-6">My Profile</h1>

    <div class="bg-white dark:bg-gray-900 rounded-xl shadow-sm border border-gray-100 dark:border-gray-800 p-6 max-w-lg">
      <div class="space-y-4">
        <div>
          <p class="text-xs text-gray-400 dark:text-gray-500 uppercase tracking-wider font-semibold mb-1">Full Name</p>
          <p class="text-gray-800 dark:text-gray-100 font-medium">{{ auth.userName }}</p>
        </div>
        <div>
          <p class="text-xs text-gray-400 dark:text-gray-500 uppercase tracking-wider font-semibold mb-1">Email</p>
          <p class="text-gray-800 dark:text-gray-100 font-medium">{{ auth.userEmail }}</p>
        </div>
        <div>
          <p class="text-xs text-gray-400 dark:text-gray-500 uppercase tracking-wider font-semibold mb-1">Role</p>
          <p class="text-gray-800 dark:text-gray-100 font-medium">{{ auth.userRole }}</p>
        </div>
        <div>
          <p class="text-xs text-gray-400 dark:text-gray-500 uppercase tracking-wider font-semibold mb-1">User ID</p>
          <p class="text-gray-800 dark:text-gray-100 font-medium">{{ auth.user?.userId }}</p>
        </div>
      </div>
    </div>
  </div>
</template>
