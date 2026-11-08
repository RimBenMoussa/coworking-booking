import { useState } from 'react'
import { api, errorMessage } from '../../api'
import { ErrorAlert, Loading } from '../../components/common'
import { useApi } from '../../useApi'
import { formatDate, formatMoney, subscriptionStatusLabels } from '../../format'
import type { Subscription, SubscriptionStatus } from '../../types'

export default function AdminSubscriptionsPage() {
  const [status, setStatus] = useState<SubscriptionStatus | ''>('EnAttenteDePaiement')
  const { data, error, loading, reload } = useApi<Subscription[]>(
    `/api/admin/subscriptions${status ? `?status=${status}` : ''}`,
  )
  const [actionError, setActionError] = useState<string | null>(null)
  const [message, setMessage] = useState<string | null>(null)

  async function activate(subscription: Subscription) {
    if (!window.confirm(`Confirmer le paiement de ${formatMoney(subscription.price)} par ${subscription.fullName} ?`)) return
    setActionError(null)
    try {
      const { data: active } = await api.post<{ startDate: string; endDate: string }>(
        `/api/admin/subscriptions/${subscription.id}/activate`,
      )
      setMessage(`Abonnement de ${subscription.fullName} actif du ${formatDate(active.startDate)} au ${formatDate(active.endDate)}.`)
      reload()
    } catch (e) {
      setActionError(errorMessage(e))
    }
  }

  return (
    <div className="container page stack">
      <div className="row" style={{ justifyContent: 'space-between' }}>
        <h1 style={{ margin: 0 }}>Abonnements</h1>
        <div className="field" style={{ minWidth: 220 }}>
          <label htmlFor="status" className="sr-only">
            Statut
          </label>
          <select id="status" value={status} onChange={(e) => setStatus(e.target.value as SubscriptionStatus | '')}>
            <option value="EnAttenteDePaiement">En attente de paiement</option>
            <option value="Active">Actifs</option>
            <option value="">Tous</option>
          </select>
        </div>
      </div>

      {message && <div className="alert alert-success">{message}</div>}
      <ErrorAlert message={error ?? actionError} />
      {loading && !data && <Loading />}
      {data?.length === 0 && <p className="muted">Aucun abonnement.</p>}

      {data && data.length > 0 && (
        <table className="table">
          <thead>
            <tr>
              <th>Client</th>
              <th>Contact</th>
              <th>Période</th>
              <th>Statut</th>
              <th>
                <span className="sr-only">Action</span>
              </th>
            </tr>
          </thead>
          <tbody>
            {data.map((s) => (
              <tr key={s.id}>
                <td data-label="Client">{s.fullName}</td>
                <td data-label="Contact">
                  {s.phone}
                  <div className="small muted">{s.email}</div>
                </td>
                <td data-label="Période">
                  {formatDate(s.startDate)} → {formatDate(s.endDate)}
                </td>
                <td data-label="Statut">
                  <span className={`badge badge-${s.status}`}>{subscriptionStatusLabels[s.status]}</span>
                  {s.status === 'Active' && !s.isCurrentlyValid && <span className="small muted"> (hors période)</span>}
                </td>
                <td data-label="Action">
                  {s.status === 'EnAttenteDePaiement' && (
                    <button className="btn btn-small" type="button" onClick={() => activate(s)}>
                      Encaisser {formatMoney(s.price)} et activer
                    </button>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  )
}
