<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { getCustomers, getCustomerById, createCustomer } from '@/services/customer.service'
import type { CustomerSummary, CustomerDetail } from '@/types/customer.types'
import type { CreateCustomerPayload } from '@/services/customer.service'

// ── State ──
const customers = ref<CustomerSummary[]>([])
const loading = ref(true)
const error = ref<string | null>(null)
const page = ref(1)
const pageSize = ref(10)
const totalCount = ref(0)
const totalPages = ref(0)
const search = ref('')
const sortField = ref('')
const sortDir = ref<'asc' | 'desc'>('asc')

// Detail modal
const showDetail = ref(false)
const detailLoading = ref(false)
const selectedCustomer = ref<CustomerDetail | null>(null)

// ── Computed ──
const pageNumbers = computed(() => {
  const pages: number[] = []
  for (let i = 1; i <= totalPages.value; i++) pages.push(i)
  return pages
})

// Create modal state
const showCreateModal = ref(false)
const createForm = ref<CreateCustomerPayload>({
  email: '', password: '', fullName: '', phone: '',
  businessName: null, customerType: 'residential',
  serviceAddress: null, city: null, region: null,
})
const createLoading = ref(false)
const createError = ref('')
const createValidation = ref<Record<string, string>>({})
const optimisticCustomer = ref<CustomerSummary | null>(null)

// ── Fetch ──
async function fetchCustomers() {
  loading.value = true
  error.value = null
  try {
    const result = await getCustomers(page.value, pageSize.value, search.value || undefined, sortField.value || undefined, sortDir.value === 'desc')
    customers.value = result.items
    totalCount.value = result.totalCount
    totalPages.value = result.totalPages
  } catch (e: any) {
    error.value = e?.message || 'Failed to load customers'
  } finally {
    loading.value = false
  }
}

onMounted(fetchCustomers)

// ── Search debounce ──
let deb: ReturnType<typeof setTimeout> | null = null
watch(search, () => {
  if (deb) clearTimeout(deb)
  deb = setTimeout(() => { page.value = 1; fetchCustomers() }, 300)
})

// ── Sort ──
function toggleSort(field: string) {
  if (sortField.value === field) sortDir.value = sortDir.value === 'asc' ? 'desc' : 'asc'
  else { sortField.value = field; sortDir.value = 'asc' }
  page.value = 1; fetchCustomers()
}
function sortIcon(field: string) {
  if (sortField.value !== field) return '↕'
  return sortDir.value === 'asc' ? '↑' : '↓'
}

// ── Pagination ──
function goToPage(p: number) {
  page.value = p; fetchCustomers(); window.scrollTo({ top: 0, behavior: 'smooth' })
}

// ── Status helpers ──
function statusClass(s: string) {
  switch (s) {
    case 'active': return 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-300'
    case 'inactive': return 'bg-yellow-100 text-yellow-700 dark:bg-yellow-900/30 dark:text-yellow-300'
    case 'suspended': return 'bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-300'
    case 'blacklisted': return 'bg-gray-200 text-gray-700 dark:bg-gray-700 dark:text-gray-300'
    default: return 'bg-gray-100 text-gray-600 dark:bg-gray-800 dark:text-gray-400'
  }
}
function statusDot(s: string) {
  switch (s) {
    case 'active': return 'bg-green-500'
    case 'inactive': return 'bg-yellow-500'
    case 'suspended': return 'bg-red-500'
    default: return 'bg-gray-400'
  }
}

// ── Create Customer ──
function validateCreateForm(): boolean {
  const v: Record<string, string> = {}
  if (!createForm.value.email.trim()) v.email = 'Email is required'
  else if (!/\S+@\S+\.\S+/.test(createForm.value.email)) v.email = 'Invalid email'
  if (!createForm.value.password || createForm.value.password.length < 4) v.password = 'Minimum 4 characters'
  if (!createForm.value.fullName.trim()) v.fullName = 'Full name is required'
  if (!createForm.value.phone.trim()) v.phone = 'Phone is required'
  createValidation.value = v
  return Object.keys(v).length === 0
}

