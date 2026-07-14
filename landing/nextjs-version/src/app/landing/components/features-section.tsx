"use client"

import { useState, useEffect } from 'react'
import { ChevronLeft, ChevronRight, X } from 'lucide-react'
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
import { Badge } from '@/components/ui/badge'

interface DashboardScreenshot {
  src: string
  alt: string
}

interface ScreenshotCarouselProps {
  images: DashboardScreenshot[]
  activeIndex: number
  onActiveIndexChange: (index: number) => void
  onOpen?: () => void
  expanded?: boolean
}

const dashboardScreenshots: DashboardScreenshot[] = [
  {
    src: '/dash1.png',
    alt: 'Dashboard do RendaTop com vencimentos próximos e distribuição da carteira por banco',
  },
  {
    src: '/dash2.png',
    alt: 'Visão geral do dashboard do RendaTop',
  },
]

function ScreenshotCarousel({
  images,
  activeIndex,
  onActiveIndexChange,
  onOpen,
  expanded = false,
}: ScreenshotCarouselProps) {
  const showNavigation = images.length > 1
  const goToPrevious = () => {
    onActiveIndexChange((activeIndex - 1 + images.length) % images.length)
  }

  const goToNext = () => {
    onActiveIndexChange((activeIndex + 1) % images.length)
  }

  const imageTrack = (
    <div
      className="flex w-full transition-transform duration-700 ease-in-out motion-reduce:transition-none"
      style={{ transform: `translateX(-${activeIndex * 100}%)` }}
    >
      {images.map((item, index) => (
        <div
          key={item.src}
          className={expanded
            ? 'flex h-[82vh] w-full shrink-0 items-center justify-center'
            : 'aspect-[4/3] w-full shrink-0'}
          aria-hidden={index !== activeIndex}
        >
          <img
            src={item.src}
            alt={index === activeIndex ? item.alt : ''}
            className="h-full w-full object-contain"
          />
        </div>
      ))}
    </div>
  )

  return (
    <div
      className={expanded ? 'w-full' : 'w-full max-w-2xl'}
      role="region"
      aria-roledescription="carrossel"
      aria-label="Capturas do dashboard do RendaTop"
    >
      <div className={`group relative overflow-hidden rounded-xl ${expanded ? 'bg-black/20' : 'border border-neutral-800 bg-background shadow-xl transition-shadow duration-300 hover:shadow-2xl'}`}>
        {onOpen ? (
          <button
            type="button"
            onClick={onOpen}
            className="block w-full cursor-zoom-in"
            aria-label={`Ampliar imagem ${activeIndex + 1} de ${images.length}`}
          >
            {imageTrack}
          </button>
        ) : imageTrack}

        {showNavigation && (
          <>
            <button
              type="button"
              onClick={goToPrevious}
              className="absolute left-3 top-1/2 z-10 flex size-10 -translate-y-1/2 items-center justify-center rounded-full bg-black/55 text-white shadow-md backdrop-blur-sm transition-colors hover:bg-black/75"
              aria-label="Ver captura anterior"
            >
              <ChevronLeft className="size-5" />
            </button>
            <button
              type="button"
              onClick={goToNext}
              className="absolute right-3 top-1/2 z-10 flex size-10 -translate-y-1/2 items-center justify-center rounded-full bg-black/55 text-white shadow-md backdrop-blur-sm transition-colors hover:bg-black/75"
              aria-label="Ver próxima captura"
            >
              <ChevronRight className="size-5" />
            </button>
            <span className="absolute bottom-3 right-3 rounded-full bg-black/55 px-2.5 py-1 text-xs font-medium text-white backdrop-blur-sm">
              {activeIndex + 1} / {images.length}
            </span>
          </>
        )}
      </div>

      {showNavigation && (
        <div className={`mt-3 flex justify-center gap-2 ${expanded ? 'text-white' : ''}`}>
          {images.map((item, index) => (
            <button
              key={item.src}
              type="button"
              onClick={() => onActiveIndexChange(index)}
              className={`h-2 rounded-full transition-all ${index === activeIndex ? 'w-7 bg-primary' : expanded ? 'w-2 bg-white/45 hover:bg-white/70' : 'w-2 bg-muted-foreground/35 hover:bg-muted-foreground/60'}`}
              aria-label={`Ver captura ${index + 1}`}
              aria-current={index === activeIndex ? 'true' : undefined}
            />
          ))}
        </div>
      )}
    </div>
  )
}

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
  {
    icon: ChartNoAxesGantt,
    title: 'Comprovante',
    description: 'Importe seu comprovante de investimento e deixe o RendaTop preencher os dados para você.',
  }
]


