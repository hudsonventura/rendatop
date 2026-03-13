import * as React from "react"
import { Label, Pie, PieChart, Sector } from "recharts"
import {
    Card,
    CardContent,
    CardDescription,
    CardHeader,
    CardTitle,
} from "@/components/ui/card"
import { Skeleton } from "@/components/ui/skeleton"

// ── Chart colours ─────────────────────────────────────────────────────────────

const CHART_COLORS = [
    "var(--chart-1)",
    "var(--chart-2)",
    "var(--chart-3)",
    "var(--chart-4)",
    "var(--chart-5)",
]

// ── Helpers ───────────────────────────────────────────────────────────────────

function formatCurrency(value) {
    return new Intl.NumberFormat("pt-BR", {
        style: "currency",
        currency: "BRL",
        minimumFractionDigits: 2,
    }).format(value)
}

function buildChartData(investments) {
    const map = new Map()
    const colorMap = new Map()

    for (const inv of investments) {
        if (inv.archived) continue
        const firstCalc = inv.calculated?.[0]
        const liquidValue = firstCalc?.value_liq ?? inv.value
        const bankName = inv.bank?.name || "Banco Desconhecido"
        const bankColor = inv.bank?.color
        map.set(bankName, (map.get(bankName) ?? 0) + liquidValue)
        if (bankColor) colorMap.set(bankName, bankColor)
    }

    return Array.from(map.entries()).map(([bank, value], index) => ({
        bank,
        value,
        fill: colorMap.get(bank) ?? CHART_COLORS[index % CHART_COLORS.length],
    }))
}

// ── Custom active shape ───────────────────────────────────────────────────────

function ActiveShape({ outerRadius = 0, ...props }) {
    return (
        <g>
            <Sector {...props} outerRadius={outerRadius + 10} />
            <Sector
                {...props}
                outerRadius={outerRadius + 25}
                innerRadius={outerRadius + 12}
            />
        </g>
    )
}

// ── Component ─────────────────────────────────────────────────────────────────

