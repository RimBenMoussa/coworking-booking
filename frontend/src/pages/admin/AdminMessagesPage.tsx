import { ErrorAlert, Loading } from '../../components/common'
import { useApi } from '../../useApi'
import { formatDateTime } from '../../format'
import type { ContactMessage } from '../../types'

export default function AdminMessagesPage() {
  const { data, error, loading } = useApi<ContactMessage[]>('/api/admin/messages')

  return (
    <div className="container narrow page stack">
      <h1 style={{ margin: 0 }}>Messages</h1>
      <ErrorAlert message={error} />
      {loading && !data && <Loading />}
      {data?.length === 0 && <p className="muted">Aucun message pour le moment.</p>}
      {data?.map((m) => (
        <article key={m.id} className="card stack" style={{ gap: 8 }}>
          <div className="row" style={{ justifyContent: 'space-between' }}>
            <strong>{m.name}</strong>
            <span className="small muted">{formatDateTime(m.createdAt)}</span>
          </div>
          <div className="small">
            {m.email && <a href={`mailto:${m.email}`}>{m.email}</a>}
            {m.email && m.phone && ' · '}
            {m.phone && <a href={`tel:${m.phone}`}>{m.phone}</a>}
          </div>
          <p style={{ margin: 0, whiteSpace: 'pre-wrap' }}>{m.message}</p>
        </article>
      ))}
    </div>
  )
}
