import * as React from "react"
import { useState, useEffect, useRef } from "react"

import { format } from "date-fns"
import { Calendar as CalendarIcon } from "lucide-react"

import { cn } from "@/lib/utils"
import { Button } from "@/components/ui/button"
import { Calendar } from "@/components/ui/calendar"
import { Input } from "@/components/ui/input"

const Calendario = ({ field, disabled = false }) => {

	const [date, setDate] = useState()
	const [inputValue, setInputValue] = useState("")
	const [isOpen, setIsOpen] = useState(false)
	const containerRef = useRef(null)

	const formatDate = (input) => {
		const numbers = input.replace(/\D/g, "")
		let formatted = ""

		if (numbers.length > 0) formatted += numbers.substring(0, 2)
		if (numbers.length > 2) formatted += "/" + numbers.substring(2, 4)
		if (numbers.length > 4) formatted += "/" + numbers.substring(4, 8)

		return formatted
	}

	useEffect(() => {
		if (disabled == true) {
			setDate(null)
			setInputValue("")
			field.onChange(null)
		}
	}, [disabled])

	// Close when clicking outside
	useEffect(() => {
		const handleClickOutside = (event) => {
			if (containerRef.current && !containerRef.current.contains(event.target)) {
				setIsOpen(false)
			}
		}
		if (isOpen) {
			document.addEventListener("mousedown", handleClickOutside)
		}
		return () => {
			document.removeEventListener("mousedown", handleClickOutside)
		}
	}, [isOpen])

	const handleInputChange = (event) => {
		const formatted = formatDate(event.target.value)
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
		field.onChange(newDate)
		setDate(newDate)
		if (newDate) {
			setInputValue(format(newDate, "dd/MM/yyyy"))
			setIsOpen(false)
		}
	}

	const toggleOpen = () => {
		if (!disabled) {
			setIsOpen((prev) => !prev)
		}
	}

	return (
		<div ref={containerRef} style={{ position: "relative", display: "inline-block" }}>
			<Button
				type="button"
				variant="outline"
				className={cn(
					"w-[240px] pl-3 text-left font-normal",
					!field.value && "text-muted-foreground"
				)}
				disabled={disabled}
				onClick={toggleOpen}
			>
				{field.value ? (
					format(field.value, "dd/MM/yyyy")
				) : (
					<span>Escolha uma data</span>
				)}
				<CalendarIcon className="ml-auto h-4 w-4 opacity-50" />
			</Button>

			{isOpen && (
				<div
					style={{
						position: "absolute",
						top: "calc(100% + 4px)",
						left: 0,
						zIndex: 9999,
						background: "var(--popover, white)",
						border: "1px solid var(--border, #e2e8f0)",
						borderRadius: "0.375rem",
						boxShadow: "0 4px 24px rgba(0,0,0,0.18)",
						minWidth: "240px",
					}}
				>
					<Input
						type="text"
						value={inputValue}
						onChange={handleInputChange}
						placeholder="DD/MM/AAAA"
						className="w-full rounded-b-none border-x-0 border-t-0"
						autoFocus
					/>
					<Calendar
						mode="single"
						selected={date}
						onSelect={handleCalendarSelect}
					/>
				</div>
			)}
		</div>
	)
}

export default Calendario
