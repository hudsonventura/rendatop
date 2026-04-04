import { useMemo } from "react";
import { useEffect, useState } from "react";
import axiosInstance from "@/utils/axiosConfig";

import { BaseLayout } from "@/components/layouts/base-layout";
import Logged from "@/components/Logged";
import InvestmentsAdd from "@/components/InvestmentsAdd";
import InvestmentsDataTable from "@/components/InvestmentsDataTable";
import RecurringInvestmentsManager from "@/components/RecurringInvestmentsManager";
import { Checkbox } from "@/components/ui/checkbox";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { getReinvestmentInitialValues } from "@/utils/investment-actions";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { getInvestmentTypeLabel } from "@/utils/investment-types";

function InvestmentsTableSkeleton() {
    return (
        <div className="space-y-4">
            <div className="overflow-hidden rounded-lg border">
                <div className="border-b bg-muted p-4">
                    <div className="grid grid-cols-7 gap-3">
                        {Array.from({ length: 7 }).map((_, i) => (
                            <Skeleton key={i} className="h-4 w-full" />
                        ))}
                    </div>
                </div>
                <div className="space-y-3 p-4">
                    {Array.from({ length: 6 }).map((_, i) => (
                        <div key={i} className="grid grid-cols-7 gap-3">
                            {Array.from({ length: 7 }).map((__, j) => (
                                <Skeleton key={j} className="h-5 w-full" />
                            ))}
                        </div>
                    ))}
                </div>
            </div>
        </div>
    );
}

