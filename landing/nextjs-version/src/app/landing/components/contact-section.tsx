"use client"

import { zodResolver } from "@hookform/resolvers/zod"
import { useForm } from "react-hook-form"
import { z } from "zod"
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Input } from '@/components/ui/input'
import { Textarea } from '@/components/ui/textarea'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from "@/components/ui/form"
import { Mail, MessageCircle, Github, BookOpen } from 'lucide-react'

const contactFormSchema = z.object({
  firstName: z.string().min(2, {
    message: "Informe pelo menos 2 caracteres.",
  }),
  lastName: z.string().min(2, {
    message: "Informe pelo menos 2 caracteres.",
  }),
  email: z.string().email({
    message: "Informe um e-mail valido.",
  }),
  subject: z.string().min(5, {
    message: "Informe um assunto com pelo menos 5 caracteres.",
  }),
  message: z.string().min(10, {
    message: "Escreva uma mensagem com pelo menos 10 caracteres.",
  }),
})

export function ContactSection() {
  const form = useForm<z.infer<typeof contactFormSchema>>({
    resolver: zodResolver(contactFormSchema),
    defaultValues: {
      firstName: "",
      lastName: "",
      email: "",
      subject: "",
      message: "",
    },
  })

  function onSubmit(values: z.infer<typeof contactFormSchema>) {
    console.log(values)
    form.reset()
  }



  return (
    <section id="contact" className="py-24 sm:py-32">
      <div className="container mx-auto px-4 sm:px-6 lg:px-8">
        <div className="mx-auto max-w-2xl text-center mb-16">
          <Badge variant="outline" className="mb-4">Contato</Badge>
          <h2 className="text-3xl font-bold tracking-tight sm:text-4xl mb-4">
            Quer conversar?
          </h2>
          <p className="text-lg text-muted-foreground">
            Dúvida, reclamação, sugestão ou elogio?
            Entre em contato com a gente! Envie um email ou uma mensagem pelo WhatApp . Estamos sempre abertos a ouvir nossos usuários e melhorar o Rendatop.
          </p>
          <div className="flex justify-center items-center gap-6 mt-6">
            <a href="mailto:contato@rendatop.com.br" target='_blank' className="flex items-center space-x-2 cursor-pointer" rel="noopener noreferrer">
              <Mail className="h-6 w-6" />
              <span className="text-sm font-medium">contato@rendatop.com.br</span>
            </a>
            <a href={`https://wa.me/${process.env.NEXT_PUBLIC_WHATSAPP_PHONE}`} target='_blank' className="flex items-center space-x-2 cursor-pointer" rel="noopener noreferrer">
              <MessageCircle className="h-6 w-6" />
              <span className="text-sm font-medium">WhatsApp</span>
            </a>

          </div>
        </div>



        
        </div>
  
    </section>  
  )
}
