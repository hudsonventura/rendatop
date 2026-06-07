import type { Metadata } from "next"
import Link from "next/link"
import { ArrowRight, BriefcaseBusiness, ChartCandlestick, Handshake, ShieldCheck } from "lucide-react"
import { LandingNavbar } from "@/app/landing/components/navbar"
import { LandingFooter } from "@/app/landing/components/footer"
import { StructuredData } from "@/components/structured-data"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import { Card, CardContent } from "@/components/ui/card"
import { absoluteUrl, createPageMetadata } from "@/lib/seo"

const pillars = [
  {
    icon: ChartCandlestick,
    title: "Especialização em investimentos",
    description:
      "Desenhamos produtos digitais com foco na rotina de investidores, no acompanhamento da carteira e na leitura clara dos dados.",
    },
  {
    icon: BriefcaseBusiness,
    title: "Tecnologia com aplicação prática",
    description:
      "Transformamos regras de negócio, fluxos operacionais e análise financeira em experiências simples, confiáveis e escaláveis.",
  },
  {
    icon: ShieldCheck,
    title: "Clareza e confiança",
    description:
      "Priorizamos organização, transparência e previsibilidade para que cada funcionalidade gere valor real no dia a dia.",
  },
  {
    icon: Handshake,
    title: "Parceria de longo prazo",
    description:
      "Trabalhamos lado a lado com clientes e usuários para evoluir produtos continuamente, com atencao ao contexto e aos objetivos de cada operacao.",
  },
]

const aboutTitle = "Sobre | RendaTop"
const aboutDescription =
  "Conheca a RendaTop, software house especializada em solucoes digitais para investimentos, organização de carteira e inteligencia operacional."

export const metadata: Metadata = createPageMetadata({
  title: aboutTitle,
  description: aboutDescription,
  path: "/sobre",
  keywords: [
    "software house para investimentos",
    "organizacao da carteira",
    "tecnologia financeira",
  ],
})

