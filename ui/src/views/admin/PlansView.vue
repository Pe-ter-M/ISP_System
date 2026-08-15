<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { getAdminPlans, getPlanStats, getPlanDetail, createPlan, updatePlan, deletePlan } from '@/services/plan.service'
import { formatPrice, formatSpeed, formatDuration } from '@/types/plan.types'
import { useToastStore } from '@/stores/toast.store'
import { useSettingsStore } from '@/stores/settings.store'
import FieldTip from '@/components/FieldTip.vue'
import Can from '@/components/Can.vue'
import type { PlanSummary, PlanDetail, CreatePlanPayload, UpdatePlanPayload } from '@/types/plan.types'

const toast = useToastStore()
const settingsStore = useSettingsStore()
/** Configured currency code (e.g. KES) used in form labels */
const currency = computed(() => settingsStore.value('currency') ?? 'KES')

// ── State ──
const plans = ref<PlanSummary[]>([])
const loading = ref(true)
const error = ref<string | null>(null)

const page = ref(1)
const pageSize = ref(10)
const totalCount = ref(0)
const totalPages = ref(0)
const search = ref('')
const statusFilter = ref<'all' | 'active' | 'inactive'>('all')
const sortField = ref<string>('sortOrder')
const sortDir = ref<'asc' | 'desc'>('asc')

// ── Modal state ──
const showFormModal = ref(false)
const formMode = ref<'create' | 'edit'>('create')
const editingId = ref<number | null>(null)
const formLoading = ref(false) // true while fetching detail to prefill edit form
const formSaving = ref(false)
const formError = ref('')
const formValidation = ref<Record<string, string>>({})
const optimisticPlan = ref<PlanSummary | null>(null)

const showDetail = ref(false)
const detailLoading = ref(false)
const selectedPlan = ref<PlanDetail | null>(null)

const showDelete = ref(false)
const deleteTarget = ref<PlanSummary | null>(null)
const deleteLoading = ref(false)
const deleteError = ref('')

const toggleBusyId = ref<number | null>(null)
const actionError = ref('')

// ── Form ──
const form = ref({
  name: '',
  description: '',
  priceKes: 0,
  billingCycle: 'monthly',
  downloadMbps: 0,
  uploadMbps: 0,
  maxDevices: 1,
  sessionTimeoutHours: 24,
  idleTimeoutMinutes: 10,
  sortOrder: 0,
  isActive: true,
})

const billingCycleOptions = ['monthly', 'quarterly', 'semi-annual', 'yearly']

// ── Stats (server-computed) ──
const stats = ref({ totalPlans: 0, activePlans: 0, inactivePlans: 0, totalSubscribers: 0 })

// ── Pagination (server-driven) ──
const pageNumbers = computed(() => {
  const pages: number[] = []
  for (let i = 1; i <= totalPages.value; i++) pages.push(i)
  return pages
})

// ── Fetch ──
async function fetchPlans() {
  loading.value = true
  error.value = null
  try {
    const result = await getAdminPlans({
      page: page.value,
      pageSize: pageSize.value,
      search: search.value || undefined,
      status: statusFilter.value,
      sortBy: sortField.value,
      sortDesc: sortDir.value === 'desc',
    })
    plans.value = result.items
    totalCount.value = result.totalCount
    totalPages.value = result.totalPages
  } catch (e: unknown) {
    error.value = errMsg(e, 'Failed to load plans')
  } finally {
    loading.value = false
  }
}

async function fetchStats() {
  try {
    stats.value = await getPlanStats()
  } catch {
    // Non-critical; cards just keep their last known values
  }
}

onMounted(() => {
  fetchPlans()
  fetchStats()
})

// ── Search with debounce ──
let debounceTimer: ReturnType<typeof setTimeout> | null = null
watch(search, () => {
  if (debounceTimer) clearTimeout(debounceTimer)
  debounceTimer = setTimeout(() => {
    page.value = 1
    fetchPlans()
  }, 300)
})

// ── Sort (server-side) ──
function toggleSort(field: string) {
  if (sortField.value === field) sortDir.value = sortDir.value === 'asc' ? 'desc' : 'asc'
  else { sortField.value = field; sortDir.value = 'asc' }
  page.value = 1
  fetchPlans()
}
function sortIcon(field: string) {
  if (sortField.value !== field) return '↕'
  return sortDir.value === 'asc' ? '↑' : '↓'
}

// ── Pagination ──
function goToPage(p: number) {
  page.value = p
  fetchPlans()
  window.scrollTo({ top: 0, behavior: 'smooth' })
}

function clearFilters() {
  search.value = ''
  statusFilter.value = 'all'
  page.value = 1
  fetchPlans()
}

// ── Helpers ──
function toNum(v: number | string | null | undefined): number {
  const n = typeof v === 'string' ? Number(v) : Number(v ?? 0)
  return Number.isFinite(n) ? n : 0
}
/** Extract a readable message from API errors (matches the { message, error } error shapes) */
function errMsg(e: unknown, fallback: string): string {
  if (e && typeof e === 'object') {
    const msg = (e as { message?: unknown }).message
    if (typeof msg === 'string' && msg) return msg
    const err = (e as { error?: unknown }).error
    if (typeof err === 'string' && err) return err
  }
  return fallback
}
function speedText(p: PlanSummary): string {
  const down = p.bandwidthDownKbps ? formatSpeed(p.bandwidthDownKbps) : 'Unlimited'
  const up = p.bandwidthUpKbps ? formatSpeed(p.bandwidthUpKbps) : 'Unlimited'
  return `${down} ↓ / ${up} ↑`
}

