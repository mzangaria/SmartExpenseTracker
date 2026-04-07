import { useEffect, useState } from 'react'
import { NavLink, useLocation } from 'react-router-dom'
import { apiRequest } from '../api/client.js'
import { useAuth } from '../hooks/useAuth.js'
import { FINANCIAL_MESSAGES_UPDATED_EVENT } from '../utils/financial-message-events.js'

export function InboxLink() {
  const { token, isAuthenticated } = useAuth()
  const location = useLocation()
  const [count, setCount] = useState(0)

  useEffect(() => {
    async function loadCount() {
      if (!isAuthenticated) {
        setCount(0)
        return
      }

      try {
        const result = await apiRequest('/financial-messages/unread-count', { token })
        setCount(result.count)
      } catch {
        setCount(0)
      }
    }

    loadCount()
  }, [isAuthenticated, location.pathname, token])

  useEffect(() => {
    function handleMessagesUpdated(event) {
      const nextCount = event.detail?.unreadCount
      if (typeof nextCount === 'number') {
        setCount(nextCount)
      }
    }

    window.addEventListener(FINANCIAL_MESSAGES_UPDATED_EVENT, handleMessagesUpdated)
    return () => window.removeEventListener(FINANCIAL_MESSAGES_UPDATED_EVENT, handleMessagesUpdated)
  }, [])

  return (
    <NavLink to="/inbox" className="topnav-inbox">
      Inbox
      {count > 0 ? <span className="topnav-badge">{count}</span> : null}
    </NavLink>
  )
}
