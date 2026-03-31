import { useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { BarChart, Bar, CartesianGrid, LineChart, Line, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts'
import { apiRequest } from '../api/client.js'
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
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    async function loadDashboard() {
      setLoading(true)
      const [year, month] = selectedMonth.split('-')

      try {
        const [summaryResult, breakdownResult, trendsResult, insightsResult, expenseResult] = await Promise.all([
          apiRequest(`/analytics/monthly-summary?year=${year}&month=${month}`, { token }),
          apiRequest(`/analytics/category-breakdown?year=${year}&month=${month}`, { token }),
          apiRequest(`/analytics/trends?year=${year}&month=${month}`, { token }),
          apiRequest(`/analytics/insights?year=${year}&month=${month}`, { token }),
          apiRequest(`/expenses?year=${year}&month=${month}`, { token }),
        ])

        setSummary(summaryResult)
        setBreakdown(breakdownResult)
        setTrends(trendsResult)
        setInsights(insightsResult)
        setRecentExpenses(expenseResult.slice(0, 5))
      } catch (error) {
        if (error.status === 401) {
          clearSession()
        }
      } finally {
        setLoading(false)
      }
    }

    loadDashboard()
  }, [clearSession, selectedMonth, token])

  const empty = !loading && (summary?.numberOfExpenses ?? 0) === 0

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

        {!loading && !empty ? (
          <>
            <div className="stats-grid">
              <StatCard title="Total Spent" value={formatCurrency(summary.totalSpent, summary.currency)} helper="Selected month" />
              <StatCard title="Number of Expenses" value={summary.numberOfExpenses} helper="Selected month" />
              <StatCard title="Average Expense" value={formatCurrency(summary.averageExpense, summary.currency)} helper="Selected month" />
              <StatCard title="Largest Expense" value={formatCurrency(summary.largestExpense, summary.currency)} helper="Selected month" />
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
                      <span>{formatCurrency(expense.amount, expense.currency)}</span>
                    </div>
                  ))}
                </div>
              </article>
            </div>
          </>
        ) : null}
      </section>
    </div>
  )
}
