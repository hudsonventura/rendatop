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
		if (!rawPercent || rawPercent <= 0) return null

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

		// CDI (index=0): approximate using ~13% Selic annual (reasonable current estimate)
		if (indexType === 0) {
			const selicApprox = 0.1315 // approximate annual Selic
			effectivePercent = selicApprox / 365 * days * rawPercent / 100
		}
		// %a.a. (index=2) or IPCA+ (index=1)
		else {
			effectivePercent = rawPercent / 366 * (days - 3) / 100
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
			isEstimate: indexType === 0,
		}
	}, [watchValue, watchIndex, watchIndexPercent, watchDateBuy, watchDueDate, watchTaxes, watchLiquidez])

	return (
		<div className="rounded-lg border border-green-500/20 bg-green-500/5 p-4 space-y-3">
			<div className="flex items-center gap-2">
				<TrendingUp className="h-4 w-4 text-green-600 dark:text-green-400" />
				<span className="text-sm font-medium">Simulação do investimento</span>
				{preview?.isEstimate && (
					<Badge variant="outline" className="text-xs text-muted-foreground">
						Selic estimada
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
					<span className="text-sm font-medium text-red-500">
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
			username: "",
			taxes: true,
		},
	})

	function onSubmit(values) {
		// Do something with the form values.
		// ✅ This will be type-safe and validated.
		console.log(values)


		axiosInstance
			.post("/Investments", JSON.stringify(values))
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
									const [search, setSearch] = React.useState("")
									const ref = React.useRef(null)

									React.useEffect(() => {
										if (!popbank) return
										const handler = (e) => {
											if (ref.current && !ref.current.contains(e.target)) {
												setPopbank(false)
											}
										}
										document.addEventListener("mousedown", handler)
										return () => document.removeEventListener("mousedown", handler)
									}, [popbank])

									const displayName = bankList.find((b) => b.code === field.value)?.name
									const filtered = bankList.filter((b) =>
										b.name.toLowerCase().includes(search.toLowerCase())
									)

									return (
										<FormItem className="flex flex-col">
											<FormLabel>Banco</FormLabel>
											<FormControl>
												<div ref={ref} className="relative w-[200px]">
													<Button
														type="button"
														variant="outline"
														role="combobox"
														className={cn(
															"w-full justify-between",
															!field.value && "text-muted-foreground"
														)}
														onClick={() => setPopbank((v) => !v)}
													>
														{displayName || "Selecione seu banco"}
														<ChevronsUpDown className="ml-2 h-4 w-4 shrink-0 opacity-50" />
													</Button>
													{popbank && (
														<div className="absolute z-50 top-full mt-1 w-full rounded-md border bg-popover shadow-md">
															<div className="p-1 border-b">
																<input
																	autoFocus
																	className="w-full bg-transparent text-sm outline-none placeholder:text-muted-foreground px-2 py-1"
																	placeholder="Buscar banco..."
																	value={search}
																	onChange={(e) => setSearch(e.target.value)}
																/>
															</div>
															<div className="max-h-[180px] overflow-y-auto p-1">
																{filtered.length === 0 && (
																	<p className="text-sm text-muted-foreground text-center py-2">
																		Nenhum banco encontrado.
																	</p>
																)}
																{filtered.map((bank) => (
																	<div
																		key={bank.id}
																		className={cn(
																			"flex items-center gap-2 rounded-sm px-2 py-1.5 text-sm cursor-pointer hover:bg-accent hover:text-accent-foreground",
																			field.value === bank.code && "bg-accent"
																		)}
																		onMouseDown={(e) => {
																			e.preventDefault()
																			field.onChange(bank.code)
																			setSearch("")
																			setPopbank(false)
																		}}
																	>
																		<Check
																			className={cn(
																				"h-4 w-4 shrink-0",
																				field.value === bank.code ? "opacity-100" : "opacity-0"
																			)}
																		/>
																		{bank.name}
																	</div>
																))}
															</div>
														</div>
													)}
												</div>
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

