<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { getNas, createNas, updateNas, deleteNas } from '@/services/nas.service'
import { useToastStore } from '@/stores/toast.store'
import FieldTip from '@/components/FieldTip.vue'
import Can from '@/components/Can.vue'
import type { NasClient } from '@/types/nas.types'
import type { CreateNasPayload, UpdateNasPayload } from '@/types/nas.types'

const toast = useToastStore()

// ── State ──
const nasList = ref<NasClient[]>([])
const loading = ref(true)
const error = ref<string | null>(null)

const page = ref(1)
const pageSize = ref(10)
const totalCount = ref(0)
const totalPages = ref(0)
const search = ref('')
const typeFilter = ref('')
const sortField = ref<string>('id')
const sortDir = ref<'asc' | 'desc'>('asc')

const actionError = ref('')

// ── Detail modal ──
const showDetail = ref(false)
const selectedNas = ref<NasClient | null>(null)

// ── Form modal ──
const showFormModal = ref(false)
const formMode = ref<'create' | 'edit'>('create')
const editingId = ref<number | null>(null)
const formSaving = ref(false)
const formError = ref('')
const formValidation = ref<Record<string, string>>({})
const optimisticNas = ref<NasClient | null>(null)

// ── Delete modal ──
const showDelete = ref(false)
const deleteTarget = ref<NasClient | null>(null)
const deleteLoading = ref(false)
const deleteError = ref('')

// ── Form ──
const form = ref({
  nasname: '',
  shortname: '',
  ports: null as number | null,
  secret: '',
  server: '',
  community: '',
  description: '',
})

// Known NAS vendor types for the dropdown; "Other" opens a free-text field for custom vendors
const nasTypes = ['other', 'cisco', 'mikrotik', 'huawei', 'ubiquiti', 'juniper', 'zte', 'nokia', 'tp-link', 'cambium', 'radwin']
const typeOptions = nasTypes.filter(t => t !== 'other')
const typeSelect = ref('other')
const typeCustom = ref('')

const resolvedType = computed(() =>
  typeSelect.value === 'other' ? (typeCustom.value.trim() || 'other') : typeSelect.value,
)

// ── Computed ──
const pageNumbers = computed(() => {
  const pages: number[] = []
  for (let i = 1; i <= totalPages.value; i++) pages.push(i)
  return pages
})

// ── Fetch ──
async function fetchNas() {
  loading.value = true
  error.value = null
  try {
    const result = await getNas({
      page: page.value,
      pageSize: pageSize.value,
      search: search.value || undefined,
      sortBy: sortField.value,
      sortDesc: sortDir.value === 'desc',
      type: typeFilter.value || undefined,
    })
    nasList.value = result.items
    totalCount.value = result.totalCount
    totalPages.value = result.totalPages
  } catch (e: unknown) {
    error.value = errMsg(e, 'Failed to load NAS clients')
  } finally {
    loading.value = false
  }
}

onMounted(fetchNas)

// ── Search with debounce ──
let debounceTimer: ReturnType<typeof setTimeout> | null = null
watch(search, () => {
  if (debounceTimer) clearTimeout(debounceTimer)
  debounceTimer = setTimeout(() => {
    page.value = 1
    fetchNas()
  }, 300)
})

// ── Sort ──
function toggleSort(field: string) {
  if (sortField.value === field) sortDir.value = sortDir.value === 'asc' ? 'desc' : 'asc'
  else { sortField.value = field; sortDir.value = 'asc' }
  page.value = 1
  fetchNas()
}
function sortIcon(field: string) {
  if (sortField.value !== field) return '↕'
  return sortDir.value === 'asc' ? '↑' : '↓'
}

// ── Pagination ──
function goToPage(p: number) {
  page.value = p
  fetchNas()
  window.scrollTo({ top: 0, behavior: 'smooth' })
}

