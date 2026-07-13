import React, { useState } from "react"
import ReactDOM from "react-dom"
import {
    flexRender,
    getCoreRowModel,
    getSortedRowModel,
    getPaginationRowModel,
    useReactTable,
} from "@tanstack/react-table"
import {
    EllipsisVertical,
    Eye,
    Pencil,
    HandCoins,
    CopyPlus,
    Archive,
    ArchiveRestore,
    Trash2,
    ArrowUpDown,
    ChevronLeft,
    ChevronRight,
    ChevronsLeft,
    ChevronsRight,
} from "lucide-react"

import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import {
    Dialog,
    DialogContent,
    DialogDescription,
    DialogFooter,
    DialogHeader,
    DialogTitle,
} from "@/components/ui/dialog"
import { Tooltip, TooltipContent, TooltipTrigger } from "@/components/ui/tooltip"
import {
    Select,
    SelectContent,
    SelectItem,
    SelectTrigger,
    SelectValue,
} from "@/components/ui/select"
import { Label } from "@/components/ui/label"
import {
    Table,
    TableBody,
    TableCell,
    TableFooter,
    TableHead,
    TableHeader,
    TableRow,
} from "@/components/ui/table"

import InvestmentsEdit from "@/components/InvestmentsEdit"
import InvestmentsRedeem from "@/components/InvestmentsRedeem"
import RedemptionEdit from "@/components/RedemptionEdit"
import IrBadge from "@/components/IrBadge"
import IofBadge from "@/components/IofBadge"
import axiosInstance from "@/utils/axiosConfig"

// ── Helpers ───────────────────────────────────────────────────────────────────

const formatCurrency = (val) =>
    val.toLocaleString("pt-BR", { minimumFractionDigits: 2, maximumFractionDigits: 2 })

const VALUE_LIQUID_CURRENT_HINT = "Valor líquido atual, já descontados os impostos, considerando o resgate hoje. Não reflete o valor no vencimento para investimentos sem liquidez diária."
const OVERDUE_INVESTMENT_HINT = "Este investimento passou da data de vencimento e provavelmente foi resgatado automaticamente pelo banco. Informe o reinvestimento, o resgate total ou arquive este item."
const EARLY_DUE_WINDOW_DAYS = 5
const UPCOMING_DUE_PRIORITY_DAYS = 30

function getBankLogoSrc(bank) {
    const rawCode = bank?.code
    if (rawCode === null || rawCode === undefined || rawCode === "") return null

    const normalizedCode = String(rawCode).padStart(3, "0")
    return `/bank-logos/${normalizedCode}.svg`
}

function BankCell({ bank }) {
    const bankName = bank?.name || "Banco Desconhecido"
    const logoSrc = getBankLogoSrc(bank)

    return (
        <span className="flex items-center gap-2 min-w-0">
            {logoSrc ? (
                <img
                    src={logoSrc}
                    alt=""
                    aria-hidden="true"
                    className="h-4 w-4 shrink-0 rounded-sm object-contain"
                    onError={(event) => {
                        event.currentTarget.style.display = "none"
                    }}
                />
            ) : null}
            <span className="whitespace-normal break-words leading-snug min-w-0">
                {bankName}
            </span>
        </span>
    )
}

function parseDateValue(dateValue) {
    if (!dateValue) return null

    if (dateValue instanceof Date) {
        return Number.isNaN(dateValue.getTime())
            ? null
            : new Date(dateValue.getFullYear(), dateValue.getMonth(), dateValue.getDate())
    }

    const match = String(dateValue).match(/^(\d{4})-(\d{2})-(\d{2})/)
    if (match) {
        const [, year, month, day] = match
        return new Date(Number(year), Number(month) - 1, Number(day))
    }

    const parsed = new Date(dateValue)
    if (Number.isNaN(parsed.getTime())) return null

    return new Date(parsed.getFullYear(), parsed.getMonth(), parsed.getDate())
}

const formatDate = (dateValue) => {
    const parsed = parseDateValue(dateValue)
    return parsed ? parsed.toLocaleDateString("pt-BR") : "-"
}

const getDateSortValue = (dateValue) => parseDateValue(dateValue)?.getTime()

function getTodayStart() {
    const today = new Date()
    today.setHours(0, 0, 0, 0)
    return today
}

const isDueDateTodayOrPast = (dateStr) => {
    if (!dateStr) return false
    const due = parseDateValue(dateStr)
    if (!due) return false

    const today = getTodayStart()
    return due <= today
}

const isDueDateWithinArchiveWindow = (dateStr) => {
    if (!dateStr) return false
    const due = parseDateValue(dateStr)
    if (!due) return false

    const threshold = getTodayStart()
    threshold.setDate(threshold.getDate() + EARLY_DUE_WINDOW_DAYS)

    return due <= threshold
}

const isDueDatePast = (dateStr) => {
    if (!dateStr) return false
    const due = parseDateValue(dateStr)
    if (!due) return false

    return due < getTodayStart()
}

function getDueDateSortValue(dateValue) {
    const dueDate = parseDateValue(dateValue)
    const today = getTodayStart()
    const todayTimestamp = today.getTime()
    const windowEnd = new Date(today)
    windowEnd.setDate(windowEnd.getDate() + UPCOMING_DUE_PRIORITY_DAYS)
    const dueTimestamp = dueDate?.getTime()

    if (dueDate && dueDate < today)
        return dueTimestamp ?? 0

    if (dueDate && dueDate >= today && dueDate <= windowEnd)
        return 1_000_000_000_000_000 + (dueTimestamp ?? 0)

    if (!dueDate)
        return 2_000_000_000_000_000 + todayTimestamp

    return 3_000_000_000_000_000 + dueTimestamp
}

const canShowReinvest = (dateStr) => {
    return isDueDateWithinArchiveWindow(dateStr)
}

