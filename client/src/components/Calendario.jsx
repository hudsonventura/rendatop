import * as React from "react"
import { useState, useEffect } from "react"

import { format } from "date-fns"
import { ptBR } from "date-fns/locale"
import { CalendarIcon } from "lucide-react"

import { cn } from "@/lib/utils"
import { Button } from "@/components/ui/button"
import { Calendar } from "@/components/ui/calendar"
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover"
import { Input } from "@/components/ui/input"

const Calendario = ({ field, disabled = false }) => {
	const [date, setDate] = useState(field.value || undefined)
	const [inputValue, setInputValue] = useState(
		field.value ? format(field.value, "dd/MM/yyyy") : ""
	)
	const [isOpen, setIsOpen] = useState(false)

	useEffect(() => {
		if (disabled) {
			setDate(undefined)
			setInputValue("")
			field.onChange(null)
			setIsOpen(false)
		}
	}, [disabled])

	const formatMasked = (input) => {
		const numbers = input.replace(/\D/g, "")
		let formatted = ""
		if (numbers.length > 0) formatted += numbers.substring(0, 2)
		if (numbers.length > 2) formatted += "/" + numbers.substring(2, 4)
		if (numbers.length > 4) formatted += "/" + numbers.substring(4, 8)
		return formatted
	}

	const handleInputChange = (event) => {
		const formatted = formatMasked(event.target.value)
		setInputValue(formatted)

		if (formatted.length === 10) {
			const [day, month, year] = formatted.split("/")
			const newDate = new Date(parseInt(year), parseInt(month) - 1, parseInt(day))
			if (!isNaN(newDate.getTime())) {
				setDate(newDate)
				field.onChange(newDate)
			}
		}
	}

	const handleCalendarSelect = (newDate) => {
		setDate(newDate)
		field.onChange(newDate)
		if (newDate) {
			setInputValue(format(newDate, "dd/MM/yyyy"))
			setIsOpen(false)
		}
	}

	return (
		<Popover open={isOpen} onOpenChange={(open) => !disabled && setIsOpen(open)}>
			<PopoverTrigger asChild>
				<Button
					type="button"
					variant="outline"
					className={cn(
						"w-[240px] pl-3 text-left font-normal",
						!field.value && "text-muted-foreground"
					)}
					disabled={disabled}
				>
					{field.value ? (
						format(field.value, "dd/MM/yyyy")
					) : (
						<span>Escolha uma data</span>
					)}
					<CalendarIcon className="ml-auto h-4 w-4 opacity-50" />
				</Button>
			</PopoverTrigger>
			<PopoverContent className="w-auto p-0" align="start">
				<div className="px-3 pt-3">
					<Input
						type="text"
						value={inputValue}
						onChange={handleInputChange}
						placeholder="DD/MM/AAAA"
						className="w-full"
					/>
				</div>
				<Calendar
					mode="single"
					selected={date}
					onSelect={handleCalendarSelect}
					captionLayout="dropdown"
					locale={ptBR}
					fromYear={2000}
					toYear={2050}
					className="p-3"
				/>
			</PopoverContent>
		</Popover>
	)
}

export default Calendario
