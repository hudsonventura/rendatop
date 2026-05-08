import Logged from "@/components/Logged"
import RecurringInvestmentsManager from "@/components/RecurringInvestmentsManager"
import { BaseLayout } from "@/components/layouts/base-layout"

const RecurringInvestmentsPage = () => {
    return (
        <>
            <Logged />
            <BaseLayout
                title="Investimentos Recorrentes"
                description="Cadastre e acompanhe os investimentos gerados automaticamente."
            >
                <div className="px-4 lg:px-6">
                    <RecurringInvestmentsManager />
                </div>
            </BaseLayout>
        </>
    )
}

export default RecurringInvestmentsPage
