<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { getRoles, createRole, deleteRole, getRolePermissions, setRolePermissions } from '@/services/role.service'
import { getPermissions } from '@/services/user.service'
import { computeInvalidCodes } from '@/utils/permissions'
import FieldTip from '@/components/FieldTip.vue'
import type { Role, RolePermission } from '@/types/role.types'
import type { Permission } from '@/types/user.types'

// ── State ──
const roles = ref<Role[]>([])
const loading = ref(true)
const error = ref<string | null>(null)

// All permissions catalogue — fetched once, cached for this page
const allPermissions = ref<Permission[]>([])

// ── Create modal ──
const showCreateModal = ref(false)
const createName = ref('')
const createDescription = ref('')
const createLoading = ref(false)
const createError = ref('')
const createNameError = ref('')

// ── Delete role ──
const deleteTarget = ref<Role | null>(null)
const deleteLoading = ref(false)
const deleteError = ref('')

// ── Permissions modal ──
const showPermsModal = ref(false)
const selectedRole = ref<Role | null>(null)
const rolePerms = ref<Set<string>>(new Set())
const permsLoading = ref(false)
const permsError = ref('')
const editPerms = ref(false)
const pendingCodes = ref<Set<string>>(new Set())
const saveLoading = ref(false)
const saveError = ref('')
const saveSuccess = ref(false)

/** Codes currently selected that are invalid (non-view permission without its resource view). */
const invalidCodes = computed(() =>
  computeInvalidCodes(pendingCodes.value, allPermissions.value.map(p => p.code)),
)

// ── Fetch ──
async function fetchRoles() {
  loading.value = true
  error.value = null
  try {
    roles.value = await getRoles()
  } catch (e: any) {
    error.value = e?.message || 'Failed to load roles'
  } finally {
    loading.value = false
  }
}

onMounted(() => {
  fetchRoles()
  getPermissions().then(data => { allPermissions.value = data }).catch(() => {})
})

// ── Create role ──
function openCreate() {
  createName.value = ''
  createDescription.value = ''
  createError.value = ''
  createNameError.value = ''
  showCreateModal.value = true
}

async function handleCreate() {
  createNameError.value = ''
  createError.value = ''
  if (!createName.value.trim()) {
    createNameError.value = 'Role name is required'
    return
  }
  createLoading.value = true
  try {
    const created = await createRole(createName.value.trim(), createDescription.value.trim() || null)
    roles.value = [...roles.value, created].sort((a, b) => a.name.localeCompare(b.name))
    showCreateModal.value = false
  } catch (e: any) {
    createError.value = e?.message || 'Failed to create role'
  } finally {
    createLoading.value = false
  }
}

// ── Delete role ──
function openDelete(role: Role) {
  deleteTarget.value = role
  deleteError.value = ''
}

function closeDelete() {
  deleteTarget.value = null
  deleteError.value = ''
}

async function handleDelete() {
  if (!deleteTarget.value) return
  deleteLoading.value = true
  deleteError.value = ''
  try {
    await deleteRole(deleteTarget.value.id)
    roles.value = roles.value.filter(r => r.id !== deleteTarget.value!.id)
    closeDelete()
  } catch (e: any) {
    deleteError.value = e?.message || e?.error || 'Failed to delete role'
  } finally {
    deleteLoading.value = false
  }
}

// ── Permissions modal ──
async function openPerms(role: Role) {
  selectedRole.value = role
  showPermsModal.value = true
  editPerms.value = false
  saveSuccess.value = false
  saveError.value = ''
  permsLoading.value = true
  permsError.value = ''
  rolePerms.value = new Set()
  try {
    const data = await getRolePermissions(role.id)
    rolePerms.value = new Set(data.map(p => p.code))
  } catch (e: any) {
    permsError.value = e?.message || 'Failed to load permissions'
  } finally {
    permsLoading.value = false
  }
}

function closePerms() {
  showPermsModal.value = false
  selectedRole.value = null
  editPerms.value = false
  pendingCodes.value = new Set()
  saveError.value = ''
  saveSuccess.value = false
}

