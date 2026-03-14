import React, { useEffect, useMemo, useState } from "react";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { AlertCircle } from "lucide-react";
import axiosInstance from "@/utils/axiosConfig";

const ResetTotp = () => {
    const token = useMemo(() => {
        const params = new URLSearchParams(window.location.search);
        return params.get("token") || "";
    }, []);

    const [loading, setLoading] = useState(false);
    const [errorMessage, setErrorMessage] = useState("");
    const [successMessage, setSuccessMessage] = useState("");

    useEffect(() => {
        if (!successMessage) return;

        const timerId = window.setTimeout(() => {
            window.location.href = "/login";
        }, 1500);

        return () => window.clearTimeout(timerId);
    }, [successMessage]);

    const handleConfirm = async () => {
        setErrorMessage("");

        if (!token) {
            setErrorMessage("Link para remover o TOTP ausente ou inválido.");
            return;
        }

        setLoading(true);

        axiosInstance.post("/totp-reset/confirm", { token })
            .then((response) => {
                setSuccessMessage(response?.data?.message || "Autenticação em duas etapas removida com sucesso.");
            })
            .catch((error) => {
                setErrorMessage(typeof error?.response?.data === "string"
                    ? error.response.data
                    : "Não foi possível remover o TOTP.");
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
                        <CardTitle className="text-2xl font-bold tracking-tight">Confirmar remoção do TOTP</CardTitle>
                        <CardDescription>
                            Confirme a remoção da autenticação em duas etapas desta conta. Depois você poderá entrar só com email e senha.
                        </CardDescription>
                    </CardHeader>
                    <CardContent className="space-y-5">
                        <Button type="button" className="w-full h-11 font-medium" disabled={loading || !!successMessage} onClick={handleConfirm}>
                            {loading ? "Removendo..." : "Remover autenticação em duas etapas"}
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
                                <AlertTitle><b>TOTP removido</b></AlertTitle>
                                <AlertDescription>{successMessage} Redirecionando para o login...</AlertDescription>
                            </Alert>
                        )}
                    </CardContent>
                </Card>
            </div>
        </div>
    );
};

export default ResetTotp;
