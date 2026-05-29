"use client"

import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Card, CardContent } from '@/components/ui/card'
import { CardDecorator } from '@/components/ui/card-decorator'
import { LayoutDashboard, Wallet, BellRing, ShieldCheck } from 'lucide-react'

const values = [
  {
    icon: LayoutDashboard,
    title: 'Cofrinhos',
    description: 'Visualize os próximos vencimentos, distribuição por banco e evolução da carteira ao longo do tempo.',
  },
  {
    icon: Wallet,
    title: 'Carteira organizada',
    description: 'Cadastre investimentos, acompanhe o que já pode ser resgatado e mantenha historico com arquivamento.',
  },
  {
    icon: BellRing,
    title: 'Rotina acompanhada',
    description: 'Use calendario, centro de notificacoes e canais externos para nao perder datas de resgates.',
  },
  {
    icon: ShieldCheck,
    title: 'Gráficos',
    description: 'Veja a distribuição do seu patrimonio entre os bancos e acompanhe o crescimento da carteira.',
  },
]

export function AboutSection() {
  return (
    <section id="about" className="py-24 sm:py-32">
      <div className="container mx-auto px-4 sm:px-6 lg:px-8">
        <div className="mx-auto max-w-4xl text-center mb-16">
          <Badge variant="outline" className="mb-4">
            Por que o RendaTop?
          </Badge>
          <h2 className="text-3xl font-bold tracking-tight sm:text-4xl mb-6">
            Uma visão mais organizada para a sua rotina de investimentos
          </h2>
          <p className="text-lg text-muted-foreground mb-8">
            O app foi estruturado para concentrar o acompanhamento da carteira em poucas telas bem definidas,
            com linguagem direta e foco no que precisa ser acompanhado ao longo do mês.<br />
            Muitos investimentos espalhados entre vários banco? Aqui você pode acompanhar tudo em um so lugar.
          </p>
        </div>

        <div className="grid grid-cols-1 gap-x-8 gap-y-12 sm:grid-cols-2 xl:grid-cols-4 mb-12">
          {values.map((value, index) => (
            <Card key={index} className='group shadow-xs py-2'>
              <CardContent className='p-8'>
                <div className='flex flex-col items-center text-center'>
                  <CardDecorator>
                    <value.icon className='h-6 w-6' aria-hidden />
                  </CardDecorator>
                  <h3 className='mt-6 font-medium text-balance'>{value.title}</h3>
                  <p className='text-muted-foreground mt-3 text-sm'>{value.description}</p>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>

        
      </div>
    </section>
  )
}
