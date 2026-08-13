<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { getUsers } from '@/services/user.service'
import type { UserDetail } from '@/types/user.types'

// ── State ──
const users = ref<UserDetail[]>([])
const loading = ref(true)
const error = ref<string | null>(null)

const page = ref(1)
const pageSize = ref(10)
const totalCount = ref(0)
const totalPages = ref(0)
const search = ref('')
const sortField = ref<string>('name')
const sortDir = ref<'asc' | 'desc'>('asc')

// ── Computed ──
const pageNumbers = computed(() => {
  const pages: number[] = []
  for (let i = 1; i <= totalPages.value; i++) pages.push(i)
  return pages
})

// ── Fetch ──
async function fetchUsers() {
  loading.value = true
  error.value = null
  try {
    const result = await getUsers(page.value, pageSize.value, search.value || undefined, sortField.value || undefined, sortDir.value === 'desc')
    users.value = result.items
    totalCount.value = result.totalCount
    totalPages.value = result.totalPages
  } catch (e: unknown) {
    error.value = errMsg(e, 'Failed to load users')
  } finally {
    loading.value = false
  }
}

onMounted(fetchUsers)

// ── Search with debounce ──
let debounceTimer: ReturnType<typeof setTimeout> | null = null
watch(search, () => {
  if (debounceTimer) clearTimeout(debounceTimer)
  debounceTimer = setTimeout(() => {
    page.value = 1
    fetchUsers()
  }, 300)
})

// ── Sort ──
function toggleSort(field: string) {
  if (sortField.value === field) sortDir.value = sortDir.value === 'asc' ? 'desc' : 'asc'
  else { sortField.value = field; sortDir.value = 'asc' }
  page.value = 1
  fetchUsers()
}
function sortIcon(field: string) {
  if (sortField.value !== field) return '↕'
  return sortDir.value === 'asc' ? '↑' : '↓'
}

// ── Pagination ──
function goToPage(p: number) {
  page.value = p
  fetchUsers()
  window.scrollTo({ top: 0, behavior: 'smooth' })
}

// ── Helpers ──
function errMsg(e: unknown, fallback: string): string {
  if (e && typeof e === 'object') {
    const msg = (e as { message?: unknown }).message
    if (typeof msg === 'string' && msg) return msg
    const err = (e as { error?: unknown }).error
    if (typeof err === 'string' && err) return err
  }
  return fallback
}

function roleClass(roleName: string): string {
  switch (roleName) {
    case 'Admin': return 'bg-purple-100 text-purple-700 dark:bg-purple-900/30 dark:text-purple-300'
    case 'Secretary': return 'bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-300'
    case 'Head Technician': return 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-300'
    case 'Field Technician': return 'bg-orange-100 text-orange-700 dark:bg-orange-900/30 dark:text-orange-300'
    default: return 'bg-gray-100 text-gray-600 dark:bg-gray-800 dark:text-gray-400'
  }
}

function fmtDate(iso: string | undefined): string {
  if (!iso) return '—'
  return new Date(iso).toLocaleDateString()
}
</script>

