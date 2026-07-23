<script setup lang="ts">
import { RouterLink, RouterView, useRoute, useRouter } from 'vue-router'
import { ref, computed } from 'vue'
import { useAuthStore } from '@/stores/auth.store'
import { useThemeStore } from '@/stores/theme.store'
import { useOrganizationStore } from '@/stores/organization.store'
import { getFilteredNav } from '@/config/navigation'

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()
const theme = useThemeStore()
const org = useOrganizationStore()

const sidebarOpen = ref(false)

const filteredNav = computed(() => getFilteredNav(auth.userPermissions))

function isActive(path: string) {
  return route.path === path || route.path.startsWith(path + '/')
}

function handleLogout() {
  auth.logout()
  router.push('/login')
}

function closeSidebar() {
  sidebarOpen.value = false
}

function goProfile() {
  closeSidebar()
  router.push('/admin/profile')
}
</script>

<template>
  <div class="min-h-screen bg-gray-50 dark:bg-gray-950 flex">
    <!-- Mobile overlay -->
    <div
      v-if="sidebarOpen"
      class="fixed inset-0 bg-black/40 z-40 lg:hidden"
      @click="sidebarOpen = false"
    ></div>

    <!-- Sidebar -->
    <aside
      class="fixed lg:static inset-y-0 left-0 z-50 w-64 bg-white dark:bg-gray-900 border-r border-gray-200 dark:border-gray-800 transition-transform duration-300 flex flex-col"
      :class="sidebarOpen ? 'translate-x-0' : '-translate-x-full lg:translate-x-0'"
    >
      <!-- Sidebar Header -->
      <div class="h-16 flex items-center gap-3 px-5 border-b border-gray-200 dark:border-gray-800">
        <div class="w-8 h-8 bg-blue-600 rounded-lg flex items-center justify-center text-white font-bold text-sm flex-shrink-0">
          {{ org.shortName?.charAt(0) || org.name?.charAt(0) || 'P' }}
        </div>
        <span class="font-bold text-gray-800 dark:text-gray-100 truncate">
          {{ org.shortName || org.name || 'PhantomNet' }}
        </span>
      </div>

      <!-- Nav Items -->
      <nav class="flex-1 overflow-y-auto px-3 py-4 space-y-6">
        <div v-for="(section, si) in filteredNav" :key="si">
          <p v-if="section.label" class="px-3 text-xs font-semibold uppercase tracking-wider text-gray-400 dark:text-gray-500 mb-2">
            {{ section.label }}
          </p>
          <div class="space-y-1">
            <RouterLink
              v-for="item in section.items"
              :key="item.path"
              :to="item.path"
              @click="closeSidebar"
              class="flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium transition-all duration-200 no-underline"
              :class="isActive(item.path)
                ? 'bg-blue-50 dark:bg-blue-900/20 text-blue-700 dark:text-blue-400'
                : 'text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-800 hover:text-gray-900 dark:hover:text-gray-200'"
            >
              <span v-html="item.icon" class="flex-shrink-0"></span>
              {{ item.label }}
            </RouterLink>
          </div>
        </div>
      </nav>

      <!-- Profile / Logout -->
      <div class="border-t border-gray-200 dark:border-gray-800 p-3 space-y-1">
        <button
          @click="goProfile"
          class="w-full flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-800 transition-all duration-200 cursor-pointer"
        >
          <svg class="w-5 h-5 flex-shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="1.5">
            <path stroke-linecap="round" stroke-linejoin="round" d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z"/>
          </svg>
          <span class="truncate">{{ auth.userName || auth.userEmail }}</span>
        </button>
        <button
          @click="handleLogout"
          class="w-full flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium text-red-600 dark:text-red-400 hover:bg-red-50 dark:hover:bg-red-900/20 transition-all duration-200 cursor-pointer"
        >
          <svg class="w-5 h-5 flex-shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="1.5">
            <path stroke-linecap="round" stroke-linejoin="round" d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1"/>
          </svg>
          Sign Out
        </button>
      </div>
    </aside>

    <!-- Main Content -->
    <div class="flex-1 flex flex-col min-w-0">
      <!-- Top Bar -->
      <header class="h-16 bg-white dark:bg-gray-900 border-b border-gray-200 dark:border-gray-800 flex items-center justify-between px-4 lg:px-6 sticky top-0 z-30">
        <div class="flex items-center gap-3">
          <!-- Hamburger (mobile) -->
          <button
            @click="sidebarOpen = !sidebarOpen"
            class="lg:hidden p-2 rounded-lg text-gray-500 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-800 transition cursor-pointer"
          >
            <svg class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
              <path stroke-linecap="round" stroke-linejoin="round" d="M4 6h16M4 12h16M4 18h16"/>
            </svg>
          </button>
          <h2 class="text-lg font-semibold text-gray-800 dark:text-gray-100">
            {{ route.meta?.title || 'Dashboard' }}
          </h2>
        </div>

        <div class="flex items-center gap-2">
          <button
            @click="theme.toggle"
            class="p-2 rounded-lg bg-gray-100 dark:bg-gray-800 text-gray-500 dark:text-gray-400 hover:bg-gray-200 dark:hover:bg-gray-700 transition cursor-pointer"
            :title="theme.isDark ? 'Light mode' : 'Dark mode'"
          >
            <span v-if="theme.isDark">☀️</span>
            <span v-else>🌙</span>
          </button>

          <span class="hidden sm:block text-sm text-gray-500 dark:text-gray-400 px-2">
            {{ auth.userName }}
          </span>
        </div>
      </header>

      <!-- Page Content -->
      <main class="flex-1 p-4 lg:p-6 overflow-y-auto">
        <RouterView />
      </main>
    </div>
  </div>
</template>
