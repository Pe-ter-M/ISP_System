<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { getStaff, getStaffById, createStaff, updateStaff, deleteStaff, getStaffStats } from '@/services/staff.service'
import { getRoles, getRolePermissions } from '@/services/role.service'
import { getUserById, getPermissions, updateUserPermissions } from '@/services/user.service'
import { computeInvalidCodes } from '@/utils/permissions'
import { formatPrice } from '@/types/plan.types'
import { useToastStore } from '@/stores/toast.store'
import { useSettingsStore } from '@/stores/settings.store'
import FieldTip from '@/components/FieldTip.vue'
import Can from '@/components/Can.vue'
import type { StaffSummary } from '@/types/staff.types'
import type { CreateStaffPayload, UpdateStaffPayload } from '@/types/staff.types'
import type { Role } from '@/types/role.types'
import type { Permission, PermissionOverride } from '@/types/user.types'

const toast = useToastStore()
const settingsStore = useSettingsStore()
/** Configured currency code (e.g. KES) used in form labels */
const currency = computed(() => settingsStore.value('currency') ?? 'KES')

// ── State ──
const staff = ref<StaffSummary[]>([])
const loading = ref(true)
const error = ref<string | null>(null)
const page = ref(1)
const pageSize = ref(10)
const totalCount = ref(0)
const totalPages = ref(0)
const search = ref('')
const sortField = ref('name')
const sortDir = ref<'asc' | 'desc'>('asc')

const stats = ref({ total: 0, active: 0, inactive: 0, totalMonthlySalaryCents: 0 })

// Detail modal
const showDetail = ref(false)
const detailLoading = ref(false)
const selectedStaff = ref<StaffSummary | null>(null)

// Create modal
const showCreateModal = ref(false)
const createLoading = ref(false)
const createError = ref('')
const createValidation = ref<Record<string, string>>({})
const optimisticStaff = ref<StaffSummary | null>(null)
const roles = ref<Role[]>([])
const rolesLoading = ref(false)
const createForm = ref({
  roleId: null as number | null,
  fullName: '',
  email: '',
  password: '',
  phone: '',
  salaryKes: 0,
  employmentType: 'full-time',
  notes: '',
})

// Edit modal
const showEditModal = ref(false)
const editLoading = ref(false)
const editSaving = ref(false)
const editError = ref('')
const editValidation = ref<Record<string, string>>({})
const editingId = ref<number | null>(null)
const editForm = ref({
  roleId: 0,
  fullName: '',
  email: '',
  phone: '',
  salaryKes: 0,
  employmentType: 'full-time',
  notes: '',
  status: 'active',
})

// Delete modal
const showDelete = ref(false)
const deleteTarget = ref<StaffSummary | null>(null)
const deleteLoading = ref(false)
const deleteError = ref('')

const employmentTypes = ['full-time', 'part-time', 'contract']

// ── Computed ──
const pageNumbers = computed(() => {
  const pages: number[] = []
  for (let i = 1; i <= totalPages.value; i++) pages.push(i)
  return pages
})

/** Roles a staff account can hold — the Customer role is excluded */
const staffRoles = computed(() => roles.value.filter(r => r.name.toLowerCase() !== 'customer'))

// ── Fetch ──
async function fetchStaff() {
  loading.value = true
  error.value = null
  try {
    const result = await getStaff({
      page: page.value,
      pageSize: pageSize.value,
      search: search.value || undefined,
      sortBy: sortField.value,
      sortDesc: sortDir.value === 'desc',
    })
    staff.value = result.items
    totalCount.value = result.totalCount
    totalPages.value = result.totalPages
  } catch (e: unknown) {
    error.value = errMsg(e, 'Failed to load staff')
  } finally {
    loading.value = false
  }
}

async function fetchStats() {
  try {
    stats.value = await getStaffStats()
  } catch {
    // Non-critical; cards keep last known values
  }
}

onMounted(() => {
  fetchStaff()
  fetchStats()
  loadRoles()
})

// ── Search debounce ──
let deb: ReturnType<typeof setTimeout> | null = null
watch(search, () => {
  if (deb) clearTimeout(deb)
  deb = setTimeout(() => { page.value = 1; fetchStaff() }, 300)
})

// ── Sort ──
function toggleSort(field: string) {
  if (sortField.value === field) sortDir.value = sortDir.value === 'asc' ? 'desc' : 'asc'
  else { sortField.value = field; sortDir.value = 'asc' }
  page.value = 1; fetchStaff()
}
function sortIcon(field: string) {
  if (sortField.value !== field) return '↕'
  return sortDir.value === 'asc' ? '↑' : '↓'
}

