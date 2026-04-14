export const SUPPORT_STATUS = {
    AGUARDANDO_ATENDIMENTO: "AguardandoAtendimento",
    EM_ATENDIMENTO: "EmAtendimento",
    AGUARDANDO_RESPOSTA_USUARIO: "AguardandoRespostaUsuario",
    ENCERRADO: "Encerrado",
    CANCELADO: "Cancelado",
}

export const SUPPORT_SCOPE = {
    OPEN: "open",
    ARCHIVED: "archived",
    ALL: "all",
}

export const SUPPORT_STATUS_OPTIONS = [
    { value: SUPPORT_STATUS.AGUARDANDO_ATENDIMENTO, label: "Aguardando atendimento" },
    { value: SUPPORT_STATUS.EM_ATENDIMENTO, label: "Em atendimento" },
    { value: SUPPORT_STATUS.AGUARDANDO_RESPOSTA_USUARIO, label: "Aguardando resposta do usuário" },
    { value: SUPPORT_STATUS.ENCERRADO, label: "Encerrado" },
    { value: SUPPORT_STATUS.CANCELADO, label: "Cancelado" },
]

export const SUPPORT_SCOPE_OPTIONS = [
    { value: SUPPORT_SCOPE.OPEN, label: "Abertos" },
    { value: SUPPORT_SCOPE.ARCHIVED, label: "Arquivados" },
    { value: SUPPORT_SCOPE.ALL, label: "Todos" },
]

export const SUPPORT_ATTACHMENT_MAX_BYTES = 1024 * 1024

export const SUPPORT_ATTACHMENT_EXTENSIONS = [
    ".png",
    ".jpg",
    ".jpeg",
    ".webp",
    ".gif",
    ".pdf",
    ".doc",
    ".docx",
    ".xls",
    ".xlsx",
    ".ppt",
    ".pptx",
]

export const SUPPORT_ATTACHMENT_ACCEPT = SUPPORT_ATTACHMENT_EXTENSIONS.join(",")

export function getSupportStatusLabel(status) {
    return SUPPORT_STATUS_OPTIONS.find((item) => item.value === status)?.label || status || "-"
}



export function getSupportChangeSourceLabel(source) {
    if (source === "SystemOnCreate") return "Criação do chamado"
    if (source === "AdminManual") return "Alteração manual"
    if (source === "SystemOnUserReply") return "Retorno do usuário"
    return source || "-"
}

export function getSupportSenderTypeLabel(senderType) {
    if (String(senderType || "").toLowerCase() === "admin") return "Admin"
    return "Usuário"
}

export function getSupportStatusTone(status) {
    switch (status) {
        case SUPPORT_STATUS.AGUARDANDO_ATENDIMENTO:
            return "bg-amber-100 text-amber-900 border-amber-200"
        case SUPPORT_STATUS.EM_ATENDIMENTO:
            return "bg-sky-100 text-sky-900 border-sky-200"
        case SUPPORT_STATUS.AGUARDANDO_RESPOSTA_USUARIO:
            return "bg-violet-100 text-violet-900 border-violet-200"
        case SUPPORT_STATUS.ENCERRADO:
            return "bg-emerald-100 text-emerald-900 border-emerald-200"
        case SUPPORT_STATUS.CANCELADO:
            return "bg-zinc-200 text-zinc-900 border-zinc-300"
        default:
            return "bg-secondary text-secondary-foreground border-transparent"
    }
}

export function getSupportPendingTone(pendingFor) {
    if (pendingFor === "admin") return "bg-amber-50 text-amber-900 border-amber-200"
    if (pendingFor === "user") return "bg-violet-50 text-violet-900 border-violet-200"
    return "bg-zinc-100 text-zinc-700 border-zinc-200"
}

export function isSupportArchived(status) {
    return status === SUPPORT_STATUS.ENCERRADO || status === SUPPORT_STATUS.CANCELADO
}

export function isSupportAwaitingAdmin(status) {
    return status === SUPPORT_STATUS.AGUARDANDO_ATENDIMENTO || status === SUPPORT_STATUS.EM_ATENDIMENTO
}

export function isSupportAwaitingUser(status) {
    return status === SUPPORT_STATUS.AGUARDANDO_RESPOSTA_USUARIO
}

export function formatSupportDateTime(value) {
    if (!value) return "-"

    return new Date(value).toLocaleString("pt-BR", {
        day: "2-digit",
        month: "2-digit",
        year: "numeric",
        hour: "2-digit",
        minute: "2-digit",
    })
}

export function formatAttachmentSize(sizeBytes) {
    if (!Number.isFinite(sizeBytes)) return "-"

    if (sizeBytes >= 1024 * 1024) {
        return `${(sizeBytes / (1024 * 1024)).toFixed(2)} MB`
    }

    return `${Math.max(1, Math.round(sizeBytes / 1024))} KB`
}

export function isAllowedSupportFile(fileName) {
    const normalizedName = String(fileName || "").toLowerCase()
    return SUPPORT_ATTACHMENT_EXTENSIONS.some((extension) => normalizedName.endsWith(extension))
}
