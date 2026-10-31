import axios, { AxiosError } from 'axios'

// URL de l'API lue au build depuis VITE_API_URL (fichier .env.development.local en local).
export const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL ?? 'http://localhost:5080',
})

export const TOKEN_KEY = 'coworking.adminToken'

// Ajoute le jeton du gestionnaire à chaque requête.
api.interceptors.request.use((config) => {
  const token = localStorage.getItem(TOKEN_KEY)
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

// Jeton expiré : on renvoie le gestionnaire vers la page de connexion.
api.interceptors.response.use(
  (response) => response,
  (error: AxiosError) => {
    if (error.response?.status === 401 && error.config?.url?.startsWith('/api/admin')) {
      localStorage.removeItem(TOKEN_KEY)
      window.location.assign('/admin/login')
    }
    return Promise.reject(error)
  },
)

interface ProblemDetails {
  title?: string
  detail?: string
  errors?: Record<string, string[]>
}

// Message lisible à partir d'une erreur de l'API (format ProblemDetails).
export function errorMessage(error: unknown): string {
  if (axios.isAxiosError(error)) {
    if (!error.response) return "Impossible de joindre le serveur. Vérifiez votre connexion et réessayez."
    const problem = error.response.data as ProblemDetails | undefined
    if (problem?.errors) return Object.values(problem.errors).flat().join(' ')
    if (problem?.detail) return problem.detail
    if (problem?.title) return problem.title
  }
  return 'Une erreur inattendue est survenue.'
}
