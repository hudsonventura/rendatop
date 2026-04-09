"use client"

import { useEffect } from 'react'
import { useSearchParams } from 'next/navigation'
import { LandingNavbar } from './components/navbar'
import { HeroSection } from './components/hero-section'
import { LogoCarousel } from './components/logo-carousel'
import { StatsSection } from './components/stats-section'
import { FeaturesSection } from './components/features-section'
import { BlogSection } from './components/blog-section'
import { PricingSection } from './components/pricing-section'
import { CTASection } from './components/cta-section'
import { ContactSection } from './components/contact-section'
import { FaqSection } from './components/faq-section'
import { LandingFooter } from './components/footer'
import { AboutSection } from './components/about-section'

export function LandingPageContent() {
  const searchParams = useSearchParams()

  useEffect(() => {
    const visit = (searchParams.get('visit') || '').trim()
    const normalizedVisit = visit ? visit.toLowerCase() : 'direct'
    const storageKey = `landing-next-visit:${window.location.pathname}:${normalizedVisit}`
    const apiBaseUrl = (process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000').replace(/\/+$/, '')

    if (sessionStorage.getItem(storageKey)) {
      return
    }

    sessionStorage.setItem(storageKey, '1')

    fetch(`${apiBaseUrl}/public/landing-visits`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ visit }),
    }).catch((error) => {
      sessionStorage.removeItem(storageKey)
      console.error('Erro ao registrar visita da landing page:', error)
    })
  }, [searchParams])

  return (
    <div className="min-h-screen bg-background">
      {/* Navigation */}
      <LandingNavbar />

      {/* Main Content */}
      <main>
        <HeroSection />
        <LogoCarousel />
        <StatsSection />
        <AboutSection />
        <FeaturesSection />
        <BlogSection />
        <PricingSection />
        <FaqSection />
        <CTASection />
        <ContactSection />
      </main>

      {/* Footer */}
      <LandingFooter />
    </div>
  )
}
