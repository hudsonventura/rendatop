"use client"

import { CircleHelp } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Accordion, AccordionContent, AccordionItem, AccordionTrigger } from '@/components/ui/accordion'
import { Badge } from '@/components/ui/badge'

type FaqItem = {
  value: string
  question: string
  answer: string
}

const faqItems: FaqItem[] = [
  {
    value: 'item-1',
    question: 'Para quem o RendaTop faz mais sentido?',
    answer:
      'Para pessoas que querem acompanhar investimentos com mais organizacao, especialmente em renda fixa, sem depender apenas de planilhas e lembretes soltos.',
  },
  {
    value: 'item-2',
    question: 'Quais investimentos aparecem melhor no app hoje?',
    answer:
      'A estrutura atual evidencia bem investimentos de renda fixa com CDI, IPCA+ e percentual ao ano. Alguns recursos para acoes e cripto ja aparecem previstos nos planos como evolucao futura.',
  },
  {
    value: 'item-3',
    question: 'Como funcionam as notificacoes?',
    answer:
      'O produto possui centro de notificacoes no proprio app e pode usar e-mail e Telegram. O WhatsApp fica disponivel conforme o plano escolhido e a configuracao da conta.',
  },
  {
    value: 'item-4',
    question: 'Existe integracao com calendario?',
    answer:
      'Sim. Alem da tela de calendario dentro do app, os planos pagos habilitam calendario ICS para acompanhamento no Outlook ou em outros aplicativos de agenda.',
  },
  {
    value: 'item-5',
    question: 'O app tem leitura de comprovantes por IA?',
    answer:
      'Sim. O cadastro de investimentos pode importar comprovantes com apoio de IA, respeitando os limites mensais definidos em cada plano.',
  },
  {
    value: 'item-6',
    question: 'Posso reforcar a seguranca da conta?',
    answer:
      'Sim. A area de configuracoes inclui TOTP, testes dos canais de notificacao e gestao dos dados principais da conta para manter o acesso mais controlado.',
  },
]

const FaqSection = () => {
  return (
    <section id="faq" className="py-24 sm:py-32">
      <div className="container mx-auto px-4 sm:px-6 lg:px-8">
        <div className="mx-auto max-w-2xl text-center mb-16">
          <Badge variant="outline" className="mb-4">FAQ</Badge>
          <h2 className="text-3xl font-bold tracking-tight sm:text-4xl mb-4">
            Perguntas frequentes
          </h2>
          <p className="text-lg text-muted-foreground">
            Respostas objetivas sobre o funcionamento atual do produto, com base no que ja esta implementado no app.
          </p>
        </div>

        <div className="max-w-4xl mx-auto">
          <div className='bg-transparent'>
            <div className='p-0'>
              <Accordion type='single' collapsible className='space-y-5'>
                {faqItems.map(item => (
                  <AccordionItem key={item.value} value={item.value} className='rounded-md !border bg-transparent'>
                    <AccordionTrigger className='cursor-pointer items-center gap-4 rounded-none bg-transparent py-2 ps-3 pe-4 hover:no-underline data-[state=open]:border-b'>
                      <div className='flex items-center gap-4'>
                        <div className='bg-primary/10 text-primary flex size-9 shrink-0 items-center justify-center rounded-full'>
                          <CircleHelp className='size-5' />
                        </div>
                        <span className='text-start font-semibold'>{item.question}</span>
                      </div>
                    </AccordionTrigger>
                    <AccordionContent className='p-4 bg-transparent'>{item.answer}</AccordionContent>
                  </AccordionItem>
                ))}
              </Accordion>
            </div>
          </div>

          
        </div>
      </div>
    </section>
  )
}

export { FaqSection }