const MyInvestments = () => {
    const ALL_BANKS = "__all_banks__";
    const ALL_TYPES = "__all_types__";
    const UNCATEGORIZED_TYPE = "__uncategorized__";
    const [investments, setInvestments] = useState([]);
    const [loadingInvestments, setLoadingInvestments] = useState(true);
    const [reload, setReload] = useState(0);
    const [reinvestOpen, setReinvestOpen] = useState(false);
    const [reinvestInitialValues, setReinvestInitialValues] = useState(null);
    const [showArchived, setShowArchived] = useState(false);
    const [activeTab, setActiveTab] = useState("available");
    const [selectedBank, setSelectedBank] = useState(ALL_BANKS);
    const [selectedType, setSelectedType] = useState(ALL_TYPES);

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

    const { available, locked } = useMemo(() => {
        const today = new Date();
        today.setHours(0, 0, 0, 0);

        const available = [];
        const locked = [];

        for (const inv of investments) {
            if (!showArchived && inv.archived) {
                continue;
            }

            if (!inv.due_date) {
                available.push(inv);
                continue;
            }

            const due = new Date(inv.due_date);
            due.setHours(0, 0, 0, 0);

            if (due <= today) {
                available.push(inv);
            } else {
                locked.push(inv);
            }
        }

        return { available, locked };
    }, [investments, showArchived]);

    const bankOptions = useMemo(() => {
        const map = new Map();

        for (const investment of investments) {
            const bankName = investment?.bank?.name;
            if (!bankName) continue;

            const key = String(bankName);
            if (!map.has(key)) {
                map.set(key, {
                    value: key,
                    label: key,
                });
            }
        }

        return Array.from(map.values()).sort((a, b) => a.label.localeCompare(b.label));
    }, [investments]);

    const typeOptions = useMemo(() => {
        const uniqueTypes = Array.from(new Set(
            investments
                .map((investment) => investment?.investment_type)
                .filter(Boolean)
        ));

        return [
            { value: UNCATEGORIZED_TYPE, label: "Não categorizado" },
            ...uniqueTypes
            .map((type) => ({
                value: type,
                label: getInvestmentTypeLabel(type) || type,
            }))
            .sort((a, b) => a.label.localeCompare(b.label)),
        ];
    }, [investments]);

    const filteredAvailable = useMemo(
        () => available.filter((investment) => {
            const matchesBank = selectedBank === ALL_BANKS || investment?.bank?.name === selectedBank;
            const matchesType =
                selectedType === ALL_TYPES ||
                (selectedType === UNCATEGORIZED_TYPE
                    ? !investment?.investment_type
                    : investment?.investment_type === selectedType);
            return matchesBank && matchesType;
        }),
        [available, selectedBank, selectedType]
    );

    const filteredLocked = useMemo(
        () => locked.filter((investment) => {
            const matchesBank = selectedBank === ALL_BANKS || investment?.bank?.name === selectedBank;
            const matchesType =
                selectedType === ALL_TYPES ||
                (selectedType === UNCATEGORIZED_TYPE
                    ? !investment?.investment_type
                    : investment?.investment_type === selectedType);
            return matchesBank && matchesType;
        }),
        [locked, selectedBank, selectedType]
    );

    return (
        <>
            <Logged />
            <BaseLayout title="Meus Investimentos" description="Cadastre, acompanhe e gerencie seus investimentos">
                <div className="px-4 lg:px-6 space-y-6">
                    <div className="flex items-center justify-between">
                        <h2 className="text-lg font-semibold tracking-tight">Carteira</h2>
                        {activeTab !== "recurring" && (
                            <InvestmentsAdd setReload={setReload} />
                        )}
                    </div>
                    {activeTab !== "recurring" && (
                        <label className="flex items-center gap-2 text-sm text-muted-foreground">
                            <Checkbox
                                checked={showArchived}
                                onCheckedChange={(checked) => setShowArchived(Boolean(checked))}
                            />
                            Mostrar investimentos arquivados junto com os ativos
                        </label>
                    )}

                    {loadingInvestments ? (
                        <div className="space-y-4">
                            <div className="flex gap-2">
                                <Skeleton className="h-9 w-56 rounded-md" />
                                <Skeleton className="h-9 w-56 rounded-md" />
                            </div>
                            <InvestmentsTableSkeleton />
                        </div>
                    ) : (
                        <Tabs value={activeTab} onValueChange={setActiveTab} className="w-full">
                            <div className="flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between">
                                <TabsList>
                                    <TabsTrigger
                                        value="available"
                                        className="cursor-pointer text-green-700 dark:text-green-400 data-[state=active]:bg-green-100 data-[state=active]:text-green-800 dark:data-[state=active]:bg-green-900/30 dark:data-[state=active]:text-green-300"
                                    >
                                        Disponíveis para resgate
                                        <Badge
                                            variant="outline"
                                            className="ml-1.5 border-green-200 bg-green-100 text-green-800 dark:border-green-800 dark:bg-green-900/40 dark:text-green-300"
                                        >
                                            {filteredAvailable.length}
                                        </Badge>
                                    </TabsTrigger>
                                    <TabsTrigger value="locked" className="cursor-pointer">
                                        Bloqueados até o vencimento
                                        <Badge variant="secondary" className="ml-1.5">{filteredLocked.length}</Badge>
                                    </TabsTrigger>
                                    <TabsTrigger value="recurring" className="cursor-pointer">
                                        Recorrentes
                                    </TabsTrigger>
                                </TabsList>

                                <div className="flex flex-col gap-2 sm:flex-row">
                                    <Select value={selectedBank} onValueChange={setSelectedBank}>
                                        <SelectTrigger className="w-full sm:w-52">
                                            <SelectValue placeholder="Filtrar por banco" />
                                        </SelectTrigger>
                                        <SelectContent>
                                            <SelectItem value={ALL_BANKS}>Todos os bancos</SelectItem>
                                            {bankOptions.map((bank) => (
                                                <SelectItem key={bank.value} value={bank.value}>
                                                    {bank.label}
                                                </SelectItem>
                                            ))}
                                        </SelectContent>
                                    </Select>

                                    <Select value={selectedType} onValueChange={setSelectedType}>
                                        <SelectTrigger className="w-full sm:w-48">
                                            <SelectValue placeholder="Filtrar por tipo" />
                                        </SelectTrigger>
                                        <SelectContent>
                                            <SelectItem value={ALL_TYPES}>Todos os tipos</SelectItem>
                                            {typeOptions.map((type) => (
                                                <SelectItem key={type.value} value={type.value}>
                                                    {type.label}
                                                </SelectItem>
                                            ))}
                                        </SelectContent>
                                    </Select>
                                </div>
                            </div>
                            <TabsContent value="available">
                                <InvestmentsDataTable investments={filteredAvailable} setReload={setReload} onReinvest={handleReinvest} />
                            </TabsContent>
                            <TabsContent value="locked">
                                <InvestmentsDataTable investments={filteredLocked} setReload={setReload} onReinvest={handleReinvest} />
                            </TabsContent>
                            <TabsContent value="recurring">
                                <RecurringInvestmentsManager />
                            </TabsContent>
                        </Tabs>
                    )}
                </div>
            </BaseLayout>
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

export default MyInvestments;
