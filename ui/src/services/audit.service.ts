import api from './api'
import type {
  AuditLogDetail,
  AuditLogListItem,
  AuditQuery,
  PagedAuditLogs,
} from '@/types/audit.types'

/** Paginated audit log listing with server-side filter/search/sort */
export async function getAuditLogs(query: AuditQuery = {}): Promise<PagedAuditLogs> {
  const res = await api.get('/audit', {
    params: {
      page: query.page ?? 1,
      pageSize: query.pageSize ?? 20,
      entityType: query.entityType || undefined,
      action: query.action || undefined,
      search: query.search || undefined,
      sortBy: query.sortBy || undefined,
      sortDesc: query.sortDesc ?? true,
    },
  })
  return res.data as PagedAuditLogs
}

/** Full detail of a single audit entry (actor, IP, changes, entity label) */
export async function getAuditLog(id: number): Promise<AuditLogDetail> {
  const res = await api.get(`/audit/${id}`)
  return res.data as AuditLogDetail
}

/** Human label for an entity type (display filter options). */
export function entityTypeLabel(entityType: string): string {
  const map: Record<string, string> = {
    auth: 'Auth',
    customer: 'Customer',
    staff: 'Staff',
    user: 'User',
    role: 'Role',
    plan: 'Plan',
    nas: 'NAS',
    setting: 'Setting',
    organization: 'Organization',
  }
  return map[entityType] ?? entityType.charAt(0).toUpperCase() + entityType.slice(1)
}

/** Human label for an action. */
export function actionLabel(action: string): string {
  const map: Record<string, string> = {
    create: 'Created',
    update: 'Updated',
    delete: 'Deleted',
    login_success: 'Login success',
    login_failure: 'Login failed',
  }
  return map[action] ?? action
}

export type AuditLogListRow = AuditLogListItem
