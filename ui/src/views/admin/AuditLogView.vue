<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { getAuditLogs } from '@/services/audit.service'
import { actionLabel, entityTypeLabel } from '@/services/audit.service'
import { formatDateTime } from '@/utils/format'
import AuditEntryDetailModal from '@/components/AuditEntryDetailModal.vue'
import type { AuditLogListItem, AuditAction } from '@/types/audit.types'

const items = ref<AuditLogListItem[]>([])
const loading = ref(true)
const error = ref<string | null>(null)

const page = ref(1)
const pageSize = ref(20)
const totalCount = ref(0)
const totalPages = ref(0)
const search = ref('')
const entityFilter = ref('all')
const actionFilter = ref<'all' | AuditAction>('all')
const sortField = ref<string>('created')
const sortDir = ref<'asc' | 'desc'>('desc')

// ── Detail modal ──
const showDetail = ref(false)
const detailId = ref<number | null>(null)

const pageNumbers = computed(() => {
  const pages: number[] = []
  for (let i = 1; i <= totalPages.value; i++) pages.push(i)
  return pages
})

const entityOptions = [
  'all', 'auth', 'customer', 'staff', 'user', 'role', 'plan', 'nas', 'setting', 'organization',
]

async function fetchLogs() {
  loading.value = true
  error.value = null
  try {
    const result = await getAuditLogs({
      page: page.value,
      pageSize: pageSize.value,
      search: search.value || undefined,
      entityType: entityFilter.value === 'all' ? undefined : entityFilter.value,
      action: actionFilter.value === 'all' ? undefined : actionFilter.value,
      sortBy: sortField.value,
      sortDesc: sortDir.value === 'desc',
    })
    items.value = result.items
    totalCount.value = result.totalCount
    totalPages.value = result.totalPages
  } catch (e: unknown) {
    error.value = errMsg(e, 'Failed to load audit log')
  } finally {
    loading.value = false
  }
}

onMounted(fetchLogs)

// ── Search debounce ──
let debounceTimer: ReturnType<typeof setTimeout> | null = null
watch(search, () => {
  if (debounceTimer) clearTimeout(debounceTimer)
  debounceTimer = setTimeout(() => {
    page.value = 1
    fetchLogs()
  }, 300)
})

// ── Filters ──
function onFilterChange() {
  page.value = 1
  fetchLogs()
}

// ── Sort ──
function toggleSort(field: string) {
  if (sortField.value === field) sortDir.value = sortDir.value === 'asc' ? 'desc' : 'asc'
  else { sortField.value = field; sortDir.value = 'desc' }
  page.value = 1
  fetchLogs()
}
function sortIcon(field: string) {
  if (sortField.value !== field) return '↕'
  return sortDir.value === 'asc' ? '↑' : '↓'
}

// ── Pagination ──
function goToPage(p: number) {
  page.value = p
  fetchLogs()
}
function clearFilters() {
  search.value = ''
  entityFilter.value = 'all'
  actionFilter.value = 'all'
  page.value = 1
  fetchLogs()
}

function errMsg(e: unknown, fallback: string): string {
  if (e && typeof e === 'object') {
    const msg = (e as { message?: unknown }).message
    if (typeof msg === 'string' && msg) return msg
    const err = (e as { error?: unknown }).error
    if (typeof err === 'string' && err) return err
  }
  return fallback
}

function actionClass(action: string): string {
  switch (action) {
    case 'create': return 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-300'
    case 'update': return 'bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-300'
    case 'delete': return 'bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-300'
    case 'login_success': return 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-300'
    case 'login_failure': return 'bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-300'
    default: return 'bg-gray-100 text-gray-600 dark:bg-gray-800 dark:text-gray-400'
  }
}

function actorName(a: AuditLogListItem['actor']): string {
  if (!a || a.type === 'anonymous') return 'Anonymous'
  return a.fullName || a.type.charAt(0).toUpperCase() + a.type.slice(1)
}

function openDetail(id: number) {
  detailId.value = id
  showDetail.value = true
}
function closeDetail() {
  showDetail.value = false
  detailId.value = null
}
</script>

