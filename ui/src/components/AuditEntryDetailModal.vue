<script setup lang="ts">
import { ref, watch } from 'vue'
import { getAuditLog } from '@/services/audit.service'
import { actionLabel, entityTypeLabel } from '@/services/audit.service'
import { formatDateTime } from '@/utils/format'
import type { AuditLogDetail } from '@/types/audit.types'

const props = defineProps<{
  open: boolean
  auditId: number | null
  zIndex?: number
}>()

const emit = defineEmits<{ (e: 'close'): void }>()

const detail = ref<AuditLogDetail | null>(null)
const loading = ref(false)
const error = ref('')
const showChanges = ref(true)

watch(
  () => props.open,
  async (open) => {
    if (open && props.auditId != null) {
      loading.value = true
      error.value = ''
      showChanges.value = true
      try {
        detail.value = await getAuditLog(props.auditId)
      } catch (e: unknown) {
        error.value = e && typeof e === 'object' && (e as { message?: string }).message
          ? String((e as { message: string }).message)
          : 'Failed to load audit entry'
      } finally {
        loading.value = false
      }
    }
  }
)

function close() {
  emit('close')
  detail.value = null
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

function actorLabel(): string {
  const a = detail.value?.actor
  if (!a) return 'Anonymous'
  if (a.type === 'customer') return 'Customer'
  if (a.type === 'staff') return 'Staff'
  if (a.type === 'anonymous') return 'Anonymous'
  return a.type
}
</script>

<template>
  <Teleport to="body">
    <div v-if="open" class="fixed inset-0 z-50 flex items-center justify-center p-4" :style="{ zIndex }" @click.self="close">
      <div class="absolute inset-0 bg-black/50 backdrop-blur-sm" @click="close"></div>
      <div class="relative bg-white dark:bg-gray-900 rounded-2xl shadow-2xl w-full max-w-xl max-h-[90vh] overflow-y-auto animate-modal-in p-6 sm:p-7">
        <div class="flex items-start justify-between gap-4 mb-5">
          <div>
            <div class="flex items-center gap-2">
              <h2 class="text-lg font-bold text-gray-800 dark:text-gray-100">Audit Entry</h2>
              <span :class="actionClass(detail?.action || '')" class="px-2 py-0.5 rounded-full text-xs font-medium capitalize">{{ actionLabel(detail?.action || '') }}</span>
            </div>
            <p class="text-xs text-gray-400 dark:text-gray-500 mt-1">
              {{ detail ? entityTypeLabel(detail.entityType) : '' }}
              <template v-if="detail?.entityId"> · #{{ detail.entityId }}</template>
            </p>
          </div>
          <button @click="close"
            class="w-7 h-7 rounded-full bg-gray-100 dark:bg-gray-800 flex items-center justify-center text-gray-400 hover:bg-gray-200 dark:hover:bg-gray-700 cursor-pointer text-sm">✕</button>
        </div>

        <!-- Loading -->
        <div v-if="loading" class="flex justify-center py-14">
          <div class="w-8 h-8 border-4 border-blue-500 border-t-transparent rounded-full animate-spin"></div>
        </div>

        <!-- Error -->
        <div v-else-if="error" class="text-center py-10">
          <p class="text-red-500 dark:text-red-400">{{ error }}</p>
          <button @click="close" class="mt-4 text-blue-600 dark:text-blue-400 hover:underline cursor-pointer">Close</button>
        </div>

        <div v-else-if="detail" class="space-y-5">
          <!-- Summary -->
          <div class="bg-gray-50 dark:bg-gray-800 rounded-xl p-4">
            <p class="text-xs text-gray-400 dark:text-gray-500 uppercase tracking-wider mb-1">Summary</p>
            <p class="text-sm font-medium text-gray-800 dark:text-gray-100">{{ detail.summary }}</p>
          </div>

          <!-- Actor -->
          <div class="bg-gray-50 dark:bg-gray-800 rounded-xl p-4">
            <p class="text-xs text-gray-400 dark:text-gray-500 uppercase tracking-wider mb-1">Performed by</p>
            <div v-if="detail.actor && detail.actor.type !== 'anonymous'" class="flex items-center gap-3">
              <div class="w-9 h-9 rounded-full flex items-center justify-center text-sm font-bold flex-shrink-0"
                :class="detail.actor.type === 'staff'
                  ? 'bg-blue-100 text-blue-600 dark:bg-blue-900/50 dark:text-blue-400'
                  : 'bg-purple-100 text-purple-600 dark:bg-purple-900/50 dark:text-purple-400'">
                {{ (detail.actor.fullName || '?').charAt(0).toUpperCase() }}
              </div>
              <div class="min-w-0">
                <p class="font-medium text-gray-800 dark:text-gray-100 truncate">{{ detail.actor.fullName || 'Unknown' }}</p>
                <p class="text-xs text-gray-500 dark:text-gray-400">
                  <span class="capitalize">{{ actorLabel() }}</span>
                  <template v-if="detail.actor.role"> · {{ detail.actor.role }}</template>
                  <template v-if="detail.actor.staffCode"> · {{ detail.actor.staffCode }}</template>
                </p>
                <p v-if="detail.actor.email || detail.actor.phone" class="text-xs text-gray-400 dark:text-gray-500 mt-0.5">
                  {{ [detail.actor.email, detail.actor.phone].filter(Boolean).join(' · ') }}
                </p>
              </div>
            </div>
            <div v-else>
              <p class="font-medium text-gray-800 dark:text-gray-100">Anonymous</p>
              <p class="text-xs text-gray-400 dark:text-gray-500 mt-0.5">No authenticated user (e.g. failed login)</p>
            </div>
          </div>

          <!-- Metadata: when / where -->
          <div class="grid grid-cols-2 gap-3">
            <div class="bg-gray-50 dark:bg-gray-800 rounded-xl p-3">
              <p class="text-xs text-gray-400 dark:text-gray-500 uppercase tracking-wider mb-1">Date &amp; Time</p>
              <p class="font-medium text-gray-800 dark:text-gray-100 text-sm">{{ formatDateTime(detail.createdAt) }}</p>
            </div>
            <div class="bg-gray-50 dark:bg-gray-800 rounded-xl p-3">
              <p class="text-xs text-gray-400 dark:text-gray-500 uppercase tracking-wider mb-1">IP Address</p>
              <p class="font-medium text-gray-800 dark:text-gray-100 text-sm font-mono">{{ detail.ipAddress || '—' }}</p>
            </div>
          </div>

          <!-- Request -->
          <div class="bg-gray-50 dark:bg-gray-800 rounded-xl p-3">
            <p class="text-xs text-gray-400 dark:text-gray-500 uppercase tracking-wider mb-1">Request</p>
            <p class="font-mono text-xs text-gray-700 dark:text-gray-300">
              {{ detail.httpMethod || '—' }} {{ detail.httpPath || '—' }}
            </p>
          </div>

          <!-- Changes (old -> new) -->
          <div v-if="detail.changes && detail.changes.length > 0" class="bg-gray-50 dark:bg-gray-800 rounded-xl p-4">
            <div class="flex items-center justify-between mb-3">
              <p class="text-xs text-gray-400 dark:text-gray-500 uppercase tracking-wider">Changes</p>
              <button @click="showChanges = !showChanges"
                class="text-xs text-blue-600 dark:text-blue-400 hover:underline cursor-pointer">
                {{ showChanges ? 'Hide' : 'Show' }} ({{ detail.changes.length }})
              </button>
            </div>
            <div v-if="showChanges" class="space-y-2">
              <div v-for="(c, i) in detail.changes" :key="i"
                class="rounded-lg border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-900 px-3 py-2">
                <p class="text-xs font-semibold text-gray-600 dark:text-gray-300 mb-1">{{ c.field }}</p>
                <div class="flex items-center gap-2 text-xs">
                  <span class="text-red-600 dark:text-red-400 line-through flex-1 min-w-0 truncate">{{ c.oldValue || '—' }}</span>
                  <span class="text-gray-400">→</span>
                  <span class="text-green-600 dark:text-green-400 flex-1 min-w-0 truncate">{{ c.newValue || '—' }}</span>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </Teleport>
</template>
