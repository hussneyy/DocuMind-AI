import { Route, Routes } from 'react-router-dom'
import { Footer } from './components/layout/Footer'
import { Navbar } from './components/layout/Navbar'
import { LandingPage } from './pages/LandingPage'
import { NotFoundPage } from './pages/NotFoundPage'

function App() {
  return (
    <div className="flex min-h-screen flex-col">
      <Navbar />
      <div className="flex-1">
        <Routes>
          <Route path="/" element={<LandingPage />} />
          <Route path="*" element={<NotFoundPage />} />
        </Routes>
      </div>
      <Footer />
    </div>
  )
}

export default App
