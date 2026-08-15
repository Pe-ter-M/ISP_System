<script setup lang="ts">
import { ref, computed, watch, onMounted, onBeforeUnmount } from 'vue'
import { getCustomerById } from '@/services/customer.service'
import { reverseGeocode } from '@/services/geocode.service'
import { formatDate } from '@/utils/format'
import type { ReverseGeocodeResult } from '@/services/geocode.service'
import type { CustomerDetail, CustomerSubscription } from '@/types/customer.types'
import MapView from './MapView.vue'
import PlanDetailModal from './PlanDetailModal.vue'

/**
 * Re-usable customer detail modal.
 *
 * Used on the Customers page (opens standalone, allows edit) AND composed from
 * the Subscriptions detail modal (readonly — no edit). Supports viewing the
 * exact plan card a customer subscribed to when the subscription is not expired
 * (paused/suspended subscriptions are included).
 *
 * Editing is only offered via the `editable` flag — when a customer is being
 * viewed from another component's detail (e.g. subscription), editing is disabled.
 */
const props = withDefaults(defineProps<{
  customerId?: number | null
  /** When true, shows the "Edit Customer" button (Customers page). */
  editable?: boolean
  /** Optional load-on-mount guard — if false, wait until an id is set. */
  open?: boolean
  /** Higher z-index for stacked modals (e.g. above a subscription detail). */
  zIndex?: number
  /** Label shown above the title when this is nested inside another modal. */
  contextLabel?: string | null
}>(), {
  customerId: null,
  editable: true,
  open: true,
  zIndex: 50,
  contextLabel: null,
})

const emit = defineEmits<{
  (e: 'close'): void
  (e: 'edit', customer: CustomerDetail): void
}>()

const customer = ref<CustomerDetail | null>(null)
const loading = ref(false)
const error = ref<string | null>(null)
const location = ref<ReverseGeocodeResult | null>(null)
const locationLoading = ref(false)

// ── Plan pop-up stacked on top of this modal ──
const planModalId = ref<number | null>(null)

async function load(id: number) {
  loading.value = true
  error.value = null
  customer.value = null
  location.value = null
  locationLoading.value = false
  try {
    const d = await getCustomerById(id)
    customer.value = d
    if (d.gpsLat !== null && d.gpsLng !== null) {
      locationLoading.value = true
      reverseGeocode(d.gpsLat, d.gpsLng)
        .then(r => { location.value = r })
        .finally(() => { locationLoading.value = false })
    }
  } catch (e: unknown) {
    error.value = (e as { message?: string } | null)?.message || 'Failed to load customer details'
  } finally {
    loading.value = false
  }
}

watch(() => [props.customerId, props.open] as const, ([id, open]) => {
  if (open && id != null) load(id)
}, { immediate: true })

function close() {
  emit('close')
}

function editCustomer() {
  if (props.editable && customer.value) emit('edit', customer.value)
}

/** A subscription's plan can be viewed if it is not expired (paused included). */
function canViewPlan(sub: CustomerSubscription): boolean {
  if (sub.status === 'expired') return false
  if (sub.currentPeriodEnd) {
    const end = new Date(sub.currentPeriodEnd)
    if (!Number.isNaN(end.getTime()) && end.getTime() < Date.now()) return false
  }
  return true
}

function friendlyDate(iso: string | null | undefined): string {
  return formatDate(iso)
}

function statusClass(s: string): string {
  switch (s) {
    case 'active': return 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-300'
    case 'suspended': return 'bg-amber-100 text-amber-700 dark:bg-amber-900/30 dark:text-amber-300'
    case 'expired': return 'bg-red-100 text-red-600 dark:bg-red-900/30 dark:text-red-300'
    default: return 'bg-gray-100 text-gray-500 dark:bg-gray-800 dark:text-gray-400'
  }
}

function onBackdrop() { emit('close') }
function onKeydown(e: KeyboardEvent) { if (e.key === 'Escape') emit('close') }
onMounted(() => window.addEventListener('keydown', onKeydown))
onBeforeUnmount(() => window.removeEventListener('keydown', onKeydown))
</script>

