<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { getUsers, createUser, getUserById } from '@/services/user.service'
import { useThemeStore } from '@/stores/theme.store'
import type { UserDetail } from '@/types/user.types'
import type { CreateUserPayload } from '@/services/user.service'

const theme = useThemeStore()

// ── State ──
const users = ref<UserDetail[]>([])
const loading = ref(true)
const error = ref<string | null>(null)

const page = ref(1)
const pageSize = ref(10)
const totalCount = ref(0)
const totalPages = ref(0)
const search = ref('')
const sortBy = ref('')
const sortDesc = ref(false)
const sortField = ref<string>('')
const sortDir = ref<'asc' | 'desc'>('asc')

// Modal state
const showCreateModal = ref(false)
const showDetailModal = ref(false)
const detailLoading = ref(false)
const selectedUser = ref<UserDetail | null>(null)

// Create form
const createForm = ref<CreateUserPayload>({
  email: '', password: '', fullName: '', phone: null, roleId: 1,
})
const createLoading = ref(false)
const createError = ref('')
const createValidation = ref<Record<string, string>>({})
const optimisticUser = ref<UserDetail | null>(null)

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
  } catch (e: any) {
    error.value = e?.message || 'Failed to load users'
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
  if (sortField.value === field) {
    sortDir.value = sortDir.value === 'asc' ? 'desc' : 'asc'
  } else {
    sortField.value = field
    sortDir.value = 'asc'
  }
  page.value = 1
  fetchUsers()
}

function sortIcon(field: string): string {
  if (sortField.value !== field) return '↕'
  return sortDir.value === 'asc' ? '↑' : '↓'
}

// ── Pagination ──
function goToPage(p: number) {
  page.value = p
  fetchUsers()
  window.scrollTo({ top: 0, behavior: 'smooth' })
}

// ── Create User (optimistic) ──
function validateCreateForm(): boolean {
  const v: Record<string, string> = {}
  if (!createForm.value.email.trim()) v.email = 'Email is required'
  else if (!/\S+@\S+\.\S+/.test(createForm.value.email)) v.email = 'Invalid email format'
  if (!createForm.value.password) v.password = 'Password is required'
  else if (createForm.value.password.length < 4) v.password = 'Minimum 4 characters'
  if (!createForm.value.fullName.trim()) v.fullName = 'Full name is required'
  if (!createForm.value.roleId) v.role = 'Role is required'
  createValidation.value = v
  return Object.keys(v).length === 0
}

async function handleCreate() {
  if (!validateCreateForm()) return

  createLoading.value = true
  createError.value = ''

  // Build optimistic user
  const temp: UserDetail = {
    id: Date.now(), // temp id
    email: createForm.value.email,
    fullName: createForm.value.fullName,
    phone: createForm.value.phone,
    roleId: createForm.value.roleId,
    roleName: roles.find(r => r.id === createForm.value.roleId)?.name || 'Unknown',
    isActive: true,
    createdAt: new Date().toISOString(),
  }

  // Optimistic insert at top of current page
  users.value.unshift(temp)
  optimisticUser.value = temp
  showCreateModal.value = false

  try {
    const created = await createUser({
      email: createForm.value.email.trim(),
      password: createForm.value.password,
      fullName: createForm.value.fullName.trim(),
      phone: createForm.value.phone?.trim() || null,
      roleId: createForm.value.roleId,
    })

    // Replace optimistic entry with real data
    const idx = users.value.findIndex(u => u.id === temp.id)
    if (idx !== -1) users.value[idx] = created
    totalCount.value++
    totalPages.value = Math.ceil(totalCount.value / pageSize.value)
    optimisticUser.value = null

    // Reset form
    createForm.value = { email: '', password: '', fullName: '', phone: null, roleId: 1 }
  } catch (e: any) {
    // Remove optimistic entry on failure
    const idx = users.value.findIndex(u => u.id === temp.id)
    if (idx !== -1) users.value.splice(idx, 1)
    optimisticUser.value = null
    createError.value = e?.message || e?.error || 'Failed to create user'
    showCreateModal.value = true
  } finally {
    createLoading.value = false
  }
}

// ── Detail Modal ──
async function openDetail(id: number) {
  showDetailModal.value = true
  detailLoading.value = true
  selectedUser.value = null
  try {
    selectedUser.value = await getUserById(id)
  } catch {
    selectedUser.value = null
  } finally {
    detailLoading.value = false
  }
}

