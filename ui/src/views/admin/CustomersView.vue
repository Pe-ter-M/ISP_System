<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { getCustomers, getCustomerById, createCustomer, updateCustomer, deleteCustomer } from '@/services/customer.service'
import { useToastStore } from '@/stores/toast.store'
import FieldTip from '@/components/FieldTip.vue'
import type { CustomerSummary, CustomerDetail } from '@/types/customer.types'
import type { CreateCustomerPayload, UpdateCustomerPayload } from '@/services/customer.service'

const toast = useToastStore()

// ── State ──
const customers = ref<CustomerSummary[]>([])
const loading = ref(true)
const error = ref<string | null>(null)
const page = ref(1)
const pageSize = ref(10)
const totalCount = ref(0)
const totalPages = ref(0)
const search = ref('')
const sortField = ref('name')
const sortDir = ref<'asc' | 'desc'>('asc')

// Detail modal
const showDetail = ref(false)
const detailLoading = ref(false)
const selectedCustomer = ref<CustomerDetail | null>(null)

// Create modal
const showCreateModal = ref(false)
const createLoading = ref(false)
const createError = ref('')
const createValidation = ref<Record<string, string>>({})
const optimisticCustomer = ref<CustomerSummary | null>(null)
const createForm = ref({
  fullName: '',
  email: '',
  password: '',
  phone: '',
  customerType: 'residential',
  businessName: '',
  serviceAddress: '',
  city: '',
  region: '',
})

// Edit modal
const showEditModal = ref(false)
const editLoading = ref(false)
const editSaving = ref(false)
const editError = ref('')
const editValidation = ref<Record<string, string>>({})
const editingId = ref<number | null>(null)
const editForm = ref({
  fullName: '',
  email: '',
  phone: '',
  customerType: 'residential',
  businessName: '',
  serviceAddress: '',
  city: '',
  region: '',
  gpsLat: null as number | null,
  gpsLng: null as number | null,
  status: 'active',
  notes: '',
})

// Delete modal
const showDelete = ref(false)
const deleteTarget = ref<CustomerSummary | null>(null)
const deleteLoading = ref(false)
const deleteError = ref('')

// ── Computed ──
const pageNumbers = computed(() => {
  const pages: number[] = []
  for (let i = 1; i <= totalPages.value; i++) pages.push(i)
  return pages
})

