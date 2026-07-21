import axios from 'axios'

const api = axios.create({
  baseURL: '/api',
})

api.interceptors.request.use((config) => {
  const token = localStorage.getItem('auth_token')
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

api.interceptors.response.use(
  (res) => {
    if (res.data?.status === 'error') {
      return Promise.reject(res.data)
    }
    // The interceptor actually returns AxiosResponse,
    // but we change the data property to be the unwrapped value
    res.data = res.data?.data ?? res.data
    return res
  },
  (err) => {
    if (err.response?.status === 401) {
      localStorage.removeItem('auth_token')
      window.location.href = '/login'
    }
    return Promise.reject(err.response?.data || err)
  },
)

export default api
