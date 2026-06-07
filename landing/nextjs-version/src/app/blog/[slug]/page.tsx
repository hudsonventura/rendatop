import type { Metadata } from 'next'
import Link from 'next/link'
import { CalendarDays, ChevronLeft } from 'lucide-react'
import { Badge } from '@/components/ui/badge'
import { LandingFooter } from '@/app/landing/components/footer'
import { LandingNavbar } from '@/app/landing/components/navbar'
import { StructuredData } from '@/components/structured-data'
import { fetchPublishedBlogPostBySlug } from '@/lib/blog-api'
import { absoluteUrl, createPageMetadata } from '@/lib/seo'
import { notFound } from 'next/navigation'

type BlogPostPageProps = {
  params: Promise<{ slug: string }>
}

export const dynamic = 'force-dynamic'

function formatPublishedAt(value: string | null) {
  if (!value) return 'Em preparação'

  return new Intl.DateTimeFormat('pt-BR', {
    dateStyle: 'long',
  }).format(new Date(value))
}

export async function generateMetadata({ params }: BlogPostPageProps): Promise<Metadata> {
  const { slug } = await params
  const post = await fetchPublishedBlogPostBySlug(slug)

  if (!post) {
    return {
      title: 'Postagem não encontrada | RendaTop',
      robots: {
        index: false,
        follow: false,
      },
    }
  }

  return createPageMetadata({
    title: `${post.title} | Blog RendaTop`,
    description: post.excerpt,
    path: `/blog/${post.slug}`,
    type: 'article',
    images: post.cover_image_url
      ? [
          {
            url: post.cover_image_url,
            alt: post.title,
          },
        ]
      : undefined,
    publishedTime: post.published_at || undefined,
    modifiedTime: post.updated_at,
    keywords: ['blog de investimentos', 'artigo de investimentos', post.title],
  })
}

export default async function BlogPostPage({ params }: BlogPostPageProps) {
  const { slug } = await params
  const post = await fetchPublishedBlogPostBySlug(slug)

  if (!post) {
    notFound()
  }

  const structuredData = {
    '@context': 'https://schema.org',
    '@type': 'Article',
    headline: post.title,
    description: post.excerpt,
    image: post.cover_image_url ? [post.cover_image_url] : [absoluteUrl('/landing.png')],
    datePublished: post.published_at || post.created_at,
    dateModified: post.updated_at,
    mainEntityOfPage: absoluteUrl(`/blog/${post.slug}`),
    author: {
      '@type': 'Person',
      name: post.author_user_name,
    },
    publisher: {
      '@type': 'Organization',
      name: 'RendaTop',
      logo: {
        '@type': 'ImageObject',
        url: absoluteUrl('/favicon.svg'),
      },
    },
  }

  return (
    <div className="min-h-screen bg-background">
      <StructuredData data={structuredData} />
      <LandingNavbar />

      <main className="container mx-auto px-4 py-16 sm:px-6 lg:px-8">
        <div className="mx-auto max-w-4xl">
          <Link href="/blog" className="mb-8 inline-flex items-center gap-2 text-sm font-medium text-muted-foreground transition-colors hover:text-foreground">
            <ChevronLeft className="h-4 w-4" />
            Voltar para o blog
          </Link>

          <article className="overflow-hidden rounded-[2rem] border bg-card">
            {post.cover_image_url ? (
              <div className="aspect-[16/8] overflow-hidden border-b bg-muted">
                <img src={post.cover_image_url} alt={post.title} className="h-full w-full object-cover" />
              </div>
            ) : null}

            <div className="px-6 py-10 sm:px-10 lg:px-14">
              <div className="mb-8">
                <Badge variant="outline" className="mb-4">Blog</Badge>
                <h1 className="text-4xl font-bold tracking-tight sm:text-5xl">{post.title}</h1>
                <p className="mt-5 text-lg leading-8 text-muted-foreground">{post.excerpt}</p>

                <div className="mt-6 flex flex-wrap items-center gap-4 text-sm text-muted-foreground">
                  <span className="inline-flex items-center gap-2">
                    <CalendarDays className="h-4 w-4" />
                    {formatPublishedAt(post.published_at)}
                  </span>
                  <span>{post.author_user_name}</span>
                </div>
              </div>

              <div
                className="text-base leading-8 [&_a]:text-primary [&_a]:underline [&_blockquote]:my-6 [&_blockquote]:border-l-4 [&_blockquote]:border-border [&_blockquote]:pl-4 [&_blockquote]:italic [&_h1]:mb-4 [&_h1]:mt-8 [&_h1]:text-4xl [&_h1]:font-bold [&_h2]:mb-4 [&_h2]:mt-8 [&_h2]:text-3xl [&_h2]:font-semibold [&_h3]:mb-3 [&_h3]:mt-6 [&_h3]:text-2xl [&_h3]:font-semibold [&_img]:my-8 [&_img]:max-w-full [&_img]:rounded-[1.5rem] [&_img]:border [&_ol]:my-5 [&_ol]:list-decimal [&_ol]:pl-6 [&_p]:mb-5 [&_ul]:my-5 [&_ul]:list-disc [&_ul]:pl-6"
                dangerouslySetInnerHTML={{ __html: post.body_html }}
              />
            </div>
          </article>
        </div>
      </main>

      <LandingFooter />
    </div>
  )
}
