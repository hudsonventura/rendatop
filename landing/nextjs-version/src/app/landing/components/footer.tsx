"use client"

import { zodResolver } from "@hookform/resolvers/zod"
import { useForm } from "react-hook-form"
import { z } from "zod"
import { Separator } from '@/components/ui/separator'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormMessage,
} from "@/components/ui/form"
import { Logo } from '@/components/logo'
import { Globe, Heart, LogIn, Wallet } from 'lucide-react'

const newsletterSchema = z.object({
  email: z.string().email({
    message: "Informe um e-mail valido.",
  }),
})

const footerLinks = {
  product: [
    { name: 'Recursos', href: '#features' },
    { name: 'Planos', href: '#pricing' },
    { name: 'Criar conta', href: '/app/signup' },
  ],
  company: [
    { name: 'Sobre', href: '#about' },
    { name: 'FAQ', href: '#faq' },
    { name: 'Contato', href: '#contact' },
    { name: 'Entrar', href: '/app/login' },
  ],
  resources: [
    { name: 'Dashboard', href: '#features' },
    { name: 'Calendario', href: '#features' },
    { name: 'Notificacoes', href: '#features' },
    { name: 'Assinatura', href: '#pricing' },
  ],
  legal: [
    { name: 'Privacidade', href: '#privacy' },
    { name: 'Termos', href: '#terms' },
    { name: 'Seguranca', href: '#faq' },
    { name: 'Status', href: '#contact' },
  ],
}

const socialLinks = [
  { name: 'Site', href: 'https://rendatop.com.br', icon: Globe },
  { name: 'Entrar', href: '/app/login', icon: LogIn },
  { name: 'Planos', href: '#pricing', icon: Wallet },
]

export function LandingFooter() {
  const form = useForm<z.infer<typeof newsletterSchema>>({
    resolver: zodResolver(newsletterSchema),
    defaultValues: {
      email: "",
    },
  })

  function onSubmit(values: z.infer<typeof newsletterSchema>) {
    console.log(values)
    form.reset()
  }

  return (
    <footer className="border-t bg-background">
      <div className="container mx-auto px-4 sm:px-6 lg:px-8 py-16">
        <div className="mb-16">
          
        </div>

        <div className="grid gap-8 grid-cols-4 lg:grid-cols-6">
          <div className="col-span-4 lg:col-span-2 max-w-2xl">
            <div className="flex items-center space-x-2 mb-4 max-lg:justify-center">
              <a href="https://RendaTop.com.br" target='_blank' className="flex items-center space-x-2 cursor-pointer" rel="noopener noreferrer">
                <Logo size={32} />
                <span className="font-bold text-xl">RendaTop</span>
                <p>Gestão de Investimentos</p>
              </a>
            </div>
            <p className="text-muted-foreground mb-6 max-lg:text-center max-lg:flex max-lg:justify-center">
              Acompanhe e consolide seus investimentos em um unico lugar.
            </p>
            <div className="flex space-x-4 max-lg:justify-center">
              {socialLinks.map((social) => (
                <Button key={social.name} variant="ghost" size="icon" asChild>
                  <a
                    href={social.href}
                    aria-label={social.name}
                    target="_blank"
                    rel="noopener noreferrer"
                  >
                    <social.icon className="h-4 w-4" />
                  </a>
                </Button>
              ))}
            </div>
          </div>

          <div className='max-md:col-span-2 lg:col-span-1'>
            <h4 className="font-semibold mb-4">Produto</h4>
            <ul className="space-y-3">
              {footerLinks.product.map((link) => (
                <li key={link.name}>
                  <a
                    href={link.href}
                    className="text-muted-foreground hover:text-foreground transition-colors"
                  >
                    {link.name}
                  </a>
                </li>
              ))}
            </ul>
          </div>

          <div className='max-md:col-span-2 lg:col-span-1'>
            <h4 className="font-semibold mb-4">Empresa</h4>
            <ul className="space-y-3">
              {footerLinks.company.map((link) => (
                <li key={link.name}>
                  <a
                    href={link.href}
                    className="text-muted-foreground hover:text-foreground transition-colors"
                  >
                    {link.name}
                  </a>
                </li>
              ))}
            </ul>
          </div>

          <div className='max-md:col-span-2 lg:col-span-1'>
            <h4 className="font-semibold mb-4">Recursos</h4>
            <ul className="space-y-3">
              {footerLinks.resources.map((link) => (
                <li key={link.name}>
                  <a
                    href={link.href}
                    className="text-muted-foreground hover:text-foreground transition-colors"
                  >
                    {link.name}
                  </a>
                </li>
              ))}
            </ul>
          </div>

          <div className='max-md:col-span-2 lg:col-span-1'>
            <h4 className="font-semibold mb-4">Legal</h4>
            <ul className="space-y-3">
              {footerLinks.legal.map((link) => (
                <li key={link.name}>
                  <a
                    href={link.href}
                    className="text-muted-foreground hover:text-foreground transition-colors"
                  >
                    {link.name}
                  </a>
                </li>
              ))}
            </ul>
          </div>
        </div>

        <Separator className="my-8" />

        <div className="flex flex-col lg:flex-row justify-between items-center gap-2">
          <div className="flex flex-col sm:flex-row items-center gap-2 text-muted-foreground text-sm">
            <div className="flex items-center gap-1">
              <span>Made with</span>
              <Heart className="h-4 w-4 text-red-500 fill-current" />
              <span>by</span>
              <a href="https://RendaTop.com.br" target='_blank' className="font-semibold text-foreground hover:text-primary transition-colors cursor-pointer" rel="noopener noreferrer">
                RendaTop
              </a>
              <p>{new Date().getFullYear()}</p>
            </div>
            <span className="hidden sm:inline">•</span>
            <span>®  Todos os direitos reservados</span>
          </div>
          <div className="flex items-center space-x-4 text-sm text-muted-foreground mt-4 md:mt-0">
            <a href="#privacy" className="hover:text-foreground transition-colors">
              Politica de Privacidade
            </a>
            <a href="#terms" className="hover:text-foreground transition-colors">
              Termos de Uso
            </a>
            <a href="#cookies" className="hover:text-foreground transition-colors">
              Politica de Cookies
            </a>
          </div>
        </div>
      </div>
    </footer>
  )
}
