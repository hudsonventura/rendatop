export const BLOG_SCOPE_ALL = "all"
export const BLOG_SCOPE_DRAFT = "draft"
export const BLOG_SCOPE_PUBLISHED = "published"

export const BLOG_SCOPE_OPTIONS = [
    { value: BLOG_SCOPE_ALL, label: "Todos" },
    { value: BLOG_SCOPE_DRAFT, label: "Rascunhos" },
    { value: BLOG_SCOPE_PUBLISHED, label: "Publicados" },
]

export const BLOG_STATUS_LABELS = {
    Draft: "Rascunho",
    Published: "Publicado",
}

export const BLOG_SOCIAL_CHANNEL_LABELS = {
    Facebook: "Facebook",
    Instagram: "Instagram",
    LinkedIn: "LinkedIn",
}

export const BLOG_SOCIAL_STATUS_LABELS = {
    Pending: "Pendente",
    Published: "Publicado",
    Failed: "Falhou",
}

export const BLOG_IMAGE_ACCEPT = "image/png,image/jpeg,image/jpg,image/webp,image/gif"

export function getBlogStatusLabel(status) {
    return BLOG_STATUS_LABELS[String(status || "")] || "Desconhecido"
}

export function getBlogStatusTone(status) {
    switch (String(status || "")) {
        case "Published":
            return "border-emerald-200 bg-emerald-100 text-emerald-700"
        case "Draft":
        default:
            return "border-slate-200 bg-slate-100 text-slate-700"
    }
}

export function getBlogSocialChannelLabel(channel) {
    return BLOG_SOCIAL_CHANNEL_LABELS[String(channel || "")] || String(channel || "")
}

export function getBlogSocialStatusLabel(status) {
    return BLOG_SOCIAL_STATUS_LABELS[String(status || "")] || "Desconhecido"
}

export function getBlogSocialStatusTone(status) {
    switch (String(status || "")) {
        case "Published":
            return "border-emerald-200 bg-emerald-100 text-emerald-700"
        case "Failed":
            return "border-rose-200 bg-rose-100 text-rose-700"
        case "Pending":
        default:
            return "border-amber-200 bg-amber-100 text-amber-700"
    }
}

export function formatBlogDateTime(value) {
    if (!value) return "Ainda não publicado"

    const date = new Date(value)
    if (Number.isNaN(date.getTime())) return "Data inválida"

    return new Intl.DateTimeFormat("pt-BR", {
        dateStyle: "short",
        timeStyle: "short",
    }).format(date)
}

