<script setup lang="ts">
import { ref, shallowRef, watch, onMounted, onBeforeUnmount } from 'vue'
import * as L from 'leaflet'
import 'leaflet/dist/leaflet.css'
import { useThemeStore } from '@/stores/theme.store'
import { MAP_CONFIG } from '@/config/map.config'
import { searchPlaces } from '@/services/geocode.service'
import type { PlaceSearchResult } from '@/services/geocode.service'

export interface MapMarker {
  lat: number
  lng: number
  label?: string
  sublabel?: string
  color?: string
}

const props = withDefaults(defineProps<{
  lat?: number | null
  lng?: number | null
  markers?: MapMarker[]
  /** When true, clicking the map emits `select` (pick mode) */
  interactive?: boolean
  /** When true, show a place search box (Nominatim, free) */
  searchable?: boolean
  /** When true, show a button that expands the map to full screen */
  fullscreenable?: boolean
  /** When true and a location already exists, require confirmation before updating it */
  confirmUpdate?: boolean
  height?: string
  zoom?: number
  /** Auto-fit bounds when multiple markers are supplied */
  fitToMarkers?: boolean
}>(), {
  lat: null,
  lng: null,
  markers: () => [],
  interactive: false,
  searchable: false,
  fullscreenable: false,
  confirmUpdate: false,
  height: '300px',
  zoom: 15,
  fitToMarkers: false,
})

const emit = defineEmits<{
  (e: 'select', payload: { lat: number; lng: number }): void
}>()

const theme = useThemeStore()
const container = ref<HTMLDivElement | null>(null)

// Leaflet map instances are class objects — use shallowRef so Vue's ref unwrap
// (which drops private members in mapped types) never touches them.
const map = shallowRef<L.Map | null>(null)
const markerLayer = shallowRef<L.LayerGroup | null>(null)
const lastEmitted = ref<{ lat: number; lng: number } | null>(null)
// Last point the user clicked/searched in picker mode — rendered as a distinct
// preview pin so the existing location stays visible until the update commits.
const selectionRef = ref<{ lat: number; lng: number } | null>(null)
// Held point awaiting confirmation when an existing location is being updated
const pendingConfirm = ref<{ lat: number; lng: number } | null>(null)

// ── Search state ──
const searchQuery = ref('')
const searchResults = ref<PlaceSearchResult[]>([])
const searchOpen = ref(false)
const searching = ref(false)

// ── Fullscreen state ──
const isFullscreen = ref(false)

function escapeHtml(s: string): string {
  return s.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;')
}

/** Teardrop pin rendered as a divIcon so no external marker images are needed */
function pinIcon(color: string): L.DivIcon {
  return L.divIcon({
    className: 'cw-map-pin-wrap',
    html: `<div class="cw-map-pin" style="background:${color}"></div>`,
    iconSize: [26, 26],
    iconAnchor: [13, 26],
    popupAnchor: [0, -24],
  })
}

function markerList(): { lat: number; lng: number; label?: string; sublabel?: string; color?: string }[] {
  if (props.markers.length > 0) return props.markers
  if (props.lat != null && props.lng != null) return [{ lat: props.lat, lng: props.lng }]
  return []
}

