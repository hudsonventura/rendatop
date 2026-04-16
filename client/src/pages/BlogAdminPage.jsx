import { useEffect, useMemo, useRef, useState } from "react"
import { useNavigate } from "react-router-dom"
import {
    CalendarDays,
    ExternalLink,
    FileImage,
    Loader2,
    PenSquare,
    Plus,
    Save,
    Send,
} from "lucide-react"
import Logged from "@/components/Logged"
import BlogRichTextEditor from "@/components/blog/BlogRichTextEditor"
import { BaseLayout } from "@/components/layouts/base-layout"
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Card, CardContent } from "@/components/ui/card"
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from "@/components/ui/dialog"
import { Input } from "@/components/ui/input"
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
import {
    BLOG_IMAGE_ACCEPT,
    BLOG_SCOPE_ALL,
    BLOG_SCOPE_OPTIONS,
    formatBlogDateTime,
    getBlogSocialChannelLabel,
    getBlogStatusLabel,
    getBlogStatusTone,
} from "@/utils/blog"
import { getStoredUserType, isAdminUserType } from "@/utils/userSession"

const SOCIAL_CHANNELS = ["Facebook", "Instagram", "LinkedIn"]

function BlogStatusBadge({ status }) {
    return (
        <span className={cn("inline-flex items-center rounded-full border px-2.5 py-1 text-xs font-medium", getBlogStatusTone(status))}>
            {getBlogStatusLabel(status)}
        </span>
    )
}

function getSocialButtonLabel(publication) {
    if (!publication) return "Publicar"
    if (publication.status === "Failed")
        return publication.published_at ? "Falhou, republicar" : "Falhou, tentar de novo"
    if (publication.status === "Published")
        return "Publicado, postar novamente"

    return "Publicar"
}

function createEmptyDraft() {
    return {
        id: null,
        slug: "",
        title: "",
        excerpt: "",
        body_html: "",
        status: "Draft",
        cover_image_data_url: "",
        cover_asset_id: null,
        cover_asset: null,
        social_publications: [],
        public_post_url: "",
        published_at: null,
        updated_at: null,
    }
}

function normalizeError(err, fallbackMessage) {
    return (
        err?.response?.data?.message ||
        err?.response?.data?.error ||
        err?.message ||
        fallbackMessage
    )
}

