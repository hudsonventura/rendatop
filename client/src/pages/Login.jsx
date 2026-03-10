import React, { useEffect, useState } from 'react';
import { Card, CardHeader, CardTitle, CardDescription, CardContent } from "@/components/ui/card"
import { Label } from "@/components/ui/label"
import { Input } from "@/components/ui/input"
import { Button } from "@/components/ui/button"
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert"
import { AlertCircle, TrendingUp, Eye, EyeOff } from "lucide-react"
import axiosInstance from "@/utils/axiosConfig";

const Login = () => {

    const [erro, setErro] = useState(false);
    const [loading, setLoading] = useState(false);
    const [showPassword, setShowPassword] = useState(false);

    useEffect(() => {
        // If a valid session cookie already exists, skip login page
        axiosInstance.get('/Authenticated')
            .then(() => { window.location.href = '/home'; })
            .catch(() => { /* not logged in, stay on login page */ });
    }, []);

    const handleLogin = async (event) => {
        event.preventDefault();
        setErro(false);
        setLoading(true);
        const formData = new FormData(event.target);
        const email = formData.get('email');
        const password = formData.get('password');

        axiosInstance
            .post("/login", { email, password })
            .then((response) => {
                // Server sets the HttpOnly cookie — we only store display info
                const { name, email: userEmail } = response.data;
                sessionStorage.setItem('name', name);
                sessionStorage.setItem('email', userEmail);
                window.location.href = '/home';
            })
            .catch((error) => {
                setErro(true);
                setLoading(false);
            });
    };

    return (
        <div className="min-h-screen flex">
            {/* Left panel — gradient branding */}
            <div className="hidden lg:flex lg:w-1/2 relative overflow-hidden bg-gradient-to-br from-primary via-primary/80 to-primary/60 items-center justify-center">
                <div className="absolute inset-0 opacity-10">
                    <div className="absolute top-20 left-20 w-72 h-72 bg-white rounded-full blur-3xl" />
                    <div className="absolute bottom-20 right-20 w-96 h-96 bg-white rounded-full blur-3xl" />
                    <div className="absolute top-1/2 left-1/3 w-48 h-48 bg-white rounded-full blur-2xl" />
                </div>
                <div className="relative z-10 text-center space-y-6 px-12">
                    <div className="flex items-center justify-center gap-3 mb-8">
                        <div className="flex h-14 w-14 items-center justify-center rounded-xl bg-white/20 backdrop-blur-sm">
                            <TrendingUp className="h-8 w-8 text-primary-foreground" />
                        </div>
                    </div>
                    <h1 className="text-4xl font-bold text-primary-foreground tracking-tight">
                        RendaTop
                    </h1>
                    <p className="text-lg text-primary-foreground/80 max-w-md mx-auto leading-relaxed">
                        Gerencie seus investimentos de renda fixa com facilidade. Acompanhe rendimentos, impostos e vencimentos em um só lugar.
                    </p>
                    <div className="flex items-center justify-center gap-8 pt-4">
                        <div className="text-center">
                            <p className="text-2xl font-bold text-primary-foreground">CDI</p>
                            <p className="text-sm text-primary-foreground/60">Indexadores</p>
                        </div>
                        <div className="w-px h-10 bg-primary-foreground/20"></div>
                        <div className="text-center">
                            <p className="text-2xl font-bold text-primary-foreground">IPCA+</p>
                            <p className="text-sm text-primary-foreground/60">Pré-fixado</p>
                        </div>
                        <div className="w-px h-10 bg-primary-foreground/20"></div>
                        <div className="text-center">
                            <p className="text-2xl font-bold text-primary-foreground">%a.a.</p>
                            <p className="text-sm text-primary-foreground/60">Rendimento</p>
                        </div>
                    </div>
                </div>
            </div>

            {/* Right panel — login form */}
            <div className="flex-1 flex items-center justify-center p-6 bg-background">
                <div className="w-full max-w-md space-y-8">
                    {/* Mobile logo */}
                    <div className="lg:hidden flex items-center justify-center gap-3 mb-4">
                        <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-primary">
                            <TrendingUp className="h-5 w-5 text-primary-foreground" />
                        </div>
                        <span className="text-xl font-bold">RendaTop</span>
                    </div>

                    <form onSubmit={handleLogin}>
                        <Card className="border-0 shadow-xl bg-card">
                            <CardHeader className="space-y-2 pb-6">
                                <CardTitle className="text-2xl font-bold tracking-tight">Bem-vindo de volta</CardTitle>
                                <CardDescription className="text-muted-foreground">
                                    Informe seus dados para acessar sua conta
                                </CardDescription>
                            </CardHeader>
                            <CardContent>
                                <div className="space-y-5">
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
                                                className="h-11 pr-10"
                                            />
                                            <button
                                                type="button"
                                                onClick={() => setShowPassword(!showPassword)}
                                                className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground transition-colors"
                                            >
                                                {showPassword ? (
                                                    <EyeOff className="h-4 w-4" />
                                                ) : (
                                                    <Eye className="h-4 w-4" />
                                                )}
                                            </button>
                                        </div>
                                    </div>

                                    <Button
                                        type="submit"
                                        className="w-full h-11 font-medium"
                                        disabled={loading}
                                    >
                                        {loading ? (
                                            <span className="flex items-center gap-2">
                                                <span className="h-4 w-4 animate-spin rounded-full border-2 border-current border-t-transparent" />
                                                Entrando...
                                            </span>
                                        ) : (
                                            "Entrar"
                                        )}
                                    </Button>
                                    <Button variant="outline" className="w-full cursor-pointer" type="button">
                                        <svg xmlns="http://www.w3.org/2000/svg" viewBox="52 42 88 66">
                                            <path fill="#4285f4" d="M58 108h14V74L52 59v43c0 3.32 2.69 6 6 6" />
                                            <path fill="#34a853" d="M120 108h14c3.32 0 6-2.69 6-6V59l-20 15" />
                                            <path fill="#fbbc04" d="M120 48v26l20-15v-8c0-7.42-8.47-11.65-14.4-7.2" />
                                            <path fill="#ea4335" d="M72 74V48l24 18 24-18v26L96 92" />
                                            <path fill="#c5221f" d="M52 51v8l20 15V48l-5.6-4.2c-5.94-4.45-14.4-.22-14.4 7.2" />
                                        </svg>
                                        Login com Google / GMail
                                    </Button>
                                    <Button variant="outline" className="w-full cursor-pointer" type="button">
                                        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 21 21"><path fill="#f35325" d="M0 0h10v10H0z" /><path fill="#81bc06" d="M11 0h10v10H11z" /><path fill="#05a6f0" d="M0 11h10v10H0z" /><path fill="#ffba08" d="M11 11h10v10H11z" /></svg>
                                        Entrar com a Microsoft / Outlook
                                    </Button>

                                    {erro && (
                                        <Alert variant="destructive">
                                            <AlertCircle className="h-4 w-4" />
                                            <AlertTitle><b>Erro</b></AlertTitle>
                                            <AlertDescription>
                                                Email ou senha inválidos. Tente novamente.
                                            </AlertDescription>
                                        </Alert>
                                    )}

                                    <div className="pt-2 space-y-2 text-center">
                                        <a href="#" className="text-sm text-muted-foreground hover:text-foreground underline-offset-4 hover:underline transition-colors">
                                            Esqueci minha senha
                                        </a>
                                    <p className="text-sm text-muted-foreground">
                                        Não tem uma conta?{" "}
                                        <a href="/signup" className="text-foreground hover:underline underline-offset-4 font-medium">
                                            Criar conta
                                        </a>
                                    </p>
                                </div>
                                </div>
                            </CardContent>
                        </Card>
                    </form>
                </div>
            </div>
        </div>
    );
};

export default Login;
