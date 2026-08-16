<script setup lang="ts">
import { ref, watch, onMounted, onBeforeUnmount } from 'vue'
import { getPlanDetail } from '@/services/plan.service'
import { formatPrice, formatSpeed, formatDuration } from '@/types/plan.types'
import type { PlanDetail } from '@/types/plan.types'

/**
 * Re-usable plan detail modal — shows the "plan card" for a plan.
 * Used standalone (public Plans page) and composed from the customer detail
 * modal (view the exact plan a customer subscribed to).
 *
 * It is ALWAYS read-only: there is no edit action here, by design.
 */
const props = withDefaults(defineProps<{
  /** Plan id to display. When null, the modal does not render. */
  planId?: number | null
  /** Optional extra label shown under the title (e.g. "Viewing plan for: ...") */
  contextLabel?: string | null
  /** Higher z-index so stacked modals (customer over subscription) can sit above */
  zIndex?: number
}>(), {
  planId: null,
  contextLabel: null,
  zIndex: 50,
})

const emit = defineEmits<{ (e: 'close'): void }>()

const plan = ref<PlanDetail | null>(null)
const loading = ref(false)
const error = ref<string | null>(null)

async function load(id: number) {
  loading.value = true
  error.value = null
  plan.value = null
  try {
    plan.value = await getPlanDetail(id)
  } catch (e: unknown) {
    error.value = (e as { message?: string } | null)?.message || 'Failed to load plan details'
  } finally {
    loading.value = false
  }
}

watch(() => props.planId, (id) => {
  if (id != null) load(id)
}, { immediate: true })

function onBackdrop() {
  emit('close')
}

// Esc key closes the topmost modal
function onKeydown(e: KeyboardEvent) {
  if (e.key === 'Escape') emit('close')
}
onMounted(() => window.addEventListener('keydown', onKeydown))
onBeforeUnmount(() => window.removeEventListener('keydown', onKeydown))
</script>

<template>
  <Teleport to="body">
    <div v-if="planId != null" class="fixed inset-0 flex items-center justify-center p-4" :style="{ zIndex }" @click.self="onBackdrop">
      <div class="absolute inset-0 bg-black/50 backdrop-blur-sm" @click="onBackdrop"></div>
      <div class="relative bg-white dark:bg-gray-900 rounded-2xl shadow-2xl w-full max-w-lg max-h-[90vh] overflow-y-auto animate-modal-in">

        <button @click="onBackdrop"
          class="absolute top-4 right-4 z-10 w-7 h-7 rounded-full bg-gray-100 dark:bg-gray-800 flex items-center justify-center text-gray-400 hover:bg-gray-200 dark:hover:bg-gray-700 cursor-pointer text-sm">✕</button>

        <div v-if="loading" class="flex justify-center py-20">
          <div class="w-8 h-8 border-4 border-blue-500 border-t-transparent rounded-full animate-spin"></div>
        </div>

        <div v-else-if="plan" class="p-6 sm:p-8">
          <!-- Header -->
          <div class="mb-4">
            <p v-if="contextLabel" class="text-xs text-gray-400 dark:text-gray-500 mb-1">{{ contextLabel }}</p>
            <h2 class="text-2xl font-bold text-gray-900 dark:text-white">{{ plan.name }}</h2>
            <p v-if="plan.description" class="text-gray-500 dark:text-gray-400 mt-1">{{ plan.description }}</p>
          </div>

          <!-- Price highlight -->
          <div class="bg-blue-50 dark:bg-blue-900/30 rounded-xl p-4 mb-6 text-center">
            <span class="text-4xl font-bold text-blue-600 dark:text-blue-400">{{ formatPrice(plan.priceCents) }}</span>
            <span class="text-blue-400 dark:text-blue-300 text-sm"> /{{ plan.billingCycle }}</span>
          </div>

          <!-- Connection -->
          <h3 class="font-semibold text-gray-800 dark:text-gray-200 text-sm uppercase tracking-wider mb-3">Connection</h3>
          <div class="grid grid-cols-2 gap-4 mb-4">
            <div class="bg-gray-50 dark:bg-gray-800 rounded-lg p-3">
              <p class="text-xs text-gray-400 dark:text-gray-500 mb-1">Download Speed</p>
              <p class="font-semibold text-gray-800 dark:text-gray-100 text-lg">{{ formatSpeed(plan.bandwidthDownKbps) }}</p>
            </div>
            <div class="bg-gray-50 dark:bg-gray-800 rounded-lg p-3">
              <p class="text-xs text-gray-400 dark:text-gray-500 mb-1">Upload Speed</p>
              <p class="font-semibold text-gray-800 dark:text-gray-100 text-lg">{{ formatSpeed(plan.bandwidthUpKbps) }}</p>
            </div>
          </div>

          <!-- Session -->
          <h3 class="font-semibold text-gray-800 dark:text-gray-200 text-sm uppercase tracking-wider mb-3">Session</h3>
          <div class="grid grid-cols-2 gap-4">
            <div class="bg-gray-50 dark:bg-gray-800 rounded-lg p-3">
              <p class="text-xs text-gray-400 dark:text-gray-500 mb-1">Session Timeout</p>
              <p class="font-semibold text-gray-800 dark:text-gray-100">{{ formatDuration(plan.sessionTimeoutSeconds) }}</p>
            </div>
            <div class="bg-gray-50 dark:bg-gray-800 rounded-lg p-3">
              <p class="text-xs text-gray-400 dark:text-gray-500 mb-1">Idle Timeout</p>
              <p class="font-semibold text-gray-800 dark:text-gray-100">{{ formatDuration(plan.idleTimeoutSeconds) }}</p>
            </div>
          </div>

          <div class="mt-6 pt-4 flex justify-end">
            <button @click="onBackdrop"
              class="px-4 py-2 text-sm font-medium text-gray-600 dark:text-gray-300 bg-gray-100 dark:bg-gray-800 hover:bg-gray-200 dark:hover:bg-gray-700 rounded-lg transition cursor-pointer">
              Close
            </button>
          </div>
        </div>

        <div v-else class="p-8 text-center">
          <p class="text-red-500 dark:text-red-400">{{ error || 'Failed to load plan details.' }}</p>
          <button @click="onBackdrop" class="mt-4 text-blue-600 dark:text-blue-400 hover:underline cursor-pointer">Close</button>
        </div>
      </div>
    </div>
  </Teleport>
</template>

<style scoped>
@keyframes modalIn {
  from { opacity: 0; transform: scale(0.95) translateY(10px); }
  to { opacity: 1; transform: scale(1) translateY(0); }
}
.animate-modal-in { animation: modalIn 0.2s ease-out forwards; }
</style>
