<script setup lang="ts">
import { computed } from 'vue'
import { useAuthStore } from '@/stores/auth.store'

/**
 * Reusable permission gate.
 *
 * Renders its default slot only when the current user has the required
 * permission. Use it to conditionally show buttons/actions (e.g. hide Edit
 * without an `update` permission, hide Delete without a `delete` permission).
 *
 *   <Can permission="customer.create"><button>Add Customer</button></Can>
 *   <Can :permissions="['customer.update','customer.delete']">…</Can>
 *
 * With `mode="any"` (default) the slot shows if the user has ANY of the
 * listed permissions; with `mode="all"` it requires ALL of them.
 * `permission` can be '*' (always shows) or a single code (e.g. 'customer.view').
 */
const props = withDefaults(defineProps<{
  permission?: string
  permissions?: string[]
  mode?: 'any' | 'all'
}>(), {
  permission: '*',
  permissions: () => [],
  mode: 'any',
})

const auth = useAuthStore()

const allowed = computed(() => {
  // Single permission
  if (props.permission && props.permission !== '*') {
    return auth.can(props.permission)
  }
  // List of permissions
  if (props.permissions.length > 0) {
    return props.mode === 'all'
      ? props.permissions.every((p) => auth.can(p))
      : props.permissions.some((p) => auth.can(p))
  }
  // No constraint → allow
  return true
})
</script>

<template>
  <template v-if="allowed"><slot /></template>
</template>
