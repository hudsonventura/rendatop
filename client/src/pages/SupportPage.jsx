import { useEffect, useRef, useState } from "react"
import { useNavigate } from "react-router-dom"
import { Group as PanelGroup, Panel, Separator as PanelResizeHandle } from "react-resizable-panels"
import {
    Archive,
    Clock3,
    Download,
    FileText,
    Inbox,
    LifeBuoy,
    Loader2,
    MailOpen,
    Paperclip,
    Plus,
    Search,
    Send,
    ShieldCheck,
    SquarePen,
    UserRound,
    X,
} from "lucide-react"
import Logged from "@/components/Logged"
import SupportRichTextEditor from "@/components/support/SupportRichTextEditor"
import { BaseLayout } from "@/components/layouts/base-layout"
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from "@/components/ui/dialog"
import { Input } from "@/components/ui/input"
import { ScrollArea } from "@/components/ui/scroll-area"
import {
    Select,
    SelectContent,
    SelectItem,
    SelectTrigger,
    SelectValue,
} from "@/components/ui/select"
import { Skeleton } from "@/components/ui/skeleton"
import axiosInstance from "@/utils/axiosConfig"
import { cn } from "@/lib/utils"
import { getStoredUserType, isAdminUserType, persistSessionUser } from "@/utils/userSession"
import {
    SUPPORT_ATTACHMENT_ACCEPT,
    SUPPORT_ATTACHMENT_MAX_BYTES,
    SUPPORT_SCOPE,
    SUPPORT_SCOPE_OPTIONS,
    SUPPORT_STATUS_OPTIONS,
    formatAttachmentSize,
    formatSupportDateTime,
    getSupportChangeSourceLabel,
    getSupportPendingTone,
    getSupportSenderTypeLabel,
    getSupportStatusLabel,
    getSupportStatusTone,
    isAllowedSupportFile,
} from "@/utils/support"

const STATUS_FILTER_ALL = "__all__"

function StatusBadge({ status }) {
    return (
        <span className={cn("inline-flex items-center rounded-full border px-2.5 py-1 text-xs font-medium", getSupportStatusTone(status))}>
            {getSupportStatusLabel(status)}
        </span>
    )
}


function SupportImagePreview({ attachment, onDownload, compact = false }) {
    const [previewUrl, setPreviewUrl] = useState("")
    const [failed, setFailed] = useState(false)

    useEffect(() => {
        let active = true
        let objectUrl = ""

        axiosInstance
            .get(`/support/attachments/${attachment.id}`, { responseType: "blob" })
            .then((response) => {
                if (!active) return
                objectUrl = URL.createObjectURL(response.data)
                setPreviewUrl(objectUrl)
            })
            .catch(() => {
                if (!active) return
                setFailed(true)
            })

        return () => {
            active = false
            if (objectUrl) {
                URL.revokeObjectURL(objectUrl)
            }
        }
    }, [attachment.id])

    return (
        <div className={cn("rounded-xl border bg-muted/20 p-3", compact && "p-2")}>
            <div className="mb-2 flex items-center justify-between gap-3">
                <div className="min-w-0">
                    <p className="truncate text-sm font-medium">{attachment.file_name}</p>
                    <p className="text-muted-foreground text-xs">{formatAttachmentSize(attachment.size_bytes)}</p>
                </div>
                <Button type="button" variant="outline" size="sm" onClick={() => onDownload(attachment)}>
                    <Download className="size-4" />
                </Button>
            </div>
            {previewUrl && !failed ? (
                <img
                    src={previewUrl}
                    alt={attachment.file_name}
                    className={cn("w-full rounded-lg border object-cover", compact ? "max-h-40" : "max-h-72")}
                />
            ) : (
                <div className="text-muted-foreground flex min-h-24 items-center justify-center rounded-lg border border-dashed text-xs">
                    {failed ? "Falha ao carregar preview" : "Carregando preview..."}
                </div>
            )}
        </div>
    )
}

function AttachmentCard({ attachment, onDownload }) {
    if (attachment.is_image) {
        return <SupportImagePreview attachment={attachment} onDownload={onDownload} />
    }

    return (
        <div className="flex items-center justify-between rounded-xl border bg-muted/20 p-3">
            <div className="flex min-w-0 items-center gap-3">
                <div className="rounded-lg bg-background p-2">
                    <FileText className="size-4" />
                </div>
                <div className="min-w-0">
                    <p className="truncate text-sm font-medium">{attachment.file_name}</p>
                    <p className="text-muted-foreground text-xs">
                        {attachment.content_type} · {formatAttachmentSize(attachment.size_bytes)}
                    </p>
                </div>
            </div>
            <Button type="button" variant="outline" size="sm" onClick={() => onDownload(attachment)}>
                <Download className="size-4" />
                Baixar
            </Button>
        </div>
    )
}

