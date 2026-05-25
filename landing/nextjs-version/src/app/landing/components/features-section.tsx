"use client"

import {
  BarChart3,
  Zap,
  Bell,
  ArrowRight,
  CalendarDays,
  ShieldCheck,
  Sparkles,
} from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { ScreenshotPlaceholder } from '@/components/screenshot-placeholder'

const mainFeatures = [
  {
    icon: BarChart3,
    title: 'Dashboard com contexto',
    description: 'Veja vencimentos proximos, distribuicao por banco e evolucao da carteira.',
  }
]

const secondaryFeatures = [
  {
    icon: CalendarDays,
    title: 'Calendario financeiro',
    description: 'Acompanhe aplicacoes e vencimentos em uma visao mensal clara.',
  },
  {
    icon: Zap,
    title: 'Separacao por disponibilidade',
    description: 'Diferencie rapidamente o que ja pode ser resgatado do que segue bloqueado ate o vencimento.',
  },
]

export function FeaturesSection() {
  return (
    <section id="features" className="py-24 sm:py-32 bg-muted/30">
      <div className="container mx-auto px-4 sm:px-6 lg:px-8">
        <div className="mx-auto max-w-2xl text-center mb-16">
          <Badge variant="outline" className="mb-4">Principais Recursos</Badge>
          <h2 className="text-3xl font-bold tracking-tight sm:text-5xl mb-4">
            Tudo o que você precisa para acompanhar seus investimentos no dia-a-dia
          </h2>
          <p className="text-lg text-muted-foreground">
            O app RentaTop foi estruturado para concentrar o acompanhamento da carteira em poucas telas bem definidas,
            com linguagem direta e foco no que precisa ser acompanhado ao longo do mês.<br />
            Muitos investimentos espalhados entre vários banco? Aqui você pode acompanhar tudo em um so lugar.
          </p>
        </div>

        <div className="grid items-center gap-12 lg:grid-cols-2 lg:gap-8 xl:gap-16 mb-24">
          <img className="w-full h-auto max-w-2xl rounded-xl shadow-xl border border-neutral-800" src="dash1.png" alt="App screenshot" />
          <div className="space-y-6">
            <div className="space-y-4">
              <h3 className="text-2xl font-semibold tracking-tight text-balance sm:text-3xl">
                Acompanhamento claro desde a primeira tela
              </h3>
              <p className="text-muted-foreground text-base text-pretty">
                O dashboard concentra o que costuma gerar mais consulta ao longo da semana:
                vencimentos próximos, distribuição da carteira e visão consolidada do patrimonio investido.
              </p>
            </div>

            <ul className="grid gap-4 sm:grid-cols-2">
              {mainFeatures.map((feature, index) => (
                <li key={index} className="group hover:bg-accent/5 flex items-start gap-3 p-2 rounded-lg transition-colors">
                  <div className="mt-0.5 flex shrink-0 items-center justify-center">
                    <feature.icon className="size-5 text-primary" aria-hidden="true" />
                  </div>
                  <div>
                    <h3 className="text-foreground font-medium">{feature.title}</h3>
                    <p className="text-muted-foreground mt-1 text-sm">{feature.description}</p>
                  </div>
                </li>
              ))}
            </ul>

          
          </div>
        </div>

        <div className="grid items-center gap-12 lg:grid-cols-2 lg:gap-8 xl:gap-16">
          <div className="space-y-6 order-2 lg:order-1">
            <div className="space-y-4">
              <h3 className="text-2xl font-semibold tracking-tight text-balance sm:text-3xl">
                Operação da carteira com mais previsibilidade
              </h3>
              <p className="text-muted-foreground text-base text-pretty">
                A carteira foi dividida para facilitar a leitura do que esta disponível para resgate,
                do que ainda aguarda vencimento e do que precisa de atenção por notificação ou calendário.
              </p>
            </div>

            <ul className="grid gap-4 sm:grid-cols-2">
              {secondaryFeatures.map((feature, index) => (
                <li key={index} className="group hover:bg-accent/5 flex items-start gap-3 p-2 rounded-lg transition-colors">
                  <div className="mt-0.5 flex shrink-0 items-center justify-center">
                    <feature.icon className="size-5 text-primary" aria-hidden="true" />
                  </div>
                  <div>
                    <h3 className="text-foreground font-medium">{feature.title}</h3>
                    <p className="text-muted-foreground mt-1 text-sm">{feature.description}</p>
                  </div>
                </li>
              ))}
            </ul>

           
          </div>
          <img className="w-full h-auto max-w-2xl rounded-xl shadow-xl border border-neutral-800 order-1 lg:order-2" src="calendar1.png" alt="App screenshot" />

        </div>
      </div>
    </section>
  )
}
