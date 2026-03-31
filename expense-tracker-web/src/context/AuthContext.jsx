import { useEffect, useMemo, useState } from 'react'
import { apiRequest } from '../api/client.js'
import { AuthContext } from './auth-context.js'

const STORAGE_KEY = 'expense-tracker-session'

export function AuthProvider({ children }) {
  const [session, setSession] = useState(() => {
    const raw = window.localStorage.getItem(STORAGE_KEY)
    return raw ? JSON.parse(raw) : null
  })
  const [bootstrapped, setBootstrapped] = useState(false)

  useEffect(() => {
    async function bootstrap() {
      if (!session?.token) {
        setBootstrapped(true)
        return
      }

      try {
        const user = await apiRequest('/auth/me', { token: session.token })
        setSession((current) => (current ? { ...current, user } : current))
      } catch {
        window.localStorage.removeItem(STORAGE_KEY)
        setSession(null)
      } finally {
        setBootstrapped(true)
      }
    }

    bootstrap()
  }, [session?.token])

  const value = useMemo(
    () => ({
      session,
      token: session?.token ?? null,
      user: session?.user ?? null,
      isAuthenticated: Boolean(session?.token),
      bootstrapped,
      saveSession(nextSession) {
        window.localStorage.setItem(STORAGE_KEY, JSON.stringify(nextSession))
        setSession(nextSession)
      },
      clearSession() {
        window.localStorage.removeItem(STORAGE_KEY)
        setSession(null)
      },
    }),
    [bootstrapped, session],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
