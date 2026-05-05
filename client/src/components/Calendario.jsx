import { useEffect, useMemo, useRef, useState } from "react"

import { CalendarDate, getLocalTimeZone, today } from "@internationalized/date"
import { DatePicker } from "@heroui/react"
import { CalendarIcon } from "lucide-react"
import { Button } from "@/components/ui/button"

const quickDateOptions = [
	{ label: "+90", value: { days: 90 } },
	{ label: "+180", value: { days: 180 } },
	{ label: "+270", value: { days: 270 } },
	{ label: "+1 ano", value: { years: 1 } },
]

function normalizeDate(value) {
	if (!value) return undefined
	if (value instanceof Date) {
		return Number.isNaN(value.getTime()) ? undefined : value
	}

	const parsed = new Date(value)
	return Number.isNaN(parsed.getTime()) ? undefined : parsed
}

function toCalendarDate(value) {
	const normalizedValue = normalizeDate(value)
	if (!normalizedValue) return null

	return new CalendarDate(
		normalizedValue.getFullYear(),
		normalizedValue.getMonth() + 1,
		normalizedValue.getDate()
	)
}

function toJsDate(value) {
	if (!value) return null
	return new Date(value.year, value.month - 1, value.day)
}

function compareCalendarDate(left, right) {
	if (!left || !right) return 0

	if (left.year !== right.year) return left.year - right.year
	if (left.month !== right.month) return left.month - right.month
	return left.day - right.day
}

const Calendario = ({ field, disabled = false, minDate, maxDate }) => {
	const dateValue = toCalendarDate(field.value)
	const minValue = useMemo(() => toCalendarDate(minDate), [minDate])
	const maxValue = useMemo(() => toCalendarDate(maxDate), [maxDate])
	const wrapperRef = useRef(null)
	const [portalContainer, setPortalContainer] = useState(undefined)
	const [focusedValue, setFocusedValue] = useState(dateValue ?? today(getLocalTimeZone()))

	const isCalendarDateUnavailable = (value) => {
		if (minValue && compareCalendarDate(value, minValue) < 0) return true
		if (maxValue && compareCalendarDate(value, maxValue) > 0) return true
		return false
	}

	const canSelectDate = (value) => value && !isCalendarDateUnavailable(value)

	useEffect(() => {
		if (disabled) {
			field.onChange(null)
		}
	}, [disabled, field])

	useEffect(() => {
		setFocusedValue(dateValue ?? today(getLocalTimeZone()))
	}, [dateValue])

	useEffect(() => {
		const dialogContent = wrapperRef.current?.closest('[data-slot="dialog-content"]')
		setPortalContainer(dialogContent ?? undefined)
	}, [])

	const handleToday = () => {
		const currentDate = today(getLocalTimeZone())
		if (!canSelectDate(currentDate)) return
		setFocusedValue(currentDate)
		field.onChange(toJsDate(currentDate))
	}

	const handleQuickDate = (duration) => {
		const nextDate = today(getLocalTimeZone()).add(duration)
		if (!canSelectDate(nextDate)) return
		setFocusedValue(nextDate)
		field.onChange(toJsDate(nextDate))
	}

	return (
		<div ref={wrapperRef}>
			<DatePicker
				aria-label="Escolha uma data"
				value={dateValue}
				onChange={(value) => field.onChange(toJsDate(value))}
				isDisabled={disabled}
				showMonthAndYearPickers
				selectorButtonPlacement="end"
				selectorIcon={<CalendarIcon className="h-4 w-4" />}
				calendarWidth={360}
				granularity="day"
				disableAnimation
				minValue={minValue ?? undefined}
				maxValue={maxValue ?? undefined}
				isDateUnavailable={isCalendarDateUnavailable}
				placeholderValue={new CalendarDate(2026, 1, 1)}
				calendarProps={{
					focusedValue,
					onFocusChange: setFocusedValue,
					classNames: {
						cellButton:
							"data-[today=true]:bg-primary/12 data-[today=true]:font-semibold data-[today=true]:text-primary data-[today=true]:ring-1 data-[today=true]:ring-primary/40 data-[selected=true]:data-[today=true]:bg-primary data-[selected=true]:data-[today=true]:text-primary-foreground data-[selected=true]:data-[today=true]:ring-primary",
					},
				}}
				CalendarBottomContent={
					<div className="border-t border-border px-3 py-2">
						<Button
							type="button"
							variant="outline"
							className="h-9 w-full justify-center"
							onClick={handleToday}
							disabled={!canSelectDate(today(getLocalTimeZone()))}
						>
							Hoje
						</Button>
						<div className="mt-2 grid grid-cols-4 gap-2">
							{quickDateOptions.map((option) => (
								<Button
									key={option.label}
									type="button"
									variant="outline"
									className="h-8 px-2 text-xs"
									onClick={() => handleQuickDate(option.value)}
									disabled={!canSelectDate(today(getLocalTimeZone()).add(option.value))}
								>
									{option.label}
								</Button>
							))}
						</div>
					</div>
				}
				popoverProps={{
					placement: "bottom-start",
					offset: 8,
					portalContainer,
					shouldBlockScroll: false,
				}}
				classNames={{
					base: "w-[320px]",
					inputWrapper:
						"min-h-10 rounded-md border border-input bg-background shadow-xs transition-[color,box-shadow] cursor-text data-[hover=true]:bg-accent/40 group-data-[focus=true]:border-ring group-data-[focus=true]:ring-ring/50 group-data-[focus=true]:ring-[3px]",
					input: "text-sm text-foreground gap-0.5",
					segment:
						"rounded-sm px-1 text-sm text-foreground tabular-nums transition-[background-color,color,box-shadow] data-[type=literal]:px-0 data-[type=literal]:text-muted-foreground data-[editable=true]:cursor-text data-[placeholder=true]:text-muted-foreground/70 data-[editable=true]:focus:bg-primary data-[editable=true]:focus:text-primary-foreground data-[editable=true]:focus:outline-none data-[editable=true]:focus:ring-2 data-[editable=true]:focus:ring-primary/30",
					selectorButton:
						"h-10 min-w-10 rounded-r-md border-l border-input bg-transparent px-3 text-muted-foreground hover:bg-accent hover:text-accent-foreground",
					selectorIcon: "text-current",
					popoverContent:
						"z-[80] overflow-hidden rounded-md border border-border bg-popover text-popover-foreground shadow-md",
					calendar: "w-full rounded-md",
					calendarContent: "w-full p-2",
				}}
			/>
		</div>
	)
}

export default Calendario
