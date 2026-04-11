import React, { useEffect, useMemo, useRef } from 'react';
import { useState } from "react";

import { useForm } from "react-hook-form"
import { Checkbox } from "@/components/ui/checkbox"

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

import { Loader2, Plus, TrendingUp, Upload } from "lucide-react"


import { Input } from "@/components/ui/input"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import Calendario from "@/components/Calendario"
import BankCombobox from "@/components/BankCombobox"

import { zodResolver } from "@hookform/resolvers/zod"
import { z } from "zod"

import axiosInstance from "../utils/axiosConfig";
import { getIrTextClass } from "@/utils/ir-level"
import { getCachedBanks, primeBanksCache } from "@/utils/banksCache"
import { toast } from "@/hooks/use-toast"
import {
	detectInvestmentTypeFromTitle,
	INVESTMENT_TYPE_NONE,
	INVESTMENT_TYPE_OPTIONS,
} from "@/utils/investment-types"
import { fetchMoneyBoxesOverview, MONEY_BOX_NONE } from "@/utils/money-boxes"


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

function addOneYear(date) {
	const nextYear = new Date(date)
	nextYear.setFullYear(nextYear.getFullYear() + 1)
	return nextYear
}

function getIndexLabelByType(indexType, rawPercent) {
	const formattedPercent = Number(rawPercent ?? 0).toLocaleString("pt-BR", {
		minimumFractionDigits: 2,
		maximumFractionDigits: 2,
	})
	if (indexType === 0) return `${formattedPercent}% CDI`
	if (indexType === 1) return `IPCA+${formattedPercent}%`
	if (indexType === 3) return `CDI + ${formattedPercent}% a.a.`
	return `${formattedPercent}% a.a.`
}

function getLciEquivalentPercent(selectedIndexType, rawPercent, taxes) {
	const SELIC_ANNUAL_ESTIMATE = 0.1315
	const IPCA_ANNUAL_ESTIMATE = 0.045
	const assumedDays = 366
	const irFactor = 1 - (getIRPercent(taxes ?? true, assumedDays) / 100)

	let annualNetRate = 0

	if (selectedIndexType === 0) {
		annualNetRate = SELIC_ANNUAL_ESTIMATE * (rawPercent / 100) * irFactor
	} else if (selectedIndexType === 1) {
		annualNetRate = (IPCA_ANNUAL_ESTIMATE + (rawPercent / 100)) * irFactor
	} else if (selectedIndexType === 3) {
		annualNetRate = (SELIC_ANNUAL_ESTIMATE + (rawPercent / 100)) * irFactor
	} else {
		annualNetRate = (rawPercent / 100) * irFactor
	}

	return SELIC_ANNUAL_ESTIMATE <= 0 ? 0 : (annualNetRate / SELIC_ANNUAL_ESTIMATE) * 100
}

function getEquivalentPercent(indexType, selectedIndexType, rawPercent) {
	const SELIC_ANNUAL_ESTIMATE = 0.1315
	const IPCA_ANNUAL_ESTIMATE = 0.045

	let annualRate = 0

	if (selectedIndexType === 0) {
		annualRate = SELIC_ANNUAL_ESTIMATE * (rawPercent / 100)
	} else if (selectedIndexType === 1) {
		annualRate = IPCA_ANNUAL_ESTIMATE + (rawPercent / 100)
	} else if (selectedIndexType === 3) {
		annualRate = SELIC_ANNUAL_ESTIMATE + (rawPercent / 100)
	} else {
		annualRate = rawPercent / 100
	}

	if (indexType === 0) {
		return SELIC_ANNUAL_ESTIMATE <= 0 ? 0 : (annualRate / SELIC_ANNUAL_ESTIMATE) * 100
	}

	if (indexType === 1) {
		return (annualRate - IPCA_ANNUAL_ESTIMATE) * 100
	}

	if (indexType === 3) {
		return (annualRate - SELIC_ANNUAL_ESTIMATE) * 100
	}

	return annualRate * 100
}

