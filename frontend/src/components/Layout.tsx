import { useState } from 'react'
import { Link, NavLink, Outlet, useLocation } from 'react-router-dom'
import styles from './Layout.module.css'

const links = [
  { to: '/', label: 'Espaces & tarifs', end: true },
  { to: '/reserver', label: 'Réserver' },
  { to: '/devis', label: 'Salles (devis)' },
  { to: '/ma-reservation', label: 'Ma réservation' },
  { to: '/abonnement', label: 'Abonnement' },
  { to: '/contact', label: 'Contact' },
]

// Mise en page des pages publiques : en-tête avec menu (replié sur mobile) et pied de page.
export default function Layout() {
  const [menuOpen, setMenuOpen] = useState(false)
  const location = useLocation()
  const [lastPath, setLastPath] = useState(location.pathname)

  // Referme le menu mobile à chaque changement de page.
  if (location.pathname !== lastPath) {
    setLastPath(location.pathname)
    setMenuOpen(false)
  }

  return (
    <>
      <header className={styles.header}>
        <div className={`container ${styles.wrapper}`}>
          <div className={styles.bar}>
            <Link to="/" className={styles.logo}>
              <span className={styles.logoMark} aria-hidden="true" />
              Coworking Fès
            </Link>
            <button
              type="button"
              className={styles.menuButton}
              aria-expanded={menuOpen}
              aria-controls="main-nav"
              onClick={() => setMenuOpen(!menuOpen)}
            >
              {menuOpen ? 'Fermer' : 'Menu'}
            </button>
          </div>
          <nav id="main-nav" className={`${styles.nav} ${menuOpen ? styles.navOpen : ''}`} aria-label="Navigation principale">
            {links.map((link) => (
              <NavLink key={link.to} to={link.to} end={link.end}>
                {link.label}
              </NavLink>
            ))}
          </nav>
        </div>
      </header>

      <main>
        <Outlet />
      </main>

      <footer className={styles.footer}>
        <div className="container row" style={{ justifyContent: 'space-between' }}>
          <span>Ouvert 24 h/24, 7 j/7 · Fès · Paiement sur place en espèces ou par carte</span>
          <Link to="/admin">Espace gestionnaire</Link>
        </div>
      </footer>
    </>
  )
}