<template>
  <Teleport to="body">
    <!-- Plan modal stacked on top of this one when opened -->
    <PlanDetailModal
      v-if="planModalId != null"
      :plan-id="planModalId"
      context-label="Plan the customer subscribed to"
      :z-index="zIndex + 10"
      @close="planModalId = null"
    />

    <div v-if="open && customerId != null" class="fixed inset-0 flex items-center justify-center p-4" :style="{ zIndex }" @click.self="onBackdrop">
      <div class="absolute inset-0 bg-black/50 backdrop-blur-sm" @click="onBackdrop"></div>
      <div class="relative bg-white dark:bg-gray-900 rounded-2xl shadow-2xl w-full max-w-2xl max-h-[90vh] overflow-y-auto animate-modal-in p-6 sm:p-8">

        <button @click="onBackdrop"
          class="absolute top-4 right-4 z-10 w-7 h-7 rounded-full bg-gray-100 dark:bg-gray-800 flex items-center justify-center text-gray-400 hover:bg-gray-200 dark:hover:bg-gray-700 cursor-pointer text-sm">✕</button>

        <div v-if="loading" class="flex justify-center py-16">
          <div class="w-8 h-8 border-4 border-blue-500 border-t-transparent rounded-full animate-spin"></div>
        </div>

        <div v-else-if="customer" class="space-y-6">
          <!-- Header -->
          <div class="flex items-start justify-between gap-4 pb-4 border-b border-gray-100 dark:border-gray-800">
            <div>
              <p v-if="contextLabel" class="text-xs text-gray-400 dark:text-gray-500 mb-1">{{ contextLabel }}</p>
              <div class="flex items-center gap-2 flex-wrap">
                <h2 class="text-xl font-bold text-gray-800 dark:text-gray-100">{{ customer.fullName }}</h2>
                <span :class="statusClass(customer.status)" class="px-2 py-0.5 rounded-full text-xs font-medium capitalize">{{ customer.status }}</span>
                <span class="text-xs px-2 py-0.5 rounded-full font-medium"
                  :class="customer.hasActiveSubscription
                    ? 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-300'
                    : 'bg-gray-100 text-gray-500 dark:bg-gray-800 dark:text-gray-400'">
                  {{ customer.hasActiveSubscription ? 'Active subscription' : 'No active subscription' }}
                </span>
              </div>
              <p class="text-xs text-gray-400 dark:text-gray-500 font-mono mt-1">{{ customer.customerCode }}</p>
              <p v-if="customer.businessName" class="text-sm text-gray-500 dark:text-gray-400 mt-1">{{ customer.businessName }} · <span class="capitalize">{{ customer.customerType }}</span></p>
            </div>
            <!-- Edit only available on the Customers page (not when composed cross-context) -->
            <button v-if="editable" @click="editCustomer"
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
                <p class="font-semibold text-gray-800 dark:text-gray-100 text-sm break-all">{{ customer.email || '—' }}</p>
              </div>
              <div class="bg-gray-50 dark:bg-gray-800 rounded-lg p-3">
                <p class="text-xs text-gray-400 dark:text-gray-500 mb-1">Phone</p>
                <p class="font-semibold text-gray-800 dark:text-gray-100 text-sm font-mono">{{ customer.phone }}</p>
              </div>
            </div>
          </div>

          <!-- Location -->
          <div>
            <h3 class="text-sm font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider mb-3">Location</h3>
            <div class="grid grid-cols-2 gap-3">
              <div class="bg-gray-50 dark:bg-gray-800 rounded-lg p-3 col-span-2">
                <p class="text-xs text-gray-400 dark:text-gray-500 mb-1">Service Address</p>
                <p class="font-semibold text-gray-800 dark:text-gray-100 text-sm">{{ customer.serviceAddress || '—' }}</p>
              </div>
              <div class="bg-gray-50 dark:bg-gray-800 rounded-lg p-3">
                <p class="text-xs text-gray-400 dark:text-gray-500 mb-1">City</p>
                <p class="font-semibold text-gray-800 dark:text-gray-100 text-sm">{{ customer.city || '—' }}</p>
              </div>
              <div class="bg-gray-50 dark:bg-gray-800 rounded-lg p-3">
                <p class="text-xs text-gray-400 dark:text-gray-500 mb-1">Region</p>
                <p class="font-semibold text-gray-800 dark:text-gray-100 text-sm">{{ customer.region || '—' }}</p>
              </div>
              <div class="col-span-2">
                <MapView
                  v-if="customer.gpsLat !== null && customer.gpsLng !== null"
                  :lat="customer.gpsLat"
                  :lng="customer.gpsLng"
                  :height="'200px'"
                />
                <p v-if="locationLoading" class="mt-2 text-xs text-gray-400 dark:text-gray-500">Resolving location…</p>
                <p v-else-if="location && location.displayName" class="mt-2 text-xs text-gray-500 dark:text-gray-400">{{ location.displayName }}</p>
                <p v-else-if="customer.gpsLat !== null" class="mt-2 text-xs text-gray-400 dark:text-gray-500 font-mono">
                  {{ Number(customer.gpsLat).toFixed(6) }}, {{ Number(customer.gpsLng).toFixed(6) }}
                </p>
                <p v-else class="mt-2 text-xs text-gray-400 dark:text-gray-500">No location set for this customer.</p>
              </div>
            </div>
          </div>

          <!-- Access & Account -->
          <div>
            <h3 class="text-sm font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider mb-3">Access &amp; Account</h3>
            <div class="grid grid-cols-2 gap-3">
              <div class="bg-gray-50 dark:bg-gray-800 rounded-lg p-3">
                <p class="text-xs text-gray-400 dark:text-gray-500 mb-1">PPPoE Username</p>
                <p class="font-semibold text-gray-800 dark:text-gray-100 text-sm font-mono">{{ customer.usernamePpoe || '—' }}</p>
              </div>
              <div class="bg-gray-50 dark:bg-gray-800 rounded-lg p-3">
                <p class="text-xs text-gray-400 dark:text-gray-500 mb-1">PPPoE Password</p>
                <p class="font-semibold text-gray-800 dark:text-gray-100 text-sm font-mono">{{ customer.passwordPpoe || '—' }}</p>
              </div>
              <div class="bg-gray-50 dark:bg-gray-800 rounded-lg p-3">
                <p class="text-xs text-gray-400 dark:text-gray-500 mb-1">Created</p>
                <p class="font-semibold text-gray-800 dark:text-gray-100 text-sm">{{ friendlyDate(customer.createdAt) }}</p>
              </div>
              <div class="bg-gray-50 dark:bg-gray-800 rounded-lg p-3">
                <p class="text-xs text-gray-400 dark:text-gray-500 mb-1">Last Updated</p>
                <p class="font-semibold text-gray-800 dark:text-gray-100 text-sm">{{ friendlyDate(customer.updatedAt) }}</p>
              </div>
            </div>
            <p v-if="customer.notes" class="mt-3 text-sm text-gray-500 dark:text-gray-400 bg-gray-50 dark:bg-gray-800 rounded-lg px-3 py-2"><span class="font-semibold text-gray-600 dark:text-gray-300">Notes: </span>{{ customer.notes }}</p>
          </div>

          <!-- Subscriptions with plan-card view -->
          <div>
            <h3 class="text-sm font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider mb-3">Subscriptions</h3>
            <div v-if="customer.subscriptions.length === 0" class="text-center py-6 bg-gray-50 dark:bg-gray-800 rounded-xl">
              <p class="text-sm text-gray-400 dark:text-gray-500">No subscriptions yet</p>
            </div>
            <div v-else class="space-y-2">
              <div v-for="sub in customer.subscriptions" :key="sub.id"
                class="flex items-center justify-between gap-3 rounded-xl border border-gray-100 dark:border-gray-800 bg-gray-50 dark:bg-gray-800/60 px-4 py-2.5">
                <div class="min-w-0">
                  <p class="text-sm font-medium text-gray-800 dark:text-gray-100">{{ sub.planName }}</p>
                  <p class="text-xs text-gray-400 dark:text-gray-500 font-mono">{{ sub.username }}</p>
                </div>
                <div class="text-right flex items-center gap-2">
                  <button v-if="canViewPlan(sub)" @click="planModalId = sub.packageId"
                    class="px-2.5 py-1 text-xs font-medium text-blue-600 dark:text-blue-400 bg-blue-50 dark:bg-blue-900/20 hover:bg-blue-100 dark:hover:bg-blue-900/40 rounded-lg transition cursor-pointer whitespace-nowrap">
                    View Plan
                  </button>
                  <div class="text-right">
                    <span :class="statusClass(sub.status)" class="px-2 py-0.5 rounded-full text-xs font-medium capitalize">{{ sub.status }}</span>
                    <p class="text-xs text-gray-400 dark:text-gray-500 mt-1">{{ sub.currentPeriodEnd ? `ends ${friendlyDate(sub.currentPeriodEnd)}` : 'no period' }}</p>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>

        <div v-else class="p-8 text-center">
          <p class="text-red-500 dark:text-red-400">{{ error || 'Failed to load customer details.' }}</p>
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
