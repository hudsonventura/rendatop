"use client"

import { useState, useEffect } from 'react'
import { X } from 'lucide-react'
import {
  BarChart3,
  Zap,
  ChartNoAxesGantt,
  CalendarDays,
  GitCompareArrows,
  GitFork,
  Landmark,
  FlaskConical
} from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { ScreenshotPlaceholder } from '@/components/screenshot-placeholder'

export const mainFeatures = [
  {
    icon: BarChart3,
    title: 'Dashboard com contexto',
    description: 'Veja vencimentos proximos, distribuicao por banco e evolucao da carteira.',
  }
]

export const secondaryFeatures = [
  {
    icon: CalendarDays,
    title: 'Calendario financeiro',
    description: 'Acompanhe aplicações e vencimentos em uma visao mensal clara.',
  },
  {
    icon: Zap,
    title: 'Separacao por disponibilidade',
    description: 'Diferencie rapidamente o que ja pode ser resgatado do que segue bloqueado ate o vencimento.',
  },
]

export const thirdFeatures = [
  {
    icon: GitCompareArrows,
    title: 'Controle',
    description: 'Acompanhe seus investimentos, realize resgates e reinvestimentos.',
  },
  {
    icon: Landmark,
    title: 'Impostos',
    description: 'Veja de forma separada cada imposto e saiba qual o melhor investimento para resgate.',
  },
]

export const fourthFeatures = [
  {
    icon: FlaskConical,
    title: 'Simulação',
    description: 'Simule o mesmo investimento em diferentes tipos de rendimento para otimizar seus ganhos.',
  },
  {
    icon: ChartNoAxesGantt,
    title: 'Comparação',
    description: 'A comparação permitirá analisar se o seu investimento é de fato interessante ou se pode ser melhor investido em outro banco.',
  },
]


export function FeaturesSection() {
  const [selectedImage, setSelectedImage] = useState<string | null>(null)

  useEffect(() => {
    const handleEscape = (e: KeyboardEvent) => {
      if (e.key === 'Escape') {
        setSelectedImage(null)
      }
    }

    if (selectedImage) {
      document.addEventListener('keydown', handleEscape)
      document.body.style.overflow = 'hidden'
    }

    return () => {
      document.removeEventListener('keydown', handleEscape)
      document.body.style.overflow = 'unset'
    }
  }, [selectedImage])

  return (
    <>
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
            <img 
              className="w-full h-auto max-w-2xl rounded-xl shadow-xl border border-neutral-800 cursor-pointer hover:shadow-2xl transition-shadow duration-300" 
              src="dash1.png" 
              alt="App screenshot" 
              onClick={() => setSelectedImage('dash1.png')}
            />
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

          <div id="calendario"className="grid items-center gap-12 lg:grid-cols-2 lg:gap-8 xl:gap-16">
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
            <img 
              className="w-full h-auto max-w-2xl rounded-xl shadow-xl border border-neutral-800 order-1 lg:order-2 cursor-pointer hover:shadow-2xl transition-shadow duration-300" 
              src="calendar1.png" 
              alt="App screenshot" 
              onClick={() => setSelectedImage('calendar1.png')}
            />
          </div>


          <div id="controle" className="grid items-center gap-12 lg:grid-cols-2 lg:gap-8 xl:gap-16 mb-24">
            <img 
              className="w-full h-auto max-w-2xl rounded-xl shadow-xl border border-neutral-800 cursor-pointer hover:shadow-2xl transition-shadow duration-300" 
              src="investments1.png" 
              alt="App screenshot" 
              onClick={() => setSelectedImage('investments1.png')}
            />
            <div className="space-y-6">
              <div className="space-y-4">
                <h3 className="text-2xl font-semibold tracking-tight text-balance sm:text-3xl">
                  Acompanhe os rendimentos e descontos de cada investimento
                </h3>
                <p className="text-muted-foreground text-base text-pretty">
                  Na nossa listagem de investimentos, você terá um detalhamento de cada investimento, seus rendimentos, descontos e prazos de vencimento.
                </p>
              </div>

              <ul className="grid gap-4 sm:grid-cols-2">
                {thirdFeatures.map((feature, index) => (
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


          <div id="simulacao" className="grid items-center gap-12 lg:grid-cols-2 lg:gap-8 xl:gap-16">
            <div className="space-y-6 order-2 lg:order-1">
              <div className="space-y-4">
                <h3 className="text-2xl font-semibold tracking-tight text-balance sm:text-3xl">
                  Simulação e comparação de investimentos
                </h3>
                <p className="text-muted-foreground text-base text-pretty">
                  Ao adicionar um investimento, você pode simular o rendimento e comparar com outros tipo investimentos e seus rencimentos.
                  Isso pode auxiliar no processo de tomada de decisão.
                </p>
              </div>

              <ul className="grid gap-4 sm:grid-cols-2">
                {fourthFeatures.map((feature, index) => (
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
            <img 
              className="w-full h-auto max-w-2xl rounded-xl shadow-xl border border-neutral-800 order-1 lg:order-2 cursor-pointer hover:shadow-2xl transition-shadow duration-300" 
              src="new-investment1.png" 
              alt="App screenshot" 
              onClick={() => setSelectedImage('new-investment1.png')}
            />
          </div>


        </div>
      </section>

      {/* Modal com animação */}
      {selectedImage && (
        <div 
          className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/80 backdrop-blur-sm animate-in fade-in duration-200"
          onClick={() => setSelectedImage(null)}
        >
          <div 
            className="relative max-w-7xl max-h-[90vh] w-full animate-in zoom-in-95 duration-200"
            onClick={(e) => e.stopPropagation()}
          >
            <img 
              src={selectedImage} 
              alt="Expanded screenshot" 
              className="w-full h-auto rounded-xl shadow-2xl"
            />
            <button
              onClick={() => setSelectedImage(null)}
              className="absolute top-4 right-4 p-2 bg-black/50 hover:bg-black/70 text-white rounded-lg transition-colors duration-200 backdrop-blur-sm"
            >
              <X size={24} />
            </button>
          </div>
        </div>
      )}
    </>
  )
}