function DraftAttachmentList({ attachments, onRemove }) {
    if (!attachments.length) {
        return null
    }

    return (
        <div className="flex flex-wrap gap-2">
            {attachments.map((file, index) => (
                <div key={`${file.name}-${index}`} className="flex items-center gap-2 rounded-full border bg-muted px-3 py-1.5 text-xs">
                    <Paperclip className="size-3.5" />
                    <span className="max-w-52 truncate">{file.name}</span>
                    <span className="text-muted-foreground">{formatAttachmentSize(file.size)}</span>
                    <button
                        type="button"
                        className="text-muted-foreground hover:text-foreground"
                        onClick={() => onRemove(index)}
                    >
                        <X className="size-3.5" />
                    </button>
                </div>
            ))}
        </div>
    )
}

function Composer({
    title,
    description,
    html,
    onHtmlChange,
    attachments,
    onAppendFiles,
    onRemoveAttachment,
    onSubmit,
    submitLabel,
    busy,
    disabled,
    error,
    subject,
    onSubjectChange,
    subjectPlaceholder,
    onCancel,
}) {
    const fileInputRef = useRef(null)

    return (
        <Card className="gap-4">
            <CardHeader className="gap-2">
                <CardTitle>{title}</CardTitle>
                {description ? <CardDescription>{description}</CardDescription> : null}
            </CardHeader>
            <CardContent className="space-y-4">
                {onSubjectChange ? (
                    <Input
                        value={subject}
                        onChange={(event) => onSubjectChange(event.target.value)}
                        placeholder={subjectPlaceholder || "Assunto do chamado"}
                        maxLength={180}
                        disabled={disabled || busy}
                    />
                ) : null}

                <SupportRichTextEditor
                    value={html}
                    onChange={onHtmlChange}
                    onPasteFiles={onAppendFiles}
                    disabled={disabled || busy}
                    placeholder="Escreva sua mensagem com o máximo de contexto possível..."
                />

                <div className="flex flex-wrap items-center gap-3">
                    <input
                        ref={fileInputRef}
                        type="file"
                        multiple
                        accept={SUPPORT_ATTACHMENT_ACCEPT}
                        className="hidden"
                        onChange={(event) => {
                            const files = Array.from(event.target.files || [])
                            onAppendFiles(files)
                            event.target.value = ""
                        }}
                        disabled={disabled || busy}
                    />
                    <Button
                        type="button"
                        variant="outline"
                        disabled={disabled || busy}
                        onClick={() => fileInputRef.current?.click()}
                    >
                        <Paperclip className="size-4" />
                        Anexar arquivos
                    </Button>
                    <p className="text-muted-foreground text-xs">
                        Imagens, PDF e Office. Máximo de 1 MB por arquivo.
                    </p>
                </div>

                <DraftAttachmentList attachments={attachments} onRemove={onRemoveAttachment} />

                {error ? (
                    <Alert variant="destructive">
                        <AlertTitle>Não foi possível enviar</AlertTitle>
                        <AlertDescription>{error}</AlertDescription>
                    </Alert>
                ) : null}

                <div className="flex flex-wrap items-center justify-end gap-3">
                    {onCancel ? (
                        <Button type="button" variant="ghost" onClick={onCancel} disabled={busy}>
                            Cancelar
                        </Button>
                    ) : null}
                    <Button type="button" onClick={onSubmit} disabled={disabled || busy}>
                        {busy ? <Loader2 className="size-4 animate-spin" /> : <Send className="size-4" />}
                        {submitLabel}
                    </Button>
                </div>
            </CardContent>
        </Card>
    )
}

function EmptyThreadState({ isAdmin, onStartCreate }) {
    return (
        <Card className="h-full min-h-[30rem] justify-center">
            <CardContent className="flex flex-1 flex-col items-center justify-center gap-4 text-center">
                <div className="rounded-full border bg-muted p-4">
                    <MailOpen className="size-7" />
                </div>
                <div className="space-y-2">
                    <h3 className="text-lg font-semibold">Selecione um chamado</h3>
                    <p className="text-muted-foreground max-w-md text-sm">
                        {isAdmin
                            ? "Abra um chamado da fila para acompanhar o histórico, responder e atualizar o status."
                            : "Escolha um chamado para continuar a conversa com o time de atendimento."}
                    </p>
                </div>
                {!isAdmin ? (
                    <Button type="button" onClick={onStartCreate}>
                        <Plus className="size-4" />
                        Novo chamado
                    </Button>
                ) : null}
            </CardContent>
        </Card>
    )
}

function EmptyListState({ isAdmin, onStartCreate }) {
    return (
        <div className="px-6 py-10 text-center">
            <p className="font-medium">Nenhum chamado encontrado</p>
            <p className="text-muted-foreground mt-2 text-sm">
                {isAdmin
                    ? "Ajuste os filtros ou pesquise por outros termos."
                    : "Você ainda não possui chamados com esses filtros."}
            </p>
            {!isAdmin ? (
                <div className="mt-4">
                    <Button type="button" onClick={onStartCreate}>
                        <Plus className="size-4" />
                        Novo chamado
                    </Button>
                </div>
            ) : null}
        </div>
    )
}

