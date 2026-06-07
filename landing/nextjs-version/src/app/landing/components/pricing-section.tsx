"use client"

import { useEffect, useState } from 'react'
import { Check } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
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

const planDescriptions: Record<string, { description: string; cta: string; popular?: boolean; includesPrevious?: string }> = {
  free: {
    description: 'Para comecar a organizar a carteira e acompanhar vencimentos.',
    cta: 'Comecar',
    popular: false,
  },
  plus: {
    description: 'Para quem quer ampliar notificacoes e integrar o calendario ao dia-a-dia.',
    cta: 'Escolher Plus',
    popular: true,
    includesPrevious: 'Tudo do Free, mais',
  },
  pro: {
    description: 'Para uso mais frequente, com limites maiores e mais margem para evolucao.',
    cta: 'Escolher Pro',
    popular: false,
    includesPrevious: 'Tudo do Plus, mais',
  },
}

export function PricingSection() {
  const [plans, setPlans] = useState<Plan[]>([])
  const [loading, setLoading] = useState(true)

  const clientBaseUrl = API_CONFIG.BASE_URL;

  useEffect(() => {
    const baseUrl = API_CONFIG.BASE_URL;

    

    const fetchPlans = async () => {
      try {
        const response = await fetch(`${baseUrl}/public/subscription/plans`)
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
          <p className="text-lg text-muted-foreground mb-8">
            Os valores e beneficios abaixo refletem a estrutura atual do produto, com foco em notificacoes, calendario e leitura automática de comprovantes.
          </p>
        </div>

        {loading ? (
          <div className="mx-auto max-w-6xl text-center py-12">
            <p className="text-muted-foreground">Carregando planos...</p>
          </div>
        ) : plans.length === 0 ? (
          <div className="mx-auto max-w-6xl text-center py-12">
            <p className="text-muted-foreground">Planos não disponíveis no momento</p>
          </div>
        ) : (
          <div className="mx-auto max-w-6xl">
            <div className="rounded-xl border">
              <div className="grid lg:grid-cols-3">
                {plans.map((plan, index) => {
                  const metadata = planDescriptions[plan.id.toLowerCase()] || { description: '', cta: 'Escolher' }
                  const isPopular = metadata.popular || false

                  return (
                    <div
                      key={plan.id}
                      className={`p-8 grid grid-rows-subgrid row-span-4 gap-6 ${
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
                        <Button
                          className={`w-full cursor-pointer my-2 ${
                            isPopular
                              ? 'shadow-md border-[0.5px] border-white/25 shadow-black/20 bg-primary ring-1 ring-primary/15 text-primary-foreground hover:bg-primary/90'
                              : 'shadow-sm shadow-black/15 border border-transparent bg-background ring-1 ring-foreground/10 hover:bg-muted/50'
                          }`}
                          variant={isPopular ? 'default' : 'secondary'}
                          asChild
                        >
                          <a href={clientBaseUrl+'/login'}>{metadata.cta}</a>
                        </Button>
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