function getIndexLabel(investment) {
    switch (investment.index) {
        case "PERCENT_YEAR":
            return `${investment.index_percent}% a.a.`
        case "CDI":
            return `${investment.index_percent}% CDI`
        case "CDI_MAIS":
            return `CDI + ${investment.index_percent}% a.a.`
        case "IPCA_MAIS":
            return `IPCA+${investment.index_percent}%`
        default:
            return `${investment.index_percent}%`
    }
}

function getRedeemedValue(investment) {
    return (investment.redemptions ?? []).reduce((total, redemption) => total + (redemption.value ?? 0), 0)
}

function getTableCalculated(investment, index = 0) {
    return investment.table_calculated?.[index] ?? investment.calculated?.[index]
}

function getTableValue(investment) {
    return investment.table_value ?? investment.value
}

function sumInvestments(investments) {
    return investments.reduce((totals, investment) => {
        const calc = getTableCalculated(investment)

        return {
            value: totals.value + (getTableValue(investment) ?? 0),
            redeemed_value: totals.redeemed_value + getRedeemedValue(investment),
            ir: totals.ir + (calc?.IR_value ?? 0),
            iof: totals.iof + (calc?.IOF_value ?? 0),
            profit_liq: totals.profit_liq + (calc?.profit_liq ?? 0),
            value_liq: totals.value_liq + (calc?.value_liq ?? 0),
        }
    }, {
        value: 0,
        redeemed_value: 0,
        ir: 0,
        iof: 0,
        profit_liq: 0,
        value_liq: 0,
    })
}

function getRedemptionComposition(investment, redemption) {
    const baseCalc = investment.calculated?.[0]
    const redemptionValue = redemption?.value ?? 0

    if (!baseCalc || redemptionValue <= 0 || (baseCalc.value_liq ?? 0) <= 0) {
        return {
            principal: redemptionValue,
            profit: 0,
            ir: 0,
            iof: 0,
        }
    }

    const ratio = Math.min(1, redemptionValue / baseCalc.value_liq)

    return {
        principal: investment.value * ratio,
        profit: (baseCalc.profit_liq ?? 0) * ratio,
        ir: (baseCalc.IR_value ?? 0) * ratio,
        iof: (baseCalc.IOF_value ?? 0) * ratio,
    }
}

// ── View Dialog (investment details) ──────────────────────────────────────────

