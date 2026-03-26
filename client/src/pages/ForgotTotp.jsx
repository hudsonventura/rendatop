import React, { useState } from "react";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Label } from "@/components/ui/label";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { AlertCircle } from "lucide-react";
import axiosInstance from "@/utils/axiosConfig";
import { appPath } from "@/utils/appPath";

const ForgotTotp = () => {
    const [loading, setLoading] = useState(false);
    const [errorMessage, setErrorMessage] = useState("");
    const [successMessage, setSuccessMessage] = useState("");

    const handleSubmit = async (event) => {
        event.preventDefault();
        setErrorMessage("");
        setSuccessMessage("");
        setLoading(true);

        const formData = new FormData(event.target);
        const email = formData.get("email");

        axiosInstance.post("/totp-reset/request", { email })
            .then((response) => {
                setSuccessMessage(response?.data?.message || "Se existir uma conta com TOTP ativo nesse email, enviaremos um link.");
                event.target.reset();
            })
            .catch((error) => {
                setErrorMessage(typeof error?.response?.data === "string"
                    ? error.response.data
                    : "Não foi possível solicitar a remoção do TOTP.");
            })
            .finally(() => setLoading(false));
    };

    return (
        <div className="min-h-screen flex items-center justify-center bg-background p-6">
            <div className="w-full max-w-md space-y-6">
                <div className="flex items-center justify-center gap-3">
                    <div className="flex h-10 w-10 items-center justify-center overflow-hidden rounded-lg">
                        <img src="/favicon.svg" alt="RendaTop" className="h-10 w-10" />
                    </div>
                    <span className="text-xl font-bold">RendaTop</span>
                </div>

                <Card className="border-0 shadow-xl bg-card">
                    <CardHeader className="space-y-2 pb-6">
                        <CardTitle className="text-2xl font-bold tracking-tight">Remover TOTP</CardTitle>
                        <CardDescription>
                            Se você perdeu o app autenticador, informe seu email para receber um link de confirmação. O código de acesso (TOTP) será removido e poderá ser ativado novamente nas configurações 
                        </CardDescription>
                    </CardHeader>
                    <CardContent>
                        <form onSubmit={handleSubmit} className="space-y-5">
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

                            <Button type="submit" className="w-full h-11 font-medium" disabled={loading}>
                                {loading ? "Enviando..." : "Enviar link para remover TOTP"}
                            </Button>

                            {errorMessage && (
                                <Alert variant="destructive">
                                    <AlertCircle className="h-4 w-4" />
                                    <AlertTitle><b>Erro</b></AlertTitle>
                                    <AlertDescription>{errorMessage}</AlertDescription>
                                </Alert>
                            )}

                            {successMessage && (
                                <Alert>
                                    <AlertTitle><b>Email enviado</b></AlertTitle>
                                    <AlertDescription>
                                        {successMessage} <a href={appPath("/login")} className="font-medium underline underline-offset-4">Voltar para o login</a>
                                    </AlertDescription>
                                </Alert>
                            )}
                        </form>
                    </CardContent>
                </Card>
            </div>
        </div>
    );
};

export default ForgotTotp;
