import { useEffect, useMemo, useRef, useState } from "react"
import { useNavigate } from "react-router-dom"
import {
    ExternalLink,
    FileImage,
    Globe2,
    Loader2,
    PenSquare,
    Plus,
    RefreshCcw,
    Save,
    Send,
    Share2,
} from "lucide-react"
import Logged from "@/components/Logged"
import BlogRichTextEditor from "@/components/blog/BlogRichTextEditor"
import { BaseLayout } from "@/components/layouts/base-layout"
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
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
import {
    BLOG_IMAGE_ACCEPT,
    BLOG_SCOPE_ALL,
    BLOG_SCOPE_OPTIONS,
    formatBlogDateTime,
    getBlogSocialChannelLabel,
    getBlogSocialStatusLabel,
    getBlogSocialStatusTone,
    getBlogStatusLabel,
    getBlogStatusTone,
} from "@/utils/blog"
import { getStoredUserType, isAdminUserType } from "@/utils/userSession"

function BlogStatusBadge({ status }) {
    return (
        <span className={cn("inline-flex items-center rounded-full border px-2.5 py-1 text-xs font-medium", getBlogStatusTone(status))}>
            {getBlogStatusLabel(status)}
        </span>
    )
}

function SocialStatusBadge({ status }) {
    return (
        <span className={cn("inline-flex items-center rounded-full border px-2.5 py-1 text-xs font-medium", getBlogSocialStatusTone(status))}>
            {getBlogSocialStatusLabel(status)}
        </span>
    )
}