export default function AboutPage() {
  const structuredData = [
    {
      "@context": "https://schema.org",
      "@type": "AboutPage",
      name: aboutTitle,
      description: aboutDescription,
      url: absoluteUrl("/sobre"),
      isPartOf: absoluteUrl("/"),
    },
    {
      "@context": "https://schema.org",
      "@type": "Organization",
      name: "RendaTop",
      url: absoluteUrl("/"),
      logo: absoluteUrl("/favicon.svg"),
      description: aboutDescription,
    },
    {
      "@context": "https://schema.org",
      "@type": "BreadcrumbList",
      itemListElement: [
        {
          "@type": "ListItem",
          position: 1,
          name: "Inicio",
          item: absoluteUrl("/"),
        },
        {
          "@type": "ListItem",
          position: 2,
          name: "Sobre",
          item: absoluteUrl("/sobre"),
        },
      ],
    },
  ]

  return (
    <div className="min-h-screen bg-background">
      <StructuredData data={structuredData} />
      <LandingNavbar />

      <main>
        <section className="border-b bg-gradient-to-b from-muted/60 via-background to-background">
          <div className="mx-auto flex max-w-6xl flex-col gap-10 px-4 py-16 sm:px-6 lg:px-8 lg:py-24">
            <div className="max-w-3xl">
              <Badge variant="outline" className="mb-4">
                Sobre a RendaTop
              </Badge>
              <h1 className="text-4xl font-bold tracking-tight sm:text-5xl">
                Uma software house focada em solucoes para investimentos
              </h1>
              <p className="mt-6 text-base leading-8 text-muted-foreground sm:text-lg">
                A RendaTop nasceu para desenvolver produtos digitais que tornem a jornada de investir
                mais organizada, inteligível e eficiente. Atuamos como uma software house especializada em
                solucoes para investimentos, unindo tecnologia, contexto de negócio e cuidado com a
                experiência do usuário.
              </p>
              <p className="mt-4 text-base leading-8 text-muted-foreground sm:text-lg">
                Nosso trabalho e transformar informações dispersas, rotinas operacionais e indicadores
                financeiros em ferramentas claras para acompanhamento de carteira, vencimentos, alertas,
                consolidação patrimonial e tomada de decisão. Buscamos construir software que ajude o
                usuário a enxergar melhor o proprio patrimônio e agir com mais confiança.
              </p>
              
            </div>
          </div>
        </section>

        <section className="mx-auto max-w-6xl px-4 py-16 sm:px-6 lg:px-8 lg:py-20">
          <div className="max-w-3xl">
            <h2 className="text-2xl font-semibold tracking-tight sm:text-3xl">
              O que construímos?
            </h2>
            <p className="mt-4 text-base leading-8 text-muted-foreground">
              Desenvolvemos plataformas e experiências digitais voltadas para o universo de investimentos,
              com foco em organização de dados, visão consolidada da carteira, acompanhamento de eventos
              importantes e apoio a operações que exigem disciplina e precisão.
            </p>
            <p className="mt-4 text-base leading-8 text-muted-foreground">
              Mais do que publicar funcionalidades, buscamos resolver problemas concretos com interfaces
              objetivas, regras bem definidas e evolução constante. Para nos, boa tecnologia e aquela que
              simplifica a leitura do cenario e apoia decisoes melhores.
            </p>
          </div>

          <div className="mt-10 grid gap-6 md:grid-cols-2 xl:grid-cols-4">
            {pillars.map((pillar) => (
              <Card key={pillar.title} className="border-border/80 shadow-sm">
                <CardContent className="p-6">
                  <div className="mb-4 flex h-11 w-11 items-center justify-center rounded-2xl bg-primary/10">
                    <pillar.icon className="h-5 w-5" />
                  </div>
                  <h3 className="text-lg font-semibold">{pillar.title}</h3>
                  <p className="mt-3 text-sm leading-7 text-muted-foreground">
                    {pillar.description}
                  </p>
                </CardContent>
              </Card>
            ))}
          </div>
        </section>

        <section className="border-t bg-muted/40">
          <div className="mx-auto grid max-w-6xl gap-8 px-4 py-16 sm:px-6 lg:grid-cols-[1.2fr_0.8fr] lg:px-8 lg:py-20">
            <div>
              <h2 className="text-2xl font-semibold tracking-tight sm:text-3xl">
                Como pensamos produto
              </h2>
              <p className="mt-4 text-base leading-8 text-muted-foreground">
                Acreditamos que soluções para investimentos precisam equilibrar profundidade tecnica com
                usabilidade. Por isso, tratamos design, engenharia e regra de negócio como partes do
                mesmo problema. Cada entrega deve ajudar o usuário a entender melhor seus numeros, sua
                estrategia e seus proximos movimentos.
              </p>
              <p className="mt-4 text-base leading-8 text-muted-foreground">
                Seguimos uma abordagem pragmatica: identificar o que realmente importa, reduzir atrito
                operacional e construir produtos que crescam com consistencia. Esse e o tipo de software
                que gostamos de criar e evoluir.
              </p>
            </div>

            <Card className="border-border/80 shadow-sm">
              <CardContent className="flex h-full flex-col justify-center p-6">
                <p className="text-sm font-semibold uppercase tracking-[0.2em] text-muted-foreground">
                  Nosso foco
                </p>
                <ul className="mt-5 space-y-4 text-sm leading-7 text-foreground/85">
                  <li>Organização e consolidação de carteiras de investimento.</li>
                  <li>Leitura clara de vencimentos, alertas e eventos relevantes.</li>
                  <li>Experiências digitais simples para problemas financeiros complexos.</li>
                  <li>Evolução contínua com base em uso real e contexto de negócio.</li>
                </ul>
              </CardContent>
            </Card>
          </div>
        </section>

        <section className="mx-auto max-w-6xl px-4 py-16 sm:px-6 lg:px-8 lg:py-20">
          <div className="max-w-3xl">
            <Badge variant="outline" className="mb-4">
              Missão, visão e valores
            </Badge>
            <h2 className="text-2xl font-semibold tracking-tight sm:text-3xl">
              Os princípios que orientam a RendaTop
            </h2>
            <p className="mt-4 text-base leading-8 text-muted-foreground">
              Nossa atuação é guiada por uma direção clara de produto e por valores que ajudam a manter
              consistência nas decisões, na forma como construímos tecnologia e na maneira como nos
              relacionamos com usuários, parceiros e clientes.
            </p>
          </div>

          <div className="mt-10 grid gap-6 lg:grid-cols-2">
            <Card className="border-border/80 shadow-sm">
              <CardContent className="p-6">
                <h3 className="text-xl font-semibold">Missão</h3>
                <p className="mt-4 text-sm leading-7 text-muted-foreground">
                  Desenvolver soluções digitais que ajudem investidores a organizar informações,
                  acompanhar seus ativos com clareza e tomar decisões com mais confiança.
                </p>
              </CardContent>
            </Card>

            <Card className="border-border/80 shadow-sm">
              <CardContent className="p-6">
                <h3 className="text-xl font-semibold">Visão</h3>
                <p className="mt-4 text-sm leading-7 text-muted-foreground">
                  Ser reconhecida como uma referência em tecnologia para gestão de investimentos,
                  criando produtos simples, confiáveis e úteis para quem busca acompanhar seu
                  patrimônio de forma organizada e consciente.
                </p>
              </CardContent>
            </Card>
          </div>

          <div className="mt-6">
            <Card className="border-border/80 shadow-sm">
              <CardContent className="p-6 sm:p-8">
                <h3 className="text-xl font-semibold">Valores</h3>

                <div className="mt-6 grid gap-6 md:grid-cols-2 xl:grid-cols-3">
                  <div>
                    <h4 className="text-base font-semibold">Clareza acima da complexidade</h4>
                    <p className="mt-2 text-sm leading-7 text-muted-foreground">
                      Acreditamos que informações financeiras devem ser apresentadas de forma
                      compreensível e objetiva.
                    </p>
                  </div>

                  <div>
                    <h4 className="text-base font-semibold">Utilidade prática</h4>
                    <p className="mt-2 text-sm leading-7 text-muted-foreground">
                      Cada funcionalidade deve resolver um problema real e gerar valor no dia a dia
                      do usuário.
                    </p>
                  </div>

                  <div>
                    <h4 className="text-base font-semibold">Transparência</h4>
                    <p className="mt-2 text-sm leading-7 text-muted-foreground">
                      Buscamos relações honestas com usuários, parceiros e clientes, sem promessas
                      irreais ou marketing exagerado.
                    </p>
                  </div>

                  <div>
                    <h4 className="text-base font-semibold">Evolução contínua</h4>
                    <p className="mt-2 text-sm leading-7 text-muted-foreground">
                      Produtos digitais nunca estão prontos. Aprendemos constantemente com o uso
                      real para aprimorar nossas soluções.
                    </p>
                  </div>

                  <div>
                    <h4 className="text-base font-semibold">Confiabilidade</h4>
                    <p className="mt-2 text-sm leading-7 text-muted-foreground">
                      Tratamos dados, processos e operações com responsabilidade, priorizando
                      estabilidade e previsibilidade.
                    </p>
                  </div>

                  <div>
                    <h4 className="text-base font-semibold">Simplicidade inteligente</h4>
                    <p className="mt-2 text-sm leading-7 text-muted-foreground">
                      Valorizamos soluções simples para problemas complexos, reduzindo atritos e
                      aumentando a produtividade.
                    </p>
                  </div>

                  <div className="md:col-span-2 xl:col-span-3">
                    <h4 className="text-base font-semibold">Foco no longo prazo</h4>
                    <p className="mt-2 max-w-3xl text-sm leading-7 text-muted-foreground">
                      Tomamos decisões pensando na sustentabilidade do produto, na confiança dos
                      usuários e na construção de relacionamentos duradouros.
                    </p>
                  </div>
                </div>
              </CardContent>
            </Card>
          </div>
        </section>
      </main>

      <LandingFooter />
    </div>
  )
}
