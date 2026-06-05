/* eslint-disable react/prop-types */
import * as React from "react"
import { Area, AreaChart, CartesianGrid, Legend, ReferenceLine, ResponsiveContainer, Tooltip, XAxis, YAxis } from "recharts"
import { useTheme } from "next-themes"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Button } from "@/components/ui/button"
import { Skeleton } from "@/components/ui/skeleton"
import { addDays, diffInDays, getInvestmentLiquidValueAtDate, startOfDay } from "@/utils/investment-timeline"

const CHART_COLORS = [
    "var(--chart-1)",
    "var(--chart-2)",
    "var(--chart-3)",
    "var(--chart-4)",
    "var(--chart-5)",
]

function formatCurrency(value) {
    return new Intl.NumberFormat("pt-BR", {
        style: "currency",
        currency: "BRL",
        minimumFractionDigits: 2,
    }).format(value)
}

function formatTooltipDate(value) {
    return new Date(value).toLocaleDateString("pt-BR", {
        day: "2-digit",
        month: "short",
        year: "numeric",
    })
}

function getBankLogoSrc(bankCode) {
    if (bankCode === null || bankCode === undefined || bankCode === "") return null
    return `/bank-logos/${String(bankCode).padStart(3, "0")}.svg`
}

function getBankName(investment) {
    return investment.bank?.name || "Banco Desconhecido"
}

function getBankCode(investment) {
    return investment.bank?.code || null
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

function buildTimelineData(investments) {
    const validInvestments = (investments ?? []).filter((investment) => investment?.date_buy)
    if (validInvestments.length === 0) return []
    const bankColorMap = new Map()
    const bankCodeMap = new Map()

    for (const investment of validInvestments) {
        const bankName = getBankName(investment)
        const bankColor = getBankColor(investment)
        const bankCode = getBankCode(investment)

        if (bankColor && !bankColorMap.has(bankName)) {
            bankColorMap.set(bankName, bankColor)
        }

        if (bankCode && !bankCodeMap.has(bankName)) {
            bankCodeMap.set(bankName, bankCode)
        }
    }

    const bankNames = Array.from(new Set(validInvestments.map(getBankName)))
    const bankSeries = bankNames.map((bankName, index) => ({
        bankName,
        bankCode: bankCodeMap.get(bankName) ?? null,
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
            const value = getInvestmentLiquidValueAtDate(investment, currentDate)
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

function CustomTooltip({ active, label, payload }) {
    if (!active || !payload?.length) return null

    return (
        <div className="rounded-md border bg-background/95 px-3 py-2 shadow-md">
            <p className="mb-2 text-sm font-medium text-foreground">{formatTooltipDate(label)}</p>
            <div className="space-y-1">
                {payload.map((entry) => (
                    <div key={entry.dataKey} className="flex items-center gap-2 text-sm" style={{ color: entry.color }}>
                        {"payload" in entry && entry.payload?.bankCode ? (
                            <img
                                src={getBankLogoSrc(entry.payload.bankCode)}
                                alt=""
                                aria-hidden="true"
                                className="h-4 w-4 shrink-0 rounded-sm object-contain"
                                onError={(event) => {
                                    event.currentTarget.style.display = "none"
                                }}
                            />
                        ) : null}
                        <span>
                            {entry.name}: {formatCurrency(Number(entry.value))}
                        </span>
                    </div>
                ))}
            </div>
        </div>
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
                    <small>As linhas de valores no gráfico consideram o valor inicial, o valor liquido atual e o valor final previsto no vencimento de cada investimento.</small>
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
                                    {series.bankCode ? (
                                        <img
                                            src={getBankLogoSrc(series.bankCode)}
                                            alt=""
                                            aria-hidden="true"
                                            className="h-4 w-4 shrink-0 rounded-sm object-contain"
                                            onError={(event) => {
                                                event.currentTarget.style.display = "none"
                                            }}
                                        />
                                    ) : null}
                                    {series.bankName}
                                </Button>
                            )
                        })}
                    </div>
                )}
                <div className="h-[320px] w-full">
                    <ResponsiveContainer width="100%" height="100%">
                        <AreaChart data={filteredChartData} margin={{ top: 28, right: 8, left: 8, bottom: 8 }}>
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
                                content={<CustomTooltip />}
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
                                type="linear"
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
                                    type="linear"
                                    dataKey={series.key}
                                    stroke={series.color}
                                    strokeWidth={2}
                                    fill={`url(#fill-${series.key})`}
                                    fillOpacity={1}
                                    dot={false}
                                    activeDot={{ r: 3 }}
                                    name={series.bankName}
                                    bankCode={series.bankCode}
                                />
                            ))}
                        </AreaChart>
                    </ResponsiveContainer>
                </div>
            </CardContent>
        </Card>
    )
}
