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
      'Você tem muitos investimentos e já perdeu o controle dos valores e vencimentos? O RendaTop foi criado para pessoas que querem acompanhar investimentos com mais organização, especialmente em renda fixa, sem depender de planilhas, lembretes soltos e anotações. O app te avisa quando há um investimento próximo do vencimento para você antecipar a tomada de decisão.',
  },
  {
    value: 'item-2',
    question: 'Quais investimentos consigo controlar no app hoje?',
    answer:
      'A estrutura atual evidencia bem investimentos de renda fixa com CDI, IPCA+ e %a.a. Recursos para acompanhar acões e cripto já estão em desenvolvimento e estarão disponíveis em breve.',
  },
  {
    value: 'item-3',
    question: 'Como funcionam as notificações?',
    answer:
      'O RendaTop possui centro de notificações no próprio app, e-mail, Telegram e WhatsApp. O app te nofifica sempre que um investimento estiver próximo do vencimento, para que você possa se organizar e tomar decisões com antecedência. As notificações são configuráveis, e os planos pagos permitem personalizar os prazos de aviso e os canais de notificação.',
  },
  {
    value: 'item-4',
    question: 'Existe integração com calendário?',
    answer:
      'Sim. Alem da tela de calendário dentro do app, os planos pagos habilitam calendário ICS para acompanhamento no Outlook ou em outros aplicativos de agenda.',
  },
  {
    value: 'item-5',
    question: 'O app tem leitura inteligente de comprovantes?',
    answer:
      'Sim. O cadastro de investimentos pode importar comprovantes com apoio de IA. Pode-se usar imagens ou PDFs, e o app extrai as informações relevantes para preencher os campos de cadastro. Pelo app no seu celular é possível compartilhar o comprovante diretamente para o RendaTop, e no desktop é possível fazer upload do arquivo.',
  },
  {
    value: 'item-6',
    question: 'Como funciona a privacidade dos meus dados?',
    answer:
      'Nós guardamos apenas os dados que são necessários para o funcionamento do app, como informações de investimentos e preferências de notificações. Algumas informações pessoais são necessárias como nome e e-mail. Não compartilhamos seus dados com terceiros. O CPF é usado apenas no processo de pagamento dos planos pagos e não é obrigatório para o uso do app no plano gratuito. O app pode ser usado apenas com um e-mail para cadastro e acesso às funcionalidades do plano gratuito.',
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
            Veja aqui algumas infomações importantes sobre o app, seus recursos e planos. Se tiver outras dúvidas ou quiser sugerir melhorias, entre em contato conosco!
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
