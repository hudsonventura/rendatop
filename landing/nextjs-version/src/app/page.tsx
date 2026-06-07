import type { Metadata } from 'next'
import { StructuredData } from '@/components/structured-data'
import { faqItems } from '@/content/faq'
import { absoluteUrl, createPageMetadata } from '@/lib/seo'
import { LandingPageContent } from './landing/landing-page-content'

const homeTitle = 'RendaTop | Gestao de investimentos com mais clareza'
const homeDescription =
  'Organize sua carteira, acompanhe vencimentos, receba notificacoes e visualize sua evolucao financeira em um unico painel.'

export const metadata: Metadata = createPageMetadata({
  title: homeTitle,
  description: homeDescription,
  path: '/',
  keywords: [
    'gestao de investimentos',
    'renda fixa',
    'controle de carteira',
    'calendario financeiro',
    'notificacoes de vencimento',
  ],
})

export default function HomePage() {
  const backendBaseUrl = (process.env.BASE_URL_SERVER || '').trim()
  const structuredData = [
    {
      '@context': 'https://schema.org',
      '@type': 'WebSite',
      name: 'RendaTop',
      url: absoluteUrl('/'),
      inLanguage: 'pt-BR',
      description: homeDescription,
    },
    {
      '@context': 'https://schema.org',
      '@type': 'Organization',
      name: 'RendaTop',
      url: absoluteUrl('/'),
      logo: absoluteUrl('/favicon.svg'),
      description:
        'Plataforma para organizar investimentos, acompanhar vencimentos, centralizar notificacoes e entender a evolucao da carteira.',
    },
    {
      '@context': 'https://schema.org',
      '@type': 'SoftwareApplication',
      name: 'RendaTop',
      applicationCategory: 'FinanceApplication',
      operatingSystem: 'Web, Android, iOS',
      url: absoluteUrl('/'),
      description: homeDescription,
      offers: {
        '@type': 'Offer',
        price: '0',
        priceCurrency: 'BRL',
      },
    },
    {
      '@context': 'https://schema.org',
      '@type': 'FAQPage',
      mainEntity: faqItems.map((item) => ({
        '@type': 'Question',
        name: item.question,
        acceptedAnswer: {
          '@type': 'Answer',
          text: item.answer,
        },
      })),
    },
  ]

  return (
    <>
      <StructuredData data={structuredData} />
      <LandingPageContent backendBaseUrl={backendBaseUrl} />
    </>
  )
}
