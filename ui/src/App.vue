<script setup lang="ts">
import { RouterLink, RouterView } from 'vue-router'
</script>

<template>
  <div class="min-h-screen bg-gray-50">
    <!-- Navbar -->
    <nav class="bg-white shadow-sm border-b border-gray-200">
      <div class="max-w-5xl mx-auto px-4 py-3 flex items-center justify-between">
        <RouterLink to="/" class="text-xl font-bold text-blue-600 no-underline">
          PhantomNet
        </RouterLink>

        <div class="flex gap-6">
          <RouterLink
            to="/"
            class="text-gray-600 hover:text-blue-600 transition no-underline"
            active-class="text-blue-600 font-semibold"
          >
            Home
          </RouterLink>
          <RouterLink
            to="/about"
            class="text-gray-600 hover:text-blue-600 transition no-underline"
            active-class="text-blue-600 font-semibold"
          >
            About
          </RouterLink>
          <RouterLink
            to="/contact"
            class="text-gray-600 hover:text-blue-600 transition no-underline"
            active-class="text-blue-600 font-semibold"
          >
            Contact
          </RouterLink>
        </div>
      </div>
    </nav>

    <!-- Page Content -->
    <main class="max-w-5xl mx-auto px-4 py-10">
      <RouterView />
    </main>
  </div>
</template>
