import type { Metadata } from 'next'

export const SITE_NAME = 'RendaTop'
export const SITE_DESCRIPTION =
  'Gestao de investimentos com dashboard, carteira, calendario financeiro, notificacoes e acompanhamento claro da evolucao patrimonial.'
export const SITE_DEFAULT_TITLE = 'RendaTop | Gestao de investimentos com mais clareza'
export const SITE_DEFAULT_DESCRIPTION = SITE_DESCRIPTION
export const SITE_DEFAULT_KEYWORDS = [
  'RendaTop',
  'gestao de investimentos',
  'controle de investimentos',
  'renda fixa',
  'calendario financeiro',
  'notificacoes de vencimento',
  'organizacao da carteira',
]

const FALLBACK_SITE_URL = 'https://rendatop.com.br'

export type StructuredDataValue =
  | Record<string, unknown>
  | Array<Record<string, unknown>>

type MetadataImage = {
  url: string
  width?: number
  height?: number
  alt?: string
}

type CreatePageMetadataParams = {
  title: string
  description: string
  path: string
  keywords?: string[]
  type?: 'website' | 'article'
  noIndex?: boolean
  images?: MetadataImage[]
  publishedTime?: string
  modifiedTime?: string
}

export const DEFAULT_OG_IMAGE: MetadataImage = {
  url: '/landing.png',
  width: 1200,
  height: 630,
  alt: 'Painel do RendaTop para acompanhamento de investimentos',
}

function normalizeSiteUrl(value?: string | null) {
  if (!value) {
    return FALLBACK_SITE_URL
  }

  const trimmedValue = value.trim()

  if (!trimmedValue) {
    return FALLBACK_SITE_URL
  }

  if (/^https?:\/\//i.test(trimmedValue)) {
    return trimmedValue.replace(/\/+$/, '')
  }

  return `https://${trimmedValue.replace(/^\/+/, '').replace(/\/+$/, '')}`
}

export function getSiteUrl() {
  return normalizeSiteUrl(
    process.env.BASE_URL_LANDING ||
      process.env.NEXT_PUBLIC_BASE_URL_LANDING ||
      process.env.NEXT_PUBLIC_BASE_URL_CLIENT
  )
}

export function getMetadataBase() {
  return new URL(getSiteUrl())
}

export function absoluteUrl(path = '/') {
  return new URL(path, getSiteUrl()).toString()
}

export function createPageMetadata({
  title,
  description,
  path,
  keywords = [],
  type = 'website',
  noIndex = false,
  images,
  publishedTime,
  modifiedTime,
}: CreatePageMetadataParams): Metadata {
  const resolvedImages = images?.length ? images : [DEFAULT_OG_IMAGE]

  return {
    title,
    description,
    keywords: [...SITE_DEFAULT_KEYWORDS, ...keywords],
    alternates: {
      canonical: path,
    },
    robots: {
      index: !noIndex,
      follow: true,
      googleBot: {
        index: !noIndex,
        follow: true,
      },
    },
    openGraph: {
      title,
      description,
      url: path,
      siteName: SITE_NAME,
      locale: 'pt_BR',
      type,
      images: resolvedImages,
      ...(type === 'article'
        ? {
            publishedTime,
            modifiedTime,
          }
        : {}),
    },
    twitter: {
      card: 'summary_large_image',
      title,
      description,
      images: resolvedImages.map((image) => image.url),
    },
  }
}
