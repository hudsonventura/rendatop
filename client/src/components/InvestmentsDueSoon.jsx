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

const formatDate = (dateStr) =>
    new Date(dateStr).toLocaleDateString("pt-BR")

const isDueDateTodayOrPast = (dateStr) => {
    const due = new Date(dateStr)
    due.setHours(0, 0, 0, 0)
    const today = new Date()
    today.setHours(0, 0, 0, 0)
    return due <= today
}

function getDueSnapshot(investment) {
    return investment.calculated?.[1] ?? investment.calculated?.[0]
}

function canShowReinvest(dateStr) {
    if (!dateStr) return false

    const due = new Date(dateStr)
    due.setHours(0, 0, 0, 0)

    const today = new Date()
    today.setHours(0, 0, 0, 0)

    return due <= today
}

const archiveReinvestHint = "Você só poderá reinvestir o valor deste investimento ou arquivá-lo quando chegar a data de resgate"

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
                        const canArchive = showReinvest

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
                                        <DropdownMenuContent align="end" className="w-40">
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
                                                Arquivar
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
