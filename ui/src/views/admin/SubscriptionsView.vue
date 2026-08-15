<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import {
  getSubscriptions, getSubscriptionStats, getSubscriptionById,
  createSubscription, updateSubscription, updateSubscriptionStatus, deleteSubscription,
  getPaymentMethods, getPayments,
} from '@/services/subscription.service'
import { getCustomers } from '@/services/customer.service'
import { getPlans } from '@/services/plan.service'
import { formatPrice } from '@/types/plan.types'
import { formatDateShort, formatDateTime } from '@/utils/format'
import { useToastStore } from '@/stores/toast.store'
import { useAuthStore } from '@/stores/auth.store'
import FieldTip from '@/components/FieldTip.vue'
import Can from '@/components/Can.vue'
import CustomerDetailModal from '@/components/CustomerDetailModal.vue'
import PlanDetailModal from '@/components/PlanDetailModal.vue'
import type { SubscriptionSummary, SubscriptionStatus, Payment, PaymentMethodOption } from '@/types/subscription.types'
import type { CustomerSummary } from '@/types/customer.types'
import type { PlanSummary } from '@/types/plan.types'
import type { CreateSubscriptionPayload } from '@/types/subscription.types'

const toast = useToastStore()
const auth = useAuthStore()
/** Only users with subscription.update see the auto-renew column / toggle. */
const canUpdateSubscription = computed(() => auth.can('subscription.update'))

// ── State ──
const subs = ref<SubscriptionSummary[]>([])
const loading = ref(true)
const error = ref<string | null>(null)

const page = ref(1)
const pageSize = ref(10)
const totalCount = ref(0)
const totalPages = ref(0)
const search = ref('')
const statusFilter = ref<'all' | SubscriptionStatus>('all')
const sortField = ref<string>('created')
const sortDir = ref<'asc' | 'desc'>('desc')

const stats = ref({ total: 0, active: 0, suspended: 0, expired: 0 })

const toggleBusyId = ref<number | null>(null)
const actionError = ref('')

// ── Detail modal ──
const showDetail = ref(false)
const detailLoading = ref(false)
const selectedSub = ref<SubscriptionSummary | null>(null)
const payments = ref<Payment[]>([])
const paymentsLoading = ref(false)

// ── Composed detail modals (customer + plan) ──
/** Customer detail opened from the subscription modal (read-only, no edit). */
const showCustomerDetail = ref(false)
const customerDetailId = ref<number | null>(null)
/** Plan card opened from the subscription/customer detail (read-only). */
const planModalId = ref<number | null>(null)

// ── Delete modal ──
const showDelete = ref(false)
const deleteTarget = ref<SubscriptionSummary | null>(null)
const deleteLoading = ref(false)
const deleteError = ref('')

// ── Create modal ──
const showCreateModal = ref(false)
const createSaving = ref(false)
const createError = ref('')
const createValidation = ref<Record<string, string>>({})

const customerSearch = ref('')
const customerResults = ref<CustomerSummary[]>([])
const customerSearching = ref(false)
const showOnlyNoSub = ref(true)
const selectedCustomer = ref<CustomerSummary | null>(null)

const plans = ref<PlanSummary[]>([])
const plansLoading = ref(false)
const selectedPlan = ref<PlanSummary | null>(null)

const paymentMethods = ref<PaymentMethodOption[]>([])
const createForm = ref({
  paymentMethod: 'Mock',
  phoneNumber: '',
  autoRenew: true,
  referenceNotes: '',
})

// ── Computed ──
const pageNumbers = computed(() => {
  const pages: number[] = []
  for (let i = 1; i <= totalPages.value; i++) pages.push(i)
  return pages
})

const createReady = computed(() =>
  selectedCustomer.value !== null && selectedPlan.value !== null && createForm.value.phoneNumber.trim() !== '',
)

// ── Fetch ──
async function fetchSubs() {
  loading.value = true
  error.value = null
  try {
    const result = await getSubscriptions({
      page: page.value,
      pageSize: pageSize.value,
      search: search.value || undefined,
      status: statusFilter.value,
      sortBy: sortField.value,
      sortDesc: sortDir.value === 'desc',
    })
    subs.value = result.items
    totalCount.value = result.totalCount
    totalPages.value = result.totalPages
  } catch (e: unknown) {
    error.value = errMsg(e, 'Failed to load subscriptions')
  } finally {
    loading.value = false
  }
}

async function fetchStats() {
  try {
    stats.value = await getSubscriptionStats()
  } catch {
    // Non-critical; cards keep last known values
  }
}

onMounted(() => {
  fetchSubs()
  fetchStats()
})

// ── Search with debounce ──
let debounceTimer: ReturnType<typeof setTimeout> | null = null
watch(search, () => {
  if (debounceTimer) clearTimeout(debounceTimer)
  debounceTimer = setTimeout(() => {
    page.value = 1
    fetchSubs()
  }, 300)
})

// ── Sort ──
function toggleSort(field: string) {
  if (sortField.value === field) sortDir.value = sortDir.value === 'asc' ? 'desc' : 'asc'
  else { sortField.value = field; sortDir.value = 'asc' }
  page.value = 1
  fetchSubs()
}
function sortIcon(field: string) {
  if (sortField.value !== field) return '↕'
  return sortDir.value === 'asc' ? '↑' : '↓'
}

// ── Pagination ──
function goToPage(p: number) {
  page.value = p
  fetchSubs()
  window.scrollTo({ top: 0, behavior: 'smooth' })
}

