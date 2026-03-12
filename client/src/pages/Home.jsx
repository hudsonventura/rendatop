import React, { useMemo } from 'react';
import { useState, useEffect } from "react";
import axiosInstance from "@/utils/axiosConfig";

import { BaseLayout } from "@/components/layouts/base-layout";
import InvestmentsDueSoon from "@/components/InvestmentsDueSoon";
import BanksPieChart from "@/components/BanksPieChart";
import Logged from "@/components/Logged";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";

function DueSoonTableSkeleton() {
    return (
        <div className="overflow-hidden rounded-lg border">
            <div className="border-b bg-muted p-4">
                <div className="grid grid-cols-4 gap-4">
                    <Skeleton className="h-4 w-24" />
                    <Skeleton className="h-4 w-16" />
                    <Skeleton className="h-4 w-24" />
                    <Skeleton className="h-4 w-12" />
                </div>
            </div>
            <div className="space-y-3 p-4">
                {Array.from({ length: 3 }).map((_, i) => (
                    <div key={i} className="grid grid-cols-4 gap-4">
                        <Skeleton className="h-5 w-full" />
                        <Skeleton className="h-5 w-20" />
                        <Skeleton className="h-5 w-24" />
                        <Skeleton className="h-5 w-20" />
                    </div>
                ))}
            </div>
        </div>
    );
}

const Home = () => {

    const [investments, setInvestments] = useState([]);
    const [loadingInvestments, setLoadingInvestments] = useState(true);

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
    }, []);

    const dueSoon = useMemo(() => {
        const today = new Date();
        today.setHours(0, 0, 0, 0);
        const dueSoonLimit = new Date(today);
        dueSoonLimit.setDate(dueSoonLimit.getDate() + 30);
        dueSoonLimit.setHours(23, 59, 59, 999);

        const dueSoon = [];

        for (const inv of investments) {
            if (!inv.due_date) continue;
            const due = new Date(inv.due_date);
            due.setHours(0, 0, 0, 0);

            if (due <= dueSoonLimit) {
                dueSoon.push(inv);
            }
        }

        dueSoon.sort((a, b) => new Date(a.due_date).getTime() - new Date(b.due_date).getTime());
        return dueSoon;
    }, [investments]);

    return (
        <>
            <Logged />
            <BaseLayout title="Dashboard" description="Veja a distribuição por banco e os vencimentos dos próximos 30 dias">
                <div className="px-4 lg:px-6 space-y-6">
                    <BanksPieChart investments={loadingInvestments ? null : investments} />

                    <div className="space-y-3">
                        <div className="flex items-center gap-2">
                            <h2 className="text-lg font-semibold tracking-tight">Vencimentos dos próximos 30 dias</h2>
                            {loadingInvestments ? (
                                <Skeleton className="h-5 w-8 rounded-full" />
                            ) : (
                                <Badge variant="secondary">{dueSoon.length}</Badge>
                            )}
                        </div>
                        {loadingInvestments ? (
                            <DueSoonTableSkeleton />
                        ) : (
                            <InvestmentsDueSoon investments={dueSoon} />
                        )}
                    </div>
                </div>
            </BaseLayout>
        </>
    );
};

export default Home;