<template>
  <div>
    <!-- ── Header ── -->
    <div class="flex items-center justify-between mb-6 flex-wrap gap-4">
      <div>
        <h1 class="text-2xl font-bold text-gray-800 dark:text-gray-100">Audit Log</h1>
        <p class="text-sm text-gray-500 dark:text-gray-400 mt-1">{{ totalCount }} recorded events</p>
      </div>
      <div class="flex items-center gap-3 flex-wrap">
        <div class="relative">
          <svg class="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
            <path stroke-linecap="round" stroke-linejoin="round" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
          </svg>
          <input v-model="search" type="text" placeholder="Search summary, entity, IP..."
            class="pl-10 pr-4 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 text-sm focus:ring-2 focus:ring-blue-500 outline-none transition w-64" />
        </div>
        <select v-model="entityFilter" @change="onFilterChange"
          class="px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 text-sm focus:ring-2 focus:ring-blue-500 outline-none transition cursor-pointer">
          <option value="all">All entities</option>
          <option v-for="e in entityOptions.slice(1)" :key="e" :value="e">{{ entityTypeLabel(e) }}</option>
        </select>
        <select v-model="actionFilter" @change="onFilterChange"
          class="px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 text-sm focus:ring-2 focus:ring-blue-500 outline-none transition cursor-pointer">
          <option value="all">All actions</option>
          <option value="create">Created</option>
          <option value="update">Updated</option>
          <option value="delete">Deleted</option>
          <option value="login_success">Login success</option>
          <option value="login_failure">Login failed</option>
        </select>
      </div>
    </div>

    <!-- ── Loading ── -->
    <div v-if="loading && items.length === 0" class="flex justify-center py-20">
      <div class="flex flex-col items-center gap-3">
        <div class="w-8 h-8 border-4 border-blue-500 border-t-transparent rounded-full animate-spin"></div>
        <p class="text-sm text-gray-400 dark:text-gray-500">Loading audit log...</p>
      </div>
    </div>

    <!-- ── Error ── -->
    <div v-else-if="error && items.length === 0" class="text-center py-20">
      <div class="bg-red-50 dark:bg-red-900/30 border border-red-200 dark:border-red-800 rounded-xl p-6 max-w-md mx-auto">
        <p class="text-red-600 dark:text-red-400 font-medium">{{ error }}</p>
        <button @click="fetchLogs" class="mt-3 px-4 py-2 bg-red-600 hover:bg-red-700 text-white text-sm rounded-lg transition cursor-pointer">Retry</button>
      </div>
    </div>

    <!-- ── Table ── -->
    <div v-else class="bg-white dark:bg-gray-900 rounded-xl shadow-sm border border-gray-100 dark:border-gray-800 overflow-hidden">
      <div class="overflow-x-auto">
        <table class="w-full text-sm">
          <thead>
            <tr class="border-b border-gray-100 dark:border-gray-800 bg-gray-50 dark:bg-gray-800/50">
              <th @click="toggleSort('created')" class="px-4 py-3 text-left font-semibold text-gray-600 dark:text-gray-400 cursor-pointer hover:text-blue-600 dark:hover:text-blue-400 transition whitespace-nowrap">Date &amp; Time <span class="text-xs ml-1">{{ sortIcon('created') }}</span></th>
              <th @click="toggleSort('action')" class="px-4 py-3 text-left font-semibold text-gray-600 dark:text-gray-400 cursor-pointer hover:text-blue-600 dark:hover:text-blue-400 transition whitespace-nowrap">Action <span class="text-xs ml-1">{{ sortIcon('action') }}</span></th>
              <th @click="toggleSort('entity')" class="px-4 py-3 text-left font-semibold text-gray-600 dark:text-gray-400 cursor-pointer hover:text-blue-600 dark:hover:text-blue-400 transition whitespace-nowrap">Entity <span class="text-xs ml-1">{{ sortIcon('entity') }}</span></th>
              <th class="px-4 py-3 text-left font-semibold text-gray-600 dark:text-gray-400 whitespace-nowrap hidden md:table-cell">Summary</th>
              <th class="px-4 py-3 text-left font-semibold text-gray-600 dark:text-gray-400 whitespace-nowrap hidden sm:table-cell">Actor</th>
              <th class="px-4 py-3 text-left font-semibold text-gray-600 dark:text-gray-400 whitespace-nowrap hidden lg:table-cell">IP</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="item in items" :key="item.id"
              @click="openDetail(item.id)"
              class="border-b border-gray-50 dark:border-gray-800/50 hover:bg-gray-50 dark:hover:bg-gray-800/30 transition cursor-pointer">
              <td class="px-4 py-3 text-gray-600 dark:text-gray-400 whitespace-nowrap">{{ formatDateTime(item.createdAt) }}</td>
              <td class="px-4 py-3">
                <span :class="actionClass(item.action)" class="px-2 py-0.5 rounded-full text-xs font-medium capitalize whitespace-nowrap">{{ actionLabel(item.action) }}</span>
              </td>
              <td class="px-4 py-3">
                <span class="font-medium text-gray-800 dark:text-gray-200">{{ entityTypeLabel(item.entityType) }}</span>
                <span v-if="item.entityId" class="text-xs text-gray-400 dark:text-gray-500 ml-1">#{{ item.entityId }}</span>
              </td>
              <td class="px-4 py-3 text-gray-600 dark:text-gray-400 hidden md:table-cell max-w-[300px]">
                <p class="truncate">{{ item.summary }}</p>
              </td>
              <td class="px-4 py-3 text-gray-600 dark:text-gray-400 hidden sm:table-cell whitespace-nowrap">{{ actorName(item.actor) }}</td>
              <td class="px-4 py-3 text-gray-500 dark:text-gray-500 hidden lg:table-cell font-mono text-xs">{{ item.ipAddress || '—' }}</td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- ── Empty State ── -->
      <div v-if="items.length === 0 && !loading && !error" class="text-center py-16">
        <p class="text-gray-400 dark:text-gray-500">No audit events match your filters</p>
        <button @click="clearFilters" class="mt-2 text-xs text-blue-600 dark:text-blue-400 hover:underline cursor-pointer">Clear filters</button>
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

    <!-- ── Detail Modal ── -->
    <AuditEntryDetailModal
      :open="showDetail"
      :audit-id="detailId"
      :z-index="60"
      @close="closeDetail"
    />
  </div>
</template>
