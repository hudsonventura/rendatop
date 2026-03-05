import * as React from "react"
import { useState, useEffect } from "react"

import { addDays, format } from "date-fns"
import { Calendar as CalendarIcon } from "lucide-react"


import { cn } from "@/lib/utils"
import { Button } from "@/components/ui/button"
import { Calendar } from "@/components/ui/calendar"
import {
	Popover,
	PopoverContent,
	PopoverTrigger,
} from "@/components/ui/popover"
import {
	FormControl,
  } from "@/components/ui/form"
import { Input } from "@/components/ui/input"

const Calendario = ({ field, disabled = false }) => {

	const [date, setDate] = React.useState()
	const [inputValue, setInputValue] = React.useState("")

	const formatDate = (input) => {
		const numbers = input.replace(/\D/g, "")
		let formatted = ""
		
		if (numbers.length > 0) formatted += numbers.substring(0, 2)
		if (numbers.length > 2) formatted += "/" + numbers.substring(2, 4)
		if (numbers.length > 4) formatted += "/" + numbers.substring(4, 8)
		
		return formatted
	}

	useEffect(() => {
		if(disabled == true){
			setDate(null)
			setInputValue(null)
			field.value = null
		}
	}, [disabled])



	const handleInputChange = (event) => {
		const formatted = formatDate(event.target.value)
		setInputValue(formatted)
	
		if (formatted.length === 10) {
		  const [day, month, year] = formatted.split("/")
		  const newDate = new Date(parseInt(year), parseInt(month) - 1, parseInt(day))
		  if (!isNaN(newDate.getTime())) {
			setDate(newDate)
			field.value = newDate
		  }
		}
	}
	
	const handleEnter = (event) => {
		if (event.key === "Enter") 
		{
			field.onChange(date)
			field.value = date
			handleCloseManually();
		}
	}


	const handleCalendarSelect = (newDate) => {
		field.onChange(newDate)
		setDate(newDate)
		if (newDate) {
		  setInputValue(format(newDate, "dd/MM/yyyy"))
		  setDate(newDate)
			field.value = newDate
			handleCloseManually();
		}
	}
	

	const [isOpen, setIsOpen] = useState(false)

	const handleOpenChange = (open) => {
		setIsOpen(open)
	}

	const handleCloseManually = () => {
		setIsOpen(false)
	}


	return (
		<Popover open={isOpen} onOpenChange={handleOpenChange} >
			<PopoverTrigger asChild>
				<FormControl>
					<Button
						variant={"outline"}
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
				</FormControl>
			</PopoverTrigger>
			<PopoverContent className="w-auto p-0" align="start">
				<Input
					type="text"
					value={inputValue}
					onChange={handleInputChange}
					onKeyPress={handleEnter}
					placeholder="DD/MM/AAAA"
					className="w-full"
					initialFocus
				/>
				<Calendar
					mode="single"
					selected={field.value}
					onSelect={handleCalendarSelect}
					
				/>
				<div className="p-3 border-t">
          
        </div>
			</PopoverContent>
		</Popover>
	)

};

export default Calendario;

