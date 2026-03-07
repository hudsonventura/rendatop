import React, { useEffect, useState } from 'react';
import { useForm } from "react-hook-form"
import { Checkbox } from "@/components/ui/checkbox"
import { cn } from "@/lib/utils"

import {
    Dialog,
    DialogContent,
    DialogDescription,
    DialogHeader,
    DialogTitle,
    DialogTrigger,
    DialogFooter
} from "@/components/ui/dialog"

import {
    Form,
    FormControl,
    FormField,
    FormItem,
    FormLabel,
    FormMessage,
} from "@/components/ui/form"

import {
    Select,
    SelectContent,
    SelectItem,
    SelectTrigger,
    SelectValue,
} from "@/components/ui/select"

import { Check, ChevronsUpDown, Pencil } from "lucide-react"

import {
    Command,
    CommandEmpty,
    CommandGroup,
    CommandInput,
    CommandItem,
    CommandList,
} from "@/components/ui/command"

import {
    Popover,
    PopoverContent,
    PopoverTrigger,
} from "@/components/ui/popover"

import { Input } from "@/components/ui/input"
import { Button } from "@/components/ui/button"
import Calendario from "@/components/Calendario"

import { zodResolver } from "@hookform/resolvers/zod"
import { z } from "zod"
import axiosInstance from "../utils/axiosConfig"

// ── Schema (same as InvestmentsAdd) ─────────────────────────────────────────

const formSchema = z.object({
    title: z.string().min(1, { required_error: "Este campo é obrigatório." }),
    date_buy: z.date({ required_error: "Campo obrigatório" }),
    due_date: z.date().nullable().optional(),
    liquidez_diaria: z.boolean().optional(),
    taxes: z.boolean().optional(),
    bank: z.string({ required_error: "Selecione ou crie um banco" }),
    value: z
        .string()
        .min(1, { message: "O valor deve ser um número decimal.", required_error: "Este campo é obrigatório." })
        .transform((value) => value.replace(/\./g, ""))
        .transform((value) => value.replace(/\,/g, ".")),
    index: z.preprocess(
        (value) => (value === "" ? undefined : parseInt(value, 10)),
        z.number().int().nonnegative({ required_error: "Por favor selecione um index" })
    ),
    index_percent: z
        .string()
        .min(1, { required_error: "Este campo é obrigatório." })
        .transform((value) => value.replace(/\./g, ""))
        .transform((value) => value.replace(/\,/g, ".")),
})

// ── Index enum mapping (backend string → select number) ──────────────────────

const INDEX_MAP = { CDI: "0", IPCA_MAIS: "1", PERCENT_YEAR: "2" }

// ── Decimal formatter (same as InvestmentsAdd) ────────────────────────────────

function formatDecimalDisplay(num) {
    // Convert number to pt-BR display string: "1234.56" → "1.234,56"
    const [int, dec = ""] = String(num).split(".")
    const intFormatted = int.replace(/\B(?=(\d{3})+(?!\d))/g, ".")
    return dec ? `${intFormatted},${dec.substring(0, 2)}` : intFormatted
}

// ── Component ─────────────────────────────────────────────────────────────────

