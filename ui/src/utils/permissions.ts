/**
 * Permission assignment helpers enforcing the "view-first" rule:
 * a non-view permission (e.g. "customer.update") may only be assigned when its
 * resource's "view" permission (e.g. "customer.view") is present. A "view"
 * permission may be granted alone, but not the others without it.
 */

/** e.g. "customer.update" -> "customer" */
export function resourceOf(code: string): string {
  return code.split('.')[0] ?? ''
}

/** e.g. "customer.update" -> "customer.view" */
export function viewCodeOf(code: string): string {
  return `${resourceOf(code)}.view`
}

/** True when the code is a "view" permission (last segment is "view"). */
export function isViewCode(code: string): boolean {
  const parts = code.split('.')
  return parts[parts.length - 1] === 'view'
}

export interface ToggleValidation {
  ok: boolean
  message?: string
}

/**
 * Compute the set of currently-selected permissions that are INVALID under the
 * view-first rule — i.e. a non-view permission (e.g. "customer.update") whose
 * resource "view" permission (e.g. "customer.view") is not selected.
 *
 * These permissions may remain selected (shown in red) so the user remembers
 * what they were trying to do; they become valid once the resource view is
 * added, and saving is blocked while any invalid code remains.
 *
 * @param codes the currently-selected permission codes
 * @param allCodes the full catalogue of permission codes (to know whether a
 *                 resource actually has a "view" permission)
 */
export function computeInvalidCodes(codes: Iterable<string>, allCodes: Iterable<string>): Set<string> {
  const set = new Set(codes)
  const catalogue = new Set(allCodes)
  const invalid = new Set<string>()
  for (const code of set) {
    if (isViewCode(code)) continue
    const viewCode = viewCodeOf(code)
    // Only enforce when the resource actually has a "view" permission in the catalogue.
    if (!catalogue.has(viewCode)) continue
    if (!set.has(viewCode)) invalid.add(code)
  }
  return invalid
}
