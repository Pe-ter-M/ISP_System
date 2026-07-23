<script setup lang="ts">
import { useOrganizationStore } from '@/stores/organization.store'
import { useRouter } from 'vue-router'

const org = useOrganizationStore()
const router = useRouter()
</script>

<template>
  <div>
    <!-- ── Hero Section ── -->
    <section class="flex flex-col lg:flex-row items-center gap-12 py-8 sm:py-16">
      <!-- Text Content -->
      <div class="flex-1 space-y-6 animate-fade-in">
        <h1 class="text-4xl sm:text-5xl lg:text-6xl font-bold text-gray-900 dark:text-white leading-tight transition-colors duration-300">
          {{ org.tagline || 'Connect with confidence' }}
        </h1>
        <p class="text-lg sm:text-xl text-gray-600 dark:text-gray-300 leading-relaxed">
          Welcome to <span class="font-semibold text-blue-600 dark:text-blue-400">{{ org.name || 'PhantomNet' }}</span> — your reliable internet service management platform. 
          We provide seamless connectivity solutions tailored for your needs.
        </p>
        <div class="flex flex-wrap gap-4 pt-4">
          <router-link
            to="/contact"
            class="px-6 py-3 bg-blue-600 hover:bg-blue-700 text-white font-semibold rounded-lg transition-all duration-200 hover:scale-105 shadow-lg hover:shadow-blue-500/25"
          >
            Get Started
          </router-link>
          <router-link
            to="/about"
            class="px-6 py-3 bg-gray-100 dark:bg-gray-800 text-gray-700 dark:text-gray-200 font-semibold rounded-lg transition-all duration-200 hover:scale-105 border border-gray-200 dark:border-gray-700"
          >
            Learn More
          </router-link>
        </div>
      </div>

      <!-- Hero Image / Illustration -->
      <!-- TODO: Replace inline SVG with actual image from assets/public -->
      <div class="flex-1 flex justify-center animate-float">
        <div class="w-72 h-72 sm:w-80 sm:h-80 lg:w-96 lg:h-96 relative">
          <!-- WiFi illustration placeholder -->
          <div class="absolute inset-0 bg-gradient-to-br from-blue-400 to-blue-600 dark:from-blue-500 dark:to-blue-800 rounded-3xl rotate-6 opacity-20 dark:opacity-30"></div>
          <div class="absolute inset-0 bg-white dark:bg-gray-800 rounded-3xl shadow-xl flex items-center justify-center border border-gray-100 dark:border-gray-700">
            <!-- WiFi icon made with divs -->
            <div class="relative flex flex-col items-center">
              <div class="flex gap-1 mb-1">
                <div class="w-1 h-1 rounded-full bg-blue-500 dark:bg-blue-400"></div>
              </div>
              <svg class="w-32 h-32 sm:w-40 sm:h-40 text-blue-500 dark:text-blue-400" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round">
                <path d="M5 12.55a11 11 0 0 1 14.08 0" />
                <path d="M1.42 9a16 16 0 0 1 21.16 0" />
                <path d="M8.53 16.11a6 6 0 0 1 6.95 0" />
                <circle cx="12" cy="20" r="1" fill="currentColor" />
              </svg>
              <p class="mt-4 text-sm text-gray-400 dark:text-gray-500 font-medium">Reliable Connection</p>
            </div>
          </div>
        </div>
      </div>
    </section>

    <!-- ── Features Section ── -->
    <section class="py-16 sm:py-20">
      <div class="text-center mb-12">
        <h2 class="text-3xl sm:text-4xl font-bold text-gray-800 dark:text-gray-100 mb-4">Why Choose {{ org.shortName || 'Us' }}?</h2>
        <p class="text-gray-500 dark:text-gray-400 max-w-2xl mx-auto">Everything you need to manage your internet service provider business in one place.</p>
      </div>

      <!-- TODO: Replace feature icons with actual images from assets when available -->
      <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6 sm:gap-8">
        <!-- Feature 1 -->
        <div class="group bg-white dark:bg-gray-900 rounded-xl p-6 shadow-md hover:shadow-xl transition-all duration-300 hover:-translate-y-1 border border-gray-100 dark:border-gray-800">
          <div class="w-12 h-12 bg-blue-100 dark:bg-blue-900/50 rounded-lg flex items-center justify-center mb-4 transition-transform duration-300 group-hover:scale-110">
            <svg class="w-6 h-6 text-blue-600 dark:text-blue-400" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
              <path stroke-linecap="round" stroke-linejoin="round" d="M13 10V3L4 14h7v7l9-11h-7z" />
            </svg>
          </div>
          <h3 class="text-lg font-semibold text-gray-800 dark:text-gray-100 mb-2">Fast Provisioning</h3>
          <p class="text-gray-500 dark:text-gray-400 text-sm leading-relaxed">Create and manage customer subscriptions in seconds. Automated RADIUS integration for instant connectivity.</p>
        </div>

        <!-- Feature 2 -->
        <div class="group bg-white dark:bg-gray-900 rounded-xl p-6 shadow-md hover:shadow-xl transition-all duration-300 hover:-translate-y-1 border border-gray-100 dark:border-gray-800">
          <div class="w-12 h-12 bg-green-100 dark:bg-green-900/50 rounded-lg flex items-center justify-center mb-4 transition-transform duration-300 group-hover:scale-110">
            <svg class="w-6 h-6 text-green-600 dark:text-green-400" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
              <path stroke-linecap="round" stroke-linejoin="round" d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z" />
            </svg>
          </div>
          <h3 class="text-lg font-semibold text-gray-800 dark:text-gray-100 mb-2">Secure & Reliable</h3>
          <p class="text-gray-500 dark:text-gray-400 text-sm leading-relaxed">JWT authentication, role-based access control, and encrypted data storage. Your business data stays protected.</p>
        </div>

        <!-- Feature 3 -->
        <div class="group bg-white dark:bg-gray-900 rounded-xl p-6 shadow-md hover:shadow-xl transition-all duration-300 hover:-translate-y-1 border border-gray-100 dark:border-gray-800">
          <div class="w-12 h-12 bg-purple-100 dark:bg-purple-900/50 rounded-lg flex items-center justify-center mb-4 transition-transform duration-300 group-hover:scale-110">
            <svg class="w-6 h-6 text-purple-600 dark:text-purple-400" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
              <path stroke-linecap="round" stroke-linejoin="round" d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z" />
            </svg>
          </div>
          <h3 class="text-lg font-semibold text-gray-800 dark:text-gray-100 mb-2">Smart Analytics</h3>
          <p class="text-gray-500 dark:text-gray-400 text-sm leading-relaxed">Real-time usage tracking, revenue reports, and actionable insights to help grow your ISP business.</p>
        </div>
      </div>
    </section>

    <!-- ── CTA Section ── -->
    <section class="py-16 sm:py-20">
      <div class="bg-gradient-to-r from-blue-600 to-blue-800 dark:from-blue-700 dark:to-blue-900 rounded-2xl p-8 sm:p-12 text-center text-white shadow-xl">
        <h2 class="text-3xl sm:text-4xl font-bold mb-4">Ready to get started?</h2>
        <p class="text-blue-100 text-lg mb-8 max-w-2xl mx-auto">Join thousands of satisfied customers who trust {{ org.name || 'PhantomNet' }} for their internet needs.</p>
        <!-- TODO: Add proper signup/contact flow -->
        <router-link
          to="/contact"
          class="inline-block px-8 py-3 bg-white text-blue-700 font-semibold rounded-lg transition-all duration-200 hover:scale-105 hover:shadow-xl"
        >
          Contact Us Today
        </router-link>
      </div>
    </section>
  </div>
</template>

<style scoped>
@keyframes fadeIn {
  from { opacity: 0; transform: translateY(20px); }
  to { opacity: 1; transform: translateY(0); }
}
@keyframes float {
  0%, 100% { transform: translateY(0); }
  50% { transform: translateY(-10px); }
}
.animate-fade-in {
  animation: fadeIn 0.8s ease-out forwards;
}
.animate-float {
  animation: float 4s ease-in-out infinite;
}
</style>
