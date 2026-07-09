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
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from "@/components/ui/dialog"
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert"
import { AlertCircle, AlertTriangle, CheckCircle2, Loader2, MessageCircleMore, Sparkles, Trash2 } from "lucide-react"
import { UpgradePlanModal } from "@/components/upgrade-plan-modal"
import { PasswordRequirements } from "@/components/PasswordRequirements"
import {
    getBrowserPushServerConfiguration,
    getCurrentBrowserPushSubscription,
    isBrowserPushSupported,
    subscribeCurrentBrowserToPush,
    syncCurrentBrowserPushSubscription,
    unsubscribeCurrentBrowserFromPush,
} from "@/utils/browserPush"
import { formatCpf } from "@/utils/cpf"
import { persistSessionUser } from "@/utils/userSession"
import { getPasswordValidationMessage } from "@/utils/passwordPolicy"
import { appPath } from "@/utils/appPath"

const frontendBaseUrl = (import.meta.env.VITE_FRONTEND_URL || "").replace(/\/+$/, "")
const buildFrontendAssetUrl = (relativePath) =>
    frontendBaseUrl ? `${frontendBaseUrl}/${relativePath.replace(/^\/+/, "")}` : `/${relativePath.replace(/^\/+/, "")}`

const telegramChatIdSteps = [
    {
        title: "Abra seu Telegram e busque pelo app RendaTop",
        description: "Abra o aplicativo do Telegram no celular ou no computador. Na busca do Telegram, procure por 'rendatop_bot', abra a conversa com o bot e clique no botão Iniciar/Start.",
        imageAlt: "Passo 1 para obter o chatID no Telegram",
        imageSrc: buildFrontendAssetUrl("chatid1.png")
    },
    {
        title: "Copie seu chatID",
        description: "O bot responderá com o seu chatID. Copie esse número e cole no campo Chat ID do Telegram.",
        imageAlt: "Passo 4 para obter o chatID no Telegram",
        imageSrc: buildFrontendAssetUrl("chatid2.png")
    },
]