async function handleCreate() {
  if (!validateCreateForm()) return
  createLoading.value = true
  createError.value = ''

  const temp: CustomerSummary = {
    id: Date.now(), userId: 0,
    customerCode: '...',
    fullName: createForm.value.fullName,
    businessName: createForm.value.businessName,
    customerType: createForm.value.customerType,
    email: createForm.value.email,
    phone: createForm.value.phone,
    city: createForm.value.city,
    region: createForm.value.region,
    status: 'active',
    createdAt: new Date().toISOString(),
  }

  customers.value.unshift(temp)
  optimisticCustomer.value = temp
  showCreateModal.value = false

  try {
    const created = await createCustomer({
      email: createForm.value.email.trim(),
      password: createForm.value.password,
      fullName: createForm.value.fullName.trim(),
      phone: createForm.value.phone.trim(),
      businessName: createForm.value.businessName?.trim() || null,
      customerType: createForm.value.customerType,
      serviceAddress: createForm.value.serviceAddress?.trim() || null,
      city: createForm.value.city?.trim() || null,
      region: createForm.value.region?.trim() || null,
    })

    const idx = customers.value.findIndex(c => c.id === temp.id)
    if (idx !== -1) customers.value[idx] = created
    totalCount.value++
    totalPages.value = Math.ceil(totalCount.value / pageSize.value)
    optimisticCustomer.value = null
    createForm.value = {
      email: '', password: '', fullName: '', phone: '',
      businessName: null, customerType: 'residential',
      serviceAddress: null, city: null, region: null,
    }
  } catch (e: any) {
    const idx = customers.value.findIndex(c => c.id === temp.id)
    if (idx !== -1) customers.value.splice(idx, 1)
    optimisticCustomer.value = null
    createError.value = e?.message || e?.error || 'Failed to create customer'
    showCreateModal.value = true
  } finally {
    createLoading.value = false
  }
}

// ── Detail ──
async function openDetail(id: number) {
  showDetail.value = true
  detailLoading.value = true
  selectedCustomer.value = null
  try { selectedCustomer.value = await getCustomerById(id) }
  catch { selectedCustomer.value = null }
  finally { detailLoading.value = false }
}
function closeDetail() {
  showDetail.value = false; selectedCustomer.value = null
}

function subStatusClass(s: string) {
  switch (s) {
    case 'active': return 'text-green-600 dark:text-green-400'
    case 'expired': return 'text-red-600 dark:text-red-400'
    case 'suspended': return 'text-yellow-600 dark:text-yellow-400'
    default: return 'text-gray-500'
  }
}
</script>

