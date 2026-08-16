<script setup lang="ts">
import { ref, onMounted, onBeforeUnmount } from 'vue'

defineProps<{
  text: string
}>()

const open = ref(false)
const pos = ref({ top: 0, left: 0, below: true })
const trigger = ref<HTMLElement | null>(null)

const TOOLTIP_WIDTH = 224 // w-56

/** Position the tooltip at the trigger's viewport rect — never clipped by scroll containers */
function updatePosition() {
  const el = trigger.value
  if (!el) return
  const rect = el.getBoundingClientRect()
  const spaceAbove = rect.top
  const spaceBelow = window.innerHeight - rect.bottom
  // Prefer below; flip above only when there is more room up there
  const below = spaceBelow >= 160 || spaceBelow >= spaceAbove
  const cx = rect.left + rect.width / 2
  const clampedX = Math.min(Math.max(cx, TOOLTIP_WIDTH / 2 + 8), window.innerWidth - TOOLTIP_WIDTH / 2 - 8)
  pos.value = {
    top: below ? rect.bottom + 8 : rect.top - 8,
    left: clampedX,
    below,
  }
}

function show() {
  open.value = true
  updatePosition()
}

function hide() {
  open.value = false
}

/** Keep the tooltip glued to the trigger while open (modal scroll, resize) */
function onReposition() {
  if (open.value) updatePosition()
}

onMounted(() => {
  window.addEventListener('scroll', onReposition, true)
  window.addEventListener('resize', onReposition)
})
onBeforeUnmount(() => {
  window.removeEventListener('scroll', onReposition, true)
  window.removeEventListener('resize', onReposition)
})
</script>

<template>
  <span ref="trigger" class="relative inline-flex" @mouseenter="show" @mouseleave="hide">
    <button
      type="button"
      :aria-label="`More info: ${text}`"
      @focus="show"
      @blur="hide"
      class="w-4 h-4 rounded-full bg-gray-200 dark:bg-gray-700 text-gray-500 dark:text-gray-300 text-[10px] font-bold flex items-center justify-center cursor-help select-none transition-colors hover:bg-blue-200 dark:hover:bg-blue-900/50 hover:text-blue-600 dark:hover:text-blue-300 focus:outline-none focus:ring-2 focus:ring-blue-500"
    >?</button>

    <!-- Portaled to body so no modal/overflow container can clip it -->
    <Teleport to="body">
      <div
        v-if="open"
        role="tooltip"
        class="fixed z-[70] w-56 px-3 py-2 rounded-lg bg-gray-800 dark:bg-gray-950 text-gray-100 dark:text-gray-200 text-xs leading-snug shadow-lg pointer-events-none animate-tip-in"
        :style="{
          top: `${pos.top}px`,
          left: `${pos.left}px`,
          transform: pos.below ? 'translate(-50%, 0)' : 'translate(-50%, -100%)',
        }"
      >
        {{ text }}
      </div>
    </Teleport>
  </span>
</template>

<style scoped>
@keyframes tipIn {
  from { opacity: 0; }
  to { opacity: 1; }
}
.animate-tip-in {
  animation: tipIn 0.12s ease-out forwards;
}
</style>