function isSameIteration(message, historyItem) {
    if (!message || !historyItem) return false

    const messageTime = new Date(message.created_at).getTime()
    const historyTime = new Date(historyItem.created_at).getTime()
    const sameActor =
        String(message.sender_user_id || "") === String(historyItem.actor_user_id || "") &&
        String(message.sender_user_name || "").trim().toLowerCase() === String(historyItem.actor_user_name || "").trim().toLowerCase()

    return sameActor && Math.abs(messageTime - historyTime) <= 1000
}

function buildSupportTimeline(ticketDetail) {
    if (!ticketDetail) return []

    const remainingHistory = [...(ticketDetail.status_history || [])]
    const items = (ticketDetail.messages || []).map((message) => {
        const statusChanges = remainingHistory.filter((historyItem) => isSameIteration(message, historyItem))

        statusChanges.forEach((historyItem) => {
            const index = remainingHistory.findIndex((item) => item.id === historyItem.id)
            if (index >= 0) {
                remainingHistory.splice(index, 1)
            }
        })

        return {
            type: "message",
            id: `message-${message.id}`,
            created_at: message.created_at,
            message,
            statusChanges,
        }
    })

    remainingHistory.forEach((historyItem) => {
        items.push({
            type: "status",
            id: `status-${historyItem.id}`,
            created_at: historyItem.created_at,
            historyItem,
        })
    })

    return items.sort((left, right) => new Date(left.created_at).getTime() - new Date(right.created_at).getTime())
}

function getErrorMessage(error, fallbackMessage) {
    if (typeof error?.response?.data === "string" && error.response.data.trim()) {
        return error.response.data
    }

    if (typeof error?.message === "string" && error.message.trim()) {
        return error.message
    }

    return fallbackMessage
}

