import React, { useMemo } from 'react';
import { useState, useEffect } from "react";
import axiosInstance from "@/utils/axiosConfig";

import { BaseLayout } from "@/components/layouts/base-layout";
import InvestmentsDataTable from "@/components/InvestmentsDataTable";
import InvestmentsAdd from "@/components/InvestmentsAdd";
import InvestmentsDueSoon from "@/components/InvestmentsDueSoon";
import BanksPieChart from "@/components/BanksPieChart";
import Logged from "@/components/Logged";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { Badge } from "@/components/ui/badge";

const Home = () => {

    const [investments, setInvestments] = useState([]);
    const [reload, setReload] = useState(0);
    useEffect(() => {
        axiosInstance
            .get("/Investments")
            .then((response) => {
                setInvestments(response.data);
            })
            .catch((err) => {
                console.error("Erro ao buscar investimentos:", err);
            });
    }, [reload]);

    // Split: available = no due date OR due date <= today; locked = future due date
    const { available, locked, dueSoon } = useMemo(() => {
        const today = new Date();
        today.setHours(0, 0, 0, 0);
        const dueSoonLimit = new Date(today);
        dueSoonLimit.setDate(dueSoonLimit.getDate() + 30);
        dueSoonLimit.setHours(23, 59, 59, 999);

        const available = [];
        const locked = [];
        const dueSoon = [];

        for (const inv of investments) {
            if (!inv.due_date) {
                available.push(inv);
            } else {
                const due = new Date(inv.due_date);
                due.setHours(0, 0, 0, 0);

                if (due > today && due <= dueSoonLimit) {
                    dueSoon.push(inv);
                }

                if (due <= today) {
                    available.push(inv);
                } else {
                    locked.push(inv);
                }
            }
        }

        dueSoon.sort((a, b) => new Date(a.due_date).getTime() - new Date(b.due_date).getTime());

        return { available, locked, dueSoon };
    }, [investments]);

    return (
        <>
            <Logged />
            <BaseLayout title="Investimentos" description="Acompanhe e gerencie seus investimentos de renda fixa">
                <div className="px-4 lg:px-6 space-y-6">
                    <BanksPieChart investments={investments} />

                    <div className="space-y-3">
                        <div className="flex items-center gap-2">
                            <h2 className="text-lg font-semibold tracking-tight">Vencimentos dos próximos 30 dias</h2>
                            <Badge variant="secondary">{dueSoon.length}</Badge>
                        </div>
                        <InvestmentsDueSoon investments={dueSoon} />
                    </div>
                    <hr />
                    <div className="flex items-center justify-between">
                        <h2 className="text-lg font-semibold tracking-tight">Meus Investimentos</h2>
                        <InvestmentsAdd setReload={setReload} />
                    </div>

                    <Tabs defaultValue="available" className="w-full">
                        <TabsList>
                            <TabsTrigger value="available" className="cursor-pointer">
                                Disponíveis para resgate
                                <Badge variant="secondary" className="ml-1.5">{available.length}</Badge>
                            </TabsTrigger>
                            <TabsTrigger value="locked" className="cursor-pointer">
                                Bloqueados até o vencimento
                                <Badge variant="secondary" className="ml-1.5">{locked.length}</Badge>
                            </TabsTrigger>
                        </TabsList>
                        <TabsContent value="available">
                            <InvestmentsDataTable investments={available} setReload={setReload} />
                        </TabsContent>
                        <TabsContent value="locked">
                            <InvestmentsDataTable investments={locked} setReload={setReload} />
                        </TabsContent>
                    </Tabs>
                </div>
            </BaseLayout>
        </>
    );
};

export default Home;
