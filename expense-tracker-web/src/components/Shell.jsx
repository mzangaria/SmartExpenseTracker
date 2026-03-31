import { NavLink, useNavigate } from 'react-router-dom'
import { useAuth } from '../hooks/useAuth.js'

export function Shell({ children }) {
  const navigate = useNavigate()
  const { clearSession, user } = useAuth()

  function handleLogout() {
    clearSession()
    navigate('/login')
  }

  return (
    <div className="app-shell">
      <header className="topbar">
        <div>
          <p className="eyebrow">Smart Expense Tracker</p>
          <h1>AI-assisted financial tracking</h1>
        </div>
        <nav className="topnav">
          <NavLink to="/dashboard">Dashboard</NavLink>
          <NavLink to="/expenses">Expenses</NavLink>
          <NavLink to="/dashboard#analytics">Analytics</NavLink>
          <NavLink to="/profile">Profile</NavLink>
          <button className="ghost-button" onClick={handleLogout}>Logout</button>
        </nav>
      </header>
      <div className="identity-chip">{user?.email}</div>
      <main>{children}</main>
    </div>
  )
}