// ── Form helpers ──
function resetForm() {
  form.value = {
    name: '', description: '', priceKes: 0, billingCycle: 'monthly',
    downloadMbps: 0, uploadMbps: 0, maxDevices: 1,
    sessionTimeoutHours: 24, idleTimeoutMinutes: 10, sortOrder: 0, isActive: true,
  }
}

function fillForm(d: PlanDetail) {
  form.value = {
    name: d.name,
    description: d.description ?? '',
    priceKes: d.priceCents / 100,
    billingCycle: d.billingCycle,
    downloadMbps: (d.bandwidthDownKbps ?? 0) / 1000,
    uploadMbps: (d.bandwidthUpKbps ?? 0) / 1000,
    maxDevices: d.maxDevices,
    sessionTimeoutHours: d.sessionTimeoutSeconds / 3600,
    idleTimeoutMinutes: d.idleTimeoutSeconds / 60,
    sortOrder: d.sortOrder,
    isActive: d.isActive,
  }
}

function validateForm(): boolean {
  const v: Record<string, string> = {}
  if (!form.value.name.trim()) v.name = 'Plan name is required'
  if (toNum(form.value.priceKes) < 0) v.priceKes = 'Price cannot be negative'
  if (toNum(form.value.downloadMbps) < 0) v.downloadMbps = 'Download speed cannot be negative'
  if (toNum(form.value.uploadMbps) < 0) v.uploadMbps = 'Upload speed cannot be negative'
  if (toNum(form.value.maxDevices) < 1) v.maxDevices = 'Must be 1 or greater'
  if (toNum(form.value.sessionTimeoutHours) < 0) v.sessionTimeoutHours = 'Cannot be negative'
  if (toNum(form.value.idleTimeoutMinutes) < 0) v.idleTimeoutMinutes = 'Cannot be negative'
  formValidation.value = v
  return Object.keys(v).length === 0
}

function buildPayload(): CreatePlanPayload | UpdatePlanPayload {
  const downKbps = toNum(form.value.downloadMbps) > 0 ? Math.round(toNum(form.value.downloadMbps) * 1000) : null
  const upKbps = toNum(form.value.uploadMbps) > 0 ? Math.round(toNum(form.value.uploadMbps) * 1000) : null
  const base = {
    name: form.value.name.trim(),
    description: formMode.value === 'create' ? form.value.description.trim() || null : form.value.description.trim(),
    priceCents: Math.round(toNum(form.value.priceKes) * 100),
    billingCycle: form.value.billingCycle.trim() || 'monthly',
    bandwidthUpKbps: upKbps,
    bandwidthDownKbps: downKbps,
    sessionTimeoutSeconds: Math.round(toNum(form.value.sessionTimeoutHours) * 3600),
    idleTimeoutSeconds: Math.round(toNum(form.value.idleTimeoutMinutes) * 60),
    maxDevices: Math.max(1, Math.round(toNum(form.value.maxDevices))),
    sortOrder: Math.round(toNum(form.value.sortOrder)),
  }
  if (formMode.value === 'edit') {
    return { ...base, isActive: form.value.isActive } as UpdatePlanPayload
  }
  return base as CreatePlanPayload
}

// ── Create / Edit ──
function openCreate() {
  formMode.value = 'create'
  editingId.value = null
  resetForm()
  formError.value = ''
  formValidation.value = {}
  formLoading.value = false
  showFormModal.value = true
}

async function openEdit(p: PlanSummary) {
  formMode.value = 'edit'
  editingId.value = p.id
  formError.value = ''
  formValidation.value = {}
  formLoading.value = true
  showFormModal.value = true
  try {
    const d = await getPlanDetail(p.id, true)
    fillForm(d)
  } catch (e: unknown) {
    // Fall back to summary data so the form still opens
    formError.value = errMsg(e, 'Could not load full details — editing with basic fields')
    fillForm({
      id: p.id, name: p.name, description: p.description, priceCents: p.priceCents,
      billingCycle: p.billingCycle, bandwidthUpKbps: p.bandwidthUpKbps,
      bandwidthDownKbps: p.bandwidthDownKbps, sessionTimeoutSeconds: 86400,
      idleTimeoutSeconds: 600, maxDevices: p.maxDevices, isActive: p.isActive,
      sortOrder: p.sortOrder, groupName: '', activeSubscribersCount: p.activeSubscribersCount,
    })
  } finally {
    formLoading.value = false
  }
}

function closeForm() {
  if (formSaving.value) return
  showFormModal.value = false
  editingId.value = null
  optimisticPlan.value = null
}

