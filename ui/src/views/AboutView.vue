<script setup lang="ts">
import { useOrganizationStore } from '@/stores/organization.store'

const org = useOrganizationStore()
</script>

<template>
  <div>
    <!-- ── Page Header ── -->
    <section class="text-center py-12 sm:py-16 animate-fade-in">
      <h1 class="text-4xl sm:text-5xl font-bold text-gray-900 dark:text-white mb-4">
        About {{ org.name || 'PhantomNet' }}
      </h1>
      <!-- TODO: Replace with actual mission text -->
      <p class="text-lg text-gray-500 dark:text-gray-400 max-w-2xl mx-auto">
        We're dedicated to providing reliable internet solutions that connect communities and businesses across the region.
      </p>
      <!-- TODO: Later — populate with real mission text -->
      <!-- TODO: Later — add company history/timeline -->
    </section>

    <!-- ── Stats Section ── -->
    <section class="py-12 sm:py-16">
      <div class="grid grid-cols-2 lg:grid-cols-4 gap-6">
        <!-- TODO: Replace placeholder stats with real dashboard data when available -->
        <div class="bg-white dark:bg-gray-900 rounded-xl p-6 text-center shadow-md border border-gray-100 dark:border-gray-800 transition-transform duration-300 hover:scale-105">
          <div class="text-3xl sm:text-4xl font-bold text-blue-600 dark:text-blue-400 mb-2">500+</div>
          <div class="text-gray-500 dark:text-gray-400 text-sm">Happy Customers</div>
        </div>
        <div class="bg-white dark:bg-gray-900 rounded-xl p-6 text-center shadow-md border border-gray-100 dark:border-gray-800 transition-transform duration-300 hover:scale-105">
          <div class="text-3xl sm:text-4xl font-bold text-green-600 dark:text-green-400 mb-2">99.9%</div>
          <div class="text-gray-500 dark:text-gray-400 text-sm">Uptime Guarantee</div>
        </div>
        <div class="bg-white dark:bg-gray-900 rounded-xl p-6 text-center shadow-md border border-gray-100 dark:border-gray-800 transition-transform duration-300 hover:scale-105">
          <div class="text-3xl sm:text-4xl font-bold text-purple-600 dark:text-purple-400 mb-2">5+</div>
          <div class="text-gray-500 dark:text-gray-400 text-sm">Years Experience</div>
        </div>
        <div class="bg-white dark:bg-gray-900 rounded-xl p-6 text-center shadow-md border border-gray-100 dark:border-gray-800 transition-transform duration-300 hover:scale-105">
          <div class="text-3xl sm:text-4xl font-bold text-orange-600 dark:text-orange-400 mb-2">24/7</div>
          <div class="text-gray-500 dark:text-gray-400 text-sm">Support Available</div>
        </div>
      </div>

      <!-- TODO: Replace placeholder image paths when images are added -->
      <!-- TODO: Later — add actual customer success stories / testimonials -->
    </section>

    <!-- ── Values Section ── -->
    <section class="py-12 sm:py-16">
      <h2 class="text-3xl font-bold text-gray-800 dark:text-gray-100 mb-8 text-center">Our Values</h2>
      <div class="grid grid-cols-1 md:grid-cols-3 gap-6">
        <div class="bg-white dark:bg-gray-900 rounded-xl p-6 shadow-md border border-gray-100 dark:border-gray-800">
          <div class="text-2xl mb-3">🚀</div>
          <h3 class="text-lg font-semibold text-gray-800 dark:text-gray-100 mb-2">Innovation</h3>
          <p class="text-gray-500 dark:text-gray-400 text-sm">Constantly improving our technology to deliver faster and more reliable connectivity.</p>
        </div>
        <div class="bg-white dark:bg-gray-900 rounded-xl p-6 shadow-md border border-gray-100 dark:border-gray-800">
          <div class="text-2xl mb-3">🤝</div>
          <h3 class="text-lg font-semibold text-gray-800 dark:text-gray-100 mb-2">Community</h3>
          <p class="text-gray-500 dark:text-gray-400 text-sm">Building bridges through connectivity, one customer at a time.</p>
        </div>
        <div class="bg-white dark:bg-gray-900 rounded-xl p-6 shadow-md border border-gray-100 dark:border-gray-800">
          <div class="text-2xl mb-3">🎯</div>
          <h3 class="text-lg font-semibold text-gray-800 dark:text-gray-100 mb-2">Reliability</h3>
          <p class="text-gray-500 dark:text-gray-400 text-sm">We stand by our service. If you're not connected, we're working on it.</p>
        </div>
      </div>
    </section>

    <!-- ── Technology Stack ── -->
    <section class="py-12 sm:py-16">
      <h2 class="text-3xl font-bold text-gray-800 dark:text-gray-100 mb-8 text-center">Our Technology</h2>
      <div class="bg-white dark:bg-gray-900 rounded-xl p-8 shadow-md border border-gray-100 dark:border-gray-800">
        <div class="grid grid-cols-2 md:grid-cols-4 gap-6 text-center">
          <div>
            <div class="text-3xl font-bold text-blue-600 dark:text-blue-400 mb-1">.NET 9</div>
            <div class="text-xs text-gray-400 dark:text-gray-500">Backend API</div>
          </div>
          <div>
            <div class="text-3xl font-bold text-green-600 dark:text-green-400 mb-1">Vue 3</div>
            <div class="text-xs text-gray-400 dark:text-gray-500">Frontend</div>
          </div>
          <div>
            <div class="text-3xl font-bold text-purple-600 dark:text-purple-400 mb-1">PostgreSQL</div>
            <div class="text-xs text-gray-400 dark:text-gray-500">Database</div>
          </div>
          <div>
            <div class="text-3xl font-bold text-orange-600 dark:text-orange-400 mb-1">FreeRADIUS</div>
            <div class="text-xs text-gray-400 dark:text-gray-500">Authentication</div>
          </div>
        </div>
      </div>
    </section>
  </div>
</template>

<style scoped>
@keyframes fadeIn {
  from { opacity: 0; transform: translateY(20px); }
  to { opacity: 1; transform: translateY(0); }
}
.animate-fade-in {
  animation: fadeIn 0.6s ease-out forwards;
}
</style>
