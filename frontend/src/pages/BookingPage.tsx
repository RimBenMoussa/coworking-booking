import { useEffect, useState, type FormEvent } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { api, errorMessage } from '../api'
import { ErrorAlert, Loading, StatusBadge } from '../components/common'
import { useApi } from '../useApi'
import { formatDateTime, formatMoney, formatTime, nextQuarterHour, todayInFes } from '../format'
import type { Availability, Booking, Space, SpacesResponse } from '../types'
import styles from './BookingPage.module.css'

type Mode = 'reservation' | 'devis'

// Estimation affichée avant de réserver (le prix réel est calculé au départ).
function priceHint(space: Space, hours: number, persons: number): string {
  if (space.onQuote) return 'Sur devis : le gestionnaire vous communiquera un montant.'
  const amount = (type: string) => space.tariffs.find((t) => t.type === type)?.amount ?? 0
  if (space.type === 'PostePartage')
    return `${formatMoney(amount('Court'))} moins de 6 h · ${formatMoney(amount('Journee'))} la journée · ${formatMoney(amount('Nuit'))} la nuit (arrivée 20:00–05:59)`
  const perPerson = space.tariffs.some((t) => t.type === 'HeureParPersonne')
  const total = perPerson ? hours * amount('HeureParPersonne') * persons : hours * amount('Heure')
  return `Environ ${formatMoney(total)} pour ${hours} h (chaque heure entamée est due).`
}

function AvailabilityBox({ availability, error }: { availability: Availability | null; error: string | null }) {
  if (error) return <div className={styles.availability}>{error}</div>
  if (!availability) return <div className={styles.availability}>Vérification des disponibilités…</div>

  if (availability.type === 'PostePartage') {
    return (
      <div className={styles.availability} aria-live="polite">
        {availability.isAvailable ? (
          <span className={styles.ok}>
            {availability.remainingPlaces} place{availability.remainingPlaces > 1 ? 's' : ''} libre
            {availability.remainingPlaces > 1 ? 's' : ''} sur {availability.capacity} à cette heure
          </span>
        ) : (
          <span className={styles.ko}>Complet à cette heure : choisissez un autre horaire ou un autre espace.</span>
        )}
      </div>
    )
  }

  return (
    <div className={styles.availability} aria-live="polite">
      {availability.isAvailable ? (
        <span className={styles.ok}>Créneau libre</span>
      ) : (
        <span className={styles.ko}>Créneau déjà pris</span>
      )}
      {availability.busySlots.length > 0 && (
        <>
          <div className="small muted" style={{ marginTop: 6 }}>
            Déjà réservé ce jour-là :
          </div>
          <ul className="small">
            {availability.busySlots.map((slot) => (
              <li key={slot.start}>
                {formatTime(slot.start)} – {formatTime(slot.end)}
              </li>
            ))}
          </ul>
        </>
      )}
    </div>
  )
}

