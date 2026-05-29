import { API_CONFIG } from "@/config/api"

export type PublicBlogPostListItem = {
  id: string
  slug: string
  title: string
  excerpt: string
  author_user_name: string
  cover_image_url: string | null
  published_at: string | null
  public_post_url: string
}

export type PublicBlogPostListResponse = {
  items: PublicBlogPostListItem[]
}

export type PublicBlogPostDetail = {
  id: string
  slug: string
  title: string
  excerpt: string
  body_html: string
  body_text: string
  author_user_name: string
  cover_image_url: string | null
  published_at: string | null
  created_at: string
  updated_at: string
  public_post_url: string
}

function getBackendBaseUrl() {
  return API_CONFIG.BASE_URL;
}

async function fetchJson<T>(path: string): Promise<T> {
  const baseUrl = getBackendBaseUrl()

  if (!baseUrl) {
    throw new Error('BASE_URL_SERVER não configurado para o blog público.')
  }

  const response = await fetch(`${baseUrl}${path}`, {
    next: { revalidate: 60 },
  })

  if (!response.ok) {
    throw new Error(`Falha ao carregar ${path}: ${response.status}`)
  }

  return response.json()
}

export async function fetchPublishedBlogPosts(limit?: number) {
  const suffix = typeof limit === 'number' && limit > 0 ? `?limit=${limit}` : ''
  return fetchJson<PublicBlogPostListResponse>(`/public/blog/posts${suffix}`)
}

export async function fetchPublishedBlogPostBySlug(slug: string) {
  const baseUrl = getBackendBaseUrl()

  if (!baseUrl) {
    throw new Error('BASE_URL_SERVER não configurado para o blog público.')
  }

  const response = await fetch(`${baseUrl}/public/blog/posts/${encodeURIComponent(slug)}`, {
    next: { revalidate: 60 },
  })

  if (response.status === 404) {
    return null
  }

  if (!response.ok) {
    throw new Error(`Falha ao carregar o post ${slug}: ${response.status}`)
  }

  return response.json() as Promise<PublicBlogPostDetail>
}

