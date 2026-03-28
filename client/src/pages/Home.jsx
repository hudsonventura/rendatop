import { useMemo } from 'react';
import { useState, useEffect } from "react";
import axiosInstance from "@/utils/axiosConfig";

import { BaseLayout } from "@/components/layouts/base-layout";
import InvestmentsAdd from "@/components/InvestmentsAdd";
import InvestmentsDueSoon from "@/components/InvestmentsDueSoon";
import BanksPieChart from "@/components/BanksPieChart";
import PortfolioTimelineChart from "@/components/PortfolioTimelineChart";
import Logged from "@/components/Logged";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import {
    Dialog,
    DialogContent,
    DialogDescription,
    DialogFooter,
    DialogHeader,
    DialogTitle,
} from "@/components/ui/dialog";
import { Skeleton } from "@/components/ui/skeleton";
import { Archive } from "lucide-react";
import { getReinvestmentInitialValues } from "@/utils/investment-actions";

function DueSoonTableSkeleton() {
    return (
        <div className="overflow-hidden rounded-lg border">
            <div className="border-b bg-muted p-4">
                <div className="grid grid-cols-4 gap-4">
                    <Skeleton className="h-4 w-24" />
                    <Skeleton className="h-4 w-16" />
                    <Skeleton className="h-4 w-24" />
                    <Skeleton className="h-4 w-12" />
                </div>
            </div>
            <div className="space-y-3 p-4">
                {Array.from({ length: 3 }).map((_, i) => (
                    <div key={i} className="grid grid-cols-4 gap-4">
                        <Skeleton className="h-5 w-full" />
                        <Skeleton className="h-5 w-20" />
                        <Skeleton className="h-5 w-24" />
                        <Skeleton className="h-5 w-20" />
                    </div>
                ))}
            </div>
        </div>
    );
}

const Home = () => {

    const [investments, setInvestments] = useState([]);
    const [loadingInvestments, setLoadingInvestments] = useState(true);
    const [reload, setReload] = useState(0);
    const [reinvestOpen, setReinvestOpen] = useState(false);
    const [reinvestInitialValues, setReinvestInitialValues] = useState(null);
    const [archiveOpen, setArchiveOpen] = useState(false);
    const [selectedInvestment, setSelectedInvestment] = useState(null);

    useEffect(() => {
        let cancelled = false;
        setLoadingInvestments(true);

        axiosInstance
            .get("/Investments")
            .then((response) => {
                if (cancelled) return;
                setInvestments(response.data ?? []);
            })
            .catch((err) => {
                console.error("Erro ao buscar investimentos:", err);
                if (cancelled) return;
                setInvestments([]);
            })
            .finally(() => {
                if (cancelled) return;
                setLoadingInvestments(false);
            });

        return () => {
            cancelled = true;
        };
    }, [reload]);

    const handleReinvest = (investment) => {
        setReinvestInitialValues(getReinvestmentInitialValues(investment));
        setReinvestOpen(true);
    };

    const handleOpenArchive = (investment) => {
        setSelectedInvestment(investment);
        setArchiveOpen(true);
    };

    const handleArchive = () => {
        if (!selectedInvestment) return;

        axiosInstance
            .patch(`/Investments/${selectedInvestment.id}/archive`, {
                archived: !selectedInvestment.archived,
            })
            .then(() => {
                setArchiveOpen(false);
                setSelectedInvestment(null);
                setReload(Math.floor(Math.random() * 10000) + 1);
            })
            .catch((err) => console.error("Erro ao arquivar investimento:", err));
    };

    const dueSoon = useMemo(() => {
        const today = new Date();
        today.setHours(0, 0, 0, 0);
        const dueSoonLimit = new Date(today);
        dueSoonLimit.setDate(dueSoonLimit.getDate() + 30);
        dueSoonLimit.setHours(23, 59, 59, 999);

        const dueSoon = [];

        for (const inv of investments) {
            if (inv.archived) continue;
            if (!inv.due_date) continue;
            const due = new Date(inv.due_date);
            due.setHours(0, 0, 0, 0);

            if (due <= dueSoonLimit) {
                dueSoon.push(inv);
            }
        }

        dueSoon.sort((a, b) => new Date(a.due_date).getTime() - new Date(b.due_date).getTime());
        return dueSoon;
    }, [investments]);

    return (
        <>
            <Logged />
            <BaseLayout title="Dashboard" description="Veja a distribuição por banco e os vencimentos dos próximos 30 dias">
                <div className="px-4 lg:px-6 space-y-6">
                    <div className="space-y-3">
                        <div className="flex items-center gap-2">
                            <h2 className="text-lg font-semibold tracking-tight">Vencimentos dos próximos 30 dias</h2>
                            {loadingInvestments ? (
                                <Skeleton className="h-5 w-8 rounded-full" />
                            ) : (
                                <Badge variant="secondary">{dueSoon.length}</Badge>
                            )}
                        </div>
                        {loadingInvestments ? (
                            <DueSoonTableSkeleton />
                        ) : (
                            <InvestmentsDueSoon
                                investments={dueSoon}
                                onArchive={handleOpenArchive}
                                onReinvest={handleReinvest}
                            />
                        )}
                    </div>
                </div>
                <BanksPieChart investments={loadingInvestments ? null : investments} />
                <PortfolioTimelineChart investments={loadingInvestments ? null : investments} />
            </BaseLayout>
            <Dialog
                open={archiveOpen}
                onOpenChange={(open) => {
                    setArchiveOpen(open);
                    if (!open) setSelectedInvestment(null);
                }}
            >
                <DialogContent className="sm:max-w-sm">
                    <DialogHeader>
                        <DialogTitle className="flex items-center gap-2">
                            <Archive className="h-5 w-5" />
                            Arquivar investimento
                        </DialogTitle>
                        <DialogDescription>
                            Deseja arquivar <strong>{selectedInvestment?.title}</strong>? Ele deixará de aparecer em Meus Investimentos por padrão.
                        </DialogDescription>
                    </DialogHeader>
                    <DialogFooter className="flex gap-2 sm:gap-2">
                        <Button variant="outline" className="flex-1" onClick={() => setArchiveOpen(false)}>
                            Não
                        </Button>
                        <Button className="flex-1" onClick={handleArchive}>
                            Sim, arquivar
                        </Button>
                    </DialogFooter>
                </DialogContent>
            </Dialog>
            <InvestmentsAdd
                setReload={setReload}
                externalOpen={reinvestOpen}
                onExternalClose={() => {
                    setReinvestOpen(false);
                    setReinvestInitialValues(null);
                }}
                initialValues={reinvestInitialValues}
            />
        </>
    );
};

export default Home;
