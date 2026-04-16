import type { Metadata } from 'next'
import Link from 'next/link'
import { ArrowRight, CalendarDays, PenSquare } from 'lucide-react'
import { Badge } from '@/components/ui/badge'
import { Card, CardContent } from '@/components/ui/card'
import { LandingFooter } from '@/app/landing/components/footer'
import { LandingNavbar } from '@/app/landing/components/navbar'
import { fetchPublishedBlogPosts } from '@/lib/blog-api'

export const metadata: Metadata = {
  title: 'Blog | RendaTop',
  description: 'Artigos sobre organização da carteira, clareza financeira e uso inteligente do RendaTop.',
}

export const dynamic = 'force-dynamic'

function formatPublishedAt(value: string | null) {
  if (!value) return 'Em preparação'

  return new Intl.DateTimeFormat('pt-BR', {
    dateStyle: 'long',
  }).format(new Date(value))
}

export default async function BlogIndexPage() {
  let posts = []

  try {
    const response = await fetchPublishedBlogPosts()
    posts = response.items || []
  } catch {
    posts = []
  }

  return (
    <div className="min-h-screen bg-background">
      <LandingNavbar />

      <main className="container mx-auto px-4 py-16 sm:px-6 lg:px-8">
        <div className="mx-auto mb-14 max-w-3xl text-center">
          <Badge variant="outline" className="mb-4">Blog</Badge>
          <h1 className="text-4xl font-bold tracking-tight sm:text-5xl">
            Conteúdo para quem quer investir com organização e contexto
          </h1>
          <p className="mt-6 text-lg text-muted-foreground">
            Aprenda a investir de maneira inteligente e com segurança, com conteúdos sobre organização da carteira, clareza financeira e uso inteligente do RendaTop. 
          </p>
        </div>

        {posts.length > 0 ? (
          <div className="grid grid-cols-1 gap-8 lg:grid-cols-2">
            {posts.map((post) => (
              <Link key={post.id} href={`/blog/${post.slug}`} className="group block">
                <Card className="h-full overflow-hidden rounded-[2rem] border bg-card py-0 transition-transform duration-300 group-hover:-translate-y-1">
                  <div className="aspect-[16/9] overflow-hidden border-b bg-muted">
                    {post.cover_image_url ? (
                      <img
                        src={post.cover_image_url}
                        alt={post.title}
                        className="h-full w-full object-cover transition-transform duration-500 group-hover:scale-[1.02]"
                      />
                    ) : (
                      <div className="flex h-full items-center justify-center bg-gradient-to-br from-primary/15 via-transparent to-primary/5">
                        <PenSquare className="h-12 w-12 text-primary/60" />
                      </div>
                    )}
                  </div>
                  <CardContent className="space-y-5 p-8">
                    <div className="flex flex-wrap items-center gap-3 text-xs uppercase tracking-[0.2em] text-muted-foreground">
                      <span className="inline-flex items-center gap-1">
                        <CalendarDays className="h-3.5 w-3.5" />
                        {formatPublishedAt(post.published_at)}
                      </span>
                      <span>{post.author_user_name}</span>
                    </div>
                    <div>
                      <h2 className="text-3xl font-semibold leading-tight">{post.title}</h2>
                      <p className="mt-4 text-base leading-7 text-muted-foreground">{post.excerpt}</p>
                    </div>
                    <span className="inline-flex items-center gap-2 text-sm font-semibold text-primary">
                      Ler artigo
                      <ArrowRight className="h-4 w-4 transition-transform duration-300 group-hover:translate-x-1" />
                    </span>
                  </CardContent>
                </Card>
              </Link>
            ))}
          </div>
        ) : (
          <Card className="mx-auto max-w-2xl rounded-[2rem] border-dashed">
            <CardContent className="px-8 py-16 text-center">
              <h2 className="text-2xl font-semibold">Nenhum artigo publicado ainda</h2>
              <p className="mt-3 text-muted-foreground">
                Assim que os primeiros posts forem publicados no painel admin, eles aparecerão aqui.
              </p>
            </CardContent>
          </Card>
        )}
      </main>

      <LandingFooter />
    </div>
  )
}
