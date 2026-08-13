import { MAP_CONFIG } from '@/config/map.config'

export interface ReverseGeocodeResult {
  displayName: string
  city: string | null
  region: string | null
  country: string | null
}

export interface PlaceSearchResult {
  lat: number
  lng: number
  displayName: string
}

/** Session caches — repeated lookups (e.g. reopening a modal) never hit the network */
const cache = new Map<string, ReverseGeocodeResult>()
const searchCache = new Map<string, PlaceSearchResult[]>()
let lastRequestAt = 0

function wait(ms: number) {
  return new Promise(resolve => setTimeout(resolve, ms))
}

/**
 * Reverse geocode coordinates into a human-readable location.
 * Free OSM Nominatim endpoint — no API key. Respects the 1 request/second usage policy.
 * Returns null when the lookup fails (offline / blocked), so callers can fall back.
 */
export async function reverseGeocode(lat: number, lng: number): Promise<ReverseGeocodeResult | null> {
  const key = `${lat.toFixed(5)},${lng.toFixed(5)}`
  const hit = cache.get(key)
  if (hit) return hit

  const since = Date.now() - lastRequestAt
  if (since < 1100) await wait(1100 - since)
  lastRequestAt = Date.now()

  try {
    const url = `${MAP_CONFIG.nominatimBase}/reverse?format=jsonv2&lat=${lat.toFixed(6)}&lon=${lng.toFixed(6)}&zoom=18&addressdetails=1`
    const res = await fetch(url, { headers: { Accept: 'application/json' } })
    if (!res.ok) return null
    const data = await res.json()
    const a = data.address ?? {}
    const result: ReverseGeocodeResult = {
      displayName: String(data.display_name ?? ''),
      city: a.city ?? a.town ?? a.village ?? a.municipality ?? null,
      region: a.state ?? a.county ?? a.country ?? null,
      country: a.country ?? null,
    }
    cache.set(key, result)
    return result
  } catch {
    return null
  }
}

/**
 * Forward geocode — search for a place by name/address.
 * Free OSM Nominatim endpoint — no API key. Shares the 1 req/s throttle with reverse geocoding.
 */
export async function searchPlaces(query: string): Promise<PlaceSearchResult[]> {
  const q = query.trim()
  if (q.length < 3) return []
  const key = `q:${q.toLowerCase()}`
  const hit = searchCache.get(key)
  if (hit) return hit

  const since = Date.now() - lastRequestAt
  if (since < 1100) await wait(1100 - since)
  lastRequestAt = Date.now()

  try {
    const url = `${MAP_CONFIG.nominatimBase}/search?format=jsonv2&q=${encodeURIComponent(q)}&limit=6&addressdetails=1`
    const res = await fetch(url, { headers: { Accept: 'application/json' } })
    if (!res.ok) return []
    const data: unknown = await res.json()
    const items = Array.isArray(data) ? data : []
    const results: PlaceSearchResult[] = items.map((r: Record<string, unknown>) => ({
      lat: Number(r.lat),
      lng: Number(r.lon),
      displayName: String(r.display_name ?? ''),
    }))
    searchCache.set(key, results)
    return results
  } catch {
    return []
  }
}