// ── Fetch ──
async function fetchCustomers() {
  loading.value = true
  error.value = null
  try {
    const result = await getCustomers(page.value, pageSize.value, search.value || undefined, sortField.value || undefined, sortDir.value === 'desc')
    customers.value = result.items
    totalCount.value = result.totalCount
    totalPages.value = result.totalPages
  } catch (e: unknown) {
    error.value = errMsg(e, 'Failed to load customers')
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

function statusClass(s: string) {
  switch (s) {
    case 'active': return 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-300'
    case 'inactive': return 'bg-yellow-100 text-yellow-700 dark:bg-yellow-900/30 dark:text-yellow-300'
    case 'suspended': return 'bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-300'
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

/** Human-friendly date, e.g. "5th November 2026" */
function friendlyDate(iso: string | null | undefined): string {
  if (!iso) return '—'
  const d = new Date(iso)
  if (Number.isNaN(d.getTime())) return '—'
  const day = d.getDate()
  const suffix = day % 10 === 1 && day !== 11 ? 'st' : day % 10 === 2 && day !== 12 ? 'nd' : day % 10 === 3 && day !== 13 ? 'rd' : 'th'
  const month = d.toLocaleString('en-GB', { month: 'long' })
  return `${day}${suffix} ${month} ${d.getFullYear()}`
}

// ── Create Customer ──
function resetCreateForm() {
  createForm.value = {
    fullName: '', email: '', password: '', phone: '',
    customerType: 'residential', businessName: '', serviceAddress: '', city: '', region: '',
  }
}

function openCreate() {
  resetCreateForm()
  createError.value = ''
  createValidation.value = {}
  showCreateModal.value = true
}

function validateCreateForm(): boolean {
  const v: Record<string, string> = {}
  if (!createForm.value.fullName.trim()) v.fullName = 'Full name is required'
  if (!createForm.value.email.trim()) v.email = 'Email is required'
  else if (!/\S+@\S+\.\S+/.test(createForm.value.email)) v.email = 'Invalid email'
  if (!createForm.value.password || createForm.value.password.length < 4) v.password = 'Minimum 4 characters'
  if (!createForm.value.phone.trim()) v.phone = 'Phone is required'
  if (createForm.value.customerType === 'business' && !createForm.value.businessName.trim()) v.businessName = 'Business name is required for business accounts'
  createValidation.value = v
  return Object.keys(v).length === 0
}

async function handleCreate() {
  if (!validateCreateForm()) return
  createLoading.value = true
  createError.value = ''

  const payload: CreateCustomerPayload = {
    email: createForm.value.email.trim(),
    password: createForm.value.password,
    fullName: createForm.value.fullName.trim(),
    phone: createForm.value.phone.trim(),
    businessName: createForm.value.customerType === 'business' ? createForm.value.businessName.trim() || null : null,
    customerType: createForm.value.customerType,
    serviceAddress: createForm.value.serviceAddress.trim() || null,
    city: createForm.value.city.trim() || null,
    region: createForm.value.region.trim() || null,
  }

  const temp: CustomerSummary = {
    id: Date.now(), userId: 0,
    customerCode: '…', fullName: payload.fullName,
    businessName: payload.businessName, customerType: payload.customerType,
    email: payload.email, phone: payload.phone,
    city: payload.city, region: payload.region,
    status: 'active', hasActiveSubscription: false,
    createdAt: new Date().toISOString(),
  }
  customers.value.unshift(temp)
  optimisticCustomer.value = temp
  showCreateModal.value = false

  try {
    const created = await createCustomer(payload)
    const idx = customers.value.findIndex(c => c.id === temp.id)
    if (idx !== -1) customers.value[idx] = created
    optimisticCustomer.value = null
    totalCount.value++
    totalPages.value = Math.max(1, Math.ceil(totalCount.value / pageSize.value))
    toast.success(`Customer "${created.fullName}" created`)
    if (page.value !== 1) { page.value = 1; fetchCustomers() }
  } catch (e: unknown) {
    const idx = customers.value.findIndex(c => c.id === temp.id)
    if (idx !== -1) customers.value.splice(idx, 1)
    optimisticCustomer.value = null
    createError.value = errMsg(e, 'Failed to create customer')
    toast.error(createError.value)
    showCreateModal.value = true
  } finally {
    createLoading.value = false
  }
}

// ── Detail ──
async function openDetail(c: CustomerSummary) {
  showDetail.value = true
  detailLoading.value = true
  selectedCustomer.value = null
  try {
    selectedCustomer.value = await getCustomerById(c.id)
  } catch (e: unknown) {
    selectedCustomer.value = null
    toast.error(errMsg(e, 'Failed to load customer details'))
  } finally {
    detailLoading.value = false
  }
}
function closeDetail() {
  showDetail.value = false
  selectedCustomer.value = null
}

// ── Edit ──
function fillEditForm(d: CustomerDetail) {
  editForm.value = {
    fullName: d.fullName,
    email: d.email ?? '',
    phone: d.phone,
    customerType: d.customerType,
    businessName: d.businessName ?? '',
    serviceAddress: d.serviceAddress ?? '',
    city: d.city ?? '',
    region: d.region ?? '',
    gpsLat: d.gpsLat,
    gpsLng: d.gpsLng,
    status: d.status,
    notes: d.notes ?? '',
  }
}

async function openEdit(c: CustomerSummary) {
  editingId.value = c.id
  editError.value = ''
  editValidation.value = {}
  editLoading.value = true
  showEditModal.value = true
  try {
    const d = await getCustomerById(c.id)
    fillEditForm(d)
  } catch (e: unknown) {
    editError.value = errMsg(e, 'Could not load customer details')
  } finally {
    editLoading.value = false
  }
}

/** Edit from the detail modal — snapshot before closing so the id survives */
function editFromDetail() {
  const d = selectedCustomer.value
  if (!d) return
  closeDetail()
  openEdit(d)
}

function closeEdit() {
  if (editSaving.value) return
  showEditModal.value = false
  editingId.value = null
}

function validateEditForm(): boolean {
  const v: Record<string, string> = {}
  if (!editForm.value.fullName.trim()) v.fullName = 'Full name is required'
  if (!editForm.value.email.trim()) v.email = 'Email is required'
  else if (!/\S+@\S+\.\S+/.test(editForm.value.email)) v.email = 'Invalid email'
  if (!editForm.value.phone.trim()) v.phone = 'Phone is required'
  if (editForm.value.customerType === 'business' && !editForm.value.businessName.trim()) v.businessName = 'Business name is required for business accounts'
  editValidation.value = v
  return Object.keys(v).length === 0
}

async function handleEdit() {
  if (editingId.value === null) return
  if (!validateEditForm()) return
  editSaving.value = true
  editError.value = ''
  const payload: UpdateCustomerPayload = {
    fullName: editForm.value.fullName.trim(),
    email: editForm.value.email.trim(),
    phone: editForm.value.phone.trim(),
    businessName: editForm.value.customerType === 'business' ? editForm.value.businessName.trim() || null : null,
    customerType: editForm.value.customerType,
    serviceAddress: editForm.value.serviceAddress.trim() || null,
    city: editForm.value.city.trim() || null,
    region: editForm.value.region.trim() || null,
    gpsLat: editForm.value.gpsLat,
    gpsLng: editForm.value.gpsLng,
    status: editForm.value.status,
    notes: editForm.value.notes.trim() || null,
  }
  try {
    const updated = await updateCustomer(editingId.value, payload)
    const idx = customers.value.findIndex(c => c.id === updated.id)
    if (idx !== -1) customers.value[idx] = updated
    showEditModal.value = false
    editingId.value = null
    toast.success(`Customer "${updated.fullName}" updated`)
  } catch (e: unknown) {
    editError.value = errMsg(e, 'Failed to update customer')
    toast.error(editError.value)
  } finally {
    editSaving.value = false
  }
}

// ── Delete ──
function openDelete(c: CustomerSummary) {
  deleteTarget.value = c
  deleteError.value = ''
  showDelete.value = true
}
async function handleDelete() {
  if (!deleteTarget.value) return
  const target = deleteTarget.value
  deleteLoading.value = true
  deleteError.value = ''
  try {
    const result = await deleteCustomer(target.id)
    customers.value = customers.value.filter(c => c.id !== target.id)
    totalCount.value = Math.max(0, totalCount.value - 1)
    totalPages.value = Math.max(1, Math.ceil(totalCount.value / pageSize.value))
    if (customers.value.length === 0 && page.value > 1) {
      page.value -= 1
      fetchCustomers()
    }
    showDelete.value = false
    deleteTarget.value = null
    if (result.hardDeleted) toast.success(`Customer "${target.fullName}" deleted`)
    else toast.info(result.message || `Customer "${target.fullName}" deactivated`)
  } catch (e: unknown) {
    deleteError.value = errMsg(e, 'Failed to delete customer')
    toast.error(deleteError.value)
  } finally {
    deleteLoading.value = false
  }
}
function cancelDelete() {
  if (deleteLoading.value) return
  showDelete.value = false
  deleteTarget.value = null
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
          <input v-model="search" type="text" placeholder="Search name, code, email, phone..."
            class="pl-10 pr-4 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 text-sm focus:ring-2 focus:ring-blue-500 outline-none transition w-64" />
        </div>
        <button @click="openCreate"
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
        class="px-2 py-1 rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 text-xs outline-none cursor-pointer">
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
              <th @click="toggleSort('name')" class="px-4 py-3 text-left font-semibold text-gray-600 dark:text-gray-400 cursor-pointer hover:text-blue-600 dark:hover:text-blue-400 transition whitespace-nowrap">Customer <span class="text-xs ml-1">{{ sortIcon('name') }}</span></th>
              <th class="px-4 py-3 text-left font-semibold text-gray-600 dark:text-gray-400 whitespace-nowrap hidden md:table-cell">Contact</th>
              <th @click="toggleSort('code')" class="px-4 py-3 text-left font-semibold text-gray-600 dark:text-gray-400 cursor-pointer hover:text-blue-600 dark:hover:text-blue-400 transition whitespace-nowrap hidden lg:table-cell">Code <span class="text-xs ml-1">{{ sortIcon('code') }}</span></th>
              <th class="px-4 py-3 text-left font-semibold text-gray-600 dark:text-gray-400 whitespace-nowrap">Subscription</th>
              <th @click="toggleSort('status')" class="px-4 py-3 text-left font-semibold text-gray-600 dark:text-gray-400 cursor-pointer hover:text-blue-600 dark:hover:text-blue-400 transition whitespace-nowrap">Status <span class="text-xs ml-1">{{ sortIcon('status') }}</span></th>
              <th @click="toggleSort('created')" class="px-4 py-3 text-left font-semibold text-gray-600 dark:text-gray-400 cursor-pointer hover:text-blue-600 dark:hover:text-blue-400 transition whitespace-nowrap hidden sm:table-cell">Created <span class="text-xs ml-1">{{ sortIcon('created') }}</span></th>
              <th class="px-4 py-3 text-right font-semibold text-gray-600 dark:text-gray-400 whitespace-nowrap">Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="c in customers" :key="c.id"
              class="border-b border-gray-50 dark:border-gray-800/50 hover:bg-gray-50 dark:hover:bg-gray-800/30 transition"
              :class="{ 'opacity-50': optimisticCustomer?.id === c.id }">
              <td class="px-4 py-3">
                <div class="flex items-center gap-3">
                  <div class="w-8 h-8 rounded-full bg-blue-100 dark:bg-blue-900/50 flex items-center justify-center text-xs font-bold text-blue-600 dark:text-blue-400 flex-shrink-0">{{ c.fullName.charAt(0).toUpperCase() }}</div>
                  <div>
                    <p class="font-medium text-gray-800 dark:text-gray-200">
                      {{ c.fullName }}
                      <span v-if="optimisticCustomer?.id === c.id" class="text-xs text-blue-500 font-normal ml-1">Creating...</span>
                    </p>
                    <p v-if="c.businessName" class="text-xs text-gray-400 dark:text-gray-500">{{ c.businessName }} · {{ c.customerType }}</p>
                    <p v-else class="text-xs text-gray-400 dark:text-gray-500 capitalize">{{ c.customerType }}</p>
                  </div>
                </div>
              </td>
              <td class="px-4 py-3 hidden md:table-cell">
                <p class="text-gray-600 dark:text-gray-400">{{ c.email || '—' }}</p>
                <p class="text-xs text-gray-400 dark:text-gray-500 font-mono">{{ c.phone }}</p>
              </td>
              <td class="px-4 py-3 text-gray-600 dark:text-gray-400 font-mono text-xs hidden lg:table-cell">{{ c.customerCode }}</td>
              <td class="px-4 py-3">
                <span class="inline-flex items-center gap-1.5 text-xs font-medium px-2 py-0.5 rounded-full"
                  :class="c.hasActiveSubscription
                    ? 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-300'
                    : 'bg-gray-100 text-gray-500 dark:bg-gray-800 dark:text-gray-400'">
                  <span :class="c.hasActiveSubscription ? 'bg-green-500' : 'bg-gray-300 dark:bg-gray-600'" class="w-1.5 h-1.5 rounded-full inline-block"></span>
                  {{ c.hasActiveSubscription ? 'Active sub' : 'No sub' }}
                </span>
              </td>
              <td class="px-4 py-3">
                <div class="flex items-center gap-1.5">
                  <span :class="statusDot(c.status)" class="w-2 h-2 rounded-full inline-block"></span>
                  <span :class="statusClass(c.status)" class="px-2 py-0.5 rounded-full text-xs font-medium capitalize">{{ c.status }}</span>
                </div>
              </td>
              <td class="px-4 py-3 text-gray-500 dark:text-gray-400 hidden sm:table-cell whitespace-nowrap">{{ friendlyDate(c.createdAt) }}</td>
              <td class="px-4 py-3 text-right">
                <div class="flex items-center justify-end gap-1.5">
                  <button @click="openDetail(c)"
                    class="px-3 py-1.5 text-xs font-medium text-blue-600 dark:text-blue-400 bg-blue-50 dark:bg-blue-900/20 hover:bg-blue-100 dark:hover:bg-blue-900/40 rounded-lg transition cursor-pointer">
                    View
                  </button>
                  <button @click="openEdit(c)"
                    class="px-3 py-1.5 text-xs font-medium text-indigo-600 dark:text-indigo-400 bg-indigo-50 dark:bg-indigo-900/20 hover:bg-indigo-100 dark:hover:bg-indigo-900/40 rounded-lg transition cursor-pointer">
                    Edit
                  </button>
                  <button @click="openDelete(c)"
                    class="px-3 py-1.5 text-xs font-medium text-red-600 dark:text-red-400 bg-red-50 dark:bg-red-900/20 hover:bg-red-100 dark:hover:bg-red-900/40 rounded-lg transition cursor-pointer">
                    Delete
                  </button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- ── Empty State ── -->
      <div v-if="customers.length === 0 && !loading && !error" class="text-center py-16">
        <p class="text-gray-400 dark:text-gray-500">No customers found</p>
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

    <!-- ── Create Customer Modal ── -->
    <Teleport to="body">
      <div v-if="showCreateModal" class="fixed inset-0 z-50 flex items-center justify-center p-4" @click.self="showCreateModal = false">
        <div class="absolute inset-0 bg-black/50 backdrop-blur-sm" @click="showCreateModal = false"></div>
        <div class="relative bg-white dark:bg-gray-900 rounded-2xl shadow-2xl w-full max-w-lg max-h-[90vh] overflow-y-auto animate-modal-in p-6">
          <div class="flex items-center justify-between mb-5">
            <h2 class="text-lg font-bold text-gray-800 dark:text-gray-100">Add Customer</h2>
            <button @click="showCreateModal = false" :disabled="createLoading"
              class="w-7 h-7 rounded-full bg-gray-100 dark:bg-gray-800 flex items-center justify-center text-gray-400 hover:bg-gray-200 dark:hover:bg-gray-700 cursor-pointer text-sm disabled:opacity-40">✕</button>
          </div>

          <form @submit.prevent="handleCreate" class="space-y-4">
            <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <div>
                <div class="flex items-center gap-1.5 mb-1">
                  <label class="text-sm font-medium text-gray-700 dark:text-gray-300">Full Name *</label>
                  <FieldTip text="The customer full name, used across the system." />
                </div>
                <input v-model="createForm.fullName" type="text" placeholder="Jane Wanjiku"
                  class="w-full px-3 py-2 rounded-lg border text-sm focus:ring-2 focus:ring-blue-500 outline-none transition"
                  :class="createValidation.fullName ? 'border-red-400' : 'border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100'" />
                <p v-if="createValidation.fullName" class="text-xs text-red-500 mt-1">{{ createValidation.fullName }}</p>
              </div>

              <div>
                <div class="flex items-center gap-1.5 mb-1">
                  <label class="text-sm font-medium text-gray-700 dark:text-gray-300">Phone *</label>
                  <FieldTip text="Used for M-Pesa / Airtel payments and contact." />
                </div>
                <input v-model="createForm.phone" type="tel" placeholder="+254 712 345 678"
                  class="w-full px-3 py-2 rounded-lg border text-sm focus:ring-2 focus:ring-blue-500 outline-none transition"
                  :class="createValidation.phone ? 'border-red-400' : 'border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100'" />
                <p v-if="createValidation.phone" class="text-xs text-red-500 mt-1">{{ createValidation.phone }}</p>
              </div>

              <div>
                <div class="flex items-center gap-1.5 mb-1">
                  <label class="text-sm font-medium text-gray-700 dark:text-gray-300">Email *</label>
                  <FieldTip text="Login email for the customer portal." />
                </div>
                <input v-model="createForm.email" type="email" placeholder="jane@example.com"
                  class="w-full px-3 py-2 rounded-lg border text-sm focus:ring-2 focus:ring-blue-500 outline-none transition"
                  :class="createValidation.email ? 'border-red-400' : 'border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100'" />
                <p v-if="createValidation.email" class="text-xs text-red-500 mt-1">{{ createValidation.email }}</p>
              </div>

              <div>
                <div class="flex items-center gap-1.5 mb-1">
                  <label class="text-sm font-medium text-gray-700 dark:text-gray-300">Password *</label>
                  <FieldTip text="Initial password, minimum 4 characters. The customer should change it after first login." />
                </div>
                <input v-model="createForm.password" type="password" placeholder="••••••"
                  class="w-full px-3 py-2 rounded-lg border text-sm focus:ring-2 focus:ring-blue-500 outline-none transition"
                  :class="createValidation.password ? 'border-red-400' : 'border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100'" />
                <p v-if="createValidation.password" class="text-xs text-red-500 mt-1">{{ createValidation.password }}</p>
              </div>

              <div>
                <div class="flex items-center gap-1.5 mb-1">
                  <label class="text-sm font-medium text-gray-700 dark:text-gray-300">Account Type</label>
                  <FieldTip text="Residential for home users, business for companies or offices." />
                </div>
                <select v-model="createForm.customerType"
                  class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 text-sm focus:ring-2 focus:ring-blue-500 outline-none transition cursor-pointer capitalize">
                  <option value="residential">Residential</option>
                  <option value="business">Business</option>
                </select>
              </div>

              <div v-if="createForm.customerType === 'business'" class="sm:col-span-2">
                <div class="flex items-center gap-1.5 mb-1">
                  <label class="text-sm font-medium text-gray-700 dark:text-gray-300">Business Name *</label>
                  <FieldTip text="Registered company or business name shown on invoices." />
                </div>
                <input v-model="createForm.businessName" type="text" placeholder="Wanjiku Enterprises Ltd"
                  class="w-full px-3 py-2 rounded-lg border text-sm focus:ring-2 focus:ring-blue-500 outline-none transition"
                  :class="createValidation.businessName ? 'border-red-400' : 'border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100'" />
                <p v-if="createValidation.businessName" class="text-xs text-red-500 mt-1">{{ createValidation.businessName }}</p>
              </div>

              <div class="sm:col-span-2">
                <div class="flex items-center gap-1.5 mb-1">
                  <label class="text-sm font-medium text-gray-700 dark:text-gray-300">Service Address</label>
                  <FieldTip text="Installation address for the connection." />
                </div>
                <input v-model="createForm.serviceAddress" type="text" placeholder="Plot 12, Kimathi Street"
                  class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 text-sm focus:ring-2 focus:ring-blue-500 outline-none transition" />
              </div>

              <div>
                <div class="flex items-center gap-1.5 mb-1">
                  <label class="text-sm font-medium text-gray-700 dark:text-gray-300">City</label>
                  <FieldTip text="City or town of the customer." />
                </div>
                <input v-model="createForm.city" type="text" placeholder="Nairobi"
                  class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 text-sm focus:ring-2 focus:ring-blue-500 outline-none transition" />
              </div>

              <div>
                <div class="flex items-center gap-1.5 mb-1">
                  <label class="text-sm font-medium text-gray-700 dark:text-gray-300">Region</label>
                  <FieldTip text="County or region." />
                </div>
                <input v-model="createForm.region" type="text" placeholder="Nairobi County"
                  class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 text-sm focus:ring-2 focus:ring-blue-500 outline-none transition" />
              </div>
            </div>

            <p v-if="createError" class="text-sm text-red-600 dark:text-red-400 bg-red-50 dark:bg-red-900/20 rounded-lg px-3 py-2">{{ createError }}</p>

            <div class="flex gap-3 pt-2">
              <button type="button" @click="showCreateModal = false" :disabled="createLoading"
                class="flex-1 px-4 py-2.5 text-sm font-medium text-gray-600 dark:text-gray-300 bg-gray-100 dark:bg-gray-800 hover:bg-gray-200 dark:hover:bg-gray-700 rounded-lg transition cursor-pointer disabled:opacity-40">
                Cancel
              </button>
              <button type="submit" :disabled="createLoading"
                class="flex-1 px-4 py-2.5 text-sm font-medium text-white bg-blue-600 hover:bg-blue-700 rounded-lg transition-all duration-200 cursor-pointer disabled:opacity-50 flex items-center justify-center gap-2">
                <span v-if="createLoading" class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
                Create Customer
              </button>
            </div>
          </form>
        </div>
      </div>
    </Teleport>

    <!-- ── Edit Customer Modal ── -->
    <Teleport to="body">
      <div v-if="showEditModal" class="fixed inset-0 z-50 flex items-center justify-center p-4" @click.self="closeEdit">
        <div class="absolute inset-0 bg-black/50 backdrop-blur-sm" @click="closeEdit"></div>
        <div class="relative bg-white dark:bg-gray-900 rounded-2xl shadow-2xl w-full max-w-lg max-h-[90vh] overflow-y-auto animate-modal-in p-6">
          <div class="flex items-center justify-between mb-5">
            <h2 class="text-lg font-bold text-gray-800 dark:text-gray-100">Edit Customer</h2>
            <button @click="closeEdit" :disabled="editSaving"
              class="w-7 h-7 rounded-full bg-gray-100 dark:bg-gray-800 flex items-center justify-center text-gray-400 hover:bg-gray-200 dark:hover:bg-gray-700 cursor-pointer text-sm disabled:opacity-40">✕</button>
          </div>

          <div v-if="editLoading" class="flex justify-center py-16">
            <div class="w-8 h-8 border-4 border-blue-500 border-t-transparent rounded-full animate-spin"></div>
          </div>

          <form v-else @submit.prevent="handleEdit" class="space-y-4">
            <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <div>
                <div class="flex items-center gap-1.5 mb-1">
                  <label class="text-sm font-medium text-gray-700 dark:text-gray-300">Full Name *</label>
                  <FieldTip text="The customer full name, used across the system." />
                </div>
                <input v-model="editForm.fullName" type="text"
                  class="w-full px-3 py-2 rounded-lg border text-sm focus:ring-2 focus:ring-blue-500 outline-none transition"
                  :class="editValidation.fullName ? 'border-red-400' : 'border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100'" />
                <p v-if="editValidation.fullName" class="text-xs text-red-500 mt-1">{{ editValidation.fullName }}</p>
              </div>

              <div>
                <div class="flex items-center gap-1.5 mb-1">
                  <label class="text-sm font-medium text-gray-700 dark:text-gray-300">Phone *</label>
                  <FieldTip text="Used for M-Pesa / Airtel payments and contact." />
                </div>
                <input v-model="editForm.phone" type="tel"
                  class="w-full px-3 py-2 rounded-lg border text-sm focus:ring-2 focus:ring-blue-500 outline-none transition"
                  :class="editValidation.phone ? 'border-red-400' : 'border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100'" />
                <p v-if="editValidation.phone" class="text-xs text-red-500 mt-1">{{ editValidation.phone }}</p>
              </div>

              <div>
                <div class="flex items-center gap-1.5 mb-1">
                  <label class="text-sm font-medium text-gray-700 dark:text-gray-300">Email *</label>
                  <FieldTip text="Login email for the customer portal." />
                </div>
                <input v-model="editForm.email" type="email"
                  class="w-full px-3 py-2 rounded-lg border text-sm focus:ring-2 focus:ring-blue-500 outline-none transition"
                  :class="editValidation.email ? 'border-red-400' : 'border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100'" />
                <p v-if="editValidation.email" class="text-xs text-red-500 mt-1">{{ editValidation.email }}</p>
              </div>

              <div>
                <div class="flex items-center gap-1.5 mb-1">
                  <label class="text-sm font-medium text-gray-700 dark:text-gray-300">Status</label>
                  <FieldTip text="Inactive accounts cannot log in or authenticate with RADIUS." />
                </div>
                <select v-model="editForm.status"
                  class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 text-sm focus:ring-2 focus:ring-blue-500 outline-none transition cursor-pointer capitalize">
                  <option value="active">Active</option>
                  <option value="inactive">Inactive</option>
                </select>
              </div>

              <div>
                <div class="flex items-center gap-1.5 mb-1">
                  <label class="text-sm font-medium text-gray-700 dark:text-gray-300">Account Type</label>
                  <FieldTip text="Residential for home users, business for companies or offices." />
                </div>
                <select v-model="editForm.customerType"
                  class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 text-sm focus:ring-2 focus:ring-blue-500 outline-none transition cursor-pointer capitalize">
                  <option value="residential">Residential</option>
                  <option value="business">Business</option>
                </select>
              </div>

              <div v-if="editForm.customerType === 'business'" class="sm:col-span-2">
                <div class="flex items-center gap-1.5 mb-1">
                  <label class="text-sm font-medium text-gray-700 dark:text-gray-300">Business Name *</label>
                  <FieldTip text="Registered company or business name shown on invoices." />
                </div>
                <input v-model="editForm.businessName" type="text"
                  class="w-full px-3 py-2 rounded-lg border text-sm focus:ring-2 focus:ring-blue-500 outline-none transition"
                  :class="editValidation.businessName ? 'border-red-400' : 'border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100'" />
                <p v-if="editValidation.businessName" class="text-xs text-red-500 mt-1">{{ editValidation.businessName }}</p>
              </div>

              <div class="sm:col-span-2">
                <div class="flex items-center gap-1.5 mb-1">
                  <label class="text-sm font-medium text-gray-700 dark:text-gray-300">Service Address</label>
                  <FieldTip text="Installation address for the connection." />
                </div>
                <input v-model="editForm.serviceAddress" type="text"
                  class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 text-sm focus:ring-2 focus:ring-blue-500 outline-none transition" />
              </div>

              <div>
                <div class="flex items-center gap-1.5 mb-1">
                  <label class="text-sm font-medium text-gray-700 dark:text-gray-300">City</label>
                  <FieldTip text="City or town of the customer." />
                </div>
                <input v-model="editForm.city" type="text"
                  class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 text-sm focus:ring-2 focus:ring-blue-500 outline-none transition" />
              </div>

              <div>
                <div class="flex items-center gap-1.5 mb-1">
                  <label class="text-sm font-medium text-gray-700 dark:text-gray-300">Region</label>
                  <FieldTip text="County or region." />
                </div>
                <input v-model="editForm.region" type="text"
                  class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 text-sm focus:ring-2 focus:ring-blue-500 outline-none transition" />
              </div>

              <div>
                <div class="flex items-center gap-1.5 mb-1">
                  <label class="text-sm font-medium text-gray-700 dark:text-gray-300">GPS Latitude</label>
                  <FieldTip text="Optional GPS coordinates for site visits and mapping." />
                </div>
                <input v-model.number="editForm.gpsLat" type="number" step="any" placeholder="-1.2921"
                  class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 text-sm focus:ring-2 focus:ring-blue-500 outline-none transition" />
              </div>

              <div>
                <div class="flex items-center gap-1.5 mb-1">
                  <label class="text-sm font-medium text-gray-700 dark:text-gray-300">GPS Longitude</label>
                  <FieldTip text="Optional GPS coordinates for site visits and mapping." />
                </div>
                <input v-model.number="editForm.gpsLng" type="number" step="any" placeholder="36.8219"
                  class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 text-sm focus:ring-2 focus:ring-blue-500 outline-none transition" />
              </div>

              <div class="sm:col-span-2">
                <div class="flex items-center gap-1.5 mb-1">
                  <label class="text-sm font-medium text-gray-700 dark:text-gray-300">Notes</label>
                  <FieldTip text="Internal notes about this customer, visible only to staff." />
                </div>
                <textarea v-model="editForm.notes" rows="2"
                  class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 text-sm focus:ring-2 focus:ring-blue-500 outline-none transition resize-none"></textarea>
              </div>
            </div>

            <p v-if="editError" class="text-sm text-red-600 dark:text-red-400 bg-red-50 dark:bg-red-900/20 rounded-lg px-3 py-2">{{ editError }}</p>

            <div class="flex gap-3 pt-2">
              <button type="button" @click="closeEdit" :disabled="editSaving"
                class="flex-1 px-4 py-2.5 text-sm font-medium text-gray-600 dark:text-gray-300 bg-gray-100 dark:bg-gray-800 hover:bg-gray-200 dark:hover:bg-gray-700 rounded-lg transition cursor-pointer disabled:opacity-40">
                Cancel
              </button>
              <button type="submit" :disabled="editSaving"
                class="flex-1 px-4 py-2.5 text-sm font-medium text-white bg-blue-600 hover:bg-blue-700 rounded-lg transition-all duration-200 cursor-pointer disabled:opacity-50 flex items-center justify-center gap-2">
                <span v-if="editSaving" class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
                Save Changes
              </button>
            </div>
          </form>
        </div>
      </div>
    </Teleport>

    <!-- ── Detail Modal ── -->
    <Teleport to="body">
      <div v-if="showDetail" class="fixed inset-0 z-50 flex items-center justify-center p-4" @click.self="closeDetail">
        <div class="absolute inset-0 bg-black/50 backdrop-blur-sm" @click="closeDetail"></div>
        <div class="relative bg-white dark:bg-gray-900 rounded-2xl shadow-2xl w-full max-w-2xl max-h-[90vh] overflow-y-auto animate-modal-in p-6 sm:p-8">
          <button @click="closeDetail"
            class="absolute top-4 right-4 w-7 h-7 rounded-full bg-gray-100 dark:bg-gray-800 flex items-center justify-center text-gray-400 hover:bg-gray-200 dark:hover:bg-gray-700 cursor-pointer text-sm">✕</button>

          <div v-if="detailLoading" class="flex justify-center py-16">
            <div class="w-8 h-8 border-4 border-blue-500 border-t-transparent rounded-full animate-spin"></div>
          </div>

          <div v-else-if="selectedCustomer" class="space-y-6">
            <!-- Header -->
            <div class="flex items-start justify-between gap-4 pb-4 border-b border-gray-100 dark:border-gray-800">
              <div>
                <div class="flex items-center gap-2 flex-wrap">
                  <h2 class="text-xl font-bold text-gray-800 dark:text-gray-100">{{ selectedCustomer.fullName }}</h2>
                  <span :class="statusClass(selectedCustomer.status)" class="px-2 py-0.5 rounded-full text-xs font-medium capitalize">{{ selectedCustomer.status }}</span>
                  <span class="text-xs px-2 py-0.5 rounded-full font-medium"
                    :class="selectedCustomer.hasActiveSubscription
                      ? 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-300'
                      : 'bg-gray-100 text-gray-500 dark:bg-gray-800 dark:text-gray-400'">
                    {{ selectedCustomer.hasActiveSubscription ? 'Active subscription' : 'No active subscription' }}
                  </span>
                </div>
                <p class="text-xs text-gray-400 dark:text-gray-500 font-mono mt-1">{{ selectedCustomer.customerCode }}</p>
                <p v-if="selectedCustomer.businessName" class="text-sm text-gray-500 dark:text-gray-400 mt-1">{{ selectedCustomer.businessName }} · <span class="capitalize">{{ selectedCustomer.customerType }}</span></p>
              </div>
              <button @click="editFromDetail"
                class="px-3 py-1.5 text-xs font-medium text-indigo-600 dark:text-indigo-400 bg-indigo-50 dark:bg-indigo-900/20 hover:bg-indigo-100 dark:hover:bg-indigo-900/40 rounded-lg transition cursor-pointer whitespace-nowrap">
                Edit Customer
              </button>
            </div>

            <!-- Contact -->
            <div>
              <h3 class="text-sm font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider mb-3">Contact</h3>
              <div class="grid grid-cols-2 gap-3">
                <div class="bg-gray-50 dark:bg-gray-800 rounded-lg p-3">
                  <p class="text-xs text-gray-400 dark:text-gray-500 mb-1">Email</p>
                  <p class="font-semibold text-gray-800 dark:text-gray-100 text-sm break-all">{{ selectedCustomer.email || '—' }}</p>
                </div>
                <div class="bg-gray-50 dark:bg-gray-800 rounded-lg p-3">
                  <p class="text-xs text-gray-400 dark:text-gray-500 mb-1">Phone</p>
                  <p class="font-semibold text-gray-800 dark:text-gray-100 text-sm font-mono">{{ selectedCustomer.phone }}</p>
                </div>
              </div>
            </div>

            <!-- Location -->
            <div>
              <h3 class="text-sm font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider mb-3">Location</h3>
              <div class="grid grid-cols-2 gap-3">
                <div class="bg-gray-50 dark:bg-gray-800 rounded-lg p-3 col-span-2">
                  <p class="text-xs text-gray-400 dark:text-gray-500 mb-1">Service Address</p>
                  <p class="font-semibold text-gray-800 dark:text-gray-100 text-sm">{{ selectedCustomer.serviceAddress || '—' }}</p>
                </div>
                <div class="bg-gray-50 dark:bg-gray-800 rounded-lg p-3">
                  <p class="text-xs text-gray-400 dark:text-gray-500 mb-1">City</p>
                  <p class="font-semibold text-gray-800 dark:text-gray-100 text-sm">{{ selectedCustomer.city || '—' }}</p>
                </div>
                <div class="bg-gray-50 dark:bg-gray-800 rounded-lg p-3">
                  <p class="text-xs text-gray-400 dark:text-gray-500 mb-1">Region</p>
                  <p class="font-semibold text-gray-800 dark:text-gray-100 text-sm">{{ selectedCustomer.region || '—' }}</p>
                </div>
                <div v-if="selectedCustomer.gpsLat !== null" class="bg-gray-50 dark:bg-gray-800 rounded-lg p-3 col-span-2">
                  <p class="text-xs text-gray-400 dark:text-gray-500 mb-1">GPS</p>
                  <p class="font-semibold text-gray-800 dark:text-gray-100 text-sm font-mono">{{ selectedCustomer.gpsLat }}, {{ selectedCustomer.gpsLng }}</p>
                </div>
              </div>
            </div>

            <!-- Access -->
            <div>
              <h3 class="text-sm font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider mb-3">Access &amp; Account</h3>
              <div class="grid grid-cols-2 gap-3">
                <div class="bg-gray-50 dark:bg-gray-800 rounded-lg p-3">
                  <p class="text-xs text-gray-400 dark:text-gray-500 mb-1">PPPoE Username</p>
                  <p class="font-semibold text-gray-800 dark:text-gray-100 text-sm font-mono">{{ selectedCustomer.usernamePpoe || '—' }}</p>
                </div>
                <div class="bg-gray-50 dark:bg-gray-800 rounded-lg p-3">
                  <p class="text-xs text-gray-400 dark:text-gray-500 mb-1">PPPoE Password</p>
                  <p class="font-semibold text-gray-800 dark:text-gray-100 text-sm font-mono">{{ selectedCustomer.passwordPpoe || '—' }}</p>
                </div>
                <div class="bg-gray-50 dark:bg-gray-800 rounded-lg p-3">
                  <p class="text-xs text-gray-400 dark:text-gray-500 mb-1">Created</p>
                  <p class="font-semibold text-gray-800 dark:text-gray-100 text-sm">{{ friendlyDate(selectedCustomer.createdAt) }}</p>
                </div>
                <div class="bg-gray-50 dark:bg-gray-800 rounded-lg p-3">
                  <p class="text-xs text-gray-400 dark:text-gray-500 mb-1">Last Updated</p>
                  <p class="font-semibold text-gray-800 dark:text-gray-100 text-sm">{{ friendlyDate(selectedCustomer.updatedAt) }}</p>
                </div>
              </div>
              <p v-if="selectedCustomer.notes" class="mt-3 text-sm text-gray-500 dark:text-gray-400 bg-gray-50 dark:bg-gray-800 rounded-lg px-3 py-2"><span class="font-semibold text-gray-600 dark:text-gray-300">Notes: </span>{{ selectedCustomer.notes }}</p>
            </div>

            <!-- Subscriptions -->
            <div>
              <h3 class="text-sm font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider mb-3">Subscriptions</h3>
              <div v-if="selectedCustomer.subscriptions.length === 0" class="text-center py-6 bg-gray-50 dark:bg-gray-800 rounded-xl">
                <p class="text-sm text-gray-400 dark:text-gray-500">No subscriptions yet</p>
              </div>
              <div v-else class="space-y-2">
                <div v-for="sub in selectedCustomer.subscriptions" :key="sub.id"
                  class="flex items-center justify-between gap-3 rounded-xl border border-gray-100 dark:border-gray-800 bg-gray-50 dark:bg-gray-800/60 px-4 py-2.5">
                  <div>
                    <p class="text-sm font-medium text-gray-800 dark:text-gray-100">{{ sub.planName }}</p>
                    <p class="text-xs text-gray-400 dark:text-gray-500 font-mono">{{ sub.username }}</p>
                  </div>
                  <div class="text-right">
                    <span :class="statusClass(sub.status)" class="px-2 py-0.5 rounded-full text-xs font-medium capitalize">{{ sub.status }}</span>
                    <p class="text-xs text-gray-400 dark:text-gray-500 mt-1">{{ sub.currentPeriodEnd ? `ends ${friendlyDate(sub.currentPeriodEnd)}` : 'no period' }}</p>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <div v-else class="p-8 text-center">
            <p class="text-red-500 dark:text-red-400">Failed to load customer details.</p>
            <button @click="closeDetail" class="mt-4 text-blue-600 dark:text-blue-400 hover:underline cursor-pointer">Close</button>
          </div>
        </div>
      </div>
    </Teleport>

    <!-- ── Delete Confirmation Modal ── -->
    <Teleport to="body">
      <div v-if="showDelete" class="fixed inset-0 z-50 flex items-center justify-center p-4" @click.self="cancelDelete">
        <div class="absolute inset-0 bg-black/50 backdrop-blur-sm" @click="cancelDelete"></div>
        <div class="relative bg-white dark:bg-gray-900 rounded-2xl shadow-2xl w-full max-w-md animate-modal-in p-6">
          <div class="flex items-start gap-4">
            <div class="w-10 h-10 rounded-full bg-red-100 dark:bg-red-900/30 flex items-center justify-center flex-shrink-0">
              <svg class="w-5 h-5 text-red-600 dark:text-red-400" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                <path stroke-linecap="round" stroke-linejoin="round" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
              </svg>
            </div>
            <div>
              <h2 class="text-lg font-bold text-gray-800 dark:text-gray-100">Delete Customer</h2>
              <p class="text-sm text-gray-500 dark:text-gray-400 mt-1">
                Are you sure you want to delete <span class="font-semibold text-gray-700 dark:text-gray-200">{{ deleteTarget?.fullName }}</span>?
                If the customer has subscription history the account is deactivated instead — otherwise it is permanently removed.
              </p>
            </div>
          </div>

          <p v-if="deleteError" class="mt-4 text-sm text-red-600 dark:text-red-400 bg-red-50 dark:bg-red-900/20 rounded-lg px-3 py-2">{{ deleteError }}</p>

          <div class="flex gap-3 mt-6">
            <button type="button" @click="cancelDelete" :disabled="deleteLoading"
              class="flex-1 px-4 py-2.5 text-sm font-medium text-gray-600 dark:text-gray-300 bg-gray-100 dark:bg-gray-800 hover:bg-gray-200 dark:hover:bg-gray-700 rounded-lg transition cursor-pointer disabled:opacity-40">
              Cancel
            </button>
            <button type="button" @click="handleDelete" :disabled="deleteLoading"
              class="flex-1 px-4 py-2.5 text-sm font-medium text-white bg-red-600 hover:bg-red-700 rounded-lg transition-all duration-200 cursor-pointer disabled:opacity-50 flex items-center justify-center gap-2">
              <span v-if="deleteLoading" class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
              Delete Customer
            </button>
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
.animate-modal-in {
  animation: modalIn 0.25s ease-out forwards;
}
</style>
