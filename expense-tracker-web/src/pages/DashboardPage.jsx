import { useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { BarChart, Bar, CartesianGrid, LineChart, Line, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts'
import { apiRequest } from '../api/client.js'
import { BudgetManager } from '../components/BudgetManager.jsx'
import { BudgetVarianceList } from '../components/BudgetVarianceList.jsx'
import { BudgetWarningStrip } from '../components/BudgetWarningStrip.jsx'
import { StatCard } from '../components/StatCard.jsx'
import { useAuth } from '../hooks/useAuth.js'
import { formatCurrency, monthOptions } from '../utils/formatters.js'

export function DashboardPage() {
  const { token, clearSession } = useAuth()
  const options = useMemo(() => monthOptions(6), [])
  const [selectedMonth, setSelectedMonth] = useState(options[0].value)
  const [summary, setSummary] = useState(null)
  const [breakdown, setBreakdown] = useState([])
  const [trends, setTrends] = useState([])
  const [insights, setInsights] = useState([])
  const [recentExpenses, setRecentExpenses] = useState([])
  const [categories, setCategories] = useState([])
  const [budgets, setBudgets] = useState([])
  const [budgetVariance, setBudgetVariance] = useState([])
  const [budgetError, setBudgetError] = useState('')
  const [savingBudgetCategoryId, setSavingBudgetCategoryId] = useState(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    async function loadDashboard() {
      setLoading(true)
      setBudgetError('')
      const [year, month] = selectedMonth.split('-')

      try {
        const [summaryResult, breakdownResult, trendsResult, insightsResult, expenseResult, categoriesResult, budgetsResult, varianceResult] = await Promise.all([
          apiRequest(`/analytics/monthly-summary?year=${year}&month=${month}`, { token }),
          apiRequest(`/analytics/category-breakdown?year=${year}&month=${month}`, { token }),
          apiRequest(`/analytics/trends?year=${year}&month=${month}`, { token }),
          apiRequest(`/analytics/insights?year=${year}&month=${month}`, { token }),
          apiRequest(`/expenses?year=${year}&month=${month}`, { token }),
          apiRequest('/categories', { token }),
          apiRequest('/budgets', { token }),
          apiRequest(`/analytics/budget-variance?year=${year}&month=${month}`, { token }),
        ])

        setSummary(summaryResult)
        setBreakdown(breakdownResult)
        setTrends(trendsResult)
        setInsights(insightsResult)
        setRecentExpenses(expenseResult.slice(0, 5))
        setCategories(categoriesResult)
        setBudgets(budgetsResult)
        setBudgetVariance(varianceResult)
      } catch (error) {
        if (error.status === 401) {
          clearSession()
        }
        setBudgetError('Budget data could not be loaded for this dashboard refresh.')
      } finally {
        setLoading(false)
      }
    }

    loadDashboard()
  }, [clearSession, selectedMonth, token])

  const empty = !loading && (summary?.numberOfExpenses ?? 0) === 0
  const warningItems = budgetVariance
    .filter((item) => item.showInWarningStrip)
    .sort((left, right) => severity(right.status) - severity(left.status))
    .map((item) => ({
      ...item,
      message:
        item.status === 'over_budget'
          ? `${item.categoryName} is over budget by ${formatCurrency(Math.abs(item.remainingAmount))}`
          : item.status === 'reached'
            ? `${item.categoryName} reached its monthly budget`
            : `${item.categoryName} is at ${item.usagePercent.toFixed(0)}% of budget`,
    }))

  async function refreshBudgetData() {
    const [year, month] = selectedMonth.split('-')
    const [budgetsResult, varianceResult] = await Promise.all([
      apiRequest('/budgets', { token }),
      apiRequest(`/analytics/budget-variance?year=${year}&month=${month}`, { token }),
    ])
    setBudgets(budgetsResult)
    setBudgetVariance(varianceResult)
  }

  async function handleSaveBudget(categoryId, amount) {
    setSavingBudgetCategoryId(categoryId)
    try {
      await apiRequest(`/budgets/${categoryId}`, { method: 'PUT', token, body: { amount } })
      await refreshBudgetData()
    } finally {
      setSavingBudgetCategoryId(null)
    }
  }

  async function handleDeleteBudget(categoryId) {
    setSavingBudgetCategoryId(categoryId)
    try {
      await apiRequest(`/budgets/${categoryId}`, { method: 'DELETE', token })
      await refreshBudgetData()
    } finally {
      setSavingBudgetCategoryId(null)
    }
  }

  return (
    <div className="page-grid">
      <section className="panel">
        <div className="panel-header">
          <div>
            <p className="eyebrow">Dashboard</p>
            <h2>Monthly overview</h2>
          </div>
          <div className="inline-actions">
            <select value={selectedMonth} onChange={(event) => setSelectedMonth(event.target.value)}>
              {options.map((option) => <option key={option.value} value={option.value}>{option.label}</option>)}
            </select>
            <Link className="primary-link" to="/expenses/new">Add Expense</Link>
          </div>
        </div>

        {loading ? <p>Loading dashboard...</p> : null}
        {empty ? <p>No expenses yet. Add your first expense to get started.</p> : null}
        {!loading ? <BudgetWarningStrip items={warningItems} /> : null}

        {!loading && !empty ? (
          <>
            <div className="stats-grid">
              <StatCard title="Total Spent" value={formatCurrency(summary.totalSpent)} helper="Selected month" />
              <StatCard title="Number of Expenses" value={summary.numberOfExpenses} helper="Selected month" />
              <StatCard title="Average Expense" value={formatCurrency(summary.averageExpense)} helper="Selected month" />
              <StatCard title="Largest Expense" value={formatCurrency(summary.largestExpense)} helper="Selected month" />
            </div>

            <div id="analytics" className="chart-grid">
              <article className="chart-card">
                <h3>Category breakdown</h3>
                <ResponsiveContainer width="100%" height={260}>
                  <BarChart data={breakdown}>
                    <CartesianGrid strokeDasharray="3 3" />
                    <XAxis dataKey="categoryName" />
                    <YAxis />
                    <Tooltip />
                    <Bar dataKey="totalAmount" fill="#0f766e" radius={[8, 8, 0, 0]} />
                  </BarChart>
                </ResponsiveContainer>
              </article>
              <article className="chart-card">
                <h3>Monthly trend</h3>
                <ResponsiveContainer width="100%" height={260}>
                  <LineChart data={trends}>
                    <CartesianGrid strokeDasharray="3 3" />
                    <XAxis dataKey="periodLabel" />
                    <YAxis />
                    <Tooltip />
                    <Line type="monotone" dataKey="totalAmount" stroke="#ea580c" strokeWidth={3} />
                  </LineChart>
                </ResponsiveContainer>
              </article>
            </div>

            <div className="two-column">
              <article className="panel">
                <h3>Insights</h3>
                <div className="stack-list">
                  {insights.map((insight) => (
                    <div key={`${insight.title}-${insight.message}`} className="insight-card">
                      <strong>{insight.title}</strong>
                      <p>{insight.message}</p>
                      {insight.context ? <span>{insight.context}</span> : null}
                    </div>
                  ))}
                </div>
              </article>
              <article className="panel">
                <div className="panel-header">
                  <h3>Recent expenses</h3>
                  <Link to="/expenses">View all</Link>
                </div>
                <div className="stack-list">
                  {recentExpenses.map((expense) => (
                    <div key={expense.id} className="expense-row">
                      <div>
                        <strong>{expense.description}</strong>
                        <p>{expense.categoryName}</p>
                      </div>
                      <span>{formatCurrency(expense.amount)}</span>
                    </div>
                  ))}
                </div>
              </article>
            </div>

            <div className="two-column">
              <BudgetManager
                categories={categories}
                budgets={budgets}
                onSave={handleSaveBudget}
                onDelete={handleDeleteBudget}
                savingCategoryId={savingBudgetCategoryId}
              />
              <BudgetVarianceList items={budgetVariance} />
            </div>

            {budgetError ? <p className="inline-error">{budgetError}</p> : null}
          </>
        ) : null}

        {!loading && empty ? (
          <>
            <BudgetManager
              categories={categories}
              budgets={budgets}
              onSave={handleSaveBudget}
              onDelete={handleDeleteBudget}
              savingCategoryId={savingBudgetCategoryId}
            />
            <BudgetVarianceList items={budgetVariance} />
            {budgetError ? <p className="inline-error">{budgetError}</p> : null}
          </>
        ) : null}
      </section>
    </div>
  )
}

function severity(status) {
  switch (status) {
    case 'over_budget':
      return 3
    case 'reached':
      return 2
    case 'warning':
      return 1
    default:
      return 0
  }
}
