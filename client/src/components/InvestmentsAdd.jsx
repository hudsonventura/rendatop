import React, { useEffect, useMemo } from 'react';
import { useState } from "react";

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
	FormDescription
} from "@/components/ui/form"

import {
	Select,
	SelectContent,
	SelectItem,
	SelectTrigger,
	SelectValue,
} from "@/components/ui/select"

import { Check, ChevronsUpDown, Plus, TrendingUp } from "lucide-react"

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
import { Badge } from "@/components/ui/badge"
import Calendario from "@/components/Calendario"

import { zodResolver } from "@hookform/resolvers/zod"
import { z } from "zod"

import axiosInstance from "../utils/axiosConfig";
import { getIrTextClass } from "@/utils/ir-level"


// ── IR / IOF helpers (mirror backend logic) ──────────────────────────────────

function getIRPercent(taxes, days) {
	if (!taxes) return 0
	if (days <= 180) return 22.5
	if (days <= 365) return 20
	if (days <= 730) return 17.5
	return 15
}

function getIOFPercent(days) {
	if (days >= 30) return 0
	return 100 - days * 3.333333333333
}

function formatBRL(v) {
	return v.toLocaleString("pt-BR", { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

// ── Preview Component ─────────────────────────────────────────────────────────

function InvestmentPreview({ form }) {
	const watchValue = form.watch("value")
	const watchIndex = form.watch("index")
	const watchIndexPercent = form.watch("index_percent")
	const watchDateBuy = form.watch("date_buy")
	const watchDueDate = form.watch("due_date")
	const watchTaxes = form.watch("taxes")
	const watchLiquidez = form.watch("liquidez_diaria")

	const SELIC_ANNUAL_ESTIMATE = 0.1315
	const IPCA_ANNUAL_ESTIMATE = 0.045

	const preview = useMemo(() => {
		// Parse value (remove dots, replace comma with dot)
		const rawValue = typeof watchValue === "string"
			? parseFloat(watchValue.replace(/\./g, "").replace(",", "."))
			: Number(watchValue)
		if (!rawValue || rawValue <= 0) return null

		const indexType = parseInt(watchIndex, 10)
		if (isNaN(indexType)) return null

		// Parse index_percent
		const rawPercent = typeof watchIndexPercent === "string"
			? parseFloat(watchIndexPercent.replace(/\./g, "").replace(",", "."))
			: Number(watchIndexPercent)
		if (Number.isNaN(rawPercent) || rawPercent < 0) return null

		if (!watchDateBuy) return null

		const dateBuy = new Date(watchDateBuy)
		const sellDate = watchLiquidez || !watchDueDate ? new Date() : new Date(watchDueDate)

		if (sellDate <= dateBuy) return null

		const days = Math.floor((sellDate - dateBuy) / (1000 * 60 * 60 * 24))
		if (days <= 0) return null

		const taxes = watchTaxes ?? true
		const IR = getIRPercent(taxes, days) / 100
		const IOF = getIOFPercent(days) / 100

		let effectivePercent = 0
		let estimateLabel = null

		// CDI (index=0): uses an estimated annual Selic base
		if (indexType === 0) {
			const annualRate = SELIC_ANNUAL_ESTIMATE * (rawPercent / 100)
			effectivePercent = annualRate / 365 * days
			estimateLabel = "Selic"
		}
		// IPCA+ (index=1): estimated annual IPCA + spread
		else if (indexType === 1) {
			const annualRate = IPCA_ANNUAL_ESTIMATE + (rawPercent / 100)
			effectivePercent = annualRate / 366 * (days - 3)
			estimateLabel = "IPCA"
		}
		// %a.a. (index=2)
		else {
			effectivePercent = (rawPercent / 100) / 366 * (days - 3)
		}

		const profitBrute = rawValue * effectivePercent
		const profitBruteIOF = profitBrute * (1 - IOF)
		const irValue = profitBruteIOF * IR
		const profitLiq = profitBruteIOF * (1 - IR)

		return {
			profitBrute,
			irPercent: IR * 100,
			irValue,
			profitLiq,
			days,
			isEstimate: indexType === 0 || indexType === 1,
			estimateLabel,
		}
	}, [watchValue, watchIndex, watchIndexPercent, watchDateBuy, watchDueDate, watchTaxes, watchLiquidez])

	return (
		<div className="rounded-lg border border-green-500/20 bg-green-500/5 p-4 space-y-3">
			<div className="flex items-center gap-2">
				<TrendingUp className="h-4 w-4 text-green-600 dark:text-green-400" />
				<span className="text-sm font-medium">Simulação do investimento</span>
				{preview?.isEstimate && (
					<Badge variant="outline" className="text-xs text-muted-foreground">
						{preview.estimateLabel} estimado
					</Badge>
				)}
			</div>
			<div className="grid grid-cols-3 gap-4">
				<div className="flex flex-col gap-1">
					<span className="text-xs text-muted-foreground">Rendimento Bruto</span>
					<span className="text-sm font-medium">
						{preview ? `R$ ${formatBRL(preview.profitBrute)}` : "-"}
					</span>
				</div>
				<div className="flex flex-col gap-1">
					<span className="text-xs text-muted-foreground">
						IR {preview ? `(${preview.irPercent.toFixed(1)}%)` : ""}
					</span>
					<span className={`text-sm font-medium ${preview ? getIrTextClass(preview.irPercent) : "text-muted-foreground"}`}>
						{preview ? `- R$ ${formatBRL(preview.irValue)}` : "-"}
					</span>
				</div>
				<div className="flex flex-col gap-1">
					<span className="text-xs text-muted-foreground">Rendimento Líquido</span>
					<div className="flex items-center gap-1.5">
						<span className="text-sm font-semibold text-green-600 dark:text-green-400">
							{preview ? `R$ ${formatBRL(preview.profitLiq)}` : "-"}
						</span>
						{preview && (
							<Badge variant="outline" className="text-xs text-green-600 border-green-200 dark:text-green-400 dark:border-green-800">
								<TrendingUp className="h-3 w-3 mr-0.5" />
								+{(preview.profitLiq / (parseFloat(String(watchValue).replace(/\./g, "").replace(",", ".")) || 1) * 100).toFixed(1)}%
							</Badge>
						)}
					</div>
				</div>
			</div>
			<p className="text-xs text-muted-foreground">
				{preview
					? <>Estimativa para {preview.days} dias. Valores podem variar. Aqui é considerado que o IPCA ou CDI permanecerão os mesmo até a data de vencimento. <br />A taxa de juros pode variar durante o período conforme os ídices IPCA e CDI variarem. <br />Isso é apenas uma estimativa para enteder o seu investimento.</>
					: "Preencha os campos acima para ver a simulação do investimento."
				}
			</p>
		</div>
	)
}



const formSchema = z.object({
	title: z.string().min(1, { required_error: "Este campo é obrigatório." }),
	date_buy: z.date({ required_error: "Campo obrigatório" }),
	due_date: z.date().nullable().optional(),
	liquidez_diaria: z.boolean().optional(),
	taxes: z.boolean().optional(),
	bank_code: z.number({ required_error: "Selecione um banco", invalid_type_error: "Selecione um banco" }),
	value: z
		.string()
		.min(1, { message: "O valor deve ser um número decimal.", required_error: "Este campo é obrigatório." })
		.transform((value) => value.replace(/\./g, ""))
		.transform((value) => value.replace(/\,/g, ".")),
	index: z.preprocess((value) => (value === "" ? undefined : parseInt(value, 10)), z.number().int().nonnegative({ required_error: "Por favor selecione um index" })),
	index_percent: z
		.string()
		.min(1, { required_error: "Este campo é obrigatório." })
		.transform((value) => value.replace(/\./g, ""))
		.transform((value) => value.replace(/\,/g, ".")),
}).superRefine((values, ctx) => {
	if (!values.liquidez_diaria && !values.due_date) {
		ctx.addIssue({
			code: z.ZodIssueCode.custom,
			path: ["due_date"],
			message: "Data de vencimento é obrigatória quando não há liquidez diária.",
		})
	}
});




const InvestmentsAdd = ({ setReload }) => {
	const [isOpen, setIsOpen] = useState(false);



	const [bankList, setBankList] = useState([]);
	useEffect(() => {
		axiosInstance
			.get("/Banks") // `/posts` será concatenado ao `baseURL`
			.then((response) => {
				setBankList(response.data);
			})
			.catch((err) => {
				//setError(err.message);
				//setLoading(false);
			});
	}, []);


	const form = useForm({
		resolver: zodResolver(formSchema),
		defaultValues: {
			title: "",
			date_buy: undefined,
			due_date: undefined,
			liquidez_diaria: false,
			taxes: true,
			bank_code: undefined,
			value: "",
			index: "",
			index_percent: "",
		},
	})

	function onSubmit(values) {
		const payload = {
			title: values.title,
			date_buy: values.date_buy,
			date_expected_sell: values.liquidez_diaria ? null : (values.due_date ?? null),
			taxes: values.taxes ?? true,
			bank_code: values.bank_code,
			value: Number(values.value),
			index: Number(values.index),
			index_percent: Number(values.index_percent),
		}

		axiosInstance
			.post("/Investments", payload)
			.then((response) => {
				setIsOpen(false);
				setReload(Math.floor(Math.random() * 10000) + 1);
			})
			.catch((err) => {
				//setError(err.message);
				//setLoading(false);
			}
			);
	}


	const handleInputChangeDecimal = (event) => {
		const input = event.target;
		let value = input.value;

		// Remove caracteres que não sejam números ou vírgulas
		value = value.replace(/[^\d,]/g, "");

		// Adiciona vírgula como separador de milhares
		const parts = value.split(",");
		const intPart = parts[0].replace(/\B(?=(\d{3})+(?!\d))/g, ".");
		const decimalPart = parts[1] ? parts[1].substring(0, 2) : "";
		input.value = parts.length > 1 ? `${intPart},${decimalPart}` : intPart;

		// Atualiza o campo index_percent no formulário
		form.setValue(input.name, input.value);
	};

	// bank combobox open state
	const [popbank, setPopbank] = useState(false);

	const [liquidez_diaria, setLiquidezDiaria] = useState(false);



	return (
		<Dialog open={isOpen} onOpenChange={setIsOpen}>
			<DialogTrigger asChild>
				<Button
					type="button"
					onClick={() => setIsOpen(true)}
					size="sm"
				>
					<Plus className="h-4 w-4 mr-1" />
					Adicionar investimento
				</Button>
			</DialogTrigger>
			<DialogContent className="w-[95vw] sm:max-w-5xl md:w-[85vw] max-h-[90vh] overflow-y-auto">
				<DialogHeader>
					<DialogTitle>Adicionando novo investimento</DialogTitle>
					<DialogDescription>Preencha os dados do seu novo investimento.</DialogDescription>
				</DialogHeader>

				<Form {...form}>
					<form onSubmit={form.handleSubmit(onSubmit)} className="space-y-6">
						<FormField
							control={form.control}
							name="title"
							render={({ field }) => (
								<FormItem>
									<FormLabel>Identificação do investimento</FormLabel>
									<FormControl>
										<Input placeholder="Informe um título para o seu investimento" {...field} />
									</FormControl>
									<FormMessage>{form.formState.errors.name?.message}</FormMessage>
								</FormItem>
							)}
						/>

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
														field.onChange(checked);
														setLiquidezDiaria(checked);
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

						<div className="flex flex-wrap gap-4">
							<FormField
								control={form.control}
								name="bank_code"
								render={({ field }) => {
									const selectedBank = bankList.find((b) => b.code === field.value)
									const formatBankLabel = (bank) => `${String(bank.code ?? "").padStart(3, "0")} - ${bank.name}`

									return (
										<FormItem className="flex flex-col">
											<FormLabel>Banco</FormLabel>
											<FormControl>
													<Popover modal={true} open={popbank} onOpenChange={setPopbank}>
													<PopoverTrigger asChild>
														<Button
															type="button"
															variant="outline"
															role="combobox"
															className={cn(
																"w-[280px] justify-between",
																!field.value && "text-muted-foreground"
															)}
														>
															{selectedBank ? formatBankLabel(selectedBank) : "Selecione seu banco"}
															<ChevronsUpDown className="ml-2 h-4 w-4 shrink-0 opacity-50" />
														</Button>
													</PopoverTrigger>
													<PopoverContent align="start" className="w-[280px] p-0 z-[70]">
														<Command>
															<CommandInput placeholder="Buscar banco..." />
															<CommandList>
																<CommandEmpty>Nenhum banco encontrado.</CommandEmpty>
																<CommandGroup>
																	{bankList.map((bank) => (
																		<CommandItem
																			key={bank.id}
																			value={formatBankLabel(bank)}
																			onSelect={() => {
																				field.onChange(bank.code)
																				setPopbank(false)
																			}}
																		>
																			<Check
																				className={cn(
																					"h-4 w-4",
																					field.value === bank.code ? "opacity-100" : "opacity-0"
																				)}
																			/>
																			{formatBankLabel(bank)}
																		</CommandItem>
																	))}
																</CommandGroup>
															</CommandList>
														</Command>
													</PopoverContent>
												</Popover>
											</FormControl>
											<FormMessage />
										</FormItem>
									)
								}}
							/>

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

							<FormField
								control={form.control}
								name="index"
								render={({ field }) => (
									<FormItem className="w-64">
										<FormLabel>Indexador (CDI, IPCA+ ou %a.a.)</FormLabel>
										<Select onValueChange={field.onChange} defaultValue={field.value}>
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

							<FormField
								control={form.control}
								name="index_percent"
								render={({ field }) => (
									<FormItem className="w-32">
										<FormLabel>Valor do indexador</FormLabel>
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

						{/* Investment Preview */}
						<InvestmentPreview form={form} />

						<DialogFooter className="flex justify-between pt-4">
							<Button
								type="button"
								variant="outline"
								onClick={() => setIsOpen(false)}
							>
								Cancelar
							</Button>
							<Button
								type="submit"
								variant="destructive"
								onClick={onsubmit}
							>
								Adicionar
							</Button>
						</DialogFooter>
					</form>
				</Form>
			</DialogContent>
		</Dialog>
	);
};
export default InvestmentsAdd;
