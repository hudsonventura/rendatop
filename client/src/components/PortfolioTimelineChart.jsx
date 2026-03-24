import * as React from "react"
import { Area, AreaChart, CartesianGrid, Legend, ReferenceLine, ResponsiveContainer, Tooltip, XAxis, YAxis } from "recharts"
import { useTheme } from "next-themes"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Button } from "@/components/ui/button"
import { Skeleton } from "@/components/ui/skeleton"

const DAY_MS = 24 * 60 * 60 * 1000
const SELIC_ANNUAL_ESTIMATE = 0.1315
const IPCA_ANNUAL_ESTIMATE = 0.045
const CHART_COLORS = [
    "var(--chart-1)",
    "var(--chart-2)",
    "var(--chart-3)",
    "var(--chart-4)",
    "var(--chart-5)",
]

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

function getBankName(investment) {
    return investment.bank?.name || "Banco Desconhecido"
}

function getBankColor(investment) {
    return investment.bank?.color || null
}

function getBankKey(bankName) {
    return `bank_${bankName
        .normalize("NFD")
        .replace(/[\u0300-\u036f]/g, "")
        .replace(/[^a-zA-Z0-9]+/g, "_")
        .replace(/^_+|_+$/g, "")
        .toLowerCase()}`
}

function estimateLiquidValue(investment, date) {
    const investedValue = Number(investment.value ?? 0)
    if (investedValue <= 0 || !investment.date_buy) return 0

    const startDate = startOfDay(new Date(investment.date_buy))
    const finishDate = investment.due_date ? startOfDay(new Date(investment.due_date)) : null
    const targetDate = startOfDay(date)

    if (finishDate && targetDate > finishDate) {
        return 0
    }

    const currentDate = targetDate

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
    const bankColorMap = new Map()

    for (const investment of validInvestments) {
        const bankName = getBankName(investment)
        const bankColor = getBankColor(investment)

        if (bankColor && !bankColorMap.has(bankName)) {
            bankColorMap.set(bankName, bankColor)
        }
    }

    const bankNames = Array.from(new Set(validInvestments.map(getBankName)))
    const bankSeries = bankNames.map((bankName, index) => ({
        bankName,
        key: getBankKey(bankName),
        color: bankColorMap.get(bankName) ?? CHART_COLORS[index % CHART_COLORS.length],
    }))

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
        const bankTotals = Object.fromEntries(bankSeries.map(({ key }) => [key, 0]))

        for (const investment of validInvestments) {
            const value = estimateLiquidValue(investment, currentDate)
            const bankKey = getBankKey(getBankName(investment))
            bankTotals[bankKey] += value
        }

        const liquidValue = Object.values(bankTotals).reduce((sum, value) => sum + value, 0)

        chartData.push({
            date: currentDate.toISOString(),
            liquidValue: Number(liquidValue.toFixed(2)),
            ...Object.fromEntries(
                Object.entries(bankTotals).map(([key, value]) => [key, Number(value.toFixed(2))])
            ),
        })
    }

    return { chartData, bankSeries }
}

