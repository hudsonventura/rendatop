import * as React from "react"
import { Area, AreaChart, CartesianGrid, ReferenceLine, ResponsiveContainer, Tooltip, XAxis, YAxis } from "recharts"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Skeleton } from "@/components/ui/skeleton"

const DAY_MS = 24 * 60 * 60 * 1000
const SELIC_ANNUAL_ESTIMATE = 0.1315
const IPCA_ANNUAL_ESTIMATE = 0.045

function startOfDay(date) {
    const normalized = new Date(date)
    normalized.setHours(0, 0, 0, 0)
    return normalized
}

function addDays(date, days) {
    const next = new Date(date)
    next.setDate(next.getDate() + days)
    return next
}

function diffInDays(start, end) {
    return Math.floor((startOfDay(end).getTime() - startOfDay(start).getTime()) / DAY_MS)
}

function getIRPercent(taxes, days) {
    if (!taxes) return 0
    if (days <= 180) return 22.5
    if (days <= 365) return 20
    if (days <= 730) return 17.5
    return 15
}

function getIOFPercent(days) {
    if (days >= 30) return 0
    return 100 - days * 3.333333333333
}

function formatCurrency(value) {
    return new Intl.NumberFormat("pt-BR", {
        style: "currency",
        currency: "BRL",
        minimumFractionDigits: 2,
    }).format(value)
}

function estimateLiquidValue(investment, date) {
    const investedValue = Number(investment.value ?? 0)
    if (investedValue <= 0 || !investment.date_buy) return 0

    const startDate = startOfDay(new Date(investment.date_buy))
    const finishDate = investment.due_date ? startOfDay(new Date(investment.due_date)) : null
    const currentDate = finishDate && startOfDay(date) > finishDate
        ? finishDate
        : startOfDay(date)

    if (Number.isNaN(startDate.getTime()) || currentDate < startDate) {
        return 0
    }

    const days = diffInDays(startDate, currentDate)
    if (days <= 0) {
        return investedValue
    }

    const taxes = investment.taxes ?? true
    const irRate = getIRPercent(taxes, days) / 100
    const iofRate = getIOFPercent(days) / 100
    const indexPercent = Number(investment.index_percent ?? 0)

    let effectivePercent = 0

    if (investment.index === "CDI") {
        effectivePercent = (SELIC_ANNUAL_ESTIMATE * (indexPercent / 100) * days) / 365
    } else if (investment.index === "IPCA_MAIS") {
        effectivePercent = ((IPCA_ANNUAL_ESTIMATE + indexPercent / 100) * Math.max(days - 3, 0)) / 366
    } else {
        effectivePercent = ((indexPercent / 100) * Math.max(days - 3, 0)) / 366
    }

    const grossProfit = investedValue * effectivePercent
    const grossAfterIof = grossProfit * (1 - iofRate)
    const netProfit = grossAfterIof * (1 - irRate)

    return investedValue + netProfit
}

function buildTimelineData(investments) {
    const validInvestments = (investments ?? []).filter((investment) => investment?.date_buy)
    if (validInvestments.length === 0) return []

    const firstInvestmentDate = validInvestments.reduce((earliest, investment) => {
        const date = startOfDay(new Date(investment.date_buy))
        return date < earliest ? date : earliest
    }, startOfDay(new Date(validInvestments[0].date_buy)))

    const today = startOfDay(new Date())
    const lastInvestmentDate = validInvestments.reduce((latest, investment) => {
        if (!investment?.due_date) return latest

        const dueDate = startOfDay(new Date(investment.due_date))
        if (Number.isNaN(dueDate.getTime())) return latest

        return dueDate > latest ? dueDate : latest
    }, today)

    const chartEndDate = lastInvestmentDate > today ? lastInvestmentDate : today
    const totalDays = diffInDays(firstInvestmentDate, chartEndDate)
    const chartData = []

    for (let offset = 0; offset <= totalDays; offset += 1) {
        const currentDate = addDays(firstInvestmentDate, offset)
        const liquidValue = validInvestments.reduce(
            (sum, investment) => sum + estimateLiquidValue(investment, currentDate),
            0
        )

        chartData.push({
            date: currentDate.toISOString(),
            liquidValue: Number(liquidValue.toFixed(2)),
        })
    }

    return chartData
}

