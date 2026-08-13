/**
 * Central map configuration — free, no-API-key providers only.
 * Swap any URL here if you later self-host tiles or switch providers.
 */
export const MAP_CONFIG = {
  /** OSM standard raster tiles (free, no key, attribution required, moderate use) */
  tileUrl: 'https://tile.openstreetmap.org/{z}/{x}/{y}.png',
  tileAttribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors',
  /** OSM Nominatim — free reverse/forward geocoding (usage policy: 1 request/second) */
  nominatimBase: 'https://nominatim.openstreetmap.org',
  /** Default view when no coordinates are set (Nairobi, Kenya) */
  defaultCenter: { lat: -1.2864, lng: 36.8172 },
  defaultZoom: 15,
} as const
