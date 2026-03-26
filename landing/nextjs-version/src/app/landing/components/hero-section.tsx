"use client"

import Link from 'next/link'
import { ArrowRight, Play } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { DotPattern } from '@/components/dot-pattern'
import { ScreenshotPlaceholder } from '@/components/screenshot-placeholder'

export function HeroSection() {
  return (
    <section id="hero" className="relative overflow-hidden bg-gradient-to-b from-background to-background/80 pt-16 sm:pt-20 pb-16">
      <div className="absolute inset-0">
        <DotPattern className="opacity-100" size="md" fadeStyle="ellipse" />
      </div>

      <div className="container mx-auto px-4 sm:px-6 lg:px-8 relative">
        <div className="mx-auto max-w-4xl text-center">
          <Badge variant="outline" className="mb-6">
            Gestao de investimentos em um so lugar
          </Badge>

          <h1 className="mb-6 text-4xl font-bold tracking-tight sm:text-6xl lg:text-7xl">
            Acompanhe sua carteira
            <span className="bg-gradient-to-r from-primary to-primary/60 bg-clip-text text-transparent">
              {" "}com mais clareza{" "}
            </span>
            e menos planilha
          </h1>

          <p className="mx-auto mb-10 max-w-2xl text-lg text-muted-foreground sm:text-xl">
            O RendaTop centraliza dashboard, carteira, calendario, notificacoes e configuracoes para voce acompanhar seus investimentos com mais organizacao.
          </p>

          <div className="flex flex-col gap-4 sm:flex-row sm:justify-center">
            <Button size="lg" className="text-base cursor-pointer" asChild>
              <Link href="/app/signup">
                Criar conta
                <ArrowRight className="ml-2 h-4 w-4" />
              </Link>
            </Button>
            <Button variant="outline" size="lg" className="text-base cursor-pointer" asChild>
              <a href="#screenshots">
                <Play className="mr-2 h-4 w-4" />
                Ver telas do app
              </a>
            </Button>
          </div>
        </div>

        <div className="mx-auto mt-20 max-w-6xl">
          <div className="relative group">
            <div className="absolute top-2 lg:-top-8 left-1/2 transform -translate-x-1/2 w-[90%] mx-auto h-24 lg:h-80 bg-primary/50 rounded-full blur-3xl"></div>
            <ScreenshotPlaceholder
              title="Dashboard principal"
              subtitle="Espaco reservado para a captura da tela inicial"
              caption="Aqui voce pode inserir um screenshot do dashboard com vencimentos proximos, distribuicao por banco e evolucao da carteira."
              badges={['Vencimentos dos proximos 30 dias', 'Distribuicao por banco', 'Evolucao da carteira']}
            />
          </div>
        </div>
      </div>
    </section>
  )
}