function Confirmation({ booking }: { booking: Booking }) {
  const isQuote = booking.status === 'DevisDemande'
  return (
    <div className={`card ${styles.confirmation}`}>
      <h1>{isQuote ? 'Demande de devis envoyée' : 'Réservation confirmée'}</h1>
      <p className="muted">
        {isQuote ? 'Conservez ce code pour suivre votre demande :' : "Présentez ce code à l'accueil :"}
      </p>
      <div className={styles.code} aria-label={`Code de réservation ${booking.code.split('').join(' ')}`}>
        {booking.code}
      </div>
      <dl className="details" style={{ textAlign: 'left' }}>
        <dt>Espace</dt>
        <dd>{booking.spaceName}</dd>
        <dt>Arrivée prévue</dt>
        <dd>{formatDateTime(booking.plannedArrival)}</dd>
        {booking.spaceType !== 'PostePartage' && (
          <>
            <dt>Jusqu'à</dt>
            <dd>{formatTime(booking.plannedEnd)}</dd>
            <dt>Personnes</dt>
            <dd>{booking.persons}</dd>
          </>
        )}
        <dt>Statut</dt>
        <dd>
          <StatusBadge status={booking.status} />
        </dd>
        {booking.isSubscriber && (
          <>
            <dt>Abonnement</dt>
            <dd>Abonné·e : accès inclus aux postes partagés</dd>
          </>
        )}
      </dl>
      <ul className={styles.reminders}>
        {isQuote ? (
          <li>Le gestionnaire étudie votre demande et vous recontacte avec un devis.</li>
        ) : (
          <li>
            <strong>Votre place est gardée 15 minutes</strong> après l'heure prévue, soit jusqu'à{' '}
            {formatTime(booking.holdUntil)}. Au-delà, elle est libérée.
          </li>
        )}
        <li>
          <strong>Paiement au départ</strong>, selon le temps réellement passé (espèces ou carte).
        </li>
        <li>
          <strong>Annulation gratuite</strong> à tout moment avec votre code et votre téléphone.
        </li>
      </ul>
      <div className="row" style={{ justifyContent: 'center', marginTop: 20 }}>
        <Link className="btn btn-secondary" to={`/ma-reservation?code=${booking.code}&phone=${booking.phone}`}>
          Voir ou annuler
        </Link>
        <Link className="btn" to="/">
          Retour à l'accueil
        </Link>
      </div>
    </div>
  )
}