function renderMarkers() {
  if (!map.value || !markerLayer.value) return
  markerLayer.value.clearLayers()
  const list = markerList()
  for (const mk of list) {
    const marker = L.marker([mk.lat, mk.lng], { icon: pinIcon(mk.color ?? '#3b82f6') })
    if (mk.label || mk.sublabel) {
      marker.bindPopup(
        `<div class="cw-map-popup">` +
        `<p class="cw-map-popup-label">${escapeHtml(mk.label ?? '')}</p>` +
        (mk.sublabel ? `<p class="cw-map-popup-sub">${escapeHtml(mk.sublabel)}</p>` : '') +
        `</div>`,
      )
    }
    marker.addTo(markerLayer.value)
  }
  if (props.fitToMarkers && list.length > 1) {
    const bounds = L.latLngBounds(list.map(mk => [mk.lat, mk.lng] as [number, number]))
    map.value.fitBounds(bounds, { padding: [48, 48], maxZoom: 16 })
  }

  // Selection preview: the point the user is moving to, shown alongside the
  // committed location until the parent commits the same coordinates.
  // Amber when it replaces an existing location (update), indigo when new.
  const sel = selectionRef.value
  if (props.interactive && sel && (props.lat == null || props.lng == null ||
      Math.abs(props.lat - sel.lat) > 1e-6 || Math.abs(props.lng - sel.lng) > 1e-6)) {
    const isUpdate = props.lat != null && props.lng != null
    const preview = L.marker([sel.lat, sel.lng], { icon: pinIcon(isUpdate ? '#f59e0b' : '#6366f1') })
    preview.bindPopup(
      `<div class="cw-map-popup">` +
      `<p class="cw-map-popup-label">${isUpdate ? 'Updating location' : 'New location'}</p>` +
      `<p class="cw-map-popup-sub">${sel.lat.toFixed(6)}, ${sel.lng.toFixed(6)}</p>` +
      `</div>`,
    )
    preview.addTo(markerLayer.value)
  }
}

function flyTo(lat: number, lng: number, zoom: number = props.zoom) {
  if (!map.value) return
  map.value.flyTo([lat, lng], zoom)
}

/** Select a search result — fly there and, in picker mode, report the coordinates */
function pickPlace(r: PlaceSearchResult) {
  searchOpen.value = false
  searchQuery.value = r.displayName
  flyTo(r.lat, r.lng, 16)
  if (props.interactive) selectPoint({ lat: r.lat, lng: r.lng })
}

/**
 * Handle a picked point (map click or search). If an existing location would be
 * overwritten and `confirmUpdate` is set, hold the point for confirmation instead
 * of committing immediately — the amber "Updating location" pin makes it visible.
 */
function selectPoint(point: { lat: number; lng: number }) {
  selectionRef.value = point
  const hasExisting = props.lat != null && props.lng != null
  const differs = !hasExisting ||
    Math.abs(props.lat! - point.lat) > 1e-6 ||
    Math.abs(props.lng! - point.lng) > 1e-6
  if (props.confirmUpdate && hasExisting && differs) {
    pendingConfirm.value = point
  } else {
    pendingConfirm.value = null
    lastEmitted.value = point
    emit('select', point)
  }
  renderMarkers()
}

function confirmPending() {
  if (!pendingConfirm.value) return
  const point = pendingConfirm.value
  pendingConfirm.value = null
  lastEmitted.value = point
  emit('select', point)
  renderMarkers()
}

function cancelPending() {
  pendingConfirm.value = null
  selectionRef.value = null
  renderMarkers()
}

function closeSearch() {
  searchOpen.value = false
}

/** Delay closing so a result click (mousedown) lands before blur */
function delayCloseSearch() {
  setTimeout(closeSearch, 200)
}

function toggleFullscreen() {
  isFullscreen.value = !isFullscreen.value
  setTimeout(() => { map.value?.invalidateSize() }, 80)
}

onMounted(() => {
  if (!container.value) return
  map.value = L.map(container.value, {
    center: [props.lat ?? MAP_CONFIG.defaultCenter.lat, props.lng ?? MAP_CONFIG.defaultCenter.lng],
    zoom: props.zoom,
    attributionControl: false,
    zoomControl: false,
  })
  L.control.zoom({ position: 'topright' }).addTo(map.value)
  L.control.attribution({ prefix: false }).addTo(map.value)
  L.tileLayer(MAP_CONFIG.tileUrl, {
    attribution: MAP_CONFIG.tileAttribution,
    maxZoom: 19,
  }).addTo(map.value)
  markerLayer.value = L.layerGroup().addTo(map.value)
  renderMarkers()
  if (props.interactive) {
    map.value.on('click', (e: L.LeafletMouseEvent) => {
      selectPoint({ lat: e.latlng.lat, lng: e.latlng.lng })
    })
  }
  // Modal animations can leave the map mis-sized — re-measure once settled
  setTimeout(() => { map.value?.invalidateSize() }, 300)
})

