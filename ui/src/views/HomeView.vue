<script setup lang="tsx">
import { useOrganizationStore } from '@/stores/organization.store'
import { onMounted, ref } from 'vue'

const organizationStore = useOrganizationStore()
const loading = ref(true)
const error = ref<string | null>(null)

onMounted(async () => {
  try {
    await organizationStore.load()
    error.value = null
  } catch (e: any) {
    error.value = e?.message || 'Failed to load organization data'
  } finally {
    loading.value = false
  }
})

const renderField = (label: string, value: any) => {
  return (
    <div class="flex justify-between py-2 border-b border-gray-200">
      <span class="font-semibold text-gray-700">{label}:</span>
      <span class="text-gray-600">{value || 'N/A'}</span>
    </div>
  )
}
</script>

<template>
  <div class="max-w-4xl mx-auto p-8">
    <h1 class="text-4xl font-bold text-gray-800 mb-2">PhantomNet ISP Manager</h1>
    <p class="text-gray-500 text-lg mb-8">Welcome to your ISP Management System</p>

    <div v-if="loading" class="text-center py-8">
      <p class="text-gray-500 text-lg">Loading organization data...</p>
    </div>

    <div v-else-if="error" class="bg-red-50 border border-red-200 rounded-lg p-6">
      <h2 class="text-xl font-bold text-red-700 mb-2">Error</h2>
      <p class="text-red-600">{{ error }}</p>
    </div>

    <div v-else class="bg-white rounded-lg shadow-lg p-6">
      <h2 class="text-2xl font-bold text-gray-800 mb-6">Organization Details</h2>
      
      <div v-if="organizationStore.setupCompleted" class="space-y-2">
        <div class="flex justify-between py-2 border-b border-gray-200">
          <span class="font-semibold text-gray-700">ID:</span>
          <span class="text-gray-600">{{ organizationStore.id }}</span>
        </div>
        
        <div class="flex justify-between py-2 border-b border-gray-200">
          <span class="font-semibold text-gray-700">Organization Name:</span>
          <span class="text-gray-600">{{ organizationStore.name }}</span>
        </div>
        
        <div class="flex justify-between py-2 border-b border-gray-200">
          <span class="font-semibold text-gray-700">Short Name:</span>
          <span class="text-gray-600">{{ organizationStore.shortName || 'N/A' }}</span>
        </div>
        
        <div class="flex justify-between py-2 border-b border-gray-200">
          <span class="font-semibold text-gray-700">Tagline:</span>
          <span class="text-gray-600">{{ organizationStore.tagline || 'N/A' }}</span>
        </div>
        
        <div class="flex justify-between py-2 border-b border-gray-200">
          <span class="font-semibold text-gray-700">Currency:</span>
          <span class="text-gray-600">{{ organizationStore.currency }}</span>
        </div>
        
        <div class="flex justify-between py-2 border-b border-gray-200">
          <span class="font-semibold text-gray-700">Currency Symbol:</span>
          <span class="text-gray-600">{{ organizationStore.currencySymbol }}</span>
        </div>
        
        <div class="flex justify-between py-2 border-b border-gray-200">
          <span class="font-semibold text-gray-700">Timezone:</span>
          <span class="text-gray-600">{{ organizationStore.timezone }}</span>
        </div>
        
        <div class="flex justify-between py-2 border-b border-gray-200">
          <span class="font-semibold text-gray-700">Support Email:</span>
          <span class="text-gray-600">{{ organizationStore.supportEmail || 'N/A' }}</span>
        </div>
        
        <div class="flex justify-between py-2 border-b border-gray-200">
          <span class="font-semibold text-gray-700">Support Phone:</span>
          <span class="text-gray-600">{{ organizationStore.supportPhone || 'N/A' }}</span>
        </div>
        
        <div class="flex justify-between py-2 border-b border-gray-200">
          <span class="font-semibold text-gray-700">Address:</span>
          <span class="text-gray-600">{{ organizationStore.address || 'N/A' }}</span>
        </div>
        
        <div class="flex justify-between py-2 border-b border-gray-200">
          <span class="font-semibold text-gray-700">Created At:</span>
          <span class="text-gray-600">{{ organizationStore.createdAt }}</span>
        </div>
        
        <div class="flex justify-between py-2 border-b border-gray-200">
          <span class="font-semibold text-gray-700">Updated At:</span>
          <span class="text-gray-600">{{ organizationStore.updatedAt }}</span>
        </div>
      </div>
      
      <div v-else class="text-center py-8">
        <p class="text-gray-500 text-lg">Organization setup is not completed yet</p>
      </div>
    </div>
  </div>
</template>