function enterEdit() {
  pendingCodes.value = new Set(rolePerms.value)
  saveError.value = ''
  saveSuccess.value = false
  editPerms.value = true
}

function exitEdit() {
  editPerms.value = false
  pendingCodes.value = new Set()
  saveError.value = ''
}

function toggleCode(code: string) {
  const next = new Set(pendingCodes.value)
  if (next.has(code)) next.delete(code)
  else next.add(code)
  pendingCodes.value = next
}

const changesCount = computed(() => {
  if (!editPerms.value) return 0
  const orig = rolePerms.value
  let diff = 0
  for (const c of pendingCodes.value) if (!orig.has(c)) diff++
  for (const c of orig) if (!pendingCodes.value.has(c)) diff++
  return diff
})

/** Chip styling: selected+valid green, selected+invalid red, unselected neutral. */
function permChipClass(code: string): string {
  if (pendingCodes.value.has(code)) {
    if (invalidCodes.value.has(code)) {
      return 'bg-red-100 border-red-300 text-red-700 dark:bg-red-900/30 dark:border-red-600 dark:text-red-300 hover:bg-red-200 dark:hover:bg-red-800/50'
    }
    return 'bg-green-100 border-green-300 text-green-700 dark:bg-green-900/30 dark:border-green-600 dark:text-green-300 hover:bg-green-200 dark:hover:bg-green-800/50'
  }
  return 'bg-transparent border-gray-200 text-gray-400 dark:border-gray-700 dark:text-gray-500 hover:bg-gray-50 dark:hover:bg-gray-800 hover:text-gray-600 dark:hover:text-gray-300'
}

async function savePerms() {
  if (!selectedRole.value) return
  saveLoading.value = true
  saveError.value = ''
  try {
    await setRolePermissions(selectedRole.value.id, [...pendingCodes.value])
    rolePerms.value = new Set(pendingCodes.value)
    exitEdit()
    saveSuccess.value = true
  } catch (e: any) {
    saveError.value = e?.message || e?.error || 'Failed to save permissions'
  } finally {
    saveLoading.value = false
  }
}

// ── Computed ──
const groupedAllPermissions = computed(() => {
  const groups: Record<string, Permission[]> = {}
  for (const p of allPermissions.value) {
    const bucket = groups[p.group] ?? (groups[p.group] = [])
    bucket.push(p)
  }
  return groups
})

const groupedRolePerms = computed(() => {
  if (!rolePerms.value.size) return {}
  const groups: Record<string, string[]> = {}
  for (const code of rolePerms.value) {
    const prefix = code.split('.')[0] ?? ''
    const bucket = groups[prefix] ?? (groups[prefix] = [])
    bucket.push(code)
  }
  return groups
})

const roleBadgeClass = (name: string) => ({
  'bg-purple-100 text-purple-700 dark:bg-purple-900/30 dark:text-purple-300': name === 'Admin',
  'bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-300': name === 'Secretary',
  'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-300': name === 'Head Technician',
  'bg-orange-100 text-orange-700 dark:bg-orange-900/30 dark:text-orange-300': name === 'Field Technician',
  'bg-gray-100 text-gray-600 dark:bg-gray-800 dark:text-gray-400': !['Admin','Secretary','Head Technician','Field Technician'].includes(name),
})
</script>