onBeforeUnmount(() => {
  map.value?.remove()
  map.value = null
  markerLayer.value = null
})

// Center the map when coordinates change from the parent (e.g. geolocation result)
watch(() => [props.lat, props.lng], ([lat, lng]) => {
  if (lat == null || lng == null) {
    selectionRef.value = null
    pendingConfirm.value = null
    renderMarkers()
    return
  }
  pendingConfirm.value = null
  // Once the parent commits to the previewed point, the main pin marks it — drop the preview
  if (selectionRef.value &&
      Math.abs(selectionRef.value.lat - lat) < 1e-6 &&
      Math.abs(selectionRef.value.lng - lng) < 1e-6) {
    selectionRef.value = null
  }
  flyTo(lat, lng)
  renderMarkers()
})

// Dark mode is handled purely by CSS (see the style block) — keep the watcher
// so the reactive dependency exists and marker re-renders stay in sync
watch(() => theme.isDark, () => {
  // Tile pane filter is CSS-driven; nothing else needed
})

watch(() => props.markers, () => {
  renderMarkers()
}, { deep: true })

// ── Place search (debounced) ──
let searchTimer: ReturnType<typeof setTimeout> | null = null
watch(searchQuery, () => {
  if (searchTimer) clearTimeout(searchTimer)
  const q = searchQuery.value.trim()
  if (q.length < 3) {
    searchResults.value = []
    return
  }
  searchTimer = setTimeout(async () => {
    searching.value = true
    try {
      searchResults.value = await searchPlaces(q)
      searchOpen.value = true
    } finally {
      searching.value = false
    }
  }, 400)
})

defineExpose({ flyTo })
</script>

