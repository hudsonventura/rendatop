import React from "react";
import { Badge } from "@/components/ui/badge"
import InvestmentsEdit from "@/components/InvestmentsEdit"
import InvestmentsRedeem from "@/components/InvestmentsRedeem"
import IrBadge from "@/components/IrBadge"
import IofBadge from "@/components/IofBadge"

export default function InvestmentsContent({ investment, setReload }) {

    const formatCurrency = (val) => val.toLocaleString("pt-BR", { minimumFractionDigits: 2, maximumFractionDigits: 2 });

    const items = [
        {
            label: `IR (${investment.calculated[0].IR.toLocaleString("pt-BR", { minimumFractionDigits: 0, maximumFractionDigits: 1 })}%)`,
            badge: <IrBadge irPercent={investment.calculated[0].IR} irValue={investment.calculated[0].IR_value} showValue />,
        },
        {
            label: `IOF (${investment.calculated[0].IOF.toLocaleString("pt-BR", { maximumFractionDigits: 0 })}%)`,
            badge: <IofBadge iofPercent={investment.calculated[0].IOF} iofValue={investment.calculated[0].IOF_value} showValue />,
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
            value: `R$ ${investment.due_date
                ? formatCurrency(investment.calculated[1].profit_liq)
                : formatCurrency(investment.calculated[0].profit_liq) + ' *'}`,
            variant: "default"
        },
        {
            label: "Valor líq. estimado no venc.",
            value: `R$ ${investment.due_date
                ? formatCurrency(investment.calculated[1].value_liq)
                : formatCurrency(investment.calculated[0].value_liq) + ' *'}`,
            variant: "default"
        }
    ];

    return (
        <div className="px-4 py-4 space-y-4">
            <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-6 gap-4">
                {items.map((item, index) => (
                    <div key={index} className="flex flex-col gap-1">
                        <span className="text-xs text-muted-foreground">{item.label}</span>
                        {item.badge ? item.badge : (
                            <Badge variant={item.variant} className={`w-fit text-xs ${item.className ?? ""}`}>
                                {item.value}
                            </Badge>
                        )}
                    </div>
                ))}
            </div>
            {!investment.due_date && (
                <p className="text-xs text-muted-foreground">* Valores estimados baseados na data atual (liquidez diária)</p>
            )}
            <div className="flex justify-end gap-2 pt-1">
                <InvestmentsRedeem investment={investment} setReload={setReload} />
                <InvestmentsEdit investment={investment} setReload={setReload} />
            </div>
        </div>
    );
}
