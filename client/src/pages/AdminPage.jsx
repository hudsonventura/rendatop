import { useCallback, useEffect, useMemo, useState } from "react"
import { useNavigate } from "react-router-dom"
import { Cell, Pie, PieChart, ResponsiveContainer, Tooltip } from "recharts"
import { BaseLayout } from "@/components/layouts/base-layout"
import Logged from "@/components/Logged"
import axiosInstance from "@/utils/axiosConfig"
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Skeleton } from "@/components/ui/skeleton"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Badge } from "@/components/ui/badge"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import { getStoredUserType, isAdminUserType } from "@/utils/userSession"
import { BarChart3, ChevronLeft, ChevronRight, CreditCard, Gift, Loader2, Search, ShieldCheck, Users } from "lucide-react"

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

function formatDate(value) {
    if (!value) return "Sem assinatura"

    const date = new Date(value)
    return Number.isNaN(date.getTime())
        ? "Sem assinatura"
        : date.toLocaleDateString("pt-BR")
}

function getAuthProviderLabel(value) {
    if (value === "Google" || value === 2) return "Google"
    if (value === "Microsoft" || value === 3) return "Microsoft"
    return "Senha"
}

function getUserTypeLabel(value) {
    if (value === "Admin" || value === 2) return "Admin"
    return "Comum"
}

