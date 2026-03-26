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
  },
  {
    icon: Zap,
    title: 'Simulacao no cadastro',
    description: 'Visualize estimativas de rendimento e comparativos durante o registro do investimento.',
  },
  {
    icon: ShieldCheck,
    title: 'Configuracoes centralizadas',
    description: 'Ajuste canais de alerta, calendario publico, seguranca e dados de contato.',
  },
  {
    icon: Sparkles,
    title: 'Leitura por IA',
    description: 'Importe comprovantes para preencher campos do investimento com apoio de IA.',
  },
]

const secondaryFeatures = [
  {
    icon: CalendarDays,
    title: 'Calendario financeiro',
    description: 'Acompanhe aplicacoes e vencimentos em uma visao mensal clara.',
  },
  {
    icon: Bell,
    title: 'Notificacoes e historico',
    description: 'Receba alertas e acompanhe o que ainda esta pendente de leitura.',
  },
  {
    icon: Zap,
    title: 'Separacao por disponibilidade',
    description: 'Diferencie rapidamente o que ja pode ser resgatado do que segue bloqueado ate o vencimento.',
  },
  {
    icon: ShieldCheck,
    title: 'Planos que acompanham o uso',
    description: 'Amplie limites de IA, calendario ICS e notificacoes conforme sua necessidade.',
  },
]

export function FeaturesSection() {
  return (
    <section id="features" className="py-24 sm:py-32 bg-muted/30">
      <div className="container mx-auto px-4 sm:px-6 lg:px-8">
        <div className="mx-auto max-w-2xl text-center mb-16">
          <Badge variant="outline" className="mb-4">Recursos principais</Badge>
          <h2 className="text-3xl font-bold tracking-tight sm:text-4xl mb-4">
            Tudo o que voce precisa para acompanhar seus investimentos no dia-a-dia
          </h2>
          <p className="text-lg text-muted-foreground">
            A landing foi ajustada para refletir o produto real: dashboard, carteira, calendario, notificacoes, configuracoes e assinatura.
          </p>
        </div>

        <div className="grid items-center gap-12 lg:grid-cols-2 lg:gap-8 xl:gap-16 mb-24">
          <ScreenshotPlaceholder
            title="Screenshot do dashboard"
            subtitle="Espaco para a visao principal do app"
            caption="Use esta area para inserir a captura do painel com vencimentos, grafico por banco e evolucao da carteira."
            badges={['Dashboard', 'Vencimentos', 'Graficos']}
          />
          <div className="space-y-6">
            <div className="space-y-4">
              <h3 className="text-2xl font-semibold tracking-tight text-balance sm:text-3xl">
                Acompanhamento claro desde a primeira tela
              </h3>
              <p className="text-muted-foreground text-base text-pretty">
                O dashboard concentra o que costuma gerar mais consulta ao longo da semana:
                vencimentos proximos, distribuicao da carteira e visao consolidada do patrimonio investido.
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

            <div className="flex flex-col sm:flex-row gap-4 pe-4 pt-2">
              <Button size="lg" className="cursor-pointer" asChild>
                <a href="/app/signup" className='flex items-center'>
                  Criar conta
                  <ArrowRight className="ms-2 size-4" aria-hidden="true" />
                </a>
              </Button>
              <Button size="lg" variant="outline" className="cursor-pointer" asChild>
                <a href="#screenshots">
                  Ver capturas
                </a>
              </Button>
            </div>
          </div>
        </div>

        <div className="grid items-center gap-12 lg:grid-cols-2 lg:gap-8 xl:gap-16">
          <div className="space-y-6 order-2 lg:order-1">
            <div className="space-y-4">
              <h3 className="text-2xl font-semibold tracking-tight text-balance sm:text-3xl">
                Operacao da carteira com mais previsibilidade
              </h3>
              <p className="text-muted-foreground text-base text-pretty">
                A carteira foi dividida para facilitar a leitura do que esta disponivel para resgate,
                do que ainda aguarda vencimento e do que precisa de atencao por notificacao ou calendario.
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

            <div className="flex flex-col sm:flex-row gap-4 pe-4 pt-2">
              <Button size="lg" className="cursor-pointer" asChild>
                <a href="#pricing" className='flex items-center'>
                  Ver planos
                  <ArrowRight className="ms-2 size-4" aria-hidden="true" />
                </a>
              </Button>
              <Button size="lg" variant="outline" className="cursor-pointer" asChild>
                <a href="#contact">
                  Entrar em contato
                </a>
              </Button>
            </div>
          </div>

          <ScreenshotPlaceholder
            title="Screenshot da carteira e agenda"
            subtitle="Espaco para tela de operacao"
            caption="Aqui cabem capturas de Meus Investimentos, Calendario ou Notificacoes, mantendo a landing pronta para apresentar o fluxo real do app."
            badges={['Carteira', 'Calendario', 'Notificacoes']}
            className="order-1 lg:order-2"
          />
        </div>
      </div>
    </section>
  )
}
