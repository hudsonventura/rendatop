import { useEffect, useRef } from "react"
import { Bold, Italic, Link2, List, ListOrdered, Quote, Strikethrough, Underline } from "lucide-react"
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

export default function SupportRichTextEditor({
    value,
    onChange,
    onPasteFiles,
    disabled = false,
    placeholder = "Escreva sua mensagem...",
    className,
}) {
    const editorRef = useRef(null)

    useEffect(() => {
        const editor = editorRef.current
        if (!editor) return

        const nextValue = normalizeEditorHtml(value)
        const currentValue = normalizeEditorHtml(editor.innerHTML)

        if (currentValue !== nextValue) {
            editor.innerHTML = nextValue
        }
    }, [value])

    const runCommand = (command, commandValue) => {
        if (disabled) return

        editorRef.current?.focus()
        document.execCommand(command, false, commandValue)
        onChange(editorRef.current?.innerHTML || "")
    }

    const handleCreateLink = () => {
        if (disabled) return

        const link = window.prompt("Informe a URL do link")
        if (!link) return

        runCommand("createLink", link)
    }

    const handleInput = () => {
        onChange(editorRef.current?.innerHTML || "")
    }

    const handlePaste = (event) => {
        const items = Array.from(event.clipboardData?.items || [])
        const files = items
            .filter((item) => item.kind === "file")
            .map((item) => item.getAsFile())
            .filter(Boolean)

        if (files.length > 0) {
            event.preventDefault()
            onPasteFiles?.(files)
        }
    }

    const isEmpty = !normalizeEditorHtml(value).replace(/<[^>]+>/g, "").trim()

    return (
        <div className={cn("rounded-xl border bg-background", className)}>
            <div className="flex flex-wrap gap-2 border-b px-3 py-2">
                {TOOLBAR_ACTIONS.map((action) => (
                    <Button
                        key={action.label}
                        type="button"
                        variant="outline"
                        size="sm"
                        className="h-8 px-2"
                        disabled={disabled}
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
                    disabled={disabled}
                    onClick={handleCreateLink}
                >
                    <Link2 className="size-4" />
                    <span className="sr-only">Inserir link</span>
                </Button>
            </div>

            <div className="relative">
                {isEmpty && (
                    <div className="text-muted-foreground pointer-events-none absolute left-4 top-3 text-sm">
                        {placeholder}
                    </div>
                )}
                <div
                    ref={editorRef}
                    contentEditable={!disabled}
                    suppressContentEditableWarning
                    onInput={handleInput}
                    onBlur={handleInput}
                    onPaste={handlePaste}
                    className={cn(
                        "min-h-40 rounded-b-xl px-4 py-3 text-sm outline-none",
                        "disabled:cursor-not-allowed",
                        "[&_blockquote]:border-l-4 [&_blockquote]:border-border [&_blockquote]:pl-3 [&_blockquote]:italic",
                        "[&_ol]:list-decimal [&_ol]:pl-6 [&_p]:mb-2 [&_p:last-child]:mb-0 [&_strong]:font-semibold [&_ul]:list-disc [&_ul]:pl-6",
                        disabled && "pointer-events-none opacity-70"
                    )}
                />
            </div>
        </div>
    )
}