export default function AdminPage() {
    const navigate = useNavigate()
    const [loading, setLoading] = useState(true)
    const [error, setError] = useState("")
    const [stats, setStats] = useState(null)
    const [usersLoading, setUsersLoading] = useState(true)
    const [usersError, setUsersError] = useState("")
    const [usersData, setUsersData] = useState({ items: [], page: 1, page_size: 10, total: 0 })
    const [usersPage, setUsersPage] = useState(1)
    const [usersSearchInput, setUsersSearchInput] = useState("")
    const [usersSearch, setUsersSearch] = useState("")
    const [grantingTrial, setGrantingTrial] = useState("")

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

    useEffect(() => {
        const timer = window.setTimeout(() => {
            setUsersPage(1)
            setUsersSearch(usersSearchInput.trim())
        }, 350)

        return () => window.clearTimeout(timer)
    }, [usersSearchInput])

    const loadUsers = useCallback(() => {
        let cancelled = false
        setUsersLoading(true)
        setUsersError("")

        axiosInstance
            .get("/admin/users", {
                params: {
                    page: usersPage,
                    page_size: 10,
                    search: usersSearch || undefined,
                },
            })
            .then((response) => {
                if (cancelled) return
                setUsersData(response?.data || { items: [], page: usersPage, page_size: 10, total: 0 })
            })
            .catch((err) => {
                if (cancelled) return

                if (err?.response?.status === 403) {
                    navigate("/home", { replace: true })
                    return
                }

                setUsersError("Não foi possível carregar os usuários.")
            })
            .finally(() => {
                if (cancelled) return
                setUsersLoading(false)
            })

        return () => {
            cancelled = true
        }
    }, [navigate, usersPage, usersSearch])

    useEffect(() => loadUsers(), [loadUsers])

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
    const totalUserPages = Math.max(1, Math.ceil((usersData.total || 0) / (usersData.page_size || 10)))

    const handleGrantTrial = (user, planId) => {
        const actionKey = `${user.id}:${planId}`
        setGrantingTrial(actionKey)
        setUsersError("")

        axiosInstance
            .post(`/admin/users/${user.id}/trial`, { plan_id: planId })
            .then(() => {
                loadUsers()
            })
            .catch((err) => {
                setUsersError(err?.response?.data?.message || err?.response?.data || "Não foi possível liberar a degustação.")
            })
            .finally(() => setGrantingTrial(""))
    }

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

                            <Card>
                                <CardHeader>
                                    <div className="flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between">
                                        <div>
                                            <CardTitle>Usuários</CardTitle>
                                            <CardDescription>Busque usuários e libere degustações de 30 dias em planos pagos.</CardDescription>
                                        </div>
                                        <div className="relative w-full lg:w-80">
                                            <Search className="text-muted-foreground absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2" />
                                            <Input
                                                value={usersSearchInput}
                                                onChange={(event) => setUsersSearchInput(event.target.value)}
                                                placeholder="Buscar por nome ou email"
                                                className="pl-9"
                                            />
                                        </div>
                                    </div>
                                </CardHeader>
                                <CardContent className="space-y-4">
                                    {usersError && (
                                        <Alert variant="destructive">
                                            <AlertTitle>Falha na operação</AlertTitle>
                                            <AlertDescription>{usersError}</AlertDescription>
                                        </Alert>
                                    )}

                                    <div className="overflow-hidden rounded-lg border">
                                        <Table>
                                            <TableHeader>
                                                <TableRow>
                                                    <TableHead>Usuário</TableHead>
                                                    <TableHead>Tipo</TableHead>
                                                    <TableHead>Autenticação</TableHead>
                                                    <TableHead>Plano atual</TableHead>
                                                    <TableHead>Vigência</TableHead>
                                                    <TableHead className="text-right">Degustação</TableHead>
                                                </TableRow>
                                            </TableHeader>
                                            <TableBody>
                                                {usersLoading ? (
                                                    Array.from({ length: 5 }).map((_, index) => (
                                                        <TableRow key={index}>
                                                            <TableCell colSpan={6}>
                                                                <Skeleton className="h-8 w-full" />
                                                            </TableCell>
                                                        </TableRow>
                                                    ))
                                                ) : usersData.items.length === 0 ? (
                                                    <TableRow>
                                                        <TableCell colSpan={6} className="h-24 text-center text-muted-foreground">
                                                            Nenhum usuário encontrado.
                                                        </TableCell>
                                                    </TableRow>
                                                ) : (
                                                    usersData.items.map((user) => (
                                                        <TableRow key={user.id}>
                                                            <TableCell>
                                                                <div className="space-y-1">
                                                                    <div className="font-medium">{user.name || "Sem nome"}</div>
                                                                    <div className="text-xs text-muted-foreground">{user.email}</div>
                                                                </div>
                                                            </TableCell>
                                                            <TableCell>
                                                                <Badge variant={getUserTypeLabel(user.user_type) === "Admin" ? "default" : "secondary"}>
                                                                    {getUserTypeLabel(user.user_type)}
                                                                </Badge>
                                                            </TableCell>
                                                            <TableCell>{getAuthProviderLabel(user.auth_provider)}</TableCell>
                                                            <TableCell>
                                                                <div className="flex items-center gap-2">
                                                                    <CreditCard className="h-4 w-4 text-muted-foreground" />
                                                                    <span>{user.active_plan_name || "Free"}</span>
                                                                    {user.active_payment_method === "trial" && (
                                                                        <Badge variant="outline">Degustação</Badge>
                                                                    )}
                                                                </div>
                                                            </TableCell>
                                                            <TableCell>{formatDate(user.active_plan_period_end)}</TableCell>
                                                            <TableCell>
                                                                <div className="flex justify-end gap-2">
                                                                    {["plus", "pro"].map((planId) => {
                                                                        const actionKey = `${user.id}:${planId}`
                                                                        const busy = grantingTrial === actionKey

                                                                        return (
                                                                            <Button
                                                                                key={planId}
                                                                                type="button"
                                                                                variant="outline"
                                                                                size="sm"
                                                                                onClick={() => handleGrantTrial(user, planId)}
                                                                                disabled={Boolean(grantingTrial)}
                                                                            >
                                                                                {busy ? (
                                                                                    <Loader2 className="mr-2 h-3.5 w-3.5 animate-spin" />
                                                                                ) : (
                                                                                    <Gift className="mr-2 h-3.5 w-3.5" />
                                                                                )}
                                                                                {planId === "plus" ? "Plus" : "Pro"}
                                                                            </Button>
                                                                        )
                                                                    })}
                                                                </div>
                                                            </TableCell>
                                                        </TableRow>
                                                    ))
                                                )}
                                            </TableBody>
                                        </Table>
                                    </div>

                                    <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
                                        <p className="text-sm text-muted-foreground">
                                            Página {usersData.page || usersPage} de {totalUserPages} · {usersData.total || 0} usuário(s)
                                        </p>
                                        <div className="flex items-center gap-2">
                                            <Button
                                                type="button"
                                                variant="outline"
                                                size="sm"
                                                onClick={() => setUsersPage((current) => Math.max(1, current - 1))}
                                                disabled={usersLoading || usersPage <= 1}
                                            >
                                                <ChevronLeft className="mr-2 h-4 w-4" />
                                                Anterior
                                            </Button>
                                            <Button
                                                type="button"
                                                variant="outline"
                                                size="sm"
                                                onClick={() => setUsersPage((current) => Math.min(totalUserPages, current + 1))}
                                                disabled={usersLoading || usersPage >= totalUserPages}
                                            >
                                                Próxima
                                                <ChevronRight className="ml-2 h-4 w-4" />
                                            </Button>
                                        </div>
                                    </div>
                                </CardContent>
                            </Card>
                        </>
                    )}
                </div>
            </BaseLayout>
        </>
    )
}