const UserSettings = () => {
    const [loading, setLoading] = useState(true)
    const [saving, setSaving] = useState(false)
    const [testingTelegram, setTestingTelegram] = useState(false)
    const [testingWhatsApp, setTestingWhatsApp] = useState(false)
    const [testingEmail, setTestingEmail] = useState(false)
    const [verifyingPendingEmail, setVerifyingPendingEmail] = useState(false)
    const [resendingPendingEmail, setResendingPendingEmail] = useState(false)
    const [cancelingPendingEmail, setCancelingPendingEmail] = useState(false)
    const [telegramGuideOpen, setTelegramGuideOpen] = useState(false)
    const [upgradePrompt, setUpgradePrompt] = useState({ open: false, message: "" })
    const [success, setSuccess] = useState("")
    const [error, setError] = useState("")
    const [whatsAppError, setWhatsAppError] = useState("")
    const [telegramError, setTelegramError] = useState("")
    const [emailError, setEmailError] = useState("")
    const [pendingEmailError, setPendingEmailError] = useState("")
    const [browserError, setBrowserError] = useState("")
    const [deleteAccountError, setDeleteAccountError] = useState("")
    const [deleteAccountStepOneOpen, setDeleteAccountStepOneOpen] = useState(false)
    const [deleteAccountStepTwoOpen, setDeleteAccountStepTwoOpen] = useState(false)
    const [deleteAccountConfirmationText, setDeleteAccountConfirmationText] = useState("")
    const [deletingAccount, setDeletingAccount] = useState(false)

    const [name, setName] = useState("")
    const [email, setEmail] = useState("")
    const [currentEmail, setCurrentEmail] = useState("")
    const [pendingEmail, setPendingEmail] = useState("")
    const [emailVerificationCode, setEmailVerificationCode] = useState("")
    const [phone, setPhone] = useState("")
    const [telegramChatId, setTelegramChatId] = useState("")
    const [cpf, setCpf] = useState("")
    const [password, setPassword] = useState("")
    const [confirmPassword, setConfirmPassword] = useState("")
    const [notifyWhatsapp, setNotifyWhatsapp] = useState(false)
    const [notifyTelegram, setNotifyTelegram] = useState(true)
    const [notifyEmail, setNotifyEmail] = useState(true)
    const [notifyBrowser, setNotifyBrowser] = useState(false)
    const [calendarPublicEnabled, setCalendarPublicEnabled] = useState(false)
    const [calendarPublicUrl, setCalendarPublicUrl] = useState("")
    const [whatsappNotificationsEnabled, setWhatsappNotificationsEnabled] = useState(false)
    const [calendarIcsEnabled, setCalendarIcsEnabled] = useState(false)
    const [isAdminUser, setIsAdminUser] = useState(false)
    const [browserPushSupported, setBrowserPushSupported] = useState(false)
    const [browserPushAvailable, setBrowserPushAvailable] = useState(false)
    const [syncingBrowserPush, setSyncingBrowserPush] = useState(false)
    const [totpEnabled, setTotpEnabled] = useState(false)
    const [totpSecret, setTotpSecret] = useState("")
    const [totpUri, setTotpUri] = useState("")
    const [totpCode, setTotpCode] = useState("")
    const totpQrCodeUrl = totpUri
        ? `https://api.qrserver.com/v1/create-qr-code/?size=220x220&data=${encodeURIComponent(totpUri)}`
        : ""

    useEffect(() => {
        const browserPushIsSupported = isBrowserPushSupported()
        setBrowserPushSupported(browserPushIsSupported)

        axiosInstance
            .get("/User/Settings")
            .then((response) => {
                const data = response.data
                const canUseWhatsAppNotifications = Boolean(data.whatsapp_notifications_enabled)
                const canUseCalendarIcs = Boolean(data.calendar_ics_enabled)
                const confirmedEmail = data.email || ""
                const nextPendingEmail = data.pending_email || ""

                setName(data.name || "")
                setCurrentEmail(confirmedEmail)
                setPendingEmail(nextPendingEmail)
                setEmail(nextPendingEmail || confirmedEmail)
                setPhone(data.phone || "")
                setTelegramChatId(data.telegram_chat_id || "")
                setCpf(data.cpf || "")
                setWhatsappNotificationsEnabled(canUseWhatsAppNotifications)
                setCalendarIcsEnabled(canUseCalendarIcs)
                setIsAdminUser(String(data.user_type || "").trim().toLowerCase() === "admin")
                setNotifyWhatsapp(canUseWhatsAppNotifications && Boolean(data.notify_whatsapp))
                setNotifyTelegram(Boolean(data.notify_telegram))
                setNotifyEmail(Boolean(data.notify_email))
                setNotifyBrowser(Boolean(data.notify_browser))
                setCalendarPublicEnabled(canUseCalendarIcs && Boolean(data.calendar_public_enabled))
                setCalendarPublicUrl(canUseCalendarIcs ? data.calendar_public_url || "" : "")
                setTotpEnabled(Boolean(data.totp_enabled))
                persistSessionUser({
                    name: data.name,
                    email: confirmedEmail,
                    user_type: data.user_type,
                })
            })
            .catch(() => {
                setError("Não foi possível carregar suas configurações.")
            })
            .finally(() => {
                setLoading(false)
            })

        if (!browserPushIsSupported) {
            return
        }

        getBrowserPushServerConfiguration()
            .then(async (data) => {
                const enabled = Boolean(data?.enabled && data?.public_key)
                setBrowserPushAvailable(enabled)

                if (!enabled) {
                    setNotifyBrowser(false)
                    return
                }

                const subscription = await getCurrentBrowserPushSubscription()
                if (!subscription) {
                    setNotifyBrowser(false)
                    return
                }

                setNotifyBrowser(true)
                await syncCurrentBrowserPushSubscription()
            })
            .catch(() => {
                setBrowserPushAvailable(false)
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
        setPendingEmailError("")
        setBrowserError("")
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

        if (normalized.includes("navegador") || normalized.includes("browser")) {
            setBrowserError(text)
            setError("")
            return
        }

        setError(text)
    }

    const handleToggleWhatsApp = (checked) => {
        if (!whatsappNotificationsEnabled) {
            if (checked) {
                setNotifyWhatsapp(false)
                setWhatsAppError("")
                setError("")
                setUpgradePrompt({
                    open: true,
                    message: "Apenas usuarios de planos pagos podem ativar notificações por WhatsApp e acessar limites extendidos.",
                })
            }
            return
        }
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

    const handleToggleBrowser = async (checked) => {
        setError("")
        setSuccess("")
        setBrowserError("")
        setSyncingBrowserPush(true)

        try {
            if (!browserPushSupported) {
                throw new Error("Seu navegador não suporta notificações push.")
            }

            if (!browserPushAvailable) {
                throw new Error("Notificações no navegador ainda não estão configuradas no servidor.")
            }

            if (checked) {
                await subscribeCurrentBrowserToPush()
                setNotifyBrowser(true)
                setSuccess("Notificações do navegador habilitadas neste dispositivo.")
            } else {
                await unsubscribeCurrentBrowserFromPush()
                setNotifyBrowser(false)
                setSuccess("Notificações do navegador desabilitadas neste dispositivo.")
            }
        } catch (err) {
            const message = err instanceof Error
                ? err.message
                : "Não foi possível atualizar as notificações do navegador."
            setChannelAwareError(message)
        } finally {
            setSyncingBrowserPush(false)
        }
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

        const passwordError = password ? getPasswordValidationMessage(password) : ""
        if (passwordError) {
            setError(passwordError)
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
                const confirmedEmail = data.email || ""
                const nextPendingEmail = data.pending_email || ""
                const verificationSent = data.pending_email_verification_sent
                setName(data.name || "")
                setCurrentEmail(confirmedEmail)
                setPendingEmail(nextPendingEmail)
                setEmail(nextPendingEmail || confirmedEmail)
                persistSessionUser({
                    name: data.name,
                    email: confirmedEmail,
                    user_type: data.user_type,
                })
                setPhone(data.phone || "")
                setTelegramChatId(data.telegram_chat_id || "")
                setWhatsappNotificationsEnabled(Boolean(data.whatsapp_notifications_enabled))
                setCalendarIcsEnabled(Boolean(data.calendar_ics_enabled))
                setIsAdminUser(String(data.user_type || "").trim().toLowerCase() === "admin")
                setCalendarPublicEnabled(Boolean(data.calendar_public_enabled))
                setCalendarPublicUrl(data.calendar_public_url || "")
                setTotpEnabled(Boolean(data.totp_enabled))
                setPassword("")
                setConfirmPassword("")
                setEmailVerificationCode("")

                if (nextPendingEmail) {
                    if (verificationSent === false) {
                        setSuccess("Seus dados foram salvos, mas não conseguimos enviar o código agora. Tente reenviar abaixo.")
                    } else if (verificationSent === true) {
                        setSuccess("Enviamos um código para o novo email. Confirme-o abaixo para concluir a alteração.")
                    } else {
                        setSuccess("Suas configurações foram salvas. O novo email ainda precisa ser confirmado.")
                    }
                } else {
                    setSuccess("Configurações salvas com sucesso.")
                }
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

    const handleVerifyPendingEmail = () => {
        setError("")
        setSuccess("")
        setPendingEmailError("")
        setVerifyingPendingEmail(true)

        axiosInstance
            .post("/User/Settings/Email/Verify", { code: emailVerificationCode })
            .then((response) => {
                const data = response.data || {}
                const confirmedEmail = data.email || ""
                setCurrentEmail(confirmedEmail)
                setPendingEmail("")
                setEmail(confirmedEmail)
                setEmailVerificationCode("")
                persistSessionUser({
                    name: data.name,
                    email: confirmedEmail,
                    user_type: data.user_type,
                })
                setSuccess("Email atualizado e verificado com sucesso.")
            })
            .catch((err) => {
                const message = typeof err?.response?.data === "string"
                    ? err.response.data
                    : "Não foi possível verificar o novo email."
                setPendingEmailError(message)
            })
            .finally(() => {
                setVerifyingPendingEmail(false)
            })
    }

    const handleResendPendingEmail = () => {
        setError("")
        setSuccess("")
        setPendingEmailError("")
        setResendingPendingEmail(true)

        axiosInstance
            .post("/User/Settings/Email/Resend")
            .then((response) => {
                const message = response?.data?.message || "Novo código enviado para seu novo email."
                setSuccess(message)
            })
            .catch((err) => {
                const message = typeof err?.response?.data === "string"
                    ? err.response.data
                    : "Não foi possível reenviar o código."
                setPendingEmailError(message)
            })
            .finally(() => {
                setResendingPendingEmail(false)
            })
    }

    const handleCancelPendingEmail = () => {
        setError("")
        setSuccess("")
        setPendingEmailError("")
        setCancelingPendingEmail(true)

        axiosInstance
            .post("/User/Settings/Email/Cancel")
            .then((response) => {
                const data = response.data || {}
                const confirmedEmail = data.email || currentEmail
                setCurrentEmail(confirmedEmail)
                setPendingEmail("")
                setEmail(confirmedEmail)
                setEmailVerificationCode("")
                setSuccess("Alteração de email cancelada.")
            })
            .catch((err) => {
                const message = typeof err?.response?.data === "string"
                    ? err.response.data
                    : "Não foi possível cancelar a alteração de email."
                setPendingEmailError(message)
            })
            .finally(() => {
                setCancelingPendingEmail(false)
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

    const openDeleteAccountFlow = () => {
        setDeleteAccountError("")
        setDeleteAccountConfirmationText("")
        setDeleteAccountStepOneOpen(true)
    }

    const handleDeleteAccount = () => {
        setDeleteAccountError("")
        setSuccess("Sua conta está sendo deletada. Depois da confirmação final, seus dados não poderão ser recuperados.")
        setDeletingAccount(true)

        axiosInstance
            .delete("/User/Settings/DeleteAccount", {
                data: {
                    confirm_first_step: true,
                    confirm_second_step: true,
                    confirmation_text: deleteAccountConfirmationText,
                },
            })
            .then(() => {
                sessionStorage.clear()
                window.location.href = appPath("/login")
            })
            .catch((err) => {
                const message = typeof err?.response?.data === "string"
                    ? err.response.data
                    : "Não foi possível excluir sua conta."
                setDeleteAccountError(message)
                setSuccess("")
            })
            .finally(() => {
                setDeletingAccount(false)
            })
    }

    return (
        <>
            <Logged />
            <BaseLayout title="Configurações" description="Atualize seus dados e preferências de notificação">
                <div className="px-4 lg:px-6 space-y-6">
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
                                        {pendingEmail ? (
                                            <p className="text-xs text-muted-foreground">
                                                Seu email atual continua sendo <strong>{currentEmail}</strong> até você confirmar o código enviado para <strong>{pendingEmail}</strong>.
                                            </p>
                                        ) : (
                                            <p className="text-xs text-muted-foreground">
                                                
                                            </p>
                                        )}
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

                                    <PasswordRequirements
                                        password={password}
                                        confirmPassword={confirmPassword}
                                        visible={password.length > 0 || confirmPassword.length > 0}
                                    />

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
                                                />
                                            </div>
                                            {isAdminUser ? (
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
                                            ) : (
                                                <div className="hidden md:block" />
                                            )}
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
                                                <Button
                                                    type="button"
                                                    variant="secondary"
                                                    className="mt-3 inline-flex h-auto items-center gap-2 rounded-full border border-primary/20 bg-primary/10 px-4 py-2 text-sm font-semibold text-primary shadow-sm transition hover:bg-primary/15"
                                                    onClick={() => setTelegramGuideOpen(true)}
                                                >
                                                    <Sparkles className="h-4 w-4" />
                                                    <MessageCircleMore className="h-4 w-4" />
                                                    Veja como obter seu chatID no Telegram
                                                </Button>
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
                                            {isAdminUser ? (
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
                                            ) : (
                                                <div className="hidden md:block" />
                                            )}
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
                                            {isAdminUser ? (
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
                                            ) : (
                                                <div className="hidden md:block" />
                                            )}
                                        </div>

                                        {/* <div className="grid gap-3 rounded-md border p-3 md:grid-cols-[minmax(0,1fr)_auto_auto] md:items-center">
                                            <div>
                                                <p className="text-sm font-medium">Navegador</p>
                                                <p className="text-xs text-muted-foreground">
                                                    {browserPushSupported
                                                        ? browserPushAvailable
                                                            ? "Receber notificações neste navegador, mesmo com o site fechado."
                                                            : "O servidor ainda não foi configurado para Web Push."
                                                        : "Seu navegador atual não suporta notificações push."}
                                                </p>
                                                {browserError && (
                                                    <Alert variant="destructive" className="mt-3">
                                                        <AlertCircle className="h-4 w-4" />
                                                        <AlertTitle>Navegador</AlertTitle>
                                                        <AlertDescription>{browserError}</AlertDescription>
                                                    </Alert>
                                                )}
                                                <p className="mt-3 text-xs text-muted-foreground">
                                                    Ao ativar, o navegador solicitará permissão para exibir notificações. Você pode revogar isso depois nas configurações do próprio navegador.
                                                </p>
                                            </div>
                                            <div className="md:justify-self-center">
                                                <Switch
                                                    checked={notifyBrowser}
                                                    onCheckedChange={handleToggleBrowser}
                                                    disabled={!browserPushSupported || !browserPushAvailable || syncingBrowserPush}
                                                />
                                            </div>
                                            <div className="md:w-36 md:justify-self-end text-xs text-muted-foreground">
                                                {syncingBrowserPush
                                                    ? "Sincronizando..."
                                                    : notifyBrowser
                                                        ? "Ativo neste navegador"
                                                        : "Desativado"}
                                            </div>
                                        </div> */}
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

                                    {pendingEmail && (
                                        <div className="space-y-3 rounded-md border border-primary/20 bg-primary/5 p-4">
                                            <div className="space-y-1">
                                                <p className="text-sm font-medium">Verificar novo email</p>
                                                <p className="text-xs text-muted-foreground">
                                                    Para concluir a alteração que você salvou, informe o código enviado para <strong>{pendingEmail}</strong> ou cancele a solicitação abaixo.
                                                </p>
                                            </div>
                                            {pendingEmailError && (
                                                <Alert variant="destructive">
                                                    <AlertCircle className="h-4 w-4" />
                                                    <AlertTitle>Verificação de email</AlertTitle>
                                                    <AlertDescription>{pendingEmailError}</AlertDescription>
                                                </Alert>
                                            )}
                                            <div className="grid gap-3 lg:grid-cols-[minmax(0,1fr)_auto] lg:items-end">
                                                <div className="space-y-2">
                                                    <Label htmlFor="emailVerificationCode">Código de verificação</Label>
                                                    <Input
                                                        id="emailVerificationCode"
                                                        type="text"
                                                        value={emailVerificationCode}
                                                        onChange={(e) => setEmailVerificationCode(e.target.value)}
                                                        placeholder="Informe o código recebido"
                                                        inputMode="numeric"
                                                    />
                                                </div>
                                                <div className="flex flex-col gap-2 sm:flex-row lg:flex-wrap lg:justify-end">
                                                    <Button
                                                        type="button"
                                                        variant="outline"
                                                        onClick={handleVerifyPendingEmail}
                                                        disabled={verifyingPendingEmail || resendingPendingEmail || cancelingPendingEmail || !emailVerificationCode.trim()}
                                                    >
                                                        {verifyingPendingEmail ? "Verificando..." : "Confirmar"}
                                                    </Button>
                                                    <Button
                                                        type="button"
                                                        variant="secondary"
                                                        onClick={handleResendPendingEmail}
                                                        disabled={verifyingPendingEmail || resendingPendingEmail || cancelingPendingEmail}
                                                    >
                                                        {resendingPendingEmail ? "Reenviando..." : "Reenviar código"}
                                                    </Button>
                                                    <Button
                                                        type="button"
                                                        variant="ghost"
                                                        onClick={handleCancelPendingEmail}
                                                        disabled={verifyingPendingEmail || resendingPendingEmail || cancelingPendingEmail}
                                                    >
                                                        {cancelingPendingEmail ? "Cancelando..." : "Cancelar alteração"}
                                                    </Button>
                                                </div>
                                            </div>
                                        </div>
                                    )}

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

                    <Card className="border-red-200 bg-red-50/70 dark:border-red-950 dark:bg-red-950/20">
                        <CardHeader>
                            <CardTitle className="flex items-center gap-2 text-red-700 dark:text-red-300">
                                <AlertTriangle className="h-5 w-5" />
                                Zona de perigo
                            </CardTitle>
                            <CardDescription className="text-red-700/80 dark:text-red-300/80">
                                Exclua sua própria conta de forma permanente. Seus dados serão deletados do banco de dados e não poderão ser recuperados depois.
                            </CardDescription>
                        </CardHeader>
                        <CardContent className="space-y-4">
                            <div className="rounded-md border border-red-200 bg-white/70 p-4 text-sm text-red-800 dark:border-red-900 dark:bg-red-950/20 dark:text-red-200">
                                Se você excluir sua conta, seu acesso será encerrado, seus dados deixarão de existir no sistema e cobranças futuras vinculadas ao seu plano não continuarão no RendaTop. Esta ação é irreversível.
                            </div>

                            {deleteAccountError && (
                                <Alert variant="destructive">
                                    <AlertCircle className="h-4 w-4" />
                                    <AlertTitle>Exclusão de conta</AlertTitle>
                                    <AlertDescription>{deleteAccountError}</AlertDescription>
                                </Alert>
                            )}

                            <div className="flex justify-end">
                                <Button
                                    type="button"
                                    variant="destructive"
                                    className="gap-2"
                                    onClick={openDeleteAccountFlow}
                                    disabled={deletingAccount}
                                >
                                    <Trash2 className="h-4 w-4" />
                                    Excluir minha conta
                                </Button>
                            </div>
                        </CardContent>
                    </Card>
                </div>
            </BaseLayout>

            <Dialog
                open={deleteAccountStepOneOpen}
                onOpenChange={(open) => {
                    if (deletingAccount) return
                    setDeleteAccountStepOneOpen(open)
                }}
            >
                <DialogContent className="sm:max-w-lg">
                    <DialogHeader>
                        <DialogTitle className="flex items-center gap-2 text-red-700 dark:text-red-300">
                            <AlertTriangle className="h-5 w-5" />
                            Confirmar exclusão da conta
                        </DialogTitle>
                        <DialogDescription>
                            Sua conta está sendo preparada para exclusão permanente. Se você continuar, seus dados serão deletados do banco de dados e não poderão ser recuperados depois.
                        </DialogDescription>
                    </DialogHeader>

                    <div className="rounded-md border border-red-200 bg-red-50 p-4 text-sm text-red-800 dark:border-red-900 dark:bg-red-950/20 dark:text-red-200">
                        Isso inclui acesso à conta, investimentos, carteiras, cofrinhos, preferências, notificações, integrações e demais dados vinculados ao seu usuário no RendaTop.
                    </div>

                    <div className="flex justify-end gap-2">
                        <Button
                            type="button"
                            variant="outline"
                            onClick={() => setDeleteAccountStepOneOpen(false)}
                            disabled={deletingAccount}
                        >
                            Cancelar
                        </Button>
                        <Button
                            type="button"
                            variant="destructive"
                            onClick={() => {
                                setDeleteAccountStepOneOpen(false)
                                setDeleteAccountStepTwoOpen(true)
                            }}
                            disabled={deletingAccount}
                        >
                            Continuar
                        </Button>
                    </div>
                </DialogContent>
            </Dialog>

            <Dialog
                open={deleteAccountStepTwoOpen}
                onOpenChange={(open) => {
                    if (deletingAccount) return
                    setDeleteAccountStepTwoOpen(open)
                }}
            >
                <DialogContent className="sm:max-w-lg">
                    <DialogHeader>
                        <DialogTitle className="flex items-center gap-2 text-red-700 dark:text-red-300">
                            <Trash2 className="h-5 w-5" />
                            Confirmação final
                        </DialogTitle>
                        <DialogDescription>
                            Para confirmar a exclusão permanente, digite <strong>EXCLUIR</strong> no campo abaixo. Após isso, a conta será deletada e não poderá ser restaurada.
                        </DialogDescription>
                    </DialogHeader>

                    <div className="space-y-2">
                        <Label htmlFor="deleteAccountConfirmationText">Digite EXCLUIR</Label>
                        <Input
                            id="deleteAccountConfirmationText"
                            value={deleteAccountConfirmationText}
                            onChange={(event) => setDeleteAccountConfirmationText(event.target.value)}
                            placeholder="EXCLUIR"
                            disabled={deletingAccount}
                        />
                    </div>

                    {deleteAccountError && (
                        <Alert variant="destructive">
                            <AlertCircle className="h-4 w-4" />
                            <AlertTitle>Exclusão de conta</AlertTitle>
                            <AlertDescription>{deleteAccountError}</AlertDescription>
                        </Alert>
                    )}

                    <div className="flex justify-end gap-2">
                        <Button
                            type="button"
                            variant="outline"
                            onClick={() => setDeleteAccountStepTwoOpen(false)}
                            disabled={deletingAccount}
                        >
                            Voltar
                        </Button>
                        <Button
                            type="button"
                            variant="destructive"
                            className="gap-2"
                            onClick={handleDeleteAccount}
                            disabled={deletingAccount || deleteAccountConfirmationText.trim().toUpperCase() !== "EXCLUIR"}
                        >
                            {deletingAccount ? (
                                <>
                                    <Loader2 className="h-4 w-4 animate-spin" />
                                    Excluindo conta...
                                </>
                            ) : (
                                <>
                                    <Trash2 className="h-4 w-4" />
                                    Excluir permanentemente
                                </>
                            )}
                        </Button>
                    </div>
                </DialogContent>
            </Dialog>

            <Dialog open={telegramGuideOpen} onOpenChange={setTelegramGuideOpen}>
                <DialogContent className="w-[95vw] sm:max-w-3xl max-h-[90vh] overflow-y-auto">
                    <DialogHeader>
                        <DialogTitle>Como obter seu chatID no Telegram</DialogTitle>
                        <DialogDescription>
                            Siga estes passos no Telegram. Depois copie o chatID informado pelo bot e cole no campo do perfil.
                        </DialogDescription>
                    </DialogHeader>

                    <div className="space-y-4">
                        {telegramChatIdSteps.map((step, index) => (
                            <div key={step.title} className="rounded-lg border p-4 space-y-3">
                                <div className="flex items-center gap-3">
                                    <div className="flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-primary/10 text-sm font-semibold text-primary">
                                        {index + 1}
                                    </div>
                                    <div>
                                        <h4 className="text-sm font-medium">{step.title}</h4>
                                        <p className="text-sm text-muted-foreground">{step.description}</p>
                                    </div>
                                </div>

                                <div className="overflow-hidden rounded-md border border-dashed bg-muted/30">
                                    <img
                                        src={step.imageSrc}
                                        alt={step.imageAlt}
                                        className="h-auto w-full object-contain"
                                    />
                                </div>
                            </div>
                        ))}
                    </div>
                </DialogContent>
            </Dialog>

            <UpgradePlanModal
                open={upgradePrompt.open}
                onOpenChange={(open) => setUpgradePrompt((current) => ({ ...current, open }))}
                message={upgradePrompt.message}
            />
        </>
    )
}

export default UserSettings
