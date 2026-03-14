import { Badge } from "@/components/ui/badge"
import { Tooltip, TooltipContent, TooltipTrigger } from "@/components/ui/tooltip"
import { formatIrPercent, getIrBadgeClass, getIrBadgeLabel } from "@/utils/ir-level"

function formatCurrency(value) {
    return Number(value ?? 0).toLocaleString("pt-BR", {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2,
    })
}

export default function IrBadge({
    irPercent,
    irValue,
    variant = "secondary",
    className = "",
    showValue = false,
    showPercentInTooltip = true,
    label,
}) {
    const content = label ?? (showValue ? `IR: R$ ${formatCurrency(irValue)}` : getIrBadgeLabel(irPercent))

    const badge = (
        <Badge variant={variant} className={`${getIrBadgeClass(irPercent)} ${className}`.trim()}>
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
                <div>Alíquota de IR: {formatIrPercent(irPercent)}%</div>
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
