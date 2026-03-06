import React from "react";
import { Badge } from "@/components/ui/badge"
import { Separator } from "@/components/ui/separator"

export default function InvestmentsContent({ investment }) {

    const formatCurrency = (val) => val.toLocaleString("pt-BR", { minimumFractionDigits: 2, maximumFractionDigits: 2 });

    const items = [
        {
            label: `IR (${investment.calculated[0].IR.toLocaleString("pt-BR", { minimumFractionDigits: 0, maximumFractionDigits: 1 })}%)`,
            value: `R$ ${formatCurrency(investment.calculated[0].IR_value)}`,
            variant: investment.calculated[0].IR > 15 ? "destructive" : "secondary"
        },
        {
            label: `IOF (${investment.calculated[0].IOF.toLocaleString("pt-BR", { maximumFractionDigits: 0 })}%)`,
            value: `R$ ${formatCurrency(investment.calculated[0].IOF_value)}`,
            variant: investment.calculated[0].IOF > 0 ? "destructive" : "secondary"
        },
        {
            label: "Valor bruto",
            value: `R$ ${formatCurrency(investment.calculated[0].value_brute)}`,
            variant: "default"
        },
        {
            label: "Valor líquido atual",
            value: `R$ ${formatCurrency(investment.calculated[0].value_liq)}`,
            variant: "default"
        },
        {
            label: "Rend. líq. estimado no venc.",
            value: `R$ ${investment.date_expected_sell
                ? formatCurrency(investment.calculated[1].profit_liq)
                : formatCurrency(investment.calculated[0].profit_liq) + ' *'}`,
            variant: "default"
        },
        {
            label: "Valor líq. estimado no venc.",
            value: `R$ ${investment.date_expected_sell
                ? formatCurrency(investment.calculated[1].value_liq)
                : formatCurrency(investment.calculated[0].value_liq) + ' *'}`,
            variant: "default"
        }
    ];

    return (
        <div className="px-4 py-4">
            <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-6 gap-4">
                {items.map((item, index) => (
                    <div key={index} className="flex flex-col gap-1">
                        <span className="text-xs text-muted-foreground">{item.label}</span>
                        <Badge variant={item.variant} className="w-fit text-xs">
                            {item.value}
                        </Badge>
                    </div>
                ))}
            </div>
            {!investment.date_expected_sell && (
                <p className="text-xs text-muted-foreground mt-3">* Valores estimados baseados na data atual (liquidez diária)</p>
            )}
        </div>
    );
}