function ViewDialog({ investment, open, onOpenChange, onEdit, onRedeem, onReinvest, onEditRedemption, onDeleteRedemption, onArchive, onDelete }) {
    if (!investment) return null

    const calc = getTableCalculated(investment, 0)
    const calcDue = getTableCalculated(investment, 1)
    const hasDueEstimate = Boolean(investment.due_date && calcDue)
    const showReinvest = canShowReinvest(investment.due_date)
    const canArchive = investment.archived || isDueDateWithinArchiveWindow(investment.due_date)
    const logoSrc = getBankLogoSrc(investment.bank)
    const redemptions = [...(investment.redemptions ?? [])].sort(
        (a, b) => new Date(b.date).getTime() - new Date(a.date).getTime()
    )

    return (
        <Dialog open={open} onOpenChange={onOpenChange}>
            <DialogContent className="w-[70vw] max-w-[70vw] sm:max-w-6xl max-h-[60vh] overflow-y-auto">
                <DialogHeader>
                    <DialogTitle>{investment.title}</DialogTitle>
                    <DialogDescription className="space-y-1">
                        <div className="flex items-center gap-2">
                            {logoSrc ? (
                                <img
                                    src={logoSrc}
                                    alt=""
                                    aria-hidden="true"
                                    className="h-5 w-5 shrink-0 rounded-sm object-contain"
                                    onError={(event) => {
                                        event.currentTarget.style.display = "none"
                                    }}
                                />
                            ) : null}
                            <span>{investment.bank?.name || "Banco Desconhecido"} · {getIndexLabel(investment)}</span>
                        </div>
                        <div>
                            Data do investimento: {formatDate(investment.date_buy)}
                        </div>
                        <div>
                            Valor original investido: R$ {formatCurrency(investment.value)}
                        </div>
                    </DialogDescription>
                </DialogHeader>
                <div className="space-y-3 rounded-md border p-3 min-w-0">
                    <h4 className="text-sm font-semibold">Valor atual do investimento</h4>
                    <div className="flex flex-wrap gap-1.5">
                        <Badge variant="outline" className="text-xs whitespace-nowrap">
                            Valor investido atual: R$ {formatCurrency(getTableValue(investment))}
                        </Badge>
                        <Badge variant="outline" className="text-xs whitespace-nowrap">
                            Valor total resgatado: R$ {formatCurrency(getRedeemedValue(investment))}
                        </Badge>
                    </div>

                    <div className="grid grid-cols-1 md:grid-cols-[max-content_1fr] items-start md:items-center gap-x-2 gap-y-2">
                        <span className="text-xs text-muted-foreground md:whitespace-nowrap">Valores atuais:</span>
                        <div className="min-w-0">
                            <div className="flex flex-wrap items-center gap-1.5">
                                <IofBadge iofPercent={calc.IOF} iofValue={calc.IOF_value} showValue className="text-xs whitespace-nowrap" investmentDate={investment.date_buy} />
                                <IrBadge irPercent={calc.IR} irValue={calc.IR_value} showValue className="text-xs whitespace-nowrap" investmentDate={investment.date_buy} />
                                <Badge variant="default" className="text-xs whitespace-nowrap">
                                    Valor líquido: R$ {formatCurrency(calc.value_liq)}
                                </Badge>
                            </div>
                        </div>

                        <span className="text-xs text-muted-foreground md:whitespace-nowrap">
                            {investment.due_date
                                ? `Estimado na data de venc. (${formatDate(investment.due_date)}):`
                                : "Estimado na data de venc.:"}
                        </span>
                        <div className="min-w-0">
                            {hasDueEstimate ? (
                                <div className="flex flex-wrap items-center gap-1.5">
                                    <IofBadge iofPercent={calcDue.IOF} iofValue={calcDue.IOF_value} showValue className="text-xs whitespace-nowrap" investmentDate={investment.date_buy} />
                                    <IrBadge irPercent={calcDue.IR} irValue={calcDue.IR_value} showValue className="text-xs whitespace-nowrap" investmentDate={investment.date_buy} />
                                    <Badge variant="default" className="text-xs whitespace-nowrap">
                                        Valor líquido: R$ {formatCurrency(calcDue.value_liq)}
                                    </Badge>
                                </div>
                            ) : (
                                <Badge variant="outline" className="text-xs whitespace-nowrap">
                                    Sem data de vencimento definida
                                </Badge>
                            )}
                        </div>
                    </div>
                </div>
                <div className="space-y-2 min-w-0">
                    <h4 className="text-sm font-semibold">Resgates</h4>
                    {redemptions.length === 0 ? (
                        <p className="text-xs text-muted-foreground">Nenhum resgate registrado.</p>
                    ) : (
                        <div className="rounded-md border overflow-hidden max-w-full">
                            <Table>
                                <TableHeader>
                                    <TableRow>
                                        <TableHead>Título</TableHead>
                                        <TableHead>Data</TableHead>
                                        <TableHead className="text-right">Valor</TableHead>
                                        <TableHead className="text-right">Ações</TableHead>
                                    </TableRow>
                                </TableHeader>
                                <TableBody>
                                    {redemptions.map((redemption) => (
                                        <TableRow key={redemption.id}>
                                            <TableCell className="font-medium whitespace-normal break-words max-w-[340px]">
                                                {redemption.title}
                                            </TableCell>
                                            <TableCell>{formatDate(redemption.date)}</TableCell>
                                            <TableCell className="text-right">
                                                <div className="space-y-1 text-right">
                                                    <div>R$ {formatCurrency(redemption.value)}</div>
                                                    <div className="text-[11px] text-muted-foreground leading-snug">
                                                        {(() => {
                                                            const composition = getRedemptionComposition(investment, redemption)
                                                            return (
                                                                <>
                                                                    <div>Valor original: R$ {formatCurrency(composition.principal)}</div>
                                                                    <div>Lucro: R$ {formatCurrency(composition.profit)}</div>
                                                                    <div>IR: R$ {formatCurrency(composition.ir)}</div>
                                                                    <div>IOF: R$ {formatCurrency(composition.iof)}</div>
                                                                </>
                                                            )
                                                        })()}
                                                    </div>
                                                </div>
                                            </TableCell>
                                            <TableCell className="text-right">
                                                <Button
                                                    variant="ghost"
                                                    size="icon"
                                                    className="size-8 cursor-pointer"
                                                    onClick={() => { onOpenChange(false); onEditRedemption(redemption) }}
                                                >
                                                    <Pencil className="h-4 w-4" />
                                                    <span className="sr-only">Editar resgate</span>
                                                </Button>
                                                <Button
                                                    variant="ghost"
                                                    size="icon"
                                                    className="size-8 cursor-pointer text-destructive hover:text-destructive"
                                                    onClick={() => onDeleteRedemption(redemption)}
                                                >
                                                    <Trash2 className="h-4 w-4" />
                                                    <span className="sr-only">Excluir resgate</span>
                                                </Button>
                                            </TableCell>
                                        </TableRow>
                                    ))}
                                </TableBody>
                            </Table>
                        </div>
                    )}
                </div>
                {!investment.due_date && (
                    <p className="text-xs text-muted-foreground">
                        * Valores estimados baseados na data atual (liquidez diária)
                    </p>
                )}
                <DialogFooter className="flex gap-2 sm:gap-2 pt-2">
                    <Button
                        variant="outline"
                        className="cursor-pointer"
                        onClick={() => { onOpenChange(false); onEdit() }}
                    >
                        <Pencil className="h-4 w-4 mr-1" />
                        Editar
                    </Button>
                    <Button
                        variant="secondary"
                        className="cursor-pointer"
                        disabled={!showReinvest}
                        onClick={() => { onOpenChange(false); onReinvest() }}
                    >
                        <CopyPlus className="h-4 w-4 mr-1" />
                        Reinvestir
                    </Button>
                    <Button
                        variant="secondary"
                        className="cursor-pointer"
                        onClick={() => { onOpenChange(false); onRedeem() }}
                    >
                        <HandCoins className="h-4 w-4 mr-1" />
                        Resgatar
                    </Button>
                    <Button
                        variant="secondary"
                        className="cursor-pointer"
                        disabled={!canArchive}
                        onClick={() => {
                            if (!canArchive) return
                            onOpenChange(false)
                            onArchive()
                        }}
                    >
                        {investment.archived ? <ArchiveRestore className="h-4 w-4 mr-1" /> : <Archive className="h-4 w-4 mr-1" />}
                        {investment.archived ? "Desarquivar" : "Arquivar"}
                    </Button>
                    <Button
                        variant="destructive"
                        className="cursor-pointer"
                        onClick={() => { onOpenChange(false); onDelete() }}
                    >
                        <Trash2 className="h-4 w-4 mr-1" />
                        Excluir
                    </Button>
                </DialogFooter>
            </DialogContent>
        </Dialog>
    )
}

// ── Delete Dialog ─────────────────────────────────────────────────────────────

function DeleteDialog({ investment, open, onOpenChange, onConfirm }) {
    if (!investment) return null

    return (
        <Dialog open={open} onOpenChange={onOpenChange}>
            <DialogContent className="sm:max-w-sm">
                <DialogHeader>
                    <DialogTitle className="flex items-center gap-2">
                        <Trash2 className="h-5 w-5 text-destructive" />
                        Excluir investimento
                    </DialogTitle>
                    <DialogDescription>
                        Tem certeza que deseja excluir <strong>{investment.title}</strong>?
                        Essa ação não pode ser desfeita.
                    </DialogDescription>
                </DialogHeader>
                <DialogFooter className="flex gap-2 sm:gap-2">
                    <Button variant="outline" className="flex-1" onClick={() => onOpenChange(false)}>
                        Cancelar
                    </Button>
                    <Button variant="destructive" className="flex-1" onClick={onConfirm}>
                        <Trash2 className="h-4 w-4 mr-1" />
                        Excluir
                    </Button>
                </DialogFooter>
            </DialogContent>
        </Dialog>
    )
}

