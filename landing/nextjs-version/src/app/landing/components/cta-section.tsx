"use client"

import { ArrowRight, TrendingUp, Wallet, Bell } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Separator } from '@/components/ui/separator'

export function CTASection() {
  return (
    <section className='py-16 lg:py-24 bg-muted/80'>
      <div className='container mx-auto px-4 lg:px-8'>
        <div className='mx-auto max-w-4xl'>
          <div className='text-center'>
            <div className='space-y-8'>
              <div className='flex flex-col items-center gap-4'>
                <Badge variant='outline' className='flex items-center gap-2'>
                  <TrendingUp className='size-3' />
                  RendaTop
                </Badge>

                <div className='text-muted-foreground flex items-center gap-4 text-sm'>
                  <span className='flex items-center gap-1'>
                    <div className='size-2 rounded-full bg-green-500' />
                    Dashboard
                  </span>
                  <Separator orientation='vertical' className='!h-4' />
                  <span>Calendario</span>
                  <Separator orientation='vertical' className='!h-4' />
                  <span>Notificacoes</span>
                </div>
              </div>

              <div className='space-y-6'>
                <h1 className='text-4xl font-bold tracking-tight text-balance sm:text-5xl lg:text-6xl'>
                  Organize sua carteira
                  <span className='flex sm:inline-flex justify-center'>
                    <span className='relative mx-2'>
                      <span className='bg-gradient-to-r from-primary to-secondary bg-clip-text text-transparent'>
                        com mais contexto
                      </span>
                      <div className='absolute start-0 -bottom-2 h-1 w-full bg-gradient-to-r from-primary/30 to-secondary/30' />
                    </span>
                    a cada vencimento
                  </span>
                </h1>

                <p className='text-muted-foreground mx-auto max-w-2xl text-balance lg:text-xl'>
                  A landing agora conversa com o produto real e ja deixa espacos definidos para screenshots do dashboard,
                  da carteira e da agenda do app.
                </p>
              </div>

              <div className='flex flex-col justify-center gap-4 sm:flex-row sm:gap-6'>
                <Button size='lg' className='cursor-pointer px-8 py-6 text-lg font-medium' asChild>
                  <a href='/app/signup'>
                    <Wallet className='me-2 size-5' />
                    Criar conta
                  </a>
                </Button>
                <Button variant='outline' size='lg' className='cursor-pointer px-8 py-6 text-lg font-medium group' asChild>
                  <a href='#screenshots'>
                    <Bell className='me-2 size-5' />
                    Ver capturas
                    <ArrowRight className='ms-2 size-4 transition-transform group-hover:translate-x-1' />
                  </a>
                </Button>
              </div>

              <div className='text-muted-foreground flex flex-wrap items-center justify-center gap-6 text-sm'>
                <div className='flex items-center gap-2'>
                  <div className='size-2 rounded-full bg-green-600 dark:bg-green-400 me-1' />
                  <span>Planos Free, Plus e Pro</span>
                </div>
                <div className='flex items-center gap-2'>
                  <div className='size-2 rounded-full bg-blue-600 dark:bg-blue-400 me-1' />
                  <span>Notificacoes por multiplos canais</span>
                </div>
                <div className='flex items-center gap-2'>
                  <div className='size-2 rounded-full bg-purple-600 dark:bg-purple-400 me-1' />
                  <span>Leitura de comprovantes por IA</span>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>
  )
}
