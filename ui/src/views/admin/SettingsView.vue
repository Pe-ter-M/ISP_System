<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { getSettings, createSetting, updateSetting, deleteSetting } from '@/services/settings.service'
import { getPaymentMethods } from '@/services/subscription.service'
import { useToastStore } from '@/stores/toast.store'
import { useSettingsStore } from '@/stores/settings.store'
import FieldTip from '@/components/FieldTip.vue'
import type { SettingItem } from '@/types/settings.types'
import type { CreateSettingPayload, UpdateSettingPayload } from '@/types/settings.types'
import type { PaymentMethodOption } from '@/types/subscription.types'

const toast = useToastStore()
/** Cached public settings used across the site — refresh so edits apply immediately */
const settingsStore = useSettingsStore()

// ── State ──
const settings = ref<SettingItem[]>([])
const loading = ref(true)
const error = ref<string | null>(null)

// Create modal
const showCreateModal = ref(false)
const createLoading = ref(false)
const createError = ref('')
const createValidation = ref<Record<string, string>>({})
const createForm = ref({ key: '', value: '', description: '' })
/** Known-key dropdown choice in the add modal ('custom' opens a free-text input) */
const keyChoice = ref('')

// Edit modal
const showEditModal = ref(false)
const editSaving = ref(false)
const editError = ref('')
const editValidation = ref<Record<string, string>>({})
const editingKey = ref<string | null>(null)
const editForm = ref({ value: '', description: '' })

// Delete modal
const showDelete = ref(false)
const deleteTarget = ref<SettingItem | null>(null)
const deleteLoading = ref(false)
const deleteError = ref('')

// Payment method multi-select (only meaningful for the payment_methods key)
const gateways = ref<PaymentMethodOption[]>([])
const gatewaysLoading = ref(false)
const selectedPaymentMethods = ref<string[]>([])

// Common settings keys — the dropdown of items that can be added
const commonKeys = [
  'company_name', 'company_short_name', 'company_phone', 'company_email', 'company_address',
  'business_hours', 'business_days', 'currency', 'payment_methods',
   'sms_balance_threshold',
]

// ── Fetch ──
async function fetchSettings() {
  loading.value = true
  error.value = null
  try {
    settings.value = await getSettings()
  } catch (e: unknown) {
    error.value = errMsg(e, 'Failed to load settings')
  } finally {
    loading.value = false
  }
}

onMounted(() => {
  fetchSettings()
  loadGateways()
})

/** Registered gateways (Mock, Mpesa, Airtel) — used for the payment_methods multi-select */
async function loadGateways() {
  if (gateways.value.length > 0) return
  gatewaysLoading.value = true
  try {
    gateways.value = await getPaymentMethods()
  } catch {
    gateways.value = []
  } finally {
    gatewaysLoading.value = false
  }
}

/** Accept a JSON array or comma-separated list (mirrors the backend parser) */
function parsePayMethods(raw: string): string[] {
  const trimmed = raw.trim()
  if (!trimmed) return []
  if (trimmed.startsWith('[')) {
    try {
      const parsed: unknown = JSON.parse(trimmed)
      if (Array.isArray(parsed)) return parsed.filter((x): x is string => typeof x === 'string')
    } catch {
      // fall through to comma parsing
    }
  }
  return trimmed.split(',').map(s => s.trim()).filter(Boolean)
}

function toggleGateway(code: string) {
  const idx = selectedPaymentMethods.value.indexOf(code)
  if (idx >= 0) selectedPaymentMethods.value.splice(idx, 1)
  else selectedPaymentMethods.value.push(code)
}