function buildPreview({
	rawValue,
	indexType,
	rawPercent,
	dateBuy,
	watchDueDate,
	watchLiquidez,
	watchTaxes,
}) {
	const SELIC_ANNUAL_ESTIMATE = 0.1315
	const IPCA_ANNUAL_ESTIMATE = 0.045

	const sellDate = watchLiquidez
		? addOneYear(dateBuy)
		: !watchDueDate
			? new Date()
			: new Date(watchDueDate)

	if (sellDate <= dateBuy) return null

	const days = Math.floor((sellDate - dateBuy) / (1000 * 60 * 60 * 24))
	if (days <= 0) return null

	const taxes = watchTaxes ?? true
	const IR = getIRPercent(taxes, days) / 100
	const IOF = getIOFPercent(days) / 100

	let effectivePercent = 0
	let estimateLabel = null

	if (indexType === 0) {
		const annualRate = SELIC_ANNUAL_ESTIMATE * (rawPercent / 100)
		effectivePercent = annualRate / 365 * days
		estimateLabel = "Selic estimada"
	} else if (indexType === 1) {
		const annualRate = IPCA_ANNUAL_ESTIMATE + (rawPercent / 100)
		effectivePercent = annualRate / 366 * (days - 3)
		estimateLabel = "IPCA estimado"
	} else if (indexType === 3) {
		const annualRate = SELIC_ANNUAL_ESTIMATE + (rawPercent / 100)
		effectivePercent = annualRate / 366 * (days - 3)
		estimateLabel = "CDI estimado"
	} else {
		effectivePercent = (rawPercent / 100) / 366 * (days - 3)
	}

	const profitBrute = rawValue * effectivePercent
	const profitBruteIOF = profitBrute * (1 - IOF)
	const irValue = profitBruteIOF * IR
	const profitLiq = profitBruteIOF * (1 - IR)

	return {
		indexType,
		indexLabel: getIndexLabelByType(indexType, rawPercent),
		profitBrute,
		irPercent: IR * 100,
		irValue,
		profitLiq,
		days,
		isEstimate: indexType === 0 || indexType === 1,
		estimateLabel,
	}
}

function PreviewCard({ title, preview, rawValue }) {
	return (
		<div className="rounded-lg border border-green-500/20 bg-green-500/5 p-4 space-y-3">
			<div className="flex items-center gap-2 flex-wrap">
				<TrendingUp className="h-4 w-4 text-green-600 dark:text-green-400" />
				<span className="text-sm font-medium">{title}</span>
				{preview?.indexLabel && (
					<Badge variant="outline" className="text-xs">
						{preview.indexLabel}
					</Badge>
				)}
				{preview?.isEstimate && (
					<Badge variant="outline" className="text-xs text-muted-foreground">
						{preview.estimateLabel}
					</Badge>
				)}
			</div>
			<div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
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
								+{(preview.profitLiq / (rawValue || 1) * 100).toFixed(1)}%
							</Badge>
						)}
					</div>
				</div>
			</div>

		</div>
	)
}

