import axios from 'axios'

export const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL ?? '/api',
  withCredentials: true,
  headers: {
    // Required by the API's CSRF defense for cookie-authenticated state-changing requests.
    'X-Requested-With': 'DocuMindClient',
  },
})

export interface ApiErrorBody {
  errors?: string[]
  title?: string
  error?: string
}

export function extractErrorMessages(error: unknown): string[] {
  if (axios.isAxiosError<ApiErrorBody>(error)) {
    const data = error.response?.data
    if (data?.errors?.length) return data.errors
    if (data?.title) return [data.title]
    if (data?.error) return [data.error]
    if (error.message) return [error.message]
  }
  return ['Something went wrong. Please try again.']
}
