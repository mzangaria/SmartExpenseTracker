import { useCallback, useEffect, useState } from 'react'
import { apiRequest } from '../api/client.js'
import { CategoryCreator } from '../components/CategoryCreator.jsx'
import { useAuth } from '../hooks/useAuth.js'

export function ProfilePage() {
  const { token, user } = useAuth()
  const [categories, setCategories] = useState([])

  const loadCategories = useCallback(async () => {
    const result = await apiRequest('/categories', { token })
    setCategories(result)
  }, [token])

  useEffect(() => {
    async function initialize() {
      await loadCategories()
    }

    initialize()
  }, [loadCategories])

  async function handleCreateCategory(name) {
    await apiRequest('/categories', { method: 'POST', token, body: { name } })
    await loadCategories()
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
