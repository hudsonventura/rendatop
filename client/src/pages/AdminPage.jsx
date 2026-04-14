import { useEffect, useMemo, useState } from "react"
import { useNavigate } from "react-router-dom"
import { Cell, Pie, PieChart, ResponsiveContainer, Tooltip } from "recharts"
import { BaseLayout } from "@/components/layouts/base-layout"
import Logged from "@/components/Logged"
import axiosInstance from "@/utils/axiosConfig"
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Skeleton } from "@/components/ui/skeleton"
import { getStoredUserType, isAdminUserType } from "@/utils/userSession"
import { BarChart3, CreditCard, ShieldCheck, Users } from "lucide-react"

const CHART_COLORS = [
    "var(--chart-1)",
    "var(--chart-2)",
    "var(--chart-3)",
    "var(--chart-4)",
    "var(--chart-5)",
]

function buildChartItems(items, keyField, valueField, labelField) {
    return items
        .filter((item) => Number(item?.[valueField] || 0) > 0)
        .map((item, index) => ({
            key: item[keyField],
            label: item[labelField],
            value: Number(item[valueField] || 0),
            fill: CHART_COLORS[index % CHART_COLORS.length],
        }))
}

function buildAuthItems(authProviderCounts) {
    const items = [
        { key: "without_sso", label: "Sem SSO", value: Number(authProviderCounts.without_sso || 0) },
        { key: "google", label: "SSO Google", value: Number(authProviderCounts.google || 0) },
        { key: "microsoft", label: "SSO Microsoft", value: Number(authProviderCounts.microsoft || 0) },
    ]

    return items
        .filter((item) => item.value > 0)
        .map((item, index) => ({
            ...item,
            fill: CHART_COLORS[index % CHART_COLORS.length],
        }))
}

function renderPieLabel(total) {
    return ({ cx, cy, midAngle, outerRadius, percent, value }) => {
        if (!cx || !cy || !outerRadius || !value || !percent) return null

        const radius = outerRadius + 26
        const x = cx + radius * Math.cos((-midAngle * Math.PI) / 180)
        const y = cy + radius * Math.sin((-midAngle * Math.PI) / 180)
        const percentage = (percent * 100).toFixed(1)

        return (
            <text
                x={x}
                y={y}
                fill="currentColor"
                textAnchor={x > cx ? "start" : "end"}
                dominantBaseline="central"
                className="text-xs"
            >
                <tspan x={x} dy="-0.2em" fontSize="12" fontWeight="600">
                    {value}
                </tspan>
                <tspan x={x} dy="1.2em" fontSize="11" fill="currentColor" opacity="0.7">
                    {percentage}%
                </tspan>
            </text>
        )
    }
}

function PieMetricCard({ title, description, icon: Icon, items, emptyMessage }) {
    const total = useMemo(
        () => items.reduce((sum, item) => sum + Number(item.value || 0), 0),
        [items]
    )

    return (
        <Card className="h-full">
            <CardHeader>
                <div className="flex items-center gap-2">
                    <Icon className="h-5 w-5" />
                    <CardTitle>{title}</CardTitle>
                </div>
                <CardDescription>{description}</CardDescription>
            </CardHeader>
            <CardContent>
                {items.length > 0 ? (
                    <div className="space-y-4">
                        <div className="h-[360px]">
                            <ResponsiveContainer width="100%" height="100%">
                                <PieChart>
                                    <Tooltip
                                        formatter={(value, _name, payload) => {
                                            const current = Number(value || 0)
                                            const percentage = total > 0 ? ((current / total) * 100).toFixed(1) : "0.0"
                                            return [`${current} (${percentage}%)`, payload?.payload?.label]
                                        }}
                                    />
                                    <Pie
                                        data={items}
                                        dataKey="value"
                                        nameKey="label"
                                        cx="50%"
                                        cy="50%"
                                        innerRadius={65}
                                        outerRadius={110}
                                        paddingAngle={2}
                                        stroke="none"
                                        labelLine={false}
                                        label={renderPieLabel(total)}
                                    >
                                        {items.map((item) => (
                                            <Cell key={item.key} fill={item.fill} />
                                        ))}
                                    </Pie>
                                </PieChart>
                            </ResponsiveContainer>
                        </div>

                        <div className="grid gap-2">
                            {items.map((item) => {
                                const percentage = total > 0 ? ((item.value / total) * 100).toFixed(1) : "0.0"

                                return (
                                    <div key={item.key} className="flex items-center justify-between rounded-lg border p-3">
                                        <div className="flex items-center gap-3">
                                            <span
                                                className="h-3 w-3 rounded-full"
                                                style={{ backgroundColor: item.fill }}
                                            />
                                            <span className="text-sm font-medium">{item.label}</span>
                                        </div>
                                        <div className="text-right">
                                            <div className="font-semibold">{item.value}</div>
                                            <div className="text-muted-foreground text-xs">{percentage}%</div>
                                        </div>
                                    </div>
                                )
                            })}
                        </div>
                    </div>
                ) : (
                    <p className="text-sm text-muted-foreground">{emptyMessage}</p>
                )}
            </CardContent>
        </Card>
    )
}

