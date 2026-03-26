"use client"

import {
  Building2,
  Rocket,
  Settings,
  Bell,
  Wallet,
  CalendarDays
} from 'lucide-react'

const menuSections = [
  {
    title: 'Carteira',
    items: [
      {
        title: 'Dashboard',
        description: 'Visualize vencimentos, distribuicao por banco e evolucao da carteira.',
        icon: Rocket,
        href: '#features'
      },
      {
        title: 'Meus investimentos',
        description: 'Cadastre, acompanhe, resgate e arquive investimentos com mais contexto.',
        icon: Wallet,
        href: '#features'
      }
    ]
  },
  {
    title: 'Rotina',
    items: [
      {
        title: 'Calendario',
        description: 'Acompanhe datas de aplicacao e vencimento em uma visao mensal clara.',
        icon: CalendarDays,
        href: '#features'
      },
      {
        title: 'Notificacoes',
        description: 'Receba alertas no app e acompanhe o historico de leitura.',
        icon: Bell,
        href: '#features'
      }
    ]
  },
  {
    title: 'Conta',
    items: [
      {
        title: 'Configuracoes',
        description: 'Ajuste email, telefone, canais de alerta, calendario e TOTP.',
        icon: Settings,
        href: '#features'
      },
      {
        title: 'Assinatura',
        description: 'Escolha entre os planos Free, Plus e Pro conforme sua rotina.',
        icon: Building2,
        href: '#pricing'
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
