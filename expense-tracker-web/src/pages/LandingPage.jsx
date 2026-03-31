import { Link } from 'react-router-dom'

export function LandingPage() {
  return (
    <div className="landing-page">
      <section className="hero-panel">
        <p className="eyebrow">Smart Expense Tracker with AI Insights</p>
        <h1>Fast expense tracking with reliable analytics and AI-assisted categorization.</h1>
        <p className="hero-copy">
          Track spending in seconds, create your own categories, and understand the story behind your month without leaving the web.
        </p>
        <div className="inline-actions">
          <Link className="primary-link" to="/register">Create your account</Link>
          <Link className="ghost-link" to="/login">Welcome back</Link>
        </div>
      </section>
    </div>
  )
}