function ComparativesCard({ items }) {
	return (
		<div className="rounded-lg border border-dashed border-muted-foreground/30 bg-muted/20 p-4 space-y-3">
			<div className="text-sm font-medium">Comparativos <small>Valores aproximados. Não deve ser levado em regra.</small></div>
			{items?.length ? (
				<div className="space-y-2 text-sm">
					{items.map((item) => (
						<div key={item.label} className="text-muted-foreground">
							{item.label}
						</div>
					))}
				</div>
			) : (
				<p className="text-xs text-muted-foreground">
					Preencha os campos acima para ver os equivalentes do investimento.
				</p>
			)}
		</div>
	)
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

	const previews = useMemo(() => {
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
		const selected = buildPreview({
			rawValue,
			indexType,
			rawPercent,
			dateBuy,
			watchDueDate,
			watchLiquidez,
			watchTaxes,
		})

		const comparisons = [0, 1, 2, 3]
			.filter((type) => type !== indexType)
			.map((type) => {
				const equivalentPercent = getEquivalentPercent(type, indexType, rawPercent)
				return {
					type,
					equivalentPercent,
					label: `Equivale a um ${getIndexLabelByType(type, equivalentPercent)}`,
				}
			})
			.filter((item) => !(item.type === 3 && Math.abs(item.equivalentPercent) < 0.005))

		const lciComparison = (watchTaxes ?? true)
			? {
				type: "lci",
				label: `Equivale a um LCI/LCA de ${Number(
					getLciEquivalentPercent(indexType, rawPercent, watchTaxes)
				).toLocaleString("pt-BR", {
					minimumFractionDigits: 2,
					maximumFractionDigits: 2,
				})}% CDI`,
			}
			: null

		return { rawValue, selected, comparisons, lciComparison }
	}, [watchValue, watchIndex, watchIndexPercent, watchDateBuy, watchDueDate, watchTaxes, watchLiquidez])

	return (
		<div className="space-y-4">
			<PreviewCard
				title="Simulação do investimento"
				preview={previews?.selected ?? null}
				rawValue={previews?.rawValue ?? 0}
			/>
			<ComparativesCard
				items={[
					...(previews?.comparisons ?? []),
					...(previews?.lciComparison ? [previews.lciComparison] : []),
				]}
			/>
			<p className="text-xs text-muted-foreground">
				Estimativa para o período selecionado.
				Aqui é considerado que o IPCA ou CDI permanecerão os mesmo até a data de vencimento.
				<br />
				Se usada a liquidez diária, a data de vencimento considerada 365 dias.
				<br />
				A taxa de juros pode variar durante o período conforme os ídices IPCA e CDI variarem.
				<br />
				Isso é apenas uma estimativa para enteder o seu investimento.<b>Valores podem variar.</b>
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
	investment_type: z.preprocess(
		(value) => (value === "" || value === INVESTMENT_TYPE_NONE ? undefined : value),
		z.string().optional()
	),
	money_box_id: z.preprocess(
		(value) => (value === "" || value === MONEY_BOX_NONE ? undefined : value),
		z.string().optional()
	),
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




function formatDecimalDisplay(value) {
	if (value === null || value === undefined || value === "") return ""

	const [int, dec = ""] = String(value).split(".")
	const intFormatted = int.replace(/\B(?=(\d{3})+(?!\d))/g, ".")
	return dec ? `${intFormatted},${dec.substring(0, 2)}` : intFormatted
}

function parseApiDate(value) {
	if (!value) return undefined

	const parsed = new Date(value)
	if (!Number.isNaN(parsed.getTime())) return parsed

	const match = String(value).match(/^(\d{4})-(\d{2})-(\d{2})/)
	if (!match) return undefined

	const [, year, month, day] = match
	const normalized = new Date(Number(year), Number(month) - 1, Number(day))
	return Number.isNaN(normalized.getTime()) ? undefined : normalized
}

const InvestmentsAdd = ({ setReload, externalOpen, onExternalClose, initialValues }) => {
	const [internalOpen, setInternalOpen] = useState(false);
	const [isExtracting, setIsExtracting] = useState(false);
	const fileInputRef = useRef(null);
	const autoDetectedInvestmentTypeRef = useRef(detectInvestmentTypeFromTitle(initialValues?.title ?? "") ?? INVESTMENT_TYPE_NONE);
	const isOpen = externalOpen !== undefined ? externalOpen : internalOpen;
	const setIsOpen = (value) => {
		if (externalOpen !== undefined) {
			if (!value && onExternalClose) onExternalClose();
			return;
		}

		setInternalOpen(value);
	};



	const [bankList, setBankList] = useState([]);
	const [moneyBoxesOverview, setMoneyBoxesOverview] = useState({
		items: [],
		selection_enabled: true,
		restriction_message: null,
	});
	useEffect(() => {
		getCachedBanks()
			.then((banks) => {
				setBankList(banks);
				primeBanksCache(banks);
			})
			.catch(() => { });
	}, []);

	useEffect(() => {
		if (!isOpen) return;

		fetchMoneyBoxesOverview()
			.then((data) => {
				setMoneyBoxesOverview(data);
			})
			.catch(() => {
				setMoneyBoxesOverview({
					items: [],
					selection_enabled: true,
					restriction_message: null,
				});
			});
	}, [isOpen]);


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
			investment_type: INVESTMENT_TYPE_NONE,
			money_box_id: MONEY_BOX_NONE,
			index: "",
			index_percent: "",
		},
	})

	useEffect(() => {
		if (!isOpen) return;

		autoDetectedInvestmentTypeRef.current = detectInvestmentTypeFromTitle(initialValues?.title ?? "") ?? INVESTMENT_TYPE_NONE;
		form.reset({
			title: initialValues?.title ?? "",
			date_buy: initialValues?.date_buy ?? undefined,
			due_date: initialValues?.due_date ?? undefined,
			liquidez_diaria: initialValues?.liquidez_diaria ?? false,
			taxes: initialValues?.taxes ?? true,
			bank_code: initialValues?.bank_code ?? undefined,
			value: initialValues?.value ?? "",
			investment_type: initialValues?.investment_type ?? INVESTMENT_TYPE_NONE,
			money_box_id: initialValues?.money_box_id ?? MONEY_BOX_NONE,
			index: initialValues?.index ?? "",
			index_percent: initialValues?.index_percent ?? "",
		});
		setLiquidezDiaria(initialValues?.liquidez_diaria ?? false);
	}, [form, initialValues, isOpen]);

	const syncInvestmentTypeWithTitle = (nextTitle) => {
		const detectedType = detectInvestmentTypeFromTitle(nextTitle) ?? INVESTMENT_TYPE_NONE;
		const currentType = form.getValues("investment_type") || INVESTMENT_TYPE_NONE;

		if (currentType === INVESTMENT_TYPE_NONE || currentType === autoDetectedInvestmentTypeRef.current) {
			form.setValue("investment_type", detectedType, {
				shouldDirty: true,
				shouldValidate: true,
			});
		}

		autoDetectedInvestmentTypeRef.current = detectedType;
	};

	function onSubmit(values) {
		const payload = {
			title: values.title,
			date_buy: values.date_buy,
			date_expected_sell: values.liquidez_diaria ? null : (values.due_date ?? null),
			taxes: values.taxes ?? true,
			bank_code: values.bank_code,
			value: Number(values.value),
			investment_type: values.investment_type,
			money_box_id: values.money_box_id ?? null,
			index: Number(values.index),
			index_percent: Number(values.index_percent),
		}

		axiosInstance
			.post("/Investments", payload)
			.then(() => {
				if (!initialValues?.source_investment_id) return Promise.resolve();

				return axiosInstance.patch(`/Investments/${initialValues.source_investment_id}/archive`, {
					archived: true,
				});
			})
			.then(() => {
				setIsOpen(false);
				form.reset({
					title: "",
					date_buy: undefined,
					due_date: undefined,
					liquidez_diaria: false,
					taxes: true,
					bank_code: undefined,
					value: "",
					investment_type: INVESTMENT_TYPE_NONE,
					money_box_id: MONEY_BOX_NONE,
					index: "",
					index_percent: "",
				});
				setLiquidezDiaria(false);
				setReload(Math.floor(Math.random() * 10000) + 1);
			})
			.catch((err) => {
				//setError(err.message);
				//setLoading(false);
			}
			);
	}

	const applyExtractedValues = (data) => {
		if (data.title) {
			form.setValue("title", data.title, { shouldDirty: true });
			syncInvestmentTypeWithTitle(data.title);
		}
		const parsedDateBuy = parseApiDate(data.date_buy);
		if (parsedDateBuy) {
			form.setValue("date_buy", parsedDateBuy, { shouldDirty: true });
		}
		const parsedDueDate = parseApiDate(data.due_date);
		if (parsedDueDate) {
			form.setValue("due_date", parsedDueDate, { shouldDirty: true });
		}
		if (typeof data.liquidez_diaria === "boolean") {
			form.setValue("liquidez_diaria", data.liquidez_diaria, { shouldDirty: true });
			setLiquidezDiaria(data.liquidez_diaria);
			if (data.liquidez_diaria) {
				form.setValue("due_date", undefined, { shouldDirty: true });
			}
		}
		if (typeof data.taxes === "boolean") form.setValue("taxes", data.taxes, { shouldDirty: true });
		if (typeof data.bank_code === "number") form.setValue("bank_code", data.bank_code, { shouldDirty: true });
		if (data.value !== null && data.value !== undefined) {
			form.setValue("value", formatDecimalDisplay(data.value), { shouldDirty: true });
		}
		if (data.index !== null && data.index !== undefined) {
			form.setValue("index", String(data.index), { shouldDirty: true });
		}
		if (data.index_percent !== null && data.index_percent !== undefined) {
			form.setValue("index_percent", formatDecimalDisplay(data.index_percent), { shouldDirty: true });
		}
	};

	const handleExtractFile = async (event) => {
		const file = event.target.files?.[0];
		event.target.value = "";

		if (!file) return;

		const formData = new FormData();
		formData.append("file", file);
		setIsExtracting(true);

		try {
			const response = await axiosInstance.post("/Investments/extract", formData, {
				headers: {
					"Content-Type": "multipart/form-data",
				},
				timeout: 60000,
			});

			applyExtractedValues(response.data ?? {});
			toast({
				title: "Campos preenchidos",
				description: response.data?.notes ?? "A IA preencheu os campos encontrados no documento.",
			});
		} catch (error) {
			const message = error?.response?.data?.message ?? "Não foi possível extrair os dados do arquivo.";
			toast({
				title: "Falha ao ler documento",
				description: message,
				variant: "destructive",
			});
		} finally {
			setIsExtracting(false);
		}
	};


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

	const [liquidez_diaria, setLiquidezDiaria] = useState(false);



	return (
		<Dialog open={isOpen} onOpenChange={setIsOpen}>
			{externalOpen === undefined && (
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
			)}
			<DialogContent className="w-[95vw] sm:max-w-5xl md:w-[85vw] max-h-[90vh] overflow-y-auto">
				<DialogHeader>
					<DialogTitle>{initialValues ? "Reinvestir em novo investimento" : "Adicionando novo investimento"}</DialogTitle>
					<DialogDescription>Preencha os dados do seu novo investimento.</DialogDescription>
				</DialogHeader>

				<Form {...form}>
					<form onSubmit={form.handleSubmit(onSubmit)} className="space-y-6">
						<div className="rounded-lg border border-dashed border-muted-foreground/30 bg-muted/20 p-4">
							<div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
								<div className="space-y-1">
									<p className="text-sm font-medium">Importar comprovante com IA</p>
									<p className="text-xs">
										Envie `txt`, `html`, imagem ou `pdf` para tentar preencher os campos automaticamente.
									</p>
									<p className="text-xs text-muted-foreground">
										Não armazenamos os arquivos enviados em nosso servidores. Após a adição do seu investimento, o arquivo é deletado imediatamente.
									</p>
								</div>
								<div className="flex items-center gap-2">
									<input
										ref={fileInputRef}
										type="file"
										className="hidden"
										accept=".txt,.html,.htm,.pdf,image/png,image/jpeg,image/webp"
										onChange={handleExtractFile}
									/>
									<Button
										type="button"
										variant="outline"
										onClick={() => fileInputRef.current?.click()}
										disabled={isExtracting}
									>
										{isExtracting ? <Loader2 className="mr-2 h-4 w-4 animate-spin" /> : <Upload className="mr-2 h-4 w-4" />}
										{isExtracting ? "Lendo arquivo..." : "Processar com IA"}
									</Button>
								</div>
							</div>
						</div>

						<div className="flex flex-wrap gap-4">
							<FormField
								control={form.control}
								name="bank_code"
								render={({ field }) => {
									return (
										<FormItem className="flex flex-col shrink-0">
											<FormLabel>Banco</FormLabel>
											<FormControl>
												<BankCombobox
													banks={bankList}
													value={field.value}
													onChange={field.onChange}
												/>
											</FormControl>
											<FormMessage />
										</FormItem>
									)
								}}
							/>
							<FormField
								control={form.control}
								name="title"
								render={({ field }) => (
									<FormItem className="min-w-0 flex-1">
										<FormLabel>Identificação do investimento</FormLabel>
										<FormControl>
											<Input
												placeholder="Informe um título para o seu investimento"
												{...field}
												onChange={(event) => {
													field.onChange(event);
													syncInvestmentTypeWithTitle(event.target.value);
												}}
												className="w-full"
											/>
										</FormControl>
										<FormMessage>{form.formState.errors.name?.message}</FormMessage>
									</FormItem>
								)}
							/>
						</div>

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
							
						</div>

						<div className="flex flex-wrap gap-2">
							<FormField
								control={form.control}
								name="investment_type"
								render={({ field }) => (
									<FormItem className="w-40">
										<FormLabel>Tipo do investimento</FormLabel>
										<Select
											onValueChange={(value) => {
												field.onChange(value)
											}}
											value={field.value || INVESTMENT_TYPE_NONE}
										>
											<FormControl className="w-40">
												<SelectTrigger>
													<SelectValue placeholder="Opcional" />
												</SelectTrigger>
											</FormControl>
											<SelectContent>
												<SelectItem value={INVESTMENT_TYPE_NONE}>Não informado</SelectItem>
												{INVESTMENT_TYPE_OPTIONS.map((option) => (
													<SelectItem key={option.value} value={option.value}>
														{option.label}
													</SelectItem>
												))}
											</SelectContent>
										</Select>
										<FormMessage />
									</FormItem>
								)}
							/>
							<FormField
								control={form.control}
								name="money_box_id"
								render={({ field }) => (
									<FormItem className="w-48">
										<FormLabel>Cofrinho</FormLabel>
										<Select
											onValueChange={field.onChange}
											value={field.value || MONEY_BOX_NONE}
											disabled={!moneyBoxesOverview.selection_enabled}
										>
											<FormControl>
												<SelectTrigger>
													<SelectValue placeholder="Opcional" />
												</SelectTrigger>
											</FormControl>
											<SelectContent>
												<SelectItem value={MONEY_BOX_NONE}>Sem cofrinho</SelectItem>
												{moneyBoxesOverview.items.map((item) => (
													<SelectItem key={item.id} value={item.id}>
														{item.name}
													</SelectItem>
												))}
											</SelectContent>
										</Select>
										
										<FormMessage />
									</FormItem>
								)}
							/>
							<FormField
								control={form.control}
								name="value"
								render={({ field }) => (
									<FormItem className="w-35">
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
									<FormItem className="w-50">
										<FormLabel>Indexador (CDI, IPCA+, %a.a.)</FormLabel>
										<Select onValueChange={field.onChange} value={field.value}>
											<FormControl className="w-50">
												<SelectTrigger>
													<SelectValue placeholder="Selecione o indexador" />
												</SelectTrigger>
											</FormControl>
											<SelectContent>
												<SelectItem value="0">CDI</SelectItem>
												<SelectItem value="1">IPCA+</SelectItem>
												<SelectItem value="2">%a.a.</SelectItem>
												<SelectItem value="3">CDI + %a.a.</SelectItem>
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
									<FormItem className="w-27">
										<FormLabel>% indexador</FormLabel>
										<FormControl>
											<Input
												placeholder="14,99"
												{...field}
												onChange={handleInputChangeDecimal}
												className="text-sm"
											/>
										</FormControl>
										<FormMessage />
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
