import { useCallback, useEffect, useMemo, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { apiRequest } from '../api/client.js'
import { DeleteDialog } from '../components/DeleteDialog.jsx'
import { ExpenseForm } from '../components/ExpenseForm.jsx'
import { useAuth } from '../hooks/useAuth.js'
import { formatCurrency, formatDate, monthOptions } from '../utils/formatters.js'

export function ExpensesPage({ mode }) {
  const navigate = useNavigate()
  const { expenseId } = useParams()
  const { token, clearSession } = useAuth()
  const monthChoices = useMemo(() => monthOptions(6), [])
  const [categories, setCategories] = useState([])
  const [expenses, setExpenses] = useState([])
  const [selectedMonth, setSelectedMonth] = useState(monthChoices[0].value)
  const [filters, setFilters] = useState({ search: '', categoryId: '', minAmount: '', maxAmount: '' })
  const [editingExpense, setEditingExpense] = useState(null)
  const [errorMessage, setErrorMessage] = useState('')
  const [aiMessage, setAiMessage] = useState(null)
  const [saving, setSaving] = useState(false)
  const [aiLoading, setAiLoading] = useState(false)
  const [parseLoading, setParseLoading] = useState(false)
  const [deleteTarget, setDeleteTarget] = useState(null)

  const loadCategories = useCallback(async () => {
    const result = await apiRequest('/categories', { token })
    setCategories(result)
    return result
  }, [token])

  const loadExpenses = useCallback(async () => {
    const [year, month] = selectedMonth.split('-')
    const params = new URLSearchParams({ year, month })
    if (filters.search) params.set('search', filters.search)
    if (filters.categoryId) params.set('categoryId', filters.categoryId)
    if (filters.minAmount) params.set('minAmount', filters.minAmount)
    if (filters.maxAmount) params.set('maxAmount', filters.maxAmount)

    const result = await apiRequest(`/expenses?${params.toString()}`, { token })
    setExpenses(result)
  }, [filters.categoryId, filters.maxAmount, filters.minAmount, filters.search, selectedMonth, token])

  useEffect(() => {
    async function initialize() {
      try {
        await Promise.all([loadCategories(), loadExpenses()])
        if (mode === 'edit' && expenseId) {
          const result = await apiRequest(`/expenses/${expenseId}`, { token })
          setEditingExpense(result)
        }
      } catch (error) {
        if (error.status === 401) {
          clearSession()
        }
      }
    }

    initialize()
  }, [clearSession, expenseId, loadCategories, loadExpenses, mode, token])

  async function handleCreateCategory(name) {
    const category = await apiRequest('/categories', { method: 'POST', token, body: { name } })
    await loadCategories()
    return category
  }

  async function handleSuggestCategory(description) {
    if (!description.trim()) {
      const message = { success: false, text: 'Description cannot be empty before AI suggestion.' }
      setAiMessage(message)
      return null
    }

    setAiLoading(true)
    try {
      const result = await apiRequest('/ai/classify-expense', { method: 'POST', token, body: { description } })
      if (result.success) {
        const message = { success: true, text: `Suggested category: ${result.suggestedCategory}` }
        setAiMessage(message)
      } else {
        setAiMessage({ success: false, text: result.message })
      }
      return result
    } finally {
      setAiLoading(false)
    }
  }

  async function handleParseExpense(text) {
    if (!text.trim()) {
      const message = { success: false, text: 'Enter a sentence before parsing.' }
      setAiMessage(message)
      return null
    }

    setParseLoading(true)
    try {
      const result = await apiRequest('/ai/parse-expense', { method: 'POST', token, body: { text } })
      const warnings = result.warnings?.length ? ` Warnings: ${result.warnings.join(' ')}` : ''
      const missing = result.missingFields?.length ? ` Missing: ${result.missingFields.join(', ')}.` : ''
      setAiMessage({
        success: result.success,
        text: result.success
          ? `Expense draft parsed successfully.${warnings}`
          : `${result.message ?? 'The expense was parsed partially.'}${warnings}${missing}`,
      })
      return result
    } finally {
      setParseLoading(false)
    }
  }

  async function handleSubmit(form) {
    setSaving(true)
    setErrorMessage('')
    try {
      if (mode === 'edit' && expenseId) {
        await apiRequest(`/expenses/${expenseId}`, { method: 'PUT', token, body: form })
      } else {
        await apiRequest('/expenses', { method: 'POST', token, body: form })
      }
      await loadExpenses()
      navigate('/expenses')
    } catch (error) {
      const errors = error.payload?.errors
      const firstError = errors ? Object.values(errors).flat()[0] : null
      setErrorMessage(firstError ?? 'Failed to save expense.')
    } finally {
      setSaving(false)
    }
  }

  async function handleDelete() {
    try {
      await apiRequest(`/expenses/${deleteTarget.id}`, { method: 'DELETE', token })
      setDeleteTarget(null)
      await loadExpenses()
      if (mode === 'edit') {
        navigate('/expenses')
      }
    } catch {
      setDeleteTarget(null)
    }
  }

  const showEditor = mode === 'create' || mode === 'edit'

  return (
    <div className="page-grid">
      <section className="panel">
        <div className="panel-header">
          <div>
            <p className="eyebrow">Expenses</p>
            <h2>{showEditor ? 'Expense workspace' : 'Review, filter, and update your expenses'}</h2>
          </div>
          {!showEditor ? <Link className="primary-link" to="/expenses/new">Add Expense</Link> : null}
        </div>

        {showEditor ? (
          <ExpenseForm
            key={editingExpense?.id ?? 'new-expense'}
            categories={categories}
            initialValue={editingExpense}
            onSubmit={handleSubmit}
            onSuggestCategory={handleSuggestCategory}
            onParseExpense={handleParseExpense}
            onCreateCategory={handleCreateCategory}
            saving={saving}
            aiLoading={aiLoading}
            parseLoading={parseLoading}
            errorMessage={errorMessage}
            aiMessage={aiMessage}
          />
        ) : (
          <>
            <div className="filters-grid">
              <select value={selectedMonth} onChange={(event) => setSelectedMonth(event.target.value)}>
                {monthChoices.map((option) => <option key={option.value} value={option.value}>{option.label}</option>)}
              </select>
              <select value={filters.categoryId} onChange={(event) => setFilters({ ...filters, categoryId: event.target.value })}>
                <option value="">All categories</option>
                {categories.map((category) => <option key={category.id} value={category.id}>{category.name}</option>)}
              </select>
              <input placeholder="Search description" value={filters.search} onChange={(event) => setFilters({ ...filters, search: event.target.value })} />
              <input type="number" placeholder="Min amount" value={filters.minAmount} onChange={(event) => setFilters({ ...filters, minAmount: event.target.value })} />
              <input type="number" placeholder="Max amount" value={filters.maxAmount} onChange={(event) => setFilters({ ...filters, maxAmount: event.target.value })} />
            </div>

            {expenses.length === 0 ? <p>No expenses match your filters.</p> : null}

            <div className="table-card">
              {expenses.map((expense) => (
                <div key={expense.id} className="expense-row expense-row--full">
                  <div>
                    <strong>{expense.description}</strong>
                    <p>{expense.categoryName} • {formatDate(expense.expenseDate)}</p>
                    <span>{expense.categorySource === 'ai' ? 'AI-assisted' : 'Manual'}</span>
                  </div>
                  <div className="expense-actions">
                    <strong>{formatCurrency(expense.amount)}</strong>
                    <Link to={`/expenses/${expense.id}/edit`}>Edit</Link>
                    <button className="ghost-button" onClick={() => setDeleteTarget(expense)}>Delete</button>
                  </div>
                </div>
              ))}
            </div>
          </>
        )}
      </section>

      <DeleteDialog open={Boolean(deleteTarget)} onCancel={() => setDeleteTarget(null)} onConfirm={handleDelete} />
    </div>
  )
}
