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
  onParseExpense,
  onCreateCategory,
  saving,
  aiLoading,
  parseLoading,
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
  const [inboxText, setInboxText] = useState('')

  function applyParsedDraft(result) {
    const draft = result?.draft
    if (!draft) {
      return
    }

    setForm((current) => ({
      ...current,
      description: draft.description ?? current.description,
      amount: draft.amount?.toString() ?? current.amount,
      expenseDate: draft.expenseDate ?? current.expenseDate,
      categoryId: draft.categoryId ?? current.categoryId,
      notes: draft.notes ?? current.notes,
      merchant: draft.merchant ?? current.merchant,
      useAiCategory: Boolean(draft.useAiCategory && draft.categoryId),
    }))
  }

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

  async function handleParseExpense() {
    const result = await onParseExpense(inboxText)
    if (result) {
      applyParsedDraft(result)
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
        <div className="natural-inbox">
          <label>
            Natural-Language Inbox
            <textarea
              value={inboxText}
              onChange={(event) => setInboxText(event.target.value)}
              rows="3"
              placeholder="Spent 42 ILS on sushi yesterday at Japanika"
            />
          </label>
          <div className="inline-actions">
            <button type="button" onClick={handleParseExpense} disabled={parseLoading || !inboxText.trim()}>
              {parseLoading ? 'Parsing expense...' : 'Parse Expense'}
            </button>
          </div>
          <p className="eyebrow">Write one expense in free text and review the generated draft below.</p>
        </div>

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
