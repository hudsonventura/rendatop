import { useCallback, useEffect, useState } from "react"
import { useNavigate } from "react-router-dom"
import { LockKeyhole, Sparkles } from "lucide-react"

import { SubscriptionPlanGrid } from "@/components/subscription-plan-grid"
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from "@/components/ui/dialog"
import axiosInstance from "@/utils/axiosConfig"

const DEFAULT_MESSAGE = "Apenas usuarios de planos pagos podem usar esta funcionalidade e acessar limites extendidos."

export function UpgradePlanModal({
    open,
    onOpenChange,
    message = DEFAULT_MESSAGE,
    title = "Recurso disponivel nos planos pagos",
}) {
    const navigate = useNavigate()
    const [plans, setPlans] = useState([])
    const [overview, setOverview] = useState(null)
    const [loading, setLoading] = useState(false)

    const loadPlans = useCallback(async () => {
        setLoading(true)
        try {
            const [plansRes, overviewRes] = await Promise.all([
                axiosInstance.get("/plans"),
                axiosInstance.get("/subscription/overview").catch(() => ({ data: null })),
            ])
            setPlans(plansRes.data || [])
            setOverview(overviewRes.data || null)
        } catch {
            setPlans([])
            setOverview(null)
        } finally {
            setLoading(false)
        }
    }, [])

    useEffect(() => {
        if (open && plans.length === 0) {
            loadPlans()
        }
    }, [open, plans.length, loadPlans])

    const handleSelectPlan = (plan) => {
        if (!plan?.id || plan.price <= 0) return
        onOpenChange(false)
        navigate(`/subscription?plan=${encodeURIComponent(plan.id)}`)
    }

    return (
        <Dialog open={open} onOpenChange={onOpenChange}>
            <DialogContent className="max-h-[92vh] w-[95vw] overflow-y-auto sm:max-w-5xl">
                <DialogHeader className="space-y-3">
                    <div className="flex h-11 w-11 items-center justify-center rounded-full bg-primary/10 text-primary">
                        <LockKeyhole className="h-5 w-5" />
                    </div>
                    <div className="space-y-2">
                        <DialogTitle className="flex items-center gap-2 text-xl">
                            {title}
                            <Sparkles className="h-5 w-5 text-primary" />
                        </DialogTitle>
                        <DialogDescription className="max-w-2xl text-sm leading-relaxed">
                            {message || DEFAULT_MESSAGE}
                        </DialogDescription>
                    </div>
                </DialogHeader>

                <SubscriptionPlanGrid
                    plans={plans}
                    loading={loading}
                    currentPlanId={overview?.active_subscription?.plan_id || "free"}
                    pendingPlanId={overview?.pending_subscription?.plan_id || null}
                    activeSub={overview?.active_subscription || null}
                    onSelectPlan={handleSelectPlan}
                    compact
                />
            </DialogContent>
        </Dialog>
    )
}
