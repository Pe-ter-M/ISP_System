<script setup lang="ts">
defineProps<{
  text: string
}>()
</script>

<template>
  <span class="relative inline-flex group/tip">
    <button
      type="button"
      :aria-label="`More info: ${text}`"
      class="w-4 h-4 rounded-full bg-gray-200 dark:bg-gray-700 text-gray-500 dark:text-gray-300 text-[10px] font-bold flex items-center justify-center cursor-help select-none transition-colors hover:bg-blue-200 dark:hover:bg-blue-900/50 hover:text-blue-600 dark:hover:text-blue-300 focus:outline-none focus:ring-2 focus:ring-blue-500"
    >?</button>
    <span
      class="absolute bottom-full left-1/2 -translate-x-1/2 mb-1.5 w-56 px-3 py-2 rounded-lg bg-gray-800 dark:bg-gray-950 text-gray-100 dark:text-gray-200 text-xs leading-snug shadow-lg opacity-0 group-hover/tip:opacity-100 group-focus-within/tip:opacity-100 transition-opacity duration-150 pointer-events-none z-50"
    >
      {{ text }}
    </span>
  </span>
</template>
