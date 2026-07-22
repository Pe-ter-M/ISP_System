<script setup lang="ts">
import { RouterLink, RouterView } from 'vue-router'
import { useThemeStore } from '@/stores/theme.store'

const theme = useThemeStore()
</script>

<template>
  <div class="min-h-screen bg-gray-50 dark:bg-gray-950 transition-colors">
    <!-- Navbar -->
    <nav class="bg-white dark:bg-gray-900 shadow-sm border-b border-gray-200 dark:border-gray-700 transition-colors">
      <div class="max-w-5xl mx-auto px-4 py-3 flex items-center justify-between">
        <RouterLink
          to="/"
          class="text-xl font-bold text-blue-600 dark:text-blue-400 no-underline"
        >
          PhantomNet
        </RouterLink>

        <div class="flex items-center gap-6">
          <RouterLink
            to="/"
            class="text-gray-600 dark:text-gray-300 hover:text-blue-600 dark:hover:text-blue-400 transition no-underline"
            active-class="text-blue-600 dark:text-blue-400 font-semibold"
          >
            Home
          </RouterLink>
          <RouterLink
            to="/about"
            class="text-gray-600 dark:text-gray-300 hover:text-blue-600 dark:hover:text-blue-400 transition no-underline"
            active-class="text-blue-600 dark:text-blue-400 font-semibold"
          >
            About
          </RouterLink>
          <RouterLink
            to="/contact"
            class="text-gray-600 dark:text-gray-300 hover:text-blue-600 dark:hover:text-blue-400 transition no-underline"
            active-class="text-blue-600 dark:text-blue-400 font-semibold"
          >
            Contact
          </RouterLink>

          <!-- Theme Toggle -->
          <button
            @click="theme.toggle"
            class="ml-2 p-2 rounded-lg bg-gray-100 dark:bg-gray-800 text-gray-600 dark:text-gray-300 hover:bg-gray-200 dark:hover:bg-gray-700 transition cursor-pointer"
            :title="theme.isDark ? 'Switch to light mode' : 'Switch to dark mode'"
          >
            <span v-if="theme.isDark">☀️</span>
            <span v-else>🌙</span>
          </button>
        </div>
      </div>
    </nav>

    <!-- Page Content -->
    <main class="max-w-5xl mx-auto px-4 py-10">
      <RouterView />
    </main>
  </div>
</template>
