import { useMemo, useState } from 'react'
import { formatCurrency } from '../utils/formatters.js'

export function BudgetManager({ categories, budgets, onSave, onDelete, savingCategoryId }) {
  const [drafts, setDrafts] = useState({})

  const budgetMap = useMemo(
    () => Object.fromEntries(budgets.map((budget) => [budget.categoryId, budget])),
    [budgets],
  )

  return (
    <article className="panel">
      <div className="panel-header">
        <div>
          <p className="eyebrow">Budget management</p>
          <h3>Recurring monthly budgets</h3>
        </div>
      </div>

      {!budgets.length ? <p>No budgets yet. Add a budget to start getting warning states on this dashboard.</p> : null}

      <div className="stack-list">
        {categories.map((category) => {
          const currentBudget = budgetMap[category.id]
          const draftValue = drafts[category.id] ?? currentBudget?.amount ?? ''
          const saving = savingCategoryId === category.id

          return (
            <div key={category.id} className="budget-row">
              <div>
                <strong>{category.name}</strong>
                <p>{currentBudget ? `Current budget: ${formatCurrency(currentBudget.amount)}` : 'No budget set'}</p>
              </div>
              <div className="budget-row__actions">
                <input
                  type="number"
                  min="0.01"
                  step="0.01"
                  placeholder="ILS"
                  value={draftValue}
                  onChange={(event) => setDrafts((current) => ({ ...current, [category.id]: event.target.value }))}
                />
                <button
                  type="button"
                  disabled={saving || !Number(draftValue)}
                  onClick={() => onSave(category.id, Number(draftValue))}
                >
                  {saving ? 'Saving...' : currentBudget ? 'Update' : 'Set'}
                </button>
                {currentBudget ? (
                  <button className="ghost-button" type="button" disabled={saving} onClick={() => onDelete(category.id)}>
                    Remove
                  </button>
                ) : null}
              </div>
            </div>
          )
        })}
      </div>
    </article>
  )
}
