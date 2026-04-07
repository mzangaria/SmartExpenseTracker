import { useCallback, useEffect, useState } from 'react'
import { apiRequest } from '../api/client.js'
import { useAuth } from '../hooks/useAuth.js'
import { notifyFinancialMessagesUpdated } from '../utils/financial-message-events.js'

const STATUS_OPTIONS = ['all', 'unread', 'read', 'dismissed', 'archived']
const TYPE_OPTIONS = ['all', 'anomaly', 'waste', 'spike', 'forecast', 'budgetwarning', 'coachtip', 'systeminsight']

export function InboxPage() {
  const { token, clearSession } = useAuth()
  const [messages, setMessages] = useState([])
  const [statusFilter, setStatusFilter] = useState('all')
  const [typeFilter, setTypeFilter] = useState('all')
  const [unreadCount, setUnreadCount] = useState(0)
  const [loading, setLoading] = useState(true)
  const [actingId, setActingId] = useState(null)

  const loadMessages = useCallback(async () => {
    setLoading(true)
    const params = new URLSearchParams()
    if (statusFilter !== 'all') {
      params.set('status', statusFilter)
    }
    if (typeFilter !== 'all') {
      params.set('type', typeFilter)
    }

    try {
      const path = params.size ? `/financial-messages?${params.toString()}` : '/financial-messages'
      const [messageResult, unreadResult] = await Promise.all([
        apiRequest(path, { token }),
        apiRequest('/financial-messages/unread-count', { token }),
      ])
      setMessages(messageResult)
      setUnreadCount(unreadResult.count)
      notifyFinancialMessagesUpdated(unreadResult.count)
    } catch (error) {
      if (error.status === 401) {
        clearSession()
      }
      throw error
    } finally {
      setLoading(false)
    }
  }, [clearSession, statusFilter, token, typeFilter])

  useEffect(() => {
    loadMessages()
  }, [loadMessages])

  async function updateStatus(messageId, action) {
    setActingId(messageId)
    try {
      await apiRequest(`/financial-messages/${messageId}/${action}`, { method: 'POST', token })
      await loadMessages()
    } finally {
      setActingId(null)
    }
  }

  return (
    <div className="page-grid">
      <section className="panel">
        <div className="panel-header">
          <div>
            <p className="eyebrow">Financial inbox</p>
            <h2>Saved alerts and coaching messages</h2>
          </div>
          <div className="inbox-summary">
            <span className="budget-pill budget-pill--info">{unreadCount} unread</span>
          </div>
        </div>

        <div className="filters-grid">
          <label>
            Status
            <select value={statusFilter} onChange={(event) => setStatusFilter(event.target.value)}>
              {STATUS_OPTIONS.map((option) => (
                <option key={option} value={option}>{option}</option>
              ))}
            </select>
          </label>
          <label>
            Type
            <select value={typeFilter} onChange={(event) => setTypeFilter(event.target.value)}>
              {TYPE_OPTIONS.map((option) => (
                <option key={option} value={option}>{option}</option>
              ))}
            </select>
          </label>
        </div>

        {loading ? <p>Loading inbox...</p> : null}
        {!loading && !messages.length ? <p>No messages match the current filters.</p> : null}

        <div className="stack-list">
          {messages.map((message) => (
            <article key={message.id} className={`message-card message-card--${message.status}`}>
              <div className="message-card__header">
                <div>
                  <p className="eyebrow">{formatType(message.type)}</p>
                  <h3>{message.title}</h3>
                </div>
                <div className="message-card__meta">
                  <span className={`budget-pill budget-pill--${severityTone(message.severity)}`}>{message.severity}</span>
                  <span className="message-status">{message.status}</span>
                </div>
              </div>
              <p>{message.message}</p>
              <span className="message-date">{new Date(message.createdAtUtc).toLocaleString()}</span>
              <div className="message-actions">
                {message.status !== 'read' ? (
                  <button type="button" className="ghost-button" disabled={actingId === message.id} onClick={() => updateStatus(message.id, 'read')}>
                    Mark read
                  </button>
                ) : null}
                {message.status !== 'dismissed' ? (
                  <button type="button" className="ghost-button" disabled={actingId === message.id} onClick={() => updateStatus(message.id, 'dismiss')}>
                    Dismiss
                  </button>
                ) : null}
                {message.status !== 'archived' ? (
                  <button type="button" className="ghost-button" disabled={actingId === message.id} onClick={() => updateStatus(message.id, 'archive')}>
                    Archive
                  </button>
                ) : null}
              </div>
            </article>
          ))}
        </div>
      </section>
    </div>
  )
}

function formatType(type) {
  return type.replace(/([a-z])([A-Z])/g, '$1 $2').replaceAll('_', ' ')
}

function severityTone(severity) {
  switch (severity) {
    case 'high':
      return 'over_budget'
    case 'medium':
      return 'warning'
    default:
      return 'info'
  }
}
