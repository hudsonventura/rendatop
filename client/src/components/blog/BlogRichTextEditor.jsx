import { useEffect, useRef } from "react"
import { Bold, ImagePlus, Italic, Link2, List, ListOrdered, Quote, Strikethrough, Underline } from "lucide-react"
import { Button } from "@/components/ui/button"
import { cn } from "@/lib/utils"

const TOOLBAR_ACTIONS = [
    { icon: Bold, label: "Negrito", command: "bold" },
    { icon: Italic, label: "Itálico", command: "italic" },
    { icon: Underline, label: "Sublinhado", command: "underline" },
    { icon: Strikethrough, label: "Tachado", command: "strikeThrough" },
    { icon: List, label: "Lista", command: "insertUnorderedList" },
    { icon: ListOrdered, label: "Lista ordenada", command: "insertOrderedList" },
    { icon: Quote, label: "Citação", command: "formatBlock", value: "blockquote" },
]

function normalizeEditorHtml(value) {
    return String(value || "").trim()
}

export default function BlogRichTextEditor({
    value,
    onChange,
    disabled = false,
    uploading = false,
    placeholder = "Escreva o conteúdo do post...",
    className,
}) {
    const editorRef = useRef(null)
    const fileInputRef = useRef(null)
    const savedRangeRef = useRef(null)
    const selectedImageRef = useRef(null)

    useEffect(() => {
        const editor = editorRef.current
        if (!editor) return

        const nextValue = normalizeEditorHtml(value)
        const currentValue = normalizeEditorHtml(editor.innerHTML)

        if (nextValue !== currentValue) {
            editor.innerHTML = nextValue
        }
    }, [value])

    const syncSelection = () => {
        const selection = window.getSelection()
        if (!selection || selection.rangeCount === 0) return

        const range = selection.getRangeAt(0)
        if (editorRef.current?.contains(range.commonAncestorContainer)) {
            savedRangeRef.current = range.cloneRange()
        }

        const commonNode = range.commonAncestorContainer
        const element = commonNode?.nodeType === Node.ELEMENT_NODE
            ? commonNode
            : commonNode?.parentElement

        const nextSelectedImage = element?.closest?.("img") || null
        selectedImageRef.current = nextSelectedImage instanceof HTMLImageElement ? nextSelectedImage : null
    }

    const restoreSelection = () => {
        const selection = window.getSelection()
        if (!selection) return

        selection.removeAllRanges()
        if (savedRangeRef.current) {
            selection.addRange(savedRangeRef.current)
            return
        }

        const editor = editorRef.current
        if (!editor) return

        editor.focus()
        const range = document.createRange()
        range.selectNodeContents(editor)
        range.collapse(false)
        selection.addRange(range)
    }

    const handleInput = () => {
        syncSelection()
        onChange(editorRef.current?.innerHTML || "")
    }

    const runCommand = (command, commandValue) => {
        if (disabled) return

        restoreSelection()
        editorRef.current?.focus()
        document.execCommand(command, false, commandValue)
        handleInput()
    }

    const handleCreateLink = () => {
        if (disabled) return

        const link = window.prompt("Informe a URL do link")
        if (!link) return

        runCommand("createLink", link)
    }

    const insertHtmlAtCursor = (html) => {
        restoreSelection()
        document.execCommand("insertHTML", false, html)
        handleInput()
    }

    const resizeSelectedImage = (percentage) => {
        const image = selectedImageRef.current
        if (!image || disabled) return

        image.style.width = `${percentage}%`
        image.style.maxWidth = "100%"
        image.style.height = "auto"
        image.style.display = "block"
        handleInput()
    }

    const readFileAsDataUrl = (file) =>
        new Promise((resolve, reject) => {
            const reader = new FileReader()
            reader.onload = () => resolve(String(reader.result || ""))
            reader.onerror = () => reject(reader.error || new Error("Falha ao ler imagem"))
            reader.readAsDataURL(file)
        })

    const handleSelectImages = async (event) => {
        const files = Array.from(event.target.files || [])
        event.target.value = ""

        if (!files.length || disabled) return

        for (const file of files) {
            const dataUrl = await readFileAsDataUrl(file)
            if (!dataUrl) continue

            const alt = (file.name || "").replace(/\.[^/.]+$/, "")
            insertHtmlAtCursor(
                `<p><img src="${dataUrl}" alt="${alt}" style="width:65%;max-width:100%;height:auto;display:block;"></p>`
            )
        }
    }

    const isEmpty = !normalizeEditorHtml(value).replace(/<[^>]+>/g, "").trim()

    return (
        <div className={cn("overflow-hidden rounded-xl border bg-background", className)}>
            <div className="flex flex-wrap gap-2 border-b px-3 py-2">
                {TOOLBAR_ACTIONS.map((action) => (
                    <Button
                        key={action.label}
                        type="button"
                        variant="outline"
                        size="sm"
                        className="h-8 px-2"
                        disabled={disabled || uploading}
                        onClick={() => runCommand(action.command, action.value)}
                    >
                        <action.icon className="size-4" />
                        <span className="sr-only">{action.label}</span>
                    </Button>
                ))}
                <Button
                    type="button"
                    variant="outline"
                    size="sm"
                    className="h-8 px-2"
                    disabled={disabled || uploading}
                    onClick={handleCreateLink}
                >
                    <Link2 className="size-4" />
                    <span className="sr-only">Inserir link</span>
                </Button>
                <input
                    ref={fileInputRef}
                    type="file"
                    className="hidden"
                    accept="image/png,image/jpeg,image/jpg,image/webp,image/gif"
                    multiple
                    onChange={handleSelectImages}
                    disabled={disabled || uploading}
                />
                <Button
                    type="button"
                    variant="outline"
                    size="sm"
                    className="h-8 px-2"
                    disabled={disabled || uploading}
                    onMouseDown={syncSelection}
                    onClick={() => fileInputRef.current?.click()}
                >
                    <ImagePlus className="size-4" />
                    <span className="sr-only">Inserir imagem</span>
                </Button>
            </div>

            <div className="flex flex-wrap items-center gap-3 border-b px-3 py-2">
                <span className="text-muted-foreground text-xs font-medium">Tamanho da imagem</span>
                <Button type="button" variant="outline" size="sm" className="h-8 px-3" disabled={disabled} onClick={() => resizeSelectedImage(40)}>
                    40%
                </Button>
                <Button type="button" variant="outline" size="sm" className="h-8 px-3" disabled={disabled} onClick={() => resizeSelectedImage(65)}>
                    65%
                </Button>
                <Button type="button" variant="outline" size="sm" className="h-8 px-3" disabled={disabled} onClick={() => resizeSelectedImage(100)}>
                    100%
                </Button>
                <span className="text-muted-foreground text-xs">Clique na imagem e escolha a largura.</span>
            </div>

            <div className="relative">
                {isEmpty ? (
                    <div className="text-muted-foreground pointer-events-none absolute left-4 top-3 text-sm">
                        {placeholder}
                    </div>
                ) : null}
                <div
                    ref={editorRef}
                    contentEditable={!disabled}
                    suppressContentEditableWarning
                    onInput={handleInput}
                    onBlur={handleInput}
                    onFocus={syncSelection}
                    onKeyUp={syncSelection}
                    onMouseUp={syncSelection}
                    className={cn(
                        "min-h-72 px-4 py-3 text-sm outline-none",
                        "[&_blockquote]:border-l-4 [&_blockquote]:border-border [&_blockquote]:pl-3 [&_blockquote]:italic",
                        "[&_h1]:mb-3 [&_h1]:text-3xl [&_h1]:font-bold [&_h2]:mb-3 [&_h2]:text-2xl [&_h2]:font-semibold",
                        "[&_h3]:mb-2 [&_h3]:text-xl [&_h3]:font-semibold [&_img]:my-4 [&_img]:max-w-full [&_img]:rounded-xl [&_img]:border",
                        "[&_ol]:list-decimal [&_ol]:pl-6 [&_p]:mb-3 [&_p:last-child]:mb-0 [&_strong]:font-semibold [&_ul]:list-disc [&_ul]:pl-6",
                        disabled && "pointer-events-none opacity-70"
                    )}
                />
            </div>
        </div>
    )
}
