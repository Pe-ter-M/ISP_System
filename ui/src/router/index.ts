import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '@/stores/auth.store'
import { getFilteredNav } from '@/config/navigation'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    // ── Public site (has header, footer) ──
    {
      path: '/',
      component: () => import('../layouts/PublicLayout.vue'),
      children: [
        { path: '', name: 'home', component: () => import('../views/HomeView.vue') },
        { path: 'about', name: 'about', component: () => import('../views/AboutView.vue') },
        { path: 'contact', name: 'contact', component: () => import('../views/ContactView.vue') },
        { path: 'plans', name: 'plans', component: () => import('../views/PlansView.vue') },
        { path: 'login', name: 'login', component: () => import('../views/LoginView.vue') },
      ],
    },

    // ── Admin app (sidebar, no public header/footer) ──
    {
      path: '/admin',
      component: () => import('../layouts/AdminLayout.vue'),
      meta: { requiresAuth: true },
      redirect: '/admin/dashboard',
      children: [
        { path: 'dashboard', name: 'dashboard', component: () => import('../views/admin/DashboardView.vue'), meta: { title: 'Dashboard' } },
        { path: 'profile', name: 'profile', component: () => import('../views/admin/ProfileView.vue'), meta: { title: 'Profile' } },
        { path: 'customers', redirect: '/admin/users/customers' },
        { path: 'subscriptions', name: 'subscriptions', component: () => import('../views/admin/SubscriptionsView.vue'), meta: { title: 'Subscriptions', requiresPermission: 'subscription.view' } },
        { path: 'plans', name: 'admin-plans', component: () => import('../views/admin/PlansView.vue'), meta: { title: 'Plans', requiresPermission: 'plan.view' } },
        { path: 'sessions', name: 'sessions', component: () => import('../views/admin/PlaceholderView.vue'), meta: { title: 'Live Sessions', requiresPermission: 'session.view' } },
        { path: 'nas', name: 'nas', component: () => import('../views/admin/NasView.vue'), meta: { title: 'NAS Clients', requiresPermission: 'radius.nas.manage' } },
        { path: 'users', name: 'admin-users', component: () => import('../views/admin/UsersView.vue'), meta: { title: 'Users', requiresPermission: 'users.view' } },
        { path: 'users/customers', name: 'admin-users-customers', component: () => import('../views/admin/CustomersView.vue'), meta: { title: 'Customers', requiresPermission: 'customer.view' } },
        { path: 'users/staff', name: 'admin-users-staff', component: () => import('../views/admin/StaffView.vue'), meta: { title: 'Staff', requiresPermission: 'staff.view' } },
        { path: 'roles', name: 'roles', component: () => import('../views/admin/RolesView.vue'), meta: { title: 'Roles', requiresPermission: 'role.manage' } },
        { path: 'audit', name: 'audit', component: () => import('../views/admin/AuditLogView.vue'), meta: { title: 'Audit Log', requiresPermission: 'audit.view' } },
        { path: 'settings', name: 'settings', component: () => import('../views/admin/SettingsView.vue'), meta: { title: 'Settings', requiresPermission: 'settings.view' } },
      ],
    },
  ],
})

// ── Auth + permission guard ──
// The dashboard route is always reachable to any authenticated admin user.
// Every other admin route carries a `requiresPermission` meta. If the signed-in
// user lacks that view permission, they are redirected to the first route they
// ARE allowed to see (so restricted roles see fewer / different URLs).
router.beforeEach((to, _from, next) => {
  const auth = useAuthStore()

  // 1. Auth required?
  if (to.meta?.requiresAuth && !auth.isAuthenticated) {
    return next('/login')
  }

  // 2. Signed-in users shouldn't see the login page
  if (to.name === 'login' && auth.isAuthenticated) {
    return next(firstPermittedRoute(auth.userPermissions))
  }

  // 3. Permission gate on admin routes
  if (to.meta?.requiresPermission) {
    const required = to.meta.requiresPermission as string
    if (!auth.userPermissions.includes(required)) {
      const fallback = firstPermittedRoute(auth.userPermissions)
      // Avoid a redirect loop when the user somehow reaches a route they can't
      // see but also has no permitted fallback at all.
      return next(fallback && fallback !== to.path ? fallback : '/admin/profile')
    }
  }

  // 4. `/admin` with no explicit child — send to the first permitted route
  if (to.path === '/admin') {
    return next(firstPermittedRoute(auth.userPermissions) || '/admin/profile')
  }

  next()
})

/** First admin route the current user is allowed to open, or null. */
function firstPermittedRoute(userPermissions: string[]): string | null {
  const nav = getFilteredNav(userPermissions)
  for (const section of nav) {
    for (const item of section.items) {
      if (item.children && item.children.length > 0) return item.children[0].path
      return item.path
    }
  }
  return null
}

export default router
