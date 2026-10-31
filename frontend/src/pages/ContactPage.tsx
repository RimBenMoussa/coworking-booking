import { useState, type ChangeEvent, type FormEvent } from 'react'
import { api, errorMessage } from '../api'
import { ErrorAlert } from '../components/common'

const emptyForm = { name: '', email: '', phone: '', message: '' }

export default function ContactPage() {
  const [form, setForm] = useState(emptyForm)
  const [error, setError] = useState<string | null>(null)
  const [sent, setSent] = useState(false)
  const [busy, setBusy] = useState(false)

  const update =
    (field: keyof typeof form) => (e: ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) =>
      setForm({ ...form, [field]: e.target.value })

  async function submit(event: FormEvent) {
    event.preventDefault()
    setBusy(true)
    setError(null)
    try {
      await api.post('/api/contact', {
        name: form.name,
        email: form.email || null,
        phone: form.phone || null,
        message: form.message,
      })
      setSent(true)
      setForm(emptyForm)
    } catch (e) {
      setError(errorMessage(e))
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="container narrow page stack">
      <div>
        <h1>Contact</h1>
        <p className="muted" style={{ margin: 0 }}>
          Une question sur les espaces, un événement à organiser ? Laissez-nous un message, on vous répond rapidement.
        </p>
      </div>

      {sent && <div className="alert alert-success">Merci, votre message a bien été envoyé.</div>}

      <form className="card stack" onSubmit={submit}>
        <div className="field">
          <label htmlFor="name">Nom</label>
          <input id="name" required minLength={2} autoComplete="name" value={form.name} onChange={update('name')} />
        </div>
        <div className="grid grid-2">
          <div className="field">
            <label htmlFor="email">Email</label>
            <input id="email" type="email" autoComplete="email" value={form.email} onChange={update('email')} />
          </div>
          <div className="field">
            <label htmlFor="phone">Téléphone</label>
            <input id="phone" type="tel" placeholder="06XXXXXXXX" autoComplete="tel" value={form.phone} onChange={update('phone')} />
          </div>
        </div>
        <p className="small muted" style={{ margin: 0 }}>
          Indiquez au moins un email ou un téléphone pour qu'on puisse vous répondre.
        </p>
        <div className="field">
          <label htmlFor="message">Message</label>
          <textarea id="message" required minLength={5} maxLength={2000} value={form.message} onChange={update('message')} />
        </div>
        <ErrorAlert message={error} />
        <button className="btn" type="submit" disabled={busy}>
          {busy ? 'Envoi…' : 'Envoyer'}
        </button>
      </form>
    </div>
  )
}
