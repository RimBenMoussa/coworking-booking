import { useState, type FormEvent } from 'react'
import { useSearchParams } from 'react-router-dom'
import { api, errorMessage } from '../../api'
import { ErrorAlert, StatusBadge } from '../../components/common'
import { formatDateTime, formatDuration, formatMoney, formatTime, paymentLabels } from '../../format'
import type { Booking, CheckOut, PaymentMethod } from '../../types'
import styles from './Admin.module.css'

// Accueil : check-in à l'arrivée, puis check-out au départ avec le prix à encaisser.
export default function AdminFrontDeskPage() {
  const [params] = useSearchParams()
  const [code, setCode] = useState(params.get('code') ?? '')
  const [error, setError] = useState<string | null>(null)
  const [success, setSuccess] = useState<string | null>(null)
  const [checkedIn, setCheckedIn] = useState<Booking | null>(null)
  const [preview, setPreview] = useState<CheckOut | null>(null)
  const [done, setDone] = useState<CheckOut | null>(null)
  const [payment, setPayment] = useState<PaymentMethod>('Especes')
  const [busy, setBusy] = useState(false)

  const normalizedCode = code.trim().toUpperCase()

  function reset() {
    setError(null)
    setSuccess(null)
    setCheckedIn(null)
    setPreview(null)
    setDone(null)
  }

  async function run(action: () => Promise<void>) {
    setBusy(true)
    reset()
    try {
      await action()
    } catch (e) {
      setError(errorMessage(e))
    } finally {
      setBusy(false)
    }
  }

  const checkIn = (event: FormEvent) => {
    event.preventDefault()
    return run(async () => {
      const { data } = await api.post<Booking>(`/api/admin/bookings/${encodeURIComponent(normalizedCode)}/check-in`)
      setCheckedIn(data)
      setSuccess(`Check-in enregistré pour ${data.fullName}.`)
    })
  }

  const prepareCheckOut = () =>
    run(async () => {
      const { data } = await api.get<CheckOut>(`/api/admin/bookings/${encodeURIComponent(normalizedCode)}/check-out`)
      setPreview(data)
    })

  const confirmCheckOut = () =>
    run(async () => {
      const { data } = await api.post<CheckOut>(`/api/admin/bookings/${encodeURIComponent(normalizedCode)}/check-out`, {
        paymentMethod: payment,
      })
      setDone(data)
      setSuccess(`Départ enregistré : ${formatMoney(data.amount)} encaissés (${paymentLabels[payment]}).`)
    })

  return (
    <div className="container narrow page stack">
      <h1 style={{ margin: 0 }}>Check-in / Check-out</h1>

      <form className="card stack" onSubmit={checkIn}>
        <div className="field">
          <label htmlFor="code">Code de réservation</label>
          <input
            id="code"
            className={styles.codeInput}
            required
            placeholder="CW-XXXXX"
            autoComplete="off"
            value={code}
            onChange={(e) => {
              setCode(e.target.value)
              reset()
            }}
          />
        </div>
        <div className="grid grid-2">
          <button className="btn" type="submit" disabled={busy || !normalizedCode}>
            Check-in (arrivée)
          </button>
          <button className="btn btn-secondary" type="button" disabled={busy || !normalizedCode} onClick={prepareCheckOut}>
            Check-out (départ)
          </button>
        </div>
      </form>

      <ErrorAlert message={error} />
      {success && <div className="alert alert-success">{success}</div>}

      {checkedIn && (
        <div className="card">
          <dl className="details">
            <dt>Client</dt>
            <dd>{checkedIn.fullName}</dd>
            <dt>Espace</dt>
            <dd>{checkedIn.spaceName}</dd>
            <dt>Arrivée</dt>
            <dd>{checkedIn.checkInAt && formatTime(checkedIn.checkInAt)}</dd>
            <dt>Abonné</dt>
            <dd>{checkedIn.isSubscriber ? 'Oui' : 'Non'}</dd>
          </dl>
        </div>
      )}

      {preview && !done && (
        <div className="card stack">
          <div className="row" style={{ justifyContent: 'space-between' }}>
            <h2 style={{ margin: 0 }}>{preview.booking.fullName}</h2>
            <StatusBadge status={preview.booking.status} />
          </div>
          <dl className="details">
            <dt>Espace</dt>
            <dd>{preview.booking.spaceName}</dd>
            <dt>Arrivée</dt>
            <dd>{preview.booking.checkInAt && formatDateTime(preview.booking.checkInAt)}</dd>
            <dt>Durée</dt>
            <dd>{formatDuration(preview.durationMinutes)}</dd>
            <dt>Calcul</dt>
            <dd>{preview.priceDetail}</dd>
          </dl>
          <div>
            <div className="label">À encaisser</div>
            <div className={styles.amount}>{formatMoney(preview.amount)}</div>
          </div>
          <fieldset className={styles.payment} style={{ border: 'none', padding: 0, margin: 0 }}>
            <legend className="sr-only">Moyen de paiement</legend>
            {(['Especes', 'Carte'] as const).map((method) => (
              <label key={method}>
                <input
                  type="radio"
                  name="payment"
                  value={method}
                  checked={payment === method}
                  onChange={() => setPayment(method)}
                />
                {paymentLabels[method]}
              </label>
            ))}
          </fieldset>
          <button className="btn btn-block" type="button" disabled={busy} onClick={confirmCheckOut}>
            Encaisser et terminer
          </button>
        </div>
      )}

      {done && (
        <div className="card">
          <dl className="details">
            <dt>Durée</dt>
            <dd>{formatDuration(done.durationMinutes)}</dd>
            <dt>Montant</dt>
            <dd>{formatMoney(done.amount)}</dd>
            <dt>Calcul</dt>
            <dd>{done.priceDetail}</dd>
          </dl>
        </div>
      )}
    </div>
  )
}