function TimelineSkeleton() {
    return (
        <Card className="m-6 flex flex-col">
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
    const { resolvedTheme } = useTheme()
    const { chartData, bankSeries } = React.useMemo(() => {
        const result = buildTimelineData(investments)
        return Array.isArray(result) ? { chartData: result, bankSeries: [] } : result
    }, [investments])
    const todayIso = React.useMemo(() => startOfDay(new Date()).toISOString(), [])
    const [selectedBanks, setSelectedBanks] = React.useState([])

    React.useEffect(() => {
        setSelectedBanks(bankSeries.map((series) => series.key))
    }, [bankSeries])

    const visibleBankSeries = React.useMemo(() => {
        if (selectedBanks.length === 0) return bankSeries
        return bankSeries.filter((series) => selectedBanks.includes(series.key))
    }, [bankSeries, selectedBanks])
    const filteredChartData = React.useMemo(() => {
        if (visibleBankSeries.length === bankSeries.length) {
            return chartData
        }

        return chartData.map((point) => {
            const liquidValue = visibleBankSeries.reduce(
                (sum, series) => sum + Number(point[series.key] ?? 0),
                0
            )

            return {
                ...point,
                liquidValue: Number(liquidValue.toFixed(2)),
            }
        })
    }, [bankSeries.length, chartData, visibleBankSeries])
    const totalLineColor = resolvedTheme === "dark" ? "#FFFFFF" : "#6B7280"

    if (!investments) {
        return <TimelineSkeleton />
    }

    if (chartData.length === 0) {
        return (
            <Card className="m-6 flex flex-col">
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
        <Card className="m-6 flex flex-col">
            <CardHeader className="pb-2">
                <CardTitle>Evolução da carteira</CardTitle>
                <CardDescription>
                    <b>Valor líquido</b> de todos seus investimento <small>(incluindo itens arquivados)</small>
                    <br />
                    <small>As linhas de valores no gráfico mostram uma subida linear considerando o valor investido e o valor liquido no vencimento. Não considera os níveis das aliquotas de IR e IOF.</small>
                </CardDescription>
            </CardHeader>
            <CardContent>
                {bankSeries.length > 0 && (
                    <div className="mb-4 flex flex-wrap gap-2">
                        <Button
                            type="button"
                            size="sm"
                            variant={selectedBanks.length === bankSeries.length ? "default" : "outline"}
                            onClick={() => setSelectedBanks(bankSeries.map((series) => series.key))}
                        >
                            Todos
                        </Button>
                        {bankSeries.map((series) => {
                            const isSelected = selectedBanks.includes(series.key)

                            return (
                                <Button
                                    key={series.key}
                                    type="button"
                                    size="sm"
                                    variant={isSelected ? "default" : "outline"}
                                    onClick={() =>
                                        setSelectedBanks((current) =>
                                            current.includes(series.key)
                                                ? current.filter((key) => key !== series.key)
                                                : [...current, series.key]
                                        )
                                    }
                                    className="gap-2"
                                >
                                    <span
                                        className="h-2.5 w-2.5 rounded-full"
                                        style={{ backgroundColor: series.color }}
                                    />
                                    {series.bankName}
                                </Button>
                            )
                        })}
                    </div>
                )}
                <div className="h-[320px] w-full">
                    <ResponsiveContainer width="100%" height="100%">
                        <AreaChart data={filteredChartData} margin={{ top: 8, right: 8, left: 8, bottom: 8 }}>
                            <defs>
                                <linearGradient id="portfolio-total-fill" x1="0" y1="0" x2="0" y2="1">
                                    <stop offset="5%" stopColor={totalLineColor} stopOpacity={0.80} />
                                    <stop offset="95%" stopColor={totalLineColor} stopOpacity={0.03} />
                                </linearGradient>
                                {bankSeries.map((series) => (
                                    <linearGradient key={series.key} id={`fill-${series.key}`} x1="0" y1="0" x2="0" y2="1">
                                        <stop offset="5%" stopColor={series.color} stopOpacity={0.16} />
                                        <stop offset="95%" stopColor={series.color} stopOpacity={0.02} />
                                    </linearGradient>
                                ))}
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
                                formatter={(value, name) => [formatCurrency(Number(value)), name]}
                                labelFormatter={(value) =>
                                    new Date(value).toLocaleDateString("pt-BR", {
                                        day: "2-digit",
                                        month: "short",
                                        year: "numeric",
                                    })
                                }
                            />
                            <Legend />
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
                                stroke={totalLineColor}
                                strokeWidth={4}
                                fill="url(#portfolio-total-fill)"
                                fillOpacity={1}
                                dot={false}
                                activeDot={{ r: 5, stroke: totalLineColor, strokeWidth: 2 }}
                                name="Total"
                            />
                            {visibleBankSeries.map((series) => (
                                <Area
                                    key={series.key}
                                    type="monotone"
                                    dataKey={series.key}
                                    stroke={series.color}
                                    strokeWidth={2}
                                    fill={`url(#fill-${series.key})`}
                                    fillOpacity={1}
                                    dot={false}
                                    activeDot={{ r: 3 }}
                                    name={series.bankName}
                                />
                            ))}
                        </AreaChart>
                    </ResponsiveContainer>
                </div>
            </CardContent>
        </Card>
    )
}
