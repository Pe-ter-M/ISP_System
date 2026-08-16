<script setup lang="ts">
import { useToastStore } from '@/stores/toast.store'
import type { ToastType } from '@/stores/toast.store'

const toast = useToastStore()

function typeClass(type: ToastType): string {
  switch (type) {
    case 'success':
      return 'bg-green-50 dark:bg-gray-800 border-green-200 dark:border-green-700'
    case 'error':
      return 'bg-red-50 dark:bg-gray-800 border-red-200 dark:border-red-700'
    default:
      return 'bg-white dark:bg-gray-800 border-gray-200 dark:border-gray-600'
  }
}

function iconClass(type: ToastType): string {
  switch (type) {
    case 'success':
      return 'text-green-600 dark:text-green-400'
    case 'error':
      return 'text-red-600 dark:text-red-400'
    default:
      return 'text-blue-600 dark:text-blue-400'
  }
}
</script>

<template>
  <Teleport to="body">
    <div class="fixed top-4 right-4 z-[100] flex flex-col items-end gap-2 w-80 max-w-[calc(100vw-2rem)] pointer-events-none">
      <TransitionGroup name="toast">
        <div
          v-for="t in toast.toasts"
          :key="t.id"
          role="status"
          class="pointer-events-auto flex items-start gap-3 rounded-xl border shadow-lg px-4 py-3 animate-toast-in"
          :class="typeClass(t.type)"
        >
          <!-- Success -->
          <svg v-if="t.type === 'success'" class="w-5 h-5 mt-0.5 flex-shrink-0" :class="iconClass(t.type)" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
            <path stroke-linecap="round" stroke-linejoin="round" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
          </svg>
          <!-- Error -->
          <svg v-else-if="t.type === 'error'" class="w-5 h-5 mt-0.5 flex-shrink-0" :class="iconClass(t.type)" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
            <path stroke-linecap="round" stroke-linejoin="round" d="M10 14l2-2m0 0l2-2m-2 2l-2-2m2 2l2 2m7-2a9 9 0 11-18 0 9 9 0 0118 0z" />
          </svg>
          <!-- Info -->
          <svg v-else class="w-5 h-5 mt-0.5 flex-shrink-0" :class="iconClass(t.type)" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
            <path stroke-linecap="round" stroke-linejoin="round" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
          </svg>

          <p class="text-sm text-gray-800 dark:text-gray-100 flex-1 leading-snug">{{ t.message }}</p>

          <button
            @click="toast.remove(t.id)"
            class="text-gray-400 hover:text-gray-600 dark:hover:text-gray-200 text-lg leading-none cursor-pointer transition"
            aria-label="Dismiss notification"
          >✕</button>
        </div>
      </TransitionGroup>
    </div>
  </Teleport>
</template>

<style scoped>
@keyframes toastIn {
  from { opacity: 0; transform: translateX(16px); }
  to { opacity: 1; transform: translateX(0); }
}
.animate-toast-in {
  animation: toastIn 0.25s ease-out forwards;
}
.toast-enter-active,
.toast-leave-active {
  transition: all 0.25s ease;
}
.toast-enter-from {
  opacity: 0;
  transform: translateX(16px);
}
.toast-leave-to {
  opacity: 0;
  transform: translateX(16px);
}
</style>
