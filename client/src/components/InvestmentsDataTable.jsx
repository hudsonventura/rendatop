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
    TrendingUp,
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
import { getIrBadgeClass } from "@/utils/ir-level"

// ── Helpers ───────────────────────────────────────────────────────────────────

const formatCurrency = (val) =>
    val.toLocaleString("pt-BR", { minimumFractionDigits: 2, maximumFractionDigits: 2 })

const formatDate = (dateStr) =>
    new Date(dateStr).toLocaleDateString("pt-BR")

const isDueDateTodayOrPast = (dateStr) => {
    if (!dateStr) return false
    const due = new Date(dateStr)
    due.setHours(0, 0, 0, 0)

    const today = new Date()
    today.setHours(0, 0, 0, 0)
    return due <= today
}

const canShowReinvest = (dateStr) => {
    if (!dateStr) return false

    const due = new Date(dateStr)
    due.setHours(0, 0, 0, 0)

    const today = new Date()
    today.setHours(0, 0, 0, 0)

    return due <= today
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

// ── View Dialog (investment details) ──────────────────────────────────────────

function ViewDialog({ investment, open, onOpenChange, onEdit, onRedeem, onReinvest, onEditRedemption, onArchive, onDelete }) {
    if (!investment) return null

    const calc = investment.calculated?.[0]
    const calcDue = investment.calculated?.[1]
    const hasDueEstimate = Boolean(investment.due_date && calcDue)
    const showReinvest = canShowReinvest(investment.due_date)
    const canArchive = investment.archived || isDueDateTodayOrPast(investment.due_date)
    const redemptions = [...(investment.redemptions ?? [])].sort(
        (a, b) => new Date(b.date).getTime() - new Date(a.date).getTime()
    )

    return (
        <Dialog open={open} onOpenChange={onOpenChange}>
            <DialogContent className="w-[70vw] max-w-[70vw] sm:max-w-6xl max-h-[60vh] overflow-y-auto">
                <DialogHeader>
                    <DialogTitle>{investment.title}</DialogTitle>
                    <DialogDescription>
                        {investment.bank?.name || "Banco Desconhecido"} · {getIndexLabel(investment)}
                    </DialogDescription>
                </DialogHeader>
                <div className="space-y-3 rounded-md border p-3 min-w-0">
                    <h4 className="text-sm font-semibold">Valor atual do investimento</h4>

                    <div className="grid grid-cols-1 md:grid-cols-[max-content_1fr] items-start md:items-center gap-x-2 gap-y-2">
                        <span className="text-xs text-muted-foreground md:whitespace-nowrap">Valores atuais:</span>
                        <div className="min-w-0">
                            <div className="flex flex-wrap items-center gap-1.5">
                                <IofBadge iofPercent={calc.IOF} iofValue={calc.IOF_value} showValue className="text-xs whitespace-nowrap" />
                                <IrBadge irPercent={calc.IR} irValue={calc.IR_value} showValue className="text-xs whitespace-nowrap" />
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
                                    <IofBadge iofPercent={calcDue.IOF} iofValue={calcDue.IOF_value} showValue className="text-xs whitespace-nowrap" />
                                    <IrBadge irPercent={calcDue.IR} irValue={calcDue.IR_value} showValue className="text-xs whitespace-nowrap" />
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
                                                R$ {formatCurrency(redemption.value)}
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

// ── Actions Cell ──────────────────────────────────────────────────────────────

function ActionsCell({ investment, onView, onEdit, onRedeem, onReinvest, onDelete }) {
    const [menuOpen, setMenuOpen] = useState(false)
    const [menuPos, setMenuPos] = useState({ top: 0, left: 0 })
    const btnRef = React.useRef(null)
    const menuRef = React.useRef(null)
    const showReinvest = canShowReinvest(investment.due_date)

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
                    <button
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
                    </button>
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

function getColumns(setReload, onView, onEdit, onRedeem, onReinvest, onDelete) {
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
                <span className="font-medium">{row.original.title}</span>
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
                const bankName = row.original.bank?.name || "Banco Desconhecido"
                return <span className="whitespace-nowrap">{bankName}</span>
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
                    className="cursor-pointer -ml-3"
                    onClick={() => column.toggleSorting(column.getIsSorted() === "asc")}
                >
                    Valor investido
                    <ArrowUpDown className="ml-1 h-4 w-4" />
                </Button>
            ),
            cell: ({ row }) => (
                <span className="whitespace-nowrap">R$ {formatCurrency(row.original.value)}</span>
            ),
        },
        {
            id: "ir",
            header: "IR",
            cell: ({ row }) => {
                const ir = row.original.calculated?.[0]?.IR ?? 0
                const irValue = row.original.calculated?.[0]?.IR_value ?? 0

                return (
                    <IrBadge asBadge={false} irPercent={ir} irValue={irValue} showValue showPercentInTooltip className="whitespace-nowrap" />
                )
            },
            sortingFn: (rowA, rowB) =>
                (rowA.original.calculated?.[0]?.IR_value ?? 0) - (rowB.original.calculated?.[0]?.IR_value ?? 0),
        },
        {
            id: "iof",
            header: "IOF",
            cell: ({ row }) => {
                const iof = row.original.calculated?.[0]?.IOF ?? 0
                const iofValue = row.original.calculated?.[0]?.IOF_value ?? 0

                return (
                    <IofBadge asBadge={false} iofPercent={iof} iofValue={iofValue} showValue showPercentInTooltip className="whitespace-nowrap" />
                )
            },
            sortingFn: (rowA, rowB) =>
                (rowA.original.calculated?.[0]?.IOF_value ?? 0) - (rowB.original.calculated?.[0]?.IOF_value ?? 0),
        },
        {
            id: "profit_liq",
            header: "Lucro líquido",
            cell: ({ row }) => {
                const profit = row.original.calculated[0].profit_liq
                const invested = row.original.value
                const pct = invested > 0 ? (profit / invested * 100) : 0
                return (
                    <div className="flex items-center gap-1.5 whitespace-nowrap">
                        <span className="text-green-600 dark:text-green-400">
                            R$ {formatCurrency(profit)}
                        </span>
                        {pct > 0 && (
                            <Badge variant="outline" className="text-xs text-green-600 border-green-200 dark:text-green-400 dark:border-green-800">
                                <TrendingUp className="h-3 w-3 mr-0.5" />
                                +{pct.toLocaleString("pt-BR", { minimumFractionDigits: 1, maximumFractionDigits: 1 })}%
                            </Badge>
                        )}
                    </div>
                )
            },
            sortingFn: (rowA, rowB) =>
                rowA.original.calculated[0].profit_liq - rowB.original.calculated[0].profit_liq,
        },
        {
            id: "value_liq",
            header: "Valor líquido",
            cell: ({ row }) => (
                <span className="text-green-600 dark:text-green-400 whitespace-nowrap">
                    R$ {formatCurrency(row.original.calculated[0].value_liq)}
                </span>
            ),
            sortingFn: (rowA, rowB) =>
                rowA.original.calculated[0].value_liq - rowB.original.calculated[0].value_liq,
        },
        {
            id: "due_date",
            header: ({ column }) => (
                <Button
                    variant="ghost"
                    className="cursor-pointer -ml-3"
                    onClick={() => column.toggleSorting(column.getIsSorted() === "asc")}
                >
                    Vencimento
                    <ArrowUpDown className="ml-1 h-4 w-4" />
                </Button>
            ),
            cell: ({ row }) =>
                row.original.due_date ? (
                    <span
                        className={`whitespace-nowrap ${isDueDateTodayOrPast(row.original.due_date) ? "text-red-600 dark:text-red-400 font-medium" : ""}`}
                    >
                        {formatDate(row.original.due_date)}
                    </span>
                ) : (
                    <Badge variant="secondary" className="text-xs bg-green-500/10 text-green-700 dark:text-green-400 border-green-500/20">
                        Liquidez diária
                    </Badge>
                ),
            sortingFn: (rowA, rowB) => {
                const a = rowA.original.due_date ? new Date(rowA.original.due_date).getTime() : 0
                const b = rowB.original.due_date ? new Date(rowB.original.due_date).getTime() : 0
                return a - b
            },
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
                    onDelete={onDelete}
                />
            ),
            enableSorting: false,
        },
    ]
}

