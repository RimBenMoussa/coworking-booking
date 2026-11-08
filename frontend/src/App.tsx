import { BrowserRouter, Link, Route, Routes } from 'react-router-dom'
import { AuthProvider, RequireAuth } from './auth'
import AdminLayout from './components/AdminLayout'
import Layout from './components/Layout'
import AdminDashboardPage from './pages/admin/AdminDashboardPage'
import AdminFrontDeskPage from './pages/admin/AdminFrontDeskPage'
import AdminLoginPage from './pages/admin/AdminLoginPage'
import AdminMessagesPage from './pages/admin/AdminMessagesPage'
import AdminPricesPage from './pages/admin/AdminPricesPage'
import AdminQuotesPage from './pages/admin/AdminQuotesPage'
import AdminSubscriptionsPage from './pages/admin/AdminSubscriptionsPage'
import BookingPage from './pages/BookingPage'
import ContactPage from './pages/ContactPage'
import HomePage from './pages/HomePage'
import MyBookingPage from './pages/MyBookingPage'
import SubscriptionPage from './pages/SubscriptionPage'

function NotFound() {
  return (
    <div className="container page">
      <h1>Page introuvable</h1>
      <Link to="/">Retour à l'accueil</Link>
    </div>
  )
}

export default function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <Routes>
          <Route element={<Layout />}>
            <Route index element={<HomePage />} />
            {/* key : repart d'un formulaire vide en passant de /reserver à /devis */}
            <Route path="reserver" element={<BookingPage key="reservation" mode="reservation" />} />
            <Route path="devis" element={<BookingPage key="devis" mode="devis" />} />
            <Route path="ma-reservation" element={<MyBookingPage />} />
            <Route path="abonnement" element={<SubscriptionPage />} />
            <Route path="contact" element={<ContactPage />} />
            <Route path="*" element={<NotFound />} />
          </Route>

          <Route path="admin/login" element={<AdminLoginPage />} />
          <Route
            path="admin"
            element={
              <RequireAuth>
                <AdminLayout />
              </RequireAuth>
            }
          >
            <Route index element={<AdminDashboardPage />} />
            <Route path="accueil" element={<AdminFrontDeskPage />} />
            <Route path="devis" element={<AdminQuotesPage />} />
            <Route path="abonnements" element={<AdminSubscriptionsPage />} />
            <Route path="tarifs" element={<AdminPricesPage />} />
            <Route path="messages" element={<AdminMessagesPage />} />
          </Route>
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  )
}
