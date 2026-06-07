import type { Metadata } from 'next'
import { createPageMetadata } from '@/lib/seo'
import { LandingPageContent } from './landing-page-content'

export const metadata: Metadata = createPageMetadata({
  title: 'RendaTop | Gestao de investimentos com mais clareza',
  description:
    'Organize sua carteira, acompanhe vencimentos, receba notificacoes e visualize sua evolucao financeira em um unico painel.',
  path: '/',
  noIndex: true,
})

export default function LandingPage() {
  const backendBaseUrl = (process.env.BASE_URL_SERVER || '').trim()

  return <LandingPageContent backendBaseUrl={backendBaseUrl} />
}
