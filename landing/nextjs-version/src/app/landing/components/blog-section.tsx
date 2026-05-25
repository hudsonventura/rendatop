"use client"

import { useEffect, useState } from 'react'
import Link from 'next/link'
import { ArrowRight, CalendarDays, PenSquare } from 'lucide-react'
import { Badge } from '@/components/ui/badge'
import { Card, CardContent } from '@/components/ui/card'
import type { PublicBlogPostListItem } from '@/lib/blog-api'

type BlogSectionProps = {
  backendBaseUrl: string
}

function formatPublishedAt(value: string | null) {
  if (!value) return 'Em preparação'

  return new Intl.DateTimeFormat('pt-BR', {
    dateStyle: 'medium',
  }).format(new Date(value))
}

export function BlogSection({ backendBaseUrl }: BlogSectionProps) {
  const [posts, setPosts] = useState<PublicBlogPostListItem[]>([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    let cancelled = false
    const apiBaseUrl = (backendBaseUrl || '').trim().replace(/\/+$/, '')

    if (!apiBaseUrl) {
      setLoading(false)
      return
    }

    fetch(`${apiBaseUrl}/public/blog/posts?limit=3`)
      .then((response) => {
        if (!response.ok) {
          throw new Error('Falha ao carregar posts')
        }

        return response.json()
      })
      .then((data) => {
        if (cancelled) return
        setPosts(data?.items || [])
      })
      .catch(() => {
        if (cancelled) return
        setPosts([])
      })
      .finally(() => {
        if (cancelled) return
        setLoading(false)
      })

    return () => {
      cancelled = true
    }
  }, [backendBaseUrl])

  return (
    <section id="blog" className="bg-muted/30 py-24 sm:py-32">
      <div id="screenshots" className="relative -top-24" />
      <div className="container mx-auto px-4 sm:px-6 lg:px-8">
        <div className="mb-16 flex flex-col gap-6 lg:flex-row lg:items-end lg:justify-between">
          <div className="max-w-2xl">
            <Badge variant="outline" className="mb-4">Blog</Badge>
            <h2 className="text-3xl font-bold tracking-tight sm:text-4xl">
              Dicas e informações sobre investimentos
            </h2>
            <p className="mt-4 text-lg text-muted-foreground">
              Tenha conteúdo relevante para te ajudar a aproveitar melhor as funcionalidades do app e acompanhar as principais novidades do mercado financeiro. Novos artigos serão publicados regularmente para te manter informado e inspirado na sua jornada de investimentos.
            </p>
          </div>

          <Link
            href="/blog"
            className="inline-flex items-center gap-2 text-sm font-semibold text-primary transition-opacity hover:opacity-80"
          >
            Ver todos os artigos
            <ArrowRight className="h-4 w-4" />
          </Link>
        </div>

        {loading ? (
          <div className="grid grid-cols-1 gap-6 lg:grid-cols-3">
            {Array.from({ length: 3 }).map((_, index) => (
              <div key={index} className="h-[28rem] animate-pulse rounded-3xl border bg-card" />
            ))}
          </div>
        ) : posts.length > 0 ? (
          <div className="grid grid-cols-1 gap-6 lg:grid-cols-3">
            {posts.map((post) => (
              <Link key={post.id} href={`/blog/${post.slug}`} className="group block">
                <Card className="h-full overflow-hidden rounded-3xl border bg-card py-0 transition-transform duration-300 group-hover:-translate-y-1">
                  <div className="aspect-[16/10] overflow-hidden border-b bg-muted">
                    {post.cover_image_url ? (
                      <img
                        src={post.cover_image_url}
                        alt={post.title}
                        className="h-full w-full object-cover transition-transform duration-500 group-hover:scale-[1.02]"
                      />
                    ) : (
                      <div className="flex h-full items-center justify-center bg-gradient-to-br from-primary/15 via-transparent to-primary/5">
                        <PenSquare className="h-10 w-10 text-primary/60" />
                      </div>
                    )}
                  </div>
                  <CardContent className="space-y-4 p-6">
                    <div className="flex items-center gap-3 text-xs uppercase tracking-[0.2em] text-muted-foreground">
                      <span className="inline-flex items-center gap-1">
                        <CalendarDays className="h-3.5 w-3.5" />
                        {formatPublishedAt(post.published_at)}
                      </span>
                      <span>{post.author_user_name}</span>
                    </div>
                    <div>
                      <h3 className="text-2xl font-semibold leading-tight">{post.title}</h3>
                      <p className="mt-3 text-muted-foreground">{post.excerpt}</p>
                    </div>
                    <span className="inline-flex items-center gap-2 text-sm font-semibold text-primary">
                      Continuar leitura
                      <ArrowRight className="h-4 w-4 transition-transform duration-300 group-hover:translate-x-1" />
                    </span>
                  </CardContent>
                </Card>
              </Link>
            ))}
          </div>
        ) : (
          <Card className="rounded-3xl border-dashed">
            <CardContent className="px-6 py-16 text-center">
              <h3 className="text-xl font-semibold">Novos artigos chegando</h3>
              <p className="mt-3 text-muted-foreground">
                Assim que as primeiras postagens forem publicadas, elas aparecerão aqui com destaque.
              </p>
            </CardContent>
          </Card>
        )}
      </div>
    </section>
  )
}
