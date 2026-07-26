import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '@/stores/auth.store'

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
        { path: 'customers', name: 'customers', component: () => import('../views/admin/CustomersView.vue'), meta: { title: 'Customers' } },
        { path: 'subscriptions', name: 'subscriptions', component: () => import('../views/admin/PlaceholderView.vue'), meta: { title: 'Subscriptions' } },
        { path: 'plans', name: 'admin-plans', component: () => import('../views/admin/PlaceholderView.vue'), meta: { title: 'Plans' } },
        { path: 'sessions', name: 'sessions', component: () => import('../views/admin/PlaceholderView.vue'), meta: { title: 'Live Sessions' } },
        { path: 'nas', name: 'nas', component: () => import('../views/admin/PlaceholderView.vue'), meta: { title: 'NAS Clients' } },
        { path: 'users', name: 'admin-users', component: () => import('../views/admin/UsersView.vue'), meta: { title: 'Users' } },
        { path: 'roles', name: 'roles', component: () => import('../views/admin/PlaceholderView.vue'), meta: { title: 'Roles' } },
        { path: 'audit', name: 'audit', component: () => import('../views/admin/PlaceholderView.vue'), meta: { title: 'Audit Log' } },
        { path: 'settings', name: 'settings', component: () => import('../views/admin/PlaceholderView.vue'), meta: { title: 'Settings' } },
      ],
    },
  ],
})

// ── Auth guard ──
router.beforeEach((to, _from, next) => {
  const auth = useAuthStore()

  if (to.meta?.requiresAuth && !auth.isAuthenticated) {
    next('/login')
  } else if (to.name === 'login' && auth.isAuthenticated) {
    next('/admin/dashboard')
  } else {
    next()
  }
})

export default router