<template>
  <div>
    <!-- ── Header ── -->
    <div class="flex items-center justify-between mb-6 flex-wrap gap-4">
      <div>
        <h1 class="text-2xl font-bold text-gray-800 dark:text-gray-100">Customers</h1>
        <p class="text-sm text-gray-500 dark:text-gray-400 mt-1">{{ totalCount }} total customers</p>
      </div>
      <div class="flex items-center gap-3">
        <div class="relative">
          <svg class="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
            <path stroke-linecap="round" stroke-linejoin="round" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
          </svg>
          <input v-model="search" type="text" placeholder="Search customers..."
            class="pl-10 pr-4 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 text-sm focus:ring-2 focus:ring-blue-500 outline-none transition w-56" />
        </div>
        <button @click="showCreateModal = true"
          class="px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white text-sm font-medium rounded-lg transition-all duration-200 cursor-pointer flex items-center gap-2 whitespace-nowrap">
          <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
            <path stroke-linecap="round" stroke-linejoin="round" d="M12 4v16m8-8H4" />
          </svg>
          Add Customer
        </button>
      </div>
    </div>

    <!-- ── Page Size ── -->
    <div class="flex items-center gap-2 mb-4">
      <span class="text-xs text-gray-400 dark:text-gray-500">Show</span>
      <select v-model="pageSize" @change="page = 1; fetchCustomers()"
        class="px-2 py-1 rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 text-xs outline-none">
        <option :value="5">5</option>
        <option :value="10">10</option>
        <option :value="20">20</option>
        <option :value="50">50</option>
      </select>
      <span class="text-xs text-gray-400 dark:text-gray-500">per page</span>
    </div>

    <!-- ── Loading ── -->
    <div v-if="loading && customers.length === 0" class="flex justify-center py-20">
      <div class="flex flex-col items-center gap-3">
        <div class="w-8 h-8 border-4 border-blue-500 border-t-transparent rounded-full animate-spin"></div>
        <p class="text-sm text-gray-400 dark:text-gray-500">Loading customers...</p>
      </div>
    </div>

    <!-- ── Error ── -->
    <div v-else-if="error && customers.length === 0" class="text-center py-20">
      <div class="bg-red-50 dark:bg-red-900/30 border border-red-200 dark:border-red-800 rounded-xl p-6 max-w-md mx-auto">
        <p class="text-red-600 dark:text-red-400 font-medium">{{ error }}</p>
        <button @click="fetchCustomers" class="mt-3 px-4 py-2 bg-red-600 hover:bg-red-700 text-white text-sm rounded-lg transition cursor-pointer">Retry</button>
      </div>
    </div>

    <!-- ── Table ── -->
    <div v-else class="bg-white dark:bg-gray-900 rounded-xl shadow-sm border border-gray-100 dark:border-gray-800 overflow-hidden">
      <div class="overflow-x-auto">
        <table class="w-full text-sm">
          <thead>
            <tr class="border-b border-gray-100 dark:border-gray-800 bg-gray-50 dark:bg-gray-800/50">
              <th @click="toggleSort('code')" class="px-4 py-3 text-left font-semibold text-gray-600 dark:text-gray-400 cursor-pointer hover:text-blue-600 dark:hover:text-blue-400 transition whitespace-nowrap">Code <span class="text-xs ml-1">{{ sortIcon('code') }}</span></th>
              <th @click="toggleSort('name')" class="px-4 py-3 text-left font-semibold text-gray-600 dark:text-gray-400 cursor-pointer hover:text-blue-600 dark:hover:text-blue-400 transition whitespace-nowrap">Name <span class="text-xs ml-1">{{ sortIcon('name') }}</span></th>
              <th class="px-4 py-3 text-left font-semibold text-gray-600 dark:text-gray-400 whitespace-nowrap">Phone</th>
              <th @click="toggleSort('email')" class="px-4 py-3 text-left font-semibold text-gray-600 dark:text-gray-400 cursor-pointer hover:text-blue-600 dark:hover:text-blue-400 transition whitespace-nowrap hidden sm:table-cell">Email <span class="text-xs ml-1">{{ sortIcon('email') }}</span></th>
              <th @click="toggleSort('city')" class="px-4 py-3 text-left font-semibold text-gray-600 dark:text-gray-400 cursor-pointer hover:text-blue-600 dark:hover:text-blue-400 transition whitespace-nowrap hidden md:table-cell">City <span class="text-xs ml-1">{{ sortIcon('city') }}</span></th>
              <th @click="toggleSort('status')" class="px-4 py-3 text-left font-semibold text-gray-600 dark:text-gray-400 cursor-pointer hover:text-blue-600 dark:hover:text-blue-400 transition whitespace-nowrap">Status <span class="text-xs ml-1">{{ sortIcon('status') }}</span></th>
              <th class="px-4 py-3 text-right font-semibold text-gray-600 dark:text-gray-400 whitespace-nowrap">Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="c in customers" :key="c.id"
              class="border-b border-gray-50 dark:border-gray-800/50 hover:bg-gray-50 dark:hover:bg-gray-800/30 transition">
              <td class="px-4 py-3 font-mono text-xs text-gray-500 dark:text-gray-400">{{ c.customerCode }}</td>
              <td class="px-4 py-3">
                <div class="flex items-center gap-3">
                  <div class="w-8 h-8 rounded-full bg-blue-100 dark:bg-blue-900/50 flex items-center justify-center text-xs font-bold text-blue-600 dark:text-blue-400 flex-shrink-0">{{ c.fullName.charAt(0).toUpperCase() }}</div>
                  <div>
                    <p class="font-medium text-gray-800 dark:text-gray-200">{{ c.fullName }}</p>
                    <p v-if="c.businessName" class="text-xs text-gray-400 dark:text-gray-500">{{ c.businessName }}</p>
                  </div>
                </div>
              </td>
              <td class="px-4 py-3 text-gray-600 dark:text-gray-400 font-mono text-xs">{{ c.phone }}</td>
              <td class="px-4 py-3 text-gray-600 dark:text-gray-400 hidden sm:table-cell">{{ c.email || '—' }}</td>
              <td class="px-4 py-3 text-gray-500 dark:text-gray-400 hidden md:table-cell">{{ c.city || '—' }}</td>
              <td class="px-4 py-3">
                <div class="flex items-center gap-1.5">
                  <span :class="statusDot(c.status)" class="w-2 h-2 rounded-full inline-block"></span>
                  <span :class="statusClass(c.status)" class="px-2 py-0.5 rounded-full text-xs font-medium">{{ c.status }}</span>
                </div>
              </td>
              <td class="px-4 py-3 text-right">
                <button @click="openDetail(c.id)"
                  class="px-3 py-1.5 text-xs font-medium text-blue-600 dark:text-blue-400 bg-blue-50 dark:bg-blue-900/20 hover:bg-blue-100 dark:hover:bg-blue-900/40 rounded-lg transition cursor-pointer">
                  More Info
                </button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <div v-if="customers.length === 0 && !loading && !error" class="text-center py-16">
        <p class="text-gray-400 dark:text-gray-500">No customers found</p>
      </div>

      <!-- ── Pagination ── -->
      <div v-if="totalPages > 1" class="flex items-center justify-between px-4 py-3 border-t border-gray-100 dark:border-gray-800 bg-gray-50 dark:bg-gray-800/50">
        <p class="text-xs text-gray-400 dark:text-gray-500">Showing {{ ((page - 1) * pageSize) + 1 }}–{{ Math.min(page * pageSize, totalCount) }} of {{ totalCount }}</p>
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
    <Teleport to="body">
      <div v-if="showDetail" class="fixed inset-0 z-50 flex items-center justify-center p-4" @click.self="closeDetail">
        <div class="absolute inset-0 bg-black/50 backdrop-blur-sm" @click="closeDetail"></div>
        <div class="relative bg-white dark:bg-gray-900 rounded-2xl shadow-2xl w-full max-w-2xl max-h-[90vh] overflow-y-auto animate-modal-in p-6 sm:p-8">

          <!-- Close -->
          <button @click="closeDetail"
            class="absolute top-4 right-4 w-7 h-7 rounded-full bg-gray-100 dark:bg-gray-800 flex items-center justify-center text-gray-400 hover:bg-gray-200 dark:hover:bg-gray-700 cursor-pointer text-sm">✕</button>

          <!-- Loading -->
          <div v-if="detailLoading" class="flex justify-center py-16">
            <div class="w-8 h-8 border-4 border-blue-500 border-t-transparent rounded-full animate-spin"></div>
          </div>

          <!-- Content -->
          <div v-else-if="selectedCustomer" class="space-y-6">

            <!-- Header -->
            <div class="flex items-center gap-4 pb-4 border-b border-gray-100 dark:border-gray-800">
              <div class="w-14 h-14 rounded-full bg-blue-100 dark:bg-blue-900/50 flex items-center justify-center text-xl font-bold text-blue-600 dark:text-blue-400">
                {{ selectedCustomer.fullName.charAt(0).toUpperCase() }}
              </div>
              <div>
                <p class="text-lg font-bold text-gray-800 dark:text-gray-100">{{ selectedCustomer.fullName }}</p>
                <p class="text-xs text-gray-400 dark:text-gray-500 font-mono">{{ selectedCustomer.customerCode }}</p>
                <div class="flex items-center gap-2 mt-1">
                  <span :class="statusDot(selectedCustomer.status)" class="w-2 h-2 rounded-full inline-block"></span>
                  <span class="text-sm font-medium capitalize" :class="{
                    'text-green-600 dark:text-green-400': selectedCustomer.status === 'active',
                    'text-yellow-600 dark:text-yellow-400': selectedCustomer.status === 'inactive',
                    'text-red-600 dark:text-red-400': selectedCustomer.status === 'suspended',
                  }">{{ selectedCustomer.status }}</span>
                </div>
              </div>
            </div>

            <!-- Contact Details -->
            <div>
              <h3 class="text-sm font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider mb-3">Contact Details</h3>
              <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
                <div class="bg-gray-50 dark:bg-gray-800 rounded-lg p-3 sm:col-span-2">
                  <p class="text-xs text-gray-400 dark:text-gray-500 mb-1">Phone</p>
                  <p class="font-medium text-gray-800 dark:text-gray-200">{{ selectedCustomer.phone }}</p>
                </div>
                <div class="bg-gray-50 dark:bg-gray-800 rounded-lg p-3 sm:col-span-2">
                  <p class="text-xs text-gray-400 dark:text-gray-500 mb-1">Email</p>
                  <p class="font-medium text-gray-800 dark:text-gray-200">{{ selectedCustomer.email || '—' }}</p>
                </div>
              </div>
            </div>

            <!-- Address -->
            <div>
              <h3 class="text-sm font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider mb-3">Address</h3>
              <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
                <div class="bg-gray-50 dark:bg-gray-800 rounded-lg p-3 sm:col-span-2">
                  <p class="text-xs text-gray-400 dark:text-gray-500 mb-1">Service Address</p>
                  <p class="font-medium text-gray-800 dark:text-gray-200">{{ selectedCustomer.serviceAddress || '—' }}</p>
                </div>
                <div class="bg-gray-50 dark:bg-gray-800 rounded-lg p-3">
                  <p class="text-xs text-gray-400 dark:text-gray-500 mb-1">City</p>
                  <p class="font-medium text-gray-800 dark:text-gray-200">{{ selectedCustomer.city || '—' }}</p>
                </div>
                <div class="bg-gray-50 dark:bg-gray-800 rounded-lg p-3">
                  <p class="text-xs text-gray-400 dark:text-gray-500 mb-1">Region</p>
                  <p class="font-medium text-gray-800 dark:text-gray-200">{{ selectedCustomer.region || '—' }}</p>
                </div>
              </div>
            </div>

            <!-- Info -->
            <div>
              <h3 class="text-sm font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider mb-3">Info</h3>
              <div class="grid grid-cols-1 sm:grid-cols-3 gap-3">
                <div class="bg-gray-50 dark:bg-gray-800 rounded-lg p-3">
                  <p class="text-xs text-gray-400 dark:text-gray-500 mb-1">Type</p>
                  <p class="font-medium text-gray-800 dark:text-gray-200 capitalize">{{ selectedCustomer.customerType }}</p>
                </div>
                <div class="bg-gray-50 dark:bg-gray-800 rounded-lg p-3">
                  <p class="text-xs text-gray-400 dark:text-gray-500 mb-1">Registered</p>
                  <p class="font-medium text-gray-800 dark:text-gray-200">{{ new Date(selectedCustomer.createdAt).toLocaleDateString() }}</p>
                </div>
                <div class="bg-gray-50 dark:bg-gray-800 rounded-lg p-3">
                  <p class="text-xs text-gray-400 dark:text-gray-500 mb-1">Last Updated</p>
                  <p class="font-medium text-gray-800 dark:text-gray-200">{{ new Date(selectedCustomer.updatedAt).toLocaleDateString() }}</p>
                </div>
              </div>
              <div v-if="selectedCustomer.businessName" class="mt-3 bg-gray-50 dark:bg-gray-800 rounded-lg p-3">
                <p class="text-xs text-gray-400 dark:text-gray-500 mb-1">Business Name</p>
                <p class="font-medium text-gray-800 dark:text-gray-200">{{ selectedCustomer.businessName }}</p>
              </div>
            </div>

            <!-- GPS -->
            <div v-if="selectedCustomer.gpsLat && selectedCustomer.gpsLng">
              <h3 class="text-sm font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider mb-3">GPS Location</h3>
              <div class="flex gap-3">
                <div class="bg-gray-50 dark:bg-gray-800 rounded-lg p-3 flex-1">
                  <p class="text-xs text-gray-400 dark:text-gray-500 mb-1">Latitude</p>
                  <p class="font-mono text-sm font-medium text-gray-800 dark:text-gray-200">{{ selectedCustomer.gpsLat.toFixed(6) }}</p>
                </div>
                <div class="bg-gray-50 dark:bg-gray-800 rounded-lg p-3 flex-1">
                  <p class="text-xs text-gray-400 dark:text-gray-500 mb-1">Longitude</p>
                  <p class="font-mono text-sm font-medium text-gray-800 dark:text-gray-200">{{ selectedCustomer.gpsLng.toFixed(6) }}</p>
                </div>
              </div>
            </div>

            <!-- Notes -->
            <div v-if="selectedCustomer.notes">
              <h3 class="text-sm font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider mb-3">Notes</h3>
              <div class="bg-yellow-50 dark:bg-yellow-900/10 border border-yellow-100 dark:border-yellow-900/30 rounded-lg p-3">
                <p class="text-sm text-gray-700 dark:text-gray-300">{{ selectedCustomer.notes }}</p>
              </div>
            </div>

            <!-- Subscriptions -->
            <div>
              <h3 class="text-sm font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider mb-3">
                Subscriptions ({{ selectedCustomer.subscriptions.length }})
              </h3>
              <div v-if="selectedCustomer.subscriptions.length === 0" class="text-sm text-gray-400 dark:text-gray-500 text-center py-4">
                No subscriptions for this customer
              </div>
              <div v-else class="space-y-2">
                <div v-for="sub in selectedCustomer.subscriptions" :key="sub.id"
                  class="bg-gray-50 dark:bg-gray-800 rounded-lg p-3 flex items-center justify-between flex-wrap gap-2">
                  <div>
                    <p class="font-medium text-gray-800 dark:text-gray-200">{{ sub.planName }}</p>
                    <p class="text-xs text-gray-400 dark:text-gray-500">Username: {{ sub.username }}</p>
                  </div>
                  <div class="text-right">
                    <p class="text-sm font-medium" :class="subStatusClass(sub.status)">{{ sub.status }}</p>
                    <p v-if="sub.currentPeriodEnd" class="text-xs text-gray-400 dark:text-gray-500">Expires: {{ new Date(sub.currentPeriodEnd).toLocaleDateString() }}</p>
                  </div>
                </div>
              </div>
            </div>

          </div>
        </div>
      </div>
    </Teleport>

    <!-- ── Create Customer Modal ── -->
    <Teleport to="body">
      <div v-if="showCreateModal" class="fixed inset-0 z-50 flex items-center justify-center p-4">
        <div class="absolute inset-0 bg-black/50 backdrop-blur-sm" @click="showCreateModal = false"></div>
        <div class="relative bg-white dark:bg-gray-900 rounded-2xl shadow-2xl w-full max-w-lg max-h-[90vh] overflow-y-auto animate-modal-in p-6">
          <div class="flex items-center justify-between mb-5">
            <h2 class="text-lg font-bold text-gray-800 dark:text-gray-100">Add New Customer</h2>
            <button @click="showCreateModal = false" class="w-7 h-7 rounded-full bg-gray-100 dark:bg-gray-800 flex items-center justify-center text-gray-400 hover:bg-gray-200 dark:hover:bg-gray-700 cursor-pointer text-sm">✕</button>
          </div>

          <form @submit.prevent="handleCreate" class="space-y-4">
            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Full Name *</label>
              <input v-model="createForm.fullName" type="text" placeholder="John Kamau"
                class="w-full px-3 py-2 rounded-lg border text-sm focus:ring-2 focus:ring-blue-500 outline-none transition"
                :class="createValidation.fullName ? 'border-red-400' : 'border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100'" />
              <p v-if="createValidation.fullName" class="text-xs text-red-500 mt-1">{{ createValidation.fullName }}</p>
            </div>

            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Email *</label>
              <input v-model="createForm.email" type="email" placeholder="john@example.com"
                class="w-full px-3 py-2 rounded-lg border text-sm focus:ring-2 focus:ring-blue-500 outline-none transition"
                :class="createValidation.email ? 'border-red-400' : 'border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100'" />
              <p v-if="createValidation.email" class="text-xs text-red-500 mt-1">{{ createValidation.email }}</p>
            </div>

            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Phone *</label>
              <input v-model="createForm.phone" type="tel" placeholder="+254 712 345 678"
                class="w-full px-3 py-2 rounded-lg border text-sm focus:ring-2 focus:ring-blue-500 outline-none transition"
                :class="createValidation.phone ? 'border-red-400' : 'border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100'" />
              <p v-if="createValidation.phone" class="text-xs text-red-500 mt-1">{{ createValidation.phone }}</p>
            </div>

            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Password *</label>
              <input v-model="createForm.password" type="password" placeholder="••••••••"
                class="w-full px-3 py-2 rounded-lg border text-sm focus:ring-2 focus:ring-blue-500 outline-none transition"
                :class="createValidation.password ? 'border-red-400' : 'border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100'" />
              <p v-if="createValidation.password" class="text-xs text-red-500 mt-1">{{ createValidation.password }}</p>
            </div>

            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Customer Type</label>
              <select v-model="createForm.customerType"
                class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 text-sm outline-none focus:ring-2 focus:ring-blue-500">
                <option value="residential">Residential</option>
                <option value="business">Business</option>
                <option value="enterprise">Enterprise</option>
              </select>
            </div>

            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Business Name</label>
              <input v-model="createForm.businessName" type="text" placeholder="e.g. Kamau Enterprises"
                class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 text-sm outline-none focus:ring-2 focus:ring-blue-500" />
            </div>

            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Service Address</label>
              <input v-model="createForm.serviceAddress" type="text" placeholder="Plot 123, Mwiki Road"
                class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 text-sm outline-none focus:ring-2 focus:ring-blue-500" />
            </div>

            <div class="grid grid-cols-2 gap-3">
              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">City</label>
                <input v-model="createForm.city" type="text" placeholder="Nairobi"
                  class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 text-sm outline-none focus:ring-2 focus:ring-blue-500" />
              </div>
              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Region</label>
                <input v-model="createForm.region" type="text" placeholder="Nairobi"
                  class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 text-sm outline-none focus:ring-2 focus:ring-blue-500" />
              </div>
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
                {{ createLoading ? 'Creating...' : 'Add Customer' }}
              </button>
            </div>
          </form>
        </div>
      </div>
    </Teleport>
  </div>
</template>

<style scoped>
@keyframes modalIn { from { opacity: 0; transform: scale(0.95) translateY(10px); } to { opacity: 1; transform: scale(1) translateY(0); } }
.animate-modal-in { animation: modalIn 0.2s ease-out forwards; }
</style>
