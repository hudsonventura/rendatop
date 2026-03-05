import React, { useEffect } from 'react';
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

import { Check, ChevronsUpDown } from "lucide-react"

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

import axiosInstance from "../utils/axiosConfig";




const formSchema = z.object({
  title: z.string().min(1, { required_error: "Este campo é obrigatório." }),
  date_buy: z.date({ required_error: "Campo obrigatório" }),
  date_expected_sell: z.date().optional(),
  liquidez_diaria: z.boolean().optional(),
  taxes: z.boolean().optional(),
  bank: z.string({required_error: "Selecione ou crie um banco",}),
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








const InvestmentsAdd = ({setReload}) => {
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
		},
	})

	function onSubmit(values) {
		// Do something with the form values.
		// ✅ This will be type-safe and validated.
		console.log(values)


		axiosInstance
			.post("/Investments", JSON.stringify(values)) // `/posts` será concatenado ao `baseURL`
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

	const [popbank, setPopbank] = useState([]);
	//create bank
	const CreateBank = (e) => {
		if (e.key === "Enter") 
		{
			var new_bank = e.target.value;
			form.setValue("bank", new_bank)
		}

	}

	const [liquidez_diaria, setLiquidezDiaria] = useState(false);



	return (
		<Dialog className="max-w-[45rem]" open={isOpen} >
			<DialogTrigger>
				<button
					type="button"
					className="rounded-md bg-primary px-4 py-2 text-sm font-medium text-white shadow-sm hover:bg-primary/90 focus:outline-none focus:ring-2 focus:ring-primary/90 focus:ring-offset-2 focus:ring-offset-background"
					onClick={() => setIsOpen(true)}
				>
					Adicionar investimento
				</button>
			</DialogTrigger>
			<DialogContent className="max-w-4xl w-[50vw]">

				<DialogHeader>
					<DialogTitle>Adicionando novo investimento</DialogTitle>
					<DialogDescription>
						<Form {...form}>
							<form onSubmit={form.handleSubmit(onSubmit)} className="space-y-8">




								<FormField
									control={form.control}
									name="title"
									render={({ field }) => (
										<FormItem>
											<FormLabel>Identificação do investimento</FormLabel>
											<FormControl>
												<Input placeholder="Informe um ítulo para o seu investimento" {...field} />
											</FormControl>
											<FormMessage>{form.formState.errors.name?.message}</FormMessage>
										</FormItem>
									)}
								/>

								<div className="flex gap-4">
									<FormField
										control={form.control}
										name="date_buy"
										render={({ field }) => (
											<FormItem>
												<FormLabel>Data da aplicação</FormLabel>
												<FormControl>
													<Calendario field={field} />
												</FormControl>
												<FormMessage>{form.formState.errors.name?.message}</FormMessage>
											</FormItem>
										)}
									/>
									<FormField
										control={form.control}
										name="date_expected_sell"
										render={({ field }) => (
											<FormItem>
												<FormLabel>Data do vencimento / regate</FormLabel>
												<FormControl>
													<Calendario field={field} disabled={liquidez_diaria} />
												</FormControl>
												<FormMessage>{form.formState.errors.name?.message}</FormMessage>
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
																textColor="text-foreground" 
															/>
														</FormControl>
														<span style={{ marginLeft: "0.5rem" }}>Liquidez diária </span>
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
																textColor="text-foreground" 
															/>
														</FormControl>
														<span style={{ marginLeft: "0.5rem" }}>Possui incidência de impostos </span>
													</div>
												</FormItem>
											)}
										/>		

								</div>

								<div className="flex gap-4">
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
													className={cn(
														"w-[200px] justify-between",
														!field.value && "text-muted-foreground"
													)}
													>
														{field.value}
														{field.value
														? bankList.find(
															(bank) => bank === field.value
														)?.label
														: "Selecione seu banco"}
													<ChevronsUpDown className="ml-2 h-4 w-4 shrink-0 opacity-50" />
													</Button>
												</FormControl>
												</PopoverTrigger>
												<PopoverContent className="w-[200px] p-0">
												<Command>
													<CommandInput placeholder="Buscar ou criar" onKeyDown={(event) => {
														if (event.key === "Enter") {
															event.preventDefault()
															event.stopPropagation()
															setPopbank(false)
															CreateBank(event)
														}
													}} />
													<CommandList>
													<CommandEmpty>ENTER para criar novo banco</CommandEmpty>
													<CommandGroup>
														{bankList.map((bank) => (
														<CommandItem
															value={bank}
															key={bank}
															onSelect={() => {
															form.setValue("bank", bank)
															}}
														>
															{bank}
															<Check
															className={cn(
																"ml-auto",
																bank === field.value
																? "opacity-100"
																: "opacity-0"
															)}
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
														placeholder="Ex.: 108% ou 13,11% ou IPCA+7,60%" 
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

								
								<DialogFooter className="flex justify-between" style={{ marginTop: "2rem" }}>
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
										//onClick={() => setIsOpen(false)}
										onClick={onsubmit}
									>
										Adicionar
									</Button>
								</DialogFooter>

							</form>
						</Form>
					</DialogDescription>
				</DialogHeader>
			</DialogContent>
		</Dialog>
	);
};
export default InvestmentsAdd;


