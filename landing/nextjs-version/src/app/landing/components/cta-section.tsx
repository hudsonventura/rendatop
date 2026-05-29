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
                  Organize sua carteira com mais contexto e monitore cada vencimento
                </h1>

                <p className='text-muted-foreground mx-auto max-w-2xl text-balance lg:text-xl'>
                  A landing agora conversa com o produto real e ja deixa espacos definidos para screenshots do dashboard,
                  da carteira e da agenda do app.
                </p>
              </div>

              

              
            </div>
          </div>
        </div>
      </div>
    </section>
  )
}