function DeleteRedemptionDialog({ redemption, open, onOpenChange, onConfirm }) {
    if (!redemption) return null

    return (
        <Dialog open={open} onOpenChange={onOpenChange}>
            <DialogContent className="sm:max-w-sm">
                <DialogHeader>
                    <DialogTitle className="flex items-center gap-2">
                        <Trash2 className="h-5 w-5 text-destructive" />
                        Excluir resgate
                    </DialogTitle>
                    <DialogDescription>
                        Tem certeza que deseja excluir <strong>{redemption.title}</strong>?
                        Essa ação não pode ser desfeita.
                    </DialogDescription>
                </DialogHeader>
                <DialogFooter className="flex gap-2 sm:gap-2">
                    <Button variant="outline" className="flex-1" onClick={() => onOpenChange(false)}>
                        Cancelar
                    </Button>
                    <Button variant="destructive" className="flex-1" onClick={onConfirm}>
                        <Trash2 className="h-4 w-4 mr-1" />
                        Excluir
                    </Button>
                </DialogFooter>
            </DialogContent>
        </Dialog>
    )
}

function ArchiveDialog({ investment, open, onOpenChange, onConfirm }) {
    if (!investment) return null

    const isArchived = Boolean(investment.archived)
    const title = isArchived ? "Desarquivar investimento" : "Arquivar investimento"
    const description = isArchived
        ? `Deseja desarquivar ${investment.title}? Ele voltará para a lista principal.`
        : `Deseja arquivar ${investment.title}? Ele deixará de aparecer em Meus Investimentos por padrão.`
    const actionLabel = isArchived ? "Sim, desarquivar" : "Sim, arquivar"
    const Icon = isArchived ? ArchiveRestore : Archive

    return (
        <Dialog open={open} onOpenChange={onOpenChange}>
            <DialogContent className="sm:max-w-sm">
                <DialogHeader>
                    <DialogTitle className="flex items-center gap-2">
                        <Icon className="h-5 w-5" />
                        {title}
                    </DialogTitle>
                    <DialogDescription>{description}</DialogDescription>
                </DialogHeader>
                <DialogFooter className="flex gap-2 sm:gap-2">
                    <Button variant="outline" className="flex-1" onClick={() => onOpenChange(false)}>
                        Não
                    </Button>
                    <Button className="flex-1" onClick={onConfirm}>
                        {actionLabel}
                    </Button>
                </DialogFooter>
            </DialogContent>
        </Dialog>
    )
}

const archiveReinvestHint = "Você só poderá reinvestir o valor deste investimento ou arquivá-lo a partir de 5 dias corridos antes da data de resgate"

function ActionMenuItemWithHint({ disabled, onClick, className, children }) {
    const item = (
        <button
            className={className}
            disabled={disabled}
            onClick={onClick}
        >
            {children}
        </button>
    )

    if (!disabled) return item

    return (
        <Tooltip>
            <TooltipTrigger asChild>
                <span className="block w-full cursor-not-allowed">{item}</span>
            </TooltipTrigger>
            <TooltipContent side="left" className="max-w-xs">
                {archiveReinvestHint}
            </TooltipContent>
        </Tooltip>
    )
}

// ── Actions Cell ──────────────────────────────────────────────────────────────

function ActionsCell({ investment, onView, onEdit, onRedeem, onReinvest, onArchive, onDelete }) {
    const [menuOpen, setMenuOpen] = useState(false)
    const [menuPos, setMenuPos] = useState({ top: 0, left: 0 })
    const btnRef = React.useRef(null)
    const menuRef = React.useRef(null)
    const showReinvest = canShowReinvest(investment.due_date)
    const canArchive = investment.archived || isDueDateWithinArchiveWindow(investment.due_date)

    // Close menu when clicking outside or scrolling
    React.useEffect(() => {
        if (!menuOpen) return
        const close = () => setMenuOpen(false)
        const handleClick = (e) => {
            if (
                menuRef.current && !menuRef.current.contains(e.target) &&
                btnRef.current && !btnRef.current.contains(e.target)
            ) {
                close()
            }
        }
        document.addEventListener("mousedown", handleClick)
        window.addEventListener("scroll", close, true)
        window.addEventListener("resize", close)
        return () => {
            document.removeEventListener("mousedown", handleClick)
            window.removeEventListener("scroll", close, true)
            window.removeEventListener("resize", close)
        }
    }, [menuOpen])

    const toggleMenu = (e) => {
        e.stopPropagation()
        if (!menuOpen && btnRef.current) {
            const rect = btnRef.current.getBoundingClientRect()
            setMenuPos({ top: rect.bottom + 4, left: rect.right - 160 }) // 160 = menu width (w-40)
        }
        setMenuOpen((prev) => !prev)
    }

    return (
        <>
            <span ref={btnRef} className="inline-flex">
                <Button
                    variant="ghost"
                    className="text-muted-foreground flex size-8 cursor-pointer"
                    size="icon"
                    onClick={toggleMenu}
                >
                    <EllipsisVertical />
                    <span className="sr-only">Abrir menu</span>
                </Button>
            </span>

            {menuOpen && ReactDOM.createPortal(
                <div
                    ref={menuRef}
                    className="fixed z-50 w-40 rounded-md border bg-popover p-1 shadow-md"
                    style={{ top: menuPos.top, left: menuPos.left }}
                >
                    <button
                        className="flex w-full items-center gap-2 rounded-sm px-2 py-1.5 text-sm hover:bg-accent cursor-pointer"
                        onClick={() => { setMenuOpen(false); onView(investment) }}
                    >
                        <Eye className="h-4 w-4" />
                        Visualizar
                    </button>
                    <button
                        className="flex w-full items-center gap-2 rounded-sm px-2 py-1.5 text-sm hover:bg-accent cursor-pointer"
                        onClick={() => { setMenuOpen(false); onEdit(investment) }}
                    >
                        <Pencil className="h-4 w-4" />
                        Editar
                    </button>
                    <button
                        className="flex w-full items-center gap-2 rounded-sm px-2 py-1.5 text-sm hover:bg-accent cursor-pointer"
                        onClick={() => { setMenuOpen(false); onRedeem(investment) }}
                    >
                        <HandCoins className="h-4 w-4" />
                        Resgatar
                    </button>
                    <ActionMenuItemWithHint
                        className="flex w-full items-center gap-2 rounded-sm px-2 py-1.5 text-sm hover:bg-accent disabled:cursor-not-allowed disabled:opacity-50"
                        disabled={!showReinvest}
                        onClick={() => {
                            if (!showReinvest) return
                            setMenuOpen(false)
                            onReinvest(investment)
                        }}
                    >
                        <CopyPlus className="h-4 w-4" />
                        Reinvestir
                    </ActionMenuItemWithHint>
                    <ActionMenuItemWithHint
                        className="flex w-full items-center gap-2 rounded-sm px-2 py-1.5 text-sm hover:bg-accent disabled:cursor-not-allowed disabled:opacity-50"
                        disabled={!canArchive}
                        onClick={() => {
                            if (!canArchive) return
                            setMenuOpen(false)
                            onArchive(investment)
                        }}
                    >
                        <Archive className="h-4 w-4" />
                        Arquivar
                    </ActionMenuItemWithHint>
                    <div className="my-1 h-px bg-border" />
                    <button
                        className="flex w-full items-center gap-2 rounded-sm px-2 py-1.5 text-sm text-destructive hover:bg-destructive/10 cursor-pointer"
                        onClick={() => { setMenuOpen(false); onDelete(investment) }}
                    >
                        <Trash2 className="h-4 w-4" />
                        Excluir
                    </button>
                </div>,
                document.body
            )}
        </>
    )
}