<template>
  <div>
    <!-- ── Header ── -->
    <div class="flex items-center justify-between mb-6 flex-wrap gap-4">
      <div>
        <h1 class="text-2xl font-bold text-gray-800 dark:text-gray-100">Users</h1>
        <p class="text-sm text-gray-500 dark:text-gray-400 mt-1">{{ totalCount }} total users</p>
      </div>
      <div class="relative">
        <svg class="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
          <path stroke-linecap="round" stroke-linejoin="round" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
        </svg>
        <input v-model="search" type="text" placeholder="Search name, email, phone..."
          class="pl-10 pr-4 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 text-sm focus:ring-2 focus:ring-blue-500 outline-none transition w-64" />
      </div>
    </div>

    <!-- ── Page Size ── -->
    <div class="flex items-center gap-2 mb-4">
      <span class="text-xs text-gray-400 dark:text-gray-500">Show</span>
      <select v-model="pageSize" @change="page = 1; fetchUsers()"
        class="px-2 py-1 rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 text-xs outline-none cursor-pointer">
        <option :value="5">5</option>
        <option :value="10">10</option>
        <option :value="20">20</option>
        <option :value="50">50</option>
      </select>
      <span class="text-xs text-gray-400 dark:text-gray-500">per page</span>
    </div>

    <!-- ── Loading ── -->
    <div v-if="loading && users.length === 0" class="flex justify-center py-20">
      <div class="flex flex-col items-center gap-3">
        <div class="w-8 h-8 border-4 border-blue-500 border-t-transparent rounded-full animate-spin"></div>
        <p class="text-sm text-gray-400 dark:text-gray-500">Loading users...</p>
      </div>
    </div>

    <!-- ── Error ── -->
    <div v-else-if="error && users.length === 0" class="text-center py-20">
      <div class="bg-red-50 dark:bg-red-900/30 border border-red-200 dark:border-red-800 rounded-xl p-6 max-w-md mx-auto">
        <p class="text-red-600 dark:text-red-400 font-medium">{{ error }}</p>
        <button @click="fetchUsers" class="mt-3 px-4 py-2 bg-red-600 hover:bg-red-700 text-white text-sm rounded-lg transition cursor-pointer">Retry</button>
      </div>
    </div>

    <!-- ── Table ── -->
    <div v-else class="bg-white dark:bg-gray-900 rounded-xl shadow-sm border border-gray-100 dark:border-gray-800 overflow-hidden">
      <div class="overflow-x-auto">
        <table class="w-full text-sm">
          <thead>
            <tr class="border-b border-gray-100 dark:border-gray-800 bg-gray-50 dark:bg-gray-800/50">
              <th @click="toggleSort('name')" class="px-4 py-3 text-left font-semibold text-gray-600 dark:text-gray-400 cursor-pointer hover:text-blue-600 dark:hover:text-blue-400 transition whitespace-nowrap">Name <span class="text-xs ml-1">{{ sortIcon('name') }}</span></th>
              <th @click="toggleSort('role')" class="px-4 py-3 text-left font-semibold text-gray-600 dark:text-gray-400 cursor-pointer hover:text-blue-600 dark:hover:text-blue-400 transition whitespace-nowrap">Role <span class="text-xs ml-1">{{ sortIcon('role') }}</span></th>
              <th @click="toggleSort('email')" class="px-4 py-3 text-left font-semibold text-gray-600 dark:text-gray-400 cursor-pointer hover:text-blue-600 dark:hover:text-blue-400 transition whitespace-nowrap hidden sm:table-cell">Email <span class="text-xs ml-1">{{ sortIcon('email') }}</span></th>
              <th class="px-4 py-3 text-left font-semibold text-gray-600 dark:text-gray-400 whitespace-nowrap hidden md:table-cell">Phone</th>
              <th @click="toggleSort('active')" class="px-4 py-3 text-left font-semibold text-gray-600 dark:text-gray-400 cursor-pointer hover:text-blue-600 dark:hover:text-blue-400 transition whitespace-nowrap">Status <span class="text-xs ml-1">{{ sortIcon('active') }}</span></th>
              <th @click="toggleSort('created')" class="px-4 py-3 text-left font-semibold text-gray-600 dark:text-gray-400 cursor-pointer hover:text-blue-600 dark:hover:text-blue-400 transition whitespace-nowrap hidden lg:table-cell">Created <span class="text-xs ml-1">{{ sortIcon('created') }}</span></th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="u in users" :key="u.id"
              class="border-b border-gray-50 dark:border-gray-800/50 hover:bg-gray-50 dark:hover:bg-gray-800/30 transition">
              <td class="px-4 py-3">
                <div class="flex items-center gap-3">
                  <div class="w-8 h-8 rounded-full bg-blue-100 dark:bg-blue-900/50 flex items-center justify-center text-xs font-bold text-blue-600 dark:text-blue-400 flex-shrink-0">
                    {{ u.fullName.charAt(0).toUpperCase() }}
                  </div>
                  <p class="font-medium text-gray-800 dark:text-gray-200">{{ u.fullName }}</p>
                </div>
              </td>
              <td class="px-4 py-3">
                <span :class="roleClass(u.roleName)" class="px-2 py-0.5 rounded-full text-xs font-medium">{{ u.roleName }}</span>
              </td>
              <td class="px-4 py-3 text-gray-600 dark:text-gray-400 hidden sm:table-cell">{{ u.email }}</td>
              <td class="px-4 py-3 text-gray-600 dark:text-gray-400 font-mono text-xs hidden md:table-cell">{{ u.phone || '—' }}</td>
              <td class="px-4 py-3">
                <span class="flex items-center gap-1.5">
                  <span :class="u.isActive ? 'bg-green-500' : 'bg-gray-400'" class="w-2 h-2 rounded-full inline-block"></span>
                  <span class="text-xs text-gray-500 dark:text-gray-400">{{ u.isActive ? 'Active' : 'Inactive' }}</span>
                </span>
              </td>
              <td class="px-4 py-3 text-gray-500 dark:text-gray-400 hidden lg:table-cell">{{ fmtDate(u.createdAt) }}</td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- ── Empty State ── -->
      <div v-if="users.length === 0 && !loading && !error" class="text-center py-16">
        <p class="text-gray-400 dark:text-gray-500">No users found</p>
      </div>

      <!-- ── Pagination ── -->
      <div v-if="totalPages > 1" class="flex items-center justify-between px-4 py-3 border-t border-gray-100 dark:border-gray-800 bg-gray-50 dark:bg-gray-800/50">
        <p class="text-xs text-gray-400 dark:text-gray-500">
          Showing {{ ((page - 1) * pageSize) + 1 }}–{{ Math.min(page * pageSize, totalCount) }} of {{ totalCount }}
        </p>
        <div class="flex items-center gap-1">
          <button @click="goToPage(page - 1)" :disabled="page === 1"
            class="px-3 py-1.5 text-xs rounded-lg transition disabled:opacity-30 disabled:cursor-not-allowed cursor-pointer bg-gray-100 dark:bg-gray-800 text-gray-600 dark:text-gray-300 hover:bg-gray-200 dark:hover:bg-gray-700">←</button>
          <button v-for="p in pageNumbers" :key="p" @click="goToPage(p)"
            class="w-8 h-7 text-xs rounded-lg transition cursor-pointer"
            :class="p === page ? 'bg-blue-600 text-white' : 'bg-gray-100 dark:bg-gray-800 text-gray-600 dark:text-gray-300 hover:bg-gray-200 dark:hover:bg-gray-700'">{{ p }}</button>
          <button @click="goToPage(page + 1)" :disabled="page === totalPages"
            class="px-3 py-1.5 text-xs rounded-lg transition disabled:opacity-30 disabled:cursor-not-allowed cursor-pointer bg-gray-100 dark:bg-gray-800 text-gray-600 dark:text-gray-300 hover:bg-gray-200 dark:hover:bg-gray-700">→</button>
        </div>
      </div>
    </div>
  </div>
</template>
