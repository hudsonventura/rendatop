"use client"

import { zodResolver } from "@hookform/resolvers/zod"
import { useForm } from "react-hook-form"
import { z } from "zod"
import { cn } from "@/lib/utils"
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

const loginFormSchema = z.object({
    email: z.string().email("Invalid email address"),
    password: z.string().min(6, "Password must be at least 6 characters"),
})

type LoginFormValues = z.infer<typeof loginFormSchema>

export function LoginForm1({
    className,
    ...props
}: React.ComponentProps<"div">) {
    const form = useForm<LoginFormValues>({
        resolver: zodResolver(loginFormSchema),
        defaultValues: {
            // email: "test@example.com",
            // password: "password",
        },
    })

    return (
        <div className={cn("flex flex-col gap-6", className)} {...props}>
            <Card>
                <CardHeader className="text-center">
                    <CardTitle className="text-xl">Seja bem vindo de volta</CardTitle>
                    <CardDescription>
                        Entre com suas credenciais abaixo ou faça login com sua conta do GMail ou Outlook/Hotmail
                    </CardDescription>
                </CardHeader>
                <CardContent>
                    <Form {...form}>
                        <form action="/">
                            <div className="grid gap-6">
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
                                                        href="/templates/dashboard/shadcn-dashboard-landing-template/auth/forgot-password"
                                                        className="ml-auto text-sm underline-offset-4 hover:underline"
                                                    >
                                                        Esqueceu sua senha?
                                                    </a>
                                                </div>
                                                <FormControl>
                                                    <Input type="password" {...field} />
                                                </FormControl>
                                                <FormMessage />
                                            </FormItem>
                                        )}
                                    />
                                    <Button type="submit" className="w-full cursor-pointer">
                                        Entrar
                                    </Button>

                                    <Button variant="outline" className="w-full cursor-pointer" type="button">
                                        <svg xmlns="http://www.w3.org/2000/svg" viewBox="52 42 88 66">
                                            <path fill="#4285f4" d="M58 108h14V74L52 59v43c0 3.32 2.69 6 6 6" />
                                            <path fill="#34a853" d="M120 108h14c3.32 0 6-2.69 6-6V59l-20 15" />
                                            <path fill="#fbbc04" d="M120 48v26l20-15v-8c0-7.42-8.47-11.65-14.4-7.2" />
                                            <path fill="#ea4335" d="M72 74V48l24 18 24-18v26L96 92" />
                                            <path fill="#c5221f" d="M52 51v8l20 15V48l-5.6-4.2c-5.94-4.45-14.4-.22-14.4 7.2" />
                                        </svg>
                                        Login with Google
                                    </Button>
                                    <Button variant="outline" className="w-full cursor-pointer" type="button">
                                        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 21 21"><path fill="#f35325" d="M0 0h10v10H0z" /><path fill="#81bc06" d="M11 0h10v10H11z" /><path fill="#05a6f0" d="M0 11h10v10H0z" /><path fill="#ffba08" d="M11 11h10v10H11z" /></svg>
                                        Login with Microsoft
                                    </Button>
                                </div>
                                <div className="text-center text-sm">
                                    Don&apos;t have an account?{" "}
                                    <a href="/templates/dashboard/shadcn-dashboard-landing-template/auth/sign-up" className="underline underline-offset-4">
                                        Sign up
                                    </a>
                                </div>
                            </div>
                        </form>
                    </Form>
                </CardContent>
            </Card>
            <div className="text-muted-foreground *:[a]:hover:text-primary text-center text-xs text-balance *:[a]:underline *:[a]:underline-offset-4">
                Caso se continue você aceita nossos <a href="#">Termos de Serviços</a>{" "}
                e a nossa <a href="#">Política de Privacidade</a>.
            </div>
        </div>
    )
}
