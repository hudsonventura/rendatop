import { useDeferredValue, useMemo, useState } from "react"
import {
    Combobox,
    ComboboxButton,
    ComboboxInput,
    ComboboxOption,
    ComboboxOptions,
} from "@headlessui/react"
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
    const [query, setQuery] = useState("")
    const deferredQuery = useDeferredValue(query)

    const bankOptions = useMemo(
        () =>
            banks.map((bank) => {
                const label = formatBankLabel(bank)
                return {
                    bank,
                    label,
                    searchText: label.toLowerCase(),
                }
            }),
        [banks]
    )

    const selectedBank = useMemo(
        () => banks.find((bank) => bank.code === value) ?? null,
        [banks, value]
    )

    const filteredBanks = useMemo(() => {
        const normalizedQuery = deferredQuery.trim().toLowerCase()
        if (!normalizedQuery) return bankOptions

        return bankOptions.filter((option) => option.searchText.includes(normalizedQuery))
    }, [bankOptions, deferredQuery])

    return (
        <Combobox
            value={selectedBank}
            onChange={(bank) => {
                onChange(bank?.code)
                setQuery("")
            }}
            nullable
        >
            <div className={cn("relative", className)}>
                <div className="relative w-[280px] overflow-hidden rounded-md border border-input bg-background text-left shadow-sm focus-within:ring-2 focus-within:ring-ring focus-within:ring-offset-2">
                    <ComboboxInput
                        className="w-full border-none bg-transparent py-2 pl-3 pr-10 text-sm text-foreground outline-none placeholder:text-muted-foreground"
                        displayValue={(bank) => (bank ? formatBankLabel(bank) : "")}
                        onChange={(event) => setQuery(event.target.value)}
                        placeholder={placeholder}
                    />
                    <ComboboxButton className="absolute inset-y-0 right-0 flex items-center pr-3 text-muted-foreground">
                        <ChevronsUpDown className="h-4 w-4" aria-hidden="true" />
                    </ComboboxButton>
                </div>

                <ComboboxOptions className="absolute z-[70] mt-1 max-h-60 w-[280px] overflow-auto rounded-md border bg-popover p-1 text-sm text-popover-foreground shadow-md focus:outline-none empty:invisible">
                    {filteredBanks.length === 0 ? (
                        <div className="px-3 py-2 text-sm text-muted-foreground">
                            Nenhum banco encontrado.
                        </div>
                    ) : (
                        filteredBanks.map(({ bank, label }) => (
                            <ComboboxOption
                                key={bank.id}
                                value={bank}
                                className={({ focus }) =>
                                    cn(
                                        "relative cursor-default select-none rounded-sm py-2 pl-9 pr-3",
                                        focus && "bg-accent text-accent-foreground"
                                    )
                                }
                            >
                                {({ selected }) => (
                                    <>
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
                                    </>
                                )}
                            </ComboboxOption>
                        ))
                    )}
                </ComboboxOptions>
            </div>
        </Combobox>
    )
}
