export function BudgetWarningStrip({ items }) {
  if (!items.length) {
    return null
  }

  return (
    <div className="budget-warning-strip">
      <div className="budget-warning-strip__items">
        {items.slice(0, 3).map((item) => (
          <span key={item.categoryId} className={`budget-pill budget-pill--${item.status}`}>
            {item.message}
          </span>
        ))}
      </div>
      {items.length > 3 ? <a href="#budget-variance">View all budget alerts</a> : null}
    </div>
  )
}
