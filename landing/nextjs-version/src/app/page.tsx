import type { Metadata } from 'next'
import { LandingPageContent } from './landing/landing-page-content'

// Metadata for the landing page
export const metadata: Metadata = {
  title: 'RendaTop | Gestao de investimentos com mais clareza',
  description: 'Organize sua carteira, acompanhe vencimentos, receba notificacoes e visualize sua evolucao financeira em um unico painel.',
  keywords: ['rendatop', 'gestao de investimentos', 'renda fixa', 'calendario financeiro', 'notificacoes de vencimento'],
  openGraph: {
    title: 'RendaTop | Gestao de investimentos com mais clareza',
    description: 'Dashboard, carteira, calendario e notificacoes para acompanhar seus investimentos com organizacao.',
    type: 'website',
  },
  twitter: {
    card: 'summary_large_image',
    title: 'RendaTop | Gestao de investimentos com mais clareza',
    description: 'Organize sua carteira e acompanhe vencimentos, alertas e evolucao dos investimentos.',
  },
}

export default function HomePage() {
  const backendBaseUrl = (process.env.BASE_URL_SERVER || '').trim()

  return <LandingPageContent backendBaseUrl={backendBaseUrl} />
}
