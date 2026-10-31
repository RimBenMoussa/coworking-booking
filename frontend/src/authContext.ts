import { createContext, useContext } from 'react'

export interface AuthContextValue {
  isLoggedIn: boolean
  login: (email: string, password: string) => Promise<void>
  logout: () => void
}

export const AuthContext = createContext<AuthContextValue | null>(null)

export function useAuth() {
  const context = useContext(AuthContext)
  if (!context) throw new Error('useAuth doit être utilisé dans <AuthProvider>')
  return context
}