function clearFilters() {
  search.value = ''
  typeFilter.value = ''
  page.value = 1
  fetchNas()
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

function toNum(v: number | string | null | undefined): number {
  const n = typeof v === 'string' ? Number(v) : Number(v ?? 0)
  return Number.isFinite(n) ? n : 0
}

function typeClass(type: string): string {
  switch (type.toLowerCase()) {
    case 'cisco': return 'bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-300'
    case 'mikrotik': return 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-300'
    case 'huawei': return 'bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-300'
    case 'ubiquiti': return 'bg-purple-100 text-purple-700 dark:bg-purple-900/30 dark:text-purple-300'
    case 'juniper': return 'bg-orange-100 text-orange-700 dark:bg-orange-900/30 dark:text-orange-300'
    case 'zte': return 'bg-rose-100 text-rose-700 dark:bg-rose-900/30 dark:text-rose-300'
    case 'nokia': return 'bg-sky-100 text-sky-700 dark:bg-sky-900/30 dark:text-sky-300'
    case 'tp-link': return 'bg-emerald-100 text-emerald-700 dark:bg-emerald-900/30 dark:text-emerald-300'
    case 'cambium': return 'bg-teal-100 text-teal-700 dark:bg-teal-900/30 dark:text-teal-300'
    case 'radwin': return 'bg-cyan-100 text-cyan-700 dark:bg-cyan-900/30 dark:text-cyan-300'
    default: return 'bg-gray-100 text-gray-600 dark:bg-gray-800 dark:text-gray-400'
  }
}

// ── Form helpers ──
function resetForm() {
  form.value = {
    nasname: '', shortname: '', ports: null,
    secret: '', server: '', community: '', description: '',
  }
  typeSelect.value = 'other'
  typeCustom.value = ''
}

function fillForm(n: NasClient) {
  form.value = {
    nasname: n.nasname,
    shortname: n.shortname,
    ports: n.ports,
    secret: '', // never returned by the API — blank keeps the existing secret on update
    server: n.server ?? '',
    community: n.community ?? '',
    description: n.description ?? '',
  }
  // Preserve custom vendor types via the "Other" free-text field
  if (n.type === 'other' || typeOptions.includes(n.type)) {
    typeSelect.value = n.type
    typeCustom.value = ''
  } else {
    typeSelect.value = 'other'
    typeCustom.value = n.type
  }
}

function validateForm(): boolean {
  const v: Record<string, string> = {}
  if (!form.value.nasname.trim()) v.nasname = 'NAS name is required'
  if (!form.value.shortname.trim()) v.shortname = 'Short name is required'
  if (formMode.value === 'create' && !form.value.secret.trim()) v.secret = 'Secret is required'
  if (toNum(form.value.ports) < 0) v.ports = 'Ports cannot be negative'
  formValidation.value = v
  return Object.keys(v).length === 0
}

function buildPayload(): CreateNasPayload | UpdateNasPayload {
  const base = {
    nasname: form.value.nasname.trim(),
    shortname: form.value.shortname.trim(),
    type: resolvedType.value,
    ports: form.value.ports !== null && form.value.ports !== undefined ? Math.round(toNum(form.value.ports)) : null,
    server: form.value.server.trim() || null,
    community: form.value.community.trim() || null,
    description: form.value.description.trim() || null,
  }
  if (formMode.value === 'edit') {
    const payload: UpdateNasPayload = { ...base }
    if (form.value.secret.trim()) payload.secret = form.value.secret.trim()
    return payload
  }
  return { ...base, secret: form.value.secret.trim() } as CreateNasPayload
}

// ── Create / Edit ──
function openCreate() {
  formMode.value = 'create'
  editingId.value = null
  resetForm()
  formError.value = ''
  formValidation.value = {}
  showFormModal.value = true
}

function openEdit(n: NasClient) {
  formMode.value = 'edit'
  editingId.value = n.id
  fillForm(n)
  formError.value = ''
  formValidation.value = {}
  showFormModal.value = true
}

function closeForm() {
  if (formSaving.value) return
  showFormModal.value = false
  editingId.value = null
  optimisticNas.value = null
}

async function submitForm() {
  if (!validateForm()) return
  formSaving.value = true
  formError.value = ''
  const payload = buildPayload()

  if (formMode.value === 'create') {
    // Optimistic insert at top of list
    const temp: NasClient = {
      id: Date.now(),
      nasname: (payload as CreateNasPayload).nasname,
      shortname: (payload as CreateNasPayload).shortname,
      type: (payload as CreateNasPayload).type,
      ports: (payload as CreateNasPayload).ports,
      server: (payload as CreateNasPayload).server ?? '',
      community: (payload as CreateNasPayload).community,
      description: (payload as CreateNasPayload).description,
    }
    nasList.value.unshift(temp)
    optimisticNas.value = temp
    showFormModal.value = false

    try {
      const created = await createNas(payload as CreateNasPayload)
      const idx = nasList.value.findIndex(n => n.id === temp.id)
      if (idx !== -1) nasList.value[idx] = created
      optimisticNas.value = null
      totalCount.value++
      totalPages.value = Math.max(1, Math.ceil(totalCount.value / pageSize.value))
      toast.success(`NAS client "${created.shortname}" created`)
      if (page.value !== 1) { page.value = 1; fetchNas() }
    } catch (e: unknown) {
      const idx = nasList.value.findIndex(n => n.id === temp.id)
      if (idx !== -1) nasList.value.splice(idx, 1)
      optimisticNas.value = null
      formError.value = errMsg(e, 'Failed to create NAS client')
      toast.error(formError.value)
      showFormModal.value = true
    }
  } else if (editingId.value !== null) {
    try {
      const updated = await updateNas(editingId.value, payload as UpdateNasPayload)
      const idx = nasList.value.findIndex(n => n.id === updated.id)
      if (idx !== -1) nasList.value[idx] = updated
      showFormModal.value = false
      toast.success(`NAS client "${updated.shortname}" updated`)
    } catch (e: unknown) {
      formError.value = errMsg(e, 'Failed to update NAS client')
      toast.error(formError.value)
    }
  }
  formSaving.value = false
}

// ── Detail ──
function openDetail(n: NasClient) {
  selectedNas.value = n
  showDetail.value = true
}
function closeDetail() {
  showDetail.value = false
  selectedNas.value = null
}

/** Edit from the detail modal — snapshot the client BEFORE closing so it survives */
function editFromDetail() {
  const n = selectedNas.value
  if (!n) return
  closeDetail()
  openEdit(n)
}

// ── Delete ──
function openDelete(n: NasClient) {
  deleteTarget.value = n
  deleteError.value = ''
  showDelete.value = true
}
async function confirmDelete() {
  if (!deleteTarget.value) return
  const target = deleteTarget.value
  deleteLoading.value = true
  deleteError.value = ''
  try {
    await deleteNas(target.id)
    nasList.value = nasList.value.filter(n => n.id !== target.id)
    totalCount.value = Math.max(0, totalCount.value - 1)
    totalPages.value = Math.max(1, Math.ceil(totalCount.value / pageSize.value))
    if (nasList.value.length === 0 && page.value > 1) {
      page.value -= 1
      fetchNas()
    }
    showDelete.value = false
    deleteTarget.value = null
    toast.success(`NAS client "${target.shortname}" deleted`)
  } catch (e: unknown) {
    deleteError.value = errMsg(e, 'Failed to delete NAS client')
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
        <h1 class="text-2xl font-bold text-gray-800 dark:text-gray-100">NAS Clients</h1>
        <p class="text-sm text-gray-500 dark:text-gray-400 mt-1">
          {{ totalCount }} total NAS clients
        </p>
      </div>
      <div class="flex items-center gap-3">
        <div class="relative">
          <svg class="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
            <path stroke-linecap="round" stroke-linejoin="round" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
          </svg>
          <input v-model="search" type="text" placeholder="Search NAS name, short name..."
            class="pl-10 pr-4 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 text-sm focus:ring-2 focus:ring-blue-500 outline-none transition w-64" />
        </div>
        <select v-model="typeFilter" @change="page = 1; fetchNas()"
          class="px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 text-sm focus:ring-2 focus:ring-blue-500 outline-none transition cursor-pointer capitalize">
          <option value="">All types</option>
          <option v-for="t in nasTypes" :key="t" :value="t" class="capitalize">{{ t }}</option>
        </select>
        <Can permission="radius.nas.manage">
          <button @click="openCreate"
            class="px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white text-sm font-medium rounded-lg transition-all duration-200 cursor-pointer flex items-center gap-2 whitespace-nowrap">
            <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
              <path stroke-linecap="round" stroke-linejoin="round" d="M12 4v16m8-8H4" />
            </svg>
            Add NAS
          </button>
        </Can>
      </div>
    </div>

    <!-- ── Action error banner ── -->
    <div v-if="actionError"
      class="mb-4 px-4 py-3 rounded-lg bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 text-sm text-red-600 dark:text-red-400">
      {{ actionError }}
    </div>

    <!-- ── Page Size ── -->
    <div class="flex items-center gap-2 mb-4">
      <span class="text-xs text-gray-400 dark:text-gray-500">Show</span>
      <select v-model="pageSize" @change="page = 1; fetchNas()"
        class="px-2 py-1 rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 text-xs outline-none cursor-pointer">
        <option :value="5">5</option>
        <option :value="10">10</option>
        <option :value="20">20</option>
        <option :value="50">50</option>
      </select>
      <span class="text-xs text-gray-400 dark:text-gray-500">per page</span>
    </div>

    <!-- ── Loading ── -->
    <div v-if="loading && nasList.length === 0" class="flex justify-center py-20">
      <div class="flex flex-col items-center gap-3">
        <div class="w-8 h-8 border-4 border-blue-500 border-t-transparent rounded-full animate-spin"></div>
        <p class="text-sm text-gray-400 dark:text-gray-500">Loading NAS clients...</p>
      </div>
    </div>

    <!-- ── Error ── -->
    <div v-else-if="error && nasList.length === 0" class="text-center py-20">
      <div class="bg-red-50 dark:bg-red-900/30 border border-red-200 dark:border-red-800 rounded-xl p-6 max-w-md mx-auto">
        <p class="text-red-600 dark:text-red-400 font-medium">{{ error }}</p>
        <button @click="fetchNas" class="mt-3 px-4 py-2 bg-red-600 hover:bg-red-700 text-white text-sm rounded-lg transition cursor-pointer">Retry</button>
      </div>
    </div>

    <!-- ── Table ── -->
    <div v-else class="bg-white dark:bg-gray-900 rounded-xl shadow-sm border border-gray-100 dark:border-gray-800 overflow-hidden">
      <div class="overflow-x-auto">
        <table class="w-full text-sm">
          <thead>
            <tr class="border-b border-gray-100 dark:border-gray-800 bg-gray-50 dark:bg-gray-800/50">
              <th @click="toggleSort('nasname')" class="px-4 py-3 text-left font-semibold text-gray-600 dark:text-gray-400 cursor-pointer hover:text-blue-600 dark:hover:text-blue-400 transition whitespace-nowrap">NAS Name <span class="text-xs ml-1">{{ sortIcon('nasname') }}</span></th>
              <th @click="toggleSort('shortname')" class="px-4 py-3 text-left font-semibold text-gray-600 dark:text-gray-400 cursor-pointer hover:text-blue-600 dark:hover:text-blue-400 transition whitespace-nowrap hidden sm:table-cell">Short Name <span class="text-xs ml-1">{{ sortIcon('shortname') }}</span></th>
              <th @click="toggleSort('type')" class="px-4 py-3 text-left font-semibold text-gray-600 dark:text-gray-400 cursor-pointer hover:text-blue-600 dark:hover:text-blue-400 transition whitespace-nowrap">Type <span class="text-xs ml-1">{{ sortIcon('type') }}</span></th>
              <th class="px-4 py-3 text-left font-semibold text-gray-600 dark:text-gray-400 whitespace-nowrap hidden md:table-cell">Ports</th>
              <th @click="toggleSort('server')" class="px-4 py-3 text-left font-semibold text-gray-600 dark:text-gray-400 cursor-pointer hover:text-blue-600 dark:hover:text-blue-400 transition whitespace-nowrap hidden lg:table-cell">Server <span class="text-xs ml-1">{{ sortIcon('server') }}</span></th>
              <th class="px-4 py-3 text-left font-semibold text-gray-600 dark:text-gray-400 whitespace-nowrap hidden xl:table-cell">Community</th>
              <th class="px-4 py-3 text-right font-semibold text-gray-600 dark:text-gray-400 whitespace-nowrap">Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="n in nasList" :key="n.id"
              class="border-b border-gray-50 dark:border-gray-800/50 hover:bg-gray-50 dark:hover:bg-gray-800/30 transition"
              :class="{ 'opacity-50': optimisticNas?.id === n.id }">
              <td class="px-4 py-3">
                <div>
                  <p class="font-medium text-gray-800 dark:text-gray-200">
                    {{ n.nasname }}
                    <span v-if="optimisticNas?.id === n.id" class="text-xs text-blue-500 font-normal ml-1">Creating...</span>
                  </p>
                  <p v-if="n.description" class="text-xs text-gray-400 dark:text-gray-500 mt-0.5 max-w-[220px] truncate">{{ n.description }}</p>
                </div>
              </td>
              <td class="px-4 py-3 text-gray-600 dark:text-gray-400 hidden sm:table-cell">{{ n.shortname }}</td>
              <td class="px-4 py-3">
                <span :class="typeClass(n.type)" class="px-2 py-0.5 rounded-full text-xs font-medium capitalize">{{ n.type }}</span>
              </td>
              <td class="px-4 py-3 text-gray-600 dark:text-gray-400 hidden md:table-cell">{{ n.ports ?? 'Any' }}</td>
              <td class="px-4 py-3 text-gray-600 dark:text-gray-400 font-mono text-xs hidden lg:table-cell">{{ n.server || '—' }}</td>
              <td class="px-4 py-3 text-gray-600 dark:text-gray-400 font-mono text-xs hidden xl:table-cell">{{ n.community || '—' }}</td>
              <td class="px-4 py-3 text-right">
                <div class="flex items-center justify-end gap-1.5">
                  <button @click="openDetail(n)"
                    class="px-3 py-1.5 text-xs font-medium text-blue-600 dark:text-blue-400 bg-blue-50 dark:bg-blue-900/20 hover:bg-blue-100 dark:hover:bg-blue-900/40 rounded-lg transition cursor-pointer">
                    View
                  </button>
                  <Can permission="radius.nas.manage">
                    <button @click="openEdit(n)"
                      class="px-3 py-1.5 text-xs font-medium text-indigo-600 dark:text-indigo-400 bg-indigo-50 dark:bg-indigo-900/20 hover:bg-indigo-100 dark:hover:bg-indigo-900/40 rounded-lg transition cursor-pointer">
                      Edit
                    </button>
                    <button @click="openDelete(n)"
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
      <div v-if="nasList.length === 0 && !loading && !error" class="text-center py-16">
        <p class="text-gray-400 dark:text-gray-500">No NAS clients match your filters</p>
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

    <!-- ── Create / Edit NAS Modal ── -->
    <Teleport to="body">
      <div v-if="showFormModal" class="fixed inset-0 z-50 flex items-center justify-center p-4" @click.self="closeForm">
        <div class="absolute inset-0 bg-black/50 backdrop-blur-sm" @click="closeForm"></div>
        <div class="relative bg-white dark:bg-gray-900 rounded-2xl shadow-2xl w-full max-w-lg max-h-[90vh] overflow-y-auto animate-modal-in p-6">

          <div class="flex items-center justify-between mb-5">
            <h2 class="text-lg font-bold text-gray-800 dark:text-gray-100">
              {{ formMode === 'create' ? 'Add NAS Client' : 'Edit NAS Client' }}
            </h2>
            <button @click="closeForm" :disabled="formSaving"
              class="w-7 h-7 rounded-full bg-gray-100 dark:bg-gray-800 flex items-center justify-center text-gray-400 hover:bg-gray-200 dark:hover:bg-gray-700 cursor-pointer text-sm disabled:opacity-40">✕</button>
          </div>

          <form @submit.prevent="submitForm" class="space-y-4">
            <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <div class="sm:col-span-2">
                <div class="flex items-center gap-1.5 mb-1">
                  <label class="text-sm font-medium text-gray-700 dark:text-gray-300">NAS Name *</label>
                  <FieldTip text="The client IP or hostname FreeRADIUS uses to identify this device, e.g. 192.168.1.1 or router-01. Must be unique." />
                </div>
                <input v-model="form.nasname" type="text" placeholder="192.168.1.1"
                  class="w-full px-3 py-2 rounded-lg border text-sm focus:ring-2 focus:ring-blue-500 outline-none transition"
                  :class="formValidation.nasname ? 'border-red-400' : 'border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100'" />
                <p v-if="formValidation.nasname" class="text-xs text-red-500 mt-1">{{ formValidation.nasname }}</p>
              </div>

              <div>
                <div class="flex items-center gap-1.5 mb-1">
                  <label class="text-sm font-medium text-gray-700 dark:text-gray-300">Short Name *</label>
                  <FieldTip text="A short label for this device shown in lists, e.g. Router 01 or Tower B." />
                </div>
                <input v-model="form.shortname" type="text" placeholder="Router 01"
                  class="w-full px-3 py-2 rounded-lg border text-sm focus:ring-2 focus:ring-blue-500 outline-none transition"
                  :class="formValidation.shortname ? 'border-red-400' : 'border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100'" />
                <p v-if="formValidation.shortname" class="text-xs text-red-500 mt-1">{{ formValidation.shortname }}</p>
              </div>

              <div>
                <div class="flex items-center gap-1.5 mb-1">
                  <label class="text-sm font-medium text-gray-700 dark:text-gray-300">Type *</label>
                  <FieldTip text="Vendor type of the device. Used by FreeRADIUS for vendor-specific attributes. Pick a known vendor, or choose Other to type one." />
                </div>
                <select v-model="typeSelect"
                  class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 text-sm focus:ring-2 focus:ring-blue-500 outline-none transition cursor-pointer capitalize">
                  <option value="other">Other…</option>
                  <option v-for="t in typeOptions" :key="t" :value="t" class="capitalize">{{ t }}</option>
                </select>
                <input v-if="typeSelect === 'other'" v-model="typeCustom" type="text" placeholder="Type custom vendor (e.g. teltonika)"
                  class="mt-2 w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 text-sm focus:ring-2 focus:ring-blue-500 outline-none transition" />
              </div>

              <div>
                <div class="flex items-center gap-1.5 mb-1">
                  <label class="text-sm font-medium text-gray-700 dark:text-gray-300">Ports</label>
                  <FieldTip text="UDP port the RADIUS client uses. Blank or 0 means any port." />
                </div>
                <input v-model.number="form.ports" type="number" min="0" step="1" placeholder="0"
                  class="w-full px-3 py-2 rounded-lg border text-sm focus:ring-2 focus:ring-blue-500 outline-none transition"
                  :class="formValidation.ports ? 'border-red-400' : 'border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100'" />
                <p v-if="formValidation.ports" class="text-xs text-red-500 mt-1">{{ formValidation.ports }}</p>
              </div>

              <div>
                <div class="flex items-center gap-1.5 mb-1">
                  <label class="text-sm font-medium text-gray-700 dark:text-gray-300">Server</label>
                  <FieldTip text="The RADIUS server address this device talks to. Usually this server, e.g. 127.0.0.1." />
                </div>
                <input v-model="form.server" type="text" placeholder="127.0.0.1"
                  class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 text-sm focus:ring-2 focus:ring-blue-500 outline-none transition" />
              </div>

              <div>
                <div class="flex items-center gap-1.5 mb-1">
                  <label class="text-sm font-medium text-gray-700 dark:text-gray-300">Community</label>
                  <FieldTip text="SNMP community string used for network monitoring. Optional." />
                </div>
                <input v-model="form.community" type="text" placeholder="public"
                  class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 text-sm focus:ring-2 focus:ring-blue-500 outline-none transition" />
              </div>

              <div class="sm:col-span-2">
                <div class="flex items-center gap-1.5 mb-1">
                  <label class="text-sm font-medium text-gray-700 dark:text-gray-300">{{ formMode === 'create' ? 'Secret *' : 'Secret' }}</label>
                  <FieldTip :text="formMode === 'create'
                    ? 'Shared secret between this device and FreeRADIUS. Must match the device configuration.'
                    : 'Shared secret between this device and FreeRADIUS. Leave blank to keep the current secret. It is never shown after saving.'" />
                </div>
                <input v-model="form.secret" type="password" :placeholder="formMode === 'edit' ? 'Leave blank to keep current' : '••••••••'"
                  class="w-full px-3 py-2 rounded-lg border text-sm focus:ring-2 focus:ring-blue-500 outline-none transition"
                  :class="formValidation.secret ? 'border-red-400' : 'border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100'" />
                <p v-if="formValidation.secret" class="text-xs text-red-500 mt-1">{{ formValidation.secret }}</p>
              </div>

              <div class="sm:col-span-2">
                <div class="flex items-center gap-1.5 mb-1">
                  <label class="text-sm font-medium text-gray-700 dark:text-gray-300">Description</label>
                  <FieldTip text="Optional notes about this device or its location." />
                </div>
                <textarea v-model="form.description" rows="2" placeholder="Location, purpose, notes..."
                  class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 text-sm focus:ring-2 focus:ring-blue-500 outline-none transition resize-none"></textarea>
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
                {{ formMode === 'create' ? 'Create NAS' : 'Save Changes' }}
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
        <div class="relative bg-white dark:bg-gray-900 rounded-2xl shadow-2xl w-full max-w-lg max-h-[90vh] overflow-y-auto animate-modal-in p-6 sm:p-8">

          <button @click="closeDetail"
            class="absolute top-4 right-4 w-7 h-7 rounded-full bg-gray-100 dark:bg-gray-800 flex items-center justify-center text-gray-400 hover:bg-gray-200 dark:hover:bg-gray-700 cursor-pointer text-sm">✕</button>

          <div v-if="selectedNas" class="space-y-6">
            <!-- Header -->
            <div class="flex items-start justify-between gap-4 pb-4 border-b border-gray-100 dark:border-gray-800">
              <div>
                <div class="flex items-center gap-2">
                  <h2 class="text-xl font-bold text-gray-800 dark:text-gray-100">{{ selectedNas.nasname }}</h2>
                  <span :class="typeClass(selectedNas.type)" class="px-2 py-0.5 rounded-full text-xs font-medium capitalize">{{ selectedNas.type }}</span>
                </div>
                <p v-if="selectedNas.description" class="text-sm text-gray-500 dark:text-gray-400 mt-1">{{ selectedNas.description }}</p>
              </div>
              <Can permission="radius.nas.manage">
                <button @click="editFromDetail"
                  class="px-3 py-1.5 text-xs font-medium text-indigo-600 dark:text-indigo-400 bg-indigo-50 dark:bg-indigo-900/20 hover:bg-indigo-100 dark:hover:bg-indigo-900/40 rounded-lg transition cursor-pointer whitespace-nowrap">
                  Edit
                </button>
              </Can>
            </div>

            <!-- Details -->
            <div>
              <h3 class="text-sm font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider mb-3">Configuration</h3>
              <div class="grid grid-cols-2 gap-3">
                <div class="bg-gray-50 dark:bg-gray-800 rounded-lg p-3">
                  <p class="text-xs text-gray-400 dark:text-gray-500 mb-1">Short Name</p>
                  <p class="font-semibold text-gray-800 dark:text-gray-100">{{ selectedNas.shortname }}</p>
                </div>
                <div class="bg-gray-50 dark:bg-gray-800 rounded-lg p-3">
                  <p class="text-xs text-gray-400 dark:text-gray-500 mb-1">Ports</p>
                  <p class="font-semibold text-gray-800 dark:text-gray-100">{{ selectedNas.ports ?? 'Any' }}</p>
                </div>
                <div class="bg-gray-50 dark:bg-gray-800 rounded-lg p-3">
                  <p class="text-xs text-gray-400 dark:text-gray-500 mb-1">Server</p>
                  <p class="font-semibold text-gray-800 dark:text-gray-100 font-mono text-sm">{{ selectedNas.server || '—' }}</p>
                </div>
                <div class="bg-gray-50 dark:bg-gray-800 rounded-lg p-3">
                  <p class="text-xs text-gray-400 dark:text-gray-500 mb-1">Community</p>
                  <p class="font-semibold text-gray-800 dark:text-gray-100 font-mono text-sm">{{ selectedNas.community || '—' }}</p>
                </div>
                <div class="bg-gray-50 dark:bg-gray-800 rounded-lg p-3 col-span-2">
                  <p class="text-xs text-gray-400 dark:text-gray-500 mb-1">Shared Secret</p>
                  <p class="font-semibold text-gray-800 dark:text-gray-100">•••••••••• <span class="text-xs font-normal text-gray-400">(hidden — the API never returns it)</span></p>
                </div>
              </div>
            </div>
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
              <h2 class="text-lg font-bold text-gray-800 dark:text-gray-100">Delete NAS Client</h2>
              <p class="text-sm text-gray-500 dark:text-gray-400 mt-1">
                Are you sure you want to delete <span class="font-semibold text-gray-700 dark:text-gray-200">{{ deleteTarget?.nasname }}</span> ({{ deleteTarget?.shortname }})?
                Devices using this NAS entry will no longer authenticate with RADIUS. This cannot be undone.
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
              Delete NAS
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
