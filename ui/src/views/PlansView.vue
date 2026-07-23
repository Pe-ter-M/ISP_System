<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { getPlans, getPlanDetail } from '@/services/plan.service'
import { formatPrice, formatSpeed } from '@/types/plan.types'
import type { PlanSummary, PlanDetail } from '@/types/plan.types'

// ── State ──
const plans = ref<PlanSummary[]>([])
const loading = ref(true)
const error = ref<string | null>(null)

// Pagination
const currentPage = ref(1)
const perPage = 6

// Modal state
const showModal = ref(false)
const selectedPlan = ref<PlanDetail | null>(null)
const modalLoading = ref(false)

// ── Computed ──
const totalPages = computed(() => Math.ceil(plans.value.length / perPage))

const paginatedPlans = computed(() => {
  const start = (currentPage.value - 1) * perPage
  return plans.value.slice(start, start + perPage)
})

const pageNumbers = computed(() => {
  const pages: number[] = []
  for (let i = 1; i <= totalPages.value; i++) pages.push(i)
  return pages
})

// Plan card colors
const cardColors = [
  { bg: 'from-blue-500 to-blue-600', badge: 'bg-blue-100 text-blue-700 dark:bg-blue-900/50 dark:text-blue-300' },
  { bg: 'from-green-500 to-emerald-600', badge: 'bg-green-100 text-green-700 dark:bg-green-900/50 dark:text-green-300' },
  { bg: 'from-purple-500 to-violet-600', badge: 'bg-purple-100 text-purple-700 dark:bg-purple-900/50 dark:text-purple-300' },
  { bg: 'from-orange-500 to-amber-600', badge: 'bg-orange-100 text-orange-700 dark:bg-orange-900/50 dark:text-orange-300' },
  { bg: 'from-rose-500 to-pink-600', badge: 'bg-rose-100 text-rose-700 dark:bg-rose-900/50 dark:text-rose-300' },
  { bg: 'from-cyan-500 to-teal-600', badge: 'bg-cyan-100 text-cyan-700 dark:bg-cyan-900/50 dark:text-cyan-300' },
]

function getColor(index: number) {
  return cardColors[index % cardColors.length]
}

// ── Actions ──
onMounted(async () => {
  try {
    plans.value = await getPlans()
  } catch (e: any) {
    error.value = e?.message || 'Failed to load plans'
  } finally {
    loading.value = false
  }
})

function goToPage(page: number) {
  currentPage.value = page
  window.scrollTo({ top: 0, behavior: 'smooth' })
}

async function openModal(id: number) {
  showModal.value = true
  modalLoading.value = true
  try {
    selectedPlan.value = await getPlanDetail(id)
  } catch (e: any) {
    selectedPlan.value = null
  } finally {
    modalLoading.value = false
  }
}

function closeModal() {
  showModal.value = false
  selectedPlan.value = null
}

/** Convert seconds to a human-readable duration string */
function formatDuration(seconds: number): string {
  if (seconds >= 86400) {
    const days = seconds / 86400
    return `${days} day${days > 1 ? 's' : ''}`
  }
  if (seconds >= 3600) {
    const hours = Math.floor(seconds / 3600)
    const mins = Math.round((seconds % 3600) / 60)
    return mins > 0 ? `${hours}h ${mins}m` : `${hours} hours`
  }
  return `${Math.round(seconds / 60)} min`
}
</script>