export default function AdminPage() {
    const navigate = useNavigate()
    const [loading, setLoading] = useState(true)
    const [error, setError] = useState("")
    const [stats, setStats] = useState(null)

    useEffect(() => {
        const storedUserType = getStoredUserType()
        if (storedUserType && !isAdminUserType(storedUserType)) {
            navigate("/home", { replace: true })
            return
        }

        let cancelled = false

        axiosInstance
            .get("/admin/stats")
            .then((response) => {
                if (cancelled) return
                setStats(response?.data || null)
            })
            .catch((err) => {
                if (cancelled) return

                if (err?.response?.status === 403) {
                    navigate("/home", { replace: true })
                    return
                }

                setError("Não foi possível carregar os indicadores administrativos.")
            })
            .finally(() => {
                if (cancelled) return
                setLoading(false)
            })

        return () => {
            cancelled = true
        }
    }, [navigate])

    const planChartItems = useMemo(
        () => buildChartItems(stats?.users_by_plan || [], "plan_id", "users_count", "plan_name"),
        [stats?.users_by_plan]
    )
    const authChartItems = useMemo(
        () => buildAuthItems(stats?.auth_provider_counts || {}),
        [stats?.auth_provider_counts]
    )
    const visitChartItems = useMemo(
        () => buildChartItems(stats?.visits_by_origin || [], "visit", "visits_count", "visit"),
        [stats?.visits_by_origin]
    )

    return (
        <>
            <Logged />
            <BaseLayout title="Admin" description="Acompanhe métricas de usuários, planos, autenticação e visitas">
                <div className="space-y-6 px-4 lg:px-6">
                    {loading ? (
                        <div className="grid gap-4 xl:grid-cols-3">
                            <Skeleton className="h-[32rem] rounded-xl" />
                            <Skeleton className="h-[32rem] rounded-xl" />
                            <Skeleton className="h-[32rem] rounded-xl" />
                        </div>
                    ) : error ? (
                        <Alert variant="destructive">
                            <AlertTitle>Falha ao carregar</AlertTitle>
                            <AlertDescription>{error}</AlertDescription>
                        </Alert>
                    ) : (
                        <>
                            <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
                                <Card>
                                    <CardHeader className="pb-3">
                                        <div className="flex items-center justify-between gap-3">
                                            <div>
                                                <CardDescription>Usuários totais</CardDescription>
                                                <CardTitle className="text-3xl">{stats?.total_users ?? 0}</CardTitle>
                                            </div>
                                            <div className="rounded-lg border bg-muted/30 p-2">
                                                <Users className="h-4 w-4" />
                                            </div>
                                        </div>
                                    </CardHeader>
                                    <CardContent>
                                        <p className="text-sm text-muted-foreground">Quantidade total de contas cadastradas.</p>
                                    </CardContent>
                                </Card>
                            </div>

                            <div className="grid gap-4 xl:grid-cols-3">
                                <PieMetricCard
                                    title="Planos"
                                    description="Distribuição dos usuários por plano efetivo atual."
                                    icon={CreditCard}
                                    items={planChartItems}
                                    emptyMessage="Nenhum dado de plano disponível."
                                />
                                <PieMetricCard
                                    title="Autenticação"
                                    description="Distribuição por método principal de autenticação."
                                    icon={ShieldCheck}
                                    items={authChartItems}
                                    emptyMessage="Nenhum dado de autenticação disponível."
                                />
                                <PieMetricCard
                                    title="Visitas por origem"
                                    description="Contagem agrupada pelo campo `visit` da tabela `landing_visits`."
                                    icon={BarChart3}
                                    items={visitChartItems}
                                    emptyMessage="Nenhuma visita registrada até o momento."
                                />
                            </div>
                        </>
                    )}
                </div>
            </BaseLayout>
        </>
    )
}