async function submitForm() {
  if (!validateForm()) return
  formSaving.value = true
  formError.value = ''
  const payload = buildPayload()

  if (formMode.value === 'create') {
    // Optimistic insert at top of list
    const temp: PlanSummary = {
      id: Date.now(),
      name: (payload as CreatePlanPayload).name,
      description: (payload as CreatePlanPayload).description,
      priceCents: (payload as CreatePlanPayload).priceCents,
      billingCycle: (payload as CreatePlanPayload).billingCycle,
      bandwidthUpKbps: (payload as CreatePlanPayload).bandwidthUpKbps,
      bandwidthDownKbps: (payload as CreatePlanPayload).bandwidthDownKbps,
      maxDevices: (payload as CreatePlanPayload).maxDevices ?? 1,
      isActive: true,
      sortOrder: (payload as CreatePlanPayload).sortOrder ?? 0,
      activeSubscribersCount: 0,
    }
    plans.value.unshift(temp)
    optimisticPlan.value = temp
    showFormModal.value = false

    try {
      const created = await createPlan(payload as CreatePlanPayload)
      const idx = plans.value.findIndex(p => p.id === temp.id)
      if (idx !== -1) plans.value[idx] = created
      optimisticPlan.value = null
      totalCount.value++
      totalPages.value = Math.max(1, Math.ceil(totalCount.value / pageSize.value))
      toast.success(`Plan "${created.name}" created`)
      // Jump to page 1 so the new plan is visible; refresh ordering
      if (page.value !== 1) { page.value = 1; fetchPlans() }
      fetchStats()
    } catch (e: unknown) {
      const idx = plans.value.findIndex(p => p.id === temp.id)
      if (idx !== -1) plans.value.splice(idx, 1)
      optimisticPlan.value = null
      formError.value = errMsg(e, 'Failed to create plan')
      toast.error(formError.value)
      showFormModal.value = true
    }
  } else if (editingId.value !== null) {
    try {
      const updated = await updatePlan(editingId.value, payload as UpdatePlanPayload)
      const idx = plans.value.findIndex(p => p.id === updated.id)
      if (idx !== -1) plans.value[idx] = updated
      showFormModal.value = false
      toast.success(`Plan "${updated.name}" updated`)
      fetchStats()
    } catch (e: unknown) {
      formError.value = errMsg(e, 'Failed to update plan')
      toast.error(formError.value)
    }
  }
  formSaving.value = false
}

// ── Detail ──
async function openDetail(p: PlanSummary) {
  showDetail.value = true
  detailLoading.value = true
  selectedPlan.value = null
  try {
    selectedPlan.value = await getPlanDetail(p.id, true)
  } catch {
    selectedPlan.value = null
  } finally {
    detailLoading.value = false
  }
}
function closeDetail() {
  showDetail.value = false
  selectedPlan.value = null
}

/** Edit from the detail modal — snapshot the plan BEFORE closing so it survives */
function editFromDetail() {
  const p = selectedPlan.value
  if (!p) return
  closeDetail()
  openEdit(p)
}

