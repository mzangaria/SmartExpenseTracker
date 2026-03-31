export function StatCard({ title, value, helper }) {
  return (
    <article className="stat-card">
      <p>{title}</p>
      <strong>{value}</strong>
      <span>{helper}</span>
    </article>
  )
}
