export function DeleteDialog({ open, onCancel, onConfirm }) {
  if (!open) {
    return null
  }

  return (
    <div className="modal-backdrop">
      <div className="modal-card">
        <h3>Delete Expense</h3>
        <p>Are you sure you want to delete this expense? This action cannot be undone.</p>
        <div className="inline-actions">
          <button className="danger-button" onClick={onConfirm}>Delete</button>
          <button className="ghost-button" onClick={onCancel}>Cancel</button>
        </div>
      </div>
    </div>
  )
}
