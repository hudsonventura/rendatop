import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Landmark } from 'lucide-react'


export default function InvestmentsResumeCard({ bank, value }) {

    return (
        <Card className="shadow-sm hover:shadow-md transition-shadow duration-200">
            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                <CardTitle className="text-sm font-medium text-muted-foreground">{bank}</CardTitle>
                <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-primary/10">
                    <Landmark className="h-4 w-4 text-primary" />
                </div>
            </CardHeader>
            <CardContent>
                <div className="text-2xl font-bold tracking-tight">
                    {Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL', minimumFractionDigits: 2 }).format(value)}
                </div>
                <p className="text-xs text-muted-foreground mt-1">
                    Valor líquido atual
                </p>
            </CardContent>
        </Card>
    );
}