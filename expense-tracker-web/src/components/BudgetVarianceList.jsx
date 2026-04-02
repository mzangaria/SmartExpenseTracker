import { formatCurrency } from '../utils/formatters.js'

export function BudgetVarianceList({ items }) {
  return (
    <article id="budget-variance" className="panel">
      <div className="panel-header">
        <div>
          <p className="eyebrow">Budget variance</p>
          <h3>Budget vs actual for the selected month</h3>
        </div>
      </div>

      {!items.length ? <p>No budget variance yet. Set a budget for a category to track warning thresholds.</p> : null}

      <div className="stack-list">
        {items.map((item) => (
          <div key={item.categoryId} className={`variance-card variance-card--${item.status}`}>
            <div>
              <strong>{item.categoryName}</strong>
              <p>Budget {formatCurrency(item.budgetAmount)} • Actual {formatCurrency(item.actualAmount)}</p>
            </div>
            <div className="variance-card__meta">
              <strong>{item.usagePercent.toFixed(2)}%</strong>
              <span>
                {item.status === 'over_budget'
                  ? `Over by ${formatCurrency(Math.abs(item.remainingAmount))}`
                  : `Remaining ${formatCurrency(item.remainingAmount)}`}
              </span>
            </div>
          </div>
        ))}
      </div>
    </article>
  )
}