<template>
  <div
    class="cw-map w-full rounded-xl overflow-hidden border border-gray-200 dark:border-gray-700 relative"
    :class="{ 'cw-map-fullscreen': isFullscreen }"
    :style="isFullscreen ? undefined : { height }"
  >
    <div ref="container" class="w-full h-full"></div>

    <!-- ── Place search ── -->
    <div v-if="searchable" class="cw-map-search absolute top-2 left-2 z-[1100] w-64 max-w-[calc(100%-4rem)]">
      <div class="relative">
        <svg class="absolute left-2.5 top-1/2 -translate-y-1/2 w-3.5 h-3.5 text-gray-400" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
          <path stroke-linecap="round" stroke-linejoin="round" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
        </svg>
        <input
          v-model="searchQuery"
          type="text"
          placeholder="Search a place…"
          @focus="searchOpen = true"
          @blur="delayCloseSearch"
          class="w-full pl-8 pr-8 py-1.5 text-xs rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 shadow-md focus:ring-2 focus:ring-blue-500 outline-none transition placeholder:text-gray-400"
        />
        <span v-if="searching" class="absolute right-2.5 top-1/2 -translate-y-1/2 w-3 h-3 border-2 border-blue-500 border-t-transparent rounded-full animate-spin"></span>
      </div>
      <ul
        v-if="searchOpen && searchResults.length > 0"
        class="mt-1 rounded-lg border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 shadow-lg overflow-hidden divide-y divide-gray-100 dark:divide-gray-700"
      >
        <li v-for="(r, i) in searchResults" :key="i">
          <button
            type="button"
            @mousedown.prevent="pickPlace(r)"
            class="w-full px-3 py-2 text-left text-xs text-gray-700 dark:text-gray-300 hover:bg-blue-50 dark:hover:bg-blue-900/30 transition cursor-pointer line-clamp-2"
          >{{ r.displayName }}</button>
        </li>
      </ul>
    </div>

    <!-- ── Fullscreen toggle ── -->
    <button
      v-if="fullscreenable"
      type="button"
      @click="toggleFullscreen"
      class="absolute top-2 right-2 z-[1100] w-7 h-7 rounded-lg bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 shadow-md flex items-center justify-center text-gray-600 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 transition cursor-pointer"
      :title="isFullscreen ? 'Exit full screen' : 'Expand to full screen'"
    >
      <svg v-if="!isFullscreen" class="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
        <path stroke-linecap="round" stroke-linejoin="round" d="M4 8V4m0 0h4M4 4l5 5m11-1V4m0 0h-4m4 0l-5 5M4 16v4m0 0h4m-4 0l5-5m11 5l-5-5m5 5v-4m0 4h-4" />
      </svg>
      <svg v-else class="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
        <path stroke-linecap="round" stroke-linejoin="round" d="M9 9V4m0 0H4m5 0L4 9m11-5h5m0 0v5m0-5l-5 5M9 20v-5m0 5H4m5 0l-5-5m11 5h5m0 0v-5m0 5l-5-5" />
      </svg>
    </button>

    <!-- ── Update confirmation (never change a location by mistake) ── -->
    <div v-if="pendingConfirm" class="absolute bottom-3 inset-x-2 z-[1100] flex justify-center pointer-events-none">
      <div class="pointer-events-auto flex items-center gap-3 rounded-xl border border-amber-300 dark:border-amber-700 bg-amber-50 dark:bg-amber-900/40 px-4 py-2.5 shadow-lg flex-wrap justify-center">
        <svg class="w-4 h-4 text-amber-600 dark:text-amber-400 flex-shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
          <path stroke-linecap="round" stroke-linejoin="round" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
        </svg>
        <p class="text-xs font-semibold text-amber-800 dark:text-amber-300">You are updating the customer location</p>
        <div class="flex items-center gap-2">
          <button type="button" @click="confirmPending"
            class="px-3 py-1 text-xs font-medium text-white bg-amber-600 hover:bg-amber-700 rounded-lg transition cursor-pointer">
            Confirm update
          </button>
          <button type="button" @click="cancelPending"
            class="px-3 py-1 text-xs font-medium text-amber-800 dark:text-amber-200 bg-amber-100 dark:bg-amber-800/60 hover:bg-amber-200 dark:hover:bg-amber-700 rounded-lg transition cursor-pointer">
            Cancel
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<!-- Marker + popup elements are created dynamically, so these styles must be global (not scoped) -->
<style>
.cw-map-pin-wrap {
  background: transparent;
  border: none;
}
.cw-map-pin {
  position: relative;
  width: 26px;
  height: 26px;
  border-radius: 50% 50% 50% 0;
  transform: rotate(-45deg);
  border: 2px solid #fff;
  box-shadow: 0 2px 6px rgba(0, 0, 0, 0.35);
  cursor: pointer;
}
.cw-map-pin::after {
  content: '';
  position: absolute;
  inset: 0;
  margin: auto;
  width: 8px;
  height: 8px;
  border-radius: 50%;
  background: #fff;
}
.cw-map-popup {
  padding: 2px 0;
}
.cw-map-popup-label {
  font-size: 13px;
  font-weight: 600;
  color: #111827;
  margin: 0;
}
.cw-map-popup-sub {
  font-size: 11px;
  color: #6b7280;
  margin: 2px 0 0;
}
.leaflet-popup-content-wrapper {
  border-radius: 10px;
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.18);
}
.dark .leaflet-popup-content-wrapper,
.dark .leaflet-popup-tip {
  background: #1f2937;
}
.dark .cw-map-popup-label {
  color: #f9fafb;
}
.dark .cw-map-popup-sub {
  color: #9ca3af;
}
/* Dark mode map: invert the raster tiles so the map follows the app theme */
.dark .cw-map .leaflet-tile-pane {
  filter: invert(1) hue-rotate(200deg) brightness(0.9) contrast(0.9);
}
/* Fullscreen mode */
.cw-map-fullscreen {
  position: fixed;
  inset: 0;
  z-index: 90;
  border-radius: 0;
  border: none;
  height: 100vh !important;
}
</style>
