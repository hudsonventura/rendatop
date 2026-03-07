import React, { useState } from "react"
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
import {
    DropdownMenu,
    DropdownMenuContent,
    DropdownMenuItem,
    DropdownMenuSeparator,
    DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
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
import axiosInstance from "@/utils/axiosConfig"

// ── Helpers ───────────────────────────────────────────────────────────────────

const formatCurrency = (val) =>
    val.toLocaleString("pt-BR", { minimumFractionDigits: 2, maximumFractionDigits: 2 })

const formatDate = (dateStr) =>
    new Date(dateStr).toLocaleDateString("pt-BR")

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

function ViewDialog({ investment, open, onOpenChange }) {
    if (!investment) return null

    const calc = investment.calculated?.[0]
    const calcDue = investment.calculated?.[1]

    const items = [
        {
            label: `IR (${calc.IR.toLocaleString("pt-BR", { minimumFractionDigits: 0, maximumFractionDigits: 1 })}%)`,
            value: `R$ ${formatCurrency(calc.IR_value)}`,
            variant: calc.IR > 15 ? "destructive" : "secondary",
        },
        {
            label: `IOF (${calc.IOF.toLocaleString("pt-BR", { maximumFractionDigits: 0 })}%)`,
            value: `R$ ${formatCurrency(calc.IOF_value)}`,
            variant: calc.IOF > 0 ? "destructive" : "secondary",
        },
        {
            label: "Valor bruto",
            value: `R$ ${formatCurrency(calc.value_brute)}`,
            variant: "default",
        },
        {
            label: "Valor líquido atual",
            value: `R$ ${formatCurrency(calc.value_liq)}`,
            variant: "default",
        },
        {
            label: "Rend. líq. estimado no venc.",
            value: `R$ ${investment.due_date
                ? formatCurrency(calcDue.profit_liq)
                : formatCurrency(calc.profit_liq) + " *"}`,
            variant: "default",
        },
        {
            label: "Valor líq. estimado no venc.",
            value: `R$ ${investment.due_date
                ? formatCurrency(calcDue.value_liq)
                : formatCurrency(calc.value_liq) + " *"}`,
            variant: "default",
        },
    ]

    return (
        <Dialog open={open} onOpenChange={onOpenChange}>
            <DialogContent className="max-w-xl">
                <DialogHeader>
                    <DialogTitle>{investment.title}</DialogTitle>
                    <DialogDescription>
                        {investment.bank} · {getIndexLabel(investment)}
                    </DialogDescription>
                </DialogHeader>
                <div className="grid grid-cols-2 md:grid-cols-3 gap-4">
                    {items.map((item, i) => (
                        <div key={i} className="flex flex-col gap-1">
                            <span className="text-xs text-muted-foreground">{item.label}</span>
                            <Badge variant={item.variant} className="w-fit text-xs">
                                {item.value}
                            </Badge>
                        </div>
                    ))}
                </div>
                {!investment.due_date && (
                    <p className="text-xs text-muted-foreground">
                        * Valores estimados baseados na data atual (liquidez diária)
                    </p>
                )}
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

// ── Actions Cell ──────────────────────────────────────────────────────────────

function ActionsCell({ investment, setReload }) {
    const [viewOpen, setViewOpen] = useState(false)
    const [editOpen, setEditOpen] = useState(false)
    const [deleteOpen, setDeleteOpen] = useState(false)

    const handleDelete = () => {
        axiosInstance
            .delete(`/Investments/${investment.id}`)
            .then(() => {
                setDeleteOpen(false)
                setReload(Math.floor(Math.random() * 10000) + 1)
            })
            .catch((err) => console.error("Erro ao excluir:", err))
    }

    return (
        <>
            <DropdownMenu>
                <DropdownMenuTrigger asChild>
                    <Button
                        variant="ghost"
                        className="data-[state=open]:bg-muted text-muted-foreground flex size-8 cursor-pointer"
                        size="icon"
                    >
                        <EllipsisVertical />
                        <span className="sr-only">Abrir menu</span>
                    </Button>
                </DropdownMenuTrigger>
                <DropdownMenuContent align="end" className="w-40">
                    <DropdownMenuItem className="cursor-pointer" onClick={() => setViewOpen(true)}>
                        <Eye className="h-4 w-4 mr-2" />
                        Visualizar
                    </DropdownMenuItem>
                    <DropdownMenuItem className="cursor-pointer" onClick={() => setEditOpen(true)}>
                        <Pencil className="h-4 w-4 mr-2" />
                        Editar
                    </DropdownMenuItem>
                    <DropdownMenuSeparator />
                    <DropdownMenuItem
                        className="cursor-pointer text-destructive focus:text-destructive"
                        onClick={() => setDeleteOpen(true)}
                    >
                        <Trash2 className="h-4 w-4 mr-2" />
                        Excluir
                    </DropdownMenuItem>
                </DropdownMenuContent>
            </DropdownMenu>

            {/* View Dialog */}
            <ViewDialog
                investment={investment}
                open={viewOpen}
                onOpenChange={setViewOpen}
            />

            {/* Edit Dialog — controlled externally */}
            {editOpen && (
                <InvestmentsEdit
                    investment={investment}
                    setReload={setReload}
                    externalOpen={editOpen}
                    onExternalClose={() => setEditOpen(false)}
                />
            )}

            {/* Delete Dialog */}
            <DeleteDialog
                investment={investment}
                open={deleteOpen}
                onOpenChange={setDeleteOpen}
                onConfirm={handleDelete}
            />
        </>
    )
}

// ── Column definitions ────────────────────────────────────────────────────────

function getColumns(setReload) {
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
            id: "profit_liq",
            header: "Lucro líquido",
            cell: ({ row }) => (
                <span className="text-green-600 dark:text-green-400 whitespace-nowrap">
                    R$ {formatCurrency(row.original.calculated[0].profit_liq)}
                </span>
            ),
            sortingFn: (rowA, rowB) =>
                rowA.original.calculated[0].profit_liq - rowB.original.calculated[0].profit_liq,
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
                    <span className="whitespace-nowrap">{formatDate(row.original.due_date)}</span>
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
                <ActionsCell investment={row.original} setReload={setReload} />
            ),
            enableSorting: false,
        },
    ]
}

// ── Main Component ────────────────────────────────────────────────────────────

export default function InvestmentsDataTable({ investments, setReload }) {
    const [sorting, setSorting] = useState([])
    const [pagination, setPagination] = useState({ pageIndex: 0, pageSize: 10 })

    const columns = React.useMemo(() => getColumns(setReload), [setReload])

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
                                <TableRow key={row.id}>
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
        </div>
    )
}
