"use client"

import { Check } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'

const plans = [
  {
    name: 'Free',
    description: 'Para comecar a organizar a carteira e acompanhar vencimentos.',
    monthlyPrice: 0,
    features: [
      '2 leituras de comprovantes por mes',
      'Controle completo de investimentos',
      'Calendario de vencimentos na plataforma',
      'Notificacoes por Telegram e e-mail',
      'Suporte padrao',
    ],
    cta: 'Comecar',
    popular: false,
  },
  {
    name: 'Plus',
    description: 'Para quem quer ampliar notificacoes e integrar o calendario ao dia-a-dia.',
    monthlyPrice: 6.9,
    features: [
      '10 leituras de comprovantes por mes',
      'Calendario de vencimentos no Outlook ou app de calendario',
      'Notificacoes por Telegram, e-mail e WhatsApp',
      'Importacao e exportacao de dados em breve',
      'Suporte prioritario',
    ],
    cta: 'Escolher Plus',
    popular: true,
    includesPrevious: 'Tudo do Free, mais',
  },
  {
    name: 'Pro',
    description: 'Para uso mais frequente, com limites maiores e mais margem para evolucao.',
    monthlyPrice: 14.9,
    features: [
      '30 leituras de comprovantes por mes',
      'Calendario ICS e notificacoes completas',
      'Maior capacidade prevista para acoes brasileiras e cripto',
      'Importacao e exportacao de dados em breve',
      'Suporte prioritario',
    ],
    cta: 'Escolher Pro',
    popular: false,
    includesPrevious: 'Tudo do Plus, mais',
  },
]

export function PricingSection() {
  return (
    <section id="pricing" className="py-24 sm:py-32 bg-muted/40">
      <div className="container mx-auto px-4 sm:px-6 lg:px-8">
        <div className="mx-auto max-w-2xl text-center mb-12">
          <Badge variant="outline" className="mb-4">Planos</Badge>
          <h2 className="text-3xl font-bold tracking-tight sm:text-4xl mb-4">
            Escolha o plano que faz sentido para a sua rotina
          </h2>
          <p className="text-lg text-muted-foreground mb-8">
            Os valores e beneficios abaixo refletem a estrutura atual do produto, com foco em notificacoes, IA e calendario.
          </p>
        </div>

        <div className="mx-auto max-w-6xl">
          <div className="rounded-xl border">
            <div className="grid lg:grid-cols-3">
              {plans.map((plan, index) => (
                <div
                  key={index}
                  className={`p-8 grid grid-rows-subgrid row-span-4 gap-6 ${
                    plan.popular
                      ? 'my-2 mx-4 rounded-xl bg-card border-transparent shadow-xl ring-1 ring-foreground/10 backdrop-blur'
                      : ''
                  }`}
                >
                  <div>
                    <div className="text-lg font-medium tracking-tight mb-2">{plan.name}</div>
                    <div className="text-muted-foreground text-balance text-sm">{plan.description}</div>
                  </div>

                  <div>
                    <div className="text-4xl font-bold mb-1">
                      {plan.name === 'Free'
                        ? 'R$ 0'
                        : `R$ ${plan.monthlyPrice.toFixed(2).replace('.', ',')}`}
                    </div>
                    <div className="text-muted-foreground text-sm">
                      Por mes
                    </div>
                  </div>

                  <div>
                    <Button
                      className={`w-full cursor-pointer my-2 ${
                        plan.popular
                          ? 'shadow-md border-[0.5px] border-white/25 shadow-black/20 bg-primary ring-1 ring-primary/15 text-primary-foreground hover:bg-primary/90'
                          : 'shadow-sm shadow-black/15 border border-transparent bg-background ring-1 ring-foreground/10 hover:bg-muted/50'
                      }`}
                      variant={plan.popular ? 'default' : 'secondary'}
                      asChild
                    >
                      <a href="/app/signup">{plan.cta}</a>
                    </Button>
                  </div>

                  <div>
                    <ul role="list" className="space-y-3 text-sm">
                      {plan.includesPrevious && (
                        <li className="flex items-center gap-3 font-medium">
                          {plan.includesPrevious}:
                        </li>
                      )}
                      {plan.features.map((feature, featureIndex) => (
                        <li key={featureIndex} className="flex items-center gap-3">
                          <Check className="text-muted-foreground size-4 flex-shrink-0" strokeWidth={2.5} />
                          <span>{feature}</span>
                        </li>
                      ))}
                    </ul>
                  </div>
                </div>
              ))}
            </div>
          </div>
        </div>

        <div className="mt-16 text-center">
          <p className="text-muted-foreground">
            Ficou em duvida sobre o melhor plano?{' '}
            <Button variant="link" className="p-0 h-auto cursor-pointer" asChild>
              <a href="#contact">
                Fale conosco
              </a>
            </Button>
          </p>
        </div>
      </div>
    </section>
  )
}