<template>
  <div>
    <!-- ── Header ── -->
    <div class="flex items-center justify-between mb-6 flex-wrap gap-4">
      <div>
        <h1 class="text-2xl font-bold text-gray-800 dark:text-gray-100">Roles</h1>
        <p class="text-sm text-gray-500 dark:text-gray-400 mt-1">{{ roles.length }} roles configured</p>
      </div>
      <button
        @click="openCreate"
        class="px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white text-sm font-medium rounded-lg transition flex items-center gap-2 cursor-pointer"
      >
        <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
          <path stroke-linecap="round" stroke-linejoin="round" d="M12 4v16m8-8H4" />
        </svg>
        Add Role
      </button>
    </div>

    <!-- ── Loading ── -->
    <div v-if="loading" class="flex justify-center py-20">
      <div class="flex flex-col items-center gap-3">
        <div class="w-8 h-8 border-4 border-blue-500 border-t-transparent rounded-full animate-spin"></div>
        <p class="text-sm text-gray-400 dark:text-gray-500">Loading roles…</p>
      </div>
    </div>

    <!-- ── Error ── -->
    <div v-else-if="error" class="text-center py-20">
      <div class="bg-red-50 dark:bg-red-900/30 border border-red-200 dark:border-red-800 rounded-xl p-6 max-w-md mx-auto">
        <p class="text-red-600 dark:text-red-400 font-medium">{{ error }}</p>
        <button @click="fetchRoles" class="mt-3 px-4 py-2 bg-red-600 hover:bg-red-700 text-white text-sm rounded-lg transition cursor-pointer">Retry</button>
      </div>
    </div>

    <!-- ── Roles grid ── -->
    <div v-else class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
      <div
        v-for="role in roles"
        :key="role.id"
        class="bg-white dark:bg-gray-900 rounded-xl border border-gray-100 dark:border-gray-800 shadow-sm hover:shadow-md transition-shadow p-5 flex flex-col gap-4"
      >
        <!-- Card header -->
        <div class="flex items-start justify-between gap-3">
          <div class="flex items-center gap-3">
            <!-- Role avatar -->
            <div class="w-10 h-10 rounded-full flex items-center justify-center text-sm font-bold flex-shrink-0"
              :class="roleBadgeClass(role.name)"
            >
              {{ role.name.charAt(0).toUpperCase() }}
            </div>
            <div>
              <p class="font-semibold text-gray-800 dark:text-gray-100 leading-tight">{{ role.name }}</p>
              <p v-if="role.description" class="text-xs text-gray-400 dark:text-gray-500 mt-0.5 leading-snug">{{ role.description }}</p>
            </div>
          </div>
          <!-- System role lock badge -->
          <span
            v-if="role.isSystemRole"
            class="flex-shrink-0 flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-medium bg-amber-100 text-amber-700 dark:bg-amber-900/30 dark:text-amber-300"
            title="System roles cannot be deleted"
          >
            <svg class="w-3 h-3" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
              <path stroke-linecap="round" stroke-linejoin="round" d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z" />
            </svg>
            System
          </span>
        </div>

        <!-- Manage button -->
        <button
          @click="openPerms(role)"
          class="mt-auto w-full py-2 text-sm font-medium text-blue-600 dark:text-blue-400 border border-blue-200 dark:border-blue-800 rounded-lg hover:bg-blue-50 dark:hover:bg-blue-900/20 transition cursor-pointer"
        >
          Manage Permissions
        </button>

        <!-- Delete button -->
        <button
          v-if="!role.isSystemRole"
          @click="openDelete(role)"
          class="w-full py-2 text-sm font-medium text-red-600 dark:text-red-400 border border-red-200 dark:border-red-800/60 rounded-lg hover:bg-red-50 dark:hover:bg-red-900/20 transition cursor-pointer flex items-center justify-center gap-1.5"
        >
          <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
            <path stroke-linecap="round" stroke-linejoin="round" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6M9 7V4a1 1 0 011-1h4a1 1 0 011 1v3M4 7h16" />
          </svg>
          Delete
        </button>
      </div>

      <!-- Empty state -->
      <div v-if="roles.length === 0" class="col-span-full text-center py-16">
        <p class="text-gray-400 dark:text-gray-500">No roles found</p>
      </div>
    </div>

    <!-- ══════════════════════════════════════
         Create Role Modal
    ══════════════════════════════════════ -->
    <Teleport to="body">
      <div v-if="showCreateModal" class="fixed inset-0 z-50 flex items-center justify-center p-4" @click.self="showCreateModal = false">
        <div class="absolute inset-0 bg-black/50 backdrop-blur-sm" @click="showCreateModal = false"></div>
        <div class="relative bg-white dark:bg-gray-900 rounded-2xl shadow-2xl w-full max-w-md animate-modal-in p-6">
          <div class="flex items-center justify-between mb-5">
            <h2 class="text-lg font-bold text-gray-800 dark:text-gray-100">Add Role</h2>
            <button @click="showCreateModal = false" class="w-7 h-7 rounded-full bg-gray-100 dark:bg-gray-800 flex items-center justify-center text-gray-400 hover:bg-gray-200 dark:hover:bg-gray-700 cursor-pointer text-sm">✕</button>
          </div>

          <form @submit.prevent="handleCreate" class="space-y-4">
            <div>
              <div class="flex items-center gap-1.5 mb-1">
                <label class="text-sm font-medium text-gray-700 dark:text-gray-300">Role Name *</label>
                <FieldTip text="The name of the role, shown when assigning users. Must be unique." />
              </div>
              <input
                v-model="createName"
                type="text"
                placeholder="e.g. Billing Manager"
                class="w-full px-3 py-2 rounded-lg border text-sm focus:ring-2 focus:ring-blue-500 outline-none transition"
                :class="createNameError ? 'border-red-400' : 'border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100'"
              />
              <p v-if="createNameError" class="text-xs text-red-500 mt-1">{{ createNameError }}</p>
            </div>

            <div>
              <div class="flex items-center gap-1.5 mb-1">
                <label class="text-sm font-medium text-gray-700 dark:text-gray-300">Description</label>
                <FieldTip text="Short description of what this role is for, e.g. handles billing and support." />
              </div>
              <input
                v-model="createDescription"
                type="text"
                placeholder="Short description of this role"
                class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 text-sm focus:ring-2 focus:ring-blue-500 outline-none transition"
              />
            </div>

            <p v-if="createError" class="text-sm text-red-600 dark:text-red-400 bg-red-50 dark:bg-red-900/20 rounded-lg px-3 py-2">{{ createError }}</p>

            <div class="flex gap-3 pt-1">
              <button type="button" @click="showCreateModal = false"
                class="flex-1 px-4 py-2.5 text-sm font-medium text-gray-600 dark:text-gray-300 bg-gray-100 dark:bg-gray-800 hover:bg-gray-200 dark:hover:bg-gray-700 rounded-lg transition cursor-pointer">
                Cancel
              </button>
              <button type="submit" :disabled="createLoading"
                class="flex-1 px-4 py-2.5 text-sm font-medium text-white bg-blue-600 hover:bg-blue-700 disabled:bg-blue-400 rounded-lg transition flex items-center justify-center gap-2 cursor-pointer">
                <span v-if="createLoading" class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
                {{ createLoading ? 'Creating…' : 'Create Role' }}
              </button>
            </div>
          </form>
        </div>
      </div>
    </Teleport>

    <!-- ══════════════════════════════════════
         Permissions Modal
    ══════════════════════════════════════ -->
    <Teleport to="body">
      <div v-if="showPermsModal" class="fixed inset-0 z-50 flex items-center justify-center p-4" @click.self="closePerms">
        <div class="absolute inset-0 bg-black/50 backdrop-blur-sm" @click="closePerms"></div>
        <div
          class="relative bg-white dark:bg-gray-900 rounded-2xl shadow-2xl w-full animate-modal-in flex flex-col"
          :class="editPerms ? 'max-w-2xl max-h-[92vh]' : 'max-w-lg max-h-[88vh]'"
        >
          <!-- Sticky header -->
          <div class="flex-shrink-0 flex items-center justify-between px-6 pt-5 pb-4 border-b border-gray-100 dark:border-gray-800">
            <div>
              <h2 class="text-lg font-bold text-gray-800 dark:text-gray-100">
                {{ editPerms ? 'Edit Permissions' : 'Role Permissions' }}
              </h2>
              <p v-if="selectedRole" class="text-xs text-gray-400 dark:text-gray-500 mt-0.5 flex items-center gap-1.5">
                <span>{{ selectedRole.name }}</span>
                <span v-if="selectedRole.isSystemRole" class="px-1.5 py-0.5 rounded-full bg-amber-100 text-amber-700 dark:bg-amber-900/30 dark:text-amber-300 text-[10px] font-medium">System</span>
              </p>
            </div>
            <div class="flex items-center gap-2">
              <template v-if="editPerms">
                <span
                  v-if="changesCount > 0"
                  class="text-xs font-medium px-2 py-1 rounded-full bg-blue-100 text-blue-700 dark:bg-blue-900/40 dark:text-blue-300"
                >{{ changesCount }} change{{ changesCount !== 1 ? 's' : '' }}</span>
                <button @click="exitEdit"
                  class="px-3 py-1.5 text-sm font-medium text-gray-600 dark:text-gray-300 bg-gray-100 dark:bg-gray-800 hover:bg-gray-200 dark:hover:bg-gray-700 rounded-lg transition cursor-pointer">
                  Cancel
                </button>
                <button @click="savePerms" :disabled="saveLoading || changesCount === 0 || invalidCodes.size > 0"
                  class="px-3 py-1.5 text-sm font-medium text-white bg-blue-600 hover:bg-blue-700 disabled:bg-blue-400 disabled:cursor-not-allowed rounded-lg transition flex items-center gap-1.5 cursor-pointer">
                  <span v-if="saveLoading" class="w-3.5 h-3.5 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
                  {{ saveLoading ? 'Saving…' : 'Save' }}
                </button>
              </template>
              <button @click="closePerms" class="w-7 h-7 rounded-full bg-gray-100 dark:bg-gray-800 flex items-center justify-center text-gray-400 hover:bg-gray-200 dark:hover:bg-gray-700 cursor-pointer text-sm ml-1">✕</button>
            </div>
          </div>

          <!-- Scrollable body -->
          <div class="overflow-y-auto flex-1 px-6 pb-6">

            <!-- Loading -->
            <div v-if="permsLoading" class="flex justify-center py-12">
              <div class="w-8 h-8 border-4 border-blue-500 border-t-transparent rounded-full animate-spin"></div>
            </div>

            <!-- Error loading perms -->
            <div v-else-if="permsError" class="py-8 text-center">
              <p class="text-red-500 dark:text-red-400 text-sm">{{ permsError }}</p>
              <button @click="selectedRole && openPerms(selectedRole)" class="mt-2 text-sm text-blue-500 hover:underline cursor-pointer">Retry</button>
            </div>

            <div v-else>

              <!-- ════ VIEW MODE ════ -->
              <div v-if="!editPerms" class="space-y-5 pt-4">

                <!-- Success banner -->
                <div
                  v-if="saveSuccess"
                  class="flex items-center gap-2.5 px-4 py-3 rounded-xl bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 text-green-700 dark:text-green-300 text-sm"
                >
                  <svg class="w-4 h-4 flex-shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2.5">
                    <path stroke-linecap="round" stroke-linejoin="round" d="M5 13l4 4L19 7" />
                  </svg>
                  Permissions updated successfully.
                </div>

                <!-- Empty -->
                <div v-if="rolePerms.size === 0" class="text-center py-8">
                  <p class="text-gray-400 dark:text-gray-500 text-sm mb-3">This role has no permissions assigned.</p>
                  <button
                    v-if="allPermissions.length > 0"
                    @click="enterEdit"
                    class="px-4 py-2 text-sm font-medium text-blue-600 dark:text-blue-400 border border-blue-200 dark:border-blue-800 rounded-lg hover:bg-blue-50 dark:hover:bg-blue-900/20 transition cursor-pointer"
                  >Assign Permissions</button>
                </div>

                <!-- Permission chips grouped -->
                <div v-else class="space-y-4">
                  <div class="flex items-center justify-between">
                    <p class="text-sm font-semibold text-gray-700 dark:text-gray-300 flex items-center gap-1.5">
                      Assigned Permissions
                      <span class="text-xs font-normal text-gray-400">{{ rolePerms.size }} total</span>
                      <FieldTip text="The permissions this role grants. They control what users with this role can see and do across the system." />
                    </p>
                    <button
                      v-if="allPermissions.length > 0"
                      @click="enterEdit"
                      class="text-xs font-medium px-2.5 py-1 rounded-lg bg-blue-50 text-blue-600 hover:bg-blue-100 dark:bg-blue-900/20 dark:text-blue-400 dark:hover:bg-blue-900/40 transition cursor-pointer"
                    >Edit</button>
                  </div>
                  <div v-for="(codes, group) in groupedRolePerms" :key="group">
                    <p class="text-xs font-semibold uppercase tracking-wider text-gray-400 dark:text-gray-500 mb-1.5">{{ group }}</p>
                    <div class="flex flex-wrap gap-1.5">
                      <span
                        v-for="code in codes"
                        :key="code"
                        class="px-2 py-0.5 rounded-full text-xs font-mono bg-green-50 border border-green-200 text-green-700 dark:bg-green-900/20 dark:border-green-700 dark:text-green-300"
                      >{{ code.split('.')[1] }}</span>
                    </div>
                  </div>
                </div>
              </div>

              <!-- ════ EDIT MODE ════ -->
              <div v-else class="space-y-5 pt-4">

                <!-- Legend -->
                <div class="rounded-xl bg-gray-50 dark:bg-gray-800/50 border border-gray-100 dark:border-gray-800 px-4 py-3">
                  <p class="text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wide mb-2 flex items-center gap-1.5">
                    Click any permission to toggle it
                    <FieldTip text="Permissions are grouped by module. Green chips are assigned to this role, grey ones are not. Hover any chip for its description." />
                  </p>
                  <div class="flex flex-wrap gap-x-4 gap-y-1.5 text-xs text-gray-600 dark:text-gray-400">
                    <span class="flex items-center gap-1.5"><span class="w-2.5 h-2.5 rounded-full bg-green-400"></span>Assigned to role</span>
                    <span class="flex items-center gap-1.5"><span class="w-2.5 h-2.5 rounded-full bg-gray-300 dark:bg-gray-600"></span>Not assigned</span>
                  </div>
                </div>

                <!-- Invalid permissions warning (view-first rule) -->
                <div
                  v-if="invalidCodes.size > 0"
                  class="rounded-xl bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 px-4 py-3"
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

                <!-- Permission toggles grouped -->
                <div class="space-y-4">
                  <div v-for="(perms, group) in groupedAllPermissions" :key="group">
                    <p class="text-xs font-semibold uppercase tracking-wider text-gray-400 dark:text-gray-500 mb-2">{{ group }}</p>
                    <div class="flex flex-wrap gap-1.5">
                      <button
                        v-for="perm in perms"
                        :key="perm.code"
                        @click="toggleCode(perm.code)"
                        :title="perm.description"
                        class="px-2.5 py-1 rounded-full text-xs font-medium border transition-all duration-150 cursor-pointer select-none flex items-center gap-1"
                        :class="permChipClass(perm.code)"
                      >
                        <span class="opacity-80 text-[10px] leading-none">{{ pendingCodes.has(perm.code) ? '✓' : '+' }}</span>
                        {{ perm.code.split('.')[1] }}
                      </button>
                    </div>
                  </div>
                </div>

                <!-- Save error -->
                <p v-if="saveError" class="text-sm text-red-600 dark:text-red-400 bg-red-50 dark:bg-red-900/20 rounded-xl px-4 py-3 border border-red-200 dark:border-red-800">
                  {{ saveError }}
                </p>

                <!-- Bottom bar -->
                <div class="flex items-center justify-between pt-1 border-t border-gray-100 dark:border-gray-800">
                  <p class="text-xs text-gray-400 dark:text-gray-500">
                    <span v-if="invalidCodes.size > 0">{{ invalidCodes.size }} invalid permission{{ invalidCodes.size === 1 ? '' : 's' }} (need view)</span>
                    <span v-else-if="changesCount === 0">No changes yet</span>
                    <span v-else class="text-blue-600 dark:text-blue-400 font-medium">{{ changesCount }} pending change{{ changesCount !== 1 ? 's' : '' }}</span>
                  </p>
                  <div class="flex gap-2">
                    <button @click="exitEdit"
                      class="px-4 py-2 text-sm font-medium text-gray-600 dark:text-gray-300 bg-gray-100 dark:bg-gray-800 hover:bg-gray-200 dark:hover:bg-gray-700 rounded-lg transition cursor-pointer">
                      Cancel
                    </button>
                    <button @click="savePerms" :disabled="saveLoading || changesCount === 0 || invalidCodes.size > 0"
                      class="px-4 py-2 text-sm font-medium text-white bg-blue-600 hover:bg-blue-700 disabled:bg-blue-400 disabled:cursor-not-allowed rounded-lg transition flex items-center gap-1.5 cursor-pointer">
                      <span v-if="saveLoading" class="w-3.5 h-3.5 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
                      {{ saveLoading ? 'Saving…' : 'Save Changes' }}
                    </button>
                  </div>
                </div>
              </div>

            </div>
          </div>
        </div>
      </div>
    </Teleport>

    <!-- ══════════════════════════════════════
         Delete Role Confirmation Modal
    ══════════════════════════════════════ -->
    <Teleport to="body">
      <div v-if="deleteTarget" class="fixed inset-0 z-50 flex items-center justify-center p-4" @click.self="closeDelete">
        <div class="absolute inset-0 bg-black/50 backdrop-blur-sm" @click="closeDelete"></div>
        <div class="relative bg-white dark:bg-gray-900 rounded-2xl shadow-2xl w-full max-w-md animate-modal-in p-6">
          <div class="flex items-start gap-4">
            <div class="w-10 h-10 rounded-full bg-red-100 dark:bg-red-900/30 flex items-center justify-center flex-shrink-0">
              <svg class="w-5 h-5 text-red-600 dark:text-red-400" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                <path stroke-linecap="round" stroke-linejoin="round" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
              </svg>
            </div>
            <div class="flex-1 min-w-0">
              <h2 class="text-lg font-bold text-gray-800 dark:text-gray-100">Delete Role</h2>
              <p class="text-sm text-gray-500 dark:text-gray-400 mt-1">
                Are you sure you want to delete
                <span class="font-semibold text-gray-700 dark:text-gray-300">{{ deleteTarget.name }}</span>?
                This action cannot be undone.
              </p>
            </div>
            <button @click="closeDelete" class="w-7 h-7 rounded-full bg-gray-100 dark:bg-gray-800 flex items-center justify-center text-gray-400 hover:bg-gray-200 dark:hover:bg-gray-700 cursor-pointer text-sm flex-shrink-0">✕</button>
          </div>

          <p v-if="deleteError" class="mt-4 text-sm text-red-600 dark:text-red-400 bg-red-50 dark:bg-red-900/20 rounded-lg px-3 py-2">{{ deleteError }}</p>

          <div class="flex gap-3 mt-6">
            <button type="button" @click="closeDelete" :disabled="deleteLoading"
              class="flex-1 px-4 py-2.5 text-sm font-medium text-gray-600 dark:text-gray-300 bg-gray-100 dark:bg-gray-800 hover:bg-gray-200 dark:hover:bg-gray-700 rounded-lg transition cursor-pointer disabled:opacity-50">
              Cancel
            </button>
            <button type="button" @click="handleDelete" :disabled="deleteLoading"
              class="flex-1 px-4 py-2.5 text-sm font-medium text-white bg-red-600 hover:bg-red-700 disabled:bg-red-400 rounded-lg transition flex items-center justify-center gap-2 cursor-pointer">
              <span v-if="deleteLoading" class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
              {{ deleteLoading ? 'Deleting…' : 'Delete Role' }}
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
  to   { opacity: 1; transform: scale(1)    translateY(0);    }
}
.animate-modal-in { animation: modalIn 0.2s ease-out forwards; }
</style>
