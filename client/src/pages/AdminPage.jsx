import { useEffect, useState } from "react"
import { useNavigate } from "react-router-dom"
import { BaseLayout } from "@/components/layouts/base-layout"
import Logged from "@/components/Logged"
import axiosInstance from "@/utils/axiosConfig"
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert"
import { Badge } from "@/components/ui/badge"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Skeleton } from "@/components/ui/skeleton"
import { getStoredUserType, isAdminUserType } from "@/utils/userSession"
import { BarChart3, ShieldCheck, Users } from "lucide-react"

function MetricCard({ title, value, description }) {
    return (
        <Card>
            <CardHeader className="pb-3">
                <CardDescription>{title}</CardDescription>
                <CardTitle className="text-3xl">{value}</CardTitle>
            </CardHeader>
            <CardContent>
                <p className="text-sm text-muted-foreground">{description}</p>
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

    const planCounts = stats?.users_by_plan || []
    const visitOrigins = stats?.visits_by_origin || []
    const authProviderCounts = stats?.auth_provider_counts || {}

    return (
        <>
            <Logged />
            <BaseLayout title="Admin" description="Acompanhe métricas de usuários, planos, autenticação e visitas">
                <div className="px-4 lg:px-6 space-y-6">
                    {loading ? (
                        <div className="grid gap-4 md:grid-cols-3">
                            <Skeleton className="h-36 rounded-xl" />
                            <Skeleton className="h-36 rounded-xl" />
                            <Skeleton className="h-36 rounded-xl" />
                        </div>
                    ) : error ? (
                        <Alert variant="destructive">
                            <AlertTitle>Falha ao carregar</AlertTitle>
                            <AlertDescription>{error}</AlertDescription>
                        </Alert>
                    ) : (
                        <>
                            <div className="grid gap-4 md:grid-cols-3">
                                <MetricCard
                                    title="Usuários totais"
                                    value={stats?.total_users ?? 0}
                                    description="Quantidade total de contas cadastradas."
                                />
                                <MetricCard
                                    title="Login sem SSO"
                                    value={authProviderCounts.without_sso ?? 0}
                                    description="Contas classificadas como autenticação por email e senha."
                                />
                                <MetricCard
                                    title="Origens de visita"
                                    value={visitOrigins.length}
                                    description="Quantidade de origens distintas registradas em landing visits."
                                />
                            </div>

                            <div className="grid gap-6 xl:grid-cols-2">
                                <Card>
                                    <CardHeader>
                                        <div className="flex items-center gap-2">
                                            <Users className="h-5 w-5" />
                                            <CardTitle>Usuários por plano</CardTitle>
                                        </div>
                                        <CardDescription>Plano efetivo atual, contando `free` para quem não possui assinatura ativa.</CardDescription>
                                    </CardHeader>
                                    <CardContent className="space-y-3">
                                        {planCounts.map((item) => (
                                            <div key={item.plan_id} className="flex items-center justify-between rounded-lg border p-3">
                                                <div>
                                                    <p className="font-medium">{item.plan_name}</p>
                                                    <p className="text-sm text-muted-foreground">{item.plan_id}</p>
                                                </div>
                                                <Badge variant="secondary">{item.users_count}</Badge>
                                            </div>
                                        ))}
                                    </CardContent>
                                </Card>

                                <Card>
                                    <CardHeader>
                                        <div className="flex items-center gap-2">
                                            <ShieldCheck className="h-5 w-5" />
                                            <CardTitle>Autenticação</CardTitle>
                                        </div>
                                        <CardDescription>Distribuição por método principal de autenticação.</CardDescription>
                                    </CardHeader>
                                    <CardContent className="space-y-3">
                                        <div className="flex items-center justify-between rounded-lg border p-3">
                                            <span>Sem SSO</span>
                                            <Badge variant="secondary">{authProviderCounts.without_sso ?? 0}</Badge>
                                        </div>
                                        <div className="flex items-center justify-between rounded-lg border p-3">
                                            <span>Google</span>
                                            <Badge variant="secondary">{authProviderCounts.google ?? 0}</Badge>
                                        </div>
                                        <div className="flex items-center justify-between rounded-lg border p-3">
                                            <span>Microsoft</span>
                                            <Badge variant="secondary">{authProviderCounts.microsoft ?? 0}</Badge>
                                        </div>
                                    </CardContent>
                                </Card>
                            </div>

                            <Card>
                                <CardHeader>
                                    <div className="flex items-center gap-2">
                                        <BarChart3 className="h-5 w-5" />
                                        <CardTitle>Visitas por origem</CardTitle>
                                    </div>
                                    <CardDescription>Contagem agrupada pelo campo `visit` da tabela `landing_visits`.</CardDescription>
                                </CardHeader>
                                <CardContent className="space-y-3">
                                    {visitOrigins.length > 0 ? (
                                        visitOrigins.map((item) => (
                                            <div key={item.visit} className="flex items-center justify-between rounded-lg border p-3">
                                                <span className="font-medium">{item.visit}</span>
                                                <Badge variant="secondary">{item.visits_count}</Badge>
                                            </div>
                                        ))
                                    ) : (
                                        <p className="text-sm text-muted-foreground">Nenhuma visita registrada até o momento.</p>
                                    )}
                                </CardContent>
                            </Card>
                        </>
                    )}
                </div>
            </BaseLayout>
        </>
    )
}
