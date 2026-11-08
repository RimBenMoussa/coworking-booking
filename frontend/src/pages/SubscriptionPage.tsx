import { useState, type ChangeEvent, type FormEvent } from 'react'
import { api, errorMessage } from '../api'
import { ErrorAlert } from '../components/common'
import { useApi } from '../useApi'
import { formatDate, formatMoney, todayInFes } from '../format'
import type { SpacesResponse } from '../types'

interface CreatedSubscription {
  id: number
  startDate: string
  endDate: string
  price: number
}

export default function SubscriptionPage() {
  const { data } = useApi<SpacesResponse>('/api/spaces')
  const [form, setForm] = useState({ fullName: '', phone: '', email: '', startDate: todayInFes() })
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)
  const [created, setCreated] = useState<CreatedSubscription | null>(null)

  const update = (field: keyof typeof form) => (e: ChangeEvent<HTMLInputElement>) =>
    setForm({ ...form, [field]: e.target.value })

  async function submit(event: FormEvent) {
    event.preventDefault()
    setBusy(true)
    setError(null)
    try {
      const response = await api.post<CreatedSubscription>('/api/subscriptions', form)
      setCreated(response.data)
    } catch (e) {
      setError(errorMessage(e))
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="container narrow page stack">
      <div>
        <h1>Abonnement mensuel</h1>
        <p className="muted" style={{ margin: 0 }}>
          {data ? formatMoney(data.subscriptionPrice) : '…'} par mois : accès illimité à l'Open Space et à la Terrasse
          panoramique, jour et nuit. Les phone box et les salles restent payantes.
        </p>
      </div>

      {created ? (
        <div className="card stack">
          <div className="alert alert-success">Demande enregistrée (n° {created.id}).</div>
          <p style={{ margin: 0 }}>
            Passez à l'accueil pour régler <strong>{formatMoney(created.price)}</strong> en espèces ou par carte. Votre
            abonnement est activé dès le paiement, pour un mois (du {formatDate(created.startDate)} au{' '}
            {formatDate(created.endDate)} si vous payez avant le début).
          </p>
          <p className="muted small" style={{ margin: 0 }}>
            Ensuite, réservez avec le même numéro de téléphone : vos postes partagés seront gratuits.
          </p>
        </div>
      ) : (
        <form className="card stack" onSubmit={submit}>
          <div className="field">
            <label htmlFor="fullName">Nom complet</label>
            <input id="fullName" required minLength={3} autoComplete="name" value={form.fullName} onChange={update('fullName')} />
          </div>
          <div className="grid grid-2">
            <div className="field">
              <label htmlFor="phone">Téléphone</label>
              <input id="phone" type="tel" required placeholder="06XXXXXXXX" autoComplete="tel" value={form.phone} onChange={update('phone')} />
            </div>
            <div className="field">
              <label htmlFor="email">Email</label>
              <input id="email" type="email" required autoComplete="email" value={form.email} onChange={update('email')} />
            </div>
          </div>
          <div className="field">
            <label htmlFor="startDate">Date de début souhaitée</label>
            <input id="startDate" type="date" required min={todayInFes()} value={form.startDate} onChange={update('startDate')} />
          </div>
          <ErrorAlert message={error} />
          <button className="btn" type="submit" disabled={busy}>
            {busy ? 'Envoi…' : "Demander l'abonnement"}
          </button>
        </form>
      )}
    </div>
  )
}