// ── Delete ──
function openDelete(p: PlanSummary) {
  deleteTarget.value = p
  deleteError.value = ''
  showDelete.value = true
}
async function confirmDelete() {
  if (!deleteTarget.value) return
  const target = deleteTarget.value
  deleteLoading.value = true
  deleteError.value = ''
  try {
    await deletePlan(target.id)
    plans.value = plans.value.filter(p => p.id !== target.id)
    totalCount.value = Math.max(0, totalCount.value - 1)
    totalPages.value = Math.max(1, Math.ceil(totalCount.value / pageSize.value))
    // If we emptied the last page, step back one
    if (plans.value.length === 0 && page.value > 1) {
      page.value -= 1
      fetchPlans()
    }
    showDelete.value = false
    deleteTarget.value = null
    toast.success(`Plan "${target.name}" deleted`)
    fetchStats()
  } catch (e: unknown) {
    deleteError.value = errMsg(e, 'Failed to delete plan')
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

// ── Toggle active ──
async function toggleActive(p: PlanSummary) {
  if (toggleBusyId.value !== null) return
  toggleBusyId.value = p.id
  actionError.value = ''
  try {
    const updated = await updatePlan(p.id, { isActive: !p.isActive })
    const idx = plans.value.findIndex(x => x.id === p.id)
    if (idx !== -1) plans.value[idx] = updated
    toast.success(updated.isActive ? `Plan "${updated.name}" activated` : `Plan "${updated.name}" deactivated`)
    // Row may no longer match the active/inactive filter — refresh
    if (statusFilter.value !== 'all') fetchPlans()
    fetchStats()
  } catch (e: unknown) {
    actionError.value = errMsg(e, 'Failed to update plan status')
    toast.error(actionError.value)
  } finally {
    toggleBusyId.value = null
  }
}

// ── Status badge classes ──
function statusClass(active: boolean) {
  return active
    ? 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-300'
    : 'bg-gray-100 text-gray-600 dark:bg-gray-800 dark:text-gray-400'
}
function statusDot(active: boolean) {
  return active ? 'bg-green-500' : 'bg-gray-400'
}
</script>

<template>
  <div>
    <!-- ── Header ── -->
    <div class="flex items-center justify-between mb-6 flex-wrap gap-4">
      <div>
        <h1 class="text-2xl font-bold text-gray-800 dark:text-gray-100">Plans</h1>
        <p class="text-sm text-gray-500 dark:text-gray-400 mt-1">
          {{ totalCount }} total plans · {{ stats.activePlans }} active · {{ stats.totalSubscribers }} subscribers
        </p>
      </div>
      <div class="flex items-center gap-3">
        <div class="relative">
          <svg class="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
            <path stroke-linecap="round" stroke-linejoin="round" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
          </svg>
          <input v-model="search" type="text" placeholder="Search plans..."
            class="pl-10 pr-4 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 text-sm focus:ring-2 focus:ring-blue-500 outline-none transition w-56" />
        </div>
        <select v-model="statusFilter" @change="page = 1; fetchPlans()"
          class="px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 text-sm focus:ring-2 focus:ring-blue-500 outline-none transition cursor-pointer">
          <option value="all">All statuses</option>
          <option value="active">Active</option>
          <option value="inactive">Inactive</option>
        </select>
        <Can permission="plan.create">
          <button @click="openCreate"
            class="px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white text-sm font-medium rounded-lg transition-all duration-200 cursor-pointer flex items-center gap-2 whitespace-nowrap">
            <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
              <path stroke-linecap="round" stroke-linejoin="round" d="M12 4v16m8-8H4" />
            </svg>
            Add Plan
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
        <p class="text-xs font-medium text-gray-400 dark:text-gray-500">Total Plans</p>
        <p class="text-2xl font-bold text-gray-800 dark:text-gray-100 mt-1">{{ stats.totalPlans }}</p>
      </div>
      <div class="bg-white dark:bg-gray-900 rounded-xl shadow-sm border border-gray-100 dark:border-gray-800 p-4">
        <p class="text-xs font-medium text-gray-400 dark:text-gray-500">Active Plans</p>
        <p class="text-2xl font-bold text-green-600 dark:text-green-400 mt-1">{{ stats.activePlans }}</p>
      </div>
      <div class="bg-white dark:bg-gray-900 rounded-xl shadow-sm border border-gray-100 dark:border-gray-800 p-4">
        <p class="text-xs font-medium text-gray-400 dark:text-gray-500">Inactive Plans</p>
        <p class="text-2xl font-bold text-gray-500 dark:text-gray-400 mt-1">{{ stats.inactivePlans }}</p>
      </div>
      <div class="bg-white dark:bg-gray-900 rounded-xl shadow-sm border border-gray-100 dark:border-gray-800 p-4">
        <p class="text-xs font-medium text-gray-400 dark:text-gray-500">Active Subscribers</p>
        <p class="text-2xl font-bold text-blue-600 dark:text-blue-400 mt-1">{{ stats.totalSubscribers }}</p>
      </div>
    </div>

    <!-- ── Page Size ── -->
    <div class="flex items-center gap-2 mb-4">
      <span class="text-xs text-gray-400 dark:text-gray-500">Show</span>
      <select v-model="pageSize" @change="page = 1; fetchPlans()"
        class="px-2 py-1 rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 text-xs outline-none cursor-pointer">
        <option :value="5">5</option>
        <option :value="10">10</option>
        <option :value="20">20</option>
        <option :value="50">50</option>
      </select>
      <span class="text-xs text-gray-400 dark:text-gray-500">per page</span>
    </div>

    <!-- ── Loading ── -->
    <div v-if="loading && plans.length === 0" class="flex justify-center py-20">
      <div class="flex flex-col items-center gap-3">
        <div class="w-8 h-8 border-4 border-blue-500 border-t-transparent rounded-full animate-spin"></div>
        <p class="text-sm text-gray-400 dark:text-gray-500">Loading plans...</p>
      </div>
    </div>

    <!-- ── Error ── -->
    <div v-else-if="error && plans.length === 0" class="text-center py-20">
      <div class="bg-red-50 dark:bg-red-900/30 border border-red-200 dark:border-red-800 rounded-xl p-6 max-w-md mx-auto">
        <p class="text-red-600 dark:text-red-400 font-medium">{{ error }}</p>
        <button @click="fetchPlans" class="mt-3 px-4 py-2 bg-red-600 hover:bg-red-700 text-white text-sm rounded-lg transition cursor-pointer">Retry</button>
      </div>
    </div>

    <!-- ── Table ── -->
    <div v-else class="bg-white dark:bg-gray-900 rounded-xl shadow-sm border border-gray-100 dark:border-gray-800 overflow-hidden">
      <div class="overflow-x-auto">
        <table class="w-full text-sm">
          <thead>
            <tr class="border-b border-gray-100 dark:border-gray-800 bg-gray-50 dark:bg-gray-800/50">
              <th @click="toggleSort('name')" class="px-4 py-3 text-left font-semibold text-gray-600 dark:text-gray-400 cursor-pointer hover:text-blue-600 dark:hover:text-blue-400 transition whitespace-nowrap">Plan <span class="text-xs ml-1">{{ sortIcon('name') }}</span></th>
              <th @click="toggleSort('priceCents')" class="px-4 py-3 text-left font-semibold text-gray-600 dark:text-gray-400 cursor-pointer hover:text-blue-600 dark:hover:text-blue-400 transition whitespace-nowrap">Price <span class="text-xs ml-1">{{ sortIcon('priceCents') }}</span></th>
              <th @click="toggleSort('billingCycle')" class="px-4 py-3 text-left font-semibold text-gray-600 dark:text-gray-400 cursor-pointer hover:text-blue-600 dark:hover:text-blue-400 transition whitespace-nowrap hidden md:table-cell">Cycle <span class="text-xs ml-1">{{ sortIcon('billingCycle') }}</span></th>
              <th @click="toggleSort('speed')" class="px-4 py-3 text-left font-semibold text-gray-600 dark:text-gray-400 cursor-pointer hover:text-blue-600 dark:hover:text-blue-400 transition whitespace-nowrap hidden lg:table-cell">Speed <span class="text-xs ml-1">{{ sortIcon('speed') }}</span></th>
              <th @click="toggleSort('maxDevices')" class="px-4 py-3 text-left font-semibold text-gray-600 dark:text-gray-400 cursor-pointer hover:text-blue-600 dark:hover:text-blue-400 transition whitespace-nowrap hidden sm:table-cell">Devices <span class="text-xs ml-1">{{ sortIcon('maxDevices') }}</span></th>
              <th @click="toggleSort('subscribers')" class="px-4 py-3 text-left font-semibold text-gray-600 dark:text-gray-400 cursor-pointer hover:text-blue-600 dark:hover:text-blue-400 transition whitespace-nowrap hidden sm:table-cell">Subs <span class="text-xs ml-1">{{ sortIcon('subscribers') }}</span></th>
              <th @click="toggleSort('isActive')" class="px-4 py-3 text-left font-semibold text-gray-600 dark:text-gray-400 cursor-pointer hover:text-blue-600 dark:hover:text-blue-400 transition whitespace-nowrap">Status <span class="text-xs ml-1">{{ sortIcon('isActive') }}</span></th>
              <th class="px-4 py-3 text-right font-semibold text-gray-600 dark:text-gray-400 whitespace-nowrap">Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="p in plans" :key="p.id"
              class="border-b border-gray-50 dark:border-gray-800/50 hover:bg-gray-50 dark:hover:bg-gray-800/30 transition"
              :class="{ 'opacity-50': optimisticPlan?.id === p.id }">
              <td class="px-4 py-3">
                <div>
                  <p class="font-medium text-gray-800 dark:text-gray-200">
                    {{ p.name }}
                    <span v-if="optimisticPlan?.id === p.id" class="text-xs text-blue-500 font-normal ml-1">Creating...</span>
                  </p>
                  <p v-if="p.description" class="text-xs text-gray-400 dark:text-gray-500 mt-0.5 max-w-[220px] truncate">{{ p.description }}</p>
                </div>
              </td>
              <td class="px-4 py-3">
                <p class="font-semibold text-gray-800 dark:text-gray-200">{{ formatPrice(p.priceCents) }}</p>
                <p class="text-xs text-gray-400 dark:text-gray-500 capitalize">{{ p.billingCycle }}</p>
              </td>
              <td class="px-4 py-3 text-gray-600 dark:text-gray-400 capitalize hidden md:table-cell">{{ p.billingCycle }}</td>
              <td class="px-4 py-3 text-xs text-gray-600 dark:text-gray-400 hidden lg:table-cell whitespace-nowrap">{{ speedText(p) }}</td>
              <td class="px-4 py-3 text-gray-600 dark:text-gray-400 hidden sm:table-cell">{{ p.maxDevices }}</td>
              <td class="px-4 py-3 text-gray-600 dark:text-gray-400 hidden sm:table-cell">{{ p.activeSubscribersCount ?? 0 }}</td>
              <td class="px-4 py-3">
                <div class="flex items-center gap-2">
                  <span :class="statusDot(p.isActive)" class="w-2 h-2 rounded-full inline-block"></span>
                  <span :class="statusClass(p.isActive)" class="px-2 py-0.5 rounded-full text-xs font-medium">{{ p.isActive ? 'Active' : 'Inactive' }}</span>
                  <Can permission="plan.update">
                    <button
                      @click="toggleActive(p)"
                      :disabled="toggleBusyId !== null"
                      class="text-xs font-medium text-blue-600 dark:text-blue-400 hover:underline disabled:opacity-40 disabled:cursor-not-allowed cursor-pointer transition">
                      {{ toggleBusyId === p.id ? '…' : (p.isActive ? 'Deactivate' : 'Activate') }}
                    </button>
                  </Can>
                </div>
              </td>
              <td class="px-4 py-3 text-right">
                <div class="flex items-center justify-end gap-1.5">
                  <button @click="openDetail(p)"
                    class="px-3 py-1.5 text-xs font-medium text-blue-600 dark:text-blue-400 bg-blue-50 dark:bg-blue-900/20 hover:bg-blue-100 dark:hover:bg-blue-900/40 rounded-lg transition cursor-pointer">
                    View
                  </button>
                  <Can permission="plan.update">
                    <button @click="openEdit(p)"
                      class="px-3 py-1.5 text-xs font-medium text-indigo-600 dark:text-indigo-400 bg-indigo-50 dark:bg-indigo-900/20 hover:bg-indigo-100 dark:hover:bg-indigo-900/40 rounded-lg transition cursor-pointer">
                      Edit
                    </button>
                  </Can>
                  <Can permission="plan.delete">
                    <button @click="openDelete(p)"
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
      <div v-if="plans.length === 0 && !loading && !error" class="text-center py-16">
        <p class="text-gray-400 dark:text-gray-500">No plans match your filters</p>
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

    <!-- ── Create / Edit Plan Modal ── -->
    <Teleport to="body">
      <div v-if="showFormModal" class="fixed inset-0 z-50 flex items-center justify-center p-4" @click.self="closeForm">
        <div class="absolute inset-0 bg-black/50 backdrop-blur-sm" @click="closeForm"></div>
        <div class="relative bg-white dark:bg-gray-900 rounded-2xl shadow-2xl w-full max-w-lg max-h-[90vh] overflow-y-auto animate-modal-in p-6">

          <div class="flex items-center justify-between mb-5">
            <h2 class="text-lg font-bold text-gray-800 dark:text-gray-100">
              {{ formMode === 'create' ? 'Add New Plan' : 'Edit Plan' }}
            </h2>
            <button @click="closeForm" :disabled="formSaving"
              class="w-7 h-7 rounded-full bg-gray-100 dark:bg-gray-800 flex items-center justify-center text-gray-400 hover:bg-gray-200 dark:hover:bg-gray-700 cursor-pointer text-sm disabled:opacity-40">✕</button>
          </div>

          <!-- Loading detail for edit -->
          <div v-if="formLoading" class="flex justify-center py-16">
            <div class="w-8 h-8 border-4 border-blue-500 border-t-transparent rounded-full animate-spin"></div>
          </div>

          <form v-else @submit.prevent="submitForm" class="space-y-4">
            <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <div class="sm:col-span-2">
                <div class="flex items-center gap-1.5 mb-1">
                  <label class="text-sm font-medium text-gray-700 dark:text-gray-300">Plan Name *</label>
                  <FieldTip text="Public name customers see on the plans page. Must be unique." />
                </div>
                <input v-model="form.name" type="text" placeholder="Home Fibre 30"
                  class="w-full px-3 py-2 rounded-lg border text-sm focus:ring-2 focus:ring-blue-500 outline-none transition"
                  :class="formValidation.name ? 'border-red-400' : 'border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100'" />
                <p v-if="formValidation.name" class="text-xs text-red-500 mt-1">{{ formValidation.name }}</p>
              </div>

              <div class="sm:col-span-2">
                <div class="flex items-center gap-1.5 mb-1">
                  <label class="text-sm font-medium text-gray-700 dark:text-gray-300">Description</label>
                  <FieldTip text="Short description shown to customers on the public plans page." />
                </div>
                <textarea v-model="form.description" rows="2" placeholder="Short description shown to customers"
                  class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 text-sm focus:ring-2 focus:ring-blue-500 outline-none transition resize-none"></textarea>
              </div>

              <div>
                <div class="flex items-center gap-1.5 mb-1">
                  <label class="text-sm font-medium text-gray-700 dark:text-gray-300">Price ({{ currency }}) *</label>
                  <FieldTip text="Amount customers pay each billing cycle, in Kenya Shillings. Stored as cents internally." />
                </div>
                <input v-model.number="form.priceKes" type="number" min="0" step="0.01" placeholder="1500"
                  class="w-full px-3 py-2 rounded-lg border text-sm focus:ring-2 focus:ring-blue-500 outline-none transition"
                  :class="formValidation.priceKes ? 'border-red-400' : 'border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100'" />
                <p v-if="formValidation.priceKes" class="text-xs text-red-500 mt-1">{{ formValidation.priceKes }}</p>
              </div>

              <div>
                <div class="flex items-center gap-1.5 mb-1">
                  <label class="text-sm font-medium text-gray-700 dark:text-gray-300">Billing Cycle</label>
                  <FieldTip text="How often the plan is billed: monthly, quarterly, semi-annual or yearly." />
                </div>
                <input v-model="form.billingCycle" list="billing-cycle-options" type="text" placeholder="monthly"
                  class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 text-sm focus:ring-2 focus:ring-blue-500 outline-none transition" />
                <datalist id="billing-cycle-options">
                  <option v-for="b in billingCycleOptions" :key="b" :value="b" />
                </datalist>
              </div>

              <div>
                <div class="flex items-center gap-1.5 mb-1">
                  <label class="text-sm font-medium text-gray-700 dark:text-gray-300">Download (Mbps)</label>
                  <FieldTip text="Maximum download speed. Leave 0 for an unlimited connection." />
                </div>
                <input v-model.number="form.downloadMbps" type="number" min="0" step="0.1" placeholder="30"
                  class="w-full px-3 py-2 rounded-lg border text-sm focus:ring-2 focus:ring-blue-500 outline-none transition"
                  :class="formValidation.downloadMbps ? 'border-red-400' : 'border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100'" />
                <p v-if="formValidation.downloadMbps" class="text-xs text-red-500 mt-1">{{ formValidation.downloadMbps }}</p>
              </div>

              <div>
                <div class="flex items-center gap-1.5 mb-1">
                  <label class="text-sm font-medium text-gray-700 dark:text-gray-300">Upload (Mbps)</label>
                  <FieldTip text="Maximum upload speed. Leave 0 for an unlimited connection." />
                </div>
                <input v-model.number="form.uploadMbps" type="number" min="0" step="0.1" placeholder="10"
                  class="w-full px-3 py-2 rounded-lg border text-sm focus:ring-2 focus:ring-blue-500 outline-none transition"
                  :class="formValidation.uploadMbps ? 'border-red-400' : 'border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100'" />
                <p v-if="formValidation.uploadMbps" class="text-xs text-red-500 mt-1">{{ formValidation.uploadMbps }}</p>
              </div>

              <div>
                <div class="flex items-center gap-1.5 mb-1">
                  <label class="text-sm font-medium text-gray-700 dark:text-gray-300">Max Devices *</label>
                  <FieldTip text="How many devices can use the connection at once. Written to RADIUS as Simultaneous-Use." />
                </div>
                <input v-model.number="form.maxDevices" type="number" min="1" step="1" placeholder="3"
                  class="w-full px-3 py-2 rounded-lg border text-sm focus:ring-2 focus:ring-blue-500 outline-none transition"
                  :class="formValidation.maxDevices ? 'border-red-400' : 'border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100'" />
                <p v-if="formValidation.maxDevices" class="text-xs text-red-500 mt-1">{{ formValidation.maxDevices }}</p>
              </div>

              <div>
                <div class="flex items-center gap-1.5 mb-1">
                  <label class="text-sm font-medium text-gray-700 dark:text-gray-300">Session Timeout (hours)</label>
                  <FieldTip text="Longest a connection stays up before RADIUS forces a disconnect. Default 24 hours." />
                </div>
                <input v-model.number="form.sessionTimeoutHours" type="number" min="0" step="1" placeholder="24"
                  class="w-full px-3 py-2 rounded-lg border text-sm focus:ring-2 focus:ring-blue-500 outline-none transition"
                  :class="formValidation.sessionTimeoutHours ? 'border-red-400' : 'border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100'" />
                <p v-if="formValidation.sessionTimeoutHours" class="text-xs text-red-500 mt-1">{{ formValidation.sessionTimeoutHours }}</p>
              </div>

              <div>
                <div class="flex items-center gap-1.5 mb-1">
                  <label class="text-sm font-medium text-gray-700 dark:text-gray-300">Idle Timeout (minutes)</label>
                  <FieldTip text="Disconnects the session after this many minutes with no traffic. Default 10 minutes." />
                </div>
                <input v-model.number="form.idleTimeoutMinutes" type="number" min="0" step="1" placeholder="10"
                  class="w-full px-3 py-2 rounded-lg border text-sm focus:ring-2 focus:ring-blue-500 outline-none transition"
                  :class="formValidation.idleTimeoutMinutes ? 'border-red-400' : 'border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100'" />
                <p v-if="formValidation.idleTimeoutMinutes" class="text-xs text-red-500 mt-1">{{ formValidation.idleTimeoutMinutes }}</p>
              </div>

              <div>
                <div class="flex items-center gap-1.5 mb-1">
                  <label class="text-sm font-medium text-gray-700 dark:text-gray-300">Sort Order</label>
                  <FieldTip text="Lower numbers appear first on the public plans page and admin lists." />
                </div>
                <input v-model.number="form.sortOrder" type="number" step="1" placeholder="0"
                  class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 text-sm focus:ring-2 focus:ring-blue-500 outline-none transition" />
              </div>

              <div v-if="formMode === 'edit'" class="flex items-center justify-between px-3 py-2 rounded-lg border border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-800/60">
                <div>
                  <p class="text-sm font-medium text-gray-700 dark:text-gray-300">Active</p>
                  <p class="text-xs text-gray-400 dark:text-gray-500">Inactive plans are hidden from the public site and reject logins</p>
                </div>
                <label class="relative inline-flex items-center cursor-pointer">
                  <input v-model="form.isActive" type="checkbox" class="sr-only peer" />
                  <div class="w-10 h-5 bg-gray-300 dark:bg-gray-600 rounded-full peer peer-checked:bg-green-500 after:content-[''] after:absolute after:top-0.5 after:left-0.5 after:bg-white after:rounded-full after:h-4 after:w-4 after:transition-all peer-checked:after:translate-x-5"></div>
                </label>
              </div>
            </div>

            <p v-if="formError" class="text-sm text-red-600 dark:text-red-400 bg-red-50 dark:bg-red-900/20 rounded-lg px-3 py-2">{{ formError }}</p>

            <div class="flex gap-3 pt-2">
              <button type="button" @click="closeForm" :disabled="formSaving"
                class="flex-1 px-4 py-2.5 text-sm font-medium text-gray-600 dark:text-gray-300 bg-gray-100 dark:bg-gray-800 hover:bg-gray-200 dark:hover:bg-gray-700 rounded-lg transition cursor-pointer disabled:opacity-40">
                Cancel
              </button>
              <button type="submit" :disabled="formSaving"
                class="flex-1 px-4 py-2.5 text-sm font-medium text-white bg-blue-600 hover:bg-blue-700 rounded-lg transition-all duration-200 cursor-pointer disabled:opacity-50 flex items-center justify-center gap-2">
                <span v-if="formSaving" class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
                {{ formMode === 'create' ? 'Create Plan' : 'Save Changes' }}
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

          <div v-else-if="selectedPlan" class="space-y-6">
            <!-- Header -->
            <div class="flex items-start justify-between gap-4 pb-4 border-b border-gray-100 dark:border-gray-800">
              <div>
                <div class="flex items-center gap-2">
                  <h2 class="text-xl font-bold text-gray-800 dark:text-gray-100">{{ selectedPlan.name }}</h2>
                  <span :class="statusClass(selectedPlan.isActive)" class="px-2 py-0.5 rounded-full text-xs font-medium">
                    {{ selectedPlan.isActive ? 'Active' : 'Inactive' }}
                  </span>
                </div>
                <p v-if="selectedPlan.description" class="text-sm text-gray-500 dark:text-gray-400 mt-1">{{ selectedPlan.description }}</p>
              </div>
              <Can permission="plan.update">
                <button @click="editFromDetail"
                  class="px-3 py-1.5 text-xs font-medium text-indigo-600 dark:text-indigo-400 bg-indigo-50 dark:bg-indigo-900/20 hover:bg-indigo-100 dark:hover:bg-indigo-900/40 rounded-lg transition cursor-pointer whitespace-nowrap">
                  Edit Plan
                </button>
              </Can>
            </div>

            <!-- Price highlight -->
            <div class="bg-blue-50 dark:bg-blue-900/30 rounded-xl p-4 text-center">
              <span class="text-4xl font-bold text-blue-600 dark:text-blue-400">{{ formatPrice(selectedPlan.priceCents) }}</span>
              <span class="text-blue-400 dark:text-blue-300 text-sm"> /{{ selectedPlan.billingCycle }}</span>
            </div>

            <!-- Connection -->
            <div>
              <h3 class="text-sm font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider mb-3">Connection</h3>
              <div class="grid grid-cols-2 gap-3">
                <div class="bg-gray-50 dark:bg-gray-800 rounded-lg p-3">
                  <p class="text-xs text-gray-400 dark:text-gray-500 mb-1">Download Speed</p>
                  <p class="font-semibold text-gray-800 dark:text-gray-100">{{ selectedPlan.bandwidthDownKbps ? formatSpeed(selectedPlan.bandwidthDownKbps) : 'Unlimited' }}</p>
                </div>
                <div class="bg-gray-50 dark:bg-gray-800 rounded-lg p-3">
                  <p class="text-xs text-gray-400 dark:text-gray-500 mb-1">Upload Speed</p>
                  <p class="font-semibold text-gray-800 dark:text-gray-100">{{ selectedPlan.bandwidthUpKbps ? formatSpeed(selectedPlan.bandwidthUpKbps) : 'Unlimited' }}</p>
                </div>
              </div>
            </div>

            <!-- Session -->
            <div>
              <h3 class="text-sm font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider mb-3">Session</h3>
              <div class="grid grid-cols-2 gap-3">
                <div class="bg-gray-50 dark:bg-gray-800 rounded-lg p-3">
                  <p class="text-xs text-gray-400 dark:text-gray-500 mb-1">Session Timeout</p>
                  <p class="font-semibold text-gray-800 dark:text-gray-100">{{ formatDuration(selectedPlan.sessionTimeoutSeconds) }}</p>
                </div>
                <div class="bg-gray-50 dark:bg-gray-800 rounded-lg p-3">
                  <p class="text-xs text-gray-400 dark:text-gray-500 mb-1">Idle Timeout</p>
                  <p class="font-semibold text-gray-800 dark:text-gray-100">{{ formatDuration(selectedPlan.idleTimeoutSeconds) }}</p>
                </div>
              </div>
            </div>

            <!-- RADIUS / Management -->
            <div>
              <h3 class="text-sm font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider mb-3">Management</h3>
              <div class="grid grid-cols-2 sm:grid-cols-3 gap-3">
                <div class="bg-gray-50 dark:bg-gray-800 rounded-lg p-3">
                  <p class="text-xs text-gray-400 dark:text-gray-500 mb-1">Max Devices</p>
                  <p class="font-semibold text-gray-800 dark:text-gray-100">{{ selectedPlan.maxDevices }}</p>
                </div>
                <div class="bg-gray-50 dark:bg-gray-800 rounded-lg p-3">
                  <p class="text-xs text-gray-400 dark:text-gray-500 mb-1">Active Subscribers</p>
                  <p class="font-semibold text-gray-800 dark:text-gray-100">{{ selectedPlan.activeSubscribersCount ?? 0 }}</p>
                </div>
                <div class="bg-gray-50 dark:bg-gray-800 rounded-lg p-3">
                  <p class="text-xs text-gray-400 dark:text-gray-500 mb-1">Sort Order</p>
                  <p class="font-semibold text-gray-800 dark:text-gray-100">{{ selectedPlan.sortOrder }}</p>
                </div>
                <div class="bg-gray-50 dark:bg-gray-800 rounded-lg p-3 sm:col-span-2">
                  <p class="text-xs text-gray-400 dark:text-gray-500 mb-1">RADIUS Group</p>
                  <p class="font-semibold text-gray-800 dark:text-gray-100 font-mono text-sm">{{ selectedPlan.groupName || '—' }}</p>
                </div>
              </div>
            </div>
          </div>

          <div v-else class="p-8 text-center">
            <p class="text-red-500 dark:text-red-400">Failed to load plan details.</p>
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
              <h2 class="text-lg font-bold text-gray-800 dark:text-gray-100">Delete Plan</h2>
              <p class="text-sm text-gray-500 dark:text-gray-400 mt-1">
                Are you sure you want to delete <span class="font-semibold text-gray-700 dark:text-gray-200">{{ deleteTarget?.name }}</span>?
                This permanently removes the plan and its RADIUS policies. This cannot be undone.
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
              Delete Plan
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
