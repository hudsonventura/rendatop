import { Badge } from "@/components/ui/badge"
import { Tooltip, TooltipContent, TooltipTrigger } from "@/components/ui/tooltip"
import { getIofBadgeClass } from "@/utils/iof-level"

function formatCurrency(value) {
    return Number(value ?? 0).toLocaleString("pt-BR", {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2,
    })
}

function formatPercent(value) {
    const numeric = Number(value ?? 0)
    return numeric.toLocaleString("pt-BR", {
        minimumFractionDigits: Number.isInteger(numeric) ? 0 : 1,
        maximumFractionDigits: 1,
    })
}

function formatIofZeroDate(investmentDate) {
    if (!investmentDate) return null

    const startDate = new Date(investmentDate)
    if (Number.isNaN(startDate.getTime())) return null

    const zeroDate = new Date(startDate)
    zeroDate.setDate(zeroDate.getDate() + 30)

    const today = new Date()
    today.setHours(0, 0, 0, 0)
    zeroDate.setHours(0, 0, 0, 0)
    const diffInDays = Math.max(
        0,
        Math.ceil((zeroDate.getTime() - today.getTime()) / (1000 * 60 * 60 * 24))
    )

    return {
        dateLabel: zeroDate.toLocaleDateString("pt-BR", {
            day: "2-digit",
            month: "2-digit",
            year: "numeric",
        }),
        daysUntil: diffInDays,
    }
}

export default function IofBadge({
    iofPercent,
    iofValue,
    variant = "secondary",
    className = "",
    showValue = false,
    showPercentInTooltip = false,
    label,
    asBadge = true,
    investmentDate,
}) {
    const colorClassName = `${getIofBadgeClass(iofPercent)} ${className}`.trim()
    const iofZeroDate = formatIofZeroDate(investmentDate)

    const contentNode = asBadge ? (
        <Badge variant={variant} className={colorClassName}>
            {label ?? (showValue ? `IOF: R$ ${formatCurrency(iofValue)}` : iofPercent > 0 ? "IOF" : "Isento IOF")}
        </Badge>
    ) : (
        <span className={colorClassName}>
            {label ?? (showValue ? `R$ ${formatCurrency(iofValue)}` : iofPercent > 0 ? "IOF" : "Isento IOF")}
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
                <div>Alíquota de IOF: {formatPercent(iofPercent)}%</div><br />
                {iofZeroDate && (
                    <div className="text-[11px] opacity-90">
                        IOF zerado em: {iofZeroDate.dateLabel} (em {iofZeroDate.daysUntil} dias)<br /><br />
                    </div>
                )}
                <div className="text-[11px] opacity-90">
                    Regras: <br />
                    - IOF é cobrado de forma regressiva nos primeiros 30 dias<br />
                    - Após 30 dias, a alíquota é zero
                </div>
            </TooltipContent>
        </Tooltip>
    )
}
