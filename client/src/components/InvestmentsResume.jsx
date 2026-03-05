import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { ArrowUpIcon } from 'lucide-react'
import InvestmentsResumeCard from "./InvestmentsResumeCard"


export default function InvestmentsResume({investments}) {


    return (
        <div className="flex flex-wrap">
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