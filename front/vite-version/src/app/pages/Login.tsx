"use client"

import { useState } from "react"
import { zodResolver } from "@hookform/resolvers/zod"
import { useForm } from "react-hook-form"
import { z } from "zod"
import { useNavigate } from "react-router-dom"
import { cn } from "@/lib/utils"
import { httpClient, HttpError } from "@/lib/http-client"
import { Button } from "@/components/ui/button"
import {
    Card,
    CardContent,
    CardDescription,
    CardHeader,
    CardTitle,
} from "@/components/ui/card"
import { Input } from "@/components/ui/input"
import {
    Form,
    FormControl,
    FormField,
    FormItem,
    FormLabel,
    FormMessage,
} from "@/components/ui/form"
import { Logo } from "@/components/logo"
import { Loader2 } from "lucide-react"

const loginFormSchema = z.object({
    email: z.string().email("E-mail inválido"),
    password: z.string().min(1, "A senha é obrigatória"),
})

type LoginFormValues = z.infer<typeof loginFormSchema>

export default function LoginPage() {
    const navigate = useNavigate()
    const [isLoading, setIsLoading] = useState(false)
    const [errorMessage, setErrorMessage] = useState<string | null>(null)

    const form = useForm<LoginFormValues>({
        resolver: zodResolver(loginFormSchema),
        defaultValues: {
            email: "",
            password: "",
        },
    })

    async function onSubmit(data: LoginFormValues) {
        setIsLoading(true)
        setErrorMessage(null)

        try {
            const response = await httpClient.post<{ token?: string }>("/login", {
                email: data.email,
                password: data.password,
            })

            // Se a API retornar um token, salva no localStorage
            if (response && typeof response === "object" && "token" in response) {
                localStorage.setItem("token", response.token as string)
            }

            navigate("/dashboard")
        } catch (error) {
            if (error instanceof HttpError) {
                if (error.status === 401) {
                    setErrorMessage("E-mail ou senha incorretos.")
                } else if (error.status === 400) {
                    setErrorMessage("Dados inválidos. Verifique os campos e tente novamente.")
                } else {
                    setErrorMessage("Erro ao fazer login. Tente novamente mais tarde.")
                }
            } else {
                setErrorMessage("Erro de conexão. Verifique sua internet e tente novamente.")
            }
        } finally {
            setIsLoading(false)
        }
    }

    return (
        <div className="bg-muted flex min-h-svh flex-col items-center justify-center gap-6 p-6 md:p-10">
            <div className="flex w-full max-w-sm flex-col gap-6">
                <a href="/" className="flex items-center gap-2 self-center font-medium">
                    <div className="bg-primary text-primary-foreground flex size-9 items-center justify-center rounded-md">
                        <Logo size={36} />
                    </div>
                    Rendatop
                </a>

                <div className={cn("flex flex-col gap-6")}>
                    <Card>
                        <CardHeader className="text-center">
                            <CardTitle className="text-xl">Seja bem-vindo de volta</CardTitle>
                            <CardDescription>
                                Entre com suas credenciais para acessar sua conta
                            </CardDescription>
                        </CardHeader>
                        <CardContent>
                            <Form {...form}>
                                <form onSubmit={form.handleSubmit(onSubmit)}>
                                    <div className="grid gap-6">
                                        {errorMessage && (
                                            <div className="rounded-md bg-destructive/10 border border-destructive/20 p-3 text-sm text-destructive">
                                                {errorMessage}
                                            </div>
                                        )}

                                        <div className="grid gap-4">
                                            <FormField
                                                control={form.control}
                                                name="email"
                                                render={({ field }) => (
                                                    <FormItem>
                                                        <FormLabel>Email</FormLabel>
                                                        <FormControl>
                                                            <Input
                                                                type="email"
                                                                placeholder="seu@email.com"
                                                                disabled={isLoading}
                                                                {...field}
                                                            />
                                                        </FormControl>
                                                        <FormMessage />
                                                    </FormItem>
                                                )}
                                            />
                                            <FormField
                                                control={form.control}
                                                name="password"
                                                render={({ field }) => (
                                                    <FormItem>
                                                        <div className="flex items-center">
                                                            <FormLabel>Senha</FormLabel>
                                                            <a
                                                                href="/auth/forgot-password"
                                                                className="ml-auto text-sm underline-offset-4 hover:underline"
                                                            >
                                                                Esqueceu sua senha?
                                                            </a>
                                                        </div>
                                                        <FormControl>
                                                            <Input
                                                                type="password"
                                                                disabled={isLoading}
                                                                {...field}
                                                            />
                                                        </FormControl>
                                                        <FormMessage />
                                                    </FormItem>
                                                )}
                                            />
                                            <Button
                                                type="submit"
                                                className="w-full cursor-pointer"
                                                disabled={isLoading}
                                            >
                                                {isLoading ? (
                                                    <>
                                                        <Loader2 className="mr-2 size-4 animate-spin" />
                                                        Entrando...
                                                    </>
                                                ) : (
                                                    "Entrar"
                                                )}
                                            </Button>
                                        </div>

                                        <div className="text-center text-sm">
                                            Não tem uma conta?{" "}
                                            <a
                                                href="/auth/sign-up"
                                                className="underline underline-offset-4"
                                            >
                                                Cadastre-se
                                            </a>
                                        </div>
                                    </div>
                                </form>
                            </Form>
                        </CardContent>
                    </Card>
                    <div className="text-muted-foreground *:[a]:hover:text-primary text-center text-xs text-balance *:[a]:underline *:[a]:underline-offset-4">
                        Ao continuar você aceita nossos <a href="#">Termos de Serviço</a>{" "}
                        e a nossa <a href="#">Política de Privacidade</a>.
                    </div>
                </div>
            </div>
        </div>
    )
}
