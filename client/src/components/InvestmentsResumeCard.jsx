import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"



export default function InvestmentsResumeCard({bank, value}) {


    return (
        <>
            <Card className="m-3 w-full max-w-xs shadow-xl ">
                <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                    {/* <CardTitle className="text-sm font-medium"></CardTitle> */}
                    {/* <ArrowUpIcon className={`h-4 w-4 "text-red-500 rotate-180"`} /> import { ArrowUpIcon } from 'lucide-react'*/}
                </CardHeader>
                <CardContent>
                    <div className="text-4xl font-bold">
                        {Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL', minimumFractionDigits: 2 }).format(value)}
                    </div>
                    <p className="text-xs text-muted-foreground mt-1">
                        {bank}
                    </p>
                </CardContent>
            </Card>
        </>
    );
}