const InvestmentsEdit = ({ investment, setReload }) => {
    const [isOpen, setIsOpen] = useState(false)
    const [bankList, setBankList] = useState([])
    const [popbank, setPopbank] = useState(false)
    const [liquidez_diaria, setLiquidezDiaria] = useState(!investment.due_date)

    useEffect(() => {
        axiosInstance
            .get("/Banks")
            .then((response) => setBankList(response.data))
            .catch(() => { })
    }, [])

    const form = useForm({
        resolver: zodResolver(formSchema),
        defaultValues: {
            title: investment.title,
            date_buy: new Date(investment.date_buy),
            due_date: investment.due_date
                ? new Date(investment.due_date)
                : undefined,
            liquidez_diaria: !investment.due_date,
            taxes: investment.taxes ?? true,
            bank: investment.bank,
            value: formatDecimalDisplay(investment.value),
            index: INDEX_MAP[investment.index] ?? "0",
            index_percent: formatDecimalDisplay(investment.index_percent),
        },
    })

    function onSubmit(values) {
        axiosInstance
            .patch(`/Investments/${investment.id}`, JSON.stringify(values))
            .then(() => {
                setIsOpen(false)
                setReload(Math.floor(Math.random() * 10000) + 1)
            })
            .catch(() => { })
    }

    const handleInputChangeDecimal = (event) => {
        const input = event.target
        let value = input.value
        value = value.replace(/[^\d,]/g, "")
        const parts = value.split(",")
        const intPart = parts[0].replace(/\B(?=(\d{3})+(?!\d))/g, ".")
        const decimalPart = parts[1] ? parts[1].substring(0, 2) : ""
        input.value = parts.length > 1 ? `${intPart},${decimalPart}` : intPart
        form.setValue(input.name, input.value)
    }

    const CreateBank = (e) => {
        if (e.key === "Enter") {
            form.setValue("bank", e.target.value)
        }
    }

    return (
        <Dialog open={isOpen}>
            <DialogTrigger asChild>
                <Button
                    type="button"
                    variant="outline"
                    size="sm"
                    onClick={() => setIsOpen(true)}
                >
                    <Pencil className="h-4 w-4 mr-1" />
                    Editar
                </Button>
            </DialogTrigger>

            <DialogContent className="max-w-4xl w-[90vw] md:w-[60vw] max-h-[90vh] overflow-y-auto">
                <DialogHeader>
                    <DialogTitle>Editando investimento</DialogTitle>
                    <DialogDescription>Altere os dados do investimento abaixo.</DialogDescription>
                </DialogHeader>

                <Form {...form}>
                    <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-6">

                        {/* Title */}
                        <FormField
                            control={form.control}
                            name="title"
                            render={({ field }) => (
                                <FormItem>
                                    <FormLabel>Identificação do investimento</FormLabel>
                                    <FormControl>
                                        <Input placeholder="Informe um título para o seu investimento" {...field} />
                                    </FormControl>
                                    <FormMessage />
                                </FormItem>
                            )}
                        />

                        {/* Dates + checkboxes row */}
                        <div className="flex flex-wrap gap-4">
                            <FormField
                                control={form.control}
                                name="date_buy"
                                render={({ field }) => (
                                    <FormItem>
                                        <FormLabel>Data da aplicação</FormLabel>
                                        <FormControl>
                                            <Calendario field={field} />
                                        </FormControl>
                                        <FormMessage />
                                    </FormItem>
                                )}
                            />
                            <FormField
                                control={form.control}
                                name="due_date"
                                render={({ field }) => (
                                    <FormItem>
                                        <FormLabel>Data do vencimento / resgate</FormLabel>
                                        <FormControl>
                                            <Calendario field={field} disabled={liquidez_diaria} />
                                        </FormControl>
                                        <FormMessage />
                                    </FormItem>
                                )}
                            />
                            <FormField
                                control={form.control}
                                name="liquidez_diaria"
                                render={({ field }) => (
                                    <FormItem className="flex flex-col justify-end space-y-2">
                                        <div className="flex items-center">
                                            <FormControl>
                                                <Checkbox
                                                    checked={field.value || false}
                                                    onCheckedChange={(checked) => {
                                                        field.onChange(checked)
                                                        setLiquidezDiaria(checked)
                                                    }}
                                                    className="bg-background rounded border border-input"
                                                />
                                            </FormControl>
                                            <span className="ml-2 text-sm">Liquidez diária</span>
                                        </div>
                                    </FormItem>
                                )}
                            />
                            <FormField
                                control={form.control}
                                name="taxes"
                                render={({ field }) => (
                                    <FormItem className="flex flex-col justify-end space-y-2">
                                        <div className="flex items-center">
                                            <FormControl>
                                                <Checkbox
                                                    checked={field.value ?? true}
                                                    onCheckedChange={field.onChange}
                                                    className="bg-background rounded border border-input"
                                                />
                                            </FormControl>
                                            <span className="ml-2 text-sm">Possui incidência de impostos</span>
                                        </div>
                                    </FormItem>
                                )}
                            />
                        </div>

                        {/* Bank + Value + Index row */}
                        <div className="flex flex-wrap gap-4">

                            {/* Bank combobox */}
                            <FormField
                                control={form.control}
                                name="bank"
                                render={({ field }) => (
                                    <FormItem className="flex flex-col">
                                        <FormLabel>Banco</FormLabel>
                                        <Popover open={popbank} onOpenChange={(open) => setPopbank(open)}>
                                            <PopoverTrigger asChild>
                                                <FormControl>
                                                    <Button
                                                        variant="outline"
                                                        role="combobox"
                                                        className={cn("w-[200px] justify-between", !field.value && "text-muted-foreground")}
                                                    >
                                                        {field.value ? field.value : "Selecione seu banco"}
                                                        <ChevronsUpDown className="ml-2 h-4 w-4 shrink-0 opacity-50" />
                                                    </Button>
                                                </FormControl>
                                            </PopoverTrigger>
                                            <PopoverContent className="w-[200px] p-0">
                                                <Command>
                                                    <CommandInput
                                                        placeholder="Buscar ou criar"
                                                        onKeyDown={(event) => {
                                                            if (event.key === "Enter") {
                                                                event.preventDefault()
                                                                event.stopPropagation()
                                                                setPopbank(false)
                                                                CreateBank(event)
                                                            }
                                                        }}
                                                    />
                                                    <CommandList>
                                                        <CommandEmpty>ENTER para criar novo banco</CommandEmpty>
                                                        <CommandGroup>
                                                            {bankList.map((bank) => (
                                                                <CommandItem
                                                                    value={bank}
                                                                    key={bank}
                                                                    onSelect={() => {
                                                                        form.setValue("bank", bank)
                                                                        setPopbank(false)
                                                                    }}
                                                                >
                                                                    {bank}
                                                                    <Check
                                                                        className={cn("ml-auto", bank === field.value ? "opacity-100" : "opacity-0")}
                                                                    />
                                                                </CommandItem>
                                                            ))}
                                                        </CommandGroup>
                                                    </CommandList>
                                                </Command>
                                            </PopoverContent>
                                        </Popover>
                                        <FormMessage />
                                    </FormItem>
                                )}
                            />

                            {/* Value */}
                            <FormField
                                control={form.control}
                                name="value"
                                render={({ field }) => (
                                    <FormItem className="w-32">
                                        <FormLabel>Valor investido</FormLabel>
                                        <FormControl>
                                            <Input
                                                placeholder="Ex.: 3.999,99"
                                                {...field}
                                                onChange={handleInputChangeDecimal}
                                            />
                                        </FormControl>
                                        <FormMessage />
                                    </FormItem>
                                )}
                            />

                            {/* Index */}
                            <FormField
                                control={form.control}
                                name="index"
                                render={({ field }) => (
                                    <FormItem className="w-64">
                                        <FormLabel>Indexador (CDI, IPCA+ ou %a.a.)</FormLabel>
                                        <Select onValueChange={field.onChange} defaultValue={String(field.value)}>
                                            <FormControl>
                                                <SelectTrigger>
                                                    <SelectValue placeholder="Selecione o index" />
                                                </SelectTrigger>
                                            </FormControl>
                                            <SelectContent>
                                                <SelectItem value="0">CDI</SelectItem>
                                                <SelectItem value="1">IPCA+</SelectItem>
                                                <SelectItem value="2">%a.a.</SelectItem>
                                            </SelectContent>
                                        </Select>
                                        <FormMessage />
                                    </FormItem>
                                )}
                            />

                            {/* Index percent */}
                            <FormField
                                control={form.control}
                                name="index_percent"
                                render={({ field }) => (
                                    <FormItem className="w-32">
                                        <FormLabel>Valor do indexado</FormLabel>
                                        <FormControl>
                                            <Input
                                                placeholder="Ex.: 108% ou 13,11%"
                                                {...field}
                                                onChange={handleInputChangeDecimal}
                                                className="text-sm"
                                            />
                                        </FormControl>
                                        <FormMessage />
                                    </FormItem>
                                )}
                            />
                        </div>

                        <DialogFooter className="flex justify-between pt-4">
                            <Button type="button" variant="outline" onClick={() => setIsOpen(false)}>
                                Cancelar
                            </Button>
                            <Button type="submit">
                                Salvar alterações
                            </Button>
                        </DialogFooter>
                    </form>
                </Form>
            </DialogContent>
        </Dialog>
    )
}

export default InvestmentsEdit
