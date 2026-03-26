"use client"

import { Card, CardContent } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { ScreenshotPlaceholder } from '@/components/screenshot-placeholder'

const screenshots = [
  {
    id: 1,
    category: 'Dashboard',
    title: 'Tela inicial do painel',
    description:
      'Reserve este espaco para apresentar a visao consolidada do app logo no primeiro impacto.',
  },
  {
    id: 2,
    category: 'Carteira',
    title: 'Lista e cadastro de investimentos',
    description:
      'Ideal para mostrar a separacao entre itens disponiveis, bloqueados, cadastro e leitura por IA.',
  },
  {
    id: 3,
    category: 'Agenda',
    title: 'Calendario e notificacoes',
    description:
      'Um bom lugar para evidenciar vencimentos, alertas e o historico de acompanhamento da conta.',
  },
]

export function BlogSection() {
  return (
    <section id="screenshots" className="py-24 sm:py-32 bg-muted/50">
      <div className="container mx-auto px-4 sm:px-6 lg:px-8">
        <div className="mx-auto max-w-2xl text-center mb-16">
          <Badge variant="outline" className="mb-4">Capturas do app</Badge>
          <h2 className="text-3xl font-bold tracking-tight sm:text-4xl mb-4">
            Espacos prontos para screenshots reais
          </h2>
          <p className="text-lg text-muted-foreground">
            Mantive a estrutura visual da landing e transformei esta secao em um ponto dedicado para inserir as telas mais importantes do produto.
          </p>
        </div>

        <div className="grid grid-cols-1 gap-6 lg:grid-cols-3">
          {screenshots.map((shot) => (
            <Card key={shot.id} className="overflow-hidden py-0">
              <CardContent className="space-y-4 p-6">
                <div>
                  <p className="text-muted-foreground text-xs tracking-widest uppercase">
                    {shot.category}
                  </p>
                  <h3 className="mt-2 text-xl font-bold">{shot.title}</h3>
                  <p className="mt-2 text-muted-foreground">{shot.description}</p>
                </div>
                <ScreenshotPlaceholder
                  title={shot.title}
                  subtitle="Substitua por uma captura final"
                  caption={shot.description}
                  className="shadow-none"
                />
              </CardContent>
            </Card>
          ))}
        </div>
      </div>
    </section>
  )
}
