import React, { useEffect, useState } from "react"
import { useForm } from "react-hook-form"
import { zodResolver } from "@hookform/resolvers/zod"
import { z } from "zod"
import { HandCoins } from "lucide-react"

import {
    Dialog,
    DialogContent,
    DialogDescription,
    DialogFooter,
    DialogHeader,
    DialogTitle,
    DialogTrigger,
} from "@/components/ui/dialog"
import {
    Form,
    FormControl,
    FormField,
    FormItem,
    FormLabel,
    FormMessage,
} from "@/components/ui/form"
import { Input } from "@/components/ui/input"
import { Button } from "@/components/ui/button"
import Calendario from "@/components/Calendario"
import axiosInstance from "@/utils/axiosConfig"

const formSchema = z.object({
    title: z.string().min(1, { message: "Este campo é obrigatório." }),
    date: z.date({ required_error: "Campo obrigatório" }),
    value: z
        .string()
        .min(1, { message: "Este campo é obrigatório." })
        .transform((value) => value.replace(/\./g, ""))
        .transform((value) => value.replace(/\,/g, "."))
        .refine((value) => Number.isFinite(Number(value)) && Number(value) > 0, {
            message: "Informe um valor maior que zero.",
        }),
})

export default function InvestmentsRedeem({ investment, setReload, externalOpen, onExternalClose }) {
    const [internalOpen, setInternalOpen] = useState(false)
    const isOpen = externalOpen !== undefined ? externalOpen : internalOpen
    const setIsOpen = (value) => {
        if (externalOpen !== undefined) {
            if (!value && onExternalClose) onExternalClose()
            return
        }
        setInternalOpen(value)
    }

    const form = useForm({
        resolver: zodResolver(formSchema),
        defaultValues: {
            title: "",
            date: new Date(),
            value: "",
        },
    })

    useEffect(() => {
        if (!investment || !isOpen) return
        form.reset({
            title: `Resgate - ${investment.title}`,
            date: new Date(),
            value: "",
        })
    }, [form, investment, isOpen])

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

    const onSubmit = (values) => {
        if (!investment) return

        const payload = {
            title: values.title.trim(),
            date: values.date,
            value: Number(values.value),
        }

        axiosInstance
            .put(`/Investments/${investment.id}`, JSON.stringify(payload))
            .then(() => {
                setIsOpen(false)
                setReload(Math.floor(Math.random() * 10000) + 1)
            })
            .catch(() => { })
    }

    const handleFullRedeem = () => {
        if (!investment) return
        const valueLiq = investment?.calculated?.[0]?.value_liq ?? investment.value ?? 0
        const formatted = Number(valueLiq).toLocaleString("pt-BR", {
            minimumFractionDigits: 2,
            maximumFractionDigits: 2,
        })
        form.setValue("value", formatted, { shouldValidate: true, shouldDirty: true })
    }

    return (
        <Dialog open={isOpen} onOpenChange={setIsOpen}>
            {externalOpen === undefined && (
                <DialogTrigger asChild>
                    <Button type="button" variant="secondary" size="sm" onClick={() => setIsOpen(true)}>
                        <HandCoins className="h-4 w-4 mr-1" />
                        Resgatar
                    </Button>
                </DialogTrigger>
            )}

            <DialogContent className="max-w-xl">
                <DialogHeader>
                    <DialogTitle>Criar resgate</DialogTitle>
                    <DialogDescription>
                        Informe os dados do resgate para o investimento <strong>{investment?.title}</strong>.
                    </DialogDescription>
                </DialogHeader>

                <Form {...form}>
                    <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
                        <FormField
                            control={form.control}
                            name="title"
                            render={({ field }) => (
                                <FormItem>
                                    <FormLabel>Identificação do resgate</FormLabel>
                                    <FormControl>
                                        <Input placeholder="Ex.: Resgate parcial" {...field} />
                                    </FormControl>
                                    <FormMessage />
                                </FormItem>
                            )}
                        />

                        <div className="flex flex-wrap items-start gap-4">
                            <FormField
                                control={form.control}
                                name="date"
                                render={({ field }) => (
                                    <FormItem>
                                        <FormLabel>Data do resgate</FormLabel>
                                        <FormControl>
                                            <Calendario field={field} />
                                        </FormControl>
                                        <FormMessage />
                                    </FormItem>
                                )}
                            />
                            <FormField
                                control={form.control}
                                name="value"
                                render={({ field }) => (
                                    <FormItem className="w-[280px]">
                                        <FormLabel>Valor resgatado</FormLabel>
                                        <FormControl>
                                            <div className="flex items-center gap-2">
                                                <Input
                                                    placeholder="Ex.: 1.500,00"
                                                    {...field}
                                                    onChange={handleInputChangeDecimal}
                                                />
                                                <Button type="button" variant="outline" size="sm" onClick={handleFullRedeem}>
                                                    Resgate total
                                                </Button>
                                            </div>
                                        </FormControl>
                                        <FormMessage />
                                    </FormItem>
                                )}
                            />
                        </div>

                        <DialogFooter className="flex justify-between pt-2">
                            <Button type="button" variant="outline" onClick={() => setIsOpen(false)}>
                                Cancelar
                            </Button>
                            <Button type="submit">
                                Confirmar resgate
                            </Button>
                        </DialogFooter>
                    </form>
                </Form>
            </DialogContent>
        </Dialog>
    )
}
