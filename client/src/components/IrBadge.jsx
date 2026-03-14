import { Badge } from "@/components/ui/badge"
import { Tooltip, TooltipContent, TooltipTrigger } from "@/components/ui/tooltip"
import { formatIrPercent, getIrBadgeClass, getIrBadgeLabel } from "@/utils/ir-level"

function formatCurrency(value) {
    return Number(value ?? 0).toLocaleString("pt-BR", {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2,
    })
}

function getNextIrStep(investmentDate) {
    if (!investmentDate) return null

    const startDate = new Date(investmentDate)
    if (Number.isNaN(startDate.getTime())) return null

    const today = new Date()
    startDate.setHours(0, 0, 0, 0)
    today.setHours(0, 0, 0, 0)

    const elapsedDays = Math.floor((today.getTime() - startDate.getTime()) / (1000 * 60 * 60 * 24))
    const thresholds = [
        { days: 181, label: "20%" },
        { days: 366, label: "17,5%" },
        { days: 731, label: "15%" },
    ]

    const nextThreshold = thresholds.find((threshold) => elapsedDays < threshold.days)
    if (!nextThreshold) return null

    const nextDate = new Date(startDate)
    nextDate.setDate(nextDate.getDate() + nextThreshold.days)
    const daysUntil = Math.ceil((nextDate.getTime() - today.getTime()) / (1000 * 60 * 60 * 24))

    return {
        label: nextThreshold.label,
        dateLabel: nextDate.toLocaleDateString("pt-BR", {
            day: "2-digit",
            month: "2-digit",
            year: "numeric",
        }),
        daysUntil,
    }
}

export default function IrBadge({
    irPercent,
    irValue,
    variant = "secondary",
    className = "",
    showValue = false,
    showPercentInTooltip = true,
    label,
    asBadge = true,
    investmentDate,
}) {
    const colorClassName = `${getIrBadgeClass(irPercent)} ${className}`.trim()
    const nextIrStep = getNextIrStep(investmentDate)

    const contentNode = asBadge ? (
        <Badge variant={variant} className={colorClassName}>
            {label ?? (showValue ? `IR: R$ ${formatCurrency(irValue)}` : getIrBadgeLabel(irPercent))}
        </Badge>
    ) : (
        <span className={colorClassName}>
            {label ?? (showValue ? `R$ ${formatCurrency(irValue)}` : getIrBadgeLabel(irPercent))}
        </span>
    )

    if (!showPercentInTooltip) {
        return contentNode
    }

    return (
        <Tooltip>
            <TooltipTrigger asChild>
                <span className="inline-flex">{contentNode}</span>
            </TooltipTrigger>
            <TooltipContent side="top" className="max-w-xs space-y-1">
                <div>Alíquota de IR: {formatIrPercent(irPercent)}%</div><br />
                {nextIrStep && (
                    <div className="text-[11px] opacity-90">
                        Próxima faixa: {nextIrStep.label} em {nextIrStep.dateLabel} (em {nextIrStep.daysUntil} dias)<br /><br />
                    </div>
                )}
                <div className="text-[11px] opacity-90">
                    Regras: <br />
                    - 22,5% até 180 dias<br />
                    - 20% até 365 dias<br />
                    - 17,5% até 730 dias<br />
                    - 15% mais que 730 dias
                </div>
            </TooltipContent>
        </Tooltip>
    )
}
