import React from "react"
import { Badge } from "@/components/ui/badge"
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

function getDueSnapshot(investment) {
    return investment.calculated?.[1] ?? investment.calculated?.[0]
}

export default function InvestmentsDueSoon({ investments }) {
    if (!investments?.length) {
        return (
            <div className="rounded-lg border p-4 text-sm text-muted-foreground">
                Nenhum vencimento nos próximos 30 dias.
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
                    </TableRow>
                </TableHeader>
                <TableBody>
                    {investments.map((investment) => {
                        const dueSnapshot = getDueSnapshot(investment)
                        return (
                            <TableRow key={investment.id}>
                                <TableCell>
                                    <div className="font-medium">{investment.title}</div>
                                    <div className="text-xs text-muted-foreground">
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
                                        variant={dueSnapshot?.IR > 15 ? "destructive" : "secondary"}
                                        className="whitespace-nowrap"
                                    >
                                        {(dueSnapshot?.IR ?? 0).toLocaleString("pt-BR", {
                                            minimumFractionDigits: 0,
                                            maximumFractionDigits: 1,
                                        })}% · R$ {formatCurrency(dueSnapshot?.IR_value ?? 0)}
                                    </Badge>
                                </TableCell>
                            </TableRow>
                        )
                    })}
                </TableBody>
            </Table>
        </div>
    )
}
