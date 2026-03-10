import { useState, useMemo } from "react"
import {
    ChevronLeft,
    ChevronRight,
    Calendar as CalendarIcon,
    Clock,
    ArrowDownCircle,
    ArrowUpCircle,
} from "lucide-react"
import {
    format,
    addMonths,
    subMonths,
    startOfMonth,
    endOfMonth,
    eachDayOfInterval,
    isSameMonth,
    isToday,
    isSameDay,
} from "date-fns"
import { ptBR } from "date-fns/locale"

import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import {
    Dialog,
    DialogContent,
    DialogDescription,
    DialogHeader,
    DialogTitle,
} from "@/components/ui/dialog"
import { cn } from "@/lib/utils"

// ── Helpers ───────────────────────────────────────────────────────────────────

function formatCurrency(val) {
    return new Intl.NumberFormat("pt-BR", {
        style: "currency",
        currency: "BRL",
        minimumFractionDigits: 2,
    }).format(val)
}

function getIndexLabel(investment) {
    switch (investment.index) {
        case "PERCENT_YEAR":
            return `${investment.index_percent}% a.a.`
        case "CDI":
            return `${investment.index_percent}% CDI`
        case "IPCA_MAIS":
            return `IPCA+${investment.index_percent}%`
        default:
            return `${investment.index_percent}%`
    }
}

function getBankName(investment) {
    if (!investment?.bank) return "Banco Desconhecido"
    if (typeof investment.bank === "string") return investment.bank
    return investment.bank.name || "Banco Desconhecido"
}

/**
 * Convert an array of investments into calendar events.
 * Each investment produces 1 or 2 events:
 *   - "start" event on date_buy (green)
 *   - "due" event on due_date (blue), if present
 */
function investmentsToEvents(investments) {
    const events = []
    for (const inv of investments) {
        // Start date event
        events.push({
            id: `${inv.id}-start`,
            investment: inv,
            title: inv.title,
            date: new Date(inv.date_buy),
            type: "start",
            color: "bg-green-600",
        })

        // Due date event (only if set)
        if (inv.due_date) {
            events.push({
                id: `${inv.id}-due`,
                investment: inv,
                title: inv.title,
                date: new Date(inv.due_date),
                type: "due",
                color: "bg-blue-600",
            })
        }
    }
    return events
}

// ── Component ─────────────────────────────────────────────────────────────────

