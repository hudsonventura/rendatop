"use client"

import {
  Shield,
  BarChart3,
  Database,
  Building2,
  Rocket,
  Settings,
  Zap,
  Package,
  Layout,
  Crown,
  Palette
} from 'lucide-react'

const menuSections = [
  {
    title: 'Controle Financeiro',
    items: [
      {
        title: 'Controle de juros',
        description: 'Saiba exatamente quanto você receberá de juros',
        icon: Package,
        href: '#free-blocks'
      },
      {
        title: 'Taxas e impostos',
        description: 'Controle as taxas e impostos para minimizar os custos',
        icon: Crown,
        href: '#premium-templates'
      }
    ]
  },
  {
    title: 'Controle de Tempo',
    items: [
      {
        title: 'Notificações',
        description: 'Seja notificado antes do vencimento do investimento',
        icon: Building2,
        href: '#ecommerce'
      },
      {
        title: 'Próximos vencimentos',
        description: 'Sabas quais serão os próximos investimentos a serem liquidados',
        icon: Rocket,
        href: '#saas-dashboards'
      }
    ]
  },
  {
    title: 'Controle Gráfico',
    items: [
      {
        title: 'Graficos e Indicadores',
        description: 'Acompanhe a evolução do seu patrimonio',
        icon: Database,
        href: '#docs'
      },
      {
        title: 'Relatórios',
        description: 'Liste e exporte para TXT, PDF ou Excel todos os seus investimento',
        icon: Palette,
        href: '#showcase'
      }
    ]
  }
]

export function MegaMenu() {
  return (
    <div className="w-[700px] max-w-[95vw] p-4 sm:p-6 lg:p-8 bg-background">
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6 sm:gap-8 lg:gap-12">
        {menuSections.map((section) => (
          <div key={section.title} className="space-y-4 lg:space-y-6">
            {/* Section Header */}
            <h3 className="text-sm font-medium text-muted-foreground uppercase tracking-wide">
              {section.title}
            </h3>

            {/* Section Links */}
            <div className="space-y-3 lg:space-y-4">
              {section.items.map((item) => (
                <a
                  key={item.title}
                  href={item.href}
                  className="group block space-y-1 lg:space-y-2 hover:bg-accent rounded-md p-2 lg:p-3 -mx-2 lg:-mx-3 transition-colors my-0"
                >
                  <div className="flex items-center gap-2 lg:gap-3">
                    <item.icon className="w-4 h-4 text-muted-foreground group-hover:text-primary transition-colors" />
                    <span className="text-sm font-medium text-foreground group-hover:text-primary transition-colors">
                      {item.title}
                    </span>
                  </div>
                  <p className="text-xs text-muted-foreground leading-relaxed ml-6 lg:ml-7">
                    {item.description}
                  </p>
                </a>
              ))}
            </div>
          </div>
        ))}
      </div>
    </div>
  )
}