export default function BanksPieChart({ investments }) {
    const chartData = React.useMemo(
        () => (investments && investments.length > 0 ? buildChartData(investments) : []),
        [investments]
    )

    const [activeIndex, setActiveIndex] = React.useState(0)

    React.useEffect(() => {
        setActiveIndex(0)
    }, [chartData.length])

    const totalValue = React.useMemo(
        () => chartData.reduce((sum, d) => sum + d.value, 0),
        [chartData]
    )

    // Loading state
    if (!investments) {
        return (
            <Card className="flex flex-col">
                <CardHeader className="pb-2">
                    <CardTitle>Distribuição por Banco Hoje</CardTitle>
                    <CardDescription>Resumo dos seus investimentos por instituição</CardDescription>
                </CardHeader>
                <CardContent className="flex flex-1 justify-center">
                    <div className="grid grid-cols-1 lg:grid-cols-2 gap-6 w-full">
                        <div className="flex justify-center items-center">
                            <div className="relative h-[220px] w-[220px]">
                                <Skeleton className="h-full w-full rounded-full" />
                                <div className="absolute inset-[52px] rounded-full border bg-background" />
                            </div>
                        </div>
                        <div className="flex flex-col justify-center space-y-3">
                            {Array.from({ length: 4 }).map((_, index) => (
                                <div key={index} className="flex items-center justify-between rounded-lg border p-3">
                                    <div className="flex items-center gap-3">
                                        <Skeleton className="h-3 w-3 rounded-full" />
                                        <Skeleton className="h-4 w-28" />
                                    </div>
                                    <div className="space-y-1 text-right">
                                        <Skeleton className="h-4 w-20 ml-auto" />
                                        <Skeleton className="h-3 w-10 ml-auto" />
                                    </div>
                                </div>
                            ))}
                        </div>
                    </div>
                </CardContent>
            </Card>
        )
    }

    // Empty state
    if (chartData.length === 0) {
        return (
            <Card>
                <CardHeader>
                    <CardTitle>Distribuição por Banco Hoje</CardTitle>
                    <CardDescription>Resumo dos seus investimentos por instituição</CardDescription>
                </CardHeader>
                <CardContent>
                    <p className="text-sm text-muted-foreground">
                        Não há invesimentos. Crie o seu primeiro investimento usando o botão 'Adicionar investimento'
                    </p>
                </CardContent>
            </Card>
        )
    }

    const activeSlice = chartData[activeIndex]

    return (
        <Card className="flex flex-col">
            <CardHeader className="pb-2">
                <CardTitle>Distribuição por Banco Hoje</CardTitle>
                <CardDescription>Resumo dos seus investimentos por instituição</CardDescription>
            </CardHeader>

            <CardContent className="flex flex-1 justify-center">
                <div className="grid grid-cols-1 lg:grid-cols-2 gap-6 w-full">

                    {/* ── Donut chart ── */}
                    <div className="flex justify-center">
                        <PieChart width={300} height={300}>
                            <Pie
                                data={chartData}
                                dataKey="value"
                                nameKey="bank"
                                cx="50%"
                                cy="50%"
                                innerRadius={75}
                                outerRadius={110}
                                stroke="none"
                                strokeWidth={0}
                                activeShape={ActiveShape}
                                activeIndex={[activeIndex]}
                                onMouseEnter={(_, index) => setActiveIndex(index)}
                                onClick={(_, index) => setActiveIndex(index)}
                                className="cursor-pointer"
                            >
                                <Label
                                    content={({ viewBox }) => {
                                        if (viewBox && "cx" in viewBox && "cy" in viewBox) {
                                            return (
                                                <text
                                                    x={viewBox.cx}
                                                    y={viewBox.cy}
                                                    textAnchor="middle"
                                                    dominantBaseline="middle"
                                                >
                                                    <tspan
                                                        x={viewBox.cx}
                                                        y={(viewBox.cy ?? 0) - 12}
                                                        fontSize="14"
                                                        fontWeight="bold"
                                                        fill="currentColor"
                                                    >
                                                        {formatCurrency(activeSlice?.value ?? totalValue)}
                                                    </tspan>
                                                    <tspan
                                                        x={viewBox.cx}
                                                        y={(viewBox.cy ?? 0) + 10}
                                                        fontSize="11"
                                                        fill="#888"
                                                    >
                                                        {activeSlice?.bank ?? "Total"}
                                                    </tspan>
                                                </text>
                                            )
                                        }
                                    }}
                                />
                            </Pie>
                        </PieChart>
                    </div>

                    {/* ── Legend list ── */}
                    <div className="flex flex-col justify-center space-y-2">
                        {chartData.map((item, index) => {
                            const isActive = index === activeIndex
                            const pct = totalValue > 0 ? ((item.value / totalValue) * 100).toFixed(1) : "0.0"

                            return (
                                <div
                                    key={item.bank}
                                    className={`flex items-center justify-between p-3 rounded-lg transition-colors cursor-pointer ${isActive ? "bg-muted" : "hover:bg-muted/50"}`}
                                    onClick={() => setActiveIndex(index)}
                                >
                                    <div className="flex items-center gap-3">
                                        <span
                                            className="flex h-3 w-3 shrink-0 rounded-full"
                                            style={{ backgroundColor: item.fill }}
                                        />
                                        <span className="font-medium">{item.bank}</span>
                                    </div>
                                    <div className="text-right">
                                        <div className="font-bold">{formatCurrency(item.value)}</div>
                                        <div className="text-sm text-muted-foreground">{pct}%</div>
                                    </div>
                                </div>
                            )
                        })}

                        {/* Total row */}
                        <div className="flex items-center justify-between p-3 rounded-lg border-t mt-2 pt-4">
                            <span className="font-semibold text-sm text-muted-foreground">Total</span>
                            <span className="font-bold">{formatCurrency(totalValue)}</span>
                        </div>
                    </div>

                </div>
            </CardContent>
        </Card>
    )
}