function onKeyChoiceChange() {
  if (keyChoice.value !== 'custom') createForm.value.key = keyChoice.value
  if (keyChoice.value === 'payment_methods') {
    selectedPaymentMethods.value = gateways.value.map(g => g.value)
  }
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

function friendlyDate(iso: string | null | undefined): string {
  if (!iso) return '—'
  const d = new Date(iso)
  if (Number.isNaN(d.getTime())) return '—'
  return d.toLocaleString()
}

function displayValue(s: SettingItem): string {
  if (s.isEncrypted) return '•••••••• (encrypted)'
  return s.value || '—'
}

// ── Create ──
function openCreate() {
  createForm.value = { key: '', value: '', description: '' }
  keyChoice.value = ''
  createError.value = ''
  createValidation.value = {}
  showCreateModal.value = true
  loadGateways()
}

function validateCreate(): boolean {
  const v: Record<string, string> = {}
  if (!createForm.value.key.trim()) v.key = 'Key is required'
  else if (!/^[a-z][a-z0-9_]*$/.test(createForm.value.key.trim())) v.key = 'Lowercase letters, numbers and underscores only (e.g. company_name)'
  else if (settings.value.some(s => s.key === createForm.value.key.trim())) v.key = 'This key already exists'
  if (createForm.value.key === 'payment_methods' && selectedPaymentMethods.value.length === 0) v.value = 'Select at least one payment method'
  else if (!createForm.value.value.trim()) v.value = 'Value is required'
  createValidation.value = v
  return Object.keys(v).length === 0
}

async function handleCreate() {
  if (!validateCreate()) return
  createLoading.value = true
  createError.value = ''
  const isPayMethods = createForm.value.key === 'payment_methods'
  const payload: CreateSettingPayload = {
    key: createForm.value.key.trim(),
    value: isPayMethods ? JSON.stringify(selectedPaymentMethods.value) : createForm.value.value,
    description: createForm.value.description.trim() || null,
  }
  try {
    const created = await createSetting(payload)
    settings.value.push(created)
    settings.value.sort((a, b) => a.key.localeCompare(b.key))
    showCreateModal.value = false
    settingsStore.refresh()
    toast.success(`Setting "${created.key}" created`)
  } catch (e: unknown) {
    createError.value = errMsg(e, 'Failed to create setting')
    toast.error(createError.value)
  } finally {
    createLoading.value = false
  }
}

// ── Edit ──
function openEdit(s: SettingItem) {
  editingKey.value = s.key
  editForm.value = { value: s.isEncrypted ? '' : s.value, description: s.description ?? '' }
  if (s.key === 'payment_methods') {
    const parsed = parsePayMethods(s.value)
    selectedPaymentMethods.value = parsed.length > 0 ? parsed : gateways.value.map(g => g.value)
  }
  editError.value = ''
  editValidation.value = {}
  showEditModal.value = true
  loadGateways()
}

function closeEdit() {
  if (editSaving.value) return
  showEditModal.value = false
  editingKey.value = null
}

function validateEdit(): boolean {
  const v: Record<string, string> = {}
  if (editingKey.value === 'payment_methods' && selectedPaymentMethods.value.length === 0) v.value = 'Select at least one payment method'
  else if (!editForm.value.value.trim()) v.value = 'Value is required'
  editValidation.value = v
  return Object.keys(v).length === 0
}

async function handleEdit() {
  if (editingKey.value === null) return
  if (!validateEdit()) return
  editSaving.value = true
  editError.value = ''
  const isPayMethods = editingKey.value === 'payment_methods'
  const payload: UpdateSettingPayload = {
    value: isPayMethods ? JSON.stringify(selectedPaymentMethods.value) : editForm.value.value,
    description: editForm.value.description.trim() || null,
  }
  try {
    const updated = await updateSetting(editingKey.value, payload)
    const idx = settings.value.findIndex(s => s.key === updated.key)
    if (idx !== -1) settings.value[idx] = updated
    showEditModal.value = false
    editingKey.value = null
    settingsStore.refresh()
    toast.success(`Setting "${updated.key}" updated`)
  } catch (e: unknown) {
    editError.value = errMsg(e, 'Failed to update setting')
    toast.error(editError.value)
  } finally {
    editSaving.value = false
  }
}

// ── Delete ──
function openDelete(s: SettingItem) {
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
    await deleteSetting(target.key)
    settings.value = settings.value.filter(s => s.key !== target.key)
    showDelete.value = false
    deleteTarget.value = null
    settingsStore.refresh()
    toast.success(`Setting "${target.key}" deleted`)
  } catch (e: unknown) {
    deleteError.value = errMsg(e, 'Failed to delete setting')
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
        <h1 class="text-2xl font-bold text-gray-800 dark:text-gray-100">Settings</h1>
        <p class="text-sm text-gray-500 dark:text-gray-400 mt-1">{{ settings.length }} system settings</p>
      </div>
      <button @click="openCreate"
        class="px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white text-sm font-medium rounded-lg transition-all duration-200 cursor-pointer flex items-center gap-2 whitespace-nowrap">
        <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
          <path stroke-linecap="round" stroke-linejoin="round" d="M12 4v16m8-8H4" />
        </svg>
        Add Setting
      </button>
    </div>

    <!-- ── Info note ── -->
    <div class="mb-6 rounded-xl border border-blue-200 dark:border-blue-800/60 bg-blue-50 dark:bg-blue-900/20 px-4 py-3 flex items-start gap-3">
      <svg class="w-4 h-4 text-blue-600 dark:text-blue-400 mt-0.5 flex-shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
        <path stroke-linecap="round" stroke-linejoin="round" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
      </svg>
      <p class="text-sm text-blue-800 dark:text-blue-300">
        Settings are key-value pairs consumed by the public site and the rest of the system — they are how a different ISP customises the platform.
        <span class="font-semibold">company_name</span> and the other <span class="font-semibold">company_*</span> keys override the brand name, contact details and currency shown across the webapp;
        <span class="font-semibold">payment_methods</span> (e.g. ["Mpesa","Airtel"]) controls which payment methods appear on the subscription page.
        Values marked encrypted are stored masked and never displayed.
      </p>
    </div>

    <!-- ── Loading ── -->
    <div v-if="loading && settings.length === 0" class="flex justify-center py-20">
      <div class="flex flex-col items-center gap-3">
        <div class="w-8 h-8 border-4 border-blue-500 border-t-transparent rounded-full animate-spin"></div>
        <p class="text-sm text-gray-400 dark:text-gray-500">Loading settings...</p>
      </div>
    </div>

    <!-- ── Error ── -->
    <div v-else-if="error && settings.length === 0" class="text-center py-20">
      <div class="bg-red-50 dark:bg-red-900/30 border border-red-200 dark:border-red-800 rounded-xl p-6 max-w-md mx-auto">
        <p class="text-red-600 dark:text-red-400 font-medium">{{ error }}</p>
        <button @click="fetchSettings" class="mt-3 px-4 py-2 bg-red-600 hover:bg-red-700 text-white text-sm rounded-lg transition cursor-pointer">Retry</button>
      </div>
    </div>

    <!-- ── Table ── -->
    <div v-else class="bg-white dark:bg-gray-900 rounded-xl shadow-sm border border-gray-100 dark:border-gray-800 overflow-hidden">
      <div class="overflow-x-auto">
        <table class="w-full text-sm">
          <thead>
            <tr class="border-b border-gray-100 dark:border-gray-800 bg-gray-50 dark:bg-gray-800/50">
              <th class="px-4 py-3 text-left font-semibold text-gray-600 dark:text-gray-400 whitespace-nowrap">Setting</th>
              <th class="px-4 py-3 text-left font-semibold text-gray-600 dark:text-gray-400 whitespace-nowrap">Value</th>
              <th class="px-4 py-3 text-left font-semibold text-gray-600 dark:text-gray-400 whitespace-nowrap hidden md:table-cell">Updated</th>
              <th class="px-4 py-3 text-right font-semibold text-gray-600 dark:text-gray-400 whitespace-nowrap">Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="s in settings" :key="s.key"
              class="border-b border-gray-50 dark:border-gray-800/50 hover:bg-gray-50 dark:hover:bg-gray-800/30 transition">
              <td class="px-4 py-3">
                <div class="flex items-center gap-2">
                  <p class="font-medium text-gray-800 dark:text-gray-200 font-mono text-sm">{{ s.key }}</p>
                  <span v-if="s.isEncrypted" class="px-1.5 py-0.5 rounded bg-gray-100 dark:bg-gray-800 text-gray-400 dark:text-gray-500 text-[10px] font-medium uppercase">Encrypted</span>
                </div>
                <p v-if="s.description" class="text-xs text-gray-400 dark:text-gray-500 mt-0.5 max-w-[380px]">{{ s.description }}</p>
              </td>
              <td class="px-4 py-3 text-gray-700 dark:text-gray-300 break-all max-w-[280px]">{{ displayValue(s) }}</td>
              <td class="px-4 py-3 text-gray-500 dark:text-gray-400 hidden md:table-cell whitespace-nowrap">{{ friendlyDate(s.updatedAt) }}</td>
              <td class="px-4 py-3 text-right">
                <div class="flex items-center justify-end gap-1.5">
                  <button @click="openEdit(s)"
                    class="px-3 py-1.5 text-xs font-medium text-indigo-600 dark:text-indigo-400 bg-indigo-50 dark:bg-indigo-900/20 hover:bg-indigo-100 dark:hover:bg-indigo-900/40 rounded-lg transition cursor-pointer">
                    Edit
                  </button>
                  <button @click="openDelete(s)"
                    class="px-3 py-1.5 text-xs font-medium text-red-600 dark:text-red-400 bg-red-50 dark:bg-red-900/20 hover:bg-red-100 dark:hover:bg-red-900/40 rounded-lg transition cursor-pointer">
                    Delete
                  </button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <div v-if="settings.length === 0 && !loading && !error" class="text-center py-16">
        <p class="text-gray-400 dark:text-gray-500">No settings yet</p>
        <button @click="openCreate" class="mt-2 text-xs text-blue-600 dark:text-blue-400 hover:underline cursor-pointer">Add the first setting</button>
      </div>
    </div>

    <!-- ── Create Setting Modal ── -->
    <Teleport to="body">
      <div v-if="showCreateModal" class="fixed inset-0 z-50 flex items-center justify-center p-4" @click.self="showCreateModal = false">
        <div class="absolute inset-0 bg-black/50 backdrop-blur-sm" @click="showCreateModal = false"></div>
        <div class="relative bg-white dark:bg-gray-900 rounded-2xl shadow-2xl w-full max-w-lg max-h-[90vh] overflow-y-auto animate-modal-in p-6">
          <div class="flex items-center justify-between mb-5">
            <h2 class="text-lg font-bold text-gray-800 dark:text-gray-100">Add Setting</h2>
            <button @click="showCreateModal = false" :disabled="createLoading"
              class="w-7 h-7 rounded-full bg-gray-100 dark:bg-gray-800 flex items-center justify-center text-gray-400 hover:bg-gray-200 dark:hover:bg-gray-700 cursor-pointer text-sm disabled:opacity-40">✕</button>
          </div>

          <form @submit.prevent="handleCreate" class="space-y-4">
            <div>
              <div class="flex items-center gap-1.5 mb-1">
                <label class="text-sm font-medium text-gray-700 dark:text-gray-300">Key *</label>
                <FieldTip text="Unique lowercase identifier, e.g. company_name or payment_due_days. Used by the code to read this setting — cannot be changed later." />
              </div>
              <select v-model="keyChoice" @change="onKeyChoiceChange"
                class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 text-sm focus:ring-2 focus:ring-blue-500 outline-none transition cursor-pointer">
                <option value="" disabled>Select a setting…</option>
                <option v-for="k in commonKeys" :key="k" :value="k" class="font-mono">{{ k }}</option>
                <option value="custom">Custom key…</option>
              </select>
              <input v-if="keyChoice === 'custom'" v-model="createForm.key" type="text" placeholder="my_custom_key"
                class="mt-2 w-full px-3 py-2 rounded-lg border text-sm font-mono focus:ring-2 focus:ring-blue-500 outline-none transition"
                :class="createValidation.key ? 'border-red-400' : 'border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100'" />
              <p v-if="createValidation.key" class="text-xs text-red-500 mt-1">{{ createValidation.key }}</p>
              <p v-else class="text-xs text-gray-400 mt-1">Pick a known setting — these are the ones the public site and system read.</p>
            </div>

            <div>
              <div class="flex items-center gap-1.5 mb-1">
                <label class="text-sm font-medium text-gray-700 dark:text-gray-300">Value *</label>
                <FieldTip text="The value the system reads. Store numbers as plain digits (e.g. 30) and dates as ISO format (e.g. 2026-08-01)." />
              </div>
              <template v-if="createForm.key === 'payment_methods'">
                <div v-if="gatewaysLoading" class="text-xs text-gray-400 dark:text-gray-500 py-2">Loading payment methods…</div>
                <div v-else class="flex flex-wrap gap-2">
                  <button v-for="g in gateways" :key="g.value" type="button" @click="toggleGateway(g.value)"
                    class="px-3 py-1.5 rounded-full text-xs font-medium transition cursor-pointer"
                    :class="selectedPaymentMethods.includes(g.value)
                      ? 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-300 ring-1 ring-green-400/60'
                      : 'bg-gray-100 text-gray-500 dark:bg-gray-800 dark:text-gray-400 hover:bg-gray-200 dark:hover:bg-gray-700'">
                    {{ g.label }}
                  </button>
                </div>
                <p class="text-xs text-gray-400 dark:text-gray-500 mt-1.5 font-mono">{{ JSON.stringify(selectedPaymentMethods) }}</p>
              </template>
              <textarea v-else v-model="createForm.value" rows="2" placeholder="Wifi Connect ISP"
                class="w-full px-3 py-2 rounded-lg border text-sm focus:ring-2 focus:ring-blue-500 outline-none transition"
                :class="createValidation.value ? 'border-red-400' : 'border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100'" />
              <p v-if="createValidation.value" class="text-xs text-red-500 mt-1">{{ createValidation.value }}</p>
            </div>

            <div>
              <div class="flex items-center gap-1.5 mb-1">
                <label class="text-sm font-medium text-gray-700 dark:text-gray-300">Description</label>
                <FieldTip text="Explains what this setting controls. Shown on the settings page." />
              </div>
              <input v-model="createForm.description" type="text" placeholder="Company name shown on invoices"
                class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 text-sm focus:ring-2 focus:ring-blue-500 outline-none transition" />
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
                Create Setting
              </button>
            </div>
          </form>
        </div>
      </div>
    </Teleport>

    <!-- ── Edit Setting Modal ── -->
    <Teleport to="body">
      <div v-if="showEditModal" class="fixed inset-0 z-50 flex items-center justify-center p-4" @click.self="closeEdit">
        <div class="absolute inset-0 bg-black/50 backdrop-blur-sm" @click="closeEdit"></div>
        <div class="relative bg-white dark:bg-gray-900 rounded-2xl shadow-2xl w-full max-w-lg max-h-[90vh] overflow-y-auto animate-modal-in p-6">
          <div class="flex items-center justify-between mb-5">
            <div>
              <h2 class="text-lg font-bold text-gray-800 dark:text-gray-100">Edit Setting</h2>
              <p class="text-sm text-gray-500 dark:text-gray-400 font-mono mt-0.5">{{ editingKey }}</p>
            </div>
            <button @click="closeEdit" :disabled="editSaving"
              class="w-7 h-7 rounded-full bg-gray-100 dark:bg-gray-800 flex items-center justify-center text-gray-400 hover:bg-gray-200 dark:hover:bg-gray-700 cursor-pointer text-sm disabled:opacity-40">✕</button>
          </div>

          <form @submit.prevent="handleEdit" class="space-y-4">
            <div>
              <div class="flex items-center gap-1.5 mb-1">
                <label class="text-sm font-medium text-gray-700 dark:text-gray-300">Value *</label>
                <FieldTip text="The value the system reads. Store numbers as plain digits (e.g. 30) and dates as ISO format (e.g. 2026-08-01)." />
              </div>
              <template v-if="editingKey === 'payment_methods'">
                <div v-if="gatewaysLoading" class="text-xs text-gray-400 dark:text-gray-500 py-2">Loading payment methods…</div>
                <div v-else class="flex flex-wrap gap-2">
                  <button v-for="g in gateways" :key="g.value" type="button" @click="toggleGateway(g.value)"
                    class="px-3 py-1.5 rounded-full text-xs font-medium transition cursor-pointer"
                    :class="selectedPaymentMethods.includes(g.value)
                      ? 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-300 ring-1 ring-green-400/60'
                      : 'bg-gray-100 text-gray-500 dark:bg-gray-800 dark:text-gray-400 hover:bg-gray-200 dark:hover:bg-gray-700'">
                    {{ g.label }}
                  </button>
                </div>
                <p class="text-xs text-gray-400 dark:text-gray-500 mt-1.5 font-mono">{{ JSON.stringify(selectedPaymentMethods) }}</p>
              </template>
              <textarea v-else v-model="editForm.value" rows="2"
                class="w-full px-3 py-2 rounded-lg border text-sm focus:ring-2 focus:ring-blue-500 outline-none transition"
                :class="editValidation.value ? 'border-red-400' : 'border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100'" />
              <p v-if="editValidation.value" class="text-xs text-red-500 mt-1">{{ editValidation.value }}</p>
            </div>

            <div>
              <div class="flex items-center gap-1.5 mb-1">
                <label class="text-sm font-medium text-gray-700 dark:text-gray-300">Description</label>
                <FieldTip text="Explains what this setting controls. Shown on the settings page." />
              </div>
              <input v-model="editForm.description" type="text"
                class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 text-sm focus:ring-2 focus:ring-blue-500 outline-none transition" />
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
              <h2 class="text-lg font-bold text-gray-800 dark:text-gray-100">Delete Setting</h2>
              <p class="text-sm text-gray-500 dark:text-gray-400 mt-1">
                Are you sure you want to delete <span class="font-semibold text-gray-700 dark:text-gray-200 font-mono">{{ deleteTarget?.key }}</span>?
                Anything that reads this setting will fall back to its default. This cannot be undone.
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
              Delete Setting
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
