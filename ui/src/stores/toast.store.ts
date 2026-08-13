import { defineStore } from 'pinia'
import { ref } from 'vue'

export type ToastType = 'success' | 'error' | 'info'

export interface Toast {
  id: number
  type: ToastType
  message: string
  duration: number
}

let nextId = 1

export const useToastStore = defineStore('toast', () => {
  const toasts = ref<Toast[]>([])

  function push(message: string, type: ToastType = 'success', duration = 4000): number {
    const id = nextId++
    toasts.value.push({ id, type, message, duration })
    if (duration > 0) {
      setTimeout(() => remove(id), duration)
    }
    return id
  }

  function remove(id: number) {
    toasts.value = toasts.value.filter(t => t.id !== id)
  }

  function success(message: string, duration?: number) {
    return push(message, 'success', duration)
  }

  function error(message: string, duration?: number) {
    return push(message, 'error', duration)
  }

  function info(message: string, duration?: number) {
    return push(message, 'info', duration)
  }

  return { toasts, push, remove, success, error, info }
})
