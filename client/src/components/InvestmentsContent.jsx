import React from "react";
  
import {
    CardContent,
  } from "@/components/ui/card"
  import { Badge } from "@/components/ui/badge"
  import { Separator } from "@/components/ui/separator"

  
  import { Button } from "@/components/ui/button"
  
  
export default function InvestmentsContent({investment}) {

    return (
        <>
            <CardContent style={{ marginTop: "1rem" }}>
                <div className="flex h-3 items-center space-x-2">
                    <Separator orientation="vertical" />
                    <div>IR ({investment.calculated[0].IR.toLocaleString("pt-BR", { minimumFractionDigits: 0, maximumFractionDigits: 1 })}%): 
                        <Badge variant={investment.calculated[0].IR > 15 ? "destructive" : "secondary"}> 
                            R$ {(investment.calculated[0].IR_value).toLocaleString("pt-BR", { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                        </Badge>
                    </div>
                    <Separator orientation="vertical" />
                    <div>IOF ({investment.calculated[0].IOF.toLocaleString("pt-BR", { maximumFractionDigits: 0 })}%): 
                        <Badge variant={investment.calculated[0].IOF > 0 ? "destructive" : "secondary"}> 
                            R$ {(investment.calculated[0].IOF_value).toLocaleString("pt-BR", { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                        </Badge>
                    </div>
                    
                    <Separator orientation="vertical" />
                    <div>Valor bruto:  <Badge>R$ {investment.calculated[0].value_brute.toLocaleString("pt-BR", { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</Badge > </div>
                    
                    <Separator orientation="vertical" />
                    <div>Valor líquido atual:  <Badge>R$ {investment.calculated[0].value_liq.toLocaleString("pt-BR", { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</Badge > </div>

                    <Separator orientation="vertical" />
                    <div>Rend. líq. estimado no venc.:  
                        <Badge> R$ {
                            investment.date_expected_sell ? 
                            investment.calculated[1].profit_liq.toLocaleString("pt-BR", { minimumFractionDigits: 2, maximumFractionDigits: 2 }) 
                            : investment.calculated[0].profit_liq.toLocaleString("pt-BR", { minimumFractionDigits: 2, maximumFractionDigits: 2 })+' *'}
                        </Badge > 
                    </div>

                    <Separator orientation="vertical" />
                    <div>Valor líq. estimado no venc.:  
                        <Badge> R$ {

                            investment.date_expected_sell ? 
                            investment.calculated[1].value_liq.toLocaleString("pt-BR", { minimumFractionDigits: 2, maximumFractionDigits: 2 }) 
                            : investment.calculated[0].value_liq.toLocaleString("pt-BR", { minimumFractionDigits: 2, maximumFractionDigits: 2 })+' *'}
                        </Badge > 
                    </div>
                </div>
            </CardContent>

        </>
    );

}