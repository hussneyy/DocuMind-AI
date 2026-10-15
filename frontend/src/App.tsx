import { Route, Routes } from 'react-router-dom'
import { ProtectedRoute } from './components/auth/ProtectedRoute'
import { Footer } from './components/layout/Footer'
import { Navbar } from './components/layout/Navbar'
import { DocumentLibraryPage } from './pages/DocumentLibraryPage'
import { LandingPage } from './pages/LandingPage'
import { LoginPage } from './pages/LoginPage'
import { NotFoundPage } from './pages/NotFoundPage'
import { RegisterPage } from './pages/RegisterPage'
import { SearchPage } from './pages/SearchPage'

function App() {
  return (
    <div className="flex min-h-screen flex-col">
      <Navbar />
      <div className="flex-1">
        <Routes>
          <Route path="/" element={<LandingPage />} />
          <Route path="/login" element={<LoginPage />} />
          <Route path="/register" element={<RegisterPage />} />

          <Route element={<ProtectedRoute />}>
            <Route path="/documents" element={<DocumentLibraryPage />} />
            <Route path="/search" element={<SearchPage />} />
          </Route>

          <Route path="*" element={<NotFoundPage />} />
        </Routes>
      </div>
      <Footer />
    </div>
  )
}

export default App
