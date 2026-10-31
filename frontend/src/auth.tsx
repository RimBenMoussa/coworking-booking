import { useState, type ReactNode } from 'react'
import { Navigate, useLocation } from 'react-router-dom'
import { api, TOKEN_KEY } from './api'
import { AuthContext, useAuth } from './authContext'
import type { LoginResponse } from './types'

// Le jeton du gestionnaire est gardé dans le localStorage : la session survit à un rechargement de page.
export function AuthProvider({ children }: { children: ReactNode }) {
  const [token, setToken] = useState(() => localStorage.getItem(TOKEN_KEY))

  async function login(email: string, password: string) {
    const { data } = await api.post<LoginResponse>('/api/auth/login', { email, password })
    localStorage.setItem(TOKEN_KEY, data.token)
    setToken(data.token)
  }

  function logout() {
    localStorage.removeItem(TOKEN_KEY)
    setToken(null)
  }

  return <AuthContext.Provider value={{ isLoggedIn: token !== null, login, logout }}>{children}</AuthContext.Provider>
}

// Protège les pages /admin : sans jeton, redirection vers la connexion.
export function RequireAuth({ children }: { children: ReactNode }) {
  const { isLoggedIn } = useAuth()
  const location = useLocation()
  if (!isLoggedIn) return <Navigate to="/admin/login" replace state={{ from: location.pathname }} />
  return children
}