function clearFilters() {
  search.value = ''
  statusFilter.value = 'all'
  page.value = 1
  fetchSubs()
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

function statusClass(status: SubscriptionStatus): string {
  switch (status) {
    case 'active': return 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-300'
    case 'suspended': return 'bg-yellow-100 text-yellow-700 dark:bg-yellow-900/30 dark:text-yellow-300'
    case 'expired': return 'bg-gray-200 text-gray-700 dark:bg-gray-700 dark:text-gray-300'
    default: return 'bg-gray-100 text-gray-600 dark:bg-gray-800 dark:text-gray-400'
  }
}
function statusDot(status: SubscriptionStatus): string {
  switch (status) {
    case 'active': return 'bg-green-500'
    case 'suspended': return 'bg-yellow-500'
    case 'expired': return 'bg-gray-400'
    default: return 'bg-gray-400'
  }
}

function paymentMethodLabel(method: string | null): string {
  if (!method) return '—'
  const found = paymentMethods.value.find(m => m.value.toLowerCase() === method.toLowerCase())
  return found?.label ?? method
}

// ── Create modal ──
let customerSearchTimer: ReturnType<typeof setTimeout> | null = null

async function searchCustomers() {
  customerSearching.value = true
  try {
    const result = await getCustomers(1, 8, customerSearch.value || undefined, 'name', false, showOnlyNoSub.value ? 'none' : 'all')
    customerResults.value = result.items
  } catch {
    customerResults.value = []
  } finally {
    customerSearching.value = false
  }
}

watch(customerSearch, () => {
  if (customerSearchTimer) clearTimeout(customerSearchTimer)
  customerSearchTimer = setTimeout(() => { searchCustomers() }, 250)
})

watch(showOnlyNoSub, () => { searchCustomers() })

async function openCreate() {
  createError.value = ''
  createValidation.value = {}
  createSaving.value = false
  selectedCustomer.value = null
  selectedPlan.value = null
  customerResults.value = []
  customerSearch.value = ''
  createForm.value = { paymentMethod: 'Mock', phoneNumber: '', autoRenew: true, referenceNotes: '' }
  showCreateModal.value = true

  plansLoading.value = true
  try {
    const [planList, methods] = await Promise.all([getPlans(), getPaymentMethods()])
    plans.value = planList
    paymentMethods.value = methods
    if (methods.length > 0) createForm.value.paymentMethod = methods[0]!.value
  } catch (e: unknown) {
    createError.value = errMsg(e, 'Could not load plans or payment methods')
  } finally {
    plansLoading.value = false
  }
  searchCustomers()
}

function selectCustomer(c: CustomerSummary) {
  selectedCustomer.value = c
  if (!createForm.value.phoneNumber || createForm.value.phoneNumber === '') {
    createForm.value.phoneNumber = c.phone
  }
}

function selectPlan(p: PlanSummary) {
  selectedPlan.value = p
}

function closeCreate() {
  if (createSaving.value) return
  showCreateModal.value = false
}

async function submitCreate() {
  if (!selectedCustomer.value || !selectedPlan.value) {
    createValidation.value = {
      customer: selectedCustomer.value ? '' : 'Select a customer',
      plan: selectedPlan.value ? '' : 'Select a plan',
    }
    return
  }
  if (!createForm.value.phoneNumber.trim()) {
    createValidation.value = { phone: 'Phone number is required for the payment' }
    return
  }
  createValidation.value = {}
  createSaving.value = true
  createError.value = ''

  const payload: CreateSubscriptionPayload = {
    customerId: selectedCustomer.value.id,
    packageId: selectedPlan.value.id,
    autoRenew: createForm.value.autoRenew,
    paymentMethod: createForm.value.paymentMethod,
    phoneNumber: createForm.value.phoneNumber.trim(),
    referenceNotes: createForm.value.referenceNotes.trim() || null,
  }

  try {
    const created = await createSubscription(payload)
    showCreateModal.value = false
    toast.success(`Subscription created for ${created.customerFullName} — ${created.planName}`)
    page.value = 1
    fetchSubs()
    fetchStats()
  } catch (e: unknown) {
    createError.value = errMsg(e, 'Failed to create subscription')
    toast.error(createError.value)
  } finally {
    createSaving.value = false
  }
}

// ── Detail modal ──
async function openDetail(s: SubscriptionSummary) {
  showDetail.value = true
  detailLoading.value = true
  paymentsLoading.value = true
  selectedSub.value = null
  payments.value = []
  try {
    const [sub, history] = await Promise.all([getSubscriptionById(s.id), getPayments(s.id)])
    selectedSub.value = sub
    payments.value = history
  } catch (e: unknown) {
    selectedSub.value = null
    toast.error(errMsg(e, 'Failed to load subscription details'))
  } finally {
    detailLoading.value = false
    paymentsLoading.value = false
  }
}

function closeDetail() {
  showDetail.value = false
  selectedSub.value = null
  payments.value = []
}

// ── Composed modals (view customer / plan from subscription detail) ──
/** Open the customer detail modal on top of the subscription modal (read-only). */
function openCustomerDetail() {
  if (!selectedSub.value) return
  customerDetailId.value = selectedSub.value.customerId
  showCustomerDetail.value = true
}
function closeCustomerDetail() {
  showCustomerDetail.value = false
  customerDetailId.value = null
}

/** A subscription's plan card can be viewed if it is not expired (paused included). */
function subCanViewPlan(sub: SubscriptionSummary | null): boolean {
  if (!sub) return false
  if (sub.status === 'expired') return false
  if (sub.currentPeriodEnd) {
    const end = new Date(sub.currentPeriodEnd)
    if (!Number.isNaN(end.getTime()) && end.getTime() < Date.now()) return false
  }
  return true
}
function openPlanDetail() {
  if (!selectedSub.value) return
  planModalId.value = selectedSub.value.packageId
}
function closePlanDetail() {
  planModalId.value = null
}

// ── Quick actions ──
async function toggleAutoRenew(s: SubscriptionSummary) {
  if (toggleBusyId.value !== null) return
  toggleBusyId.value = s.id
  actionError.value = ''
  try {
    const updated = await updateSubscription(s.id, { autoRenew: !s.autoRenew })
    const idx = subs.value.findIndex(x => x.id === s.id)
    if (idx !== -1) subs.value[idx] = updated
    toast.success(`Auto-renew ${updated.autoRenew ? 'enabled' : 'disabled'} for ${updated.customerFullName}`)
  } catch (e: unknown) {
    actionError.value = errMsg(e, 'Failed to update auto-renew')
    toast.error(actionError.value)
  } finally {
    toggleBusyId.value = null
  }
}

async function toggleStatus(s: SubscriptionSummary) {
  if (toggleBusyId.value !== null) return
  toggleBusyId.value = s.id
  actionError.value = ''
  const next: SubscriptionStatus = s.status === 'suspended' ? 'active' : 'suspended'
  try {
    // Suspends/resumes via the dedicated status endpoint (subscription.suspend)
    const updated = await updateSubscriptionStatus(s.id, next)
    const idx = subs.value.findIndex(x => x.id === s.id)
    if (idx !== -1) subs.value[idx] = updated
    toast.success(updated.status === 'active' ? `Subscription for ${updated.customerFullName} resumed` : `Subscription for ${updated.customerFullName} suspended`)
    // Row may no longer match the active/suspended filter — refresh
    if (statusFilter.value !== 'all') fetchSubs()
    fetchStats()
  } catch (e: unknown) {
    actionError.value = errMsg(e, 'Failed to update subscription status')
    toast.error(actionError.value)
  } finally {
    toggleBusyId.value = null
  }
}

// ── Delete ──
function openDelete(s: SubscriptionSummary) {
  deleteTarget.value = s
  deleteError.value = ''
  showDelete.value = true
}

async function confirmDelete() {
  if (!deleteTarget.value) return
  const target = deleteTarget.value
  deleteLoading.value = true
  deleteError.value = ''
  try {
    await deleteSubscription(target.id)
    subs.value = subs.value.filter(x => x.id !== target.id)
    totalCount.value = Math.max(0, totalCount.value - 1)
    totalPages.value = Math.max(1, Math.ceil(totalCount.value / pageSize.value))
    if (subs.value.length === 0 && page.value > 1) {
      page.value -= 1
      fetchSubs()
    }
    showDelete.value = false
    deleteTarget.value = null
    toast.success(`Subscription for ${target.customerFullName} deleted`)
    fetchStats()
  } catch (e: unknown) {
    deleteError.value = errMsg(e, 'Failed to delete subscription')
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
        <h1 class="text-2xl font-bold text-gray-800 dark:text-gray-100">Subscriptions</h1>
        <p class="text-sm text-gray-500 dark:text-gray-400 mt-1">
          {{ totalCount }} total · {{ stats.active }} active · {{ stats.suspended }} suspended · {{ stats.expired }} expired
        </p>
      </div>
      <div class="flex items-center gap-3">
        <div class="relative">
          <svg class="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
            <path stroke-linecap="round" stroke-linejoin="round" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
          </svg>
          <input v-model="search" type="text" placeholder="Search customer, plan, ppoe..."
            class="pl-10 pr-4 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 text-sm focus:ring-2 focus:ring-blue-500 outline-none transition w-64" />
        </div>
        <select v-model="statusFilter" @change="page = 1; fetchSubs()"
          class="px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 text-sm focus:ring-2 focus:ring-blue-500 outline-none transition cursor-pointer">
          <option value="all">All statuses</option>
          <option value="active">Active</option>
          <option value="suspended">Suspended</option>
          <option value="expired">Expired</option>
        </select>
        <Can permission="subscription.create">
          <button @click="openCreate"
            class="px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white text-sm font-medium rounded-lg transition-all duration-200 cursor-pointer flex items-center gap-2 whitespace-nowrap">
            <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
              <path stroke-linecap="round" stroke-linejoin="round" d="M12 4v16m8-8H4" />
            </svg>
            New Subscription
          </button>
        </Can>
      </div>
    </div>

    <!-- ── Action error banner ── -->
    <div v-if="actionError"
      class="mb-4 px-4 py-3 rounded-lg bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 text-sm text-red-600 dark:text-red-400">
      {{ actionError }}
    </div>

    <!-- ── Stats cards ── -->
    <div class="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-6">
      <div class="bg-white dark:bg-gray-900 rounded-xl shadow-sm border border-gray-100 dark:border-gray-800 p-4">
        <p class="text-xs font-medium text-gray-400 dark:text-gray-500">Total Subscriptions</p>
        <p class="text-2xl font-bold text-gray-800 dark:text-gray-100 mt-1">{{ stats.total }}</p>
      </div>
      <div class="bg-white dark:bg-gray-900 rounded-xl shadow-sm border border-gray-100 dark:border-gray-800 p-4">
        <p class="text-xs font-medium text-gray-400 dark:text-gray-500">Active</p>
        <p class="text-2xl font-bold text-green-600 dark:text-green-400 mt-1">{{ stats.active }}</p>
      </div>
      <div class="bg-white dark:bg-gray-900 rounded-xl shadow-sm border border-gray-100 dark:border-gray-800 p-4">
        <p class="text-xs font-medium text-gray-400 dark:text-gray-500">Suspended</p>
        <p class="text-2xl font-bold text-yellow-600 dark:text-yellow-400 mt-1">{{ stats.suspended }}</p>
      </div>
      <div class="bg-white dark:bg-gray-900 rounded-xl shadow-sm border border-gray-100 dark:border-gray-800 p-4">
        <p class="text-xs font-medium text-gray-400 dark:text-gray-500">Expired</p>
        <p class="text-2xl font-bold text-gray-500 dark:text-gray-400 mt-1">{{ stats.expired }}</p>
      </div>
    </div>

    <!-- ── Page Size ── -->
    <div class="flex items-center gap-2 mb-4">
      <span class="text-xs text-gray-400 dark:text-gray-500">Show</span>
      <select v-model="pageSize" @change="page = 1; fetchSubs()"
        class="px-2 py-1 rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 text-xs outline-none cursor-pointer">
        <option :value="5">5</option>
        <option :value="10">10</option>
        <option :value="20">20</option>
        <option :value="50">50</option>
      </select>
      <span class="text-xs text-gray-400 dark:text-gray-500">per page</span>
    </div>

    <!-- ── Loading ── -->
    <div v-if="loading && subs.length === 0" class="flex justify-center py-20">
      <div class="flex flex-col items-center gap-3">
        <div class="w-8 h-8 border-4 border-blue-500 border-t-transparent rounded-full animate-spin"></div>
        <p class="text-sm text-gray-400 dark:text-gray-500">Loading subscriptions...</p>
      </div>
    </div>

    <!-- ── Error ── -->
    <div v-else-if="error && subs.length === 0" class="text-center py-20">
      <div class="bg-red-50 dark:bg-red-900/30 border border-red-200 dark:border-red-800 rounded-xl p-6 max-w-md mx-auto">
        <p class="text-red-600 dark:text-red-400 font-medium">{{ error }}</p>
        <button @click="fetchSubs" class="mt-3 px-4 py-2 bg-red-600 hover:bg-red-700 text-white text-sm rounded-lg transition cursor-pointer">Retry</button>
      </div>
    </div>

    <!-- ── Table ── -->
    <div v-else class="bg-white dark:bg-gray-900 rounded-xl shadow-sm border border-gray-100 dark:border-gray-800 overflow-hidden">
      <div class="overflow-x-auto">
        <table class="w-full text-sm">
          <thead>
            <tr class="border-b border-gray-100 dark:border-gray-800 bg-gray-50 dark:bg-gray-800/50">
              <th @click="toggleSort('customer')" class="px-4 py-3 text-left font-semibold text-gray-600 dark:text-gray-400 cursor-pointer hover:text-blue-600 dark:hover:text-blue-400 transition whitespace-nowrap">Customer <span class="text-xs ml-1">{{ sortIcon('customer') }}</span></th>
              <th @click="toggleSort('plan')" class="px-4 py-3 text-left font-semibold text-gray-600 dark:text-gray-400 cursor-pointer hover:text-blue-600 dark:hover:text-blue-400 transition whitespace-nowrap hidden md:table-cell">Plan <span class="text-xs ml-1">{{ sortIcon('plan') }}</span></th>
              <th @click="toggleSort('username')" class="px-4 py-3 text-left font-semibold text-gray-600 dark:text-gray-400 cursor-pointer hover:text-blue-600 dark:hover:text-blue-400 transition whitespace-nowrap hidden lg:table-cell">PPPoE <span class="text-xs ml-1">{{ sortIcon('username') }}</span></th>
              <th @click="toggleSort('status')" class="px-4 py-3 text-left font-semibold text-gray-600 dark:text-gray-400 cursor-pointer hover:text-blue-600 dark:hover:text-blue-400 transition whitespace-nowrap">Status <span class="text-xs ml-1">{{ sortIcon('status') }}</span></th>
              <th @click="toggleSort('periodend')" class="px-4 py-3 text-left font-semibold text-gray-600 dark:text-gray-400 cursor-pointer hover:text-blue-600 dark:hover:text-blue-400 transition whitespace-nowrap hidden sm:table-cell">Period End <span class="text-xs ml-1">{{ sortIcon('periodend') }}</span></th>
              <th v-if="canUpdateSubscription" class="px-4 py-3 text-left font-semibold text-gray-600 dark:text-gray-400 whitespace-nowrap hidden md:table-cell">Auto-Renew</th>
              <th class="px-4 py-3 text-left font-semibold text-gray-600 dark:text-gray-400 whitespace-nowrap hidden xl:table-cell">Latest Payment</th>
              <th class="px-4 py-3 text-right font-semibold text-gray-600 dark:text-gray-400 whitespace-nowrap">Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="s in subs" :key="s.id"
              class="border-b border-gray-50 dark:border-gray-800/50 hover:bg-gray-50 dark:hover:bg-gray-800/30 transition">
              <td class="px-4 py-3">
                <div class="flex items-center gap-3">
                  <div class="w-8 h-8 rounded-full bg-blue-100 dark:bg-blue-900/50 flex items-center justify-center text-xs font-bold text-blue-600 dark:text-blue-400 flex-shrink-0">{{ s.customerFullName.charAt(0).toUpperCase() }}</div>
                  <div>
                    <p class="font-medium text-gray-800 dark:text-gray-200">{{ s.customerFullName }}</p>
                    <p class="text-xs text-gray-400 dark:text-gray-500 font-mono">{{ s.customerCode }}</p>
                  </div>
                </div>
              </td>
              <td class="px-4 py-3 hidden md:table-cell">
                <p class="font-medium text-gray-800 dark:text-gray-200">{{ s.planName }}</p>
                <p class="text-xs text-gray-400 dark:text-gray-500">{{ s.paidAmountCents > 0 ? formatPrice(s.paidAmountCents) : '—' }}</p>
              </td>
              <td class="px-4 py-3 text-gray-600 dark:text-gray-400 font-mono text-xs hidden lg:table-cell">{{ s.username || '—' }}</td>
              <td class="px-4 py-3">
                <div class="flex items-center gap-1.5">
                  <span :class="statusDot(s.status)" class="w-2 h-2 rounded-full inline-block"></span>
                  <span :class="statusClass(s.status)" class="px-2 py-0.5 rounded-full text-xs font-medium capitalize">{{ s.status }}</span>
                </div>
              </td>
              <td class="px-4 py-3 text-gray-600 dark:text-gray-400 hidden sm:table-cell whitespace-nowrap">{{ formatDateShort(s.currentPeriodEnd) }}</td>
              <td v-if="canUpdateSubscription" class="px-4 py-3 hidden md:table-cell">
                <Can permission="subscription.update">
                  <label class="relative inline-flex items-center cursor-pointer">
                    <input type="checkbox" class="sr-only peer" :checked="s.autoRenew" @change="toggleAutoRenew(s)" :disabled="toggleBusyId !== null" />
                    <div class="w-9 h-5 bg-gray-300 dark:bg-gray-600 rounded-full peer peer-checked:bg-green-500 after:content-[''] after:absolute after:top-0.5 after:left-0.5 after:bg-white after:rounded-full after:h-4 after:w-4 after:transition-all peer-checked:after:translate-x-4"></div>
                  </label>
                </Can>
              </td>
              <td class="px-4 py-3 text-xs text-gray-600 dark:text-gray-400 hidden xl:table-cell">
                <template v-if="s.paymentReference">
                  <p class="font-medium text-gray-700 dark:text-gray-300">{{ paymentMethodLabel(s.paymentMethod) }}</p>
                  <p class="text-gray-400 dark:text-gray-500 font-mono">{{ s.paymentReference }}</p>
                </template>
                <span v-else class="text-gray-400 dark:text-gray-500">No payment</span>
              </td>
              <td class="px-4 py-3 text-right">
                <div class="flex items-center justify-end gap-1.5">
                  <button @click="openDetail(s)"
                    class="px-3 py-1.5 text-xs font-medium text-blue-600 dark:text-blue-400 bg-blue-50 dark:bg-blue-900/20 hover:bg-blue-100 dark:hover:bg-blue-900/40 rounded-lg transition cursor-pointer">
                    View
                  </button>
                  <Can permission="subscription.suspend">
                    <button v-if="s.status !== 'expired'" @click="toggleStatus(s)"
                      class="px-3 py-1.5 text-xs font-medium rounded-lg transition cursor-pointer"
                      :class="s.status === 'active'
                        ? 'text-yellow-600 dark:text-yellow-400 bg-yellow-50 dark:bg-yellow-900/20 hover:bg-yellow-100 dark:hover:bg-yellow-900/40'
                        : 'text-green-600 dark:text-green-400 bg-green-50 dark:bg-green-900/20 hover:bg-green-100 dark:hover:bg-green-900/40'">
                      {{ s.status === 'active' ? 'Suspend' : 'Resume' }}
                    </button>
                  </Can>
                  <Can permission="subscription.delete">
                    <button @click="openDelete(s)"
                      class="px-3 py-1.5 text-xs font-medium text-red-600 dark:text-red-400 bg-red-50 dark:bg-red-900/20 hover:bg-red-100 dark:hover:bg-red-900/40 rounded-lg transition cursor-pointer">
                      Delete
                    </button>
                  </Can>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- ── Empty State ── -->
      <div v-if="subs.length === 0 && !loading && !error" class="text-center py-16">
        <p class="text-gray-400 dark:text-gray-500">No subscriptions match your filters</p>
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

    <!-- ── New Subscription Modal ── -->
    <Teleport to="body">
      <div v-if="showCreateModal" class="fixed inset-0 z-50 flex items-center justify-center p-4" @click.self="closeCreate">
        <div class="absolute inset-0 bg-black/50 backdrop-blur-sm" @click="closeCreate"></div>
        <div class="relative bg-white dark:bg-gray-900 rounded-2xl shadow-2xl w-full max-w-3xl max-h-[90vh] overflow-y-auto animate-modal-in p-6">

          <div class="flex items-center justify-between mb-5">
            <h2 class="text-lg font-bold text-gray-800 dark:text-gray-100">New Subscription</h2>
            <button @click="closeCreate" :disabled="createSaving"
              class="w-7 h-7 rounded-full bg-gray-100 dark:bg-gray-800 flex items-center justify-center text-gray-400 hover:bg-gray-200 dark:hover:bg-gray-700 cursor-pointer text-sm disabled:opacity-40">✕</button>
          </div>

          <!-- ── Step 1: Customer ── -->
          <div class="mb-6">
            <h3 class="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-3 flex items-center gap-1.5">
              <span class="text-blue-600 dark:text-blue-400 mr-1.5">1.</span>Select Customer
              <FieldTip text="Pick an existing customer to subscribe. Customers without an active subscription are shown first." />
            </h3>

            <div class="flex items-center gap-3 mb-3">
              <div class="relative flex-1">
                <svg class="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                  <path stroke-linecap="round" stroke-linejoin="round" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
                </svg>
                <input v-model="customerSearch" type="text" placeholder="Search customers by name, code or phone..."
                  class="w-full pl-10 pr-4 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 text-sm focus:ring-2 focus:ring-blue-500 outline-none transition" />
              </div>
              <div class="flex items-center gap-1.5">
                <label class="flex items-center gap-2 text-xs text-gray-500 dark:text-gray-400 whitespace-nowrap cursor-pointer">
                  <input v-model="showOnlyNoSub" type="checkbox" class="rounded border-gray-300 dark:border-gray-600 text-blue-600 focus:ring-blue-500 cursor-pointer" />
                  No active sub only
                </label>
                <FieldTip text="Only show customers who do not already have an active subscription. Only they are accepted by the system." />
              </div>
            </div>

            <div v-if="customerSearching" class="text-xs text-gray-400 dark:text-gray-500 py-2">Searching...</div>

            <!-- Results -->
            <div v-else-if="!selectedCustomer && customerResults.length > 0"
              class="border border-gray-200 dark:border-gray-700 rounded-xl divide-y divide-gray-100 dark:divide-gray-800 max-h-56 overflow-y-auto">
              <button v-for="c in customerResults" :key="c.id" @click="selectCustomer(c)"
                class="w-full flex items-center justify-between px-4 py-2.5 text-left hover:bg-gray-50 dark:hover:bg-gray-800/50 transition cursor-pointer">
                <div class="flex items-center gap-3">
                  <div class="w-8 h-8 rounded-full bg-blue-100 dark:bg-blue-900/50 flex items-center justify-center text-xs font-bold text-blue-600 dark:text-blue-400 flex-shrink-0">{{ c.fullName.charAt(0).toUpperCase() }}</div>
                  <div>
                    <p class="text-sm font-medium text-gray-800 dark:text-gray-200">{{ c.fullName }}</p>
                    <p class="text-xs text-gray-400 dark:text-gray-500 font-mono">{{ c.customerCode }} · {{ c.phone }}</p>
                  </div>
                </div>
                <span class="text-xs px-2 py-0.5 rounded-full font-medium"
                  :class="c.hasActiveSubscription
                    ? 'bg-amber-100 text-amber-700 dark:bg-amber-900/30 dark:text-amber-300'
                    : 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-300'">
                  {{ c.hasActiveSubscription ? 'Has active sub' : 'No active sub' }}
                </span>
              </button>
            </div>

            <p v-else-if="!selectedCustomer && !customerSearching" class="text-xs text-gray-400 dark:text-gray-500 py-2">
              {{ customerResults.length === 0 ? 'No customers found' : '' }}
            </p>

            <!-- Selected customer card -->
            <div v-if="selectedCustomer"
              class="flex items-center justify-between gap-3 rounded-xl border border-blue-200 dark:border-blue-800/60 bg-blue-50 dark:bg-blue-900/20 px-4 py-3">
              <div class="flex items-center gap-3">
                <div class="w-9 h-9 rounded-full bg-blue-600 flex items-center justify-center text-sm font-bold text-white flex-shrink-0">{{ selectedCustomer.fullName.charAt(0).toUpperCase() }}</div>
                <div>
                  <p class="text-sm font-semibold text-gray-800 dark:text-gray-100">{{ selectedCustomer.fullName }}</p>
                  <p class="text-xs text-gray-500 dark:text-gray-400 font-mono">{{ selectedCustomer.customerCode }} · {{ selectedCustomer.phone }}</p>
                </div>
              </div>
              <div class="flex items-center gap-2">
                <span class="text-xs px-2 py-0.5 rounded-full font-medium"
                  :class="selectedCustomer.hasActiveSubscription
                    ? 'bg-amber-100 text-amber-700 dark:bg-amber-900/30 dark:text-amber-300'
                    : 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-300'">
                  {{ selectedCustomer.hasActiveSubscription ? 'Has active sub' : 'No active sub' }}
                </span>
                <button @click="selectedCustomer = null" class="text-xs text-gray-400 hover:text-gray-600 dark:hover:text-gray-200 cursor-pointer">Change</button>
              </div>
            </div>
            <p v-if="selectedCustomer?.hasActiveSubscription" class="text-xs text-amber-600 dark:text-amber-400 mt-2">
              This customer already has an active subscription — the system will reject the new one until it is suspended or expired.
            </p>
          </div>

          <!-- ── Step 2: Plan ── -->
          <div class="mb-6">
            <h3 class="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-3 flex items-center gap-1.5">
              <span class="text-blue-600 dark:text-blue-400 mr-1.5">2.</span>Select Plan
              <FieldTip text="Choose the speed plan for this subscription. Only active plans can be selected." />
            </h3>

            <div v-if="plansLoading" class="text-xs text-gray-400 dark:text-gray-500 py-2">Loading plans...</div>
            <div v-else-if="plans.length === 0" class="text-xs text-gray-400 dark:text-gray-500 py-2">No active plans available.</div>
            <div v-else class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3">
              <button v-for="p in plans" :key="p.id" type="button" @click="selectPlan(p)"
                class="text-left rounded-xl border px-4 py-3 transition cursor-pointer"
                :class="selectedPlan?.id === p.id
                  ? 'border-blue-500 dark:border-blue-500 ring-2 ring-blue-500/30 bg-blue-50 dark:bg-blue-900/20'
                  : 'border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 hover:border-blue-300 dark:hover:border-blue-700'">
                <p class="font-semibold text-gray-800 dark:text-gray-100 text-sm">{{ p.name }}</p>
                <p class="text-xs text-gray-400 dark:text-gray-500 mt-0.5">{{ p.bandwidthDownKbps ? `${Math.floor(p.bandwidthDownKbps / 1000)} Mbps` : 'Unlimited' }} · {{ p.billingCycle }}</p>
                <p class="text-sm font-bold text-blue-600 dark:text-blue-400 mt-2">{{ formatPrice(p.priceCents) }}</p>
              </button>
            </div>
          </div>

          <!-- ── Step 3: Billing ── -->
          <div class="mb-6">
            <h3 class="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-3 flex items-center gap-1.5">
              <span class="text-blue-600 dark:text-blue-400 mr-1.5">3.</span>Billing &amp; Payment
              <FieldTip text="The payment provider processes the STK push. M-Pesa and Airtel are simulated in this environment." />
            </h3>

            <div class="grid grid-cols-1 sm:grid-cols-3 gap-3 mb-4">
              <label v-for="m in paymentMethods" :key="m.value"
                class="flex items-center gap-2.5 rounded-xl border px-4 py-3 cursor-pointer transition"
                :class="createForm.paymentMethod === m.value
                  ? 'border-blue-500 dark:border-blue-500 ring-2 ring-blue-500/30 bg-blue-50 dark:bg-blue-900/20'
                  : 'border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 hover:border-blue-300 dark:hover:border-blue-700'">
                <input v-model="createForm.paymentMethod" type="radio" :value="m.value" class="accent-blue-600 cursor-pointer" />
                <span class="text-sm font-medium text-gray-800 dark:text-gray-100">{{ m.label }}</span>
              </label>
            </div>

            <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <div>
                <div class="flex items-center gap-1.5 mb-1">
                  <label class="text-sm font-medium text-gray-700 dark:text-gray-300">Payment Phone *</label>
                  <FieldTip text="The mobile money line that receives the STK prompt. Pre-filled from the customer phone." />
                </div>
                <input v-model="createForm.phoneNumber" type="tel" placeholder="+254 712 345 678"
                  class="w-full px-3 py-2 rounded-lg border text-sm focus:ring-2 focus:ring-blue-500 outline-none transition"
                  :class="createValidation.phone ? 'border-red-400' : 'border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100'" />
                <p v-if="createValidation.phone" class="text-xs text-red-500 mt-1">{{ createValidation.phone }}</p>
              </div>
              <div>
                <div class="flex items-center gap-1.5 mb-1">
                  <label class="text-sm font-medium text-gray-700 dark:text-gray-300">Reference Notes</label>
                  <FieldTip text="Optional note attached to this subscription, e.g. office install or promo discount." />
                </div>
                <input v-model="createForm.referenceNotes" type="text" placeholder="Optional note"
                  class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 text-sm focus:ring-2 focus:ring-blue-500 outline-none transition" />
              </div>
            </div>

            <div class="flex items-center gap-1.5 mt-4">
              <label class="flex items-center gap-2 text-sm text-gray-700 dark:text-gray-300 cursor-pointer">
                <input v-model="createForm.autoRenew" type="checkbox" class="rounded border-gray-300 dark:border-gray-600 text-blue-600 focus:ring-blue-500 cursor-pointer" />
                Auto-renew at period end
              </label>
              <FieldTip text="When enabled, the customer is billed again automatically when the 30-day period ends." />
            </div>
          </div>

          <!-- ── Summary ── -->
          <div class="rounded-xl border border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-800/60 px-4 py-3 mb-4 flex flex-wrap items-center gap-x-6 gap-y-2 text-sm">
            <span class="text-gray-500 dark:text-gray-400">Customer: <span class="font-semibold text-gray-800 dark:text-gray-100">{{ selectedCustomer?.fullName || '—' }}</span></span>
            <span class="text-gray-500 dark:text-gray-400">Plan: <span class="font-semibold text-gray-800 dark:text-gray-100">{{ selectedPlan?.name || '—' }}</span></span>
            <span class="text-gray-500 dark:text-gray-400">Amount: <span class="font-bold text-blue-600 dark:text-blue-400">{{ selectedPlan ? formatPrice(selectedPlan.priceCents) : '—' }}</span></span>
            <span class="text-gray-500 dark:text-gray-400">Method: <span class="font-semibold text-gray-800 dark:text-gray-100">{{ paymentMethodLabel(createForm.paymentMethod) }}</span></span>
          </div>

          <p v-if="createError" class="mb-4 text-sm text-red-600 dark:text-red-400 bg-red-50 dark:bg-red-900/20 rounded-lg px-3 py-2">{{ createError }}</p>

          <div class="flex gap-3 pt-2">
            <button type="button" @click="closeCreate" :disabled="createSaving"
              class="flex-1 px-4 py-2.5 text-sm font-medium text-gray-600 dark:text-gray-300 bg-gray-100 dark:bg-gray-800 hover:bg-gray-200 dark:hover:bg-gray-700 rounded-lg transition cursor-pointer disabled:opacity-40">
              Cancel
            </button>
            <button type="button" @click="submitCreate" :disabled="createSaving || !createReady"
              class="flex-1 px-4 py-2.5 text-sm font-medium text-white bg-blue-600 hover:bg-blue-700 rounded-lg transition-all duration-200 cursor-pointer disabled:opacity-50 flex items-center justify-center gap-2">
              <span v-if="createSaving" class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
              Create Subscription
            </button>
          </div>
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

          <div v-else-if="selectedSub" class="space-y-6">
            <!-- Header -->
            <div class="flex items-start justify-between gap-4 pb-4 border-b border-gray-100 dark:border-gray-800">
              <div>
                <div class="flex items-center gap-2">
                  <h2 class="text-xl font-bold text-gray-800 dark:text-gray-100">{{ selectedSub.customerFullName }}</h2>
                  <span :class="statusClass(selectedSub.status)" class="px-2 py-0.5 rounded-full text-xs font-medium capitalize">{{ selectedSub.status }}</span>
                </div>
                <p class="text-xs text-gray-400 dark:text-gray-500 font-mono mt-1">{{ selectedSub.customerCode }}</p>
              </div>
              <div class="text-right">
                <p class="text-xs text-gray-400 dark:text-gray-500">Subscription #{{ selectedSub.id }}</p>
                <p class="text-sm font-semibold text-gray-800 dark:text-gray-100 mt-1">{{ selectedSub.planName }}</p>
              </div>
            </div>

            <!-- Quick actions: view the linked customer / plan card (read-only) -->
            <div class="flex flex-wrap items-center gap-2 -mt-1">
              <Can permission="customer.view">
                <button @click="openCustomerDetail"
                  class="px-3 py-1.5 text-xs font-medium text-blue-600 dark:text-blue-400 bg-blue-50 dark:bg-blue-900/20 hover:bg-blue-100 dark:hover:bg-blue-900/40 rounded-lg transition cursor-pointer">
                  View Customer Info
                </button>
              </Can>
              <button v-if="subCanViewPlan(selectedSub)" @click="openPlanDetail"
                class="px-3 py-1.5 text-xs font-medium text-indigo-600 dark:text-indigo-400 bg-indigo-50 dark:bg-indigo-900/20 hover:bg-indigo-100 dark:hover:bg-indigo-900/40 rounded-lg transition cursor-pointer">
                View Plan Card
              </button>
            </div>

            <!-- Info grid -->
            <div>
              <h3 class="text-sm font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider mb-3">Subscription</h3>
              <div class="grid grid-cols-2 sm:grid-cols-3 gap-3">
                <div class="bg-gray-50 dark:bg-gray-800 rounded-lg p-3">
                  <p class="text-xs text-gray-400 dark:text-gray-500 mb-1">Period Start</p>
                  <p class="font-semibold text-gray-800 dark:text-gray-100 text-sm">{{ formatDateTime(selectedSub.currentPeriodStart) }}</p>
                </div>
                <div class="bg-gray-50 dark:bg-gray-800 rounded-lg p-3">
                  <p class="text-xs text-gray-400 dark:text-gray-500 mb-1">Period End</p>
                  <p class="font-semibold text-gray-800 dark:text-gray-100 text-sm">{{ formatDateTime(selectedSub.currentPeriodEnd) }}</p>
                </div>
                <div class="bg-gray-50 dark:bg-gray-800 rounded-lg p-3">
                  <p class="text-xs text-gray-400 dark:text-gray-500 mb-1">Auto-Renew</p>
                  <p class="font-semibold text-gray-800 dark:text-gray-100 text-sm">{{ selectedSub.autoRenew ? 'Yes' : 'No' }}</p>
                </div>
                <div class="bg-gray-50 dark:bg-gray-800 rounded-lg p-3 sm:col-span-2">
                  <p class="text-xs text-gray-400 dark:text-gray-500 mb-1">PPPoE Username</p>
                  <p class="font-semibold text-gray-800 dark:text-gray-100 text-sm font-mono">{{ selectedSub.username || '—' }}</p>
                </div>
                <div class="bg-gray-50 dark:bg-gray-800 rounded-lg p-3">
                  <p class="text-xs text-gray-400 dark:text-gray-500 mb-1">Amount Paid</p>
                  <p class="font-semibold text-gray-800 dark:text-gray-100 text-sm">{{ selectedSub.paidAmountCents > 0 ? formatPrice(selectedSub.paidAmountCents) : '—' }}</p>
                </div>
              </div>
            </div>

            <!-- Payment history -->
            <div>
              <h3 class="text-sm font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider mb-3">
                Payment History
                <span class="ml-1.5 text-xs font-normal text-gray-400">{{ payments.length }} record{{ payments.length === 1 ? '' : 's' }}</span>
              </h3>

              <div v-if="paymentsLoading" class="flex justify-center py-6">
                <div class="w-6 h-6 border-4 border-blue-500 border-t-transparent rounded-full animate-spin"></div>
              </div>
              <div v-else-if="payments.length === 0" class="text-center py-8 bg-gray-50 dark:bg-gray-800 rounded-xl">
                <p class="text-sm text-gray-400 dark:text-gray-500">No payment records yet</p>
              </div>
              <div v-else class="space-y-2">
                <div v-for="p in payments" :key="p.id"
                  class="flex items-center justify-between gap-3 rounded-xl border border-gray-100 dark:border-gray-800 bg-gray-50 dark:bg-gray-800/60 px-4 py-2.5">
                  <div class="flex items-center gap-3 min-w-0">
                    <span class="w-8 h-8 rounded-full flex items-center justify-center text-xs font-bold flex-shrink-0"
                      :class="p.status === 'Completed'
                        ? 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-300'
                        : 'bg-red-100 text-red-600 dark:bg-red-900/30 dark:text-red-300'">
                      {{ p.status === 'Completed' ? '✓' : '✕' }}
                    </span>
                    <div class="min-w-0">
                      <p class="text-sm font-medium text-gray-800 dark:text-gray-100">{{ formatPrice(p.amountCents) }}</p>
                      <p class="text-xs text-gray-400 dark:text-gray-500">{{ p.paymentMethod }} · {{ formatDateTime(p.completedAt || p.createdAt) }}</p>
                    </div>
                  </div>
                  <div class="text-right min-w-0">
                    <p class="text-xs font-mono text-gray-500 dark:text-gray-400 truncate">{{ p.referenceNumber || '—' }}</p>
                    <p class="text-xs text-gray-400 dark:text-gray-500">{{ p.phoneNumber || '' }}</p>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <div v-else class="p-8 text-center">
            <p class="text-red-500 dark:text-red-400">Failed to load subscription details.</p>
            <button @click="closeDetail" class="mt-4 text-blue-600 dark:text-blue-400 hover:underline cursor-pointer">Close</button>
          </div>
        </div>
      </div>
    </Teleport>

    <!-- ── Composed modals (read-only, stacked above the subscription detail) ──
         These close one at a time: the plan card closes, then the customer, then
         the subscription. Editing the customer is disabled while composed. -->
    <CustomerDetailModal
      :open="showCustomerDetail"
      :customer-id="customerDetailId"
      :editable="false"
      context-label="Customer on this subscription"
      :z-index="60"
      @close="closeCustomerDetail"
    />
    <PlanDetailModal
      :plan-id="planModalId"
      context-label="Plan on this subscription"
      :z-index="70"
      @close="closePlanDetail"
    />

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
              <h2 class="text-lg font-bold text-gray-800 dark:text-gray-100">Delete Subscription</h2>
              <p class="text-sm text-gray-500 dark:text-gray-400 mt-1">
                Are you sure you want to delete the subscription for <span class="font-semibold text-gray-700 dark:text-gray-200">{{ deleteTarget?.customerFullName }}</span> ({{ deleteTarget?.planName }})?
                This removes the subscription, its payments, and the RADIUS credentials. This cannot be undone.
              </p>
            </div>
          </div>

          <p v-if="deleteError" class="mt-4 text-sm text-red-600 dark:text-red-400 bg-red-50 dark:bg-red-900/20 rounded-lg px-3 py-2">{{ deleteError }}</p>

          <div class="flex gap-3 mt-6">
            <button type="button" @click="cancelDelete" :disabled="deleteLoading"
              class="flex-1 px-4 py-2.5 text-sm font-medium text-gray-600 dark:text-gray-300 bg-gray-100 dark:bg-gray-800 hover:bg-gray-200 dark:hover:bg-gray-700 rounded-lg transition cursor-pointer disabled:opacity-40">
              Cancel
            </button>
            <button type="button" @click="confirmDelete" :disabled="deleteLoading"
              class="flex-1 px-4 py-2.5 text-sm font-medium text-white bg-red-600 hover:bg-red-700 rounded-lg transition-all duration-200 cursor-pointer disabled:opacity-50 flex items-center justify-center gap-2">
              <span v-if="deleteLoading" class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
              Delete Subscription
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
