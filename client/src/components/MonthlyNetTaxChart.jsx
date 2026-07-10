/* eslint-disable react/prop-types */
import * as React from "react"
import {
    Bar,
    BarChart,
    CartesianGrid,
    Legend,
    ReferenceLine,
    ResponsiveContainer,
    Tooltip,
    XAxis,
    YAxis,
} from "recharts"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Skeleton } from "@/components/ui/skeleton"

const LIQUID_COLOR = "var(--chart-2)"
const TAX_COLOR = "var(--chart-1)"

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

function formatMonth(month, includeYear = false) {
    const [year, monthNumber] = String(month).split("-").map(Number)
    if (!year || !monthNumber) return month

    return new Date(year, monthNumber - 1, 1).toLocaleDateString("pt-BR", {
        month: "short",
        ...(includeYear ? { year: "numeric" } : {}),
    }).replace(" de ", "/")
}

function ProjectionTooltip({ active, payload }) {
    const point = payload?.[0]?.payload
    if (!active || !point) return null

    return (
        <div className="min-w-56 rounded-md border bg-background/95 px-3 py-2 text-sm shadow-md">
            <div className="mb-2 flex items-center justify-between gap-4">
                <span className="font-medium capitalize">{formatMonth(point.month, true)}</span>
                {point.estimated ? (
                    <span className="rounded-full bg-muted px-2 py-0.5 text-xs text-muted-foreground">
                        Estimativa
                    </span>
                ) : null}
            </div>
            <div className="space-y-1">
                <div className="flex justify-between gap-5" style={{ color: LIQUID_COLOR }}>
                    <span>Rendimento líquido</span>
                    <strong>{formatCurrency(point.liquid_value)}</strong>
                </div>
                <div className="flex justify-between gap-5" style={{ color: TAX_COLOR }}>
                    <span>Impostos</span>
                    <strong>{formatCurrency(point.taxes_value)}</strong>
                </div>
                <div className="flex justify-between gap-5 text-muted-foreground">
                    <span>IR</span>
                    <span>{formatCurrency(point.ir_value)}</span>
                </div>
                <div className="flex justify-between gap-5 text-muted-foreground">
                    <span>IOF</span>
                    <span>{formatCurrency(point.iof_value)}</span>
                </div>
            </div>
        </div>
    )
}

function ChartSkeleton() {
    return (
        <Card className="m-6 flex flex-col">
            <CardHeader className="pb-2">
                <CardTitle>Rendimentos líquidos e impostos</CardTitle>
                <CardDescription>Quanto a carteira rendeu em cada mês</CardDescription>
            </CardHeader>
            <CardContent>
                <Skeleton className="h-[360px] w-full rounded-xl" />
            </CardContent>
        </Card>
    )
}

export default function MonthlyNetTaxChart({ data }) {
    const currentMonth = React.useMemo(
        () => data?.findLast?.((point) => !point.estimated)?.month
            ?? [...(data ?? [])].reverse().find((point) => !point.estimated)?.month,
        [data]
    )

    if (!data) return <ChartSkeleton />

    return (
        <Card className="m-6 flex flex-col">
            <CardHeader className="pb-2">
                <CardTitle>Rendimentos líquidos e impostos</CardTitle>
                <CardDescription>
                    Rendimentos de cada um dos últimos 12 meses e estimativa para os próximos 12 meses.
                    <br />
                    <small>
                        IR regressivo de 22,5%, 20%, 17,5% e 15%, conforme o tempo de cada aplicação;
                        IOF regressivo nos primeiros 30 dias. Projeções usam os índices disponíveis hoje.
                    </small>
                </CardDescription>
            </CardHeader>
            <CardContent>
                {data.length === 0 ? (
                    <p className="text-sm text-muted-foreground">
                        Não há investimentos suficientes para montar esta projeção.
                    </p>
                ) : (
                    <div className="overflow-x-auto pb-2">
                        <div className="h-[360px] min-w-[980px]">
                            <ResponsiveContainer width="100%" height="100%">
                                <BarChart data={data} margin={{ top: 24, right: 12, left: 4, bottom: 8 }}>
                                    <CartesianGrid vertical={false} strokeDasharray="3 3" />
                                    <XAxis
                                        dataKey="month"
                                        tickFormatter={(value) => formatMonth(value)}
                                        tickLine={false}
                                        axisLine={false}
                                        minTickGap={4}
                                    />
                                    <YAxis
                                        tickFormatter={formatCompactCurrency}
                                        tickLine={false}
                                        axisLine={false}
                                        width={74}
                                    />
                                    <Tooltip content={<ProjectionTooltip />} cursor={{ fill: "var(--muted)", opacity: 0.35 }} />
                                    <Legend verticalAlign="top" height={32} />
                                    {currentMonth ? (
                                        <ReferenceLine
                                            x={currentMonth}
                                            stroke="var(--muted-foreground)"
                                            strokeDasharray="4 4"
                                            label={{ value: "Hoje", position: "top", fill: "var(--muted-foreground)", fontSize: 12 }}
                                        />
                                    ) : null}
                                    <Bar
                                        dataKey="liquid_value"
                                        name="Rendimento líquido"
                                        stackId="portfolio"
                                        fill={LIQUID_COLOR}
                                        maxBarSize={34}
                                    />
                                    <Bar
                                        dataKey="taxes_value"
                                        name="Impostos (IR + IOF)"
                                        stackId="portfolio"
                                        fill={TAX_COLOR}
                                        maxBarSize={34}
                                        radius={[4, 4, 0, 0]}
                                    />
                                </BarChart>
                            </ResponsiveContainer>
                        </div>
                    </div>
                )}
            </CardContent>
        </Card>
    )
}