// ── Pagination ──
function goToPage(p: number) {
  page.value = p; fetchStaff(); window.scrollTo({ top: 0, behavior: 'smooth' })
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

function statusClass(status: string): string {
  return status === 'active'
    ? 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-300'
    : 'bg-gray-100 text-gray-600 dark:bg-gray-800 dark:text-gray-400'
}
function statusDot(status: string): string {
  return status === 'active' ? 'bg-green-500' : 'bg-gray-400'
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

// ── Roles ──
async function loadRoles() {
  rolesLoading.value = true
  try {
    roles.value = await getRoles()
    const first = staffRoles.value[0]
    createForm.value.roleId = first?.id ?? null
  } catch {
    roles.value = []
  } finally {
    rolesLoading.value = false
  }
}

// ── Create ──
function resetCreateForm() {
  const first = staffRoles.value[0]
  createForm.value = {
    roleId: first?.id ?? null,
    fullName: '', email: '', password: '', phone: '',
    salaryKes: 0, employmentType: 'full-time', notes: '',
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
  if (!createForm.value.roleId) v.role = 'Select a role'
  if (!createForm.value.fullName.trim()) v.fullName = 'Full name is required'
  if (!createForm.value.email.trim()) v.email = 'Email is required'
  else if (!/\S+@\S+\.\S+/.test(createForm.value.email)) v.email = 'Invalid email'
  if (!createForm.value.password || createForm.value.password.length < 4) v.password = 'Minimum 4 characters'
  if (!createForm.value.phone.trim()) v.phone = 'Phone is required'
  if (createForm.value.salaryKes < 0) v.salaryKes = 'Salary cannot be negative'
  createValidation.value = v
  return Object.keys(v).length === 0
}

async function handleCreate() {
  if (!validateCreateForm()) return
  createLoading.value = true
  createError.value = ''

  const payload: CreateStaffPayload = {
    roleId: createForm.value.roleId!,
    email: createForm.value.email.trim(),
    password: createForm.value.password,
    fullName: createForm.value.fullName.trim(),
    phone: createForm.value.phone.trim(),
    salaryCents: Math.round(createForm.value.salaryKes * 100),
    employmentType: createForm.value.employmentType,
    notes: createForm.value.notes.trim() || null,
  }

  const temp: StaffSummary = {
    id: Date.now(), userId: 0,
    staffCode: '…', fullName: payload.fullName,
    email: payload.email, phone: payload.phone,
    roleId: payload.roleId,
    roleName: staffRoles.value.find(r => r.id === payload.roleId)?.name ?? '—',
    salaryCents: payload.salaryCents,
    employmentType: payload.employmentType,
    dateJoined: payload.dateJoined ?? null, notes: payload.notes,
    status: 'active', isActive: true,
    createdAt: new Date().toISOString(), updatedAt: new Date().toISOString(),
  }
  staff.value.unshift(temp)
  optimisticStaff.value = temp
  showCreateModal.value = false

  try {
    const created = await createStaff(payload)
    const idx = staff.value.findIndex(s => s.id === temp.id)
    if (idx !== -1) staff.value[idx] = created
    optimisticStaff.value = null
    totalCount.value++
    totalPages.value = Math.max(1, Math.ceil(totalCount.value / pageSize.value))
    toast.success(`Staff member "${created.fullName}" added`)
    if (page.value !== 1) { page.value = 1; fetchStaff() }
    fetchStats()
  } catch (e: unknown) {
    const idx = staff.value.findIndex(s => s.id === temp.id)
    if (idx !== -1) staff.value.splice(idx, 1)
    optimisticStaff.value = null
    createError.value = errMsg(e, 'Failed to create staff member')
    toast.error(createError.value)
    showCreateModal.value = true
  } finally {
    createLoading.value = false
  }
}

// ── Detail ──
async function openDetail(s: StaffSummary) {
  showDetail.value = true
  detailLoading.value = true
  selectedStaff.value = null
  try {
    selectedStaff.value = await getStaffById(s.id)
    await ensurePermsLoaded(selectedStaff.value)
  } catch (e: unknown) {
    selectedStaff.value = null
    toast.error(errMsg(e, 'Failed to load staff details'))
  } finally {
    detailLoading.value = false
  }
}
function closeDetail() {
  showDetail.value = false
  selectedStaff.value = null
  permsExpanded.value = false
}

// ── Edit ──
function fillEditForm(s: StaffSummary) {
  editForm.value = {
    roleId: s.roleId,
    fullName: s.fullName,
    email: s.email ?? '',
    phone: s.phone,
    salaryKes: s.salaryCents / 100,
    employmentType: s.employmentType,
    notes: s.notes ?? '',
    status: s.status,
  }
}

async function openEdit(s: StaffSummary) {
  editingId.value = s.id
  editError.value = ''
  editValidation.value = {}
  editLoading.value = true
  showEditModal.value = true
  try {
    const d = await getStaffById(s.id)
    fillEditForm(d)
  } catch (e: unknown) {
    editError.value = errMsg(e, 'Could not load staff details')
  } finally {
    editLoading.value = false
  }
}

/** Edit from the detail modal — snapshot before closing so the id survives */
function editFromDetail() {
  const s = selectedStaff.value
  if (!s) return
  closeDetail()
  openEdit(s)
}

function closeEdit() {
  if (editSaving.value) return
  showEditModal.value = false
  editingId.value = null
}

// ── Role permission preview (edit modal) — reflects the selected role's defaults ──
const rolePreview = ref<{ roleName: string; codes: string[] } | null>(null)
let rolePreviewTimer: ReturnType<typeof setTimeout> | null = null
watch(() => editForm.value.roleId, async (roleId) => {
  if (rolePreviewTimer) clearTimeout(rolePreviewTimer)
  if (!roleId) {
    rolePreview.value = null
    return
  }
  rolePreviewTimer = setTimeout(async () => {
    try {
      const perms = await getRolePermissions(roleId)
      const roleName = staffRoles.value.find(r => r.id === roleId)?.name ?? 'role'
      rolePreview.value = { roleName, codes: perms.map(p => p.code) }
    } catch {
      rolePreview.value = null
    }
  }, 200)
})

function validateEditForm(): boolean {
  const v: Record<string, string> = {}
  if (!editForm.value.fullName.trim()) v.fullName = 'Full name is required'
  if (!editForm.value.email.trim()) v.email = 'Email is required'
  else if (!/\S+@\S+\.\S+/.test(editForm.value.email)) v.email = 'Invalid email'
  if (!editForm.value.phone.trim()) v.phone = 'Phone is required'
  if (editForm.value.salaryKes < 0) v.salaryKes = 'Salary cannot be negative'
  editValidation.value = v
  return Object.keys(v).length === 0
}

async function handleEdit() {
  if (editingId.value === null) return
  if (!validateEditForm()) return
  editSaving.value = true
  editError.value = ''
  const payload: UpdateStaffPayload = {
    roleId: editForm.value.roleId,
    fullName: editForm.value.fullName.trim(),
    email: editForm.value.email.trim(),
    phone: editForm.value.phone.trim(),
    salaryCents: Math.round(editForm.value.salaryKes * 100),
    employmentType: editForm.value.employmentType,
    notes: editForm.value.notes.trim() || null,
    status: editForm.value.status,
  }
  try {
    const updated = await updateStaff(editingId.value, payload)
    const idx = staff.value.findIndex(s => s.id === updated.id)
    if (idx !== -1) staff.value[idx] = updated
    showEditModal.value = false
    editingId.value = null
    toast.success(`Staff member "${updated.fullName}" updated`)
    fetchStats()
  } catch (e: unknown) {
    editError.value = errMsg(e, 'Failed to update staff member')
    toast.error(editError.value)
  } finally {
    editSaving.value = false
  }
}

// ── Permissions (view + edit) ──
const showPermsModal = ref(false)
const permsLoading = ref(false)
const permsSaving = ref(false)
const permsError = ref('')
const permsStaff = ref<StaffSummary | null>(null)
const allPermissions = ref<Permission[]>([])
const roleDefaultCodes = ref<Set<string>>(new Set())
const effectivePerms = ref<Record<string, boolean>>({})
const permOverrides = ref<PermissionOverride[]>([])
const permsLoadedFor = ref<number | null>(null)
const detailPermsLoading = ref(false)
/** Permissions section starts collapsed; edit controls appear only when expanded */
const permsExpanded = ref(false)

/** Load the master permission list + the role defaults + this user's overrides */
async function ensurePermsLoaded(s: StaffSummary) {
  if (permsLoadedFor.value === s.id && allPermissions.value.length > 0) return
  permsLoading.value = true
  detailPermsLoading.value = true
  try {
    const [perms, rolePerms, userDetail] = await Promise.all([
      getPermissions(),
      getRolePermissions(s.roleId),
      getUserById(s.userId),
    ])
    allPermissions.value = perms
    roleDefaultCodes.value = new Set(rolePerms.map(p => p.code))
    const eff = new Set(userDetail.permissions ?? [])
    const map: Record<string, boolean> = {}
    for (const p of perms) map[p.code] = eff.has(p.code)
    effectivePerms.value = map
    permOverrides.value = userDetail.permissionOverrides ?? []
    permsLoadedFor.value = s.id
  } catch (e: unknown) {
    permsError.value = errMsg(e, 'Failed to load permissions')
  } finally {
    permsLoading.value = false
    detailPermsLoading.value = false
  }
}

/** Codes that are selected (effective) but invalid — non-view permission without its resource view. */
const invalidCodes = computed(() =>
  computeInvalidCodes(
    Object.entries(effectivePerms.value).filter(([, on]) => on).map(([c]) => c),
    allPermissions.value.map(p => p.code),
  ),
)

/** Visual state of one permission: granted by role, extra (added), revoked (default disabled), invalid, or off */
function permState(code: string): 'role' | 'extra' | 'revoked' | 'invalid' | 'off' {
  const on = effectivePerms.value[code] ?? false
  const inRole = roleDefaultCodes.value.has(code)
  if (on && invalidCodes.value.has(code)) return 'invalid'
  if (inRole && on) return 'role'
  if (!inRole && on) return 'extra'
  if (inRole && !on) return 'revoked'
  return 'off'
}

function permChipClass(state: 'role' | 'extra' | 'revoked' | 'invalid' | 'off'): string {
  switch (state) {
    case 'role': return 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-300'
    case 'extra': return 'bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-300 ring-1 ring-blue-400/60 dark:ring-blue-700'
    case 'revoked': return 'bg-gray-200 text-gray-500 dark:bg-gray-700 dark:text-gray-400 line-through'
    case 'invalid': return 'bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-300 ring-1 ring-red-400/60 dark:ring-red-700'
    default: return 'bg-gray-100 text-gray-400 dark:bg-gray-800/60 dark:text-gray-600'
  }
}

const permGroups = computed(() => {
  const groups = new Map<string, Permission[]>()
  for (const p of allPermissions.value) {
    const g = p.group || 'other'
    if (!groups.has(g)) groups.set(g, [])
    groups.get(g)!.push(p)
  }
  return [...groups.entries()].map(([name, items]) => ({ name, items }))
})

const permCounts = computed(() => {
  let granted = 0
  let extra = 0
  let revoked = 0
  for (const p of allPermissions.value) {
    const st = permState(p.code)
    if (st === 'role' || st === 'extra') granted++
    if (st === 'extra') extra++
    if (st === 'revoked') revoked++
  }
  return { granted, extra, revoked, total: allPermissions.value.length }
})

function shortCode(code: string): string {
  const parts = code.split('.')
  return parts.length > 1 ? parts[1]! : code
}

function togglePerm(code: string) {
  permsError.value = ''
  effectivePerms.value[code] = !(effectivePerms.value[code] ?? false)
}

function resetPermsToRole() {
  const map: Record<string, boolean> = {}
  for (const p of allPermissions.value) map[p.code] = roleDefaultCodes.value.has(p.code)
  effectivePerms.value = map
}

/** Diff the effective state against the role defaults — only deviations become overrides */
function computeOverrides(): PermissionOverride[] {
  const out: PermissionOverride[] = []
  for (const p of allPermissions.value) {
    const on = effectivePerms.value[p.code] ?? false
    const inRole = roleDefaultCodes.value.has(p.code)
    if (on !== inRole) out.push({ code: p.code, isGranted: on })
  }
  return out
}

async function openPermsEditor(s: StaffSummary) {
  permsStaff.value = s
  permsError.value = ''
  showPermsModal.value = true
  await ensurePermsLoaded(s)
}

function closePermsModal() {
  if (permsSaving.value) return
  showPermsModal.value = false
  permsStaff.value = null
}

async function savePerms() {
  if (!permsStaff.value) return
  permsSaving.value = true
  permsError.value = ''
  try {
    await updateUserPermissions(permsStaff.value.userId, computeOverrides())
    permsLoadedFor.value = null // force a fresh load next time
    toast.success(`Permissions updated for ${permsStaff.value.fullName}`)
    showPermsModal.value = false
    permsStaff.value = null
  } catch (e: unknown) {
    permsError.value = errMsg(e, 'Failed to save permissions')
    toast.error(permsError.value)
  } finally {
    permsSaving.value = false
  }
}

// ── Delete ──
function openDelete(s: StaffSummary) {
  deleteTarget.value = s
  deleteError.value = ''
  showDelete.value = true
}
async function handleDelete() {
  if (!deleteTarget.value) return
  const target = deleteTarget.value
  deleteLoading.value = true
  deleteError.value = ''
  try {
    await deleteStaff(target.id)
    staff.value = staff.value.filter(s => s.id !== target.id)
    totalCount.value = Math.max(0, totalCount.value - 1)
    totalPages.value = Math.max(1, Math.ceil(totalCount.value / pageSize.value))
    if (staff.value.length === 0 && page.value > 1) {
      page.value -= 1
      fetchStaff()
    }
    showDelete.value = false
    deleteTarget.value = null
    toast.success(`Staff member "${target.fullName}" deleted`)
    fetchStats()
  } catch (e: unknown) {
    deleteError.value = errMsg(e, 'Failed to delete staff member')
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
        <h1 class="text-2xl font-bold text-gray-800 dark:text-gray-100">Staff</h1>
        <p class="text-sm text-gray-500 dark:text-gray-400 mt-1">
          {{ totalCount }} total · {{ stats.active }} active · {{ stats.inactive }} inactive
        </p>
      </div>
      <div class="flex items-center gap-3">
        <div class="relative">
          <svg class="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
            <path stroke-linecap="round" stroke-linejoin="round" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
          </svg>
          <input v-model="search" type="text" placeholder="Search name, code, email, phone..."
            class="pl-10 pr-4 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 text-sm focus:ring-2 focus:ring-blue-500 outline-none transition w-64" />
        </div>
        <Can permission="staff.create">
          <button @click="openCreate"
            class="px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white text-sm font-medium rounded-lg transition-all duration-200 cursor-pointer flex items-center gap-2 whitespace-nowrap">
            <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
              <path stroke-linecap="round" stroke-linejoin="round" d="M12 4v16m8-8H4" />
            </svg>
            Add Staff
          </button>
        </Can>
      </div>
    </div>

    <!-- ── Stats cards ── -->
    <div class="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-6">
      <div class="bg-white dark:bg-gray-900 rounded-xl shadow-sm border border-gray-100 dark:border-gray-800 p-4">
        <p class="text-xs font-medium text-gray-400 dark:text-gray-500">Total Staff</p>
        <p class="text-2xl font-bold text-gray-800 dark:text-gray-100 mt-1">{{ stats.total }}</p>
      </div>
      <div class="bg-white dark:bg-gray-900 rounded-xl shadow-sm border border-gray-100 dark:border-gray-800 p-4">
        <p class="text-xs font-medium text-gray-400 dark:text-gray-500">Active</p>
        <p class="text-2xl font-bold text-green-600 dark:text-green-400 mt-1">{{ stats.active }}</p>
      </div>
      <div class="bg-white dark:bg-gray-900 rounded-xl shadow-sm border border-gray-100 dark:border-gray-800 p-4">
        <p class="text-xs font-medium text-gray-400 dark:text-gray-500">Inactive</p>
        <p class="text-2xl font-bold text-gray-500 dark:text-gray-400 mt-1">{{ stats.inactive }}</p>
      </div>
      <div class="bg-white dark:bg-gray-900 rounded-xl shadow-sm border border-gray-100 dark:border-gray-800 p-4">
        <p class="text-xs font-medium text-gray-400 dark:text-gray-500">Monthly Salary Spend</p>
        <p class="text-2xl font-bold text-blue-600 dark:text-blue-400 mt-1">{{ formatPrice(stats.totalMonthlySalaryCents) }}</p>
      </div>
    </div>

    <!-- ── Page Size ── -->
    <div class="flex items-center gap-2 mb-4">
      <span class="text-xs text-gray-400 dark:text-gray-500">Show</span>
      <select v-model="pageSize" @change="page = 1; fetchStaff()"
        class="px-2 py-1 rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 text-xs outline-none cursor-pointer">
        <option :value="5">5</option>
        <option :value="10">10</option>
        <option :value="20">20</option>
        <option :value="50">50</option>
      </select>
      <span class="text-xs text-gray-400 dark:text-gray-500">per page</span>
    </div>

    <!-- ── Loading ── -->
    <div v-if="loading && staff.length === 0" class="flex justify-center py-20">
      <div class="flex flex-col items-center gap-3">
        <div class="w-8 h-8 border-4 border-blue-500 border-t-transparent rounded-full animate-spin"></div>
        <p class="text-sm text-gray-400 dark:text-gray-500">Loading staff...</p>
      </div>
    </div>

    <!-- ── Error ── -->
    <div v-else-if="error && staff.length === 0" class="text-center py-20">
      <div class="bg-red-50 dark:bg-red-900/30 border border-red-200 dark:border-red-800 rounded-xl p-6 max-w-md mx-auto">
        <p class="text-red-600 dark:text-red-400 font-medium">{{ error }}</p>
        <button @click="fetchStaff" class="mt-3 px-4 py-2 bg-red-600 hover:bg-red-700 text-white text-sm rounded-lg transition cursor-pointer">Retry</button>
      </div>
    </div>

    <!-- ── Table ── -->
    <div v-else class="bg-white dark:bg-gray-900 rounded-xl shadow-sm border border-gray-100 dark:border-gray-800 overflow-hidden">
      <div class="overflow-x-auto">
        <table class="w-full text-sm">
          <thead>
            <tr class="border-b border-gray-100 dark:border-gray-800 bg-gray-50 dark:bg-gray-800/50">
              <th @click="toggleSort('name')" class="px-4 py-3 text-left font-semibold text-gray-600 dark:text-gray-400 cursor-pointer hover:text-blue-600 dark:hover:text-blue-400 transition whitespace-nowrap">Staff <span class="text-xs ml-1">{{ sortIcon('name') }}</span></th>
              <th @click="toggleSort('role')" class="px-4 py-3 text-left font-semibold text-gray-600 dark:text-gray-400 cursor-pointer hover:text-blue-600 dark:hover:text-blue-400 transition whitespace-nowrap">Role <span class="text-xs ml-1">{{ sortIcon('role') }}</span></th>
              <th class="px-4 py-3 text-left font-semibold text-gray-600 dark:text-gray-400 whitespace-nowrap hidden md:table-cell">Contact</th>
              <th class="px-4 py-3 text-left font-semibold text-gray-600 dark:text-gray-400 whitespace-nowrap hidden lg:table-cell">Employment</th>
              <th @click="toggleSort('salary')" class="px-4 py-3 text-left font-semibold text-gray-600 dark:text-gray-400 cursor-pointer hover:text-blue-600 dark:hover:text-blue-400 transition whitespace-nowrap">Salary <span class="text-xs ml-1">{{ sortIcon('salary') }}</span></th>
              <th @click="toggleSort('status')" class="px-4 py-3 text-left font-semibold text-gray-600 dark:text-gray-400 cursor-pointer hover:text-blue-600 dark:hover:text-blue-400 transition whitespace-nowrap">Status <span class="text-xs ml-1">{{ sortIcon('status') }}</span></th>
              <th class="px-4 py-3 text-right font-semibold text-gray-600 dark:text-gray-400 whitespace-nowrap">Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="s in staff" :key="s.id"
              class="border-b border-gray-50 dark:border-gray-800/50 hover:bg-gray-50 dark:hover:bg-gray-800/30 transition"
              :class="{ 'opacity-50': optimisticStaff?.id === s.id }">
              <td class="px-4 py-3">
                <div class="flex items-center gap-3">
                  <div class="w-8 h-8 rounded-full bg-indigo-100 dark:bg-indigo-900/50 flex items-center justify-center text-xs font-bold text-indigo-600 dark:text-indigo-400 flex-shrink-0">{{ s.fullName.charAt(0).toUpperCase() }}</div>
                  <div>
                    <p class="font-medium text-gray-800 dark:text-gray-200">
                      {{ s.fullName }}
                      <span v-if="optimisticStaff?.id === s.id" class="text-xs text-blue-500 font-normal ml-1">Adding...</span>
                    </p>
                    <p class="text-xs text-gray-400 dark:text-gray-500 font-mono">{{ s.staffCode }}</p>
                  </div>
                </div>
              </td>
              <td class="px-4 py-3">
                <span :class="roleClass(s.roleName)" class="px-2 py-0.5 rounded-full text-xs font-medium">{{ s.roleName }}</span>
              </td>
              <td class="px-4 py-3 hidden md:table-cell">
                <p class="text-gray-600 dark:text-gray-400">{{ s.email || '—' }}</p>
                <p class="text-xs text-gray-400 dark:text-gray-500 font-mono">{{ s.phone }}</p>
              </td>
              <td class="px-4 py-3 hidden lg:table-cell">
                <p class="text-gray-600 dark:text-gray-400 capitalize">{{ s.employmentType }}</p>
                <p class="text-xs text-gray-400 dark:text-gray-500">Joined {{ friendlyDate(s.dateJoined) }}</p>
              </td>
              <td class="px-4 py-3 font-semibold text-gray-800 dark:text-gray-200 whitespace-nowrap">{{ formatPrice(s.salaryCents) }}</td>
              <td class="px-4 py-3">
                <div class="flex items-center gap-1.5">
                  <span :class="statusDot(s.status)" class="w-2 h-2 rounded-full inline-block"></span>
                  <span :class="statusClass(s.status)" class="px-2 py-0.5 rounded-full text-xs font-medium capitalize">{{ s.status }}</span>
                </div>
              </td>
              <td class="px-4 py-3 text-right">
                <div class="flex items-center justify-end gap-1.5">
                  <button @click="openDetail(s)"
                    class="px-3 py-1.5 text-xs font-medium text-blue-600 dark:text-blue-400 bg-blue-50 dark:bg-blue-900/20 hover:bg-blue-100 dark:hover:bg-blue-900/40 rounded-lg transition cursor-pointer">
                    View
                  </button>
                  <Can permission="staff.update">
                    <button @click="openEdit(s)"
                      class="px-3 py-1.5 text-xs font-medium text-indigo-600 dark:text-indigo-400 bg-indigo-50 dark:bg-indigo-900/20 hover:bg-indigo-100 dark:hover:bg-indigo-900/40 rounded-lg transition cursor-pointer">
                      Edit
                    </button>
                  </Can>
                  <Can permission="staff.delete">
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
      <div v-if="staff.length === 0 && !loading && !error" class="text-center py-16">
        <p class="text-gray-400 dark:text-gray-500">No staff members found</p>
        <Can permission="staff.create">
          <button @click="openCreate" class="mt-2 text-xs text-blue-600 dark:text-blue-400 hover:underline cursor-pointer">Add the first staff member</button>
        </Can>
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

    <!-- ── Create Staff Modal ── -->
    <Teleport to="body">
      <div v-if="showCreateModal" class="fixed inset-0 z-50 flex items-center justify-center p-4" @click.self="showCreateModal = false">
        <div class="absolute inset-0 bg-black/50 backdrop-blur-sm" @click="showCreateModal = false"></div>
        <div class="relative bg-white dark:bg-gray-900 rounded-2xl shadow-2xl w-full max-w-lg max-h-[90vh] overflow-y-auto animate-modal-in p-6">
          <div class="flex items-center justify-between mb-5">
            <h2 class="text-lg font-bold text-gray-800 dark:text-gray-100">Add Staff Member</h2>
            <button @click="showCreateModal = false" :disabled="createLoading"
              class="w-7 h-7 rounded-full bg-gray-100 dark:bg-gray-800 flex items-center justify-center text-gray-400 hover:bg-gray-200 dark:hover:bg-gray-700 cursor-pointer text-sm disabled:opacity-40">✕</button>
          </div>

          <form @submit.prevent="handleCreate" class="space-y-4">
            <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <div class="sm:col-span-2">
                <div class="flex items-center gap-1.5 mb-1">
                  <label class="text-sm font-medium text-gray-700 dark:text-gray-300">Role *</label>
                  <FieldTip text="The staff role controls what this account can access. The Customer role is not available for staff." />
                </div>
                <select v-model="createForm.roleId" :disabled="rolesLoading || staffRoles.length === 0"
                  class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 text-sm focus:ring-2 focus:ring-blue-500 outline-none transition cursor-pointer disabled:opacity-50">
                  <option v-if="rolesLoading" value="" disabled>Loading roles...</option>
                  <option v-for="r in staffRoles" :key="r.id" :value="r.id">{{ r.name }}</option>
                </select>
                <p v-if="createValidation.role" class="text-xs text-red-500 mt-1">{{ createValidation.role }}</p>
              </div>

              <div>
                <div class="flex items-center gap-1.5 mb-1">
                  <label class="text-sm font-medium text-gray-700 dark:text-gray-300">Full Name *</label>
                  <FieldTip text="The staff member full name." />
                </div>
                <input v-model="createForm.fullName" type="text" placeholder="John Kamau"
                  class="w-full px-3 py-2 rounded-lg border text-sm focus:ring-2 focus:ring-blue-500 outline-none transition"
                  :class="createValidation.fullName ? 'border-red-400' : 'border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100'" />
                <p v-if="createValidation.fullName" class="text-xs text-red-500 mt-1">{{ createValidation.fullName }}</p>
              </div>

              <div>
                <div class="flex items-center gap-1.5 mb-1">
                  <label class="text-sm font-medium text-gray-700 dark:text-gray-300">Phone *</label>
                  <FieldTip text="Contact number for the staff member." />
                </div>
                <input v-model="createForm.phone" type="tel" placeholder="+254 712 345 678"
                  class="w-full px-3 py-2 rounded-lg border text-sm focus:ring-2 focus:ring-blue-500 outline-none transition"
                  :class="createValidation.phone ? 'border-red-400' : 'border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100'" />
                <p v-if="createValidation.phone" class="text-xs text-red-500 mt-1">{{ createValidation.phone }}</p>
              </div>

              <div>
                <div class="flex items-center gap-1.5 mb-1">
                  <label class="text-sm font-medium text-gray-700 dark:text-gray-300">Email *</label>
                  <FieldTip text="Login email for this staff account." />
                </div>
                <input v-model="createForm.email" type="email" placeholder="john@example.com"
                  class="w-full px-3 py-2 rounded-lg border text-sm focus:ring-2 focus:ring-blue-500 outline-none transition"
                  :class="createValidation.email ? 'border-red-400' : 'border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100'" />
                <p v-if="createValidation.email" class="text-xs text-red-500 mt-1">{{ createValidation.email }}</p>
              </div>

              <div>
                <div class="flex items-center gap-1.5 mb-1">
                  <label class="text-sm font-medium text-gray-700 dark:text-gray-300">Password *</label>
                  <FieldTip text="Initial password, minimum 4 characters." />
                </div>
                <input v-model="createForm.password" type="password" placeholder="••••••"
                  class="w-full px-3 py-2 rounded-lg border text-sm focus:ring-2 focus:ring-blue-500 outline-none transition"
                  :class="createValidation.password ? 'border-red-400' : 'border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100'" />
                <p v-if="createValidation.password" class="text-xs text-red-500 mt-1">{{ createValidation.password }}</p>
              </div>

              <div>
                <div class="flex items-center gap-1.5 mb-1">
                  <label class="text-sm font-medium text-gray-700 dark:text-gray-300">Monthly Salary ({{ currency }}) *</label>
                  <FieldTip text="Gross monthly salary in Kenya Shillings. Tracked to monitor the total money spent on staff." />
                </div>
                <input v-model.number="createForm.salaryKes" type="number" min="0" step="0.01" placeholder="45000"
                  class="w-full px-3 py-2 rounded-lg border text-sm focus:ring-2 focus:ring-blue-500 outline-none transition"
                  :class="createValidation.salaryKes ? 'border-red-400' : 'border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100'" />
                <p v-if="createValidation.salaryKes" class="text-xs text-red-500 mt-1">{{ createValidation.salaryKes }}</p>
              </div>

              <div>
                <div class="flex items-center gap-1.5 mb-1">
                  <label class="text-sm font-medium text-gray-700 dark:text-gray-300">Employment Type</label>
                  <FieldTip text="Full-time, part-time or contract." />
                </div>
                <select v-model="createForm.employmentType"
                  class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 text-sm focus:ring-2 focus:ring-blue-500 outline-none transition cursor-pointer capitalize">
                  <option v-for="t in employmentTypes" :key="t" :value="t" class="capitalize">{{ t }}</option>
                </select>
              </div>

              <div class="sm:col-span-2">
                <div class="flex items-center gap-1.5 mb-1">
                  <label class="text-sm font-medium text-gray-700 dark:text-gray-300">Notes</label>
                  <FieldTip text="Internal notes about this staff member, visible only to staff. The join date is recorded automatically as the account creation date." />
                </div>
                <textarea v-model="createForm.notes" rows="2" placeholder="Position details, ID number, etc."
                  class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 text-sm focus:ring-2 focus:ring-blue-500 outline-none transition resize-none"></textarea>
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
                Add Staff
              </button>
            </div>
          </form>
        </div>
      </div>
    </Teleport>

    <!-- ── Edit Staff Modal ── -->
    <Teleport to="body">
      <div v-if="showEditModal" class="fixed inset-0 z-50 flex items-center justify-center p-4" @click.self="closeEdit">
        <div class="absolute inset-0 bg-black/50 backdrop-blur-sm" @click="closeEdit"></div>
        <div class="relative bg-white dark:bg-gray-900 rounded-2xl shadow-2xl w-full max-w-lg max-h-[90vh] overflow-y-auto animate-modal-in p-6">
          <div class="flex items-center justify-between mb-5">
            <h2 class="text-lg font-bold text-gray-800 dark:text-gray-100">Edit Staff Member</h2>
            <button @click="closeEdit" :disabled="editSaving"
              class="w-7 h-7 rounded-full bg-gray-100 dark:bg-gray-800 flex items-center justify-center text-gray-400 hover:bg-gray-200 dark:hover:bg-gray-700 cursor-pointer text-sm disabled:opacity-40">✕</button>
          </div>

          <div v-if="editLoading" class="flex justify-center py-16">
            <div class="w-8 h-8 border-4 border-blue-500 border-t-transparent rounded-full animate-spin"></div>
          </div>

          <form v-else @submit.prevent="handleEdit" class="space-y-4">
            <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <div class="sm:col-span-2">
                <div class="flex items-center gap-1.5 mb-1">
                  <label class="text-sm font-medium text-gray-700 dark:text-gray-300">Role *</label>
                  <FieldTip text="Changing the role changes what this account can access." />
                </div>
                <select v-model="editForm.roleId" :disabled="staffRoles.length === 0"
                  class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 text-sm focus:ring-2 focus:ring-blue-500 outline-none transition cursor-pointer disabled:opacity-50">
                  <option v-for="r in staffRoles" :key="r.id" :value="r.id">{{ r.name }}</option>
                </select>
              </div>

              <!-- Role permission preview — updates with the selected role -->
              <div v-if="rolePreview" class="sm:col-span-2 rounded-lg border border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-800/60 px-3 py-2">
                <p class="text-xs font-medium text-gray-600 dark:text-gray-300">
                  <span class="capitalize">{{ rolePreview.roleName }}</span> role grants
                  <span class="font-bold text-blue-600 dark:text-blue-400">{{ rolePreview.codes.length }}</span> permissions
                  <span class="text-gray-400 dark:text-gray-500">— user-level overrides still apply.</span>
                </p>
                <div class="flex flex-wrap gap-1 mt-1.5">
                  <span v-for="c in rolePreview.codes.slice(0, 12)" :key="c"
                    class="px-1.5 py-0.5 rounded bg-blue-100 dark:bg-blue-900/30 text-blue-700 dark:text-blue-300 text-[10px] font-medium">{{ shortCode(c) }}</span>
                  <span v-if="rolePreview.codes.length > 12" class="text-[10px] text-gray-400 self-center">+{{ rolePreview.codes.length - 12 }} more</span>
                </div>
              </div>

              <div>
                <div class="flex items-center gap-1.5 mb-1">
                  <label class="text-sm font-medium text-gray-700 dark:text-gray-300">Full Name *</label>
                  <FieldTip text="The staff member full name." />
                </div>
                <input v-model="editForm.fullName" type="text"
                  class="w-full px-3 py-2 rounded-lg border text-sm focus:ring-2 focus:ring-blue-500 outline-none transition"
                  :class="editValidation.fullName ? 'border-red-400' : 'border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100'" />
                <p v-if="editValidation.fullName" class="text-xs text-red-500 mt-1">{{ editValidation.fullName }}</p>
              </div>

              <div>
                <div class="flex items-center gap-1.5 mb-1">
                  <label class="text-sm font-medium text-gray-700 dark:text-gray-300">Phone *</label>
                  <FieldTip text="Contact number for the staff member." />
                </div>
                <input v-model="editForm.phone" type="tel"
                  class="w-full px-3 py-2 rounded-lg border text-sm focus:ring-2 focus:ring-blue-500 outline-none transition"
                  :class="editValidation.phone ? 'border-red-400' : 'border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100'" />
                <p v-if="editValidation.phone" class="text-xs text-red-500 mt-1">{{ editValidation.phone }}</p>
              </div>

              <div>
                <div class="flex items-center gap-1.5 mb-1">
                  <label class="text-sm font-medium text-gray-700 dark:text-gray-300">Email *</label>
                  <FieldTip text="Login email for this staff account." />
                </div>
                <input v-model="editForm.email" type="email"
                  class="w-full px-3 py-2 rounded-lg border text-sm focus:ring-2 focus:ring-blue-500 outline-none transition"
                  :class="editValidation.email ? 'border-red-400' : 'border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100'" />
                <p v-if="editValidation.email" class="text-xs text-red-500 mt-1">{{ editValidation.email }}</p>
              </div>

              <div>
                <div class="flex items-center gap-1.5 mb-1">
                  <label class="text-sm font-medium text-gray-700 dark:text-gray-300">Monthly Salary ({{ currency }}) *</label>
                  <FieldTip text="Gross monthly salary. Updating it changes the monthly salary spend total." />
                </div>
                <input v-model.number="editForm.salaryKes" type="number" min="0" step="0.01"
                  class="w-full px-3 py-2 rounded-lg border text-sm focus:ring-2 focus:ring-blue-500 outline-none transition"
                  :class="editValidation.salaryKes ? 'border-red-400' : 'border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100'" />
                <p v-if="editValidation.salaryKes" class="text-xs text-red-500 mt-1">{{ editValidation.salaryKes }}</p>
              </div>

              <div>
                <div class="flex items-center gap-1.5 mb-1">
                  <label class="text-sm font-medium text-gray-700 dark:text-gray-300">Employment Type</label>
                  <FieldTip text="Full-time, part-time or contract." />
                </div>
                <select v-model="editForm.employmentType"
                  class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 text-sm focus:ring-2 focus:ring-blue-500 outline-none transition cursor-pointer capitalize">
                  <option v-for="t in employmentTypes" :key="t" :value="t" class="capitalize">{{ t }}</option>
                </select>
              </div>

              <div>
                <div class="flex items-center gap-1.5 mb-1">
                  <label class="text-sm font-medium text-gray-700 dark:text-gray-300">Status</label>
                  <FieldTip text="Inactive accounts cannot log in." />
                </div>
                <select v-model="editForm.status"
                  class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 text-sm focus:ring-2 focus:ring-blue-500 outline-none transition cursor-pointer capitalize">
                  <option value="active">Active</option>
                  <option value="inactive">Inactive</option>
                </select>
              </div>

              <div class="sm:col-span-2">
                <div class="flex items-center gap-1.5 mb-1">
                  <label class="text-sm font-medium text-gray-700 dark:text-gray-300">Notes</label>
                  <FieldTip text="Internal notes about this staff member." />
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

          <div v-else-if="selectedStaff" class="space-y-6">
            <!-- Header -->
            <div class="flex items-start justify-between gap-4 pb-4 border-b border-gray-100 dark:border-gray-800">
              <div>
                <div class="flex items-center gap-2 flex-wrap">
                  <h2 class="text-xl font-bold text-gray-800 dark:text-gray-100">{{ selectedStaff.fullName }}</h2>
                  <span :class="roleClass(selectedStaff.roleName)" class="px-2 py-0.5 rounded-full text-xs font-medium">{{ selectedStaff.roleName }}</span>
                  <span :class="statusClass(selectedStaff.status)" class="px-2 py-0.5 rounded-full text-xs font-medium capitalize">{{ selectedStaff.status }}</span>
                </div>
                <p class="text-xs text-gray-400 dark:text-gray-500 font-mono mt-1">{{ selectedStaff.staffCode }}</p>
              </div>
              <Can permission="staff.update">
                <button @click="editFromDetail"
                  class="px-3 py-1.5 text-xs font-medium text-indigo-600 dark:text-indigo-400 bg-indigo-50 dark:bg-indigo-900/20 hover:bg-indigo-100 dark:hover:bg-indigo-900/40 rounded-lg transition cursor-pointer whitespace-nowrap">
                  Edit Staff
                </button>
              </Can>
            </div>

            <!-- Salary highlight -->
            <div class="bg-blue-50 dark:bg-blue-900/30 rounded-xl p-4 text-center">
              <span class="text-4xl font-bold text-blue-600 dark:text-blue-400">{{ formatPrice(selectedStaff.salaryCents) }}</span>
              <span class="text-blue-400 dark:text-blue-300 text-sm"> /month</span>
            </div>

            <!-- Contact -->
            <div>
              <h3 class="text-sm font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider mb-3">Contact</h3>
              <div class="grid grid-cols-2 gap-3">
                <div class="bg-gray-50 dark:bg-gray-800 rounded-lg p-3">
                  <p class="text-xs text-gray-400 dark:text-gray-500 mb-1">Email</p>
                  <p class="font-semibold text-gray-800 dark:text-gray-100 text-sm break-all">{{ selectedStaff.email || '—' }}</p>
                </div>
                <div class="bg-gray-50 dark:bg-gray-800 rounded-lg p-3">
                  <p class="text-xs text-gray-400 dark:text-gray-500 mb-1">Phone</p>
                  <p class="font-semibold text-gray-800 dark:text-gray-100 text-sm font-mono">{{ selectedStaff.phone }}</p>
                </div>
              </div>
            </div>

            <!-- Employment -->
            <div>
              <h3 class="text-sm font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider mb-3">Employment</h3>
              <div class="grid grid-cols-2 sm:grid-cols-3 gap-3">
                <div class="bg-gray-50 dark:bg-gray-800 rounded-lg p-3">
                  <p class="text-xs text-gray-400 dark:text-gray-500 mb-1">Type</p>
                  <p class="font-semibold text-gray-800 dark:text-gray-100 text-sm capitalize">{{ selectedStaff.employmentType }}</p>
                </div>
                <div class="bg-gray-50 dark:bg-gray-800 rounded-lg p-3">
                  <p class="text-xs text-gray-400 dark:text-gray-500 mb-1">Date Joined</p>
                  <p class="font-semibold text-gray-800 dark:text-gray-100 text-sm">{{ friendlyDate(selectedStaff.dateJoined) }}</p>
                </div>
              </div>
              <p v-if="selectedStaff.notes" class="mt-3 text-sm text-gray-500 dark:text-gray-400 bg-gray-50 dark:bg-gray-800 rounded-lg px-3 py-2"><span class="font-semibold text-gray-600 dark:text-gray-300">Notes: </span>{{ selectedStaff.notes }}</p>
            </div>

            <!-- Permissions (collapsed by default — expand to view and edit) -->
            <div>
              <button type="button" @click="permsExpanded = !permsExpanded"
                class="w-full flex items-center justify-between gap-3 rounded-xl border border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-800/60 px-4 py-3 transition hover:bg-gray-100 dark:hover:bg-gray-800 cursor-pointer">
                <span class="flex items-center gap-2.5">
                  <svg class="w-4 h-4 text-gray-400 transition-transform duration-200" :class="permsExpanded ? 'rotate-180' : ''" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                    <path stroke-linecap="round" stroke-linejoin="round" d="M19 9l-7 7-7-7" />
                  </svg>
                  <span class="text-sm font-semibold text-gray-700 dark:text-gray-300">Permissions</span>
                </span>
                <span class="text-xs text-gray-500 dark:text-gray-400">
                  <span v-if="detailPermsLoading">Loading…</span>
                  <template v-else>
                    {{ permCounts.granted }}/{{ permCounts.total }} granted
                    <span v-if="permCounts.extra > 0" class="text-blue-600 dark:text-blue-400"> · {{ permCounts.extra }} extra</span>
                    <span v-if="permCounts.revoked > 0" class="text-gray-400 dark:text-gray-500"> · {{ permCounts.revoked }} revoked</span>
                  </template>
                </span>
              </button>

              <div v-if="permsExpanded" class="mt-3">
                <div class="flex items-center justify-between gap-3 mb-3 flex-wrap">
                  <!-- Legend -->
                  <div class="flex items-center gap-3 text-[11px] text-gray-500 dark:text-gray-400">
                    <span class="flex items-center gap-1"><span class="w-2 h-2 rounded-full bg-green-500 inline-block"></span> Role</span>
                    <span class="flex items-center gap-1"><span class="w-2 h-2 rounded-full bg-blue-500 inline-block"></span> Extra</span>
                    <span class="flex items-center gap-1"><span class="w-2 h-2 rounded-full bg-gray-400 inline-block"></span> Revoked</span>
                  </div>
                  <div class="flex items-center gap-2">
                    <Can permission="users.update">
                      <button @click="openPermsEditor(selectedStaff)"
                        class="px-3 py-1.5 text-xs font-medium text-indigo-600 dark:text-indigo-400 bg-indigo-50 dark:bg-indigo-900/20 hover:bg-indigo-100 dark:hover:bg-indigo-900/40 rounded-lg transition cursor-pointer whitespace-nowrap">
                        Edit permissions
                      </button>
                    </Can>
                    <button @click="permsExpanded = false"
                      class="px-3 py-1.5 text-xs font-medium text-gray-600 dark:text-gray-300 bg-gray-100 dark:bg-gray-800 hover:bg-gray-200 dark:hover:bg-gray-700 rounded-lg transition cursor-pointer whitespace-nowrap">
                      Collapse
                    </button>
                  </div>
                </div>

                <div v-if="detailPermsLoading" class="text-xs text-gray-400 dark:text-gray-500 py-2">Loading permissions…</div>
                <div v-else class="space-y-3">
                  <div v-for="g in permGroups" :key="g.name" class="rounded-xl border border-gray-100 dark:border-gray-800 bg-gray-50 dark:bg-gray-800/60 p-3">
                    <p class="text-xs font-semibold text-gray-500 dark:text-gray-400 capitalize">{{ g.name }}</p>
                    <div class="flex flex-wrap gap-1.5 mt-2">
                      <span v-for="p in g.items" :key="p.code" :title="p.description"
                        class="px-2 py-0.5 rounded-full text-[11px] font-medium"
                        :class="permChipClass(permState(p.code))">
                        {{ shortCode(p.code) }}
                        <span v-if="permState(p.code) === 'extra'" class="font-bold">+</span>
                        <span v-if="permState(p.code) === 'revoked'">✕</span>
                      </span>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <div v-else class="p-8 text-center">
            <p class="text-red-500 dark:text-red-400">Failed to load staff details.</p>
            <button @click="closeDetail" class="mt-4 text-blue-600 dark:text-blue-400 hover:underline cursor-pointer">Close</button>
          </div>
        </div>
      </div>
    </Teleport>

    <!-- ── Permissions Modal ── -->
    <Teleport to="body">
      <div v-if="showPermsModal" class="fixed inset-0 z-50 flex items-center justify-center p-4" @click.self="closePermsModal">
        <div class="absolute inset-0 bg-black/50 backdrop-blur-sm" @click="closePermsModal"></div>
        <div class="relative bg-white dark:bg-gray-900 rounded-2xl shadow-2xl w-full max-w-2xl max-h-[90vh] overflow-y-auto animate-modal-in p-6">
          <div class="flex items-center justify-between mb-1">
            <div>
              <h2 class="text-lg font-bold text-gray-800 dark:text-gray-100">Edit Permissions</h2>
              <p class="text-sm text-gray-500 dark:text-gray-400 mt-0.5">
                {{ permsStaff?.fullName }} · <span class="capitalize">{{ permsStaff?.roleName }}</span> · {{ permsStaff?.staffCode }}
              </p>
            </div>
            <button @click="closePermsModal" :disabled="permsSaving"
              class="w-7 h-7 rounded-full bg-gray-100 dark:bg-gray-800 flex items-center justify-center text-gray-400 hover:bg-gray-200 dark:hover:bg-gray-700 cursor-pointer text-sm disabled:opacity-40">✕</button>
          </div>

          <!-- Legend -->
          <div class="flex flex-wrap items-center gap-3 mt-3 mb-4 text-[11px] text-gray-500 dark:text-gray-400">
            <span class="flex items-center gap-1"><span class="w-2 h-2 rounded-full bg-green-500 inline-block"></span> Granted by role</span>
            <span class="flex items-center gap-1"><span class="w-2 h-2 rounded-full bg-blue-500 inline-block"></span> Extra (added)</span>
            <span class="flex items-center gap-1"><span class="w-2 h-2 rounded-full bg-gray-400 inline-block"></span> Revoked (default disabled)</span>
            <span class="ml-auto font-medium">{{ permCounts.granted }}/{{ permCounts.total }} granted · {{ permCounts.extra }} extra · {{ permCounts.revoked }} revoked</span>
          </div>

          <div v-if="permsLoading" class="flex justify-center py-16">
            <div class="w-8 h-8 border-4 border-blue-500 border-t-transparent rounded-full animate-spin"></div>
          </div>

          <div v-else class="space-y-3 max-h-[50vh] overflow-y-auto pr-1">
            <div v-for="g in permGroups" :key="g.name" class="rounded-xl border border-gray-100 dark:border-gray-800 bg-gray-50 dark:bg-gray-800/60 p-3">
              <p class="text-xs font-semibold text-gray-500 dark:text-gray-400 capitalize mb-2">{{ g.name }}</p>
              <div class="flex flex-wrap gap-1.5">
                <button v-for="p in g.items" :key="p.code" type="button" @click="togglePerm(p.code)" :title="p.description"
                  class="px-2 py-1 rounded-full text-[11px] font-medium transition cursor-pointer hover:opacity-80"
                  :class="permChipClass(permState(p.code))">
                  {{ shortCode(p.code) }}
                </button>
              </div>
            </div>
          </div>

          <p v-if="permsError" class="mt-4 text-sm text-red-600 dark:text-red-400 bg-red-50 dark:bg-red-900/20 rounded-lg px-3 py-2">{{ permsError }}</p>

          <!-- Invalid permissions warning (view-first rule) -->
          <div
            v-if="invalidCodes.size > 0"
            class="mt-4 rounded-lg bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 px-4 py-3"
          >
            <p class="text-sm text-red-700 dark:text-red-300 flex items-start gap-2">
              <svg class="w-4 h-4 mt-0.5 flex-shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                <path stroke-linecap="round" stroke-linejoin="round" d="M12 9v4m0 4h.01M10.29 3.86L1.82 18a2 2 0 001.71 3h16.94a2 2 0 001.71-3L13.71 3.86a2 2 0 00-3.42 0z" />
              </svg>
              <span>
                <span class="font-semibold">Save blocked:</span>
                these need their resource view granted first:
                <span class="font-mono">{{ [...invalidCodes].join(', ') }}</span>.
                Add the matching view, or deselect them, to proceed.
              </span>
            </p>
          </div>

          <div class="flex gap-3 mt-5">
            <button type="button" @click="resetPermsToRole" :disabled="permsSaving"
              class="px-4 py-2.5 text-sm font-medium text-gray-600 dark:text-gray-300 bg-gray-100 dark:bg-gray-800 hover:bg-gray-200 dark:hover:bg-gray-700 rounded-lg transition cursor-pointer disabled:opacity-40">
              Reset to role
            </button>
            <div class="flex-1"></div>
            <button type="button" @click="closePermsModal" :disabled="permsSaving"
              class="px-4 py-2.5 text-sm font-medium text-gray-600 dark:text-gray-300 bg-gray-100 dark:bg-gray-800 hover:bg-gray-200 dark:hover:bg-gray-700 rounded-lg transition cursor-pointer disabled:opacity-40">
              Cancel
            </button>
            <button type="button" @click="savePerms" :disabled="permsSaving || invalidCodes.size > 0"
              class="px-4 py-2.5 text-sm font-medium text-white bg-blue-600 hover:bg-blue-700 rounded-lg transition-all duration-200 cursor-pointer disabled:opacity-50 flex items-center justify-center gap-2">
              <span v-if="permsSaving" class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
              Save Permissions
            </button>
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
              <h2 class="text-lg font-bold text-gray-800 dark:text-gray-100">Delete Staff Member</h2>
              <p class="text-sm text-gray-500 dark:text-gray-400 mt-1">
                Are you sure you want to delete <span class="font-semibold text-gray-700 dark:text-gray-200">{{ deleteTarget?.fullName }}</span> ({{ deleteTarget?.staffCode }})?
                This removes the staff record and their login account. This cannot be undone.
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
              Delete Staff
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
