import React from "react"
import { useState, useEffect } from "react"
import axiosInstance from "@/utils/axiosConfig"

import { BaseLayout } from "@/components/layouts/base-layout"
import InvestmentsCalendar from "@/components/InvestmentsCalendar"
import Logged from "@/components/Logged"

const CalendarPage = () => {
    const [investments, setInvestments] = useState(null)

    useEffect(() => {
        axiosInstance
            .get("/Investments")
            .then((response) => setInvestments(response.data))
            .catch((err) => console.error("Erro ao buscar investimentos:", err))
    }, [])

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
