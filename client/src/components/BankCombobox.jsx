import { useEffect, useMemo, useRef, useState } from "react"
import { createPortal } from "react-dom"
import { Check, ChevronsUpDown } from "lucide-react"

import { cn } from "@/lib/utils"

function formatBankLabel(bank) {
    return `${String(bank.code ?? "").padStart(3, "0")} - ${bank.name}`
}

export default function BankCombobox({
    banks,
    value,
    onChange,
    placeholder = "Selecione seu banco",
    className,
}) {
    const rootRef = useRef(null)
    const inputRef = useRef(null)
    const menuRef = useRef(null)
    const [open, setOpen] = useState(false)
    const [editing, setEditing] = useState(false)
    const [query, setQuery] = useState("")
    const [menuPosition, setMenuPosition] = useState({ top: 0, left: 0, width: 280 })

    const selectedBank = useMemo(
        () => banks.find((bank) => bank.code === value) ?? null,
        [banks, value]
    )

    const bankOptions = useMemo(
        () =>
            banks.map((bank) => {
                const label = formatBankLabel(bank)
                return {
                    bank,
                    label,
                    searchText: `${label} ${bank.name ?? ""} ${bank.code ?? ""}`.toLowerCase(),
                }
            }),
        [banks]
    )

    const filteredBanks = useMemo(() => {
        const normalizedQuery = normalizeSearch(query)
        if (!normalizedQuery) return bankOptions

        return bankOptions.filter((option) => normalizeSearch(option.searchText).includes(normalizedQuery))
    }, [bankOptions, query])

    useEffect(() => {
        const handlePointerDown = (event) => {
            if (!rootRef.current?.contains(event.target) && !menuRef.current?.contains(event.target)) {
                setOpen(false)
                setEditing(false)
                setQuery("")
            }
        }

        document.addEventListener("pointerdown", handlePointerDown)
        return () => document.removeEventListener("pointerdown", handlePointerDown)
    }, [])

    useEffect(() => {
        if (!open) return

        const updateMenuPosition = () => {
            const rect = rootRef.current?.getBoundingClientRect()
            if (!rect) return

            setMenuPosition({
                top: rect.bottom + 4,
                left: rect.left,
                width: rect.width,
            })
        }

        updateMenuPosition()
        window.addEventListener("scroll", updateMenuPosition, true)
        window.addEventListener("resize", updateMenuPosition)

        return () => {
            window.removeEventListener("scroll", updateMenuPosition, true)
            window.removeEventListener("resize", updateMenuPosition)
        }
    }, [open])

    const openForSearch = () => {
        setEditing(true)
        setOpen(true)
        setQuery("")
        requestAnimationFrame(() => inputRef.current?.select())
    }

    const selectBank = (bank) => {
        onChange(bank?.code)
        setOpen(false)
        setEditing(false)
        setQuery("")
        inputRef.current?.blur()
    }

    const inputValue = editing
        ? query
        : selectedBank
            ? formatBankLabel(selectedBank)
            : ""

    const options = open ? createPortal(
        <div
            ref={menuRef}
            className="fixed z-[1000] max-h-60 overflow-auto rounded-md border bg-popover p-1 text-sm text-popover-foreground shadow-md"
            style={{
                top: menuPosition.top,
                left: menuPosition.left,
                width: menuPosition.width,
            }}
        >
            {filteredBanks.length === 0 ? (
                <div className="px-3 py-2 text-sm text-muted-foreground">
                    Nenhum banco encontrado.
                </div>
            ) : (
                filteredBanks.map(({ bank, label }) => {
                    const selected = bank.code === value

                    return (
                        <button
                            key={bank.id}
                            type="button"
                            className="relative flex w-full cursor-default items-center rounded-sm py-2 pl-9 pr-3 text-left hover:bg-accent hover:text-accent-foreground"
                            onMouseDown={(event) => event.preventDefault()}
                            onClick={() => selectBank(bank)}
                        >
                            <span className={cn("block truncate", selected && "font-medium")}>
                                {label}
                            </span>
                            <span
                                className={cn(
                                    "absolute inset-y-0 left-0 flex items-center pl-3 text-primary",
                                    !selected && "opacity-0"
                                )}
                            >
                                <Check className="h-4 w-4" aria-hidden="true" />
                            </span>
                        </button>
                    )
                })
            )}
        </div>,
        document.body
    ) : null

    return (
        <div ref={rootRef} className={cn("relative", className)}>
            <div className="relative w-[280px] overflow-hidden rounded-md border border-input bg-background text-left shadow-sm focus-within:ring-2 focus-within:ring-ring focus-within:ring-offset-2">
                <input
                    ref={inputRef}
                    type="text"
                    role="combobox"
                    aria-expanded={open}
                    aria-autocomplete="list"
                    className="w-full border-none bg-transparent py-2 pl-3 pr-10 text-sm text-foreground outline-none placeholder:text-muted-foreground"
                    value={inputValue}
                    onFocus={openForSearch}
                    onInput={(event) => {
                        setEditing(true)
                        setOpen(true)
                        setQuery(event.currentTarget.value)
                    }}
                    onChange={(event) => {
                        setEditing(true)
                        setOpen(true)
                        setQuery(event.target.value)
                    }}
                    onKeyDown={(event) => {
                        if (event.key === "Escape") {
                            setOpen(false)
                            setEditing(false)
                            setQuery("")
                            event.currentTarget.blur()
                        }
                    }}
                    placeholder={placeholder}
                />
                <button
                    type="button"
                    className="absolute inset-y-0 right-0 flex items-center pr-3 text-muted-foreground"
                    onMouseDown={(event) => event.preventDefault()}
                    onClick={() => {
                        if (open) {
                            setOpen(false)
                            setEditing(false)
                            setQuery("")
                            return
                        }

                        inputRef.current?.focus()
                        openForSearch()
                    }}
                    aria-label="Abrir lista de bancos"
                >
                    <ChevronsUpDown className="h-4 w-4" aria-hidden="true" />
                </button>
            </div>

            {options}
        </div>
    )
}

function normalizeSearch(value) {
    return String(value ?? "")
        .normalize("NFD")
        .replace(/\p{Diacritic}/gu, "")
        .replace(/[^a-zA-Z0-9]/g, "")
        .toLowerCase()
}
