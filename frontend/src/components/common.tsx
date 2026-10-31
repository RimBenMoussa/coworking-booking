import { statusLabels } from '../format'
import type { BookingStatus } from '../types'

export function StatusBadge({ status }: { status: BookingStatus }) {
  return <span className={`badge badge-${status}`}>{statusLabels[status]}</span>
}

export function ErrorAlert({ message }: { message: string | null }) {
  if (!message) return null
  return (
    <div className="alert alert-error" role="alert">
      {message}
    </div>
  )
}

export function Loading() {
  return <p className="muted">Chargement…</p>
}
