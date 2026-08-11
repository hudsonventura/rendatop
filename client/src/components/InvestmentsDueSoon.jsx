import { Archive, CopyPlus, EllipsisVertical } from "lucide-react"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import {
    DropdownMenu,
    DropdownMenuContent,
    DropdownMenuItem,
    DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import { Tooltip, TooltipContent, TooltipTrigger } from "@/components/ui/tooltip"
import { formatIrPercent, getIrBadgeClass } from "@/utils/ir-level"
import {
    Table,
    TableBody,
    TableCell,
    TableHead,
    TableHeader,
    TableRow,
} from "@/components/ui/table"

const formatCurrency = (val) =>
    val.toLocaleString("pt-BR", { minimumFractionDigits: 2, maximumFractionDigits: 2 })
const EARLY_DUE_WINDOW_DAYS = 5

const formatDate = (dateStr) =>
    new Date(dateStr).toLocaleDateString("pt-BR")

function getBankLogoSrc(bankCode) {
    if (bankCode === null || bankCode === undefined || bankCode === "") return null
    return `/bank-logos/${String(bankCode).padStart(3, "0")}.svg`
}

const isDueDateTodayOrPast = (dateStr) => {
    const due = new Date(dateStr)
    due.setHours(0, 0, 0, 0)
    const today = new Date()
    today.setHours(0, 0, 0, 0)
    return due <= today
}

const isDueDateWithinArchiveWindow = (dateStr) => {
    if (!dateStr) return false

    const due = new Date(dateStr)
    due.setHours(0, 0, 0, 0)

    const threshold = new Date()
    threshold.setHours(0, 0, 0, 0)
    threshold.setDate(threshold.getDate() + EARLY_DUE_WINDOW_DAYS)

    return due <= threshold
}

function getDueSnapshot(investment) {
    return investment.calculated?.[1] ?? investment.calculated?.[0]
}

function canShowReinvest(dateStr) {
    return isDueDateWithinArchiveWindow(dateStr)
}

const archiveReinvestHint = "Você só poderá reinvestir o valor deste investimento ou arquivá-lo a partir de 5 dias corridos antes da data de resgate"

function DropdownActionItemWithHint({ disabled, onClick, children }) {
    const item = (
        <DropdownMenuItem
            disabled={disabled}
            onClick={onClick}
        >
            {children}
        </DropdownMenuItem>
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

export default function InvestmentsDueSoon({ investments, onArchive, onReinvest }) {
    if (!investments?.length) {
        return (
            <div className="rounded-lg border p-4 text-sm text-muted-foreground">
                Aqui aparecerão os seus invesmentos cujo vencimento está dentro dos próximos 30 dias
            </div>
        )
    }

    return (
        <div className="overflow-hidden rounded-lg border">
            <Table>
                <TableHeader className="bg-muted">
                    <TableRow>
                        <TableHead>Nome</TableHead>
                        <TableHead>Banco</TableHead>
                        <TableHead>Valor</TableHead>
                        <TableHead>Valor líquido</TableHead>
                        <TableHead>IR</TableHead>
                        <TableHead className="w-[52px]" />
                    </TableRow>
                </TableHeader>
                <TableBody>
                    {investments.map((investment) => {
                        const dueSnapshot = getDueSnapshot(investment)
                        const showReinvest = canShowReinvest(investment.due_date)
                        const canArchive = isDueDateWithinArchiveWindow(investment.due_date)

                        return (
                            <TableRow key={investment.id}>
                                <TableCell>
                                    <div className="font-medium">{investment.title}</div>
                                    <div
                                        className={`text-xs ${isDueDateTodayOrPast(investment.due_date) ? "text-red-600 dark:text-red-400 font-medium" : "text-muted-foreground"}`}
                                    >
                                        Vencimento: {formatDate(investment.due_date)}
                                    </div>
                                </TableCell>
                                <TableCell>
                                    <div className="flex items-center gap-2 min-w-0">
                                        {investment.bank?.code ? (
                                            <img
                                                src={getBankLogoSrc(investment.bank.code)}
                                                alt=""
                                                aria-hidden="true"
                                                className="h-4 w-4 shrink-0 rounded-sm object-contain"
                                                onError={(event) => {
                                                    event.currentTarget.style.display = "none"
                                                }}
                                            />
                                        ) : null}
                                        <span className="truncate">
                                            {investment.bank?.name || "Banco Desconhecido"}
                                        </span>
                                    </div>
                                </TableCell>
                                <TableCell className="whitespace-nowrap">
                                    R$ {formatCurrency(investment.value)}
                                </TableCell>
                                <TableCell className="whitespace-nowrap text-green-600 dark:text-green-400">
                                    R$ {formatCurrency(dueSnapshot?.value_liq ?? investment.value)}
                                </TableCell>
                                <TableCell>
                                    <Badge
                                        variant="secondary"
                                        className={`whitespace-nowrap ${getIrBadgeClass(dueSnapshot?.IR ?? 0)}`}
                                    >
                                        {formatIrPercent(dueSnapshot?.IR ?? 0)}% · R$ {formatCurrency(dueSnapshot?.IR_value ?? 0)}
                                    </Badge>
                                </TableCell>
                                <TableCell className="text-right">
                                    <DropdownMenu>
                                        <DropdownMenuTrigger asChild>
                                            <Button
                                                variant="ghost"
                                                size="icon"
                                                className="size-8 cursor-pointer text-muted-foreground"
                                            >
                                                <EllipsisVertical className="h-4 w-4" />
                                                <span className="sr-only">Abrir menu</span>
                                            </Button>
                                        </DropdownMenuTrigger>
                                        <DropdownMenuContent align="end" className="w-60">
                                            <DropdownActionItemWithHint
                                                disabled={!showReinvest}
                                                onClick={() => {
                                                    if (!showReinvest) return
                                                    onReinvest?.(investment)
                                                }}
                                            >
                                                <CopyPlus className="mr-2 h-4 w-4" />
                                                Reinvestir
                                            </DropdownActionItemWithHint>
                                            <DropdownActionItemWithHint
                                                disabled={!canArchive}
                                                onClick={() => {
                                                    if (!canArchive) return
                                                    onArchive?.(investment)
                                                }}
                                            >
                                                <Archive className="mr-2 h-4 w-4" />
                                                Arquivar / Resgate Total
                                            </DropdownActionItemWithHint>
                                        </DropdownMenuContent>
                                    </DropdownMenu>
                                </TableCell>
                            </TableRow>
                        )
                    })}
                </TableBody>
            </Table>
        </div>
    )
}
