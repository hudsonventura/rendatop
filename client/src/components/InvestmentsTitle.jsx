import React from "react";
import { Badge } from "@/components/ui/badge"
import {
    HoverCard,
    HoverCardContent,
    HoverCardTrigger,
} from "@/components/ui/hover-card"
import { getIrBadgeClass, getIrBadgeLabel } from "@/utils/ir-level"
import { getIofBadgeClass } from "@/utils/iof-level"


export default function InvestmentsTitle({ investment }) {

    const getIndexLabel = () => {
        switch (investment.index) {
            case 'PERCENT_YEAR':
                return `${investment.index_percent}% aa`;
            case 'CDI':
                return `${investment.index_percent}% CDI`;
            case 'IPCA_MAIS':
                return `IPCA+${investment.index_percent}%`;
            default:
                return `${investment.index_percent}% ??`;
        }
    };

    const daysSinceBuy = Math.round((new Date() - new Date(investment.date_buy)) / 1000 / 60 / 60 / 24);
    const formatDate = (dateStr) => new Date(dateStr).toLocaleString("pt-BR", { year: "numeric", month: "2-digit", day: "2-digit" });
    const formatCurrency = (val) => val.toLocaleString("pt-BR", { minimumFractionDigits: 2, maximumFractionDigits: 2 });

    return (
        <div className="flex flex-col md:flex-row md:items-center w-full gap-2 text-left">
            {/* Left: Title + Index */}
            <div className="flex items-center gap-2 min-w-0 flex-shrink-0">
                <span className="font-semibold text-sm truncate">{investment.title}</span>
                <Badge variant="outline" className="text-xs whitespace-nowrap">{getIndexLabel()}</Badge>
            </div>

            {/* Center: Badges */}
            <div className="flex flex-wrap items-center gap-1.5">
                {/* IOF Badge */}
                {daysSinceBuy < 30 ? (
                    <HoverCard>
                        <HoverCardTrigger>
                            <Badge variant="secondary" className={`text-xs ${getIofBadgeClass(1)}`}>IOF</Badge>
                        </HoverCardTrigger>
                        <HoverCardContent className="text-xs">
                            Isenção IOF em {formatDate(new Date(new Date(investment.date_buy).getTime() + 30 * 24 * 60 * 60 * 1000))}
                        </HoverCardContent>
                    </HoverCard>
                ) : (
                    <Badge variant="secondary" className={`text-xs ${getIofBadgeClass(0)}`}>
                        Isento IOF
                    </Badge>
                )}

                {/* Liquidez Badge */}
                {!investment.due_date ? (
                    <Badge variant="secondary" className="text-xs bg-green-500/10 text-green-700 dark:text-green-400 border-green-500/20">
                        Liquidez diária
                    </Badge>
                ) : (
                    <Badge variant="secondary" className="text-xs">
                        Venc: {formatDate(investment.due_date)}
                    </Badge>
                )}

                {/* IR Badge */}
                <Badge variant="secondary" className={`text-xs ${getIrBadgeClass(investment.calculated[0].IR)}`}>
                    {getIrBadgeLabel(investment.calculated[0].IR)}
                </Badge>
            </div>

            {/* Right: Values */}
            <div className="flex items-center gap-2 md:ml-auto flex-shrink-0">
                <HoverCard>
                    <HoverCardTrigger>
                        <Badge variant="secondary" className="text-xs text-green-700 dark:text-green-400">
                            + R$ {formatCurrency(investment.calculated[0].profit_liq)}
                        </Badge>
                    </HoverCardTrigger>
                    <HoverCardContent className="text-xs">
                        Valor líquido atual, caso o resgate seja feito hoje.
                    </HoverCardContent>
                </HoverCard>
                <Badge className="text-sm font-semibold">
                    R$ {formatCurrency(investment.value)}
                </Badge>
            </div>
        </div>
    );
}