export default function BookingPage({ mode }: { mode: Mode }) {
  const [params] = useSearchParams()
  const { data, error: loadError, loading } = useApi<SpacesResponse>('/api/spaces')

  const spaces = (data?.spaces ?? []).filter((s) => (mode === 'devis' ? s.onQuote : !s.onQuote))
  const [chosenId, setChosenId] = useState<number | null>(params.get('espace') ? Number(params.get('espace')) : null)
  const space = spaces.find((s) => s.id === chosenId) ?? spaces[0]

  const [fullName, setFullName] = useState('')
  const [phone, setPhone] = useState('')
  // Valeurs par défaut tirées de l'heure du serveur, tant que l'utilisateur n'a rien saisi.
  const [dateInput, setDate] = useState<string | null>(null)
  const [timeInput, setTime] = useState<string | null>(null)
  const date = dateInput ?? (data ? todayInFes(data.localNow) : '')
  const time = timeInput ?? (data ? nextQuarterHour(data.localNow) : '')
  const [persons, setPersons] = useState(1)
  const [durationHours, setDurationHours] = useState(mode === 'devis' ? 2 : 1)

  const [availability, setAvailability] = useState<Availability | null>(null)
  const [availabilityError, setAvailabilityError] = useState<string | null>(null)
  const [submitError, setSubmitError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)
  const [confirmed, setConfirmed] = useState<Booking | null>(null)

  const isShared = space?.type === 'PostePartage'
  const spaceId = space?.id

  // Disponibilité recalculée à chaque changement (avec un petit délai pour ne pas appeler l'API à chaque frappe).
  useEffect(() => {
    if (!spaceId || !date || !time) return
    const timer = setTimeout(async () => {
      setAvailability(null)
      try {
        const { data } = await api.get<Availability>(`/api/spaces/${spaceId}/availability`, {
          params: { date, time, durationHours },
        })
        setAvailability(data)
        setAvailabilityError(null)
      } catch (e) {
        setAvailabilityError(errorMessage(e))
      }
    }, 300)
    return () => clearTimeout(timer)
  }, [spaceId, date, time, durationHours])

  async function submit(event: FormEvent) {
    event.preventDefault()
    if (!space) return
    setSubmitting(true)
    setSubmitError(null)
    try {
      const { data } = await api.post<Booking>('/api/bookings', {
        spaceId: space.id,
        fullName,
        phone,
        date,
        time,
        persons: isShared ? null : persons,
        durationHours: isShared ? null : durationHours,
      })
      setConfirmed(data)
      window.scrollTo({ top: 0 })
    } catch (e) {
      setSubmitError(errorMessage(e))
    } finally {
      setSubmitting(false)
    }
  }

  if (confirmed) {
    return (
      <div className="container narrow page">
        <Confirmation booking={confirmed} />
      </div>
    )
  }

  const maxHours = space?.type === 'Cabine' ? 8 : 12

  return (
    <div className="container narrow page">
      <h1>{mode === 'devis' ? 'Demande de devis pour une salle' : 'Réserver une place'}</h1>
      <p className="muted">
        {mode === 'devis'
          ? 'Indiquez le créneau souhaité et le nombre de participants. Le gestionnaire valide la demande et vous propose un prix.'
          : 'Pas besoin de compte. Vous recevez un code à présenter à l’accueil, et vous payez au départ.'}
      </p>

      <ErrorAlert message={loadError} />
      {loading && <Loading />}

      {space && (
        <form className="card stack" onSubmit={submit}>
          <fieldset className={styles.spaces}>
            <legend className="label" style={{ marginBottom: 8 }}>
              Espace
            </legend>
            {spaces.map((s) => (
              <label key={s.id} className={styles.spaceOption}>
                <input
                  type="radio"
                  name="space"
                  value={s.id}
                  checked={s.id === space.id}
                  onChange={() => {
                    setChosenId(s.id)
                    setPersons(1)
                  }}
                />
                <span>
                  <strong>{s.name}</strong>
                  <small>{s.type === 'PostePartage' ? `${s.capacity} places` : `Jusqu'à ${s.capacity} pers.`}</small>
                </span>
              </label>
            ))}
          </fieldset>

          <p className="small muted" style={{ margin: 0 }}>
            {priceHint(space, durationHours, persons)}
          </p>

          <div className="grid grid-2">
            <div className="field">
              <label htmlFor="date">Date d'arrivée</label>
              <input id="date" type="date" required min={todayInFes(data?.localNow)} value={date} onChange={(e) => setDate(e.target.value)} />
            </div>
            <div className="field">
              <label htmlFor="time">Heure d'arrivée</label>
              <input id="time" type="time" required step={300} value={time} onChange={(e) => setTime(e.target.value)} />
            </div>
          </div>

          {!isShared && (
            <div className="grid grid-2">
              <div className="field">
                <label htmlFor="persons">Nombre de personnes</label>
                <select id="persons" value={persons} onChange={(e) => setPersons(Number(e.target.value))}>
                  {Array.from({ length: space.capacity }, (_, i) => i + 1).map((n) => (
                    <option key={n} value={n}>
                      {n}
                    </option>
                  ))}
                </select>
              </div>
              <div className="field">
                <label htmlFor="duration">Durée prévue</label>
                <select id="duration" value={durationHours} onChange={(e) => setDurationHours(Number(e.target.value))}>
                  {Array.from({ length: maxHours }, (_, i) => i + 1).map((n) => (
                    <option key={n} value={n}>
                      {n} h
                    </option>
                  ))}
                </select>
              </div>
            </div>
          )}

          <AvailabilityBox availability={availability} error={availabilityError} />

          <div className="field">
            <label htmlFor="fullName">Nom complet</label>
            <input
              id="fullName"
              required
              minLength={3}
              maxLength={100}
              autoComplete="name"
              value={fullName}
              onChange={(e) => setFullName(e.target.value)}
            />
          </div>
          <div className="field">
            <label htmlFor="phone">Téléphone</label>
            <input
              id="phone"
              type="tel"
              required
              inputMode="tel"
              autoComplete="tel"
              placeholder="06XXXXXXXX"
              value={phone}
              onChange={(e) => setPhone(e.target.value)}
            />
            <span className="hint">Mobile marocain (06 ou 07). Il sert à retrouver ou annuler votre réservation.</span>
          </div>

          <ErrorAlert message={submitError} />

          <button className="btn btn-block" type="submit" disabled={submitting || availability?.isAvailable === false}>
            {submitting ? 'Envoi…' : mode === 'devis' ? 'Envoyer la demande de devis' : 'Confirmer la réservation'}
          </button>
        </form>
      )}
    </div>
  )
}
