import { useState } from 'react'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import { apiRequest } from '../api/client.js'
import { useAuth } from '../hooks/useAuth.js'

export function LoginPage() {
  const navigate = useNavigate()
  const location = useLocation()
  const { saveSession } = useAuth()
  const [form, setForm] = useState({ email: '', password: '' })
  const [error, setError] = useState('')
  const [saving, setSaving] = useState(false)

  async function handleSubmit(event) {
    event.preventDefault()
    setSaving(true)
    setError('')

    try {
      const session = await apiRequest('/auth/login', { method: 'POST', body: form })
      saveSession(session)
      navigate(location.state?.from ?? '/dashboard')
    } catch (submitError) {
      setError(submitError.payload?.message ?? 'Wrong credentials.')
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="auth-page">
      <form className="auth-card" onSubmit={handleSubmit}>
        <p className="eyebrow">Welcome back</p>
        <h1>Log in</h1>
        <label>
          Email
          <input type="email" required value={form.email} onChange={(event) => setForm({ ...form, email: event.target.value })} />
        </label>
        <label>
          Password
          <input type="password" required value={form.password} onChange={(event) => setForm({ ...form, password: event.target.value })} />
        </label>
        {error ? <p className="inline-error">{error}</p> : null}
        <button type="submit" disabled={saving}>{saving ? 'Logging in...' : 'Log In'}</button>
        <Link to="/register">Create account</Link>
      </form>
    </div>
  )
}
