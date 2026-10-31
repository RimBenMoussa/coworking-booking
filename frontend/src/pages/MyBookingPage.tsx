import { useEffect, useState, type FormEvent } from 'react'
import { useSearchParams } from 'react-router-dom'
import { api, errorMessage } from '../api'
import { ErrorAlert, StatusBadge } from '../components/common'
import { formatDateTime, formatMoney, formatTime, paymentLabels } from '../format'
import type { Booking } from '../types'

export default function MyBookingPage() {
  const [params] = useSearchParams()
  const [code, setCode] = useState(params.get('code') ?? '')
  const [phone, setPhone] = useState(params.get('phone') ?? '')
  const [booking, setBooking] = useState<Booking | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [message, setMessage] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  const fetchBooking = (searchCode: string, searchPhone: string) =>
    api.get<Booking>(`/api/bookings/${encodeURIComponent(searchCode.trim())}`, { params: { phone: searchPhone } })

  async function search() {
    setBusy(true)
    setError(null)
    setMessage(null)
    try {
      const { data } = await fetchBooking(code, phone)
      setBooking(data)
    } catch (e) {
      setBooking(null)
      setError(errorMessage(e))
    } finally {
      setBusy(false)
    }
  }

  // Arrivée depuis la page de confirmation : code et téléphone déjà dans l'URL, on affiche directement.
  useEffect(() => {
    const initialCode = params.get('code')
    const initialPhone = params.get('phone')
    if (!initialCode || !initialPhone) return
    fetchBooking(initialCode, initialPhone)
      .then(({ data }) => setBooking(data))
      .catch((e: unknown) => setError(errorMessage(e)))
  }, [params])

  async function cancel() {
    if (!booking || !window.confirm(`Annuler la réservation ${booking.code} ?`)) return
    setBusy(true)
    setError(null)
    try {
      const { data } = await api.post<Booking>(`/api/bookings/${booking.code}/cancel`, { phone })
      setBooking(data)
      setMessage('Votre réservation est annulée. Merci de nous avoir prévenus !')
    } catch (e) {
      setError(errorMessage(e))
    } finally {
      setBusy(false)
    }
  }

  function onSubmit(event: FormEvent) {
    event.preventDefault()
    void search()
  }

  const canCancel = booking && (booking.status === 'Confirmee' || booking.status === 'DevisDemande')

  return (
    <div className="container narrow page stack">
      <div>
        <h1>Ma réservation</h1>
        <p className="muted" style={{ margin: 0 }}>
          Entrez le code reçu (ex. CW-7K3P9) et le téléphone utilisé pour réserver.
        </p>
      </div>

      <form className="card stack" onSubmit={onSubmit}>
        <div className="grid grid-2">
          <div className="field">
            <label htmlFor="code">Code</label>
            <input
              id="code"
              required
              placeholder="CW-XXXXX"
              autoCapitalize="characters"
              value={code}
              onChange={(e) => setCode(e.target.value.toUpperCase())}
            />
          </div>
          <div className="field">
            <label htmlFor="phone">Téléphone</label>
            <input id="phone" type="tel" required placeholder="06XXXXXXXX" value={phone} onChange={(e) => setPhone(e.target.value)} />
          </div>
        </div>
        <button className="btn" type="submit" disabled={busy}>
          Rechercher
        </button>
      </form>

      <ErrorAlert message={error} />
      {message && <div className="alert alert-success">{message}</div>}

      {booking && (
        <section className="card stack">
          <div className="row" style={{ justifyContent: 'space-between' }}>
            <h2 style={{ margin: 0 }}>{booking.code}</h2>
            <StatusBadge status={booking.status} />
          </div>
          <dl className="details">
            <dt>Nom</dt>
            <dd>{booking.fullName}</dd>
            <dt>Espace</dt>
            <dd>{booking.spaceName}</dd>
            <dt>Arrivée prévue</dt>
            <dd>{formatDateTime(booking.plannedArrival)}</dd>
            {booking.spaceType !== 'PostePartage' && (
              <>
                <dt>Fin prévue</dt>
                <dd>{formatTime(booking.plannedEnd)}</dd>
                <dt>Personnes</dt>
                <dd>{booking.persons}</dd>
              </>
            )}
            {booking.status === 'Confirmee' && (
              <>
                <dt>Place gardée jusqu'à</dt>
                <dd>{formatTime(booking.holdUntil)}</dd>
              </>
            )}
            {booking.quoteAmount !== null && (
              <>
                <dt>Devis</dt>
                <dd>{formatMoney(booking.quoteAmount)}</dd>
              </>
            )}
            {booking.managerNote && (
              <>
                <dt>Note</dt>
                <dd>{booking.managerNote}</dd>
              </>
            )}
            {booking.amountDue !== null && (
              <>
                <dt>Montant payé</dt>
                <dd>
                  {formatMoney(booking.amountDue)}
                  {booking.paymentMethod && ` (${paymentLabels[booking.paymentMethod]})`}
                </dd>
              </>
            )}
          </dl>
          {canCancel && (
            <button className="btn btn-danger" type="button" onClick={cancel} disabled={busy}>
              Annuler gratuitement
            </button>
          )}
        </section>
      )}
    </div>
  )
}
