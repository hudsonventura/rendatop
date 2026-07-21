"use client"

import { useEffect } from 'react'
import { useSearchParams } from 'next/navigation'
import { LandingNavbar } from './components/navbar'
import { HeroSection } from './components/hero-section'
import { LogoCarousel } from './components/logo-carousel'
import AppDownloadButtons from './components/app-download-buttons'
import { StatsSection } from './components/stats-section'
import { FeaturesSection } from './components/features-section'
import { BlogSection } from './components/blog-section'
import { PricingSection } from './components/pricing-section'
import { CTASection } from './components/cta-section'
import { ContactSection } from './components/contact-section'
import { FaqSection } from './components/faq-section'
import { LandingFooter } from './components/footer'
import { AboutSection } from './components/about-section'
import { ScrollReveal } from './components/scroll-reveal'

type LandingPageContentProps = {
  backendBaseUrl: string
}

export function LandingPageContent({ backendBaseUrl }: LandingPageContentProps) {
  const searchParams = useSearchParams()

  useEffect(() => {
    const visit = (searchParams.get('visit') || '').trim()
    const normalizedVisit = visit ? visit.toLowerCase() : 'direct'
    const storageKey = `landing-next-visit:${window.location.pathname}:${normalizedVisit}`
    const apiBaseUrl = (backendBaseUrl || '').replace(/\/+$/, '')

    if (!apiBaseUrl) {
      console.error('BASE_URL_SERVER não configurado para registrar visitas da landing page.')
      return
    }

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
  }, [backendBaseUrl, searchParams])

  return (
    <div className="min-h-screen bg-background">
      {/* Navigation */}
      <LandingNavbar />

      {/* Main Content */}
      <main>
        <ScrollReveal><HeroSection /></ScrollReveal>
        <ScrollReveal><LogoCarousel /></ScrollReveal>
        <ScrollReveal><FeaturesSection /></ScrollReveal>
        <ScrollReveal><AppDownloadButtons /></ScrollReveal>
        <ScrollReveal><StatsSection /></ScrollReveal>
        <ScrollReveal><AboutSection /></ScrollReveal>
        <ScrollReveal><BlogSection backendBaseUrl={backendBaseUrl} /></ScrollReveal>
        <ScrollReveal><PricingSection /></ScrollReveal>
        {/* <CTASection /> */}
        <ScrollReveal><FaqSection /></ScrollReveal>
        <ScrollReveal><ContactSection /></ScrollReveal>
      </main>

      {/* Footer */}
      <ScrollReveal><LandingFooter /></ScrollReveal>
    </div>
  )
}
