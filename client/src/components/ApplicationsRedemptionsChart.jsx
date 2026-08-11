import * as React from "react"
import {
    Bar,
    BarChart,
    CartesianGrid,
    Legend,
    ResponsiveContainer,
    Tooltip,
    XAxis,
    YAxis,
} from "recharts"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Skeleton } from "@/components/ui/skeleton"

const APPLICATIONS_COLOR = "var(--chart-2)"
const REDEMPTIONS_COLOR = "var(--chart-1)"
const MONTH_COUNT = 12

function getMonthKey(value) {
    const match = String(value ?? "").match(/^(\d{4})-(\d{2})/)
    return match ? `${match[1]}-${match[2]}` : null
}

function getLastMonths() {
    const now = new Date()
    const currentMonth = new Date(now.getFullYear(), now.getMonth(), 1)

    return Array.from({ length: MONTH_COUNT }, (_, index) => {
        const date = new Date(currentMonth.getFullYear(), currentMonth.getMonth() - (MONTH_COUNT - 1 - index), 1)
        return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, "0")}`
    })
}

function buildChartData(investments) {
    const months = getLastMonths()
    const totals = new Map(months.map((month) => [month, { applications: 0, redemptions: 0 }]))

    for (const investment of investments ?? []) {
        const applicationMonth = getMonthKey(investment.date_buy)
        const applicationValue = Number(investment.value ?? 0)

        if (applicationMonth && totals.has(applicationMonth) && applicationValue > 0) {
            totals.get(applicationMonth).applications += applicationValue
        }

        for (const redemption of investment.redemptions ?? []) {
            const redemptionMonth = getMonthKey(redemption.date)
            const redemptionValue = Number(redemption.value ?? 0)

            if (redemptionMonth && totals.has(redemptionMonth) && redemptionValue > 0) {
                totals.get(redemptionMonth).redemptions += redemptionValue
            }
        }
    }

    return months.map((month) => ({
        month,
        applications: Number(totals.get(month).applications.toFixed(2)),
        redemptions: Number(totals.get(month).redemptions.toFixed(2)),
    }))
}

function formatCurrency(value) {
    return new Intl.NumberFormat("pt-BR", {
        style: "currency",
        currency: "BRL",
        minimumFractionDigits: 2,
    }).format(Number(value ?? 0))
}

function formatCompactCurrency(value) {
    return new Intl.NumberFormat("pt-BR", {
        style: "currency",
        currency: "BRL",
        notation: "compact",
        maximumFractionDigits: 1,
    }).format(Number(value ?? 0))
}

function formatMonth(month) {
    const [year, monthNumber] = String(month).split("-").map(Number)
    return new Date(year, monthNumber - 1, 1)
        .toLocaleDateString("pt-BR", { month: "short", year: "2-digit" })
        .replace(" de ", "/")
}

function FlowTooltip({ active, payload }) {
    const point = payload?.[0]?.payload
    if (!active || !point) return null

    return (
        <div className="min-w-48 rounded-md border bg-background/95 px-3 py-2 text-sm shadow-md">
            <p className="mb-2 font-medium capitalize">{formatMonth(point.month)}</p>
            <div className="space-y-1">
                <div className="flex justify-between gap-5" style={{ color: APPLICATIONS_COLOR }}>
                    <span>Aplicações</span>
                    <strong>{formatCurrency(point.applications)}</strong>
                </div>
                <div className="flex justify-between gap-5" style={{ color: REDEMPTIONS_COLOR }}>
                    <span>Resgates</span>
                    <strong>{formatCurrency(point.redemptions)}</strong>
                </div>
            </div>
        </div>
    )
}

function ChartSkeleton() {
    return (
        <Card className="flex h-full flex-col">
            <CardHeader className="pb-2">
                <CardTitle>Aplicações e resgates</CardTitle>
                <CardDescription>Movimentações dos últimos 12 meses</CardDescription>
            </CardHeader>
            <CardContent>
                <Skeleton className="h-[260px] w-full rounded-xl" />
            </CardContent>
        </Card>
    )
}

export default function ApplicationsRedemptionsChart({ investments }) {
    const data = React.useMemo(() => buildChartData(investments), [investments])

    if (!investments) return <ChartSkeleton />

    return (
        <Card className="flex h-full flex-col">
            <CardHeader className="pb-2">
                <CardTitle>Aplicações e resgates</CardTitle>
                <CardDescription>Valores movimentados nos últimos 12 meses</CardDescription>
            </CardHeader>
            <CardContent className="flex-1">
                <div className="h-[260px] w-full">
                    <ResponsiveContainer width="100%" height="100%">
                        <BarChart data={data} margin={{ top: 18, right: 8, left: 0, bottom: 4 }}>
                            <CartesianGrid vertical={false} strokeDasharray="3 3" />
                            <XAxis
                                dataKey="month"
                                tickFormatter={formatMonth}
                                tickLine={false}
                                axisLine={false}
                                minTickGap={12}
                            />
                            <YAxis
                                tickFormatter={formatCompactCurrency}
                                tickLine={false}
                                axisLine={false}
                                width={72}
                            />
                            <Tooltip content={<FlowTooltip />} cursor={{ fill: "var(--muted)", opacity: 0.35 }} />
                            <Legend verticalAlign="top" height={30} />
                            <Bar dataKey="applications" name="Aplicações" fill={APPLICATIONS_COLOR} maxBarSize={28} radius={[4, 4, 0, 0]} />
                            <Bar dataKey="redemptions" name="Resgates" fill={REDEMPTIONS_COLOR} maxBarSize={28} radius={[4, 4, 0, 0]} />
                        </BarChart>
                    </ResponsiveContainer>
                </div>
            </CardContent>
        </Card>
    )
}
