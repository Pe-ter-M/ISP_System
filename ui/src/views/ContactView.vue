<script setup lang="ts">
import { useOrganizationStore } from '@/stores/organization.store'
import { ref } from 'vue'

const org = useOrganizationStore()

const form = ref({
  name: '',
  email: '',
  subject: '',
  message: ''
})

const submitted = ref(false)

function handleSubmit() {
  // TODO: Later — integrate with email service / API endpoint
  submitted.value = true
}
</script>

<template>
  <div>
    <!-- ── Page Header ── -->
    <section class="text-center py-12 sm:py-16 animate-fade-in">
      <h1 class="text-4xl sm:text-5xl font-bold text-gray-900 dark:text-white mb-4">Contact Us</h1>
      <p class="text-lg text-gray-500 dark:text-gray-400 max-w-2xl mx-auto">
        Have a question or need help? We're here for you.
      </p>
    </section>

    <!-- ── Contact Form + Info ── -->
    <section class="grid grid-cols-1 lg:grid-cols-2 gap-8 sm:gap-12">
      <!-- Contact Form -->
      <div class="bg-white dark:bg-gray-900 rounded-xl p-6 sm:p-8 shadow-md border border-gray-100 dark:border-gray-800">
        <h2 class="text-2xl font-bold text-gray-800 dark:text-gray-100 mb-6">Send us a message</h2>

        <form v-if="!submitted" @submit.prevent="handleSubmit" class="space-y-5">
          <div>
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Your Name</label>
            <input
              v-model="form.name"
              type="text"
              required
              placeholder="John Kamau"
              class="w-full px-4 py-2.5 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 focus:ring-2 focus:ring-blue-500 focus:border-transparent outline-none transition-all duration-200"
            />
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Your Email</label>
            <input
              v-model="form.email"
              type="email"
              required
              placeholder="john@example.com"
              class="w-full px-4 py-2.5 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 focus:ring-2 focus:ring-blue-500 focus:border-transparent outline-none transition-all duration-200"
            />
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Subject</label>
            <input
              v-model="form.subject"
              type="text"
              required
              placeholder="How can we help?"
              class="w-full px-4 py-2.5 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 focus:ring-2 focus:ring-blue-500 focus:border-transparent outline-none transition-all duration-200"
            />
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Message</label>
            <textarea
              v-model="form.message"
              required
              rows="4"
              placeholder="Tell us more about your inquiry..."
              class="w-full px-4 py-2.5 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 focus:ring-2 focus:ring-blue-500 focus:border-transparent outline-none transition-all duration-200 resize-none"
            ></textarea>
          </div>
          <button
            type="submit"
            class="w-full px-6 py-3 bg-blue-600 hover:bg-blue-700 text-white font-semibold rounded-lg transition-all duration-200 hover:scale-[1.02] shadow-lg"
          >
            Send Message
          </button>
        </form>

        <div v-else class="text-center py-12 space-y-4">
          <div class="text-4xl">✅</div>
          <h3 class="text-xl font-semibold text-gray-800 dark:text-gray-100">Message Sent!</h3>
          <p class="text-gray-500 dark:text-gray-400">Thank you for reaching out. We'll get back to you shortly.</p>
          <button
            @click="submitted = false; form = { name: '', email: '', subject: '', message: '' }"
            class="text-blue-600 dark:text-blue-400 hover:underline font-medium"
          >
            Send another message
          </button>
        </div>
        <!-- TODO: Later — integrate with email service -->
      </div>

      <!-- Organization Contact Info -->
      <div class="space-y-6">
        <div class="bg-white dark:bg-gray-900 rounded-xl p-6 sm:p-8 shadow-md border border-gray-100 dark:border-gray-800">
          <h2 class="text-2xl font-bold text-gray-800 dark:text-gray-100 mb-6">Get in touch</h2>

          <div class="space-y-6">
            <!-- Email -->
            <div class="flex items-start gap-4">
              <div class="w-10 h-10 bg-blue-100 dark:bg-blue-900/50 rounded-lg flex items-center justify-center flex-shrink-0 mt-1">
                <svg class="w-5 h-5 text-blue-600 dark:text-blue-400" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                  <path stroke-linecap="round" stroke-linejoin="round" d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
                </svg>
              </div>
              <div>
                <h3 class="font-semibold text-gray-800 dark:text-gray-100">Email</h3>
                <p class="text-gray-500 dark:text-gray-400">{{ org.supportEmail || 'support@phantomnet.co.ke' }}</p>
                <!-- TODO: Replace fallback email with actual org data when available -->
              </div>
            </div>

            <!-- Phone -->
            <div class="flex items-start gap-4">
              <div class="w-10 h-10 bg-green-100 dark:bg-green-900/50 rounded-lg flex items-center justify-center flex-shrink-0 mt-1">
                <svg class="w-5 h-5 text-green-600 dark:text-green-400" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                  <path stroke-linecap="round" stroke-linejoin="round" d="M3 5a2 2 0 012-2h3.28a1 1 0 01.948.684l1.498 4.493a1 1 0 01-.502 1.21l-2.257 1.13a11.042 11.042 0 005.516 5.516l1.13-2.257a1 1 0 011.21-.502l4.493 1.498a1 1 0 01.684.949V19a2 2 0 01-2 2h-1C9.716 21 3 14.284 3 6V5z" />
                </svg>
              </div>
              <div>
                <h3 class="font-semibold text-gray-800 dark:text-gray-100">Phone</h3>
                <p class="text-gray-500 dark:text-gray-400">{{ org.supportPhone || '+254 700 000 000' }}</p>
              </div>
            </div>

            <!-- Address -->
            <div class="flex items-start gap-4">
              <div class="w-10 h-10 bg-purple-100 dark:bg-purple-900/50 rounded-lg flex items-center justify-center flex-shrink-0 mt-1">
                <svg class="w-5 h-5 text-purple-600 dark:text-purple-400" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                  <path stroke-linecap="round" stroke-linejoin="round" d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z" />
                  <path stroke-linecap="round" stroke-linejoin="round" d="M15 11a3 3 0 11-6 0 3 3 0 016 0z" />
                </svg>
              </div>
              <div>
                <h3 class="font-semibold text-gray-800 dark:text-gray-100">Address</h3>
                <p class="text-gray-500 dark:text-gray-400">{{ org.address || 'Nairobi, Kenya' }}</p>
                <!-- TODO: Add Google Maps / OpenStreetMap embed -->
              </div>
            </div>
          </div>
        </div>

        <!-- Business Hours -->
        <div class="bg-white dark:bg-gray-900 rounded-xl p-6 sm:p-8 shadow-md border border-gray-100 dark:border-gray-800">
          <h2 class="text-2xl font-bold text-gray-800 dark:text-gray-100 mb-4">Business Hours</h2>
          <div class="space-y-2 text-gray-500 dark:text-gray-400">
            <div class="flex justify-between">
              <span class="font-medium text-gray-700 dark:text-gray-300">Monday - Friday</span>
              <span>8:00 AM - 6:00 PM</span>
            </div>
            <div class="flex justify-between">
              <span class="font-medium text-gray-700 dark:text-gray-300">Saturday</span>
              <span>9:00 AM - 4:00 PM</span>
            </div>
            <div class="flex justify-between">
              <span class="font-medium text-gray-700 dark:text-gray-300">Sunday</span>
              <span>Closed</span>
            </div>
          </div>
        </div>

        <!-- Social Links Placeholder -->
        <div class="bg-white dark:bg-gray-900 rounded-xl p-6 sm:p-8 shadow-md border border-gray-100 dark:border-gray-800">
          <h2 class="text-2xl font-bold text-gray-800 dark:text-gray-100 mb-4">Follow Us</h2>
          <!-- TODO: Add actual social media links / icons -->
          <div class="flex gap-4 text-gray-400 dark:text-gray-500">
            <span class="p-2 bg-gray-100 dark:bg-gray-800 rounded-lg hover:bg-blue-100 dark:hover:bg-blue-900/30 hover:text-blue-600 dark:hover:text-blue-400 transition-all cursor-pointer">
              📘 Facebook
            </span>
            <span class="p-2 bg-gray-100 dark:bg-gray-800 rounded-lg hover:bg-blue-100 dark:hover:bg-blue-900/30 hover:text-blue-600 dark:hover:text-blue-400 transition-all cursor-pointer">
              🐦 Twitter
            </span>
            <span class="p-2 bg-gray-100 dark:bg-gray-800 rounded-lg hover:bg-blue-100 dark:hover:bg-blue-900/30 hover:text-blue-600 dark:hover:text-blue-400 transition-all cursor-pointer">
              📷 Instagram
            </span>
          </div>
          <!-- TODO: Later — replace with actual social media SVG icons -->
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