export function FeaturesSection() {
  const [selectedImage, setSelectedImage] = useState<string | null>(null)
  const [dashboardImageIndex, setDashboardImageIndex] = useState(0)
  const [dashboardGalleryOpen, setDashboardGalleryOpen] = useState(false)

  const modalOpen = Boolean(selectedImage) || dashboardGalleryOpen

  useEffect(() => {
    if (dashboardScreenshots.length <= 1) return

    const autoplayTimeout = window.setTimeout(() => {
      setDashboardImageIndex((current) =>
        (current + 1) % dashboardScreenshots.length)
    }, 5000)

    return () => window.clearTimeout(autoplayTimeout)
  }, [dashboardImageIndex])

  useEffect(() => {
    const handleEscape = (e: KeyboardEvent) => {
      if (e.key === 'Escape') {
        setSelectedImage(null)
        setDashboardGalleryOpen(false)
      }

      if (dashboardGalleryOpen && e.key === 'ArrowLeft') {
        setDashboardImageIndex((current) =>
          (current - 1 + dashboardScreenshots.length) % dashboardScreenshots.length)
      }

      if (dashboardGalleryOpen && e.key === 'ArrowRight') {
        setDashboardImageIndex((current) =>
          (current + 1) % dashboardScreenshots.length)
      }
    }

    if (modalOpen) {
      document.addEventListener('keydown', handleEscape)
      document.body.style.overflow = 'hidden'
    }

    return () => {
      document.removeEventListener('keydown', handleEscape)
      document.body.style.overflow = 'unset'
    }
  }, [dashboardGalleryOpen, modalOpen])

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
            <ScreenshotCarousel
              images={dashboardScreenshots}
              activeIndex={dashboardImageIndex}
              onActiveIndexChange={setDashboardImageIndex}
              onOpen={() => setDashboardGalleryOpen(true)}
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
      {modalOpen && (
        <div 
          className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/80 backdrop-blur-sm animate-in fade-in duration-200"
          onClick={() => {
            setSelectedImage(null)
            setDashboardGalleryOpen(false)
          }}
        >
          <div 
            className="relative max-w-7xl max-h-[90vh] w-full animate-in zoom-in-95 duration-200"
            onClick={(e) => e.stopPropagation()}
          >
            {dashboardGalleryOpen ? (
              <ScreenshotCarousel
                images={dashboardScreenshots}
                activeIndex={dashboardImageIndex}
                onActiveIndexChange={setDashboardImageIndex}
                expanded
              />
            ) : selectedImage ? (
              <img
                src={selectedImage}
                alt="Captura ampliada do RendaTop"
                className="max-h-[86vh] w-full rounded-xl object-contain shadow-2xl"
              />
            ) : null}
            <button
              onClick={() => {
                setSelectedImage(null)
                setDashboardGalleryOpen(false)
              }}
              className="absolute top-4 right-4 p-2 bg-black/50 hover:bg-black/70 text-white rounded-lg transition-colors duration-200 backdrop-blur-sm"
              aria-label="Fechar visualização ampliada"
            >
              <X size={24} />
            </button>
          </div>
        </div>
      )}
    </>
  )
}
