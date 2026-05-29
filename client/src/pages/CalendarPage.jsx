import React from "react"
import { useState, useEffect } from "react"
import axiosInstance from "@/utils/axiosConfig"

import { BaseLayout } from "@/components/layouts/base-layout"
import InvestmentsCalendar from "@/components/InvestmentsCalendar"
import Logged from "@/components/Logged"
import { useWallet, walletParams } from "@/contexts/wallet-context"

const CalendarPage = () => {
    const [investments, setInvestments] = useState(null)
    const { activeWalletId } = useWallet()

    useEffect(() => {
        axiosInstance
            .get("/Investments", { params: walletParams(activeWalletId) })
            .then((response) => setInvestments(response.data))
            .catch((err) => console.error("Erro ao buscar investimentos:", err))
    }, [activeWalletId])

    return (
        <>
            <Logged />
            <BaseLayout title="Calendário" description="Visualize as datas de aplicação e vencimento dos seus investimentos">
                <div className="px-4 lg:px-6">
                    <InvestmentsCalendar investments={investments} />
                </div>
            </BaseLayout>
        </>
    )
}

export default CalendarPage
