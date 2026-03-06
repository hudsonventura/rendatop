import React from 'react';
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

                    <InvestmentsTable investments={investments} />
                </div>
            </BaseLayout>
        </>
    );
};

export default Home;
