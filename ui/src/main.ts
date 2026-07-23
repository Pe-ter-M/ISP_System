import './assets/main.css'
import { createApp } from 'vue'
import { createPinia } from 'pinia'
import App from './App.vue'
import router from './router'
import { useAuthStore } from './stores/auth.store'

const app = createApp(App)
app.use(createPinia())
app.use(router)

// Restore auth session from localStorage before mounting
const auth = useAuthStore()
if (auth.token) {
  auth.restoreSession()
}

app.mount('#app')
