import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { ErrorAlert, Loading, StatusBadge } from '../../components/common'
import { useApi } from '../../useApi'
import { formatMoney, formatTime, statusLabels, todayInFes } from '../../format'
import type { Booking, BookingStatus, Occupancy } from '../../types'
import styles from './Admin.module.css'

function OccupancyTile({ item }: { item: Occupancy }) {
  const used = item.present + item.expected
  const percent = Math.min(100, Math.round((used / item.units) * 100))
  const isShared = item.type === 'PostePartage'
  return (
    <div className={`${styles.tile} ${item.free === 0 ? styles.tileFull : ''}`}>
      <span className={styles.tileName}>{item.spaceName}</span>
      <span className={styles.tileValue}>
        {isShared ? `${used}/${item.units}` : used > 0 ? 'Occupée' : 'Libre'}
      </span>
      <div className={styles.meter} aria-hidden="true">
        <span style={{ width: `${percent}%` }} />
      </div>
      <span className="small muted">
        {item.present} présent{item.present > 1 ? 's' : ''} · {item.expected} attendu{item.expected > 1 ? 's' : ''}
      </span>
    </div>
  )
}

export default function AdminDashboardPage() {
  const [date, setDate] = useState(todayInFes)
  const [status, setStatus] = useState<BookingStatus | ''>('')

  const occupancy = useApi<Occupancy[]>('/api/admin/occupancy')
  const bookings = useApi<Booking[]>(`/api/admin/bookings?date=${date}${status ? `&status=${status}` : ''}`)

  // Rafraîchissement automatique toutes les 30 secondes (occupation en direct).
  const { reload: reloadOccupancy } = occupancy
  const { reload: reloadBookings } = bookings
  useEffect(() => {
    const timer = setInterval(() => {
      reloadOccupancy()
      reloadBookings()
    }, 30_000)
    return () => clearInterval(timer)
  }, [reloadOccupancy, reloadBookings])

  return (
    <div className="container page stack">
      <section>
        <div className="row" style={{ justifyContent: 'space-between', marginBottom: 12 }}>
          <h1 style={{ margin: 0 }}>Occupation en direct</h1>
          <button
            type="button"
            className="btn btn-secondary btn-small"
            onClick={() => {
              occupancy.reload()
              bookings.reload()
            }}
          >
            Actualiser
          </button>
        </div>
        <ErrorAlert message={occupancy.error} />
        {occupancy.data && (
          <div className={styles.occupancy}>
            {occupancy.data.map((item) => (
              <OccupancyTile key={item.spaceId} item={item} />
            ))}
          </div>
        )}
      </section>

      <section className="stack">
        <h2 style={{ margin: 0 }}>Réservations</h2>
        <div className={styles.filters}>
          <div className="field">
            <label htmlFor="date">Date</label>
            <input id="date" type="date" value={date} onChange={(e) => setDate(e.target.value)} />
          </div>
          <div className="field">
            <label htmlFor="status">Statut</label>
            <select id="status" value={status} onChange={(e) => setStatus(e.target.value as BookingStatus | '')}>
              <option value="">Tous</option>
              {Object.entries(statusLabels).map(([value, label]) => (
                <option key={value} value={value}>
                  {label}
                </option>
              ))}
            </select>
          </div>
        </div>

        <ErrorAlert message={bookings.error} />
        {bookings.loading && !bookings.data && <Loading />}
        {bookings.data?.length === 0 && <p className="muted">Aucune réservation pour ces critères.</p>}

        {bookings.data && bookings.data.length > 0 && (
          <table className="table">
            <thead>
              <tr>
                <th>Heure</th>
                <th>Code</th>
                <th>Client</th>
                <th>Espace</th>
                <th>Statut</th>
                <th>Montant</th>
                <th>
                  <span className="sr-only">Actions</span>
                </th>
              </tr>
            </thead>
            <tbody>
              {bookings.data.map((b) => (
                <tr key={b.code}>
                  <td data-label="Heure">{formatTime(b.plannedArrival)}</td>
                  <td data-label="Code">
                    <strong>{b.code}</strong>
                  </td>
                  <td data-label="Client">
                    {b.fullName}
                    <div className="small muted">
                      {b.phone}
                      {b.isSubscriber && ' · abonné'}
                    </div>
                  </td>
                  <td data-label="Espace">
                    {b.spaceName}
                    {b.persons > 1 && <span className="small muted"> · {b.persons} pers.</span>}
                  </td>
                  <td data-label="Statut">
                    <StatusBadge status={b.status} />
                  </td>
                  <td data-label="Montant">{b.amountDue !== null ? formatMoney(b.amountDue) : '—'}</td>
                  <td data-label="Action">
                    {b.status === 'Confirmee' && (
                      <Link className="btn btn-small" to={`/admin/accueil?code=${b.code}`}>
                        Check-in
                      </Link>
                    )}
                    {b.status === 'Arrivee' && (
                      <Link className="btn btn-small btn-secondary" to={`/admin/accueil?code=${b.code}`}>
                        Check-out
                      </Link>
                    )}
                    {b.status === 'DevisDemande' && (
                      <Link className="btn btn-small btn-secondary" to="/admin/devis">
                        Traiter
                      </Link>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </section>
    </div>
  )
}