function createEmptyDraft() {
    return {
        id: null,
        slug: "",
        title: "",
        excerpt: "",
        body_html: "",
        status: "Draft",
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
    const [form, setForm] = useState(createEmptyDraft())

    const loadPosts = async ({ preserveSelection = true } = {}) => {
        setLoadingList(true)
        setListError("")

        try {
            const response = await axiosInstance.get("/admin/blog/posts", {
                params: { scope },
            })

            const nextPosts = response?.data?.items || []
            setPosts(nextPosts)

            if (!preserveSelection) {
                return
            }

            if (!isNewPost && selectedId && nextPosts.some((item) => item.id === selectedId)) {
                return
            }

            if (!isNewPost && nextPosts.length > 0) {
                const firstId = nextPosts[0].id
                setSelectedId(firstId)
                await loadPost(firstId)
                return
            }
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

        let cancelled = false

        ;(async () => {
            try {
                const response = await axiosInstance.get("/admin/blog/posts", {
                    params: { scope },
                })

                if (cancelled) return

                const nextPosts = response?.data?.items || []
                setPosts(nextPosts)

                if (nextPosts.length > 0) {
                    setSelectedId(nextPosts[0].id)
                    setIsNewPost(false)
                    setLoadingDetail(true)

                    try {
                        const detailResponse = await axiosInstance.get(`/admin/blog/posts/${nextPosts[0].id}`)
                        if (cancelled) return
                        applyDetail(detailResponse?.data || createEmptyDraft())
                    } catch {
                        if (cancelled) return
                        setEditorError("Não foi possível carregar a postagem selecionada.")
                    } finally {
                        if (!cancelled) {
                            setLoadingDetail(false)
                        }
                    }
                } else {
                    setIsNewPost(true)
                    setForm(createEmptyDraft())
                }
            } catch (err) {
                if (cancelled) return

                if (err?.response?.status === 403) {
                    navigate("/home", { replace: true })
                    return
                }

                setListError("Não foi possível carregar as postagens do blog.")
            } finally {
                if (!cancelled) {
                    setLoadingList(false)
                }
            }
        })()

        return () => {
            cancelled = true
        }
    }, [navigate, scope])

    const socialPublications = useMemo(
        () => form.social_publications || [],
        [form.social_publications]
    )

    const handleCreateNew = () => {
        setIsNewPost(true)
        setSelectedId(null)
        setEditorError("")
        setSuccessMessage("")
        setForm(createEmptyDraft())
    }

    const handleUploadAssets = async (files) => {
        if (!files?.length) return []

        setUploadingImages(true)
        setEditorError("")

        try {
            const uploaded = []

            for (const file of files) {
                const formData = new FormData()
                formData.append("image", file)
                formData.append("alt_text", file.name.replace(/\.[^/.]+$/, ""))

                const response = await axiosInstance.post("/admin/blog/assets", formData, {
                    headers: { "Content-Type": "multipart/form-data" },
                })

                uploaded.push(response.data)
            }

            return uploaded
        } catch (err) {
            setEditorError(normalizeError(err, "Não foi possível enviar uma ou mais imagens do post."))
            return []
        } finally {
            setUploadingImages(false)
        }
    }

    const handleCoverSelected = async (event) => {
        const file = event.target.files?.[0]
        event.target.value = ""

        if (!file) return

        const uploaded = await handleUploadAssets([file])
        if (!uploaded.length) return

        setForm((current) => ({
            ...current,
            cover_asset_id: uploaded[0].id,
            cover_asset: uploaded[0],
        }))
    }

    const buildPayload = () => ({
        title: form.title,
        excerpt: form.excerpt,
        body_html: form.body_html,
        cover_asset_id: form.cover_asset_id,
    })

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
            setSuccessMessage("Rascunho salvo com sucesso.")
            await loadPosts({ preserveSelection: false })
            setPosts((current) => {
                const exists = current.some((item) => item.id === detail.id)
                if (exists) {
                    return current.map((item) => (item.id === detail.id ? { ...item, ...detail } : item))
                }

                return [
                    {
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
                    },
                    ...current,
                ]
            })

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
            setSuccessMessage("Postagem publicada no blog e enviada para as redes sociais.")
            await loadPosts({ preserveSelection: false })
        } catch (err) {
            setEditorError(normalizeError(err, "Não foi possível publicar a postagem."))
        } finally {
            setPublishing(false)
        }
    }

    const handleRetrySocial = async (channel) => {
        if (!form.id) return

        setEditorError("")
        setSuccessMessage("")

        try {
            const response = await axiosInstance.post(`/admin/blog/posts/${form.id}/social/${channel}/retry`)
            applyDetail(response?.data || createEmptyDraft())
            setSuccessMessage(`Integração com ${getBlogSocialChannelLabel(channel)} reprocessada.`)
        } catch (err) {
            setEditorError(normalizeError(err, "Não foi possível reprocessar a rede social."))
        }
    }

    return (
        <>
            <Logged />
            <BaseLayout title="Blog" description="Crie posts ricos com imagens, publique no blog público e acompanhe o status social.">
                <div className="space-y-6 px-4 lg:px-6">
                    {listError ? (
                        <Alert variant="destructive">
                            <AlertTitle>Falha ao carregar</AlertTitle>
                            <AlertDescription>{listError}</AlertDescription>
                        </Alert>
                    ) : null}

                    {editorError ? (
                        <Alert variant="destructive">
                            <AlertTitle>Algo saiu do esperado</AlertTitle>
                            <AlertDescription>{editorError}</AlertDescription>
                        </Alert>
                    ) : null}

                    {successMessage ? (
                        <Alert>
                            <AlertTitle>Pronto</AlertTitle>
                            <AlertDescription>{successMessage}</AlertDescription>
                        </Alert>
                    ) : null}

                    <div className="grid gap-6 xl:grid-cols-[320px,minmax(0,1fr)]">
                        <Card className="overflow-hidden">
                            <CardHeader className="gap-4">
                                <div className="flex items-center justify-between gap-3">
                                    <div>
                                        <CardTitle>Postagens</CardTitle>
                                        <CardDescription>Rascunhos, publicadas e fila de edição.</CardDescription>
                                    </div>
                                    <Button type="button" onClick={handleCreateNew}>
                                        <Plus className="size-4" />
                                        Novo post
                                    </Button>
                                </div>
                                <Select value={scope} onValueChange={setScope}>
                                    <SelectTrigger>
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
                            </CardHeader>
                            <CardContent className="p-0">
                                <ScrollArea className="h-[70vh]">
                                    <div className="space-y-3 p-4">
                                        {loadingList ? (
                                            <>
                                                <Skeleton className="h-28 rounded-xl" />
                                                <Skeleton className="h-28 rounded-xl" />
                                                <Skeleton className="h-28 rounded-xl" />
                                            </>
                                        ) : posts.length === 0 ? (
                                            <div className="rounded-xl border border-dashed px-4 py-8 text-center">
                                                <p className="font-medium">Nenhuma postagem ainda.</p>
                                                <p className="text-muted-foreground mt-1 text-sm">Crie o primeiro rascunho para começar.</p>
                                            </div>
                                        ) : (
                                            posts.map((post) => (
                                                <button
                                                    key={post.id}
                                                    type="button"
                                                    onClick={() => loadPost(post.id)}
                                                    className={cn(
                                                        "w-full rounded-xl border p-4 text-left transition-colors hover:bg-muted/40",
                                                        selectedId === post.id && !isNewPost && "border-primary bg-primary/5"
                                                    )}
                                                >
                                                    <div className="flex items-start justify-between gap-3">
                                                        <div className="min-w-0">
                                                            <p className="truncate font-semibold">{post.title}</p>
                                                            <p className="text-muted-foreground mt-1 line-clamp-2 text-sm">{post.excerpt}</p>
                                                        </div>
                                                        <BlogStatusBadge status={post.status} />
                                                    </div>
                                                    <div className="mt-4 flex items-center justify-between gap-3 text-xs text-muted-foreground">
                                                        <span>{post.author_user_name}</span>
                                                        <span>{formatBlogDateTime(post.published_at || post.updated_at)}</span>
                                                    </div>
                                                </button>
                                            ))
                                        )}
                                    </div>
                                </ScrollArea>
                            </CardContent>
                        </Card>

                        <div className="grid gap-6 xl:grid-cols-[minmax(0,1fr),360px]">
                            <Card className="overflow-hidden">
                                <CardHeader className="gap-4 border-b">
                                    <div className="flex flex-wrap items-start justify-between gap-4">
                                        <div>
                                            <div className="flex flex-wrap items-center gap-2">
                                                <CardTitle>{isNewPost ? "Novo post" : form.title || "Editar postagem"}</CardTitle>
                                                <BlogStatusBadge status={form.status} />
                                            </div>
                                            <CardDescription>
                                                {isNewPost
                                                    ? "Crie um rascunho com texto rico e imagens inline."
                                                    : `Slug público: /blog/${form.slug || "..."}`}
                                            </CardDescription>
                                        </div>
                                        <div className="flex flex-wrap gap-2">
                                            <Button type="button" variant="outline" onClick={handleSave} disabled={saving || publishing || loadingDetail}>
                                                {saving ? <Loader2 className="size-4 animate-spin" /> : <Save className="size-4" />}
                                                Salvar rascunho
                                            </Button>
                                            <Button type="button" onClick={handlePublish} disabled={publishing || saving || loadingDetail}>
                                                {publishing ? <Loader2 className="size-4 animate-spin" /> : <Send className="size-4" />}
                                                Publicar
                                            </Button>
                                            {form.public_post_url ? (
                                                <Button type="button" variant="outline" onClick={() => window.open(form.public_post_url, "_blank", "noopener,noreferrer")}>
                                                    <ExternalLink className="size-4" />
                                                    Ver público
                                                </Button>
                                            ) : null}
                                        </div>
                                    </div>
                                </CardHeader>
                                <CardContent className="space-y-6 p-6">
                                    {loadingDetail ? (
                                        <div className="space-y-4">
                                            <Skeleton className="h-10 rounded-lg" />
                                            <Skeleton className="h-28 rounded-lg" />
                                            <Skeleton className="h-[24rem] rounded-lg" />
                                        </div>
                                    ) : (
                                        <>
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
                                                        rows={4}
                                                        placeholder="Resumo curto para cards, SEO e redes sociais"
                                                        className="border-input bg-background ring-offset-background placeholder:text-muted-foreground focus-visible:ring-ring min-h-28 rounded-xl border px-3 py-2 text-sm outline-none focus-visible:ring-2"
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
                                                        <img src={form.cover_asset.url} alt={form.cover_asset.alt_text || form.cover_asset.file_name || "Capa"} className="h-64 w-full object-cover" />
                                                    </div>
                                                ) : (
                                                    <div className="text-muted-foreground rounded-2xl border border-dashed px-4 py-10 text-center text-sm">
                                                        Nenhuma imagem de capa enviada ainda.
                                                    </div>
                                                )}
                                            </div>

                                            <div className="grid gap-2">
                                                <div className="flex items-center justify-between gap-3">
                                                    <label className="text-sm font-medium">Conteúdo</label>
                                                    <p className="text-muted-foreground text-xs">Use o botão de imagem para inserir screenshots e imagens inline.</p>
                                                </div>
                                                <BlogRichTextEditor
                                                    value={form.body_html}
                                                    onChange={(html) => setForm((current) => ({ ...current, body_html: html }))}
                                                    onUploadImages={handleUploadAssets}
                                                    uploading={uploadingImages}
                                                />
                                            </div>
                                        </>
                                    )}
                                </CardContent>
                            </Card>

                            <div className="space-y-6">
                                <Card>
                                    <CardHeader>
                                        <div className="flex items-center gap-2">
                                            <Share2 className="size-5" />
                                            <CardTitle>Publicação social</CardTitle>
                                        </div>
                                        <CardDescription>Status por canal e reprocessamento individual.</CardDescription>
                                    </CardHeader>
                                    <CardContent className="space-y-3">
                                        {socialPublications.map((publication) => (
                                            <div key={publication.channel} className="rounded-xl border p-4">
                                                <div className="flex items-start justify-between gap-3">
                                                    <div>
                                                        <p className="font-medium">{getBlogSocialChannelLabel(publication.channel)}</p>
                                                        <p className="text-muted-foreground text-xs">
                                                            {publication.published_at
                                                                ? `Publicado em ${formatBlogDateTime(publication.published_at)}`
                                                                : "Ainda não publicado neste canal"}
                                                        </p>
                                                    </div>
                                                    <SocialStatusBadge status={publication.status} />
                                                </div>
                                                {publication.error_message ? (
                                                    <p className="mt-3 text-sm text-rose-600">{publication.error_message}</p>
                                                ) : null}
                                                <div className="mt-3 flex flex-wrap gap-2">
                                                    {publication.remote_url ? (
                                                        <Button type="button" variant="outline" size="sm" onClick={() => window.open(publication.remote_url, "_blank", "noopener,noreferrer")}>
                                                            <Globe2 className="size-4" />
                                                            Abrir post
                                                        </Button>
                                                    ) : null}
                                                    {form.status === "Published" ? (
                                                        <Button type="button" variant="outline" size="sm" onClick={() => handleRetrySocial(publication.channel)}>
                                                            <RefreshCcw className="size-4" />
                                                            Reprocessar
                                                        </Button>
                                                    ) : null}
                                                </div>
                                            </div>
                                        ))}
                                    </CardContent>
                                </Card>

                                <Card>
                                    <CardHeader>
                                        <div className="flex items-center gap-2">
                                            <PenSquare className="size-5" />
                                            <CardTitle>Preview</CardTitle>
                                        </div>
                                        <CardDescription>Prévia do conteúdo como será mostrado no blog público.</CardDescription>
                                    </CardHeader>
                                    <CardContent className="space-y-4">
                                        <div>
                                            <div className="flex flex-wrap items-center gap-2">
                                                <h2 className="text-2xl font-bold">{form.title || "Título da postagem"}</h2>
                                                <Badge variant="outline">{getBlogStatusLabel(form.status)}</Badge>
                                            </div>
                                            <p className="text-muted-foreground mt-2 text-sm">{form.excerpt || "O resumo aparecerá aqui quando for preenchido."}</p>
                                        </div>

                                        {form.cover_asset?.url ? (
                                            <div className="overflow-hidden rounded-2xl border">
                                                <img src={form.cover_asset.url} alt={form.cover_asset.alt_text || form.cover_asset.file_name || "Capa"} className="h-52 w-full object-cover" />
                                            </div>
                                        ) : null}

                                        <div
                                            className={cn(
                                                "text-sm leading-7",
                                                "[&_a]:text-primary [&_a]:underline [&_blockquote]:border-l-4 [&_blockquote]:border-border [&_blockquote]:pl-4 [&_blockquote]:italic",
                                                "[&_h1]:mb-3 [&_h1]:text-3xl [&_h1]:font-bold [&_h2]:mb-3 [&_h2]:text-2xl [&_h2]:font-semibold [&_h3]:mb-2 [&_h3]:text-xl [&_h3]:font-semibold",
                                                "[&_img]:my-4 [&_img]:w-full [&_img]:rounded-2xl [&_img]:border [&_ol]:list-decimal [&_ol]:pl-6 [&_p]:mb-3 [&_ul]:list-disc [&_ul]:pl-6"
                                            )}
                                            dangerouslySetInnerHTML={{ __html: form.body_html || "<p>O conteúdo do post aparecerá aqui.</p>" }}
                                        />
                                    </CardContent>
                                </Card>
                            </div>
                        </div>
                    </div>
                </div>
            </BaseLayout>
        </>
    )
}
