import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { apiRequest } from '../api/client.js'
import { useAuth } from '../hooks/useAuth.js'

export function RegisterPage() {
  const navigate = useNavigate()
  const { saveSession } = useAuth()
  const [form, setForm] = useState({ email: '', password: '', confirmPassword: '' })
  const [error, setError] = useState('')
  const [saving, setSaving] = useState(false)

  async function handleSubmit(event) {
    event.preventDefault()
    setSaving(true)
    setError('')

    try {
      const session = await apiRequest('/auth/register', { method: 'POST', body: form })
      saveSession(session)
      navigate('/dashboard')
    } catch (submitError) {
      const validationErrors = submitError.payload?.errors
      const firstError = validationErrors ? Object.values(validationErrors).flat()[0] : null
      setError(firstError ?? submitError.message)
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="auth-page">
      <form className="auth-card" onSubmit={handleSubmit}>
        <p className="eyebrow">Create your account</p>
        <h1>Start tracking your expenses in minutes</h1>
        <label>
          Email
          <input type="email" required value={form.email} onChange={(event) => setForm({ ...form, email: event.target.value })} />
        </label>
        <label>
          Password
          <input type="password" minLength={8} required value={form.password} onChange={(event) => setForm({ ...form, password: event.target.value })} />
        </label>
        <label>
          Confirm Password
          <input type="password" minLength={8} required value={form.confirmPassword} onChange={(event) => setForm({ ...form, confirmPassword: event.target.value })} />
        </label>
        {error ? <p className="inline-error">{error}</p> : null}
        <button type="submit" disabled={saving}>{saving ? 'Creating...' : 'Create Account'}</button>
        <Link to="/login">Already have an account? Log in</Link>
      </form>
    </div>
  )
}
