"use client"

import { Card } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'

const productHighlights = [
    'Dashboard',
    'Carteira',
    'Calendario',
    'Notificacoes',
    'Configuracoes',
    'Assinatura',
    'Leitura por IA',
    'Seguranca com TOTP',
] as const

export function LogoCarousel() {
    return (
        <section className="pb-12 sm:pb-16 lg:pb-20 pt-12">
            <div className="container mx-auto px-4 sm:px-6 lg:px-8">
                <div className="text-center">
                    <Badge variant="outline" className="mb-4">
                        Estrutura do produto
                    </Badge>
                    <p className="text-sm font-medium text-muted-foreground mb-8">
                        Recursos que compoem a experiencia principal do RendaTop
                    </p>

                    <div className="relative">
                        <div className="absolute left-0 top-0 bottom-0 w-20 bg-gradient-to-r from-background to-transparent z-10 pointer-events-none" />
                        <div className="absolute right-0 top-0 bottom-0 w-20 bg-gradient-to-l from-background to-transparent z-10 pointer-events-none" />
                        <div className="overflow-hidden">
                            <div className="flex animate-logo-scroll space-x-8 sm:space-x-12">
                                {productHighlights.map((item, index) => (
                                    <Card
                                        key={`first-${index}`}
                                        className="flex-shrink-0 flex items-center justify-center h-16 w-44 opacity-60 hover:opacity-100 transition-opacity duration-300 border-0 shadow-none bg-transparent"
                                    >
                                        <div className="flex items-center gap-3 rounded-full border bg-background/80 px-4 py-3">
                                            <span className="text-foreground text-base font-semibold whitespace-nowrap">
                                                {item}
                                            </span>
                                        </div>
                                    </Card>
                                ))}
                                {productHighlights.map((item, index) => (
                                    <Card
                                        key={`second-${index}`}
                                        className="flex-shrink-0 flex items-center justify-center h-16 w-44 opacity-60 hover:opacity-100 transition-opacity duration-300 border-0 shadow-none bg-transparent"
                                    >
                                        <div className="flex items-center gap-3 rounded-full border bg-background/80 px-4 py-3">
                                            <span className="text-foreground text-base font-semibold whitespace-nowrap">
                                                {item}
                                            </span>
                                        </div>
                                    </Card>
                                ))}
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </section>
    )
}
