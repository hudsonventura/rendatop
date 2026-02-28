"use client"

import { zodResolver } from "@hookform/resolvers/zod"
import { useForm } from "react-hook-form"
import { z } from "zod"
import { BaseLayout } from "@/components/layouts/base-layout"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Input } from "@/components/ui/input"
import {
    Form,
    FormControl,
    FormDescription,
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
import { Button } from "@/components/ui/button"
import { Checkbox } from "@/components/ui/checkbox"
import { Separator } from "@/components/ui/separator"
import { BadgeDollarSign, CalendarIcon, Landmark, PercentIcon, ShieldCheck, TrendingUp } from "lucide-react"

const investimentoFormSchema = z.object({
    banco: z.string().min(1, "O banco é obrigatório"),
    valor: z.string().min(1, "O valor é obrigatório").refine(
        (val) => {
            const num = parseFloat(val.replace(/\./g, "").replace(",", "."))
            return !isNaN(num) && num > 0
        },
        { message: "Informe um valor válido maior que zero" }
    ),
    indexador: z.enum(["cdi", "ipca", "prefixado"], {
        message: "Selecione o indexador",
    }),
    taxaPercentual: z.string().min(1, "A taxa é obrigatória").refine(
        (val) => {
            const num = parseFloat(val.replace(",", "."))
            return !isNaN(num) && num > 0
        },
        { message: "Informe uma taxa válida" }
    ),
    isentoImposto: z.boolean(),
    dataInvestimento: z.string().min(1, "A data de investimento é obrigatória"),
    dataVencimento: z.string(),
    liquidezDiaria: z.boolean(),
}).superRefine((data, ctx) => {
    if (!data.liquidezDiaria && !data.dataVencimento) {
        ctx.addIssue({
            code: z.ZodIssueCode.custom,
            message: "A data de vencimento é obrigatória se não houver liquidez diária",
            path: ["dataVencimento"],
        })
    }

    if (data.dataInvestimento && data.dataVencimento) {
        if (new Date(data.dataVencimento) <= new Date(data.dataInvestimento)) {
            ctx.addIssue({
                code: z.ZodIssueCode.custom,
                message: "A data de vencimento deve ser posterior à data de investimento",
                path: ["dataVencimento"],
            })
        }
    }
})

type InvestimentoFormValues = z.infer<typeof investimentoFormSchema>



function formatCurrency(value: string): string {
    // Remove tudo que não é número
    const numbers = value.replace(/\D/g, "")
    if (!numbers) return ""

    // Converte para centavos
    const amount = parseInt(numbers, 10)
    const formatted = (amount / 100).toLocaleString("pt-BR", {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2,
    })

    return formatted
}

export default function NovoInvestimentoPage() {
    const form = useForm<InvestimentoFormValues>({
        resolver: zodResolver(investimentoFormSchema),
        defaultValues: {
            banco: "",
            valor: "",
            indexador: undefined,
            taxaPercentual: "",
            isentoImposto: false,
            dataInvestimento: "",
            dataVencimento: "",
            liquidezDiaria: false,
        },
    })

    const indexadorSelecionado = form.watch("indexador")
    const liquidezDiariaSelecionada = form.watch("liquidezDiaria")

    function onSubmit(data: InvestimentoFormValues) {
        // Converter valor formatado para número
        const valorNumerico = parseFloat(
            data.valor.replace(/\./g, "").replace(",", ".")
        )
        const taxaNumerico = parseFloat(data.taxaPercentual.replace(",", "."))

        const payload = {
            ...data,
            valor: valorNumerico,
            taxaPercentual: taxaNumerico,
        }

        console.log("Investimento cadastrado:", payload)
        // Aqui você faria a chamada para a API
    }

    function getIndexadorSuffix(): string {
        switch (indexadorSelecionado) {
            case "cdi":
                return "% do CDI"
            case "ipca":
                return "% a.a."
            case "prefixado":
                return "% a.a."
            default:
                return "%"
        }
    }

    return (
        <BaseLayout title="Novo Investimento" description="Cadastre um novo investimento de renda fixa">
            <div className="space-y-6 px-4 lg:px-6">
                <Form {...form}>
                    <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-6">
                        {/* Dados do Investimento */}
                        <Card>
                            <CardHeader>
                                <div className="flex items-center gap-2">
                                    <Landmark className="size-5 text-primary" />
                                    <CardTitle>Dados do Investimento</CardTitle>
                                </div>
                                <CardDescription>
                                    Informe a instituição financeira e o valor do aporte.
                                </CardDescription>
                            </CardHeader>
                            <CardContent className="space-y-4">
                                <FormField
                                    control={form.control}
                                    name="banco"
                                    render={({ field }) => (
                                        <FormItem>
                                            <FormLabel>Banco / Corretora</FormLabel>
                                            <FormControl>
                                                <Input
                                                    placeholder="Ex: Nubank, XP, Inter, BTG..."
                                                    {...field}
                                                />
                                            </FormControl>
                                            <FormMessage />
                                        </FormItem>
                                    )}
                                />
                                <FormField
                                    control={form.control}
                                    name="valor"
                                    render={({ field }) => (
                                        <FormItem>
                                            <FormLabel>Valor Investido (R$)</FormLabel>
                                            <FormControl>
                                                <div className="relative">
                                                    <span className="absolute left-3 top-1/2 -translate-y-1/2 text-muted-foreground text-sm font-medium">
                                                        R$
                                                    </span>
                                                    <Input
                                                        placeholder="0,00"
                                                        className="pl-10"
                                                        {...field}
                                                        onChange={(e) => {
                                                            const formatted = formatCurrency(e.target.value)
                                                            field.onChange(formatted)
                                                        }}
                                                    />
                                                </div>
                                            </FormControl>
                                            <FormMessage />
                                        </FormItem>
                                    )}
                                />
                            </CardContent>
                        </Card>

                        {/* Rentabilidade */}
                        <Card>
                            <CardHeader>
                                <div className="flex items-center gap-2">
                                    <TrendingUp className="size-5 text-primary" />
                                    <CardTitle>Rentabilidade</CardTitle>
                                </div>
                                <CardDescription>
                                    Selecione o tipo de indexador e a taxa contratada.
                                </CardDescription>
                            </CardHeader>
                            <CardContent className="space-y-4">
                                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                                    <FormField
                                        control={form.control}
                                        name="indexador"
                                        render={({ field }) => (
                                            <FormItem>
                                                <FormLabel>Indexador</FormLabel>
                                                <Select
                                                    onValueChange={field.onChange}
                                                    defaultValue={field.value}
                                                >
                                                    <FormControl>
                                                        <SelectTrigger className="w-full">
                                                            <SelectValue placeholder="Selecione o indexador" />
                                                        </SelectTrigger>
                                                    </FormControl>
                                                    <SelectContent>
                                                        <SelectItem value="cdi">
                                                            <div className="flex items-center gap-2">
                                                                <PercentIcon className="size-4" />
                                                                <span>% do CDI</span>
                                                            </div>
                                                        </SelectItem>
                                                        <SelectItem value="ipca">
                                                            <div className="flex items-center gap-2">
                                                                <TrendingUp className="size-4" />
                                                                <span>IPCA + %</span>
                                                            </div>
                                                        </SelectItem>
                                                        <SelectItem value="prefixado">
                                                            <div className="flex items-center gap-2">
                                                                <BadgeDollarSign className="size-4" />
                                                                <span>Prefixado (% ao ano)</span>
                                                            </div>
                                                        </SelectItem>
                                                    </SelectContent>
                                                </Select>
                                                <FormMessage />
                                            </FormItem>
                                        )}
                                    />
                                    <FormField
                                        control={form.control}
                                        name="taxaPercentual"
                                        render={({ field }) => (
                                            <FormItem>
                                                <FormLabel>
                                                    Taxa {indexadorSelecionado ? `(${getIndexadorSuffix()})` : ""}
                                                </FormLabel>
                                                <FormControl>
                                                    <div className="relative">
                                                        <Input
                                                            type="text"
                                                            inputMode="decimal"
                                                            placeholder={
                                                                indexadorSelecionado === "cdi"
                                                                    ? "Ex: 110"
                                                                    : indexadorSelecionado === "ipca"
                                                                        ? "Ex: 6,5"
                                                                        : "Ex: 13,5"
                                                            }
                                                            {...field}
                                                        />
                                                        {indexadorSelecionado && (
                                                            <span className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground text-xs">
                                                                {getIndexadorSuffix()}
                                                            </span>
                                                        )}
                                                    </div>
                                                </FormControl>
                                                <FormDescription>
                                                    {indexadorSelecionado === "cdi" && "Percentual do CDI (ex: 110 para 110% do CDI)"}
                                                    {indexadorSelecionado === "ipca" && "Taxa acima da inflação (ex: 6,5 para IPCA + 6,5%)"}
                                                    {indexadorSelecionado === "prefixado" && "Taxa fixa ao ano (ex: 13,5 para 13,5% a.a.)"}
                                                </FormDescription>
                                                <FormMessage />
                                            </FormItem>
                                        )}
                                    />
                                </div>
                            </CardContent>
                        </Card>

                        {/* Datas e Condições */}
                        <Card>
                            <CardHeader>
                                <div className="flex items-center gap-2">
                                    <CalendarIcon className="size-5 text-primary" />
                                    <CardTitle>Datas e Condições</CardTitle>
                                </div>
                                <CardDescription>
                                    Defina as datas e condições do investimento.
                                </CardDescription>
                            </CardHeader>
                            <CardContent className="space-y-4">
                                <div className="space-y-4">
                                    <FormField
                                        control={form.control}
                                        name="liquidezDiaria"
                                        render={({ field }) => (
                                            <FormItem className="flex flex-row items-start space-x-3 space-y-0 rounded-md border p-4">
                                                <FormControl>
                                                    <Checkbox
                                                        checked={field.value}
                                                        onCheckedChange={(checked) => {
                                                            field.onChange(checked)
                                                            if (checked) {
                                                                form.setValue("dataVencimento", "")
                                                                form.clearErrors("dataVencimento")
                                                            }
                                                        }}
                                                    />
                                                </FormControl>
                                                <div className="space-y-1 leading-none">
                                                    <FormLabel className="cursor-pointer">
                                                        Liquidez Diária
                                                    </FormLabel>
                                                    <FormDescription>
                                                        Marque se o investimento permite resgate a qualquer momento.
                                                    </FormDescription>
                                                </div>
                                            </FormItem>
                                        )}
                                    />

                                    <FormField
                                        control={form.control}
                                        name="isentoImposto"
                                        render={({ field }) => (
                                            <FormItem className="flex flex-row items-start space-x-3 space-y-0 rounded-md border p-4">
                                                <FormControl>
                                                    <Checkbox
                                                        checked={field.value}
                                                        onCheckedChange={field.onChange}
                                                    />
                                                </FormControl>
                                                <div className="space-y-1 leading-none">
                                                    <FormLabel className="cursor-pointer">
                                                        <div className="flex items-center gap-2">
                                                            <ShieldCheck className="size-4 text-green-600" />
                                                            Isento de Imposto de Renda
                                                        </div>
                                                    </FormLabel>
                                                    <FormDescription>
                                                        Marque para investimentos isentos de IR (ex: LCI, LCA, CRI, CRA, debêntures incentivadas).
                                                    </FormDescription>
                                                </div>
                                            </FormItem>
                                        )}
                                    />
                                </div>

                                <Separator />

                                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                                    <FormField
                                        control={form.control}
                                        name="dataInvestimento"
                                        render={({ field }) => (
                                            <FormItem>
                                                <FormLabel>Data do Investimento</FormLabel>
                                                <FormControl>
                                                    <Input type="date" {...field} />
                                                </FormControl>
                                                <FormMessage />
                                            </FormItem>
                                        )}
                                    />
                                    <FormField
                                        control={form.control}
                                        name="dataVencimento"
                                        render={({ field }) => (
                                            <FormItem>
                                                <FormLabel>Data de Vencimento</FormLabel>
                                                <FormControl>
                                                    <Input type="date" disabled={!!liquidezDiariaSelecionada} {...field} />
                                                </FormControl>
                                                <FormMessage />
                                            </FormItem>
                                        )}
                                    />
                                </div>
                            </CardContent>
                        </Card>

                        {/* Ações */}
                        <div className="flex gap-3">
                            <Button type="submit" className="cursor-pointer">
                                <BadgeDollarSign className="size-4 mr-2" />
                                Cadastrar Investimento
                            </Button>
                            <Button
                                variant="outline"
                                type="reset"
                                className="cursor-pointer"
                                onClick={() => form.reset()}
                            >
                                Limpar
                            </Button>
                        </div>
                    </form>
                </Form>
            </div>
        </BaseLayout>
    )
}
