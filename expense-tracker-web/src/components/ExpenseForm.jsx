import { useState } from 'react'
import { CategoryCreator } from './CategoryCreator.jsx'

const defaultForm = {
  description: '',
  amount: '',
  expenseDate: new Date().toISOString().slice(0, 10),
  categoryId: '',
  notes: '',
  merchant: '',
  useAiCategory: false,
}

export function ExpenseForm({
  categories,
  initialValue,
  onSubmit,
  onSuggestCategory,
  onCreateCategory,
  saving,
  aiLoading,
  errorMessage,
  aiMessage,
}) {
  const [form, setForm] = useState(() => (
    initialValue
      ? {
          description: initialValue.description ?? '',
          amount: initialValue.amount?.toString() ?? '',
          expenseDate: initialValue.expenseDate ?? new Date().toISOString().slice(0, 10),
          categoryId: initialValue.categoryId ?? '',
          notes: initialValue.notes ?? '',
          merchant: initialValue.merchant ?? '',
          useAiCategory: initialValue.categorySource === 'ai',
        }
      : defaultForm
  ))

  function updateField(field, value) {
    setForm((current) => ({ ...current, [field]: value }))
  }

  async function handleSubmit(event) {
    event.preventDefault()
    await onSubmit({
      ...form,
      amount: Number(form.amount),
      categoryId: form.categoryId,
    })
  }

  async function handleSuggestCategory() {
    const result = await onSuggestCategory(form.description)
    if (result?.suggestedCategoryId) {
      setForm((current) => ({
        ...current,
        categoryId: result.suggestedCategoryId,
        useAiCategory: true,
      }))
    }
  }

  return (
    <div className="panel">
      <div className="panel-header">
        <div>
          <p className="eyebrow">Expense Editor</p>
          <h2>{initialValue ? 'Edit Expense' : 'Add Expense'}</h2>
        </div>
      </div>

      <form className="expense-form" onSubmit={handleSubmit}>
        <label>
          Description
          <input
            value={form.description}
            onChange={(event) => updateField('description', event.target.value)}
            placeholder="Uber ride to campus"
            required
          />
        </label>

        <div className="form-grid">
          <label>
            Amount
            <input
              type="number"
              min="0.01"
              step="0.01"
              value={form.amount}
              onChange={(event) => updateField('amount', event.target.value)}
              required
            />
          </label>

          <label>
            Date
            <input
              type="date"
              value={form.expenseDate}
              onChange={(event) => updateField('expenseDate', event.target.value)}
              required
            />
          </label>
        </div>

        <div className="category-row">
          <label>
            Category
            <select
              value={form.categoryId}
              onChange={(event) => {
                updateField('categoryId', event.target.value)
                updateField('useAiCategory', false)
              }}
              required
            >
              <option value="">Choose a category</option>
              {categories.map((category) => (
                <option key={category.id} value={category.id}>
                  {category.name} {category.type === 'custom' ? '(Custom)' : ''}
                </option>
              ))}
            </select>
          </label>
          <button
            className="ghost-button"
            type="button"
            onClick={handleSuggestCategory}
            disabled={aiLoading || !form.description.trim()}
          >
            {aiLoading ? 'Getting suggestion...' : 'Suggest Category'}
          </button>
        </div>

        {aiMessage ? <p className={aiMessage.success ? 'inline-success' : 'inline-error'}>{aiMessage.text}</p> : null}

        <label>
          Merchant
          <input value={form.merchant} onChange={(event) => updateField('merchant', event.target.value)} />
        </label>

        <p className="eyebrow">All expenses are tracked in ILS.</p>

        <label>
          Notes
          <textarea value={form.notes} onChange={(event) => updateField('notes', event.target.value)} rows="4" />
        </label>

        {errorMessage ? <p className="inline-error">{errorMessage}</p> : null}

        <div className="inline-actions">
          <button type="submit" disabled={saving}>
            {saving ? 'Saving...' : 'Save Expense'}
          </button>
        </div>
      </form>

      <CategoryCreator onCreate={onCreateCategory} />
    </div>
  )
}
