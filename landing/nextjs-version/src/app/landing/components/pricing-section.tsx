"use client"

import { useEffect, useState } from 'react'
import type { CSSProperties } from 'react'
import { Check, Sparkles } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { APP_CONFIG } from '@/config/app'
import { API_CONFIG } from '@/config/api'

interface Plan {
  id: string
  name: string
  description: string
  price: number
  ai_monthly_limit: number
  calendar_ics: boolean
  stoks: number
  whatsapp_notifications: boolean
  recurring_investments: boolean
  money_boxes_limit: number
  features: Record<string, string>
}

type PlanGridStyle = CSSProperties & {
  '--plan-count': number
}

const planDescriptions: Record<string, { description: string; popular?: boolean; includesPrevious?: string }> = {
  free: {
    description: 'Para comecar a organizar a carteira e acompanhar vencimentos.',
    popular: false,
  },
  plus: {
    description: 'Para quem quer ampliar notificacoes e integrar o calendario ao dia-a-dia.',
    popular: true,
    includesPrevious: 'Tudo do Free, mais',
  },
  pro: {
    description: 'Para uso mais frequente, com limites maiores e mais margem para evolucao.',
    popular: false,
    includesPrevious: 'Tudo do Plus, mais',
  },
}

export function PricingSection() {
  const [plans, setPlans] = useState<Plan[]>([])
  const [loading, setLoading] = useState(true)

  const clientBaseUrl = APP_CONFIG.BASE_URL;
  const serverBaseUrl = API_CONFIG.BASE_URL;

  useEffect(() => {


    const fetchPlans = async () => {
      try {
        const response = await fetch(`${serverBaseUrl}/public/subscription/plans`)
        if (!response.ok) throw new Error('Falha ao carregar planos')
        const data = await response.json()
        setPlans(data)
      } catch (error) {
        console.error('Erro ao buscar planos:', error)
        // Mantém array vazio em caso de erro
      } finally {
        setLoading(false)
      }
    }

    fetchPlans()
  }, [])
  return (
    <section id="pricing" className="py-24 sm:py-32 bg-muted/40">
      <div className="container mx-auto px-4 sm:px-6 lg:px-8">
        <div className="mx-auto max-w-2xl text-center mb-12">
          <Badge variant="outline" className="mb-4">Planos</Badge>
          <h2 className="text-3xl font-bold tracking-tight sm:text-4xl mb-4">
            Escolha o plano que faz sentido para a sua rotina
          </h2>

          <div className="mb-6 rounded-xl border border-primary/20 bg-background p-5 shadow-sm">
            <div className="flex items-start gap-3 text-left">
              <div className="flex size-10 shrink-0 items-center justify-center rounded-full bg-primary/10 text-primary">
                <Sparkles className="size-5" />
              </div>
              <div>
                <p className="font-semibold">Comece com 30 dias gratuitos do plano Pro</p>
                <p className="mt-1 text-sm leading-relaxed text-muted-foreground">
                  Ao criar sua conta, você recebe o plano Pro gratuitamente por 30 dias para desfrutar de todos os recursos do sistema. Não é necessaŕio informar um numero de cartão de credito. Teste o sistema para saber se ele atende às suas espectativas antes de decidir sobre a assinatura. Aproveite esta oportunidade para explorar todas as funcionalidades e descobrir como o sistema pode facilitar sua rotina financeira.
                </p>
              </div>
            </div>
          </div>

          <Button size="lg" className="cursor-pointer" asChild>
            <a href={clientBaseUrl+'/signup'}>Crie sua conta gratuita</a>
          </Button>
        </div>

        {loading ? (
          <div className="w-full text-center py-12">
            <p className="text-muted-foreground">Carregando planos...</p>
          </div>
        ) : plans.length === 0 ? (
          <div className="w-full text-center py-12">
            <p className="text-muted-foreground">Planos não disponíveis no momento</p>
          </div>
        ) : (
          <div className="w-full">
            <div className="rounded-xl border">
              <div
                className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-[repeat(var(--plan-count),minmax(0,1fr))]"
                style={{ '--plan-count': plans.length } as PlanGridStyle}
              >
                {plans.map((plan) => {
                  const metadata = planDescriptions[plan.id.toLowerCase()] || { description: '' }
                  const isPopular = metadata.popular || false

                  return (
                    <div
                      key={plan.id}
                      className={`p-8 grid grid-rows-subgrid row-span-3 gap-6 ${
                        isPopular
                          ? 'my-2 mx-4 rounded-xl bg-card border-transparent shadow-xl ring-1 ring-foreground/10 backdrop-blur'
                          : ''
                      }`}
                    >
                      <div>
                        <div className="text-lg font-medium tracking-tight mb-2">{plan.name}</div>
                        <div className="text-muted-foreground text-balance text-sm">{metadata.description}</div>
                      </div>

                      <div>
                        <div className="text-4xl font-bold mb-1">
                          {plan.price === 0
                            ? 'R$ 0'
                            : `R$ ${plan.price.toFixed(2).replace('.', ',')}`}
                        </div>
                        <div className="text-muted-foreground text-sm">
                          Por mes
                        </div>
                      </div>

                      <div>
                        <ul role="list" className="space-y-3 text-sm">
                          {metadata.includesPrevious && (
                            <li className="flex items-center gap-3 font-medium">
                              {metadata.includesPrevious}:
                            </li>
                          )}
                          {Object.entries(plan.features).map(([key, value]) => (
                            <li key={key} className="flex items-center gap-3">
                              <Check className="text-muted-foreground size-4 flex-shrink-0" strokeWidth={2.5} />
                              <span>{value}</span>
                            </li>
                          ))}
                        </ul>
                      </div>
                    </div>
                  )
                })}
              </div>
            </div>
          </div>
        )}

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
