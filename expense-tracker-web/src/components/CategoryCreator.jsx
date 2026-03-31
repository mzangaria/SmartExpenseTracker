import { useState } from 'react'

export function CategoryCreator({ onCreate }) {
  const [name, setName] = useState('')
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')

  async function handleSubmit(event) {
    event.preventDefault()
    setSaving(true)
    setError('')

    try {
      await onCreate(name)
      setName('')
    } catch (submitError) {
      setError(submitError.message)
    } finally {
      setSaving(false)
    }
  }

  return (
    <form className="category-creator" onSubmit={handleSubmit}>
      <label>
        Add category
        <input
          value={name}
          onChange={(event) => setName(event.target.value)}
          placeholder="Groceries"
          maxLength={100}
        />
      </label>
      <button type="submit" disabled={saving || !name.trim()}>
        {saving ? 'Saving...' : 'Add Category'}
      </button>
      {error ? <p className="inline-error">{error}</p> : null}
    </form>
  )
}
