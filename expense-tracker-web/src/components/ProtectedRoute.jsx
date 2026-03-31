import { Navigate, useLocation } from 'react-router-dom'
import { useAuth } from '../hooks/useAuth.js'

export function ProtectedRoute({ children }) {
  const { bootstrapped, isAuthenticated } = useAuth()
  const location = useLocation()

  if (!bootstrapped) {
    return <div className="page-shell">Loading your session...</div>
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" replace state={{ from: location.pathname }} />
  }

  return children
}
