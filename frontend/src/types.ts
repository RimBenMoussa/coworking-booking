// Types des réponses de l'API (voir backend/src/CoworkingBooking.Api/DTOs).

export type SpaceType = 'PostePartage' | 'Cabine' | 'SallePrivee'

export type BookingStatus =
  | 'Confirmee'
  | 'DevisDemande'
  | 'Arrivee'
  | 'Terminee'
  | 'Annulee'
  | 'NonPresente'
  | 'Refusee'

export type TariffType = 'Court' | 'Journee' | 'Nuit' | 'Heure' | 'HeureParPersonne' | 'AbonnementMensuel'

export type PaymentMethod = 'Especes' | 'Carte'

export type SubscriptionStatus = 'EnAttenteDePaiement' | 'Active'

export interface Tariff {
  id: number
  spaceId: number | null
  spaceName: string | null
  type: TariffType
  label: string
  amount: number
}

export interface Space {
  id: number
  name: string
  type: SpaceType
  capacity: number
  description: string
  onQuote: boolean
  tariffs: Tariff[]
}

export interface SpacesResponse {
  spaces: Space[]
  subscriptionPrice: number
  localNow: string
}

export interface Slot {
  start: string
  end: string
}

export interface Availability {
  spaceId: number
  spaceName: string
  type: SpaceType
  start: string
  end: string
  isAvailable: boolean
  remainingPlaces: number
  capacity: number
  busySlots: Slot[]
}

export interface Booking {
  code: string
  status: BookingStatus
  spaceId: number
  spaceName: string
  spaceType: SpaceType
  fullName: string
  phone: string
  persons: number
  plannedArrival: string
  plannedEnd: string
  holdUntil: string
  isSubscriber: boolean
  checkInAt: string | null
  checkOutAt: string | null
  quoteAmount: number | null
  amountDue: number | null
  paymentMethod: PaymentMethod | null
  managerNote: string | null
  createdAt: string
}

export interface CheckOut {
  booking: Booking
  durationMinutes: number
  amount: number
  priceDetail: string
}

export interface Occupancy {
  spaceId: number
  spaceName: string
  type: SpaceType
  units: number
  present: number
  expected: number
  free: number
}

export interface Subscription {
  id: number
  fullName: string
  phone: string
  email: string
  startDate: string
  endDate: string
  status: SubscriptionStatus
  isCurrentlyValid: boolean
  price: number
  createdAt: string
  activatedAt: string | null
}

export interface ContactMessage {
  id: number
  name: string
  email: string | null
  phone: string | null
  message: string
  createdAt: string
}

export interface LoginResponse {
  token: string
  expiresAt: string
  email: string
}
