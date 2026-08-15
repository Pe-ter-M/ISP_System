/**
 * Human-friendly date formatting helpers, shared across views so dates render
 * consistently (e.g. "5th November 2026" or "5 Nov 2026, 3:45 PM").
 */

/** "13th September 2026" */
export function formatDate(iso: string | null | undefined): string {
  if (!iso) return '—'
  const d = new Date(iso)
  if (Number.isNaN(d.getTime())) return '—'
  const day = d.getDate()
  const suffix =
    day % 10 === 1 && day !== 11 ? 'st'
    : day % 10 === 2 && day !== 12 ? 'nd'
    : day % 10 === 3 && day !== 13 ? 'rd'
    : 'th'
  const month = d.toLocaleString('en-GB', { month: 'long' })
  return `${day}${suffix} ${month} ${d.getFullYear()}`
}

/** "13 Sep 2026" — compact form for tight spaces (tables, lists) */
export function formatDateShort(iso: string | null | undefined): string {
  if (!iso) return '—'
  const d = new Date(iso)
  if (Number.isNaN(d.getTime())) return '—'
  const day = d.getDate()
  const month = d.toLocaleString('en-GB', { month: 'short' })
  return `${day} ${month} ${d.getFullYear()}`
}

/** "5th November 2026, 3:45 PM" — date with a 12-hour time */
export function formatDateTime(iso: string | null | undefined): string {
  if (!iso) return '—'
  const d = new Date(iso)
  if (Number.isNaN(d.getTime())) return '—'
  const day = d.getDate()
  const suffix =
    day % 10 === 1 && day !== 11 ? 'st'
    : day % 10 === 2 && day !== 12 ? 'nd'
    : day % 10 === 3 && day !== 13 ? 'rd'
    : 'th'
  const month = d.toLocaleString('en-GB', { month: 'long' })
  const time = d.toLocaleString('en-GB', {
    hour: 'numeric',
    minute: '2-digit',
    hour12: true,
  })
  return `${day}${suffix} ${month} ${d.getFullYear()}, ${time}`
}
