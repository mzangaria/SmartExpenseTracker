import { lazy, Suspense } from 'react'
import { Navigate, Route, Routes } from 'react-router-dom'
import { ProtectedRoute } from './components/ProtectedRoute.jsx'
import { Shell } from './components/Shell.jsx'
import { useAuth } from './hooks/useAuth.js'

const LandingPage = lazy(() => import('./pages/LandingPage.jsx').then((module) => ({ default: module.LandingPage })))
const LoginPage = lazy(() => import('./pages/LoginPage.jsx').then((module) => ({ default: module.LoginPage })))
const RegisterPage = lazy(() => import('./pages/RegisterPage.jsx').then((module) => ({ default: module.RegisterPage })))
const DashboardPage = lazy(() => import('./pages/DashboardPage.jsx').then((module) => ({ default: module.DashboardPage })))
const ExpensesPage = lazy(() => import('./pages/ExpensesPage.jsx').then((module) => ({ default: module.ExpensesPage })))
const ProfilePage = lazy(() => import('./pages/ProfilePage.jsx').then((module) => ({ default: module.ProfilePage })))
const InboxPage = lazy(() => import('./pages/InboxPage.jsx').then((module) => ({ default: module.InboxPage })))

function App() {
  const { isAuthenticated } = useAuth()

  return (
    <Suspense fallback={<div className="page-shell"><p>Loading page...</p></div>}>
      <Routes>
        <Route path="/" element={<LandingPage />} />
        <Route path="/login" element={isAuthenticated ? <Navigate to="/dashboard" replace /> : <LoginPage />} />
        <Route path="/register" element={isAuthenticated ? <Navigate to="/dashboard" replace /> : <RegisterPage />} />
        <Route
          path="/dashboard"
          element={
            <ProtectedRoute>
              <Shell>
                <DashboardPage />
              </Shell>
            </ProtectedRoute>
          }
        />
        <Route
          path="/expenses"
          element={
            <ProtectedRoute>
              <Shell>
                <ExpensesPage />
              </Shell>
            </ProtectedRoute>
          }
        />
        <Route
          path="/expenses/new"
          element={
            <ProtectedRoute>
              <Shell>
                <ExpensesPage mode="create" />
              </Shell>
            </ProtectedRoute>
          }
        />
        <Route
          path="/expenses/:expenseId/edit"
          element={
            <ProtectedRoute>
              <Shell>
                <ExpensesPage mode="edit" />
              </Shell>
            </ProtectedRoute>
          }
        />
        <Route
          path="/profile"
          element={
            <ProtectedRoute>
              <Shell>
                <ProfilePage />
              </Shell>
            </ProtectedRoute>
          }
        />
        <Route
          path="/inbox"
          element={
            <ProtectedRoute>
              <Shell>
                <InboxPage />
              </Shell>
            </ProtectedRoute>
          }
        />
        <Route path="*" element={<Navigate to={isAuthenticated ? '/dashboard' : '/'} replace />} />
      </Routes>
    </Suspense>
  )
}

export default App
