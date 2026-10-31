import { useState, type FormEvent } from 'react'
import { api, errorMessage } from '../../api'
import { ErrorAlert, Loading } from '../../components/common'
import { useApi } from '../../useApi'
import type { Tariff } from '../../types'
import styles from './Admin.module.css'

export default function AdminPricesPage() {
  const { data, error, loading, reload } = useApi<Tariff[]>('/api/admin/prices')
  // Montants modifiés, par identifiant de tarif (les autres gardent leur valeur actuelle).
  const [edits, setEdits] = useState<Record<number, string>>({})
  const [saveError, setSaveError] = useState<string | null>(null)
  const [message, setMessage] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  const changed = Object.entries(edits).filter(([id, value]) => data?.find((t) => t.id === Number(id))?.amount !== Number(value))

  async function save(event: FormEvent) {
    event.preventDefault()
    setBusy(true)
    setSaveError(null)
    setMessage(null)
    try {
      await api.put('/api/admin/prices', changed.map(([id, value]) => ({ id: Number(id), amount: Number(value) })))
      setEdits({})
      setMessage('Tarifs enregistrés. Ils s’appliquent aux prochains check-out.')
      reload()
    } catch (e) {
      setSaveError(errorMessage(e))
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="container narrow page stack">
      <div>
        <h1 style={{ margin: 0 }}>Tarifs</h1>
        <p className="muted" style={{ margin: 0 }}>
          Les salles privées n'ont pas de tarif fixe : leur prix est saisi en acceptant chaque devis.
        </p>
      </div>
      {message && <div className="alert alert-success">{message}</div>}
      <ErrorAlert message={error} />
      {loading && !data && <Loading />}

      {data && (
        <form className="stack" onSubmit={save}>
          <table className="table">
            <thead>
              <tr>
                <th>Espace</th>
                <th>Tarif</th>
                <th>Montant (DH)</th>
              </tr>
            </thead>
            <tbody>
              {data.map((t) => (
                <tr key={t.id}>
                  <td data-label="Espace">{t.spaceName ?? 'Abonnement'}</td>
                  <td data-label="Tarif">{t.label}</td>
                  <td data-label="Montant">
                    <input
                      className={styles.priceInput}
                      type="number"
                      min={0}
                      step={1}
                      required
                      aria-label={`${t.spaceName ?? 'Abonnement'} – ${t.label}`}
                      value={edits[t.id] ?? String(t.amount)}
                      onChange={(e) => setEdits({ ...edits, [t.id]: e.target.value })}
                    />
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          <ErrorAlert message={saveError} />
          <button className="btn" type="submit" disabled={busy || changed.length === 0}>
            Enregistrer {changed.length > 0 && `(${changed.length})`}
          </button>
        </form>
      )}
    </div>
  )
}
