import { Card, CardContent } from "@/components/ui/card"
import { TrendingUp } from 'lucide-react'
import InvestmentsResumeCard from "./InvestmentsResumeCard"


export default function InvestmentsResume({ investments }) {

    return (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
            {
                Object.entries(
                    investments.reduce((acc, cur) => {
                        const bank = cur.bank;
                        if (!acc[bank]) acc[bank] = { bank, value: 0 };
                        acc[bank].value += cur.calculated[0].value_liq;
                        return acc;
                    }, {})
                ).map(([bank, { value }]) => (
                    <InvestmentsResumeCard key={bank} {...{ bank, value }} />
                ))
            }
        </div>
    );
}