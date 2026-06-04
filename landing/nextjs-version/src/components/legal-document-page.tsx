import Link from "next/link"
import { ShieldCheck } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Separator } from "@/components/ui/separator"
import { LandingFooter } from "@/app/landing/components/footer"
import { LandingNavbar } from "@/app/landing/components/navbar"
import { LEGAL_LAST_UPDATED } from "@/content/legal"

type LegalSection = {
  title: string
  paragraphs: string[]
}

type LegalDocumentPageProps = {
  title: string
  description: string
  sections: readonly LegalSection[]
}

export function LegalDocumentPage({
  title,
  description,
  sections,
}: LegalDocumentPageProps) {
  return (
    <div className="min-h-screen bg-background">
      <LandingNavbar />

      <main className="mx-auto max-w-5xl px-4 py-10 sm:px-6 lg:px-8">
        <div className="mb-8 flex flex-wrap items-center justify-between gap-3">
          <div>
            <Link
              href="/"
              className="inline-flex items-center gap-2 text-sm font-semibold text-muted-foreground transition-colors hover:text-foreground"
            >
              <ShieldCheck className="h-4 w-4" />
              RendaTop
            </Link>
            <h1 className="mt-4 text-3xl font-bold tracking-tight sm:text-4xl">
              {title}
            </h1>
            <p className="mt-3 max-w-3xl text-sm text-muted-foreground">
              {description}
            </p>
          </div>
          <div className="flex flex-wrap gap-2">
            <Button variant="outline" asChild>
              <Link href="/app/login">Entrar</Link>
            </Button>
            <Button asChild>
              <Link href="/app/signup">Criar conta</Link>
            </Button>
          </div>
        </div>

        <Card className="border-border/80 shadow-sm">
          <CardHeader className="space-y-3">
            <CardTitle className="text-xl">Informacoes gerais</CardTitle>
            <p className="text-sm text-muted-foreground">
              Ultima atualizacao: {LEGAL_LAST_UPDATED}
            </p>
            <p className="text-sm text-muted-foreground">
              Este documento foi publicado para estabelecer regras gerais do servico
              e transparencia sobre tratamento de dados, cookies e uso da plataforma.
            </p>
          </CardHeader>
          <CardContent className="space-y-8">
            {sections.map((section) => (
              <section key={section.title} className="space-y-3">
                <h2 className="text-lg font-semibold">{section.title}</h2>
                <div className="space-y-3 text-sm leading-7 text-foreground/80">
                  {section.paragraphs.map((paragraph) => (
                    <p key={paragraph}>{paragraph}</p>
                  ))}
                </div>
              </section>
            ))}

            <Separator />

            <div className="rounded-2xl bg-muted p-4 text-sm text-muted-foreground">
              Para solicitacoes relacionadas a dados pessoais, conta, suporte ou
              exercicio de direitos, utilize os canais oficiais do proprio RendaTop.
            </div>
          </CardContent>
        </Card>
      </main>

      <LandingFooter />
    </div>
  )
}
