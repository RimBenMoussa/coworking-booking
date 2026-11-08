import type { BookingStatus, SpaceType, SubscriptionStatus } from './types'

// Le serveur est la seule référence pour l'heure de Fès : l'API renvoie des dates déjà en heure locale
// avec leur décalage (ex. "2026-11-02T09:00:00+01:00"). On affiche cette heure telle quelle, sans la
// reconvertir avec la base de fuseaux horaires du navigateur, qui peut être différente de celle du serveur.

export const statusLabels: Record<BookingStatus, string> = {
  Confirmee: 'Confirmée',
  DevisDemande: 'Devis demandé',
  Arrivee: 'Arrivée',
  Terminee: 'Terminée',
  Annulee: 'Annulée',
  NonPresente: 'Non présenté',
  Refusee: 'Refusée',
}

export const spaceTypeLabels: Record<SpaceType, string> = {
  PostePartage: 'Poste partagé',
  Cabine: 'Cabine',
  SallePrivee: 'Salle privée',
}

export const subscriptionStatusLabels: Record<SubscriptionStatus, string> = {
  EnAttenteDePaiement: 'En attente de paiement',
  Active: 'Active',
}

export const paymentLabels = { Especes: 'Espèces', Carte: 'Carte' } as const

// "2026-11-02T09:00:00+01:00" -> Date dont l'heure UTC vaut 09:00, à formater avec timeZone: 'UTC'.
function wallClock(iso: string): Date {
  const m = /^(\d{4})-(\d{2})-(\d{2})(?:T(\d{2}):(\d{2}))?/.exec(iso)
  if (!m) return new Date(iso)
  return new Date(Date.UTC(+m[1], +m[2] - 1, +m[3], +(m[4] ?? 12), +(m[5] ?? 0)))
}

const dateTimeFormat = new Intl.DateTimeFormat('fr-FR', {
  timeZone: 'UTC',
  weekday: 'short',
  day: 'numeric',
  month: 'short',
  hour: '2-digit',
  minute: '2-digit',
})
const timeFormat = new Intl.DateTimeFormat('fr-FR', { timeZone: 'UTC', hour: '2-digit', minute: '2-digit' })
const dateFormat = new Intl.DateTimeFormat('fr-FR', { timeZone: 'UTC', day: 'numeric', month: 'long', year: 'numeric' })

export const formatDateTime = (iso: string) => dateTimeFormat.format(wallClock(iso))
export const formatTime = (iso: string) => timeFormat.format(wallClock(iso))
export const formatDate = (iso: string) => dateFormat.format(wallClock(iso))

export const formatMoney = (amount: number) => `${amount.toLocaleString('fr-FR')} DH`

export function formatDuration(minutes: number): string {
  const h = Math.floor(minutes / 60)
  const m = minutes % 60
  if (h === 0) return `${m} min`
  return m === 0 ? `${h} h` : `${h} h ${String(m).padStart(2, '0')}`
}

// Date du jour à Fès pour les valeurs par défaut des formulaires (format "YYYY-MM-DD").
// Sans heure serveur disponible, on se fie au navigateur : l'écart éventuel ne joue qu'autour de minuit.
export function todayInFes(serverNow?: string): string {
  if (serverNow) return serverNow.slice(0, 10)
  return new Intl.DateTimeFormat('en-CA', { timeZone: 'Africa/Casablanca' }).format(new Date())
}

// Prochain quart d'heure après l'heure serveur, ex. "…T09:07:12+01:00" -> "09:15".
export function nextQuarterHour(serverNow: string): string {
  const hour = Number(serverNow.slice(11, 13))
  const minute = Number(serverNow.slice(14, 16))
  const total = Math.min(hour * 60 + Math.ceil((minute + 1) / 15) * 15, 23 * 60 + 45)
  return `${String(Math.floor(total / 60)).padStart(2, '0')}:${String(total % 60).padStart(2, '0')}`
}