function closeDetail() {
  showDetailModal.value = false
  selectedUser.value = null
}

function resetCreateForm() {
  createForm.value = { email: '', password: '', fullName: '', phone: null, roleId: 1 }
  createValidation.value = {}
  createError.value = ''
}

// ── Roles from DB (hardcoded IDs match seeded data) ──
const roles = [
  { id: 1, name: 'Admin' },
  { id: 2, name: 'Customer' },
  { id: 3, name: 'Secretary' },
  { id: 4, name: 'Head Technician' },
  { id: 5, name: 'Field Technician' },
]
</script>

<template>
  <div>
    <!-- ── Header ── -->
    <div class="flex items-center justify-between mb-6 flex-wrap gap-4">
      <div>
        <h1 class="text-2xl font-bold text-gray-800 dark:text-gray-100">Users</h1>
        <p class="text-sm text-gray-500 dark:text-gray-400 mt-1">{{ totalCount }} total users</p>
      </div>
      <div class="flex items-center gap-3">
        <div class="relative">
          <svg class="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
            <path stroke-linecap="round" stroke-linejoin="round" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
          </svg>
          <input
            v-model="search"
            type="text"
            placeholder="Search users..."
            class="pl-10 pr-4 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 text-sm focus:ring-2 focus:ring-blue-500 focus:border-transparent outline-none transition-all w-56"
          />
        </div>
        <button
          @click="resetCreateForm(); showCreateModal = true"
          class="px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white text-sm font-medium rounded-lg transition-all duration-200 cursor-pointer flex items-center gap-2"
        >
          <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
            <path stroke-linecap="round" stroke-linejoin="round" d="M12 4v16m8-8H4" />
          </svg>
          Add User
        </button>
      </div>
    </div>

    <!-- ── Page Size Selector ── -->
    <div class="flex items-center gap-2 mb-4">
      <span class="text-xs text-gray-400 dark:text-gray-500">Show</span>
      <select
        v-model="pageSize"
        @change="page = 1; fetchUsers()"
        class="px-2 py-1 rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 text-xs focus:ring-2 focus:ring-blue-500 outline-none"
      >
        <option :value="5">5</option>
        <option :value="10">10</option>
        <option :value="20">20</option>
        <option :value="50">50</option>
      </select>
      <span class="text-xs text-gray-400 dark:text-gray-500">per page</span>
    </div>

    <!-- ── Loading State ── -->
    <div v-if="loading && users.length === 0" class="flex justify-center py-20">
      <div class="flex flex-col items-center gap-3">
        <div class="w-8 h-8 border-4 border-blue-500 border-t-transparent rounded-full animate-spin"></div>
        <p class="text-sm text-gray-400 dark:text-gray-500">Loading users...</p>
      </div>
    </div>

    <!-- ── Error State ── -->
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
              <th @click="toggleSort('name')" class="px-4 py-3 text-left font-semibold text-gray-600 dark:text-gray-400 cursor-pointer hover:text-blue-600 dark:hover:text-blue-400 transition whitespace-nowrap">
                Name <span class="text-xs ml-1">{{ sortIcon('name') }}</span>
              </th>
              <th @click="toggleSort('email')" class="px-4 py-3 text-left font-semibold text-gray-600 dark:text-gray-400 cursor-pointer hover:text-blue-600 dark:hover:text-blue-400 transition whitespace-nowrap">
                Email <span class="text-xs ml-1">{{ sortIcon('email') }}</span>
              </th>
              <th class="px-4 py-3 text-left font-semibold text-gray-600 dark:text-gray-400 whitespace-nowrap">Phone</th>
              <th @click="toggleSort('role')" class="px-4 py-3 text-left font-semibold text-gray-600 dark:text-gray-400 cursor-pointer hover:text-blue-600 dark:hover:text-blue-400 transition whitespace-nowrap">
                Role <span class="text-xs ml-1">{{ sortIcon('role') }}</span>
              </th>
              <th class="px-4 py-3 text-left font-semibold text-gray-600 dark:text-gray-400 whitespace-nowrap">Status</th>
              <th class="px-4 py-3 text-right font-semibold text-gray-600 dark:text-gray-400 whitespace-nowrap">Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr
              v-for="u in users"
              :key="u.id"
              class="border-b border-gray-50 dark:border-gray-800/50 hover:bg-gray-50 dark:hover:bg-gray-800/30 transition"
              :class="{ 'opacity-50': optimisticUser?.id === u.id }"
            >
              <td class="px-4 py-3">
                <div class="flex items-center gap-3">
                  <div class="w-8 h-8 rounded-full bg-blue-100 dark:bg-blue-900/50 flex items-center justify-center text-xs font-bold text-blue-600 dark:text-blue-400 flex-shrink-0">
                    {{ u.fullName.charAt(0).toUpperCase() }}
                  </div>
                  <div>
                    <p class="font-medium text-gray-800 dark:text-gray-200">{{ u.fullName }}</p>
                    <p v-if="optimisticUser?.id === u.id" class="text-xs text-blue-500 font-medium">Creating...</p>
                  </div>
                </div>
              </td>
              <td class="px-4 py-3 text-gray-600 dark:text-gray-400">{{ u.email }}</td>
              <td class="px-4 py-3 text-gray-600 dark:text-gray-400">{{ u.phone || '—' }}</td>
              <td class="px-4 py-3">
                <span class="px-2 py-0.5 rounded-full text-xs font-medium"
                  :class="{
                    'bg-purple-100 text-purple-700 dark:bg-purple-900/30 dark:text-purple-300': u.roleName === 'Admin',
                    'bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-300': u.roleName === 'Secretary',
                    'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-300': u.roleName === 'Head Technician',
                    'bg-orange-100 text-orange-700 dark:bg-orange-900/30 dark:text-orange-300': u.roleName === 'Field Technician',
                    'bg-gray-100 text-gray-600 dark:bg-gray-800 dark:text-gray-400': u.roleName === 'Customer',
                  }"
                >{{ u.roleName }}</span>
              </td>
              <td class="px-4 py-3">
                <span class="flex items-center gap-1.5">
                  <span :class="u.isActive ? 'bg-green-500' : 'bg-gray-400'" class="w-2 h-2 rounded-full inline-block"></span>
                  <span class="text-xs text-gray-500 dark:text-gray-400">{{ u.isActive ? 'Active' : 'Inactive' }}</span>
                </span>
              </td>
              <td class="px-4 py-3 text-right">
                <button
                  @click="openDetail(u.id)"
                  class="px-3 py-1.5 text-xs font-medium text-blue-600 dark:text-blue-400 bg-blue-50 dark:bg-blue-900/20 hover:bg-blue-100 dark:hover:bg-blue-900/40 rounded-lg transition cursor-pointer"
                >
                  More Info
                </button>
              </td>
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
            class="px-3 py-1.5 text-xs rounded-lg transition disabled:opacity-30 disabled:cursor-not-allowed cursor-pointer
            bg-gray-100 dark:bg-gray-800 text-gray-600 dark:text-gray-300 hover:bg-gray-200 dark:hover:bg-gray-700">
            ←
          </button>
          <button
            v-for="p in pageNumbers"
            :key="p"
            @click="goToPage(p)"
            class="w-8 h-7 text-xs rounded-lg transition cursor-pointer"
            :class="p === page ? 'bg-blue-600 text-white' : 'bg-gray-100 dark:bg-gray-800 text-gray-600 dark:text-gray-300 hover:bg-gray-200 dark:hover:bg-gray-700'"
          >{{ p }}</button>
          <button @click="goToPage(page + 1)" :disabled="page === totalPages"
            class="px-3 py-1.5 text-xs rounded-lg transition disabled:opacity-30 disabled:cursor-not-allowed cursor-pointer
            bg-gray-100 dark:bg-gray-800 text-gray-600 dark:text-gray-300 hover:bg-gray-200 dark:hover:bg-gray-700">
            →
          </button>
        </div>
      </div>
    </div>

    <!-- ── Create User Modal ── -->
    <Teleport to="body">
      <div v-if="showCreateModal" class="fixed inset-0 z-50 flex items-center justify-center p-4" @click.self="createError ? null : (showCreateModal = false)">
        <div class="absolute inset-0 bg-black/50 backdrop-blur-sm" @click="createError ? null : (showCreateModal = false)"></div>
        <div class="relative bg-white dark:bg-gray-900 rounded-2xl shadow-2xl w-full max-w-md max-h-[90vh] overflow-y-auto animate-modal-in p-6">
          <div class="flex items-center justify-between mb-5">
            <h2 class="text-lg font-bold text-gray-800 dark:text-gray-100">Add New User</h2>
            <button @click="showCreateModal = false" class="w-7 h-7 rounded-full bg-gray-100 dark:bg-gray-800 flex items-center justify-center text-gray-400 hover:bg-gray-200 dark:hover:bg-gray-700 cursor-pointer text-sm">✕</button>
          </div>

          <form @submit.prevent="handleCreate" class="space-y-4">
            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Full Name *</label>
              <input v-model="createForm.fullName" type="text" placeholder="John Kamau"
                class="w-full px-3 py-2 rounded-lg border text-sm focus:ring-2 focus:ring-blue-500 outline-none transition"
                :class="createValidation.fullName ? 'border-red-400' : 'border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100'"
              />
              <p v-if="createValidation.fullName" class="text-xs text-red-500 mt-1">{{ createValidation.fullName }}</p>
            </div>

            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Email *</label>
              <input v-model="createForm.email" type="email" placeholder="john@example.com"
                class="w-full px-3 py-2 rounded-lg border text-sm focus:ring-2 focus:ring-blue-500 outline-none transition"
                :class="createValidation.email ? 'border-red-400' : 'border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100'"
              />
              <p v-if="createValidation.email" class="text-xs text-red-500 mt-1">{{ createValidation.email }}</p>
            </div>

            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Phone</label>
              <input v-model="createForm.phone" type="tel" placeholder="+254 712 345 678"
                class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 text-sm focus:ring-2 focus:ring-blue-500 outline-none transition"
              />
            </div>

            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Password *</label>
              <input v-model="createForm.password" type="password" placeholder="••••••••"
                class="w-full px-3 py-2 rounded-lg border text-sm focus:ring-2 focus:ring-blue-500 outline-none transition"
                :class="createValidation.password ? 'border-red-400' : 'border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100'"
              />
              <p v-if="createValidation.password" class="text-xs text-red-500 mt-1">{{ createValidation.password }}</p>
            </div>

            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Role *</label>
              <select v-model="createForm.roleId"
                class="w-full px-3 py-2 rounded-lg border text-sm focus:ring-2 focus:ring-blue-500 outline-none transition"
                :class="createValidation.role ? 'border-red-400' : 'border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100'"
              >
                <option v-for="r in roles" :key="r.id" :value="r.id">{{ r.name }}</option>
              </select>
              <p v-if="createValidation.role" class="text-xs text-red-500 mt-1">{{ createValidation.role }}</p>
            </div>

            <p v-if="createError" class="text-sm text-red-600 dark:text-red-400 bg-red-50 dark:bg-red-900/20 rounded-lg px-3 py-2">{{ createError }}</p>

            <div class="flex gap-3 pt-2">
              <button type="button" @click="showCreateModal = false"
                class="flex-1 px-4 py-2.5 text-sm font-medium text-gray-600 dark:text-gray-300 bg-gray-100 dark:bg-gray-800 hover:bg-gray-200 dark:hover:bg-gray-700 rounded-lg transition cursor-pointer">
                Cancel
              </button>
              <button type="submit" :disabled="createLoading"
                class="flex-1 px-4 py-2.5 text-sm font-medium text-white bg-blue-600 hover:bg-blue-700 disabled:bg-blue-400 rounded-lg transition flex items-center justify-center gap-2 cursor-pointer">
                <span v-if="createLoading" class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
                {{ createLoading ? 'Creating...' : 'Create User' }}
              </button>
            </div>
          </form>
        </div>
      </div>
    </Teleport>

    <!-- ── Detail Modal ── -->
    <Teleport to="body">
      <div v-if="showDetailModal" class="fixed inset-0 z-50 flex items-center justify-center p-4" @click.self="closeDetail">
        <div class="absolute inset-0 bg-black/50 backdrop-blur-sm" @click="closeDetail"></div>
        <div class="relative bg-white dark:bg-gray-900 rounded-2xl shadow-2xl w-full max-w-md animate-modal-in p-6">
          <div class="flex items-center justify-between mb-5">
            <h2 class="text-lg font-bold text-gray-800 dark:text-gray-100">User Details</h2>
            <button @click="closeDetail" class="w-7 h-7 rounded-full bg-gray-100 dark:bg-gray-800 flex items-center justify-center text-gray-400 hover:bg-gray-200 dark:hover:bg-gray-700 cursor-pointer text-sm">✕</button>
          </div>

          <div v-if="detailLoading" class="flex justify-center py-10">
            <div class="w-8 h-8 border-4 border-blue-500 border-t-transparent rounded-full animate-spin"></div>
          </div>

          <div v-else-if="selectedUser" class="space-y-4">
            <div class="flex items-center gap-4 pb-4 border-b border-gray-100 dark:border-gray-800">
              <div class="w-14 h-14 rounded-full bg-blue-100 dark:bg-blue-900/50 flex items-center justify-center text-xl font-bold text-blue-600 dark:text-blue-400">
                {{ selectedUser.fullName.charAt(0).toUpperCase() }}
              </div>
              <div>
                <p class="text-lg font-bold text-gray-800 dark:text-gray-100">{{ selectedUser.fullName }}</p>
                <p class="text-sm text-gray-500 dark:text-gray-400">{{ selectedUser.roleName }}</p>
              </div>
            </div>

            <div class="space-y-3">
              <div class="flex justify-between py-2 border-b border-gray-50 dark:border-gray-800/50">
                <span class="text-sm text-gray-500 dark:text-gray-400">User ID</span>
                <span class="text-sm font-medium text-gray-800 dark:text-gray-200">{{ selectedUser.id }}</span>
              </div>
              <div class="flex justify-between py-2 border-b border-gray-50 dark:border-gray-800/50">
                <span class="text-sm text-gray-500 dark:text-gray-400">Email</span>
                <span class="text-sm font-medium text-gray-800 dark:text-gray-200">{{ selectedUser.email }}</span>
              </div>
              <div class="flex justify-between py-2 border-b border-gray-50 dark:border-gray-800/50">
                <span class="text-sm text-gray-500 dark:text-gray-400">Phone</span>
                <span class="text-sm font-medium text-gray-800 dark:text-gray-200">{{ selectedUser.phone || '—' }}</span>
              </div>
              <div class="flex justify-between py-2 border-b border-gray-50 dark:border-gray-800/50">
                <span class="text-sm text-gray-500 dark:text-gray-400">Role</span>
                <span class="text-sm font-medium">
                  <span class="px-2 py-0.5 rounded-full text-xs font-medium"
                    :class="{
                      'bg-purple-100 text-purple-700 dark:bg-purple-900/30 dark:text-purple-300': selectedUser.roleName === 'Admin',
                      'bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-300': selectedUser.roleName === 'Secretary',
                      'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-300': selectedUser.roleName === 'Head Technician',
                      'bg-orange-100 text-orange-700 dark:bg-orange-900/30 dark:text-orange-300': selectedUser.roleName === 'Field Technician',
                      'bg-gray-100 text-gray-600 dark:bg-gray-800 dark:text-gray-400': selectedUser.roleName === 'Customer',
                    }"
                  >{{ selectedUser.roleName }}</span>
                </span>
              </div>
              <div class="flex justify-between py-2 border-b border-gray-50 dark:border-gray-800/50">
                <span class="text-sm text-gray-500 dark:text-gray-400">Status</span>
                <span class="text-sm font-medium" :class="selectedUser.isActive ? 'text-green-600 dark:text-green-400' : 'text-gray-400'">
                  {{ selectedUser.isActive ? 'Active' : 'Inactive' }}
                </span>
              </div>
              <div class="flex justify-between py-2">
                <span class="text-sm text-gray-500 dark:text-gray-400">Created</span>
                <span class="text-sm font-medium text-gray-800 dark:text-gray-200">{{ new Date(selectedUser.createdAt).toLocaleDateString() }}</span>
              </div>
            </div>
          </div>
        </div>
      </div>
    </Teleport>
  </div>
</template>

<style scoped>
@keyframes modalIn {
  from { opacity: 0; transform: scale(0.95) translateY(10px); }
  to { opacity: 1; transform: scale(1) translateY(0); }
}
.animate-modal-in { animation: modalIn 0.2s ease-out forwards; }
</style>
