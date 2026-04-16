import React, { useMemo, useState } from "react";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Label } from "@/components/ui/label";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Eye, EyeOff, AlertCircle } from "lucide-react";
import { PasswordRequirements } from "@/components/PasswordRequirements";
import axiosInstance from "@/utils/axiosConfig";
import { appPath } from "@/utils/appPath";
import { getPasswordValidationMessage } from "@/utils/passwordPolicy";

const ResetPassword = () => {
    const token = useMemo(() => {
        const params = new URLSearchParams(window.location.search);
        return params.get("token") || "";
    }, []);

    const [showPassword, setShowPassword] = useState(false);
    const [showConfirmPassword, setShowConfirmPassword] = useState(false);
    const [loading, setLoading] = useState(false);
    const [errorMessage, setErrorMessage] = useState("");
    const [successMessage, setSuccessMessage] = useState("");
    const [password, setPassword] = useState("");
    const [confirmPassword, setConfirmPassword] = useState("");

    const handleSubmit = async (event) => {
        event.preventDefault();
        setErrorMessage("");
        setSuccessMessage("");

        const formData = new FormData(event.target);
        if (!token) {
            setErrorMessage("Link de redefinição ausente ou inválido.");
            return;
        }

        const passwordError = getPasswordValidationMessage(password);
        if (passwordError) {
            setErrorMessage(passwordError);
            return;
        }

        if (password !== confirmPassword) {
            setErrorMessage("A confirmação da senha não confere.");
            return;
        }

        setLoading(true);

        axiosInstance.post("/password-reset/confirm", { token, password })
            .then((response) => {
                setSuccessMessage(response?.data?.message || "Senha redefinida com sucesso.");
                event.target.reset();
                setPassword("");
                setConfirmPassword("");
                window.setTimeout(() => {
                    window.location.href = appPath("/login");
                }, 1500);
            })
            .catch((error) => {
                setErrorMessage(typeof error?.response?.data === "string"
                    ? error.response.data
                    : "Não foi possível redefinir a senha.");
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
                        <CardTitle className="text-2xl font-bold tracking-tight">Redefinir senha</CardTitle>
                        <CardDescription>
                            Digite sua nova senha
                        </CardDescription>
                    </CardHeader>
                    <CardContent>
                        <form onSubmit={handleSubmit} className="space-y-5">
                            <div className="space-y-2">
                                <Label htmlFor="password" className="text-sm font-medium">Nova senha</Label>
                                <div className="relative">
                                    <Input
                                        id="password"
                                        type={showPassword ? "text" : "password"}
                                        name="password"
                                        value={password}
                                        onChange={(e) => setPassword(e.target.value)}
                                        required
                                        minLength={9}
                                        className="h-11 pr-10"
                                    />
                                    <button
                                        type="button"
                                        onClick={() => setShowPassword((current) => !current)}
                                        className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground transition-colors"
                                    >
                                        {showPassword ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
                                    </button>
                                </div>
                            </div>

                            <div className="space-y-2">
                                <Label htmlFor="confirm_password" className="text-sm font-medium">Confirmar senha</Label>
                                <div className="relative">
                                    <Input
                                        id="confirm_password"
                                        type={showConfirmPassword ? "text" : "password"}
                                        name="confirm_password"
                                        value={confirmPassword}
                                        onChange={(e) => setConfirmPassword(e.target.value)}
                                        required
                                        minLength={9}
                                        className="h-11 pr-10"
                                    />
                                    <button
                                        type="button"
                                        onClick={() => setShowConfirmPassword((current) => !current)}
                                        className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground transition-colors"
                                    >
                                        {showConfirmPassword ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
                                    </button>
                                </div>
                            </div>

                            <PasswordRequirements
                                password={password}
                                confirmPassword={confirmPassword}
                                visible={password.length > 0 || confirmPassword.length > 0}
                            />

                            <Button type="submit" className="w-full h-11 font-medium" disabled={loading}>
                                {loading ? "Salvando..." : "Salvar nova senha"}
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
                                    <AlertTitle><b>Senha redefinida</b></AlertTitle>
                                    <AlertDescription>
                                        {successMessage} Redirecionando para o login...
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

export default ResetPassword;
