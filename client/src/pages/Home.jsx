import React, { useMemo } from 'react';
import { useState, useEffect } from "react";
import axiosInstance from "@/utils/axiosConfig";

import { BaseLayout } from "@/components/layouts/base-layout";
import InvestmentsTable from "@/components/InvestmentsTable";
import InvestmentsAdd from "@/components/InvestmentsAdd";
import InvestmentsResume from "@/components/InvestmentsResume";
import Logged from "@/components/Logged";
import BanksPieChart from "@/components/BanksPieChart";

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
    const { available, locked } = useMemo(() => {
        const today = new Date();
        today.setHours(0, 0, 0, 0);

        const available = [];
        const locked = [];

        for (const inv of investments) {
            if (!inv.date_expected_sell) {
                available.push(inv);
            } else {
                const due = new Date(inv.date_expected_sell);
                due.setHours(0, 0, 0, 0);
                if (due <= today) {
                    available.push(inv);
                } else {
                    locked.push(inv);
                }
            }
        }

        return { available, locked };
    }, [investments]);

    return (
        <>
            <Logged />
            <BaseLayout title="Investimentos" description="Acompanhe e gerencie seus investimentos de renda fixa">
                <div className="px-4 lg:px-6 space-y-6">
                    <BanksPieChart investments={investments.length > 0 ? investments : null} />
                    <InvestmentsResume investments={investments} />

                    <div className="flex items-center justify-between">
                        <h2 className="text-lg font-semibold tracking-tight">Meus Investimentos</h2>
                        <InvestmentsAdd setReload={setReload} />
                    </div>

                    {/* Available for redemption */}
                    {available.length > 0 && (
                        <div className="space-y-2">
                            <h3 className="text-md font-medium text-green-600 dark:text-green-400">
                                Disponíveis para resgate ({available.length})
                            </h3>
                            <InvestmentsTable investments={available} setReload={setReload} />
                        </div>
                    )}

                    {/* Locked investments */}
                    {locked.length > 0 && (
                        <div className="space-y-2">
                            <h3 className="text-md font-medium text-muted-foreground">
                                Bloqueados até o vencimento ({locked.length})
                            </h3>
                            <InvestmentsTable investments={locked} setReload={setReload} />
                        </div>
                    )}
                </div>
            </BaseLayout>
        </>
    );
};

export default Home;
