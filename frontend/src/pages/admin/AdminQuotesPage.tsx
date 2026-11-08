import { useState } from 'react'
import { api, errorMessage } from '../../api'
import { ErrorAlert, Loading } from '../../components/common'
import { useApi } from '../../useApi'
import { formatDateTime, formatTime } from '../../format'
import type { Booking } from '../../types'
import styles from './Admin.module.css'

function QuoteCard({ booking, onDone }: { booking: Booking; onDone: (message: string) => void }) {
  const [amount, setAmount] = useState('')
  const [note, setNote] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  async function decide(action: 'accept' | 'reject') {
    if (action === 'reject' && !window.confirm(`Refuser la demande ${booking.code} ?`)) return
    setBusy(true)
    setError(null)
    try {
      const body = action === 'accept' ? { amount: Number(amount), note: note || null } : { note: note || null }
      await api.post(`/api/admin/quotes/${booking.code}/${action}`, body)
      onDone(action === 'accept' ? `Devis ${booking.code} accepté.` : `Devis ${booking.code} refusé.`)
    } catch (e) {
      setError(errorMessage(e))
    } finally {
      setBusy(false)
    }
  }

  return (
    <article className="card stack">
      <div className="row" style={{ justifyContent: 'space-between' }}>
        <h3 style={{ margin: 0 }}>{booking.spaceName}</h3>
        <strong>{booking.code}</strong>
      </div>
      <dl className="details">
        <dt>Client</dt>
        <dd>
          {booking.fullName} · {booking.phone}
        </dd>
        <dt>Créneau</dt>
        <dd>
          {formatDateTime(booking.plannedArrival)} → {formatTime(booking.plannedEnd)}
        </dd>
        <dt>Personnes</dt>
        <dd>{booking.persons}</dd>
      </dl>
      <div className="row" style={{ alignItems: 'flex-end' }}>
        <div className="field">
          <label htmlFor={`amount-${booking.code}`}>Montant (DH)</label>
          <input
            id={`amount-${booking.code}`}
            className={styles.priceInput}
            type="number"
            min={1}
            step={1}
            value={amount}
            onChange={(e) => setAmount(e.target.value)}
          />
        </div>
        <div className="field" style={{ flex: 1, minWidth: 180 }}>
          <label htmlFor={`note-${booking.code}`}>Note (optionnelle)</label>
          <input id={`note-${booking.code}`} maxLength={500} value={note} onChange={(e) => setNote(e.target.value)} />
        </div>
      </div>
      <ErrorAlert message={error} />
      <div className="row">
        <button className="btn" type="button" disabled={busy || !amount} onClick={() => decide('accept')}>
          Accepter le devis
        </button>
        <button className="btn btn-secondary" type="button" disabled={busy} onClick={() => decide('reject')}>
          Refuser
        </button>
      </div>
    </article>
  )
}

export default function AdminQuotesPage() {
  const { data, error, loading, reload } = useApi<Booking[]>('/api/admin/bookings?status=DevisDemande')
  const [message, setMessage] = useState<string | null>(null)

  return (
    <div className="container page stack">
      <div>
        <h1 style={{ margin: 0 }}>Demandes de devis</h1>
        <p className="muted" style={{ margin: 0 }}>
          Un devis accepté réserve la salle : une autre demande qui chevauche ce créneau ne pourra plus être acceptée.
        </p>
      </div>
      {message && <div className="alert alert-success">{message}</div>}
      <ErrorAlert message={error} />
      {loading && !data && <Loading />}
      {data?.length === 0 && <p className="muted">Aucune demande en attente.</p>}
      <div className="grid grid-2">
        {data?.map((booking) => (
          <QuoteCard
            key={booking.code}
            booking={booking}
            onDone={(text) => {
              setMessage(text)
              reload()
            }}
          />
        ))}
      </div>
    </div>
  )
}
