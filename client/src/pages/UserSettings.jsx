import React, { useEffect, useState } from "react"
import { BaseLayout } from "@/components/layouts/base-layout"
import Logged from "@/components/Logged"
import axiosInstance from "@/utils/axiosConfig"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Badge } from "@/components/ui/badge"
import { Label } from "@/components/ui/label"
import { Input } from "@/components/ui/input"
import { Switch } from "@/components/ui/switch"
import { Button } from "@/components/ui/button"
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert"
import { AlertCircle, CheckCircle2 } from "lucide-react"
import { formatCpf } from "@/utils/cpf"

const UserSettings = () => {
    const [loading, setLoading] = useState(true)
    const [saving, setSaving] = useState(false)
    const [testingTelegram, setTestingTelegram] = useState(false)
    const [testingWhatsApp, setTestingWhatsApp] = useState(false)
    const [testingEmail, setTestingEmail] = useState(false)
    const [success, setSuccess] = useState("")
    const [error, setError] = useState("")
    const [whatsAppError, setWhatsAppError] = useState("")
    const [telegramError, setTelegramError] = useState("")
    const [emailError, setEmailError] = useState("")

    const [name, setName] = useState("")
    const [email, setEmail] = useState("")
    const [phone, setPhone] = useState("")
    const [telegramChatId, setTelegramChatId] = useState("")
    const [cpf, setCpf] = useState("")
    const [password, setPassword] = useState("")
    const [confirmPassword, setConfirmPassword] = useState("")
    const [notifyWhatsapp, setNotifyWhatsapp] = useState(false)
    const [notifyTelegram, setNotifyTelegram] = useState(true)
    const [notifyEmail, setNotifyEmail] = useState(true)
    const [calendarPublicEnabled, setCalendarPublicEnabled] = useState(false)
    const [calendarPublicUrl, setCalendarPublicUrl] = useState("")
    const [whatsappNotificationsEnabled, setWhatsappNotificationsEnabled] = useState(false)
    const [calendarIcsEnabled, setCalendarIcsEnabled] = useState(false)
    const [totpEnabled, setTotpEnabled] = useState(false)
    const [totpSecret, setTotpSecret] = useState("")
    const [totpUri, setTotpUri] = useState("")
    const [totpCode, setTotpCode] = useState("")
    const totpQrCodeUrl = totpUri
        ? `https://api.qrserver.com/v1/create-qr-code/?size=220x220&data=${encodeURIComponent(totpUri)}`
        : ""

    useEffect(() => {
        axiosInstance
            .get("/User/Settings")
            .then((response) => {
                const data = response.data
                const canUseWhatsAppNotifications = Boolean(data.whatsapp_notifications_enabled)
                const canUseCalendarIcs = Boolean(data.calendar_ics_enabled)

                setName(data.name || "")
                setEmail(data.email || "")
                setPhone(data.phone || "")
                setTelegramChatId(data.telegram_chat_id || "")
                setCpf(data.cpf || "")
                setWhatsappNotificationsEnabled(canUseWhatsAppNotifications)
                setCalendarIcsEnabled(canUseCalendarIcs)
                setNotifyWhatsapp(canUseWhatsAppNotifications && Boolean(data.notify_whatsapp))
                setNotifyTelegram(Boolean(data.notify_telegram))
                setNotifyEmail(Boolean(data.notify_email))
                setCalendarPublicEnabled(canUseCalendarIcs && Boolean(data.calendar_public_enabled))
                setCalendarPublicUrl(canUseCalendarIcs ? data.calendar_public_url || "" : "")
                setTotpEnabled(Boolean(data.totp_enabled))
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

    const clearChannelErrors = () => {
        setWhatsAppError("")
        setTelegramError("")
        setEmailError("")
    }

    const setChannelAwareError = (message) => {
        const text = typeof message === "string" ? message : ""
        const normalized = text.toLowerCase()

        clearChannelErrors()

        if (normalized.includes("telegram")) {
            setTelegramError(text)
            setError("")
            return
        }

        if (normalized.includes("whatsapp")) {
            setWhatsAppError(text)
            setError("")
            return
        }

        if (normalized.includes("email") || normalized.includes("e-mail")) {
            setEmailError(text)
            setError("")
            return
        }

        setError(text)
    }

    const handleToggleWhatsApp = (checked) => {
        if (!whatsappNotificationsEnabled) return
        if (checked && !phone) {
            setChannelAwareError("Informe o telefone antes de habilitar o WhatsApp.")
            setSuccess("")
            return
        }
        setError("")
        setWhatsAppError("")
        setNotifyWhatsapp(checked)
    }

    const handleToggleTelegram = (checked) => {
        if (checked && !telegramChatId.trim()) {
            setChannelAwareError("Informe o Chat ID do Telegram antes de habilitar as notificações.")
            setSuccess("")
            return
        }
        setError("")
        setTelegramError("")
        setNotifyTelegram(checked)
    }

    const handleSubmit = (event) => {
        event.preventDefault()
        setError("")
        setSuccess("")
        clearChannelErrors()

        const effectiveNotifyWhatsapp = whatsappNotificationsEnabled ? notifyWhatsapp : false
        const effectiveCalendarPublicEnabled = calendarIcsEnabled ? calendarPublicEnabled : false

        if (!name.trim()) {
            setError("Nome é obrigatório.")
            return
        }

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

        if (effectiveNotifyWhatsapp && !phone) {
            setChannelAwareError("Informe o telefone antes de habilitar o WhatsApp.")
            return
        }

        if (notifyTelegram && !telegramChatId.trim()) {
            setChannelAwareError("Informe o Chat ID do Telegram antes de habilitar as notificações.")
            return
        }

        setSaving(true)
        axiosInstance
            .patch("/User/Settings", {
                name,
                email,
                password: password || null,
                phone,
                notify_whatsapp: effectiveNotifyWhatsapp,
                notify_telegram: notifyTelegram,
                telegram_chat_id: telegramChatId.trim() || null,
                notify_email: notifyEmail,
                calendar_public_enabled: effectiveCalendarPublicEnabled,
            })
            .then((response) => {
                const data = response.data
                setName(data.name || "")
                sessionStorage.setItem("email", data.email)
                if (data.name) sessionStorage.setItem("name", data.name)
                setPhone(data.phone || "")
                setTelegramChatId(data.telegram_chat_id || "")
                setWhatsappNotificationsEnabled(Boolean(data.whatsapp_notifications_enabled))
                setCalendarIcsEnabled(Boolean(data.calendar_ics_enabled))
                setCalendarPublicEnabled(Boolean(data.calendar_public_enabled))
                setCalendarPublicUrl(data.calendar_public_url || "")
                setTotpEnabled(Boolean(data.totp_enabled))
                setPassword("")
                setConfirmPassword("")
                setSuccess("Configurações salvas com sucesso.")
            })
            .catch((err) => {
                const message = typeof err?.response?.data === "string"
                    ? err.response.data
                    : "Não foi possível salvar suas configurações."
                setChannelAwareError(message)
            })
            .finally(() => {
                setSaving(false)
            })
    }

    const handleTestTelegram = () => {
        setError("")
        setSuccess("")
        setTelegramError("")
        setTestingTelegram(true)

        axiosInstance
            .post("/User/Settings/TestTelegram", { telegram_chat_id: telegramChatId.trim() || null })
            .then((response) => {
                const message = response?.data?.message || "Mensagem de teste enviada no Telegram."
                setSuccess(message)
            })
            .catch((err) => {
                const message = typeof err?.response?.data === "string"
                    ? err.response.data
                    : "Não foi possível enviar a mensagem de teste no Telegram."
                setChannelAwareError(message)
            })
            .finally(() => {
                setTestingTelegram(false)
            })
    }

    const handleTestWhatsApp = () => {
        setError("")
        setSuccess("")
        setWhatsAppError("")
        setTestingWhatsApp(true)

        axiosInstance
            .post("/User/Settings/TestWhatsApp", { phone })
            .then((response) => {
                const message = response?.data?.message || "Mensagem de teste enviada no WhatsApp."
                setSuccess(message)
            })
            .catch((err) => {
                const message = typeof err?.response?.data === "string"
                    ? err.response.data
                    : "Não foi possível enviar a mensagem de teste no WhatsApp."
                setChannelAwareError(message)
            })
            .finally(() => {
                setTestingWhatsApp(false)
            })
    }

    const handleTestEmail = () => {
        setError("")
        setSuccess("")
        setEmailError("")
        setTestingEmail(true)

        axiosInstance
            .post("/User/Settings/TestEmail")
            .then((response) => {
                const message = response?.data?.message || "Mensagem de teste enviada por Email."
                setSuccess(message)
            })
            .catch((err) => {
                const message = typeof err?.response?.data === "string"
                    ? err.response.data
                    : "Não foi possível enviar o email de teste."
                setChannelAwareError(message)
            })
            .finally(() => {
                setTestingEmail(false)
            })
    }

    const handleGenerateTotp = () => {
        setError("")
        setSuccess("")

        axiosInstance
            .post("/User/Settings/Totp/Generate")
            .then((response) => {
                const data = response.data || {}
                setTotpSecret(data.secret || "")
                setTotpUri(data.otpauth_uri || "")
                setSuccess("QR Code TOTP gerado. Cadastre no app autenticador e confirme o código.")
            })
            .catch((err) => {
                const message = typeof err?.response?.data === "string"
                    ? err.response.data
                    : "Não foi possível gerar o QR Code TOTP."
                setError(message)
            })
    }

    const handleEnableTotp = () => {
        setError("")
        setSuccess("")

        axiosInstance
            .post("/User/Settings/Totp/Enable", {
                secret: totpSecret,
                code: totpCode,
            })
            .then((response) => {
                const data = response.data || {}
                setTotpEnabled(Boolean(data.totp_enabled))
                setTotpCode("")
                setSuccess("TOTP habilitado com sucesso.")
            })
            .catch((err) => {
                const message = typeof err?.response?.data === "string"
                    ? err.response.data
                    : "Não foi possível habilitar TOTP."
                setError(message)
            })
    }

    const handleDisableTotp = () => {
        setError("")
        setSuccess("")

        axiosInstance
            .post("/User/Settings/Totp/Disable", { code: totpCode })
            .then((response) => {
                const data = response.data || {}
                setTotpEnabled(Boolean(data.totp_enabled))
                setTotpCode("")
                setTotpSecret("")
                setTotpUri("")
                setSuccess("TOTP desabilitado.")
            })
            .catch((err) => {
                const message = typeof err?.response?.data === "string"
                    ? err.response.data
                    : "Não foi possível desabilitar TOTP."
                setError(message)
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
                                Você pode alterar nome, email, senha, telefone e preferências de notificação.
                            </CardDescription>
                        </CardHeader>
                        <CardContent>
                            {loading ? (
                                <p className="text-sm text-muted-foreground">Carregando configurações...</p>
                            ) : (
                                <form onSubmit={handleSubmit} className="space-y-5">
                                    <div className="space-y-2">
                                        <Label htmlFor="name">Nome</Label>
                                        <Input
                                            id="name"
                                            type="text"
                                            value={name}
                                            onChange={(e) => setName(e.target.value)}
                                            placeholder="Seu nome completo"
                                            required
                                        />
                                    </div>

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
                                        <Label htmlFor="cpf">CPF</Label>
                                        <Input
                                            id="cpf"
                                            value={cpf ? formatCpf(cpf) : ""}
                                            readOnly
                                            placeholder="CPF não informado"
                                        />
                                        <p className="text-xs text-muted-foreground">
                                            Preenchido automaticamente após um pagamento válido.
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
                                                placeholder="Digite uma nova senha para alterar"
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

                                        <div className="hidden md:grid md:grid-cols-[minmax(0,1fr)_auto_auto] md:items-center md:gap-3 px-3 text-xs font-medium text-muted-foreground">
                                            <span></span>
                                            <span className="justify-self-center"></span>
                                            <span className="justify-self-end"></span>
                                        </div>

                                        <div className="grid gap-3 rounded-md border p-3 md:grid-cols-[minmax(0,1fr)_auto_auto] md:items-center">
                                            <div>
                                                <div className="flex flex-wrap items-center gap-2">
                                                    <p className="text-sm font-medium">WhatsApp</p>
                                                    {!whatsappNotificationsEnabled && (
                                                        <Badge variant="secondary" className="text-[10px] uppercase tracking-wide">
                                                            Recurso Premium
                                                        </Badge>
                                                    )}
                                                </div>
                                                <p className="text-xs text-muted-foreground">
                                                    {whatsappNotificationsEnabled
                                                        ? "Receber notificações por WhatsApp"
                                                        : "Assine um plano para ativar este recurso."}
                                                </p>
                                                {whatsAppError && (
                                                    <Alert variant="destructive" className="mt-3">
                                                        <AlertCircle className="h-4 w-4" />
                                                        <AlertTitle>WhatsApp</AlertTitle>
                                                        <AlertDescription>{whatsAppError}</AlertDescription>
                                                    </Alert>
                                                )}
                                                <div className="mt-3 space-y-2">
                                                    <Label htmlFor="phone">Telefone do WhatsApp</Label>
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
                                                        Informe o número com 11 dígitos. Você pode testar antes mesmo de salvar as configurações.
                                                    </p>
                                                </div>
                                            </div>
                                            <div className="md:justify-self-center">
                                                <Switch
                                                    checked={notifyWhatsapp}
                                                    onCheckedChange={handleToggleWhatsApp}
                                                    disabled={!whatsappNotificationsEnabled}
                                                />
                                            </div>
                                            <Button
                                                type="button"
                                                variant="outline"
                                                size="sm"
                                                className="md:w-36 md:justify-self-end"
                                                onClick={handleTestWhatsApp}
                                                disabled={testingWhatsApp || !whatsappNotificationsEnabled}
                                            >
                                                {!whatsappNotificationsEnabled
                                                    ? "Recurso Premium"
                                                    : testingWhatsApp
                                                        ? "Enviando..."
                                                        : "Test WhatsApp"}
                                            </Button>
                                        </div>

                                        <div className="grid gap-3 rounded-md border p-3 md:grid-cols-[minmax(0,1fr)_auto_auto] md:items-center">
                                            <div>
                                                <div className="flex flex-wrap items-center gap-2">
                                                    <p className="text-sm font-medium">Telegram</p>
                                                </div>
                                                <p className="text-xs text-muted-foreground">Receber notificações por Telegram</p>
                                                {telegramError && (
                                                    <Alert variant="destructive" className="mt-3">
                                                        <AlertCircle className="h-4 w-4" />
                                                        <AlertTitle>Telegram</AlertTitle>
                                                        <AlertDescription>{telegramError}</AlertDescription>
                                                    </Alert>
                                                )}
                                                <div className="mt-3 space-y-2">
                                                    <Label htmlFor="telegramChatId">Chat ID do Telegram</Label>
                                                    <Input
                                                        id="telegramChatId"
                                                        type="text"
                                                        value={telegramChatId}
                                                        onChange={(e) => setTelegramChatId(e.target.value)}
                                                        placeholder="Ex.: 123456789"
                                                    />
                                                    <p className="text-xs text-muted-foreground">
                                                        Obrigatório apenas quando as notificações por Telegram estiverem ativas.
                                                    </p>
                                                </div>
                                            </div>
                                            <div className="md:justify-self-center">
                                                <Switch
                                                    checked={notifyTelegram}
                                                    onCheckedChange={handleToggleTelegram}
                                                />
                                            </div>
                                            <Button
                                                type="button"
                                                variant="outline"
                                                size="sm"
                                                className="md:w-36 md:justify-self-end"
                                                onClick={handleTestTelegram}
                                                disabled={testingTelegram}
                                            >
                                                {testingTelegram ? "Enviando..." : "Test Telegram"}
                                            </Button>
                                        </div>

                                        <div className="grid gap-3 rounded-md border p-3 md:grid-cols-[minmax(0,1fr)_auto_auto] md:items-center">
                                            <div>
                                                <p className="text-sm font-medium">Email</p>
                                                <p className="text-xs text-muted-foreground">Receber notificações por Email</p>
                                                {emailError && (
                                                    <Alert variant="destructive" className="mt-3">
                                                        <AlertCircle className="h-4 w-4" />
                                                        <AlertTitle>Email</AlertTitle>
                                                        <AlertDescription>{emailError}</AlertDescription>
                                                    </Alert>
                                                )}
                                            </div>
                                            <div className="md:justify-self-center">
                                                <Switch
                                                    checked={notifyEmail}
                                                    onCheckedChange={setNotifyEmail}
                                                />
                                            </div>
                                            <Button
                                                type="button"
                                                variant="outline"
                                                size="sm"
                                                className="md:w-36 md:justify-self-end"
                                                onClick={handleTestEmail}
                                                disabled={testingEmail}
                                            >
                                                {testingEmail ? "Enviando..." : "Test Email"}
                                            </Button>
                                        </div>
                                    </div>

                                    <div className="space-y-3 rounded-md border p-4">
                                        <h4 className="text-sm font-medium">Calendário público (.ics) para Outlook</h4>
                                        <p className="text-xs text-muted-foreground">
                                            Gere um link público para assinar no Outlook. Quando habilitado, o link público exibirá vencimento e conteúdo dos investimentos. Não compartilhe este link com ninguem.
                                            <br />
                                            Você pode assinar este link pelo Outlook, Thunderbird ou qualquer outro aplicativo de calendário compartivel com link <b>.ics</b>
                                        </p>

                                        <div className="grid gap-3 rounded-md border p-3 md:grid-cols-[minmax(0,1fr)_auto] md:items-center">
                                            <div>
                                                <div className="flex flex-wrap items-center gap-2">
                                                    <p className="text-sm font-medium">Compartilhar calendário</p>
                                                    {!calendarIcsEnabled && (
                                                        <Badge variant="secondary" className="text-[10px] uppercase tracking-wide">
                                                            Recurso Premium
                                                        </Badge>
                                                    )}
                                                </div>
                                                <p className="text-xs text-muted-foreground">
                                                    {calendarIcsEnabled
                                                        ? "Gera um link público para assinar no Outlook ou outro app de calendário."
                                                        : "Assine um plano para ativar este recurso."}
                                                </p>
                                            </div>
                                            <div className="md:justify-self-end">
                                                <Switch
                                                    checked={calendarPublicEnabled}
                                                    onCheckedChange={setCalendarPublicEnabled}
                                                    disabled={!calendarIcsEnabled}
                                                />
                                            </div>
                                        </div>

                                        {calendarIcsEnabled && calendarPublicEnabled && (
                                            <div className="space-y-2">
                                                <div className="flex flex-col gap-2 md:flex-row">
                                                    <Input
                                                        id="calendarPublicUrl"
                                                        value={calendarPublicUrl}
                                                        readOnly
                                                        placeholder="Salve as alterações para gerar o link"
                                                    />
                                                    <Button
                                                        type="button"
                                                        variant="outline"
                                                        onClick={async () => {
                                                            if (!calendarPublicUrl) return
                                                            await navigator.clipboard.writeText(calendarPublicUrl)
                                                            setSuccess("Link do calendário copiado.")
                                                            setError("")
                                                        }}
                                                        disabled={!calendarPublicUrl}
                                                    >
                                                        Copiar link
                                                    </Button>
                                                </div>
                                            </div>
                                        )}
                                    </div>

                                    <div className="space-y-3 rounded-md border p-4">
                                        <h4 className="text-sm font-medium">Código de acesso do app Authenticator <b> Autenticação em 2 fatores (TOTP)</b></h4>
                                        <p className="text-xs text-muted-foreground">
                                            Quando habilitado, o login comum (email/senha) exigirá o código TOTP. Isso aumenta a segurança da sua conta e evita acesso indevido.
                                            <br />
                                            TOTP não funciona com login via Microsoft ou GMail (SSO).
                                        </p>

                                        {!totpEnabled && (
                                            <div className="space-y-2">
                                                <Button type="button" variant="outline" onClick={handleGenerateTotp}>
                                                    Ativar segundo fator de autenticação (TOTP)
                                                </Button>

                                                {totpSecret && (
                                                    <>
                                                        {totpQrCodeUrl && (
                                                            <div className="w-fit rounded-md border p-2">
                                                                <img
                                                                    src={totpQrCodeUrl}
                                                                    alt="QR Code TOTP"
                                                                    width={220}
                                                                    height={220}
                                                                />
                                                            </div>
                                                        )}
                                                        <Input value={totpSecret} readOnly placeholder="Chave manual TOTP" />
                                                        {//<Input value={totpUri} readOnly placeholder="URI otpauth://" />
                                                        }
                                                        <Input
                                                            value={totpCode}
                                                            onChange={(e) => setTotpCode(e.target.value.replace(/\D/g, "").slice(0, 6))}
                                                            placeholder="Código TOTP (6 dígitos)"
                                                            inputMode="numeric"
                                                            maxLength={6}
                                                        />
                                                        <Button
                                                            type="button"
                                                            onClick={handleEnableTotp}
                                                            disabled={totpCode.length !== 6}
                                                        >
                                                            Habilitar TOTP
                                                        </Button>
                                                    </>
                                                )}
                                            </div>
                                        )}

                                        {totpEnabled && (
                                            <div className="space-y-2">
                                                <p className="text-sm text-green-700 dark:text-green-400">TOTP habilitado.</p>
                                                <Input
                                                    value={totpCode}
                                                    onChange={(e) => setTotpCode(e.target.value.replace(/\D/g, "").slice(0, 6))}
                                                    placeholder="Informe o código para desabilitar"
                                                    inputMode="numeric"
                                                    maxLength={6}
                                                />
                                                <Button
                                                    type="button"
                                                    variant="destructive"
                                                    onClick={handleDisableTotp}
                                                    disabled={totpCode.length !== 6}
                                                >
                                                    Desabilitar TOTP
                                                </Button>
                                            </div>
                                        )}
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