export default function InvestmentsCalendar({ investments }) {
    const [currentDate, setCurrentDate] = useState(new Date())
    const [selectedDate, setSelectedDate] = useState(null)
    const [showEventDialog, setShowEventDialog] = useState(false)
    const [selectedEvent, setSelectedEvent] = useState(null)

    const events = useMemo(
        () => (investments ? investmentsToEvents(investments) : []),
        [investments]
    )

    const monthStart = startOfMonth(currentDate)
    const monthEnd = endOfMonth(currentDate)

    // Extend to full weeks
    const calendarStart = new Date(monthStart)
    calendarStart.setDate(calendarStart.getDate() - monthStart.getDay())
    const calendarEnd = new Date(monthEnd)
    calendarEnd.setDate(calendarEnd.getDate() + (6 - monthEnd.getDay()))

    const calendarDays = eachDayOfInterval({ start: calendarStart, end: calendarEnd })

    const getEventsForDay = (date) =>
        events.filter((e) => isSameDay(e.date, date))

    const navigateMonth = (dir) =>
        setCurrentDate(dir === "prev" ? subMonths(currentDate, 1) : addMonths(currentDate, 1))

    const goToToday = () => setCurrentDate(new Date())

    const handleEventClick = (event) => {
        setSelectedEvent(event)
        setShowEventDialog(true)
    }

    const weekDays = ["Dom", "Seg", "Ter", "Qua", "Qui", "Sex", "Sáb"]

    return (
        <div className="flex flex-col h-full border rounded-lg bg-background">
            {/* ── Header ── */}
            <div className="flex flex-col flex-wrap gap-4 p-4 md:p-6 border-b md:flex-row md:items-center md:justify-between">
                <div className="flex items-center gap-3 flex-wrap">
                    <div className="flex items-center gap-1">
                        <Button variant="outline" size="sm" onClick={() => navigateMonth("prev")} className="cursor-pointer">
                            <ChevronLeft className="w-4 h-4" />
                        </Button>
                        <Button variant="outline" size="sm" onClick={() => navigateMonth("next")} className="cursor-pointer">
                            <ChevronRight className="w-4 h-4" />
                        </Button>
                        <Button variant="outline" size="sm" onClick={goToToday} className="cursor-pointer">
                            Hoje
                        </Button>
                    </div>
                    <h2 className="text-xl font-semibold capitalize">
                        {format(currentDate, "MMMM yyyy", { locale: ptBR })}
                    </h2>
                </div>

                {/* Legend */}
                <div className="flex items-center gap-4 text-sm">
                    <div className="flex items-center gap-1.5">
                        <span className="w-3 h-3 rounded-full bg-green-600" />
                        <span className="text-muted-foreground">Aplicação</span>
                    </div>
                    <div className="flex items-center gap-1.5">
                        <span className="w-3 h-3 rounded-full bg-blue-600" />
                        <span className="text-muted-foreground">Vencimento</span>
                    </div>
                </div>
            </div>

            {/* ── Week day headers ── */}
            <div className="grid grid-cols-7 border-b">
                {weekDays.map((day) => (
                    <div key={day} className="p-3 text-center font-medium text-sm text-muted-foreground border-r last:border-r-0">
                        {day}
                    </div>
                ))}
            </div>

            {/* ── Calendar grid ── */}
            <div className="grid grid-cols-7 flex-1">
                {calendarDays.map((day) => {
                    const dayEvents = getEventsForDay(day)
                    const isCurrentMonth = isSameMonth(day, currentDate)
                    const isDayToday = isToday(day)
                    const isSelected = selectedDate && isSameDay(day, selectedDate)

                    return (
                        <div
                            key={day.toISOString()}
                            className={cn(
                                "min-h-[100px] md:min-h-[120px] border-r border-b last:border-r-0 p-1.5 md:p-2 cursor-pointer transition-colors",
                                isCurrentMonth
                                    ? "bg-background hover:bg-accent/50"
                                    : "bg-muted/30 text-muted-foreground",
                                isSelected && "ring-2 ring-primary ring-inset",
                                isDayToday && "bg-accent/20"
                            )}
                            onClick={() => setSelectedDate(day)}
                        >
                            <div className="flex items-center justify-between mb-1">
                                <span
                                    className={cn(
                                        "text-sm font-medium",
                                        isDayToday &&
                                        "bg-primary text-primary-foreground rounded-md w-6 h-6 flex items-center justify-center text-xs"
                                    )}
                                >
                                    {format(day, "d")}
                                </span>
                                {dayEvents.length > 2 && (
                                    <span className="text-xs text-muted-foreground">
                                        +{dayEvents.length - 2}
                                    </span>
                                )}
                            </div>

                            <div className="space-y-1">
                                {dayEvents.slice(0, 2).map((event) => (
                                    <div
                                        key={event.id}
                                        className={cn(
                                            "text-xs p-1 rounded-sm text-white cursor-pointer truncate",
                                            event.color
                                        )}
                                        onClick={(e) => {
                                            e.stopPropagation()
                                            handleEventClick(event)
                                        }}
                                    >
                                        <div className="flex items-center gap-1">
                                            {event.type === "start" ? (
                                                <ArrowDownCircle className="w-3 h-3 flex-shrink-0" />
                                            ) : (
                                                <ArrowUpCircle className="w-3 h-3 flex-shrink-0" />
                                            )}
                                            <span className="truncate">{event.title}</span>
                                        </div>
                                    </div>
                                ))}
                            </div>
                        </div>
                    )
                })}
            </div>

            {/* ── Event detail dialog ── */}
            <Dialog open={showEventDialog} onOpenChange={setShowEventDialog}>
                <DialogContent className="max-w-md">
                    <DialogHeader>
                        <DialogTitle>{selectedEvent?.title || "Detalhes"}</DialogTitle>
                        <DialogDescription>Detalhes do investimento</DialogDescription>
                    </DialogHeader>
                    {selectedEvent && (
                        <div className="space-y-3">
                            <div className="flex items-center gap-2">
                                <Badge
                                    className={cn("text-white", selectedEvent.color)}
                                >
                                    {selectedEvent.type === "start" ? "Aplicação" : "Vencimento"}
                                </Badge>
                            </div>

                            <div className="flex items-center gap-2 text-sm">
                                <CalendarIcon className="w-4 h-4 text-muted-foreground" />
                                <span>{format(selectedEvent.date, "dd/MM/yyyy")}</span>
                            </div>

                            <div className="flex items-center gap-2 text-sm">
                                <Clock className="w-4 h-4 text-muted-foreground" />
                                <span>Banco: {getBankName(selectedEvent.investment)}</span>
                            </div>

                            <div className="grid grid-cols-2 gap-3 pt-2 border-t">
                                <div>
                                    <p className="text-xs text-muted-foreground">Valor investido</p>
                                    <p className="font-semibold">{formatCurrency(selectedEvent.investment.value)}</p>
                                </div>
                                <div>
                                    <p className="text-xs text-muted-foreground">Indexador</p>
                                    <p className="font-semibold">{getIndexLabel(selectedEvent.investment)}</p>
                                </div>
                                {selectedEvent.investment.calculated?.[0] && (
                                    <>
                                        <div>
                                            <p className="text-xs text-muted-foreground">Valor líquido atual</p>
                                            <p className="font-semibold text-green-600">
                                                {formatCurrency(selectedEvent.investment.calculated[0].value_liq)}
                                            </p>
                                        </div>
                                        <div>
                                            <p className="text-xs text-muted-foreground">Lucro líquido</p>
                                            <p className="font-semibold text-green-600">
                                                {formatCurrency(selectedEvent.investment.calculated[0].profit_liq)}
                                            </p>
                                        </div>
                                    </>
                                )}
                            </div>
                        </div>
                    )}
                </DialogContent>
            </Dialog>
        </div>
    )
}
