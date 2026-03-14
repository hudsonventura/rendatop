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

export default function IofBadge({
    iofPercent,
    iofValue,
    variant = "secondary",
    className = "",
    showValue = false,
    showPercentInTooltip = false,
    label,
}) {
    const content = label ?? (showValue ? `IOF: R$ ${formatCurrency(iofValue)}` : iofPercent > 0 ? "IOF" : "Isento IOF")

    const badge = (
        <Badge variant={variant} className={`${getIofBadgeClass(iofPercent)} ${className}`.trim()}>
            {content}
        </Badge>
    )

    if (!showPercentInTooltip) {
        return badge
    }

    return (
        <Tooltip>
            <TooltipTrigger asChild>
                <span className="inline-flex">{badge}</span>
            </TooltipTrigger>
            <TooltipContent side="top" className="max-w-xs space-y-1">
                <div>Alíquota de IOF: {formatPercent(iofPercent)}%</div>
                <div className="text-[11px] opacity-90">
                    Regras: <br />
                    - IOF é cobrado de forma regressiva nos primeiros 30 dias<br />
                    - Após 30 dias, a alíquota é zero
                </div>
            </TooltipContent>
        </Tooltip>
    )
}
