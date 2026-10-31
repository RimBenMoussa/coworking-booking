import { useCallback, useEffect, useState } from 'react'
import { api, errorMessage } from './api'

// Charge une ressource GET de l'API et expose { data, error, loading, reload }.
// La requête est relancée quand l'URL change (filtres) ou quand on appelle reload().
export function useApi<T>(url: string) {
  const [data, setData] = useState<T | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(true)
  const [version, setVersion] = useState(0)

  useEffect(() => {
    // Si l'URL change avant la réponse, on ignore l'ancienne réponse.
    let ignore = false
    api
      .get<T>(url)
      .then((response) => {
        if (ignore) return
        setData(response.data)
        setError(null)
      })
      .catch((e: unknown) => {
        if (!ignore) setError(errorMessage(e))
      })
      .finally(() => {
        if (!ignore) setLoading(false)
      })
    return () => {
      ignore = true
    }
  }, [url, version])

  const reload = useCallback(() => {
    setLoading(true)
    setVersion((v) => v + 1)
  }, [])

  return { data, error, loading, reload }
}
