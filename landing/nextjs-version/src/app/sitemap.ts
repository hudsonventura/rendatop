import type { MetadataRoute } from 'next'
import { LEGAL_LAST_UPDATED } from '@/content/legal'
import { fetchPublishedBlogPosts } from '@/lib/blog-api'
import { absoluteUrl } from '@/lib/seo'

function parseBrazilianDate(value: string) {
  const [day, month, year] = value.split(' de ')
  const monthIndexByName: Record<string, number> = {
    janeiro: 0,
    fevereiro: 1,
    marco: 2,
    abril: 3,
    maio: 4,
    junho: 5,
    julho: 6,
    agosto: 7,
    setembro: 8,
    outubro: 9,
    novembro: 10,
    dezembro: 11,
  }

  return new Date(Number(year), monthIndexByName[month], Number(day))
}

export default async function sitemap(): Promise<MetadataRoute.Sitemap> {
  const legalLastModified = parseBrazilianDate(LEGAL_LAST_UPDATED)
  const now = new Date()

  const routes: MetadataRoute.Sitemap = [
    {
      url: absoluteUrl('/'),
      lastModified: now,
      changeFrequency: 'weekly',
      priority: 1,
    },
    {
      url: absoluteUrl('/sobre'),
      lastModified: now,
      changeFrequency: 'monthly',
      priority: 0.8,
    },
    {
      url: absoluteUrl('/blog'),
      lastModified: now,
      changeFrequency: 'daily',
      priority: 0.8,
    },
    {
      url: absoluteUrl('/politica-de-privacidade'),
      lastModified: legalLastModified,
      changeFrequency: 'yearly',
      priority: 0.3,
    },
    {
      url: absoluteUrl('/termos-de-uso'),
      lastModified: legalLastModified,
      changeFrequency: 'yearly',
      priority: 0.3,
    },
    {
      url: absoluteUrl('/politica-de-cookies'),
      lastModified: legalLastModified,
      changeFrequency: 'yearly',
      priority: 0.3,
    },
  ]

  try {
    const response = await fetchPublishedBlogPosts()

    return [
      ...routes,
      ...response.items.map((post) => ({
        url: absoluteUrl(`/blog/${post.slug}`),
        lastModified: post.published_at ? new Date(post.published_at) : now,
        changeFrequency: 'weekly' as const,
        priority: 0.7,
      })),
    ]
  } catch {
    return routes
  }
}
