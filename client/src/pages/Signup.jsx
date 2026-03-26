import React, { useEffect, useState } from 'react';
import { Card, CardHeader, CardTitle, CardDescription, CardContent } from "@/components/ui/card"
import { Label } from "@/components/ui/label"
import { Input } from "@/components/ui/input"
import { Button } from "@/components/ui/button"
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert"
import { AlertCircle, TrendingUp, Eye, EyeOff, UserPlus } from "lucide-react"
import axiosInstance from "@/utils/axiosConfig";
import { appPath } from "@/utils/appPath";

const Signup = () => {

    const [erro, setErro] = useState(false);
    const [erroMessage, setErroMessage] = useState("");
    const [loading, setLoading] = useState(false);
    const [showPassword, setShowPassword] = useState(false);

    useEffect(() => {
        axiosInstance.get('/Authenticated')
            .then(() => { window.location.href = appPath('/home'); })
            .catch(() => { });
    }, []);

    const handleSignup = async (event) => {
        event.preventDefault();
        setErro(false);
        setErroMessage("");
        setLoading(true);

        const formData = new FormData(event.target);
        const name = formData.get('name');
        const email = formData.get('email');
        const password = formData.get('password');
        const confirmPassword = formData.get('confirmPassword');

        if (password !== confirmPassword) {
            setErro(true);
            setErroMessage("As senhas não conferem.");
            setLoading(false);
            return;
        }

        axiosInstance
            .post("/signup", { name, email, password })
            .then((response) => {
                const { name: userName, email: userEmail } = response.data;
                sessionStorage.setItem('name', userName);
                sessionStorage.setItem('email', userEmail);
                window.location.href = appPath('/home');
            })
            .catch((error) => {
                setErro(true);
                setErroMessage(typeof error?.response?.data === "string"
                    ? error.response.data
                    : "Não foi possível criar sua conta.");
                setLoading(false);
            });
    };

    return (
        <div className="min-h-screen flex">
            <div className="hidden lg:flex lg:w-1/2 relative overflow-hidden bg-gradient-to-br from-primary via-primary/80 to-primary/60 items-center justify-center">
                <div className="absolute inset-0 opacity-10">
                    <div className="absolute top-20 left-20 w-72 h-72 bg-white rounded-full blur-3xl" />
                    <div className="absolute bottom-20 right-20 w-96 h-96 bg-white rounded-full blur-3xl" />
                    <div className="absolute top-1/2 left-1/3 w-48 h-48 bg-white rounded-full blur-2xl" />
                </div>
                <div className="relative z-10 text-center space-y-6 px-12">
                    <div className="flex items-center justify-center gap-3 mb-8">
                        <div className="flex h-14 w-14 items-center justify-center overflow-hidden rounded-xl bg-white/20 backdrop-blur-sm">
                            <img src="/favicon.svg" alt="RendaTop" className="h-14 w-14" />
                        </div>
                    </div>
                    <h1 className="text-4xl font-bold text-primary-foreground tracking-tight">
                        RendaTop
                    </h1>
                    <p className="text-lg text-primary-foreground/80 max-w-md mx-auto leading-relaxed">
                        Crie sua conta para acompanhar investimentos, projeções e vencimentos em um único painel.
                    </p>
                </div>
            </div>

            <div className="flex-1 flex items-center justify-center p-6 bg-background">
                <div className="w-full max-w-md space-y-8">
                    <div className="lg:hidden flex items-center justify-center gap-3 mb-4">
                        <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-primary">
                            <TrendingUp className="h-5 w-5 text-primary-foreground" />
                        </div>
                        <span className="text-xl font-bold">RendaTop</span>
                    </div>

                    <form onSubmit={handleSignup}>
                        <Card className="border-0 shadow-xl bg-card">
                            <CardHeader className="space-y-2 pb-6">
                                <CardTitle className="text-2xl font-bold tracking-tight">Criar conta</CardTitle>
                                <CardDescription className="text-muted-foreground">
                                    Preencha seus dados para começar
                                </CardDescription>
                            </CardHeader>
                            <CardContent>
                                <div className="space-y-5">
                                    <div className="space-y-2">
                                        <Label htmlFor="name" className="text-sm font-medium">Nome</Label>
                                        <Input
                                            id="name"
                                            type="text"
                                            name="name"
                                            placeholder="Seu nome completo"
                                            required
                                            className="h-11"
                                        />
                                    </div>

                                    <div className="space-y-2">
                                        <Label htmlFor="email" className="text-sm font-medium">Email</Label>
                                        <Input
                                            id="email"
                                            type="email"
                                            name="email"
                                            placeholder="seu@email.com"
                                            required
                                            className="h-11"
                                        />
                                    </div>

                                    <div className="space-y-2">
                                        <Label htmlFor="password" className="text-sm font-medium">Senha</Label>
                                        <div className="relative">
                                            <Input
                                                id="password"
                                                type={showPassword ? "text" : "password"}
                                                name="password"
                                                required
                                                minLength={6}
                                                className="h-11 pr-10"
                                            />
                                            <button
                                                type="button"
                                                onClick={() => setShowPassword(!showPassword)}
                                                className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground transition-colors"
                                            >
                                                {showPassword ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
                                            </button>
                                        </div>
                                    </div>

                                    <div className="space-y-2">
                                        <Label htmlFor="confirmPassword" className="text-sm font-medium">Confirmar senha</Label>
                                        <Input
                                            id="confirmPassword"
                                            type={showPassword ? "text" : "password"}
                                            name="confirmPassword"
                                            required
                                            minLength={6}
                                            className="h-11"
                                        />
                                    </div>

                                    <Button
                                        type="submit"
                                        className="w-full h-11 font-medium"
                                        disabled={loading}
                                    >
                                        {loading ? (
                                            <span className="flex items-center gap-2">
                                                <span className="h-4 w-4 animate-spin rounded-full border-2 border-current border-t-transparent" />
                                                Criando conta...
                                            </span>
                                        ) : (
                                            "Criar conta"
                                        )}
                                    </Button>

                                    {erro && (
                                        <Alert variant="destructive">
                                            <AlertCircle className="h-4 w-4" />
                                            <AlertTitle><b>Erro</b></AlertTitle>
                                            <AlertDescription>
                                                {erroMessage}
                                            </AlertDescription>
                                        </Alert>
                                    )}

                                    <p className="text-sm text-muted-foreground text-center">
                                        Já tem uma conta?{" "}
                                        <a href={appPath("/login")} className="text-foreground hover:underline underline-offset-4 font-medium">
                                            Entrar
                                        </a>
                                    </p>
                                </div>
                            </CardContent>
                        </Card>
                    </form>
                </div>
            </div>
        </div>
    );
};

export default Signup;