// ── Column definitions ────────────────────────────────────────────────────────

function getColumns(onView, onEdit, onRedeem, onReinvest, onArchive, onDelete) {
    return [
        {
            accessorKey: "title",
            header: ({ column }) => (
                <Button
                    variant="ghost"
                    className="cursor-pointer -ml-3"
                    onClick={() => column.toggleSorting(column.getIsSorted() === "asc")}
                >
                    Título
                    <ArrowUpDown className="ml-1 h-4 w-4" />
                </Button>
            ),
            cell: ({ row }) => (
                <span className="font-medium whitespace-normal break-words leading-snug">
                    {row.original.title}
                </span>
            ),
        },
        {
            accessorKey: "bank",
            header: ({ column }) => (
                <Button
                    variant="ghost"
                    className="cursor-pointer -ml-3"
                    onClick={() => column.toggleSorting(column.getIsSorted() === "asc")}
                >
                    Banco
                    <ArrowUpDown className="ml-1 h-4 w-4" />
                </Button>
            ),
            cell: ({ row }) => {
                return <BankCell bank={row.original.bank} />
            },
            sortingFn: (rowA, rowB) => {
                const a = rowA.original.bank?.name || ""
                const b = rowB.original.bank?.name || ""
                return a.localeCompare(b)
            }
        },
        {
            id: "index",
            header: "Indexador",
            cell: ({ row }) => (
                <Badge variant="outline" className="text-muted-foreground whitespace-nowrap">
                    {getIndexLabel(row.original)}
                </Badge>
            ),
        },
        {
            accessorKey: "value",
            header: ({ column }) => (
                <Button
                    variant="ghost"
                    className="cursor-pointer -ml-3 h-auto py-1 text-left whitespace-normal"
                    onClick={() => column.toggleSorting(column.getIsSorted() === "asc")}
                >
                    Valor investido
                    <ArrowUpDown className="ml-1 h-4 w-4" />
                </Button>
            ),
            cell: ({ row }) => (
                <span className="whitespace-nowrap">R$ {formatCurrency(getTableValue(row.original))}</span>
            ),
        },
        {
            id: "date_buy",
            accessorFn: (row) => getDateSortValue(row.date_buy) ?? 0,
            header: ({ column }) => (
                <Button
                    variant="ghost"
                    className="cursor-pointer -ml-3 h-auto py-1 text-left whitespace-normal"
                    onClick={() => column.toggleSorting(column.getIsSorted() === "asc")}
                >
                    Data do investimento
                    <ArrowUpDown className="ml-1 h-4 w-4" />
                </Button>
            ),
            cell: ({ row }) => (
                <span className="whitespace-nowrap">
                    {formatDate(row.original.date_buy)}
                </span>
            ),
        },
        {
            id: "redeemed_value",
            header: ({ column }) => (
                <Button
                    variant="ghost"
                    className="cursor-pointer -ml-3 h-auto py-1 text-left whitespace-normal"
                    onClick={() => column.toggleSorting(column.getIsSorted() === "asc")}
                >
                    Valor total resgatado
                    <ArrowUpDown className="ml-1 h-4 w-4" />
                </Button>
            ),
            cell: ({ row }) => (
                <span className="whitespace-nowrap">
                    R$ {formatCurrency(getRedeemedValue(row.original))}
                </span>
            ),
            sortingFn: (rowA, rowB) =>
                getRedeemedValue(rowA.original) - getRedeemedValue(rowB.original),
        },
        {
            id: "ir",
            header: "IR",
            cell: ({ row }) => {
                const tableCalc = getTableCalculated(row.original)
                const ir = tableCalc?.IR ?? 0
                const irValue = tableCalc?.IR_value ?? 0

                return (
                    <IrBadge asBadge={false} irPercent={ir} irValue={irValue} showValue showPercentInTooltip className="whitespace-nowrap" investmentDate={row.original.date_buy} />
                )
            },
            sortingFn: (rowA, rowB) =>
                (getTableCalculated(rowA.original)?.IR_value ?? 0) - (getTableCalculated(rowB.original)?.IR_value ?? 0),
        },
        {
            id: "iof",
            header: "IOF",
            cell: ({ row }) => {
                const tableCalc = getTableCalculated(row.original)
                const iof = tableCalc?.IOF ?? 0
                const iofValue = tableCalc?.IOF_value ?? 0

                return (
                    <IofBadge asBadge={false} iofPercent={iof} iofValue={iofValue} showValue showPercentInTooltip className="whitespace-nowrap" investmentDate={row.original.date_buy} />
                )
            },
            sortingFn: (rowA, rowB) =>
                (getTableCalculated(rowA.original)?.IOF_value ?? 0) - (getTableCalculated(rowB.original)?.IOF_value ?? 0),
        },
        {
            id: "profit_liq",
            header: "Lucro líquido",
            cell: ({ row }) => {
                const profit = getTableCalculated(row.original)?.profit_liq ?? 0
                return (
                    <div className="flex items-center gap-1.5 whitespace-nowrap">
                        <span className="text-green-600 dark:text-green-400">
                            R$ {formatCurrency(profit)}
                        </span>
                        {/* {pct > 0 && (
                            <Badge variant="outline" className="text-xs text-green-600 border-green-200 dark:text-green-400 dark:border-green-800">
                                <TrendingUp className="h-3 w-3 mr-0.5" />
                                +{pct.toLocaleString("pt-BR", { minimumFractionDigits: 1, maximumFractionDigits: 1 })}%
                            </Badge>
                        )} */}
                    </div>
                )
            },
            sortingFn: (rowA, rowB) =>
                (getTableCalculated(rowA.original)?.profit_liq ?? 0) - (getTableCalculated(rowB.original)?.profit_liq ?? 0),
        },
        {
            id: "value_liq",
            accessorFn: (row) => getTableCalculated(row)?.value_liq ?? 0,
            sortDescFirst: false,
            header: ({ column }) => (
                <Tooltip>
                    <TooltipTrigger asChild>
                        <Button
                            variant="ghost"
                            className="cursor-pointer -ml-3 h-auto py-1 text-left whitespace-normal"
                            onClick={() => column.toggleSorting(column.getIsSorted() === "asc")}
                        >
                            Valor líquido
                            <ArrowUpDown className="ml-1 h-4 w-4" />
                        </Button>
                    </TooltipTrigger>
                    <TooltipContent className="max-w-xs">
                        {VALUE_LIQUID_CURRENT_HINT}
                    </TooltipContent>
                </Tooltip>
            ),
            cell: ({ row }) => (
                <Tooltip>
                    <TooltipTrigger asChild>
                        <span className="text-green-600 dark:text-green-400 whitespace-nowrap cursor-help">
                            R$ {formatCurrency(getTableCalculated(row.original)?.value_liq ?? 0)}
                        </span>
                    </TooltipTrigger>
                    <TooltipContent className="max-w-xs">
                        {VALUE_LIQUID_CURRENT_HINT}
                    </TooltipContent>
                </Tooltip>
            ),
        },
        {
            id: "due_date",
            accessorFn: (row) => getDueDateSortValue(row.due_date),
            header: ({ column }) => (
                <Button
                    variant="ghost"
                    className="cursor-pointer -ml-3 h-auto py-1 text-left whitespace-normal"
                    onClick={() => column.toggleSorting(column.getIsSorted() === "asc")}
                >
                    Vencimento
                    <ArrowUpDown className="ml-1 h-4 w-4" />
                </Button>
            ),
            cell: ({ row }) =>
                row.original.due_date ? (
                    isDueDatePast(row.original.due_date) ? (
                        <Tooltip>
                            <TooltipTrigger asChild>
                                <span
                                    className="whitespace-nowrap cursor-help font-medium text-red-600 dark:text-red-400"
                                >
                                    {formatDate(row.original.due_date)}
                                </span>
                            </TooltipTrigger>
                            <TooltipContent className="max-w-xs">
                                {OVERDUE_INVESTMENT_HINT}
                            </TooltipContent>
                        </Tooltip>
                    ) : (
                        <span
                            className={`whitespace-nowrap ${isDueDateTodayOrPast(row.original.due_date) ? "text-red-600 dark:text-red-400 font-medium" : ""}`}
                        >
                            {formatDate(row.original.due_date)}
                        </span>
                    )
                ) : (
                    <Badge variant="secondary" className="text-xs bg-green-500/10 text-green-700 dark:text-green-400 border-green-500/20">
                        Liquidez diária
                    </Badge>
                ),
        },
        {
            id: "actions",
            header: () => null,
            cell: ({ row }) => (
                <ActionsCell
                    investment={row.original}
                    onView={onView}
                    onEdit={onEdit}
                    onRedeem={onRedeem}
                    onReinvest={onReinvest}
                    onArchive={onArchive}
                    onDelete={onDelete}
                />
            ),
            enableSorting: false,
        },
    ]
}

