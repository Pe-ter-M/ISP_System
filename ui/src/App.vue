<script setup lang="ts">
import { RouterView } from 'vue-router'
import { onMounted, watchEffect } from 'vue'
import ToastHost from '@/components/ToastHost.vue'
import { useSettingsStore } from '@/stores/settings.store'
import { useCompanyInfo } from '@/composables/useCompanyInfo'

const settings = useSettingsStore()
const { companyName } = useCompanyInfo()

// Fetch public settings once on first website load; they are cached in the store
// because they rarely change, and are read all over the site (brand, contact, currency).
onMounted(() => { settings.load() })

// Keep the browser tab title in sync with the configured company name
watchEffect(() => { document.title = companyName.value })
</script>

<template>
  <RouterView />
  <ToastHost />
</template>
