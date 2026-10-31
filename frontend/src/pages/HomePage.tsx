import { Link } from 'react-router-dom'
import { ErrorAlert, Loading } from '../components/common'
import { useApi } from '../useApi'
import { formatMoney } from '../format'
import type { Space, SpacesResponse, SpaceType } from '../types'
import styles from './HomePage.module.css'

const sections: { type: SpaceType; title: string; intro: string }[] = [
  { type: 'PostePartage', title: 'Postes partagés', intro: "Installez-vous quand vous voulez : tarif court, journée ou nuit selon l'heure d'arrivée." },
  { type: 'Cabine', title: 'Phone box', intro: 'Pour un appel ou une visio au calme. Facturé par heure entamée.' },
  { type: 'SallePrivee', title: 'Salles privées', intro: 'Réunions, ateliers, formations : envoyez une demande, on vous répond avec un devis.' },
]

function SpaceCard({ space }: { space: Space }) {
  const link = space.onQuote ? `/devis?espace=${space.id}` : `/reserver?espace=${space.id}`
  return (
    <article className={`card ${styles.spaceCard}`}>
      <div className={styles.spaceHead}>
        <h3>{space.name}</h3>
        <span className={styles.capacity}>
          {space.type === 'PostePartage' ? `${space.capacity} places` : `${space.capacity} pers. max`}
        </span>
      </div>
      <p className="muted small" style={{ margin: 0 }}>
        {space.description}
      </p>
      <ul className={styles.prices}>
        {space.onQuote ? (
          <li>
            <span>Tarif</span>
            <strong>Sur devis</strong>
          </li>
        ) : (
          space.tariffs.map((t) => (
            <li key={t.id}>
              <span>{t.label}</span>
              <strong>{formatMoney(t.amount)}</strong>
            </li>
          ))
        )}
      </ul>
      <Link to={link} className="btn btn-secondary btn-block">
        {space.onQuote ? 'Demander un devis' : 'Réserver'}
      </Link>
    </article>
  )
}

export default function HomePage() {
  const { data, error, loading } = useApi<SpacesResponse>('/api/spaces')

  return (
    <>
      <section className={styles.hero}>
        <div className="container">
          <h1>Votre espace de travail à Fès, jour et nuit</h1>
          <p>
            Open Space, terrasse panoramique, phone box et salles de réunion. Réservez en une minute, sans compte : vous
            payez sur place, au départ, selon le temps passé.
          </p>
          <ul className={styles.pills}>
            <li>Ouvert 24 h/24 · 7 j/7</li>
            <li>Place gardée 15 min</li>
            <li>Annulation gratuite</li>
            <li>Paiement au départ</li>
          </ul>
          <div className={styles.heroActions}>
            <Link to="/reserver" className="btn">
              Réserver une place
            </Link>
            <Link to="/ma-reservation" className="btn btn-secondary">
              Retrouver ma réservation
            </Link>
          </div>
        </div>
      </section>

      <div className="container page">
        <ErrorAlert message={error} />
        {loading && <Loading />}

        {data &&
          sections.map((section) => (
            <section key={section.type} className={styles.section}>
              <h2>{section.title}</h2>
              <p className="muted">{section.intro}</p>
              <div className="grid grid-3">
                {data.spaces
                  .filter((s) => s.type === section.type)
                  .map((space) => (
                    <SpaceCard key={space.id} space={space} />
                  ))}
              </div>
            </section>
          ))}

        {data && (
          <section className={styles.section}>
            <div className={`card ${styles.subscription}`}>
              <div className="grid grid-2" style={{ alignItems: 'center' }}>
                <div>
                  <h2>Abonnement mensuel</h2>
                  <p style={{ margin: 0 }}>
                    Accès illimité à l'Open Space et à la Terrasse pendant un mois. Les phone box et les salles restent
                    payantes.
                  </p>
                </div>
                <div className="stack" style={{ alignItems: 'flex-start' }}>
                  <span className={styles.price}>{formatMoney(data.subscriptionPrice)} / mois</span>
                  <Link to="/abonnement" className="btn">
                    S'abonner
                  </Link>
                </div>
              </div>
            </div>
          </section>
        )}
      </div>
    </>
  )
}