// ── Main Component ────────────────────────────────────────────────────────────

export default function InvestmentsDataTable({ investments, setReload, onReinvest }) {
    const [sorting, setSorting] = useState([])
    const [pagination, setPagination] = useState({ pageIndex: 0, pageSize: 10 })

    // Shared dialog state (for double-click + actions menu)
    const [selectedInvestment, setSelectedInvestment] = useState(null)
    const [viewOpen, setViewOpen] = useState(false)
    const [editOpen, setEditOpen] = useState(false)
    const [redeemOpen, setRedeemOpen] = useState(false)
    const [selectedRedemption, setSelectedRedemption] = useState(null)
    const [editRedemptionOpen, setEditRedemptionOpen] = useState(false)
    const [deleteOpen, setDeleteOpen] = useState(false)
    const [archiveOpen, setArchiveOpen] = useState(false)

    const openView = (inv) => { setSelectedInvestment(inv); setViewOpen(true) }
    const openEdit = (inv) => { setSelectedInvestment(inv); setEditOpen(true) }
    const openRedeem = (inv) => { setSelectedInvestment(inv); setRedeemOpen(true) }
    const handleReinvest = (inv) => { onReinvest?.(inv) }
    const openEditRedemption = (redemption) => { setSelectedRedemption(redemption); setEditRedemptionOpen(true) }
    const openDelete = (inv) => { setSelectedInvestment(inv); setDeleteOpen(true) }
    const openArchive = (inv) => { setSelectedInvestment(inv); setArchiveOpen(true) }

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

    const columns = React.useMemo(
        () => getColumns(setReload, openView, openEdit, openRedeem, handleReinvest, openDelete),
        [setReload, onReinvest]
    )

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
                <Table>
                    <TableHeader className="bg-muted sticky top-0 z-10">
                        {table.getHeaderGroups().map((headerGroup) => (
                            <TableRow key={headerGroup.id}>
                                {headerGroup.headers.map((header) => (
                                    <TableHead key={header.id} colSpan={header.colSpan}>
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
                                        <TableCell key={cell.id}>
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
                </Table>
            </div>

            {/* Pagination */}
            {investments.length > 10 && (
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
