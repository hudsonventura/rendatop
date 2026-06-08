import { Check, Crown, Sparkles } from "lucide-react"

import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardFooter, CardHeader, CardTitle } from "@/components/ui/card"
import { Separator } from "@/components/ui/separator"
import { Skeleton } from "@/components/ui/skeleton"

const planIcons = { free: null, plus: Sparkles, pro: Crown }

export function SubscriptionPlanGrid({
    plans,
    loading = false,
    currentPlanId = "free",
    pendingPlanId = null,
    activeSub = null,
    onSelectPlan,
    onCancelSubscription,
    onCancelPending,
    compact = false,
}) {
    if (loading) {
        return (
            <div className="grid grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-4">
                {[1, 2, 3, 4].map((item) => (
                    <Skeleton key={item} className={compact ? "h-72 rounded-xl" : "h-80 rounded-xl"} />
                ))}
            </div>
        )
    }

    return (
        <div className="grid grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-4">
            {plans.map((plan) => {
                const isActive = currentPlanId === plan.id
                const isPending = pendingPlanId === plan.id
                const PlanIcon = planIcons[plan.id]
                const isPopular = plan.id === "plus"
                const cardBadge = isPending
                    ? (
                        <Badge variant="outline" className="border-amber-500 bg-background text-xs text-amber-600">
                            Pagamento pendente
                        </Badge>
                    )
                    : isActive
                        ? (
                            <Badge variant="outline" className="border-primary bg-background text-xs text-primary">
                                <Check className="mr-1 h-3 w-3" /> Atual
                            </Badge>
                        )
                        : isPopular
                            ? <Badge className="border border-border bg-background text-xs text-foreground">Mais popular</Badge>
                            : null

                return (
                    <Card
                        key={plan.id}
                        className={`relative flex flex-col transition-all ${
                            isActive
                                ? "border-primary ring-1 ring-primary/20"
                                : isPending
                                    ? "border-amber-500/60 ring-1 ring-amber-500/20"
                                    : "hover:border-primary/40"
                        }`}
                    >
                        {cardBadge && (
                            <div className="absolute -top-3 left-1/2 z-20 -translate-x-1/2 rounded-md bg-background px-1">
                                {cardBadge}
                            </div>
                        )}

                        <CardHeader className={compact ? "pb-3 pt-5" : "pb-4 pt-6"}>
                            <div className="flex items-center gap-2">
                                {PlanIcon && (
                                    <PlanIcon className={`h-5 w-5 ${plan.id === "pro" ? "text-yellow-400" : "text-primary"}`} />
                                )}
                                <CardTitle className="text-lg">{plan.name}</CardTitle>
                            </div>
                            <div className="mt-3">
                                {plan.price > 0 ? (
                                    <div className="flex items-baseline gap-1">
                                        <span className={compact ? "text-2xl font-bold" : "text-3xl font-bold"}>
                                            R${plan.price.toFixed(2).replace(".", ",")}
                                        </span>
                                        <span className="text-sm text-muted-foreground">/mês</span>
                                    </div>
                                ) : (
                                    <span className={compact ? "text-2xl font-bold" : "text-3xl font-bold"}>Grátis</span>
                                )}
                            </div>
                        </CardHeader>

                        <CardContent className="flex-1">
                            <Separator className="mb-4" />
                            <ul className={compact ? "space-y-2" : "space-y-2.5"}>
                                {Object.entries(plan.features || {}).map(([key, text]) => (
                                    <li key={key} className="flex items-start gap-2 text-sm">
                                        <Check className="mt-0.5 h-4 w-4 shrink-0 text-primary" />
                                        <span className="text-muted-foreground">{text}</span>
                                    </li>
                                ))}
                            </ul>
                        </CardContent>

                        <CardFooter className="pt-0">
                            {isActive ? (
                                plan.price > 0 && onCancelSubscription ? (
                                    <Button
                                        variant="outline"
                                        className="w-full"
                                        disabled={Boolean(activeSub?.cancel_at_period_end)}
                                        onClick={onCancelSubscription}
                                    >
                                        {activeSub?.cancel_at_period_end ? "Cancelamento agendado" : "Cancelar assinatura"}
                                    </Button>
                                ) : (
                                    <Button variant="outline" className="w-full" disabled>
                                        {plan.price > 0 ? "Plano atual" : "Plano básico"}
                                    </Button>
                                )
                            ) : isPending ? (
                                <Button
                                    variant="outline"
                                    className="w-full"
                                    disabled={!onCancelPending}
                                    onClick={onCancelPending}
                                >
                                    Cancelar pendência
                                </Button>
                            ) : plan.price > 0 ? (
                                <Button className="w-full" onClick={() => onSelectPlan?.(plan)}>
                                    Assinar {plan.name}
                                </Button>
                            ) : (
                                <Button variant="outline" className="w-full" disabled>
                                    Plano básico
                                </Button>
                            )}
                        </CardFooter>
                    </Card>
                )
            })}
        </div>
    )
}