function TimelineSkeleton() {
    return (
        <Card>
            <CardHeader className="pb-2">
                <CardTitle>Evolução da carteira</CardTitle>
                <CardDescription>Valor líquido total desde o primeiro investimento até hoje</CardDescription>
            </CardHeader>
            <CardContent>
                <Skeleton className="h-[320px] w-full rounded-xl" />
            </CardContent>
        </Card>
    )
}

export default function PortfolioTimelineChart({ investments }) {
    const chartData = React.useMemo(() => buildTimelineData(investments), [investments])
    const todayIso = React.useMemo(() => startOfDay(new Date()).toISOString(), [])

    if (!investments) {
        return <TimelineSkeleton />
    }

    if (chartData.length === 0) {
        return (
            <Card>
                <CardHeader>
                    <CardTitle>Evolução da carteira</CardTitle>
                    <CardDescription>Valor líquido total desde o primeiro investimento até hoje</CardDescription>
                </CardHeader>
                <CardContent>
                    <p className="text-sm text-muted-foreground">
                        Não há investimentos suficientes para montar a linha do tempo.
                    </p>
                </CardContent>
            </Card>
        )
    }

    return (
        <Card>
            <CardHeader className="pb-2">
                <CardTitle>Evolução da carteira</CardTitle>
                <CardDescription>
                    Valor líquido total do primeiro investimento até hoje, incluindo itens arquivados.
                </CardDescription>
            </CardHeader>
            <CardContent>
                <div className="h-[320px] w-full">
                    <ResponsiveContainer width="100%" height="100%">
                        <AreaChart data={chartData} margin={{ top: 8, right: 8, left: 8, bottom: 8 }}>
                            <defs>
                                <linearGradient id="portfolio-liquid-fill" x1="0" y1="0" x2="0" y2="1">
                                    <stop offset="5%" stopColor="var(--chart-1)" stopOpacity={0.35} />
                                    <stop offset="95%" stopColor="var(--chart-1)" stopOpacity={0.05} />
                                </linearGradient>
                            </defs>
                            <CartesianGrid vertical={false} strokeDasharray="3 3" />
                            <XAxis
                                dataKey="date"
                                tickLine={false}
                                axisLine={false}
                                minTickGap={32}
                                tickFormatter={(value) =>
                                    new Date(value).toLocaleDateString("pt-BR", {
                                        month: "short",
                                        year: "2-digit",
                                    })
                                }
                            />
                            <YAxis
                                tickLine={false}
                                axisLine={false}
                                width={96}
                                tickFormatter={(value) => formatCurrency(value)}
                            />
                            <Tooltip
                                formatter={(value) => [formatCurrency(Number(value)), "Valor líquido"]}
                                labelFormatter={(value) =>
                                    new Date(value).toLocaleDateString("pt-BR", {
                                        day: "2-digit",
                                        month: "short",
                                        year: "numeric",
                                    })
                                }
                            />
                            <ReferenceLine
                                x={todayIso}
                                stroke="var(--chart-5)"
                                strokeWidth={2}
                                strokeDasharray="6 6"
                                ifOverflow="extendDomain"
                                label={{
                                    value: "Hoje",
                                    position: "top",
                                    fill: "var(--chart-5)",
                                    fontSize: 12,
                                }}
                            />
                            <Area
                                type="monotone"
                                dataKey="liquidValue"
                                stroke="var(--chart-1)"
                                strokeWidth={2}
                                fill="url(#portfolio-liquid-fill)"
                                dot={false}
                                activeDot={{ r: 4 }}
                            />
                        </AreaChart>
                    </ResponsiveContainer>
                </div>
            </CardContent>
        </Card>
    )
}