export default function SupportPage() {
    const navigate = useNavigate()
    const [userType, setUserType] = useState(() => getStoredUserType())
    const [loadingBootstrap, setLoadingBootstrap] = useState(true)
    const [listLoading, setListLoading] = useState(true)
    const [detailLoading, setDetailLoading] = useState(false)
    const [submittingCreate, setSubmittingCreate] = useState(false)
    const [submittingReply, setSubmittingReply] = useState(false)
    const [submittingStatus, setSubmittingStatus] = useState(false)
    const [bootstrapError, setBootstrapError] = useState("")
    const [listError, setListError] = useState("")
    const [actionError, setActionError] = useState("")
    const [scope, setScope] = useState(SUPPORT_SCOPE.OPEN)
    const [statusFilter, setStatusFilter] = useState(STATUS_FILTER_ALL)
    const [searchInput, setSearchInput] = useState("")
    const [appliedSearch, setAppliedSearch] = useState("")
    const [listData, setListData] = useState({
        items: [],
        counts: {
            open_count: 0,
            archived_count: 0,
            waiting_admin_count: 0,
            waiting_user_count: 0,
        },
    })
    const [selectedTicketId, setSelectedTicketId] = useState(null)
    const [ticketDetail, setTicketDetail] = useState(null)
    const [statusDraft, setStatusDraft] = useState("")
    const [isCreating, setIsCreating] = useState(false)
    const [createSubject, setCreateSubject] = useState("")
    const [createHtml, setCreateHtml] = useState("")
    const [createAttachments, setCreateAttachments] = useState([])
    const [createError, setCreateError] = useState("")
    const [replyHtml, setReplyHtml] = useState("")
    const [replyAttachments, setReplyAttachments] = useState([])
    const [replyError, setReplyError] = useState("")
    const [reloadToken, setReloadToken] = useState(0)

    const isAdmin = isAdminUserType(userType)
    const counts = listData.counts
    const mergedTimeline = buildSupportTimeline(ticketDetail)
    const scopeCards = [
        {
            value: SUPPORT_SCOPE.OPEN,
            label: "Abertos",
            icon: Inbox,
            count: counts.open_count,
        },
        {
            value: SUPPORT_SCOPE.ARCHIVED,
            label: "Arquivados",
            icon: Archive,
            count: counts.archived_count,
        },
        {
            value: SUPPORT_SCOPE.ALL,
            label: "Todos",
            icon: Clock3,
            count: counts.open_count + counts.archived_count,
        },
    ]

    useEffect(() => {
        const timeout = window.setTimeout(() => {
            setAppliedSearch(searchInput.trim())
        }, 300)

        return () => {
            window.clearTimeout(timeout)
        }
    }, [searchInput])

    useEffect(() => {
        let cancelled = false

        async function bootstrap() {
            setLoadingBootstrap(true)
            setBootstrapError("")

            const stored = getStoredUserType()
            if (stored) {
                if (!cancelled) {
                    setUserType(stored)
                    setLoadingBootstrap(false)
                }
                return
            }

            try {
                const response = await axiosInstance.get("/User/Settings")
                if (cancelled) return

                const data = response?.data || {}
                const nextUserType = data.user_type || ""

                persistSessionUser({
                    name: data.name,
                    email: data.email,
                    user_type: nextUserType,
                })

                setUserType(nextUserType)
            } catch (error) {
                if (cancelled) return
                if (error?.response?.status === 401) {
                    navigate("/login", { replace: true })
                    return
                }

                setBootstrapError("Não foi possível carregar os dados da sua sessão.")
            } finally {
                if (!cancelled) {
                    setLoadingBootstrap(false)
                }
            }
        }

        bootstrap()

        return () => {
            cancelled = true
        }
    }, [navigate])

    useEffect(() => {
        if (loadingBootstrap || bootstrapError) return

        let cancelled = false
        setListLoading(true)
        setListError("")

        const params = {
            scope,
            search: appliedSearch || undefined,
            status: statusFilter !== STATUS_FILTER_ALL ? statusFilter : undefined,
        }

        axiosInstance
            .get("/support/tickets", { params })
            .then((response) => {
                if (cancelled) return

                const nextData = response?.data || { items: [], counts: {} }
                const nextItems = nextData.items || []

                setListData({
                    items: nextItems,
                    counts: {
                        open_count: nextData.counts?.open_count || 0,
                        archived_count: nextData.counts?.archived_count || 0,
                        waiting_admin_count: nextData.counts?.waiting_admin_count || 0,
                        waiting_user_count: nextData.counts?.waiting_user_count || 0,
                    },
                })

                setSelectedTicketId((currentId) => {
                    if (isCreating) {
                        return currentId
                    }

                    if (currentId && nextItems.some((item) => item.id === currentId)) {
                        return currentId
                    }

                    return null
                })

                if (!nextItems.length && !isCreating) {
                    setTicketDetail(null)
                }
            })
            .catch((error) => {
                if (cancelled) return
                if (error?.response?.status === 401) {
                    navigate("/login", { replace: true })
                    return
                }
                setListError("Não foi possível carregar os chamados.")
            })
            .finally(() => {
                if (!cancelled) {
                    setListLoading(false)
                }
            })

        return () => {
            cancelled = true
        }
    }, [appliedSearch, bootstrapError, isCreating, loadingBootstrap, navigate, reloadToken, scope, statusFilter])

    useEffect(() => {
        if (!ticketDetail) return
        setStatusDraft(ticketDetail.status || "")
    }, [ticketDetail])

    useEffect(() => {
        if (isCreating || !selectedTicketId) {
            setDetailLoading(false)
            if (isCreating) {
                setTicketDetail(null)
            }
            return
        }

        let cancelled = false
        setDetailLoading(true)
        setActionError("")

        axiosInstance
            .get(`/support/tickets/${selectedTicketId}`)
            .then((response) => {
                if (cancelled) return
                setTicketDetail(response?.data || null)
            })
            .catch((error) => {
                if (cancelled) return
                if (error?.response?.status === 404) {
                    setTicketDetail(null)
                    setReloadToken((current) => current + 1)
                    return
                }
                setActionError("Não foi possível carregar os detalhes do chamado selecionado.")
            })
            .finally(() => {
                if (!cancelled) {
                    setDetailLoading(false)
                }
            })

        return () => {
            cancelled = true
        }
    }, [isCreating, selectedTicketId])

    const handleTicketSelection = (ticketId) => {
        setIsCreating(false)
        setActionError("")
        setSelectedTicketId(ticketId)
    }

    const closeTicketModal = () => {
        setSelectedTicketId(null)
        setTicketDetail(null)
        setActionError("")
        resetReplyComposer()
    }

    const startCreateTicket = () => {
        setIsCreating(true)
        setSelectedTicketId(null)
        setTicketDetail(null)
        setActionError("")
        resetCreateComposer()
    }

    const resetCreateComposer = () => {
        setCreateSubject("")
        setCreateHtml("")
        setCreateAttachments([])
        setCreateError("")
    }

    const resetReplyComposer = () => {
        setReplyHtml("")
        setReplyAttachments([])
        setReplyError("")
    }

    const appendFiles = (files, stateSetter, errorSetter) => {
        if (!files.length) return

        const validFiles = []

        for (const file of files) {
            if (!isAllowedSupportFile(file.name)) {
                errorSetter(`O arquivo ${file.name} não é permitido.`)
                continue
            }

            if (file.size > SUPPORT_ATTACHMENT_MAX_BYTES) {
                errorSetter(`O arquivo ${file.name} ultrapassa o limite de 1 MB.`)
                continue
            }

            validFiles.push(file)
        }

        if (validFiles.length) {
            errorSetter("")
            stateSetter((current) => [...current, ...validFiles])
        }
    }

    const removeDraftAttachment = (index, stateSetter) => {
        stateSetter((current) => current.filter((_, currentIndex) => currentIndex !== index))
    }

    const downloadAttachment = async (attachment) => {
        try {
            const response = await axiosInstance.get(`/support/attachments/${attachment.id}`, {
                responseType: "blob",
            })

            const objectUrl = URL.createObjectURL(response.data)
            const link = document.createElement("a")
            link.href = objectUrl
            link.download = attachment.file_name
            document.body.appendChild(link)
            link.click()
            link.remove()
            URL.revokeObjectURL(objectUrl)
        } catch {
            setActionError("Não foi possível baixar o anexo.")
        }
    }

    const handleCreateTicket = async () => {
        setCreateError("")
        setActionError("")
        setSubmittingCreate(true)

        try {
            const formData = new FormData()
            formData.append("subject", createSubject)
            formData.append("body_html", createHtml)
            createAttachments.forEach((file) => formData.append("attachments", file))

            const response = await axiosInstance.post("/support/tickets", formData, {
                headers: {
                    "Content-Type": "multipart/form-data",
                },
            })

            const detail = response?.data || null
            resetCreateComposer()
            setIsCreating(false)
            setScope(SUPPORT_SCOPE.OPEN)
            setTicketDetail(detail)
            setSelectedTicketId(detail?.id || null)
            setReloadToken((current) => current + 1)
        } catch (error) {
            setCreateError(getErrorMessage(error, "Não foi possível abrir o chamado."))
        } finally {
            setSubmittingCreate(false)
        }
    }

    const handleReply = async () => {
        if (!ticketDetail) return

        setReplyError("")
        setActionError("")
        setSubmittingReply(true)

        try {
            const formData = new FormData()
            formData.append("body_html", replyHtml)
            replyAttachments.forEach((file) => formData.append("attachments", file))

            const response = await axiosInstance.post(`/support/tickets/${ticketDetail.id}/messages`, formData, {
                headers: {
                    "Content-Type": "multipart/form-data",
                },
            })

            setTicketDetail(response?.data || null)
            resetReplyComposer()
            setReloadToken((current) => current + 1)
        } catch (error) {
            setReplyError(getErrorMessage(error, "Não foi possível enviar a resposta."))
        } finally {
            setSubmittingReply(false)
        }
    }

    const handleChangeStatus = async () => {
        if (!ticketDetail || !statusDraft || statusDraft === ticketDetail.status) return

        setSubmittingStatus(true)
        setActionError("")

        try {
            const response = await axiosInstance.post(`/support/tickets/${ticketDetail.id}/status`, {
                status: statusDraft,
            })

            const nextDetail = response?.data || null
            setTicketDetail(nextDetail)

            if (nextDetail?.is_archived && scope === SUPPORT_SCOPE.OPEN) {
                setScope(SUPPORT_SCOPE.ALL)
            }

            setReloadToken((current) => current + 1)
        } catch (error) {
            setActionError(getErrorMessage(error, "Não foi possível atualizar o status do chamado."))
        } finally {
            setSubmittingStatus(false)
        }
    }

    const renderFiltersPane = () => (
        <div className="space-y-4">
            <Card className="gap-4">
                <CardHeader className="gap-2">
                    <div className="flex items-center gap-2">
                        <LifeBuoy className="size-5" />
                        <CardTitle>Fila</CardTitle>
                    </div>
                    <CardDescription>
                        {isAdmin
                            ? "Acompanhe a fila compartilhada e mantenha o histórico das respostas."
                            : "Abra chamados, acompanhe o andamento e responda quando o time solicitar."}
                    </CardDescription>
                </CardHeader>
                <CardContent className="space-y-3">
                    {!isAdmin ? (
                        <Button
                            type="button"
                            className="w-full justify-start"
                            onClick={startCreateTicket}
                        >
                            <SquarePen className="size-4" />
                            Novo chamado
                        </Button>
                    ) : null}

                    {scopeCards.map((item) => (
                        <button
                            key={item.value}
                            type="button"
                            onClick={() => setScope(item.value)}
                            className={cn(
                                "flex w-full items-center justify-between rounded-xl border px-3 py-3 text-left transition-colors",
                                scope === item.value ? "border-primary bg-primary/5" : "hover:bg-muted/40"
                            )}
                        >
                            <div className="flex items-center gap-3">
                                <div className="rounded-lg bg-background p-2">
                                    <item.icon className="size-4" />
                                </div>
                                <div>
                                    <p className="text-sm font-medium">{item.label}</p>
                                    <p className="text-muted-foreground text-xs">
                                        {item.value === SUPPORT_SCOPE.OPEN && (isAdmin ? "Pendências e tickets ativos" : "Chamados em andamento")}
                                        {item.value === SUPPORT_SCOPE.ARCHIVED && "Encerrados e cancelados"}
                                        {item.value === SUPPORT_SCOPE.ALL && "Visão completa para pesquisa"}
                                    </p>
                                </div>
                            </div>
                            <Badge variant="secondary">{item.count}</Badge>
                        </button>
                    ))}
                </CardContent>
            </Card>
        </div>
    )

    const renderListPane = () => (
        <Card className="gap-4">
            <CardHeader className="gap-3">
                <div className="flex items-center gap-2">
                    <Inbox className="size-5" />
                    <CardTitle>Chamados</CardTitle>
                </div>

                <div className="grid gap-3">
                    <div className="flex flex-wrap items-center gap-3">
                        {!isAdmin ? (
                            <Button type="button" onClick={startCreateTicket}>
                                <SquarePen className="size-4" />
                                Novo chamado
                            </Button>
                        ) : null}

                        <Select value={scope} onValueChange={setScope}>
                            <SelectTrigger className="w-full sm:w-[12rem]">
                                <SelectValue placeholder="Escopo" />
                            </SelectTrigger>
                            <SelectContent>
                                {SUPPORT_SCOPE_OPTIONS.map((item) => (
                                    <SelectItem key={item.value} value={item.value}>
                                        {item.label}
                                    </SelectItem>
                                ))}
                            </SelectContent>
                        </Select>

                        <div className="relative min-w-[14rem] flex-1">
                            <Search className="text-muted-foreground absolute left-3 top-1/2 size-4 -translate-y-1/2" />
                            <Input
                                value={searchInput}
                                onChange={(event) => setSearchInput(event.target.value)}
                                className="pl-9"
                                placeholder={isAdmin ? "Buscar por assunto, texto, nome ou email" : "Buscar por assunto ou mensagem"}
                            />
                        </div>

                        <Select value={statusFilter} onValueChange={setStatusFilter}>
                            <SelectTrigger className="w-full sm:w-[14rem]">
                                <SelectValue placeholder="Todos os status" />
                            </SelectTrigger>
                            <SelectContent>
                                <SelectItem value={STATUS_FILTER_ALL}>Todos os status</SelectItem>
                                {SUPPORT_STATUS_OPTIONS.map((item) => (
                                    <SelectItem key={item.value} value={item.value}>
                                        {item.label}
                                    </SelectItem>
                                ))}
                            </SelectContent>
                        </Select>
                    </div>
                </div>
            </CardHeader>

            <CardContent className="px-0">
                {listLoading ? (
                    <div className="space-y-3 px-6">
                        <Skeleton className="h-24 rounded-xl" />
                        <Skeleton className="h-24 rounded-xl" />
                        <Skeleton className="h-24 rounded-xl" />
                    </div>
                ) : listError ? (
                    <div className="px-6">
                        <Alert variant="destructive">
                            <AlertTitle>Falha ao carregar</AlertTitle>
                            <AlertDescription>{listError}</AlertDescription>
                        </Alert>
                    </div>
                ) : listData.items.length === 0 ? (
                    <EmptyListState isAdmin={isAdmin} onStartCreate={startCreateTicket} />
                ) : (
                    <ScrollArea className="h-[30rem] lg:h-[70vh]">
                        <div className="space-y-2 px-3 pb-3">
                            {listData.items.map((ticket) => (
                                <button
                                    key={ticket.id}
                                    type="button"
                                    onClick={() => handleTicketSelection(ticket.id)}
                                    className={cn(
                                        "w-full rounded-2xl border p-4 text-left transition-colors",
                                        selectedTicketId === ticket.id && !isCreating
                                            ? "border-primary bg-primary/5"
                                            : "hover:bg-muted/40",
                                        ticket.is_archived && "opacity-70"
                                    )}
                                >
                                    <div className="mb-3 flex flex-wrap items-start justify-between gap-3">
                                        <div className="min-w-0 space-y-1">
                                            <p className="truncate font-semibold">{ticket.subject}</p>
                                            <p className="text-muted-foreground text-xs">
                                                {isAdmin ? `${ticket.requester_user_name} · ${ticket.requester_user_email}` : ticket.requester_user_email}
                                            </p>
                                        </div>
                                    </div>

                                    <div className="space-y-2">
                                        <p className="text-muted-foreground line-clamp-2 text-sm">
                                            {ticket.latest_message_preview || "Sem texto na última interação."}
                                        </p>
                                        <div className="text-muted-foreground flex flex-wrap items-center gap-3 text-xs">
                                            <span>{ticket.latest_sender_user_name || "Sem remetente"}</span>
                                            <span>{ticket.message_count} mensagens</span>
                                            <span>{formatSupportDateTime(ticket.last_message_at)}</span>
                                        </div>
                                    </div>
                                </button>
                            ))}
                        </div>
                    </ScrollArea>
                )}
            </CardContent>
        </Card>
    )

    const renderCreatePane = () => {
        if (isCreating) {
            return (
                <Composer
                    title="Novo chamado"
                    description="Descreva o problema em detalhes e anexe evidências se necessário."
                    subject={createSubject}
                    onSubjectChange={setCreateSubject}
                    html={createHtml}
                    onHtmlChange={setCreateHtml}
                    attachments={createAttachments}
                    onAppendFiles={(files) => appendFiles(files, setCreateAttachments, setCreateError)}
                    onRemoveAttachment={(index) => removeDraftAttachment(index, setCreateAttachments)}
                    onSubmit={handleCreateTicket}
                    submitLabel="Abrir chamado"
                    busy={submittingCreate}
                    error={createError}
                    onCancel={() => {
                        setIsCreating(false)
                        resetCreateComposer()
                        setSelectedTicketId(null)
                    }}
                />
            )
        }

        return null
    }

    const renderTicketDetail = () => {
        return (
            <div className="space-y-6">
                <div className="space-y-4 border-b pb-5">
                    <div className="flex flex-col gap-3 xl:flex-row xl:items-start xl:justify-between">
                        <div className="space-y-2">
                            <div className="flex flex-wrap items-center gap-2">
                                <h3 className="text-xl font-semibold">{ticketDetail.subject}</h3>
                                <StatusBadge status={ticketDetail.status} />
                            </div>
                            <p className="text-muted-foreground text-sm">
                                {ticketDetail.requester_user_name} · {ticketDetail.requester_user_email}
                            </p>
                            <div className="text-muted-foreground flex flex-wrap gap-3 text-xs">
                                <span>Criado em {formatSupportDateTime(ticketDetail.created_at)}</span>
                                <span>Última interação em {formatSupportDateTime(ticketDetail.last_message_at)}</span>
                                {ticketDetail.archived_at ? <span>Arquivado em {formatSupportDateTime(ticketDetail.archived_at)}</span> : null}
                            </div>
                        </div>

                        {ticketDetail.can_current_user_change_status ? (
                            <div className="flex flex-col gap-2 sm:flex-row sm:items-center">
                                <Select value={statusDraft} onValueChange={setStatusDraft}>
                                    <SelectTrigger className="w-full sm:w-[18rem]">
                                        <SelectValue placeholder="Alterar status" />
                                    </SelectTrigger>
                                    <SelectContent>
                                        {SUPPORT_STATUS_OPTIONS.map((item) => (
                                            <SelectItem key={item.value} value={item.value}>
                                                {item.label}
                                            </SelectItem>
                                        ))}
                                    </SelectContent>
                                </Select>
                                <Button
                                    type="button"
                                    variant="outline"
                                    disabled={submittingStatus || !statusDraft || statusDraft === ticketDetail.status}
                                    onClick={handleChangeStatus}
                                >
                                    {submittingStatus ? <Loader2 className="size-4 animate-spin" /> : null}
                                    Atualizar
                                </Button>
                            </div>
                        ) : null}
                    </div>

                    {actionError ? (
                        <Alert variant="destructive">
                            <AlertTitle>Algo deu errado</AlertTitle>
                            <AlertDescription>{actionError}</AlertDescription>
                        </Alert>
                    ) : null}
                </div>

                <section className="space-y-4">
                    <div className="flex items-center gap-2">
                        <MailOpen className="size-4" />
                        <h3 className="font-semibold">Conversa e status</h3>
                    </div>

                    <div className="space-y-4">
                        {mergedTimeline.map((item) => {
                            if (item.type === "status") {
                                const historyItem = item.historyItem

                                return (
                                    <div key={item.id} className="rounded-2xl border border-dashed bg-muted/20 p-4">
                                        <div className="flex flex-wrap items-center justify-between gap-2">
                                            <div className="space-y-1">
                                                <p className="text-sm font-medium">{historyItem.actor_user_name}</p>
                                                <p className="text-muted-foreground text-xs">{getSupportChangeSourceLabel(historyItem.source)}</p>
                                            </div>
                                            <p className="text-muted-foreground text-xs">{formatSupportDateTime(historyItem.created_at)}</p>
                                        </div>
                                        <div className="mt-3 flex flex-wrap items-center gap-2 text-sm">
                                            {historyItem.from_status ? <StatusBadge status={historyItem.from_status} /> : <Badge variant="outline">Inicial</Badge>}
                                            <span className="text-muted-foreground">→</span>
                                            <StatusBadge status={historyItem.to_status} />
                                        </div>
                                    </div>
                                )
                            }

                            const { message, statusChanges } = item

                            return (
                                <div key={item.id} className="rounded-2xl border p-4">
                                    <div className="mb-3 flex flex-wrap items-center justify-between gap-3">
                                        <div className="flex min-w-0 items-center gap-3">
                                            <div className="rounded-full bg-muted p-2">
                                                {String(message.sender_user_type).toLowerCase() === "admin" ? (
                                                    <ShieldCheck className="size-4" />
                                                ) : (
                                                    <UserRound className="size-4" />
                                                )}
                                            </div>
                                            <div className="min-w-0">
                                                <p className="truncate text-sm font-semibold">{message.sender_user_name}</p>
                                                <p className="text-muted-foreground text-xs">
                                                    {getSupportSenderTypeLabel(message.sender_user_type)}
                                                </p>
                                            </div>
                                        </div>
                                        <p className="text-muted-foreground text-xs">{formatSupportDateTime(message.created_at)}</p>
                                    </div>

                                    {statusChanges.length ? (
                                        <div className="mb-4 space-y-2 rounded-xl border bg-muted/20 p-3">
                                            {statusChanges.map((historyItem) => (
                                                <div key={historyItem.id} className="space-y-2">
                                                    <p className="text-muted-foreground text-xs">{getSupportChangeSourceLabel(historyItem.source)}</p>
                                                    <div className="flex flex-wrap items-center gap-2 text-sm">
                                                        {historyItem.from_status ? <StatusBadge status={historyItem.from_status} /> : <Badge variant="outline">Inicial</Badge>}
                                                        <span className="text-muted-foreground">→</span>
                                                        <StatusBadge status={historyItem.to_status} />
                                                    </div>
                                                </div>
                                            ))}
                                        </div>
                                    ) : null}

                                    <div
                                        className={cn(
                                            "text-sm leading-7",
                                            "[&_a]:text-primary [&_a]:underline [&_blockquote]:my-3 [&_blockquote]:border-l-4 [&_blockquote]:border-border [&_blockquote]:pl-4 [&_blockquote]:italic",
                                            "[&_ol]:my-3 [&_ol]:list-decimal [&_ol]:pl-6 [&_p]:mb-3 [&_p:last-child]:mb-0 [&_ul]:my-3 [&_ul]:list-disc [&_ul]:pl-6"
                                        )}
                                        dangerouslySetInnerHTML={{ __html: message.body_html || "<p></p>" }}
                                    />

                                    {message.attachments.length ? (
                                        <div className="mt-4 grid gap-3 xl:grid-cols-2 2xl:grid-cols-3">
                                            {message.attachments.map((attachment) => (
                                                <AttachmentCard
                                                    key={attachment.id}
                                                    attachment={attachment}
                                                    onDownload={downloadAttachment}
                                                />
                                            ))}
                                        </div>
                                    ) : null}
                                </div>
                            )
                        })}
                    </div>
                </section>

                {ticketDetail.can_current_user_reply ? (
                    <Composer
                        title="Responder chamado"
                        description={
                            isAdmin
                                ? "Sua resposta ficará registrada com seu nome para todos os admins."
                                : "Assim que você responder, o chamado volta automaticamente para a fila dos admins."
                        }
                        html={replyHtml}
                        onHtmlChange={setReplyHtml}
                        attachments={replyAttachments}
                        onAppendFiles={(files) => appendFiles(files, setReplyAttachments, setReplyError)}
                        onRemoveAttachment={(index) => removeDraftAttachment(index, setReplyAttachments)}
                        onSubmit={handleReply}
                        submitLabel="Enviar resposta"
                        busy={submittingReply}
                        error={replyError}
                    />
                ) : (
                    <Alert>
                        <AlertTitle>Resposta indisponível no momento</AlertTitle>
                        <AlertDescription>
                            {ticketDetail.is_archived
                                ? "Chamados encerrados ou cancelados ficam arquivados e não recebem novas mensagens."
                                : "Este chamado ainda não está aguardando sua resposta."}
                        </AlertDescription>
                    </Alert>
                )}
            </div>
        )
    }

    return (
        <>
            <Logged />
            <BaseLayout
                title="Atendimento"
                description="Abra chamados, acompanhe o histórico completo e mantenha a conversa registrada com o time de atendimento."
            >
                <div className="px-4 lg:px-6">
                    {bootstrapError ? (
                        <Alert variant="destructive">
                            <AlertTitle>Falha ao carregar</AlertTitle>
                            <AlertDescription>{bootstrapError}</AlertDescription>
                        </Alert>
                    ) : loadingBootstrap ? (
                        <Skeleton className="h-[32rem] rounded-xl lg:h-[72vh]" />
                    ) : (
                        <>
                            {renderCreatePane()}

                            {!isCreating ? (
                                <>
                                    <div className="space-y-6 lg:hidden">
                                        {isAdmin ? renderFiltersPane() : null}
                                        {renderListPane()}
                                    </div>

                                    <div className="hidden lg:block">
                                        {isAdmin ? (
                                            <PanelGroup direction="horizontal" className="min-h-[72vh] rounded-2xl border bg-background">
                                                <Panel defaultSize={24} minSize={18}>
                                                    <div className="h-full p-4">{renderFiltersPane()}</div>
                                                </Panel>
                                                <PanelResizeHandle className="w-px bg-border" />
                                                <Panel defaultSize={76} minSize={40}>
                                                    <div className="h-full border-l p-4">{renderListPane()}</div>
                                                </Panel>
                                            </PanelGroup>
                                        ) : (
                                            renderListPane()
                                        )}
                                    </div>
                                </>
                            ) : null}
                        </>
                    )}
                </div>
            </BaseLayout>

            <Dialog open={Boolean(selectedTicketId) && !isCreating} onOpenChange={(open) => {
                if (!open) {
                    closeTicketModal()
                }
            }}>
                <DialogContent className="max-h-[94vh] w-[96vw] max-w-[96vw] sm:max-w-[96vw] xl:max-w-[1500px] overflow-hidden p-0">
                    <DialogHeader className="border-b px-6 py-5">
                        <DialogTitle>Chamado</DialogTitle>
                        <DialogDescription>
                            Histórico completo, anexos e resposta centralizados em uma visualização ampla.
                        </DialogDescription>
                    </DialogHeader>

                    <div className="max-h-[calc(92vh-5.5rem)] overflow-hidden">
                        {detailLoading ? (
                            <div className="p-6">
                                <Skeleton className="h-[32rem] rounded-xl" />
                            </div>
                        ) : ticketDetail ? (
                            <ScrollArea className="h-[calc(94vh-5.5rem)]">
                                <div className="p-6 lg:p-8">{renderTicketDetail()}</div>
                            </ScrollArea>
                        ) : (
                            <div className="p-6">
                                <EmptyThreadState isAdmin={isAdmin} onStartCreate={startCreateTicket} />
                            </div>
                        )}
                    </div>
                </DialogContent>
            </Dialog>
        </>
    )
}
