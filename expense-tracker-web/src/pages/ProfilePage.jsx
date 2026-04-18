import { useCallback, useEffect, useState } from 'react'
import { apiRequest } from '../api/client.js'
import { CategoryCreator } from '../components/CategoryCreator.jsx'
import { useAuth } from '../hooks/useAuth.js'

export function ProfilePage() {
  const { token, user } = useAuth()
  const [categories, setCategories] = useState([])
  const [telegramStatus, setTelegramStatus] = useState(null)
  const [telegramLink, setTelegramLink] = useState(null)
  const [telegramError, setTelegramError] = useState('')

  const loadCategories = useCallback(async () => {
    const result = await apiRequest('/categories', { token })
    setCategories(result)
  }, [token])

  const loadTelegramStatus = useCallback(async () => {
    const result = await apiRequest('/telegram/status', { token })
    setTelegramStatus(result)
  }, [token])

  useEffect(() => {
    async function initialize() {
      await loadCategories()
      await loadTelegramStatus()
    }

    initialize()
  }, [loadCategories, loadTelegramStatus])

  async function handleCreateCategory(name) {
    await apiRequest('/categories', { method: 'POST', token, body: { name } })
    await loadCategories()
  }

  async function handleConnectTelegram() {
    setTelegramError('')
    setTelegramLink(null)
    try {
      const result = await apiRequest('/telegram/connect-token', { method: 'POST', token })
      setTelegramLink(result)
    } catch (error) {
      setTelegramError(error.message)
    }
  }

  async function handleDisconnectTelegram() {
    setTelegramError('')
    await apiRequest('/telegram/connection', { method: 'DELETE', token })
    setTelegramLink(null)
    await loadTelegramStatus()
  }

  return (
    <div className="page-grid">
      <section className="panel">
        <p className="eyebrow">Profile</p>
        <h2>Account</h2>
        <p>{user?.email}</p>
      </section>
      <section className="panel">
        <div className="panel-header">
          <div>
            <p className="eyebrow">Telegram</p>
            <h2>Connect private chat expenses</h2>
          </div>
        </div>
        {telegramStatus?.isConnected ? (
          <div className="stack-list">
            <div className="expense-row">
              <strong>Connected</strong>
              <span>Chat {telegramStatus.telegramChatId}</span>
            </div>
            <button className="danger-button" onClick={handleDisconnectTelegram}>Disconnect Telegram</button>
          </div>
        ) : (
          <div className="stack-list">
            <p>Generate a short-lived link, then open it to authorize your private Telegram chat.</p>
            <button onClick={handleConnectTelegram}>Connect Telegram</button>
            {telegramLink && (
              <div className="expense-row">
                <a href={telegramLink.deepLink} target="_blank" rel="noreferrer">Open Telegram bot</a>
                <span>Expires {new Date(telegramLink.expiresAtUtc).toLocaleTimeString()}</span>
              </div>
            )}
          </div>
        )}
        {telegramError && <p className="inline-error">{telegramError}</p>}
      </section>
      <section className="panel">
        <div className="panel-header">
          <div>
            <p className="eyebrow">Custom categories</p>
            <h2>Create categories for your own spending patterns</h2>
          </div>
        </div>
        <CategoryCreator onCreate={handleCreateCategory} />
        <div className="stack-list">
          {categories.map((category) => (
            <div key={category.id} className="expense-row">
              <strong>{category.name}</strong>
              <span>{category.type}</span>
            </div>
          ))}
        </div>
      </section>
    </div>
  )
}