<template>
  <div>
    <!-- ── Page Header ── -->
    <section class="text-center py-8 sm:py-12 animate-fade-in">
      <h1 class="text-4xl sm:text-5xl font-bold text-gray-900 dark:text-white mb-4">Our Internet Plans</h1>
      <p class="text-lg text-gray-500 dark:text-gray-400 max-w-2xl mx-auto">
        Choose the perfect plan for your needs. All plans come with reliable connectivity and 24/7 support.
      </p>
    </section>

    <!-- ── Loading State ── -->
    <div v-if="loading" class="flex justify-center py-20">
      <div class="flex flex-col items-center gap-4">
        <div class="w-10 h-10 border-4 border-blue-500 border-t-transparent rounded-full animate-spin"></div>
        <p class="text-gray-500 dark:text-gray-400">Loading plans...</p>
      </div>
    </div>

    <!-- ── Error State ── -->
    <div v-else-if="error" class="text-center py-20">
      <div class="bg-red-50 dark:bg-red-900/30 border border-red-200 dark:border-red-800 rounded-xl p-8 max-w-md mx-auto">
        <p class="text-red-600 dark:text-red-300 text-lg font-medium">Failed to load plans</p>
        <p class="text-red-500 dark:text-red-400 text-sm mt-2">{{ error }}</p>
        <button
          @click="loading = true; error = null; getPlans().then(d => plans.value = d).catch(e => error = e.message).finally(() => loading = false)"
          class="mt-4 px-6 py-2 bg-red-600 hover:bg-red-700 text-white rounded-lg transition-all"
        >
          Retry
        </button>
      </div>
    </div>

    <!-- ── Plan Cards Grid ── -->
    <div v-else class="space-y-8">
      <div v-if="plans.length === 0" class="text-center py-20">
        <p class="text-gray-400 dark:text-gray-500 text-lg">No plans available at the moment.</p>
      </div>

      <div v-else class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6 sm:gap-8">
        <div
          v-for="(plan, index) in paginatedPlans"
          :key="plan.id"
          class="group bg-white dark:bg-gray-900 rounded-xl shadow-md hover:shadow-xl transition-all duration-300 hover:-translate-y-1 border border-gray-100 dark:border-gray-800 overflow-hidden flex flex-col"
        >
          <!-- Card Header Gradient -->
          <div :class="`bg-gradient-to-r ${getColor(index).bg} px-6 py-5`">
            <h3 class="text-xl font-bold text-white">{{ plan.name }}</h3>
            <p v-if="plan.description" class="text-white/80 text-sm mt-1">
              {{ plan.description.length > 50 ? plan.description.slice(0, 50) + '...' : plan.description }}
            </p>
          </div>

          <!-- Card Body -->
          <div class="p-6 flex-1 flex flex-col">
            <!-- Price -->
            <div class="text-center mb-4">
              <span class="text-3xl font-bold text-gray-800 dark:text-gray-100">
                {{ formatPrice(plan.priceCents) }}
              </span>
              <span class="text-gray-400 dark:text-gray-500 text-sm"> /{{ plan.billingCycle }}</span>
            </div>

            <!-- Specs -->
            <div class="space-y-3 mb-6 flex-1">
              <div class="flex items-center justify-between text-sm">
                <span class="text-gray-500 dark:text-gray-400">Download</span>
                <span class="font-semibold text-gray-700 dark:text-gray-200">{{ formatSpeed(plan.bandwidthDownKbps) }}</span>
              </div>
              <div class="flex items-center justify-between text-sm">
                <span class="text-gray-500 dark:text-gray-400">Upload</span>
                <span class="font-semibold text-gray-700 dark:text-gray-200">{{ formatSpeed(plan.bandwidthUpKbps) }}</span>
              </div>
            </div>

            <!-- Action Button -->
            <button
              @click="openModal(plan.id)"
              class="w-full px-4 py-2.5 rounded-lg transition-all duration-200 font-medium text-sm cursor-pointer"
              :class="`${getColor(index).badge} hover:opacity-80`"
            >
              View Details →
            </button>
          </div>
        </div>
      </div>

      <!-- ── Pagination ── -->
      <div v-if="totalPages > 1" class="flex items-center justify-center gap-2 py-6">
        <button
          @click="goToPage(currentPage - 1)"
          :disabled="currentPage === 1"
          class="px-4 py-2 rounded-lg text-sm font-medium transition-all duration-200 disabled:opacity-30 disabled:cursor-not-allowed cursor-pointer
            bg-gray-100 dark:bg-gray-800 text-gray-600 dark:text-gray-300 hover:bg-gray-200 dark:hover:bg-gray-700"
        >
          ← Prev
        </button>

        <button
          v-for="page in pageNumbers"
          :key="page"
          @click="goToPage(page)"
          class="w-10 h-10 rounded-lg text-sm font-medium transition-all duration-200 cursor-pointer"
          :class="page === currentPage
            ? 'bg-blue-600 text-white shadow-md'
            : 'bg-gray-100 dark:bg-gray-800 text-gray-600 dark:text-gray-300 hover:bg-gray-200 dark:hover:bg-gray-700'"
        >
          {{ page }}
        </button>

        <button
          @click="goToPage(currentPage + 1)"
          :disabled="currentPage === totalPages"
          class="px-4 py-2 rounded-lg text-sm font-medium transition-all duration-200 disabled:opacity-30 disabled:cursor-not-allowed cursor-pointer
            bg-gray-100 dark:bg-gray-800 text-gray-600 dark:text-gray-300 hover:bg-gray-200 dark:hover:bg-gray-700"
        >
          Next →
        </button>
      </div>
    </div>

    <!-- ── Plan Detail Modal ── -->
    <Teleport to="body">
      <div
        v-if="showModal"
        class="fixed inset-0 z-50 flex items-center justify-center p-4"
        @click.self="closeModal"
      >
        <!-- Backdrop -->
        <div class="absolute inset-0 bg-black/50 backdrop-blur-sm" @click="closeModal"></div>

        <!-- Modal Content -->
        <div
          class="relative bg-white dark:bg-gray-900 rounded-2xl shadow-2xl w-full max-w-lg max-h-[90vh] overflow-y-auto animate-modal-in"
        >
          <!-- Loading inside modal -->
          <div v-if="modalLoading" class="flex justify-center py-20">
            <div class="w-8 h-8 border-4 border-blue-500 border-t-transparent rounded-full animate-spin"></div>
          </div>

          <!-- Plan Detail -->
          <div v-else-if="selectedPlan" class="p-6 sm:p-8">
            <!-- Close Button -->
            <button
              @click="closeModal"
              class="absolute top-4 right-4 w-8 h-8 rounded-full bg-gray-100 dark:bg-gray-800 flex items-center justify-center text-gray-500 dark:text-gray-400 hover:bg-gray-200 dark:hover:bg-gray-700 transition-all cursor-pointer"
            >
              ✕
            </button>

            <!-- Header -->
            <div class="mb-6">
              <h2 class="text-2xl font-bold text-gray-900 dark:text-white">{{ selectedPlan.name }}</h2>
              <p v-if="selectedPlan.description" class="text-gray-500 dark:text-gray-400 mt-1">
                {{ selectedPlan.description }}
              </p>
            </div>

            <!-- Price Highlight -->
            <div class="bg-blue-50 dark:bg-blue-900/30 rounded-xl p-4 mb-6 text-center">
              <span class="text-4xl font-bold text-blue-600 dark:text-blue-400">
                {{ formatPrice(selectedPlan.priceCents) }}
              </span>
              <span class="text-blue-400 dark:text-blue-300 text-sm"> /{{ selectedPlan.billingCycle }}</span>
            </div>

            <!-- Details Grid -->
            <div class="space-y-4">
              <h3 class="font-semibold text-gray-800 dark:text-gray-200 text-sm uppercase tracking-wider">Connection</h3>

              <div class="grid grid-cols-2 gap-4">
                <div class="bg-gray-50 dark:bg-gray-800 rounded-lg p-3">
                  <p class="text-xs text-gray-400 dark:text-gray-500 mb-1">Download Speed</p>
                  <p class="font-semibold text-gray-800 dark:text-gray-100 text-lg">{{ formatSpeed(selectedPlan.bandwidthDownKbps) }}</p>
                </div>
                <div class="bg-gray-50 dark:bg-gray-800 rounded-lg p-3">
                  <p class="text-xs text-gray-400 dark:text-gray-500 mb-1">Upload Speed</p>
                  <p class="font-semibold text-gray-800 dark:text-gray-100 text-lg">{{ formatSpeed(selectedPlan.bandwidthUpKbps) }}</p>
                </div>
              </div>

              <h3 class="font-semibold text-gray-800 dark:text-gray-200 text-sm uppercase tracking-wider pt-2">Session</h3>

              <div class="grid grid-cols-2 gap-4">
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

            <!-- Footer CTA -->
            <div class="mt-6 pt-4 border-t border-gray-200 dark:border-gray-700">
              <router-link
                to="/contact"
                class="block w-full px-6 py-3 bg-blue-600 hover:bg-blue-700 text-white font-semibold rounded-lg text-center transition-all duration-200 hover:scale-[1.02]"
              >
                Get This Plan
              </router-link>
            </div>
          </div>

          <!-- Error in modal -->
          <div v-else class="p-8 text-center">
            <p class="text-red-500 dark:text-red-400">Failed to load plan details.</p>
            <button @click="closeModal" class="mt-4 text-blue-600 dark:text-blue-400 hover:underline cursor-pointer">Close</button>
          </div>
        </div>
      </div>
    </Teleport>
  </div>
</template>

<style scoped>
@keyframes fadeIn {
  from { opacity: 0; transform: translateY(20px); }
  to { opacity: 1; transform: translateY(0); }
}
@keyframes modalIn {
  from { opacity: 0; transform: scale(0.95) translateY(10px); }
  to { opacity: 1; transform: scale(1) translateY(0); }
}
.animate-fade-in {
  animation: fadeIn 0.6s ease-out forwards;
}
.animate-modal-in {
  animation: modalIn 0.25s ease-out forwards;
}
</style>
