import { useEffect, useMemo, useRef, useState } from "react"
import { Check, ChevronsUpDown, Search } from "lucide-react"

import { cn } from "@/lib/utils"
import { Button } from "@/components/ui/button"

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
    const [open, setOpen] = useState(false)
    const [query, setQuery] = useState("")

    const selectedBank = useMemo(
        () => banks.find((bank) => bank.code === value) ?? null,
        [banks, value]
    )

    const filteredBanks = useMemo(() => {
        const normalizedQuery = normalizeSearch(query)
        if (!normalizedQuery) return banks

        return banks.filter((bank) => {
            const label = formatBankLabel(bank)
            const searchText = `${label} ${bank.name ?? ""} ${bank.code ?? ""}`
            return normalizeSearch(searchText).includes(normalizedQuery)
        })
    }, [banks, query])

    useEffect(() => {
        if (!open) return

        const frame = requestAnimationFrame(() => {
            inputRef.current?.focus()
        })

        return () => cancelAnimationFrame(frame)
    }, [open])

    useEffect(() => {
        const handlePointerDown = (event) => {
            if (!rootRef.current?.contains(event.target)) {
                setOpen(false)
                setQuery("")
            }
        }

        document.addEventListener("pointerdown", handlePointerDown)
        return () => document.removeEventListener("pointerdown", handlePointerDown)
    }, [])

    const handleSelect = (bank) => {
        onChange(bank.code)
        setOpen(false)
        setQuery("")
    }

    return (
        <div ref={rootRef} className={cn("relative w-[280px]", className)}>
            <Button
                type="button"
                variant="outline"
                role="combobox"
                aria-expanded={open}
                className="w-full justify-between font-normal"
                onClick={() => setOpen((current) => !current)}
            >
                <span className={cn("truncate", !selectedBank && "text-muted-foreground")}>
                    {selectedBank ? formatBankLabel(selectedBank) : placeholder}
                </span>
                <ChevronsUpDown className="h-4 w-4 shrink-0 opacity-50" />
            </Button>

            {open ? (
                <div className="absolute top-full left-0 z-[80] mt-2 w-full overflow-hidden rounded-md border bg-popover text-popover-foreground shadow-md pointer-events-auto">
                    <div className="flex items-center gap-2 border-b px-3">
                        <Search className="h-4 w-4 shrink-0 opacity-50" />
                        <input
                            ref={inputRef}
                            type="text"
                            value={query}
                            onChange={(event) => setQuery(event.target.value)}
                            placeholder="Busque por código ou nome"
                            className="placeholder:text-muted-foreground h-10 w-full bg-transparent text-sm outline-none"
                        />
                    </div>

                    <div className="max-h-[300px] overflow-y-auto p-1">
                        {filteredBanks.length === 0 ? (
                            <div className="py-6 text-center text-sm text-muted-foreground">
                                Nenhum banco encontrado.
                            </div>
                        ) : (
                            filteredBanks.map((bank) => {
                                const selected = bank.code === value

                                return (
                                    <button
                                        key={bank.code}
                                        type="button"
                                        className="hover:bg-accent hover:text-accent-foreground flex w-full items-center gap-2 rounded-sm px-2 py-1.5 text-left text-sm"
                                        onClick={() => handleSelect(bank)}
                                    >
                                        <Check className={cn("h-4 w-4 shrink-0", selected ? "opacity-100" : "opacity-0")} />
                                        <span>{formatBankLabel(bank)}</span>
                                    </button>
                                )
                            })
                        )}
                    </div>
                </div>
            ) : null}
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