// ── Main Component ────────────────────────────────────────────────────────────

export default function InvestmentsDataTable({ investments, setReload, onReinvest, onInvestmentModalOpen }) {
    const [sorting, setSorting] = useState([{ id: "due_date", desc: false }])
    const [pagination, setPagination] = useState({ pageIndex: 0, pageSize: 20 })

    // Shared dialog state (for double-click + actions menu)
    const [selectedInvestment, setSelectedInvestment] = useState(null)
    const [viewOpen, setViewOpen] = useState(false)
    const [editOpen, setEditOpen] = useState(false)
    const [redeemOpen, setRedeemOpen] = useState(false)
    const [selectedRedemption, setSelectedRedemption] = useState(null)
    const [editRedemptionOpen, setEditRedemptionOpen] = useState(false)
    const [deleteRedemptionOpen, setDeleteRedemptionOpen] = useState(false)
    const [deleteOpen, setDeleteOpen] = useState(false)
    const [archiveOpen, setArchiveOpen] = useState(false)

    const notifyInvestmentModalOpen = () => {
        onInvestmentModalOpen?.()
    }

    const openView = (inv) => { setSelectedInvestment(inv); setViewOpen(true) }
    const openEdit = (inv) => { setSelectedInvestment(inv); setEditOpen(true) }
    const openRedeem = (inv) => { notifyInvestmentModalOpen(); setSelectedInvestment(inv); setRedeemOpen(true) }
    const handleReinvest = (inv) => { notifyInvestmentModalOpen(); onReinvest?.(inv) }
    const openEditRedemption = (redemption) => { setSelectedRedemption(redemption); setEditRedemptionOpen(true) }
    const openDeleteRedemption = (redemption) => { setSelectedRedemption(redemption); setDeleteRedemptionOpen(true) }
    const openDelete = (inv) => { notifyInvestmentModalOpen(); setSelectedInvestment(inv); setDeleteOpen(true) }
    const openArchive = (inv) => { notifyInvestmentModalOpen(); setSelectedInvestment(inv); setArchiveOpen(true) }

    React.useEffect(() => {
        setPagination((current) => ({ ...current, pageIndex: 0 }))
    }, [investments])

    const handleDelete = () => {
        if (!selectedInvestment) return
        axiosInstance
            .delete(`/Investments/${selectedInvestment.id}`)
            .then(() => {
                setDeleteOpen(false)
                setSelectedInvestment(null)
                setReload(Math.floor(Math.random() * 10000) + 1)
            })
            .catch((err) => console.error("Erro ao excluir:", err))
    }

    const handleArchive = () => {
        if (!selectedInvestment) return

        axiosInstance
            .patch(`/Investments/${selectedInvestment.id}/archive`, {
                archived: !selectedInvestment.archived,
            })
            .then(() => {
                setArchiveOpen(false)
                setSelectedInvestment(null)
                setReload(Math.floor(Math.random() * 10000) + 1)
            })
            .catch((err) => console.error("Erro ao arquivar investimento:", err))
    }

    const handleDeleteRedemption = () => {
        if (!selectedRedemption) return

        axiosInstance
            .delete(`/Redemptions/${selectedRedemption.id}`)
            .then(() => {
                setDeleteRedemptionOpen(false)
                setSelectedRedemption(null)
                setViewOpen(false)
                setSelectedInvestment(null)
                setReload(Math.floor(Math.random() * 10000) + 1)
            })
            .catch((err) => console.error("Erro ao excluir resgate:", err))
    }

    const columns = getColumns(openView, openEdit, openRedeem, handleReinvest, openArchive, openDelete)

    const totals = React.useMemo(() => sumInvestments(investments), [investments])

    const table = useReactTable({
        data: investments,
        columns,
        state: { sorting, pagination },
        onSortingChange: setSorting,
        onPaginationChange: setPagination,
        getCoreRowModel: getCoreRowModel(),
        getSortedRowModel: getSortedRowModel(),
        getPaginationRowModel: getPaginationRowModel(),
        getRowId: (row) => row.id,
    })

    return (
        <div className="space-y-4">
            <div className="overflow-hidden rounded-lg border">
                <Table className="table-fixed">
                    <TableHeader className="bg-muted sticky top-0 z-10">
                        {table.getHeaderGroups().map((headerGroup) => (
                            <TableRow key={headerGroup.id}>
                                {headerGroup.headers.map((header) => (
                                    <TableHead
                                        key={header.id}
                                        colSpan={header.colSpan}
                                        className={
                                            header.id === "title" ? "w-[15%]" :
                                            header.id === "bank" ? "w-[12%]" :
                                            header.id === "index" ? "w-[10%]" :
                                            header.id === "value" ? "w-[9%]" :
                                            header.id === "date_buy" ? "w-[10%]" :
                                            header.id === "redeemed_value" ? "w-[10%]" :
                                            header.id === "ir" ? "w-[7%]" :
                                            header.id === "iof" ? "w-[7%]" :
                                            header.id === "profit_liq" ? "w-[9%]" :
                                            header.id === "value_liq" ? "w-[9%]" :
                                            header.id === "due_date" ? "w-[8%]" :
                                            header.id === "actions" ? "w-[48px]" :
                                            ""
                                        }
                                    >
                                        {header.isPlaceholder
                                            ? null
                                            : flexRender(header.column.columnDef.header, header.getContext())}
                                    </TableHead>
                                ))}
                            </TableRow>
                        ))}
                    </TableHeader>
                    <TableBody>
                        {table.getRowModel().rows?.length ? (
                            table.getRowModel().rows.map((row) => (
                                <TableRow
                                    key={row.id}
                                    className="cursor-pointer"
                                    onDoubleClick={() => openView(row.original)}
                                >
                                    {row.getVisibleCells().map((cell) => (
                                        <TableCell
                                            key={cell.id}
                                            className={
                                                cell.column.id === "title" || cell.column.id === "bank"
                                                    ? "whitespace-normal break-words"
                                                    : cell.column.id === "actions"
                                                        ? "w-[48px]"
                                                        : ""
                                            }
                                        >
                                            {flexRender(cell.column.columnDef.cell, cell.getContext())}
                                        </TableCell>
                                    ))}
                                </TableRow>
                            ))
                        ) : (
                            <TableRow>
                                <TableCell colSpan={columns.length} className="h-24 text-center text-muted-foreground">
                                    Nenhum investimento encontrado.
                                </TableCell>
                            </TableRow>
                        )}
                    </TableBody>
                    {investments.length > 0 && (
                        <TableFooter>
                            <TableRow className="hover:bg-transparent">
                                <TableCell className="font-semibold">Totais</TableCell>
                                <TableCell className="text-muted-foreground">
                                    {investments.length} investimento(s)
                                </TableCell>
                                <TableCell />
                                <TableCell>R$ {formatCurrency(totals.value)}</TableCell>
                                <TableCell />
                                <TableCell>R$ {formatCurrency(totals.redeemed_value)}</TableCell>
                                <TableCell>R$ {formatCurrency(totals.ir)}</TableCell>
                                <TableCell>R$ {formatCurrency(totals.iof)}</TableCell>
                                <TableCell className="text-green-600 dark:text-green-400">
                                    R$ {formatCurrency(totals.profit_liq)}
                                </TableCell>
                                <TableCell className="text-green-600 dark:text-green-400">
                                    R$ {formatCurrency(totals.value_liq)}
                                </TableCell>
                                <TableCell />
                                <TableCell />
                            </TableRow>
                        </TableFooter>
                    )}
                </Table>
            </div>

            {/* Pagination */}
            {table.getPageCount() > 1 && (
                <div className="flex items-center justify-between px-2">
                    <div className="text-muted-foreground text-sm hidden lg:block">
                        {investments.length} investimento(s)
                    </div>
                    <div className="flex w-full items-center gap-6 lg:w-fit">
                        <div className="hidden items-center gap-2 lg:flex">
                            <Label htmlFor="rows-per-page" className="text-sm font-medium">
                                Linhas por página
                            </Label>
                                <Select
                                    value={`${table.getState().pagination.pageSize}`}
                                    onValueChange={(value) => table.setPageSize(Number(value))}
                                >
                                    <SelectTrigger size="sm" className="w-20 cursor-pointer" id="rows-per-page">
                                    <SelectValue />
                                </SelectTrigger>
                                <SelectContent side="top">
                                    {[10, 20, 50].map((size) => (
                                        <SelectItem key={size} value={`${size}`}>{size}</SelectItem>
                                    ))}
                                </SelectContent>
                            </Select>
                        </div>
                        <div className="flex w-fit items-center justify-center text-sm font-medium">
                            Página {table.getState().pagination.pageIndex + 1} de{" "}
                            {table.getPageCount()}
                        </div>
                        <div className="ml-auto flex items-center gap-2 lg:ml-0">
                            <Button
                                variant="outline"
                                className="hidden h-8 w-8 p-0 lg:flex cursor-pointer"
                                onClick={() => table.setPageIndex(0)}
                                disabled={!table.getCanPreviousPage()}
                            >
                                <ChevronsLeft />
                            </Button>
                            <Button
                                variant="outline"
                                size="icon"
                                className="size-8 cursor-pointer"
                                onClick={() => table.previousPage()}
                                disabled={!table.getCanPreviousPage()}
                            >
                                <ChevronLeft />
                            </Button>
                            <Button
                                variant="outline"
                                size="icon"
                                className="size-8 cursor-pointer"
                                onClick={() => table.nextPage()}
                                disabled={!table.getCanNextPage()}
                            >
                                <ChevronRight />
                            </Button>
                            <Button
                                variant="outline"
                                className="hidden size-8 lg:flex cursor-pointer"
                                size="icon"
                                onClick={() => table.setPageIndex(table.getPageCount() - 1)}
                                disabled={!table.getCanNextPage()}
                            >
                                <ChevronsRight />
                            </Button>
                        </div>
                    </div>
                </div>
            )}

            <div className="rounded-lg border border-dashed p-3 text-xs text-muted-foreground">
                Regras:
                IR regressivo conforme prazo da aplicação: 22,5% até 180 dias, 20% até 365 dias, 17,5% até 730 dias e 15% acima disso.
                IOF regressivo apenas nos primeiros 30 dias; após 30 dias, a alíquota é zero.
            </div>

            {/* Shared View Dialog */}
            <ViewDialog
                investment={selectedInvestment}
                open={viewOpen}
                onOpenChange={setViewOpen}
                onEdit={() => openEdit(selectedInvestment)}
                onRedeem={() => openRedeem(selectedInvestment)}
                onReinvest={() => handleReinvest(selectedInvestment)}
                onArchive={() => openArchive(selectedInvestment)}
                onEditRedemption={openEditRedemption}
                onDeleteRedemption={openDeleteRedemption}
                onDelete={() => openDelete(selectedInvestment)}
            />

            {/* Shared Edit Dialog */}
            {editOpen && selectedInvestment && (
                <InvestmentsEdit
                    investment={selectedInvestment}
                    setReload={setReload}
                    externalOpen={editOpen}
                    onExternalClose={() => { setEditOpen(false); setSelectedInvestment(null) }}
                />
            )}

            {/* Shared Redeem Dialog */}
            {redeemOpen && selectedInvestment && (
                <InvestmentsRedeem
                    investment={selectedInvestment}
                    setReload={setReload}
                    externalOpen={redeemOpen}
                    onExternalClose={() => { setRedeemOpen(false); setSelectedInvestment(null) }}
                />
            )}

            {/* Shared Redemption Edit Dialog */}
            {editRedemptionOpen && selectedRedemption && (
                <RedemptionEdit
                    redemption={selectedRedemption}
                    setReload={setReload}
                    externalOpen={editRedemptionOpen}
                    onExternalClose={() => { setEditRedemptionOpen(false); setSelectedRedemption(null) }}
                />
            )}

            <DeleteRedemptionDialog
                redemption={selectedRedemption}
                open={deleteRedemptionOpen}
                onOpenChange={(open) => {
                    setDeleteRedemptionOpen(open)
                    if (!open) setSelectedRedemption(null)
                }}
                onConfirm={handleDeleteRedemption}
            />

            {/* Shared Delete Dialog */}
            <DeleteDialog
                investment={selectedInvestment}
                open={deleteOpen}
                onOpenChange={setDeleteOpen}
                onConfirm={handleDelete}
            />

            <ArchiveDialog
                investment={selectedInvestment}
                open={archiveOpen}
                onOpenChange={setArchiveOpen}
                onConfirm={handleArchive}
            />
        </div>
    )
}
