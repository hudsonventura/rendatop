import { useMemo } from "react";
import { useEffect, useState } from "react";
import axiosInstance from "@/utils/axiosConfig";

import { BaseLayout } from "@/components/layouts/base-layout";
import Logged from "@/components/Logged";
import InvestmentsAdd from "@/components/InvestmentsAdd";
import InvestmentsDataTable from "@/components/InvestmentsDataTable";
import { UpgradePlanModal } from "@/components/upgrade-plan-modal";
import { Checkbox } from "@/components/ui/checkbox";
import { Skeleton } from "@/components/ui/skeleton";
import { getReinvestmentInitialValues } from "@/utils/investment-actions";
import { useWallet, walletParams } from "@/contexts/wallet-context";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { getInvestmentTypeLabel } from "@/utils/investment-types";
import { ALL_MONEY_BOXES, MONEY_BOX_UNCATEGORIZED } from "@/utils/money-boxes";
import { Input } from "@/components/ui/input";

function parseDateValue(dateValue) {
    if (!dateValue) return null;

    const match = String(dateValue).match(/^(\d{4})-(\d{2})-(\d{2})/);
    if (match) {
        const [, year, month, day] = match;
        return new Date(Number(year), Number(month) - 1, Number(day));
    }

    const parsed = new Date(dateValue);
    return Number.isNaN(parsed.getTime())
        ? null
        : new Date(parsed.getFullYear(), parsed.getMonth(), parsed.getDate());
}

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
    const ALL_INDEXES = "__all_indexes__";
    const ALL_REDEMPTION_STATUSES = "__all_redemption_statuses__";
    const [investments, setInvestments] = useState([]);
    const [investmentLimitOverview, setInvestmentLimitOverview] = useState(null);
    const [upgradePrompt, setUpgradePrompt] = useState({ open: false, message: "" });
    const [loadingInvestments, setLoadingInvestments] = useState(true);
    const [reload, setReload] = useState(0);
    const [reinvestOpen, setReinvestOpen] = useState(false);
    const [reinvestInitialValues, setReinvestInitialValues] = useState(null);
    const [showArchived, setShowArchived] = useState(false);
    const [selectedBank, setSelectedBank] = useState(ALL_BANKS);
    const [selectedType, setSelectedType] = useState(ALL_TYPES);
    const [selectedIndex, setSelectedIndex] = useState(ALL_INDEXES);
    const [selectedMoneyBox, setSelectedMoneyBox] = useState(ALL_MONEY_BOXES);
    const [searchText, setSearchText] = useState("");
    const { activeWalletId } = useWallet();
    const [selectedRedemptionStatus, setSelectedRedemptionStatus] = useState(ALL_REDEMPTION_STATUSES);

    useEffect(() => {
        let cancelled = false;
        setLoadingInvestments(true);

        Promise.all([
            axiosInstance.get("/Investments", { params: walletParams(activeWalletId) }),
            axiosInstance.get("/Investments/limits").catch(() => ({ data: null })),
        ])
            .then(([investmentsResponse, limitsResponse]) => {
                if (cancelled) return;
                const limits = limitsResponse.data ?? null;
                setInvestments(investmentsResponse.data ?? []);
                setInvestmentLimitOverview(limits);

                if (limits?.is_over_limit) {
                    setUpgradePrompt({
                        open: true,
                        message: limits.restriction_message || "Seu plano atual possui menos investimentos do que sua carteira cadastrada. Faça upgrade para liberar novos investimentos.",
                    });
                }
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
    }, [reload, activeWalletId]);

    const openInvestmentLimitPrompt = () => {
        if (!investmentLimitOverview?.is_over_limit) return;

        setUpgradePrompt({
            open: true,
            message: investmentLimitOverview.restriction_message || "Seu plano atual possui menos investimentos do que sua carteira cadastrada. Faça upgrade para liberar novos investimentos.",
        });
    };

    const handleInvestmentLimitChanged = (overview) => {
        setInvestmentLimitOverview(overview);
    };

    const handleReinvest = (investment) => {
        openInvestmentLimitPrompt();
        setReinvestInitialValues(getReinvestmentInitialValues(investment));
        setReinvestOpen(true);
    };

    const investmentsWithAvailability = useMemo(() => {
        const today = new Date();
        today.setHours(0, 0, 0, 0);

        return investments
            .filter((investment) => showArchived || !investment.archived)
            .map((investment) => {
                if (!investment.due_date) {
                    return {
                        ...investment,
                        redemption_status: "available",
                    };
                }

                const due = parseDateValue(investment.due_date);

                return {
                    ...investment,
                    redemption_status: due && due <= today ? "available" : "locked",
                };
            });
    }, [investments, showArchived]);

    const bankOptions = useMemo(() => {
        const map = new Map();

        for (const investment of investmentsWithAvailability) {
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
    }, [investmentsWithAvailability]);

    const typeOptions = useMemo(() => {
        const uniqueTypes = Array.from(new Set(
            investmentsWithAvailability
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
    }, [investmentsWithAvailability]);

    const indexOptions = useMemo(() => {
        const uniqueIndexes = Array.from(new Set(
            investmentsWithAvailability
                .map((investment) => investment?.index)
                .filter(Boolean)
        ));

        const getIndexLabel = (index) => {
            switch (index) {
                case "CDI":
                    return "CDI";
                case "IPCA_MAIS":
                    return "IPCA+";
                case "PERCENT_YEAR":
                    return "%a.a.";
                case "CDI_MAIS":
                    return "CDI + %a.a.";
                default:
                    return index;
            }
        };

        return uniqueIndexes
            .map((index) => ({
                value: index,
                label: getIndexLabel(index),
            }))
            .sort((a, b) => a.label.localeCompare(b.label));
    }, [investmentsWithAvailability]);

    const moneyBoxOptions = useMemo(() => {
        const map = new Map();

        for (const investment of investmentsWithAvailability) {
            if (!investment?.money_box?.id || !investment?.money_box?.name) continue;
            map.set(investment.money_box.id, {
                value: investment.money_box.id,
                label: investment.money_box.name,
            });
        }

        return [
            { value: MONEY_BOX_UNCATEGORIZED, label: "Sem cofrinho" },
            ...Array.from(map.values()).sort((a, b) => a.label.localeCompare(b.label)),
        ];
    }, [investmentsWithAvailability]);

    const filteredInvestments = useMemo(
        () => investmentsWithAvailability.filter((investment) => {
            const normalizedSearch = searchText.trim().toLocaleLowerCase("pt-BR");
            const matchesBank = selectedBank === ALL_BANKS || investment?.bank?.name === selectedBank;
            const matchesType =
                selectedType === ALL_TYPES ||
                (selectedType === UNCATEGORIZED_TYPE
                    ? !investment?.investment_type
                    : investment?.investment_type === selectedType);
            const matchesIndex = selectedIndex === ALL_INDEXES || investment?.index === selectedIndex;
            const matchesMoneyBox =
                selectedMoneyBox === ALL_MONEY_BOXES ||
                (selectedMoneyBox === MONEY_BOX_UNCATEGORIZED
                    ? !investment?.money_box?.id
                    : investment?.money_box?.id === selectedMoneyBox);
            const matchesRedemptionStatus =
                selectedRedemptionStatus === ALL_REDEMPTION_STATUSES ||
                investment.redemption_status === selectedRedemptionStatus;
            const matchesSearch =
                !normalizedSearch ||
                String(investment?.title ?? "")
                    .toLocaleLowerCase("pt-BR")
                    .includes(normalizedSearch);

            return matchesBank && matchesType && matchesIndex && matchesMoneyBox && matchesRedemptionStatus && matchesSearch;
        }),
        [
            investmentsWithAvailability,
            searchText,
            selectedBank,
            selectedType,
            selectedIndex,
            selectedMoneyBox,
            selectedRedemptionStatus,
        ]
    );

    return (
        <>
            <Logged />
            <BaseLayout title="Meus Investimentos" description="Cadastre, acompanhe e gerencie seus investimentos">
                <div className="px-4 lg:px-6 space-y-6">
                    <div className="flex items-center justify-between">
                        <h2 className="text-lg font-semibold tracking-tight">Carteira</h2>
                        <InvestmentsAdd
                            setReload={setReload}
                            investmentLimitOverview={investmentLimitOverview}
                            onInvestmentLimitChanged={handleInvestmentLimitChanged}
                        />
                    </div>
                    <label className="flex items-center gap-2 text-sm text-muted-foreground">
                        <Checkbox
                            checked={showArchived}
                            onCheckedChange={(checked) => setShowArchived(Boolean(checked))}
                        />
                        Mostrar também os investimentos arquivados
                    </label>

                    <div className="max-w-md">
                        <Input
                            value={searchText}
                            onChange={(event) => setSearchText(event.target.value)}
                            placeholder="Buscar por título do investimento"
                        />
                    </div>

                    {loadingInvestments ? (
                        <div className="space-y-4">
                            <div className="flex gap-2">
                                <Skeleton className="h-9 w-56 rounded-md" />
                                <Skeleton className="h-9 w-56 rounded-md" />
                            </div>
                            <InvestmentsTableSkeleton />
                        </div>
                    ) : (
                        <div className="w-full space-y-4">
                            <div className="flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between">
                                <div className="text-sm text-muted-foreground">
                                    {filteredInvestments.length} investimento(s) encontrado(s)
                                </div>

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

                                    <Select value={selectedIndex} onValueChange={setSelectedIndex}>
                                        <SelectTrigger className="w-full sm:w-44">
                                            <SelectValue placeholder="Filtrar por indexador" />
                                        </SelectTrigger>
                                        <SelectContent>
                                            <SelectItem value={ALL_INDEXES}>Todos os indexadores</SelectItem>
                                            {indexOptions.map((index) => (
                                                <SelectItem key={index.value} value={index.value}>
                                                    {index.label}
                                                </SelectItem>
                                            ))}
                                        </SelectContent>
                                    </Select>

                                    <Select value={selectedMoneyBox} onValueChange={setSelectedMoneyBox}>
                                        <SelectTrigger className="w-full sm:w-48">
                                            <SelectValue placeholder="Filtrar por cofrinho" />
                                        </SelectTrigger>
                                        <SelectContent>
                                            <SelectItem value={ALL_MONEY_BOXES}>Todos os cofrinhos</SelectItem>
                                            {moneyBoxOptions.map((moneyBox) => (
                                                <SelectItem key={moneyBox.value} value={moneyBox.value}>
                                                    {moneyBox.label}
                                                </SelectItem>
                                            ))}
                                        </SelectContent>
                                    </Select>

                                    <Select value={selectedRedemptionStatus} onValueChange={setSelectedRedemptionStatus}>
                                        <SelectTrigger className="w-full sm:w-52">
                                            <SelectValue placeholder="Filtrar por resgate" />
                                        </SelectTrigger>
                                        <SelectContent>
                                            <SelectItem value={ALL_REDEMPTION_STATUSES}>Todos os status</SelectItem>
                                            <SelectItem value="available">Disponíveis para resgate</SelectItem>
                                            <SelectItem value="locked">Bloqueados até o vencimento</SelectItem>
                                        </SelectContent>
                                    </Select>
                                </div>
                            </div>

                            <InvestmentsDataTable
                                investments={filteredInvestments}
                                setReload={setReload}
                                onReinvest={handleReinvest}
                                onInvestmentModalOpen={openInvestmentLimitPrompt}
                            />
                        </div>
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
                investmentLimitOverview={investmentLimitOverview}
                onInvestmentLimitChanged={handleInvestmentLimitChanged}
            />
            <UpgradePlanModal
                open={upgradePrompt.open}
                onOpenChange={(open) => setUpgradePrompt((current) => ({ ...current, open }))}
                message={upgradePrompt.message}
            />
        </>
    );
};

export default MyInvestments;
