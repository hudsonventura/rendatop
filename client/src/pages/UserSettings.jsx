import React, { useEffect, useState } from "react"
import { BaseLayout } from "@/components/layouts/base-layout"
import Logged from "@/components/Logged"
import axiosInstance from "@/utils/axiosConfig"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Label } from "@/components/ui/label"
import { Input } from "@/components/ui/input"
import { Switch } from "@/components/ui/switch"
import { Button } from "@/components/ui/button"
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert"
import { AlertCircle, CheckCircle2 } from "lucide-react"

const UserSettings = () => {
    const [loading, setLoading] = useState(true)
    const [saving, setSaving] = useState(false)
    const [testingTelegram, setTestingTelegram] = useState(false)
    const [testingWhatsApp, setTestingWhatsApp] = useState(false)
    const [success, setSuccess] = useState("")
    const [error, setError] = useState("")

    const [email, setEmail] = useState("")
    const [phone, setPhone] = useState("")
    const [password, setPassword] = useState("")
    const [confirmPassword, setConfirmPassword] = useState("")
    const [notifyWhatsapp, setNotifyWhatsapp] = useState(false)
    const [notifyTelegram, setNotifyTelegram] = useState(true)

    useEffect(() => {
        axiosInstance
            .get("/User/Settings")
            .then((response) => {
                const data = response.data
                setEmail(data.email || "")
                setPhone(data.phone || "")
                setNotifyWhatsapp(Boolean(data.notify_whatsapp))
                setNotifyTelegram(Boolean(data.notify_telegram))
            })
            .catch(() => {
                setError("Não foi possível carregar suas configurações.")
            })
            .finally(() => {
                setLoading(false)
            })
    }, [])

    const handlePhoneChange = (value) => {
        const digits = value.replace(/\D/g, "").slice(0, 11)
        setPhone(digits)
    }

    const handleSubmit = (event) => {
        event.preventDefault()
        setError("")
        setSuccess("")

        if (password && password.length < 6) {
            setError("A senha deve ter pelo menos 6 caracteres.")
            return
        }

        if (password !== confirmPassword) {
            setError("As senhas não conferem.")
            return
        }

        if (phone && phone.length !== 11) {
            setError("Telefone deve ter 11 dígitos no formato 99999999999.")
            return
        }

        setSaving(true)
        axiosInstance
            .patch("/User/Settings", {
                email,
                password: password || null,
                phone,
                notify_whatsapp: notifyWhatsapp,
                notify_telegram: notifyTelegram,
            })
            .then((response) => {
                const data = response.data
                sessionStorage.setItem("email", data.email)
                if (data.name) sessionStorage.setItem("name", data.name)
                setPassword("")
                setConfirmPassword("")
                setSuccess("Configurações salvas com sucesso.")
            })
            .catch((err) => {
                const message = typeof err?.response?.data === "string"
                    ? err.response.data
                    : "Não foi possível salvar suas configurações."
                setError(message)
            })
            .finally(() => {
                setSaving(false)
            })
    }

    const handleTestTelegram = () => {
        setError("")
        setSuccess("")
        setTestingTelegram(true)

        axiosInstance
            .post("/User/Settings/TestTelegram")
            .then((response) => {
                const message = response?.data?.message || "Mensagem de teste enviada no Telegram."
                setSuccess(message)
            })
            .catch((err) => {
                const message = typeof err?.response?.data === "string"
                    ? err.response.data
                    : "Não foi possível enviar a mensagem de teste no Telegram."
                setError(message)
            })
            .finally(() => {
                setTestingTelegram(false)
            })
    }

    const handleTestWhatsApp = () => {
        setError("")
        setSuccess("")
        setTestingWhatsApp(true)

        axiosInstance
            .post("/User/Settings/TestWhatsApp")
            .then((response) => {
                const message = response?.data?.message || "Mensagem de teste enviada no WhatsApp."
                setSuccess(message)
            })
            .catch((err) => {
                const message = typeof err?.response?.data === "string"
                    ? err.response.data
                    : "Não foi possível enviar a mensagem de teste no WhatsApp."
                setError(message)
            })
            .finally(() => {
                setTestingWhatsApp(false)
            })
    }

    return (
        <>
            <Logged />
            <BaseLayout title="Configurações" description="Atualize seus dados e preferências de notificação">
                <div className="px-4 lg:px-6">
                    <Card>
                        <CardHeader>
                            <CardTitle>Dados da conta</CardTitle>
                            <CardDescription>
                                Você pode alterar email, senha, telefone e preferências de notificação.
                            </CardDescription>
                        </CardHeader>
                        <CardContent>
                            {loading ? (
                                <p className="text-sm text-muted-foreground">Carregando configurações...</p>
                            ) : (
                                <form onSubmit={handleSubmit} className="space-y-5">
                                    <div className="space-y-2">
                                        <Label htmlFor="email">Email</Label>
                                        <Input
                                            id="email"
                                            type="email"
                                            value={email}
                                            onChange={(e) => setEmail(e.target.value)}
                                            placeholder="seu@email.com"
                                            required
                                        />
                                    </div>

                                    <div className="space-y-2">
                                        <Label htmlFor="phone">Telefone</Label>
                                        <Input
                                            id="phone"
                                            type="text"
                                            value={phone}
                                            onChange={(e) => handlePhoneChange(e.target.value)}
                                            placeholder="99999999999"
                                            inputMode="numeric"
                                            maxLength={11}
                                        />
                                        <p className="text-xs text-muted-foreground">
                                            Formato: 99999999999
                                        </p>
                                    </div>

                                    <div className="grid gap-4 md:grid-cols-2">
                                        <div className="space-y-2">
                                            <Label htmlFor="password">Nova senha</Label>
                                            <Input
                                                id="password"
                                                type="password"
                                                value={password}
                                                onChange={(e) => setPassword(e.target.value)}
                                                placeholder="Deixe em branco para manter"
                                            />
                                        </div>
                                        <div className="space-y-2">
                                            <Label htmlFor="confirmPassword">Confirmar nova senha</Label>
                                            <Input
                                                id="confirmPassword"
                                                type="password"
                                                value={confirmPassword}
                                                onChange={(e) => setConfirmPassword(e.target.value)}
                                                placeholder="Repita a nova senha"
                                            />
                                        </div>
                                    </div>

                                    <div className="space-y-3 rounded-md border p-4">
                                        <h4 className="text-sm font-medium">Notificações</h4>
                                        <div className="flex items-center justify-between">
                                            <div>
                                                <p className="text-sm font-medium">WhatsApp</p>
                                                <p className="text-xs text-muted-foreground">Receber notificações por WhatsApp</p>
                                            </div>
                                            <Switch
                                                checked={notifyWhatsapp}
                                                onCheckedChange={setNotifyWhatsapp}
                                            />
                                        </div>
                                        <div className="flex items-center justify-between">
                                            <div>
                                                <p className="text-sm font-medium">Telegram</p>
                                                <p className="text-xs text-muted-foreground">Receber notificações por Telegram</p>
                                            </div>
                                            <Switch
                                                checked={notifyTelegram}
                                                onCheckedChange={setNotifyTelegram}
                                            />
                                        </div>
                                        <div className="pt-2">
                                            <Button
                                                type="button"
                                                variant="outline"
                                                onClick={handleTestWhatsApp}
                                                disabled={testingWhatsApp}
                                            >
                                                {testingWhatsApp ? "Enviando teste..." : "Testar WhatsApp"}
                                            </Button>
                                        </div>
                                        <div className="pt-1">
                                            <Button
                                                type="button"
                                                variant="outline"
                                                onClick={handleTestTelegram}
                                                disabled={testingTelegram}
                                            >
                                                {testingTelegram ? "Enviando teste..." : "Testar Telegram"}
                                            </Button>
                                        </div>
                                    </div>

                                    {error && (
                                        <Alert variant="destructive">
                                            <AlertCircle className="h-4 w-4" />
                                            <AlertTitle>Erro</AlertTitle>
                                            <AlertDescription>{error}</AlertDescription>
                                        </Alert>
                                    )}

                                    {success && (
                                        <Alert variant="success">
                                            <CheckCircle2 className="h-4 w-4" />
                                            <AlertTitle>Sucesso</AlertTitle>
                                            <AlertDescription>{success}</AlertDescription>
                                        </Alert>
                                    )}

                                    <div className="flex justify-end">
                                        <Button type="submit" disabled={saving}>
                                            {saving ? "Salvando..." : "Salvar alterações"}
                                        </Button>
                                    </div>
                                </form>
                            )}
                        </CardContent>
                    </Card>
                </div>
            </BaseLayout>
        </>
    )
}

export default UserSettings
