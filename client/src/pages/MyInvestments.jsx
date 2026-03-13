import React, { useMemo } from "react";
import { useEffect, useState } from "react";
import axiosInstance from "@/utils/axiosConfig";

import { BaseLayout } from "@/components/layouts/base-layout";
import Logged from "@/components/Logged";
import InvestmentsAdd from "@/components/InvestmentsAdd";
import InvestmentsDataTable from "@/components/InvestmentsDataTable";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";

function getTodayAtMidnight() {
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    return today;
}

function formatDecimalDisplay(value) {
    if (value === null || value === undefined || value === "") return "";

    const [int, dec = ""] = String(value).split(".");
    const intFormatted = int.replace(/\B(?=(\d{3})+(?!\d))/g, ".");
    return dec ? `${intFormatted},${dec.substring(0, 2)}` : intFormatted;
}

function getReinvestmentInitialValues(investment) {
    return {
        title: investment.title ?? "",
        date_buy: getTodayAtMidnight(),
        due_date: undefined,
        liquidez_diaria: false,
        taxes: investment.taxes ?? true,
        bank_code: investment.bank?.code ?? investment.bank_code ?? undefined,
        value: formatDecimalDisplay(investment.calculated?.[0]?.value_liq ?? ""),
        index: investment.index === "CDI" ? "0" : investment.index === "IPCA_MAIS" ? "1" : investment.index === "PERCENT_YEAR" ? "2" : "",
        index_percent: formatDecimalDisplay(investment.index_percent ?? ""),
    };
}

function InvestmentsTableSkeleton() {
    return (
        <div className="space-y-4">
            <div className="overflow-hidden rounded-lg border">
                <div className="border-b bg-muted p-4">
                    <div className="grid grid-cols-7 gap-3">
                        {Array.from({ length: 7 }).map((_, i) => (
                            <Skeleton key={i} className="h-4 w-full" />
                        ))}
                    </div>
                </div>
                <div className="space-y-3 p-4">
                    {Array.from({ length: 6 }).map((_, i) => (
                        <div key={i} className="grid grid-cols-7 gap-3">
                            {Array.from({ length: 7 }).map((__, j) => (
                                <Skeleton key={j} className="h-5 w-full" />
                            ))}
                        </div>
                    ))}
                </div>
            </div>
        </div>
    );
}

const MyInvestments = () => {
    const [investments, setInvestments] = useState([]);
    const [loadingInvestments, setLoadingInvestments] = useState(true);
    const [reload, setReload] = useState(0);
    const [reinvestOpen, setReinvestOpen] = useState(false);
    const [reinvestInitialValues, setReinvestInitialValues] = useState(null);

    useEffect(() => {
        let cancelled = false;
        setLoadingInvestments(true);

        axiosInstance
            .get("/Investments")
            .then((response) => {
                if (cancelled) return;
                setInvestments(response.data ?? []);
            })
            .catch((err) => {
                console.error("Erro ao buscar investimentos:", err);
                if (cancelled) return;
                setInvestments([]);
            })
            .finally(() => {
                if (cancelled) return;
                setLoadingInvestments(false);
            });

        return () => {
            cancelled = true;
        };
    }, [reload]);

    const handleReinvest = (investment) => {
        setReinvestInitialValues(getReinvestmentInitialValues(investment));
        setReinvestOpen(true);
    };

    const { available, locked } = useMemo(() => {
        const today = new Date();
        today.setHours(0, 0, 0, 0);

        const available = [];
        const locked = [];

        for (const inv of investments) {
            if (!inv.due_date) {
                available.push(inv);
                continue;
            }

            const due = new Date(inv.due_date);
            due.setHours(0, 0, 0, 0);

            if (due <= today) {
                available.push(inv);
            } else {
                locked.push(inv);
            }
        }

        return { available, locked };
    }, [investments]);

    return (
        <>
            <Logged />
            <BaseLayout title="Meus Investimentos" description="Cadastre, acompanhe e gerencie seus investimentos">
                <div className="px-4 lg:px-6 space-y-6">
                    <div className="flex items-center justify-between">
                        <h2 className="text-lg font-semibold tracking-tight">Carteira</h2>
                        <InvestmentsAdd setReload={setReload} />
                    </div>

                    {loadingInvestments ? (
                        <div className="space-y-4">
                            <div className="flex gap-2">
                                <Skeleton className="h-9 w-56 rounded-md" />
                                <Skeleton className="h-9 w-56 rounded-md" />
                            </div>
                            <InvestmentsTableSkeleton />
                        </div>
                    ) : (
                        <Tabs defaultValue="available" className="w-full">
                            <TabsList>
                                <TabsTrigger
                                    value="available"
                                    className="cursor-pointer text-green-700 dark:text-green-400 data-[state=active]:bg-green-100 data-[state=active]:text-green-800 dark:data-[state=active]:bg-green-900/30 dark:data-[state=active]:text-green-300"
                                >
                                    Disponíveis para resgate
                                    <Badge
                                        variant="outline"
                                        className="ml-1.5 border-green-200 bg-green-100 text-green-800 dark:border-green-800 dark:bg-green-900/40 dark:text-green-300"
                                    >
                                        {available.length}
                                    </Badge>
                                </TabsTrigger>
                                <TabsTrigger value="locked" className="cursor-pointer">
                                    Bloqueados até o vencimento
                                    <Badge variant="secondary" className="ml-1.5">{locked.length}</Badge>
                                </TabsTrigger>
                            </TabsList>
                            <TabsContent value="available">
                                <InvestmentsDataTable investments={available} setReload={setReload} onReinvest={handleReinvest} />
                            </TabsContent>
                            <TabsContent value="locked">
                                <InvestmentsDataTable investments={locked} setReload={setReload} onReinvest={handleReinvest} />
                            </TabsContent>
                        </Tabs>
                    )}
                </div>
            </BaseLayout>
            <InvestmentsAdd
                setReload={setReload}
                externalOpen={reinvestOpen}
                onExternalClose={() => {
                    setReinvestOpen(false);
                    setReinvestInitialValues(null);
                }}
                initialValues={reinvestInitialValues}
            />
        </>
    );
};

export default MyInvestments;
