<script setup lang="ts">
import { RouterLink, RouterView } from 'vue-router'
import { onMounted } from 'vue'
import { useThemeStore } from '@/stores/theme.store'
import { useOrganizationStore } from '@/stores/organization.store'

const theme = useThemeStore()
const org = useOrganizationStore()

onMounted(() => {
  org.load()
})
</script>

<template>
  <div class="min-h-screen bg-gray-50 dark:bg-gray-950 transition-colors duration-300">
    <!-- Header / Navbar -->
    <header class="bg-white dark:bg-gray-900 shadow-sm border-b border-gray-200 dark:border-gray-700 transition-colors duration-300 sticky top-0 z-50">
      <div class="max-w-6xl mx-auto px-4 sm:px-6 lg:px-8">
        <div class="flex items-center justify-between h-16">
          <!-- Logo + Company Name -->
          <RouterLink to="/" class="flex items-center gap-3 no-underline group">
            <!-- TODO: Replace with actual logo from org store when available -->
            <div class="w-8 h-8 bg-blue-600 dark:bg-blue-500 rounded-lg flex items-center justify-center text-white font-bold text-sm transition-transform duration-300 group-hover:scale-110">
              {{ org.shortName?.charAt(0) || org.name.charAt(0) || 'P' }}
            </div>
            <span class="text-lg font-bold text-gray-800 dark:text-gray-100 transition-colors duration-300">
              {{ org.shortName || org.name || 'PhantomNet' }}
            </span>
          </RouterLink>

          <!-- Navigation Links -->
          <nav class="hidden md:flex items-center gap-8">
            <RouterLink
              to="/"
              class="text-gray-600 dark:text-gray-300 hover:text-blue-600 dark:hover:text-blue-400 transition-all duration-200 no-underline relative after:absolute after:bottom-0 after:left-0 after:h-0.5 after:w-0 hover:after:w-full after:bg-blue-600 dark:after:bg-blue-400 after:transition-all after:duration-300"
              active-class="text-blue-600 dark:text-blue-400 font-semibold after:w-full"
            >
              Home
            </RouterLink>
            <RouterLink
              to="/about"
              class="text-gray-600 dark:text-gray-300 hover:text-blue-600 dark:hover:text-blue-400 transition-all duration-200 no-underline relative after:absolute after:bottom-0 after:left-0 after:h-0.5 after:w-0 hover:after:w-full after:bg-blue-600 dark:after:bg-blue-400 after:transition-all after:duration-300"
              active-class="text-blue-600 dark:text-blue-400 font-semibold after:w-full"
            >
              About
            </RouterLink>
            <RouterLink
              to="/contact"
              class="text-gray-600 dark:text-gray-300 hover:text-blue-600 dark:hover:text-blue-400 transition-all duration-200 no-underline relative after:absolute after:bottom-0 after:left-0 after:h-0.5 after:w-0 hover:after:w-full after:bg-blue-600 dark:after:bg-blue-400 after:transition-all after:duration-300"
              active-class="text-blue-600 dark:text-blue-400 font-semibold after:w-full"
            >
              Contact
            </RouterLink>
          </nav>

          <!-- Right side: mobile menu + theme toggle -->
          <div class="flex items-center gap-3">
            <button
              @click="theme.toggle"
              class="p-2 rounded-lg bg-gray-100 dark:bg-gray-800 text-gray-600 dark:text-gray-300 hover:bg-gray-200 dark:hover:bg-gray-700 transition-all duration-200 cursor-pointer hover:scale-110"
              :title="theme.isDark ? 'Switch to light mode' : 'Switch to dark mode'"
            >
              <span v-if="theme.isDark">☀️</span>
              <span v-else>🌙</span>
            </button>

            <!-- TODO: Mobile hamburger menu for small screens -->
            <!-- TODO: Add mobile navigation drawer/sheet -->
          </div>
        </div>
      </div>
    </header>

    <!-- Page Content -->
    <main class="max-w-6xl mx-auto px-4 sm:px-6 lg:px-8 py-8 sm:py-12">
      <RouterView />
    </main>

    <!-- Footer -->
    <footer class="bg-white dark:bg-gray-900 border-t border-gray-200 dark:border-gray-700 transition-colors duration-300 mt-16">
      <div class="max-w-6xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        <div class="flex flex-col md:flex-row items-center justify-between gap-4 text-sm text-gray-500 dark:text-gray-400">
          <p>&copy; {{ new Date().getFullYear() }} {{ org.name || 'PhantomNet' }}. All rights reserved.</p>
          <div class="flex items-center gap-4">
            <!-- TODO: Add social media links here -->
            <!-- TODO: Add footer navigation links -->
          </div>
        </div>
      </div>
    </footer>
  </div>
</template>