export default function BlogAdminPage() {
    const navigate = useNavigate()
    const coverFileRef = useRef(null)
    const [scope, setScope] = useState(BLOG_SCOPE_ALL)
    const [loadingList, setLoadingList] = useState(true)
    const [loadingDetail, setLoadingDetail] = useState(false)
    const [saving, setSaving] = useState(false)
    const [publishing, setPublishing] = useState(false)
    const [uploadingImages, setUploadingImages] = useState(false)
    const [listError, setListError] = useState("")
    const [editorError, setEditorError] = useState("")
    const [successMessage, setSuccessMessage] = useState("")
    const [posts, setPosts] = useState([])
    const [selectedId, setSelectedId] = useState(null)
    const [isNewPost, setIsNewPost] = useState(true)
    const [isEditorOpen, setIsEditorOpen] = useState(false)
    const [form, setForm] = useState(createEmptyDraft())

    const loadPosts = async () => {
        setLoadingList(true)
        setListError("")

        try {
            const response = await axiosInstance.get("/admin/blog/posts", {
                params: { scope },
            })

            setPosts(response?.data?.items || [])
        } catch (err) {
            if (err?.response?.status === 403) {
                navigate("/home", { replace: true })
                return
            }

            setListError("Não foi possível carregar as postagens do blog.")
        } finally {
            setLoadingList(false)
        }
    }

    const applyDetail = (detail) => {
        setForm({
            id: detail.id,
            slug: detail.slug,
            title: detail.title || "",
            excerpt: detail.excerpt || "",
            body_html: detail.body_html || "",
            status: detail.status || "Draft",
            cover_image_data_url:
                detail.cover_asset?.id === "00000000-0000-0000-0000-000000000000" || detail.cover_asset?.id === null
                    ? (detail.cover_asset?.url || "")
                    : "",
            cover_asset_id: detail.cover_asset_id || null,
            cover_asset: detail.cover_asset || null,
            social_publications: detail.social_publications || [],
            public_post_url: detail.public_post_url || "",
            published_at: detail.published_at || null,
            updated_at: detail.updated_at || null,
        })
    }

    const loadPost = async (id) => {
        if (!id) return

        setLoadingDetail(true)
        setEditorError("")
        setSuccessMessage("")

        try {
            const response = await axiosInstance.get(`/admin/blog/posts/${id}`)
            applyDetail(response?.data || createEmptyDraft())
            setSelectedId(id)
            setIsNewPost(false)
            setIsEditorOpen(true)
        } catch (err) {
            if (err?.response?.status === 403) {
                navigate("/home", { replace: true })
                return
            }

            setEditorError("Não foi possível carregar a postagem selecionada.")
        } finally {
            setLoadingDetail(false)
        }
    }

    useEffect(() => {
        const storedUserType = getStoredUserType()
        if (storedUserType && !isAdminUserType(storedUserType)) {
            navigate("/home", { replace: true })
            return
        }

        loadPosts()
    }, [navigate, scope])

    const socialPublications = useMemo(
        () => form.social_publications || [],
        [form.social_publications]
    )

    const isPublishedPost = form.status === "Published"
    const socialPublicationByChannel = useMemo(
        () => new Map(socialPublications.map((publication) => [publication.channel, publication])),
        [socialPublications]
    )

    const handleCreateNew = () => {
        setIsNewPost(true)
        setSelectedId(null)
        setEditorError("")
        setSuccessMessage("")
        setForm(createEmptyDraft())
        setIsEditorOpen(true)
    }

    const closeEditor = (open) => {
        setIsEditorOpen(open)
        if (!open) {
            setLoadingDetail(false)
        }
    }

    const readFileAsDataUrl = (file) =>
        new Promise((resolve, reject) => {
            const reader = new FileReader()
            reader.onload = () => resolve(String(reader.result || ""))
            reader.onerror = () => reject(reader.error || new Error("Falha ao ler imagem"))
            reader.readAsDataURL(file)
        })

    const handleCoverSelected = async (event) => {
        const file = event.target.files?.[0]
        event.target.value = ""

        if (!file) return

        try {
            const dataUrl = await readFileAsDataUrl(file)
            setForm((current) => ({
                ...current,
                cover_image_data_url: dataUrl,
                cover_asset_id: null,
                cover_asset: {
                    id: "00000000-0000-0000-0000-000000000000",
                    file_name: file.name,
                    content_type: file.type || "image/png",
                    size_bytes: file.size || 0,
                    alt_text: file.name.replace(/\.[^/.]+$/, ""),
                    url: dataUrl,
                    created_at: new Date().toISOString(),
                },
            }))
        } catch {
            setEditorError("Não foi possível ler a imagem de capa.")
        }
    }

    const buildPayload = () => ({
        title: form.title,
        excerpt: form.excerpt,
        body_html: form.body_html,
        cover_image_data_url: form.cover_image_data_url || null,
        cover_asset_id: form.cover_asset_id,
    })

    const syncPostList = (detail) => {
        setPosts((current) => {
            const nextSummary = {
                id: detail.id,
                slug: detail.slug,
                title: detail.title,
                excerpt: detail.excerpt,
                status: detail.status,
                author_user_name: detail.author_user_name,
                cover_asset: detail.cover_asset,
                social_publications: detail.social_publications,
                published_at: detail.published_at,
                updated_at: detail.updated_at,
                public_post_url: detail.public_post_url,
            }

            const exists = current.some((item) => item.id === detail.id)
            if (exists)
                return current.map((item) => (item.id === detail.id ? { ...item, ...nextSummary } : item))

            return [nextSummary, ...current]
        })
    }

    const handleSave = async () => {
        setSaving(true)
        setEditorError("")
        setSuccessMessage("")

        try {
            const response = isNewPost
                ? await axiosInstance.post("/admin/blog/posts", buildPayload())
                : await axiosInstance.put(`/admin/blog/posts/${form.id}`, buildPayload())

            const detail = response?.data || createEmptyDraft()
            applyDetail(detail)
            setSelectedId(detail.id)
            setIsNewPost(false)
            setSuccessMessage(detail.status === "Published" ? "Alterações publicadas com sucesso." : "Rascunho salvo com sucesso.")
            syncPostList(detail)
            await loadPosts()
            return detail
        } catch (err) {
            setEditorError(normalizeError(err, "Não foi possível salvar a postagem."))
            return null
        } finally {
            setSaving(false)
        }
    }

    const handlePublish = async () => {
        let targetId = form.id

        if (!targetId) {
            const saved = await handleSave()
            targetId = saved?.id || null
        }

        if (!targetId) return

        setPublishing(true)
        setEditorError("")
        setSuccessMessage("")

        try {
            const response = await axiosInstance.post(`/admin/blog/posts/${targetId}/publish`)
            const detail = response?.data || createEmptyDraft()
            applyDetail(detail)
            setSelectedId(detail.id)
            setIsNewPost(false)
            setSuccessMessage("Postagem publicada no blog.")
            syncPostList(detail)
            await loadPosts()
        } catch (err) {
            setEditorError(normalizeError(err, "Não foi possível publicar a postagem."))
        } finally {
            setPublishing(false)
        }
    }

    const handleRetrySocial = async (channel) => {
        if (!form.id) return

        setPublishing(true)
        setEditorError("")
        setSuccessMessage("")

        try {
            const response = await axiosInstance.post(`/admin/blog/posts/${form.id}/social/${channel}/retry`)
            const detail = response?.data || createEmptyDraft()
            applyDetail(detail)
            syncPostList(detail)
            setSuccessMessage(`Publicação em ${getBlogSocialChannelLabel(channel)} processada.`)
            await loadPosts()
        } catch (err) {
            setEditorError(normalizeError(err, "Não foi possível reprocessar a rede social."))
        } finally {
            setPublishing(false)
        }
    }

    return (
        <>
            <Logged />
            <BaseLayout title="Blog" description="Crie posts ricos com imagens, publique no blog público e acompanhe o status social.">
                <div className="space-y-8 px-4 lg:px-6">
                    {listError ? (
                        <Alert variant="destructive">
                            <AlertTitle>Falha ao carregar</AlertTitle>
                            <AlertDescription>{listError}</AlertDescription>
                        </Alert>
                    ) : null}

                    {editorError && !isEditorOpen ? (
                        <Alert variant="destructive">
                            <AlertTitle>Algo saiu do esperado</AlertTitle>
                            <AlertDescription>{editorError}</AlertDescription>
                        </Alert>
                    ) : null}

                    {successMessage && !isEditorOpen ? (
                        <Alert>
                            <AlertTitle>Pronto</AlertTitle>
                            <AlertDescription>{successMessage}</AlertDescription>
                        </Alert>
                    ) : null}

                    <div className="flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
                        <div className="max-w-3xl">
                            <Badge variant="outline" className="mb-3">Blog</Badge>
                            <h1 className="text-3xl font-bold tracking-tight sm:text-4xl">Postagens e rascunhos</h1>
                            <p className="mt-3 text-base text-muted-foreground">
                                A visualização segue a mesma linguagem da landing. Clique em qualquer postagem para abrir a edição completa em modal.
                            </p>
                        </div>
                        <div className="flex flex-col gap-3 sm:flex-row sm:items-center">
                            <Select value={scope} onValueChange={setScope}>
                                <SelectTrigger className="min-w-[180px]">
                                    <SelectValue placeholder="Filtrar postagens" />
                                </SelectTrigger>
                                <SelectContent>
                                    {BLOG_SCOPE_OPTIONS.map((option) => (
                                        <SelectItem key={option.value} value={option.value}>
                                            {option.label}
                                        </SelectItem>
                                    ))}
                                </SelectContent>
                            </Select>
                            <Button type="button" onClick={handleCreateNew}>
                                <Plus className="size-4" />
                                Novo post
                            </Button>
                        </div>
                    </div>

                    {loadingList ? (
                        <div className="grid grid-cols-1 gap-8 lg:grid-cols-2">
                            <Skeleton className="h-[28rem] rounded-[2rem]" />
                            <Skeleton className="h-[28rem] rounded-[2rem]" />
                            <Skeleton className="h-[28rem] rounded-[2rem]" />
                            <Skeleton className="h-[28rem] rounded-[2rem]" />
                        </div>
                    ) : posts.length === 0 ? (
                        <Card className="mx-auto max-w-2xl rounded-[2rem] border-dashed">
                            <CardContent className="px-8 py-16 text-center">
                                <h2 className="text-2xl font-semibold">Nenhuma postagem ainda</h2>
                                <p className="mt-3 text-muted-foreground">
                                    Crie o primeiro rascunho para começar a montar o blog público.
                                </p>
                            </CardContent>
                        </Card>
                    ) : (
                        <div className="grid grid-cols-1 gap-8 lg:grid-cols-2">
                            {posts.map((post) => (
                                <button
                                    key={post.id}
                                    type="button"
                                    onClick={() => loadPost(post.id)}
                                    className="group block text-left"
                                >
                                    <Card className="h-full overflow-hidden rounded-[2rem] border bg-card py-0 transition-transform duration-300 group-hover:-translate-y-1">
                                        <div className="aspect-[16/9] overflow-hidden border-b bg-muted">
                                            {post.cover_asset?.url ? (
                                                <img
                                                    src={post.cover_asset.url}
                                                    alt={post.title}
                                                    className="h-full w-full object-cover transition-transform duration-500 group-hover:scale-[1.02]"
                                                />
                                            ) : (
                                                <div className="flex h-full items-center justify-center bg-gradient-to-br from-primary/15 via-transparent to-primary/5">
                                                    <PenSquare className="h-12 w-12 text-primary/60" />
                                                </div>
                                            )}
                                        </div>
                                        <CardContent className="space-y-5 p-8">
                                            <div className="flex flex-wrap items-center justify-between gap-3">
                                                <div className="flex flex-wrap items-center gap-3 text-xs uppercase tracking-[0.2em] text-muted-foreground">
                                                    <span className="inline-flex items-center gap-1">
                                                        <CalendarDays className="h-3.5 w-3.5" />
                                                        {formatBlogDateTime(post.published_at || post.updated_at)}
                                                    </span>
                                                    <span>{post.author_user_name}</span>
                                                </div>
                                                <BlogStatusBadge status={post.status} />
                                            </div>
                                            <div>
                                                <h2 className="text-3xl font-semibold leading-tight">{post.title}</h2>
                                                <p className="mt-4 text-base leading-7 text-muted-foreground">{post.excerpt}</p>
                                            </div>
                                            <span className="inline-flex items-center gap-2 text-sm font-semibold text-primary">
                                                Abrir edição
                                            </span>
                                        </CardContent>
                                    </Card>
                                </button>
                            ))}
                        </div>
                    )}

                    <Dialog open={isEditorOpen} onOpenChange={closeEditor}>
                        <DialogContent className="flex max-h-[96vh] w-[96vw] max-w-[96vw] flex-col overflow-hidden p-0 sm:max-w-[96vw] xl:max-w-[1680px]">
                            <DialogHeader className="border-b px-6 py-5">
                                <div className="flex flex-wrap items-start justify-between gap-4 pr-8">
                                    <div className="space-y-2">
                                        <div className="flex flex-wrap items-center gap-2">
                                            <DialogTitle>{isNewPost ? "Novo post" : form.title || "Editar postagem"}</DialogTitle>
                                            <BlogStatusBadge status={form.status} />
                                        </div>
                                        <DialogDescription>
                                            {isNewPost
                                                ? "Crie um rascunho com texto rico e imagens inline."
                                                : `Slug público: /blog/${form.slug || "..."}`}
                                        </DialogDescription>
                                    </div>
                                    <div className="flex flex-wrap gap-2">
                                        {SOCIAL_CHANNELS.map((channel) => {
                                            const publication = socialPublicationByChannel.get(channel)
                                            const isPublishedOnce = Boolean(publication?.published_at)
                                            const isFailed = publication?.status === "Failed"

                                            return (
                                                <Button
                                                    key={channel}
                                                    type="button"
                                                    variant="outline"
                                                    title={publication?.error_message || ""}
                                                    className={cn(
                                                        "justify-start text-left",
                                                        isPublishedOnce && "border-emerald-300 bg-emerald-50 text-emerald-700 hover:bg-emerald-100",
                                                        isFailed && "border-rose-300 bg-rose-50 text-rose-700 hover:bg-rose-100"
                                                    )}
                                                    disabled={!form.id || !isPublishedPost || saving || publishing || loadingDetail}
                                                    onClick={() => handleRetrySocial(channel)}
                                                >
                                                    <span className="flex flex-col items-start leading-tight">
                                                        <span>{getBlogSocialChannelLabel(channel)}</span>
                                                        <span className="text-[11px] opacity-80">{getSocialButtonLabel(publication)}</span>
                                                    </span>
                                                </Button>
                                            )
                                        })}
                                        <Button type="button" variant="outline" onClick={handleSave} disabled={saving || publishing || loadingDetail}>
                                            {saving ? <Loader2 className="size-4 animate-spin" /> : <Save className="size-4" />}
                                            {isPublishedPost ? "Salvar alterações" : "Salvar rascunho"}
                                        </Button>
                                        {!isPublishedPost ? (
                                            <Button type="button" onClick={handlePublish} disabled={publishing || saving || loadingDetail}>
                                                {publishing ? <Loader2 className="size-4 animate-spin" /> : <Send className="size-4" />}
                                                Publicar
                                            </Button>
                                        ) : null}
                                        {form.public_post_url ? (
                                            <Button type="button" variant="outline" onClick={() => window.open(form.public_post_url, "_blank", "noopener,noreferrer")}>
                                                <ExternalLink className="size-4" />
                                                Ver público
                                            </Button>
                                        ) : null}
                                    </div>
                                </div>
                            </DialogHeader>

                            <div className="min-h-0 flex-1 overflow-y-auto px-6 py-6">
                                <div className="mx-auto grid max-w-[1400px] gap-8">
                                    {editorError && isEditorOpen ? (
                                        <Alert variant="destructive">
                                            <AlertTitle>Algo saiu do esperado</AlertTitle>
                                            <AlertDescription>{editorError}</AlertDescription>
                                        </Alert>
                                    ) : null}

                                    {successMessage && isEditorOpen ? (
                                        <Alert>
                                            <AlertTitle>Pronto</AlertTitle>
                                            <AlertDescription>{successMessage}</AlertDescription>
                                        </Alert>
                                    ) : null}

                                    {loadingDetail ? (
                                        <div className="space-y-4">
                                            <Skeleton className="h-10 rounded-lg" />
                                            <Skeleton className="h-28 rounded-lg" />
                                            <Skeleton className="h-[24rem] rounded-lg" />
                                        </div>
                                    ) : (
                                        <>
                                            <div className="grid gap-6 xl:grid-cols-[minmax(0,1.2fr),minmax(320px,0.8fr)]">
                                                <div className="grid gap-4">
                                                    <div className="grid gap-2">
                                                        <label className="text-sm font-medium">Título</label>
                                                        <Input
                                                            value={form.title}
                                                            onChange={(event) => setForm((current) => ({ ...current, title: event.target.value }))}
                                                            maxLength={180}
                                                            placeholder="Título da postagem"
                                                        />
                                                    </div>

                                                    <div className="grid gap-2">
                                                        <label className="text-sm font-medium">Resumo</label>
                                                        <textarea
                                                            value={form.excerpt}
                                                            onChange={(event) => setForm((current) => ({ ...current, excerpt: event.target.value }))}
                                                            maxLength={320}
                                                            rows={5}
                                                            placeholder="Resumo curto para cards, SEO e redes sociais"
                                                            className="border-input bg-background ring-offset-background placeholder:text-muted-foreground focus-visible:ring-ring min-h-32 rounded-xl border px-3 py-2 text-sm outline-none focus-visible:ring-2"
                                                        />
                                                    </div>
                                                </div>

                                                <div className="grid gap-2">
                                                    <div className="flex items-center justify-between gap-3">
                                                        <label className="text-sm font-medium">Capa</label>
                                                        <input
                                                            ref={coverFileRef}
                                                            type="file"
                                                            accept={BLOG_IMAGE_ACCEPT}
                                                            className="hidden"
                                                            onChange={handleCoverSelected}
                                                        />
                                                        <Button type="button" variant="outline" onClick={() => coverFileRef.current?.click()} disabled={uploadingImages}>
                                                            {uploadingImages ? <Loader2 className="size-4 animate-spin" /> : <FileImage className="size-4" />}
                                                            Enviar capa
                                                        </Button>
                                                    </div>
                                                    {form.cover_asset?.url ? (
                                                        <div className="overflow-hidden rounded-2xl border">
                                                            <img src={form.cover_asset.url} alt={form.cover_asset.alt_text || form.cover_asset.file_name || "Capa"} className="h-72 w-full object-cover" />
                                                        </div>
                                                    ) : (
                                                        <div className="text-muted-foreground rounded-2xl border border-dashed px-4 py-16 text-center text-sm">
                                                            Nenhuma imagem de capa enviada ainda.
                                                        </div>
                                                    )}
                                                </div>
                                            </div>

                                            <div className="grid gap-2">
                                                <div className="flex items-center justify-between gap-3">
                                                    <label className="text-sm font-medium">Conteúdo</label>
                                                    <p className="text-muted-foreground text-xs">Use o botão de imagem para inserir screenshots e imagens inline.</p>
                                                </div>
                                                <BlogRichTextEditor
                                                    value={form.body_html}
                                                    onChange={(html) => setForm((current) => ({ ...current, body_html: html }))}
                                                    uploading={uploadingImages}
                                                />
                                            </div>

                                            <div className="space-y-5 rounded-[2rem] border bg-card p-6">
                                                <div>
                                                    <div className="flex flex-wrap items-center gap-2">
                                                        <h2 className="text-3xl font-bold">{form.title || "Título da postagem"}</h2>
                                                        <Badge variant="outline">{getBlogStatusLabel(form.status)}</Badge>
                                                    </div>
                                                    <p className="mt-3 text-base text-muted-foreground">
                                                        {form.excerpt || "O resumo aparecerá aqui quando for preenchido."}
                                                    </p>
                                                </div>

                                                {form.cover_asset?.url ? (
                                                    <div className="overflow-hidden rounded-[1.5rem] border">
                                                        <img src={form.cover_asset.url} alt={form.cover_asset.alt_text || form.cover_asset.file_name || "Capa"} className="h-64 w-full object-cover" />
                                                    </div>
                                                ) : null}

                                                <div
                                                    className={cn(
                                                        "text-sm leading-7",
                                                        "[&_a]:text-primary [&_a]:underline [&_blockquote]:border-l-4 [&_blockquote]:border-border [&_blockquote]:pl-4 [&_blockquote]:italic",
                                                        "[&_h1]:mb-3 [&_h1]:text-3xl [&_h1]:font-bold [&_h2]:mb-3 [&_h2]:text-2xl [&_h2]:font-semibold [&_h3]:mb-2 [&_h3]:text-xl [&_h3]:font-semibold",
                                                        "[&_img]:my-4 [&_img]:max-w-full [&_img]:rounded-2xl [&_img]:border [&_ol]:list-decimal [&_ol]:pl-6 [&_p]:mb-3 [&_ul]:list-disc [&_ul]:pl-6"
                                                    )}
                                                    dangerouslySetInnerHTML={{ __html: form.body_html || "<p>O conteúdo do post aparecerá aqui.</p>" }}
                                                />
                                            </div>
                                        </>
                                    )}
                                </div>
                            </div>
                        </DialogContent>
                    </Dialog>
                </div>
            </BaseLayout>
        </>
    )
}
