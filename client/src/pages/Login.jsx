import { useEffect, useState } from 'react';
import { Card, CardHeader, CardTitle, CardDescription, CardContent } from "@/components/ui/card"
import { Label } from "@/components/ui/label"
import { Input } from "@/components/ui/input"
import { Button } from "@/components/ui/button"
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert"
import { AlertCircle, Eye, EyeOff } from "lucide-react"
import axiosInstance from "@/utils/axiosConfig";
import { appPath } from "@/utils/appPath";
import LoginCandleWallpaper from "@/components/LoginCandleWallpaper";

const apiBaseUrl = (import.meta.env.VITE_API_URL || "/api").replace(/\/+$/, "") || "/api";

const Login = () => {

    const [erro, setErro] = useState(false);
    const [erroMessage, setErroMessage] = useState("Email ou senha inválidos. Tente novamente.");
    const [loading, setLoading] = useState(false);
    const [showPassword, setShowPassword] = useState(false);
    const [totpRequired, setTotpRequired] = useState(false);
    const [totpChallengeId, setTotpChallengeId] = useState("");

    const redirectToSignupVerification = (email, message) => {
        const params = new URLSearchParams();
        params.set("mode", "verify");

        if (email) {
            params.set("email", String(email));
        }

        if (message) {
            params.set("message", message);
        }

        window.location.href = `${appPath("/signup")}?${params.toString()}`;
    };

    useEffect(() => {
        const params = new URLSearchParams(window.location.search);
        const ssoStatus = params.get("sso");

        if (ssoStatus === "google_success" || ssoStatus === "microsoft_success") {
            const name = params.get("name");
            const email = params.get("email");

            if (name) sessionStorage.setItem("name", name);
            if (email) sessionStorage.setItem("email", email);
            window.location.href = appPath("/home");
            return;
        }

        if (ssoStatus === "google_error" || ssoStatus === "microsoft_error") {
            setErro(true);
            setErroMessage(params.get("message") || "Não foi possível autenticar com o provedor.");
            window.history.replaceState({}, "", appPath("/login"));
        }

        // If a valid session cookie already exists, skip login page
        axiosInstance.get('/Authenticated')
            .then(() => { window.location.href = appPath('/home'); })
            .catch(() => { /* not logged in, stay on login page */ });
    }, []);

    useEffect(() => {
        const params = new URLSearchParams(window.location.search);
        const visit = (params.get("visit") || "").trim();
        const normalizedVisit = visit ? visit.toLowerCase() : "direct";
        const storageKey = `landing-visit:${window.location.pathname}:${normalizedVisit}`;

        if (sessionStorage.getItem(storageKey)) {
            return;
        }

        sessionStorage.setItem(storageKey, "1");

        fetch(`${apiBaseUrl}/public/landing-visits`, {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
            },
            credentials: "include",
            body: JSON.stringify({ visit }),
        }).catch((error) => {
            sessionStorage.removeItem(storageKey);
            console.error("Erro ao registrar visita da landing page:", error);
        });
    }, []);

    const handleLogin = async (event) => {
        event.preventDefault();
        setErro(false);
        setLoading(true);
        const formData = new FormData(event.target);
        const email = formData.get('email');
        const password = formData.get('password');
        const totpCode = formData.get('totp_code');

        const request = totpRequired && totpChallengeId
            ? axiosInstance.post("/login/totp", {
                challenge_id: totpChallengeId,
                code: totpCode || ""
            })
            : axiosInstance.post("/login", { email, password });

        request
            .then((response) => {
                const data = response?.data || {};

                if (data.requires_totp) {
                    setTotpRequired(true);
                    setTotpChallengeId(data.challenge_id || "");
                    setErro(false);
                    setErroMessage("");
                    setLoading(false);
                    return;
                }

                // Server sets the HttpOnly cookie — we only store display info
                const { name, email: userEmail } = data;
                sessionStorage.setItem('name', name);
                sessionStorage.setItem('email', userEmail);
                window.location.href = appPath('/home');
            })
            .catch((error) => {
                const message = typeof error?.response?.data === "string"
                    ? error.response.data
                    : "Email ou senha inválidos. Tente novamente.";

                if ((message || "").toLowerCase().includes("ainda não foi ativada")) {
                    redirectToSignupVerification(email, message);
                    return;
                }

                setErro(true);
                setErroMessage(message);
                if (!totpRequired) {
                    setTotpChallengeId("");
                } else if ((message || "").toLowerCase().includes("expirado")) {
                    setTotpRequired(false);
                    setTotpChallengeId("");
                }
                setLoading(false);
            });
    };

    const handleGoogleLogin = () => {
        window.location.href = `${apiBaseUrl}/auth/google/login`;
    };


    const handleMicrosoftLogin = () => {
        window.location.href = `${apiBaseUrl}/auth/microsoft/login`;
    };

    return (
        <div className="min-h-screen flex">
            <div className="hidden lg:flex lg:w-1/2 relative overflow-hidden items-center justify-center bg-slate-950">
                <LoginCandleWallpaper />

                <div className="absolute inset-0 bg-gradient-to-tr from-slate-950/80 via-slate-950/25 to-slate-950/70" />

                <div className="relative z-10 flex w-full max-w-2xl flex-col gap-10 px-12 py-16 text-white">
                    <div className="flex items-center gap-4">
                        <div className="flex h-14 w-14 items-center justify-center overflow-hidden rounded-2xl border border-white/10 bg-white/10 shadow-lg shadow-black/20 backdrop-blur-md">
                            <img src="/favicon.svg" alt="RendaTop" className="h-14 w-14" />
                        </div>
                        <div>
                            <p className="text-sm uppercase tracking-[0.32em] text-white/55">Gestão de investimentos</p>
                            <h1 className="text-4xl font-bold tracking-tight text-white">RendaTop</h1>
                        </div>
                    </div>

                    <div className="max-w-xl space-y-4">
                        <p className="text-lg leading-relaxed text-white/78">
                            Gerencie seus investimentos de renda fixa com facilidade. Acompanhe rendimentos, impostos e vencimentos em um só lugar.
                        </p>
                    </div>

                    <div className="grid grid-cols-3 gap-4">
                        <div className="rounded-2xl border border-white/10 bg-white/5 p-4 backdrop-blur-xl">
                            <p className="text-2xl font-semibold text-white">CDI</p>
                            <p className="mt-1 text-sm text-white/55">Indexadores</p>
                        </div>
                        <div className="rounded-2xl border border-white/10 bg-white/5 p-4 backdrop-blur-xl">
                            <p className="text-2xl font-semibold text-white">IPCA+</p>
                            <p className="mt-1 text-sm text-white/55">Pré-fixado</p>
                        </div>
                        <div className="rounded-2xl border border-white/10 bg-white/5 p-4 backdrop-blur-xl">
                            <p className="text-2xl font-semibold text-white">%a.a.</p>
                            <p className="mt-1 text-sm text-white/55">Rendimento</p>
                        </div>
                    </div>
                </div>
            </div>

            {/* Right panel — login form */}
            <div className="flex-1 flex items-center justify-center p-6 bg-background">
                <div className="w-full max-w-md space-y-8">
                    {/* Mobile logo */}
                    <div className="lg:hidden flex items-center justify-center gap-3 mb-4">
                        <div className="flex h-10 w-10 items-center justify-center overflow-hidden rounded-lg">
                            <img src="/favicon.svg" alt="RendaTop" className="h-10 w-10" />
                        </div>
                        <span className="text-xl font-bold">RendaTop</span>
                    </div>

                    <form onSubmit={handleLogin}>
                        <Card className="border-0 shadow-xl bg-card">
                            <CardHeader className="space-y-2 pb-6">
                                <CardTitle className="text-2xl font-bold tracking-tight">Bem-vindo</CardTitle>
                                <CardDescription className="text-muted-foreground">
                                    {totpRequired
                                        ? "Email e senha validados. Informe o código do autenticador."
                                        : "Informe seus dados para acessar sua conta"}
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
                                            disabled={totpRequired}
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
                                                disabled={totpRequired}
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

                                    {totpRequired && (
                                        <div className="space-y-2">
                                            <Label htmlFor="totp_code" className="text-sm font-medium">Código de acesso do app Authenticator (TOTP)</Label>
                                            <Input
                                                id="totp_code"
                                                type="text"
                                                name="totp_code"
                                                placeholder="000000"
                                                inputMode="numeric"
                                                maxLength={6}
                                                className="h-11"
                                                required
                                            />
                                            <div className="pt-1 text-right">
                                                <a href={appPath("/forgot-totp")} className="text-sm text-muted-foreground hover:text-foreground underline-offset-4 hover:underline transition-colors">
                                                    Não tenho acesso ao app Authenticator
                                                </a>
                                            </div>
                                        </div>
                                    )}

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
                                            totpRequired ? "Validar código" : "Entrar"
                                        )}
                                    </Button>
                                    <Button variant="outline" className="w-full cursor-pointer" type="button" onClick={handleGoogleLogin}>
                                        <svg xmlns="http://www.w3.org/2000/svg" viewBox="52 42 88 66">
                                            <path fill="#4285f4" d="M58 108h14V74L52 59v43c0 3.32 2.69 6 6 6" />
                                            <path fill="#34a853" d="M120 108h14c3.32 0 6-2.69 6-6V59l-20 15" />
                                            <path fill="#fbbc04" d="M120 48v26l20-15v-8c0-7.42-8.47-11.65-14.4-7.2" />
                                            <path fill="#ea4335" d="M72 74V48l24 18 24-18v26L96 92" />
                                            <path fill="#c5221f" d="M52 51v8l20 15V48l-5.6-4.2c-5.94-4.45-14.4-.22-14.4 7.2" />
                                        </svg>
                                        Login com Google / GMail
                                    </Button>
                                    <Button variant="outline" className="w-full cursor-pointer" type="button" onClick={handleMicrosoftLogin}>
                                        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 21 21"><path fill="#f35325" d="M0 0h10v10H0z" /><path fill="#81bc06" d="M11 0h10v10H11z" /><path fill="#05a6f0" d="M0 11h10v10H0z" /><path fill="#ffba08" d="M11 11h10v10H11z" /></svg>
                                        Entrar com a Microsoft / Outlook
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

                                    <div className="pt-2 space-y-2 text-center">
                                        <a href={appPath("/forgot-password")} className="text-sm text-muted-foreground hover:text-foreground underline-offset-4 hover:underline transition-colors">
                                            Esqueceu sua senha?
                                        </a>
                                        <p className="text-sm text-muted-foreground">
                                            Não tem uma conta?{" "}
                                            <a href={appPath("/signup")} className="text-foreground hover:underline underline-offset-4 font-medium">
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
