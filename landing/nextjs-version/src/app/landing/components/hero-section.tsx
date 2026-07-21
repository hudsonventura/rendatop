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
        <div data-scroll-reveal-item className="mx-auto max-w-4xl text-center">
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
            No RendaTop você tem dashboard, carteira, calendário com notificações de vencimentos e leitura de comprovantes automáticas para você gerenciar e acompanhar seus investimentos com mais organizaçao.
          </p>

        </div>

        <div data-scroll-reveal-item className="">
            <img className='rounded-lg' src="landing.png"/>
        </div>
      </div>
    </section>
  )
}
