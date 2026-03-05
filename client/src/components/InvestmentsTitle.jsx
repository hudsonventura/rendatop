import React from "react";
  
import {
    Card,
    CardContent,
    CardDescription,
    CardTitle,
  } from "@/components/ui/card"
  import { Badge } from "@/components/ui/badge"

import {
    HoverCard,
    HoverCardContent,
    HoverCardTrigger,
  } from "@/components/ui/hover-card"
  

  
  
export default function InvestmentsTableLine({investment}) {


    return (
        <>
            <Card>
                <CardContent style={{ marginTop: "1rem", minWidth: "76rem" }}>
                        <CardTitle className="flex items-center space-x-2">
                            <div>{investment.title} <Badge variant="outline">{(() => {
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
                                                            })()}
                            </Badge>
                            
                            
                            </div>
                            <div className="grow flex flex-col items-end">
                                <div className="flex space-x-1">
                                    <HoverCard>
                                        <HoverCardTrigger><Badge variant="subtle" className="text-xs">+ R$ {investment.calculated[0].profit_liq.toLocaleString("pt-BR", { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</Badge></HoverCardTrigger>
                                            <HoverCardContent className="text-xs"><span>Valor líquido atual, para caso o resgate seja feito hoje. O valor pode mudar diariamente até a data de vencimento.</span></HoverCardContent>
                                    </HoverCard>
                                    <Badge className="hover:cursor-default text-lg">
                                        R$ {investment.value.toLocaleString("pt-BR", { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                                    </Badge>
                                    
                                </div>
                                <div className="text-sm text-muted-foreground self-end">
                                    {!investment.date_expected_sell
                                        ? "Liquidez diária"
                                        : "Vencimento: "+new Date(investment.date_expected_sell).toLocaleString("pt-BR", {
                                            year: "numeric",
                                            month: "2-digit",
                                            day: "2-digit",
                                        })}
                                </div>
                            </div>
                        </CardTitle>
                        <CardDescription className="grow flex justify-start">
                            {investment.bank} em {new Date(investment.date_buy).toLocaleString("pt-BR", {
                                                            year: "numeric",
                                                            month: "2-digit",
                                                            day: "2-digit",
                                                                })}
                                <div style={{marginLeft: "1%"}}>
                                    {
                                    Math.round((new Date()-new Date(investment.date_buy))/1000/60/60/24) < 30
                                    ? <HoverCard>
                                        <HoverCardTrigger><Badge variant="destructive">Incidência de IOF</Badge></HoverCardTrigger>
                                            <HoverCardContent>
                                                Isenção IOF: Aguarde até {new Date(new Date(investment.date_buy).getTime() + 30*24*60*60*1000).toLocaleString("pt-BR", {
                                                    year: "numeric",
                                                    month: "2-digit",
                                                    day: "2-digit",
                                                })} para isenção
                                            </HoverCardContent>
                                        </HoverCard>
                                    : <Badge variant="subtle" className="border-green-400 bg-green-100 dark:bg-green-800 text-green-700">
                                            Isento IOF
                                    </Badge>
                                    
                                    }
                                </div>
                                <div className="text-sm text-muted-foreground self-end" style={{marginLeft: "1%"}}>
                                    {!investment.date_expected_sell
                                        ? <div className="text-sm text-muted-foreground self-end" style={{marginLeft: "1%"}}>
                                            <Badge variant="subtle" className="border-green-400 bg-green-100 dark:bg-green-800 text-green-700">
                                                Resgate imediato 
                                            </Badge> 
                                        </div>
                                        : <Badge variant="destructive">
                                            Resgate no vencimento 
                                        </Badge> 
                                    }
                                </div>
                                <div className="text-sm text-muted-foreground self-end" style={{marginLeft: "1%"}}>
                                    {
                                        investment.calculated[0].IR > 20 
                                        ? 
                                            <HoverCard>
                                                <HoverCardTrigger><Badge variant="destructive">Incidência de 22.5% de IR</Badge></HoverCardTrigger>
                                                    <HoverCardContent>
                                                        Isenção IOF: Aguarde {new Date(new Date(investment.date_buy).getTime() + 180*24*60*60*1000).toLocaleString("pt-BR", {
                                                        year: "numeric",
                                                        month: "2-digit",
                                                        day: "2-digit",
                                                    })} dias p/ pagar menos IR (20%)
                                                </HoverCardContent>
                                            </HoverCard>
                                        : 
                                            investment.calculated[0].IR > 17.5 
                                            ? 
                                                <HoverCard>
                                                    <HoverCardTrigger><Badge Badge variant="subtle" className="inline-flex items-center justify-center rounded-full bg-orange-500 px-3 py-1 text-xs font-semibold text-white">Incidência de 20% de IR</Badge></HoverCardTrigger>
                                                        <HoverCardContent>
                                                            Isenção IOF: Aguarde {new Date(new Date(investment.date_buy).getTime() + 360*24*60*60*1000).toLocaleString("pt-BR", {
                                                            year: "numeric",
                                                            month: "2-digit",
                                                            day: "2-digit",
                                                        })} dias p/ pagar menos IR (17.5%)
                                                    </HoverCardContent>
                                                </HoverCard>
                                            : 
                                                investment.calculated[0].IR > 15 
                                                ? 
                                                    <HoverCard>
                                                        <HoverCardTrigger><Badge Badge variant="subtle" className="inline-flex items-center justify-center rounded-full bg-yellow-500 px-3 py-1 text-xs font-semibold text-white">Incidência de 17.5% de IR</Badge></HoverCardTrigger>
                                                            <HoverCardContent>
                                                                Isenção IOF: Aguarde {new Date(new Date(investment.date_buy).getTime() + 720*24*60*60*1000).toLocaleString("pt-BR", {
                                                                year: "numeric",
                                                                month: "2-digit",
                                                                day: "2-digit",
                                                            })} dias p/ pagar menos IR (15%)
                                                        </HoverCardContent>
                                                    </HoverCard>
                                                : 

                                                    investment.calculated[0].IR > 0 
                                                    ?
                                                        <Badge variant="subtle" className="border-green-400 bg-green-100 dark:bg-green-800 text-green-700">
                                                            15% de IR
                                                        </Badge>
                                                    :
                                                        <Badge variant="subtle" className="border-green-400 bg-green-100 dark:bg-green-800 text-green-700">
                                                            Isento IR
                                                        </Badge>
                                    }
                                </div>

                        </CardDescription>
                </CardContent>

            </Card>
        </>
    );

}