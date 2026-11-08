import { Link, NavLink, Outlet, useNavigate } from 'react-router-dom'
import { useAuth } from '../authContext'
import styles from './Layout.module.css'

const links = [
  { to: '/admin', label: 'Tableau du jour', end: true },
  { to: '/admin/accueil', label: 'Check-in / out' },
  { to: '/admin/devis', label: 'Devis' },
  { to: '/admin/abonnements', label: 'Abonnements' },
  { to: '/admin/tarifs', label: 'Tarifs' },
  { to: '/admin/messages', label: 'Messages' },
]

export default function AdminLayout() {
  const { logout } = useAuth()
  const navigate = useNavigate()

  return (
    <>
      <header className={styles.adminHeader}>
        <div className="container">
          <div className={styles.bar}>
            <Link to="/admin" className={styles.logo}>
              <span className={styles.logoMark} aria-hidden="true" />
              Gestionnaire
            </Link>
            <div className="row">
              <Link to="/" style={{ color: '#e9e1d9' }} className="small">
                Site public
              </Link>
              <button
                type="button"
                className={styles.logout}
                onClick={() => {
                  logout()
                  navigate('/admin/login')
                }}
              >
                Déconnexion
              </button>
            </div>
          </div>
          <nav className={styles.adminNav} aria-label="Navigation gestionnaire">
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
    </>
  )
}
