import React, { useState, useEffect, useCallback } from 'react';
import { Card, CardHeader, CardTitle, CardDescription, CardContent, CardFooter } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Separator } from "@/components/ui/separator";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription, DialogFooter } from "@/components/ui/dialog";
import { Skeleton } from "@/components/ui/skeleton";
import { Check, CreditCard, QrCode, Barcode, Loader2, Crown, Sparkles, Copy, X, ExternalLink } from "lucide-react";
import { BaseLayout } from "@/components/layouts/base-layout";
import Logged from "@/components/Logged";
import axiosInstance from "@/utils/axiosConfig";
import { formatCpf, isValidCpf, sanitizeCpf } from "@/utils/cpf";

function splitFullName(fullName) {
    const cleaned = (fullName || "").trim().replace(/\s+/g, " ");
    if (!cleaned) return { firstName: "", lastName: "" };

    const parts = cleaned.split(" ");
    if (parts.length === 1) {
        return { firstName: parts[0], lastName: parts[0] };
    }

    return {
        firstName: parts[0],
        lastName: parts.slice(1).join(" ")
    };
}

function joinErrorParts(parts) {
    return parts
        .map((part) => (typeof part === "string" ? part.trim() : ""))
        .filter(Boolean)
        .join(" | ");
}

function extractErrorMessage(error, fallbackMessage) {
    if (!error) return fallbackMessage;

    const candidates = [
        error?.response?.data,
        error?.response?.data?.Message,
        error?.response?.data?.message,
        error?.response?.data?.error,
        error?.message
    ];

    for (const candidate of candidates) {
        if (typeof candidate === "string" && candidate.trim()) {
            return candidate.trim();
        }
    }

    const responseData = error?.response?.data;
    if (responseData && typeof responseData === "object") {
        const fromCauses = Array.isArray(responseData.cause)
            ? responseData.cause
                .map((cause) => joinErrorParts([
                    cause?.code ? `code=${cause.code}` : "",
                    cause?.description,
                    cause?.message,
                    cause?.details
                ]))
                .filter(Boolean)
            : [];

        const nestedMessage = joinErrorParts([
            responseData.title,
            responseData.detail,
            responseData.error,
            responseData.message,
            responseData.Message,
            fromCauses.length > 0 ? `causas: ${fromCauses.join(" | ")}` : ""
        ]);

        if (nestedMessage) {
            return nestedMessage;
        }
    }

    if (Array.isArray(error?.cause)) {
        const sdkCauseMessage = error.cause
            .map((cause) => joinErrorParts([
                cause?.code ? `code=${cause.code}` : "",
                cause?.description,
                cause?.message,
                cause?.details
            ]))
            .filter(Boolean)
            .join(" | ");

        if (sdkCauseMessage) {
            return sdkCauseMessage;
        }
    }

    return fallbackMessage;
}

const PAYMENT_REQUEST_TIMEOUT_MS = 60000;
const PAYMENT_REQUEST_CONFIG = { timeout: PAYMENT_REQUEST_TIMEOUT_MS };
const PAYMENT_PROCESSING_MESSAGE = 'O processamento do pagamento pode demorar um pouco. Aguarde enquanto confirmamos a cobrança.';
const PAYMENT_TIMEOUT_MESSAGE = 'O processamento está demorando um pouco mais do que o previsto, mas, assim que confirmado, o sistema liberará o funcionamento do plano.';
const PAYMENT_TIMEOUT_FOLLOW_UP_MESSAGE = 'Você pode fechar esta janela e acompanhar o status na tela de assinatura.';

function isTimeoutError(error) {
    const message = String(error?.message || '');
    return error?.code === 'ECONNABORTED' || message.toLowerCase().includes('timeout');
}

function getPlanSyncState(overview, planId) {
    return {
        isActive: overview?.active_subscription?.plan_id === planId,
        isPending: overview?.pending_subscription?.plan_id === planId
    };
}

const SubscriptionPage = () => {
    const [plans, setPlans] = useState([]);
    const [activeSub, setActiveSub] = useState(null);
    const [pendingSub, setPendingSub] = useState(null);
    const [pendingCharge, setPendingCharge] = useState(null);
    const [payerFullName, setPayerFullName] = useState("");
    const [payerCpf, setPayerCpf] = useState("");
    const [loading, setLoading] = useState(true);
    const [selectedPlan, setSelectedPlan] = useState(null);
    const [paymentDialogOpen, setPaymentDialogOpen] = useState(false);
    const [pendingCancelDialogOpen, setPendingCancelDialogOpen] = useState(false);
    const [cancelDialogOpen, setCancelDialogOpen] = useState(false);
    const [cancelLoading, setCancelLoading] = useState(false);
    const [cancelError, setCancelError] = useState('');
    const [revertCancelDialogOpen, setRevertCancelDialogOpen] = useState(false);
    const [revertCancelLoading, setRevertCancelLoading] = useState(false);
    const [revertCancelError, setRevertCancelError] = useState('');
    const [cancelNotice, setCancelNotice] = useState('');

    const fetchData = useCallback(async () => {
        setLoading(true);
        try {
            const [plansRes, subRes, settingsRes] = await Promise.all([
                axiosInstance.get('/plans'),
                axiosInstance.get('/subscription/overview').catch(() => ({ data: null })),
                axiosInstance.get('/User/Settings').catch(() => ({ data: null }))
            ]);
            const overviewData = subRes.data || null;
            const settingsData = settingsRes?.data || null;
            setPlans(plansRes.data);
            setActiveSub(overviewData?.active_subscription || null);
            setPendingSub(overviewData?.pending_subscription || null);
            setPendingCharge(overviewData?.pending_charge || null);
            setPayerFullName(settingsData?.name || sessionStorage.getItem('name') || "");
            setPayerCpf(settingsData?.cpf || "");
            return overviewData;
        } catch (err) {
            console.error(err);
            return null;
        } finally {
            setLoading(false);
        }
    }, []);

    useEffect(() => { fetchData(); }, [fetchData]);

    const handleSelectPlan = (plan) => {
        if (plan.price <= 0) return;
        if (activeSub?.plan_id === plan.id) return;
        if (pendingSub?.plan_id === plan.id) return;
        setSelectedPlan(plan);
        setPaymentDialogOpen(true);
    };

    const handleCancelSubscription = async () => {
        setCancelError('');
        setCancelDialogOpen(true);
    };

    const handleConfirmCancellation = async (mode) => {
        try {
            setCancelLoading(true);
            setCancelError('');
            const result = await axiosInstance.post('/subscription/cancel-active', {
                confirm: true,
                mode
            });
            setCancelNotice(result?.data?.message || '');
            setCancelDialogOpen(false);
            await fetchData();
        } catch (err) {
            setCancelError(extractErrorMessage(err, 'Erro ao cancelar assinatura.'));
        } finally {
            setCancelLoading(false);
        }
    };

    const handleRequestPendingCancel = () => {
        setPendingCancelDialogOpen(true);
    };

    const handleConfirmPendingCancel = async () => {
        try {
            await axiosInstance.post('/subscription/cancel-pending');
            setPendingCancelDialogOpen(false);
            await fetchData();
        } catch (err) {
            console.error(err);
        }
    };

    const handleRequestRevertCancellation = () => {
        setRevertCancelError('');
        setRevertCancelDialogOpen(true);
    };

    const handleConfirmRevertCancellation = async () => {
        try {
            setRevertCancelLoading(true);
            setRevertCancelError('');
            const result = await axiosInstance.post('/subscription/cancel-scheduled/revert', {
                confirm: true
            });
            setCancelNotice(result?.data?.message || '');
            setRevertCancelDialogOpen(false);
            await fetchData();
        } catch (err) {
            setRevertCancelError(extractErrorMessage(err, 'Erro ao reverter o cancelamento agendado.'));
        } finally {
            setRevertCancelLoading(false);
        }
    };

    const currentPlanId = activeSub?.plan_id || 'free';
    const pendingPlanId = pendingSub?.plan_id || null;
    const isCardSubscription = activeSub?.payment_method?.includes('card');

    const planIcons = { free: null, plus: Sparkles, pro: Crown };

    return (
        <>
            <Logged />
            <BaseLayout title="Assinatura" description="Escolha o plano ideal para você">
                <div className="px-4 lg:px-6">
                    <div className="mx-auto w-full max-w-5xl">
                        {loading ? (
                            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                                {[1, 2, 3].map(i => (
                                    <Skeleton key={i} className="h-80 rounded-xl" />
                                ))}
                            </div>
                        ) : (
                            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                                {plans.map((plan) => {
                                    const isActive = currentPlanId === plan.id;
                                    const isPending = pendingPlanId === plan.id;
                                    const PlanIcon = planIcons[plan.id];
                                    const isPopular = plan.id === 'plus';
                                    const cardBadge = isPending
                                        ? (
                                            <Badge variant="outline" className="text-xs border-amber-500 text-amber-600 bg-background">
                                                Pagamento pendente
                                            </Badge>
                                        )
                                        : isActive
                                            ? (
                                                <Badge variant="outline" className="text-xs border-primary text-primary bg-background">
                                                    <Check className="h-3 w-3 mr-1" /> Atual
                                                </Badge>
                                            )
                                            : isPopular
                                                ? <Badge className="text-xs bg-background text-foreground border border-border">Mais popular</Badge>
                                                : null;

                                    return (
                                        <Card
                                            key={plan.id}
                                            className={`relative flex flex-col transition-all ${
                                                isActive
                                                    ? 'border-primary ring-1 ring-primary/20'
                                                    : isPending
                                                        ? 'border-amber-500/60 ring-1 ring-amber-500/20'
                                                    : 'hover:border-primary/40'
                                            }`}
                                        >
                                            {cardBadge && (
                                                <div className="absolute -top-3 left-1/2 -translate-x-1/2 z-20 px-1 bg-background rounded-md">
                                                    {cardBadge}
                                                </div>
                                            )}

                                            <CardHeader className="pb-4 pt-6">
                                                <div className="flex items-center gap-2">
                                                    {PlanIcon && (
                                                        <PlanIcon
                                                            className={`h-5 w-5 ${plan.id === "pro" ? "text-yellow-400" : "text-primary"}`}
                                                        />
                                                    )}
                                                    <CardTitle className="text-lg">{plan.name}</CardTitle>
                                                </div>
                                                <div className="mt-3">
                                                    {plan.price > 0 ? (
                                                        <div className="flex items-baseline gap-1">
                                                            <span className="text-3xl font-bold">
                                                                R${plan.price.toFixed(2).replace('.', ',')}
                                                            </span>
                                                            <span className="text-muted-foreground text-sm">/mês</span>
                                                        </div>
                                                    ) : (
                                                        <span className="text-3xl font-bold">Grátis</span>
                                                    )}
                                                </div>
                                            </CardHeader>

                                            <CardContent className="flex-1">
                                                <Separator className="mb-4" />
                                                <ul className="space-y-2.5">
                                                    {Object.entries(plan.features).map(([key, text]) => (
                                                        <li key={key} className="flex items-start gap-2 text-sm">
                                                            <Check className="h-4 w-4 text-primary mt-0.5 shrink-0" />
                                                            <span className="text-muted-foreground">{text}</span>
                                                        </li>
                                                    ))}
                                                </ul>
                                            </CardContent>

                                            <CardFooter className="pt-0">
                                                {isActive ? (
                                                    plan.price > 0 ? (
                                                        <Button
                                                            variant="outline"
                                                            className="w-full"
                                                            disabled={Boolean(activeSub?.cancel_at_period_end)}
                                                            onClick={handleCancelSubscription}
                                                        >
                                                            {activeSub?.cancel_at_period_end ? 'Cancelamento agendado' : 'Cancelar assinatura'}
                                                        </Button>
                                                    ) : (
                                                        <Button variant="outline" className="w-full" disabled>
                                                            Plano atual
                                                        </Button>
                                                    )
                                                ) : isPending ? (
                                                    <Button
                                                        variant="outline"
                                                        className="w-full"
                                                        onClick={handleRequestPendingCancel}
                                                    >
                                                        Cancelar pendência
                                                    </Button>
                                                ) : (
                                                    plan.price > 0 ? (
                                                        <Button
                                                            className="w-full"
                                                            onClick={() => handleSelectPlan(plan)}
                                                        >
                                                            Assinar {plan.name}
                                                        </Button>
                                                    ) : (
                                                        <Button variant="outline" className="w-full" disabled>
                                                            Plano básico
                                                        </Button>
                                                    )
                                                )}
                                            </CardFooter>
                                        </Card>
                                    );
                                })}
                            </div>
                        )}

                        {activeSub && activeSub.plan_id !== 'free' && (
                            <Card className="mt-6">
                                <CardContent className="pt-6">
                                    <div className="flex items-center justify-between flex-wrap gap-4">
                                        <div>
                                            <p className="text-sm text-muted-foreground">Assinatura ativa</p>
                                            <p className="font-medium">{activeSub.plan?.name || activeSub.plan_id}</p>
                                        </div>
                                        <div>
                                            <p className="text-sm text-muted-foreground">Método de pagamento</p>
                                            <p className="font-medium capitalize">{activeSub.payment_method?.replace('_', ' ')}</p>
                                        </div>
                                        <div>
                                            <p className="text-sm text-muted-foreground">Próximo vencimento</p>
                                            <p className="font-medium">
                                                {new Date(activeSub.current_period_end).toLocaleDateString('pt-BR')}
                                            </p>
                                        </div>
                                    </div>

                                    {activeSub.cancel_at_period_end && (
                                        <div className="mt-4 rounded-lg border border-amber-500/40 bg-amber-50 px-4 py-3 text-sm text-amber-900">
                                            <div className="flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
                                                <span>
                                                    Seu cancelamento já está programado para o fim do período atual. Nenhuma nova cobrança será enviada.
                                                </span>
                                                <Button
                                                    variant="outline"
                                                    size="sm"
                                                    className="border-amber-500/40 bg-white text-amber-900 hover:bg-amber-100"
                                                    onClick={handleRequestRevertCancellation}
                                                >
                                                    Cancelar solicitação
                                                </Button>
                                            </div>
                                        </div>
                                    )}

                                    {!activeSub.cancel_at_period_end && cancelNotice && (
                                        <div className="mt-4 rounded-lg border border-primary/20 bg-primary/5 px-4 py-3 text-sm text-foreground">
                                            {cancelNotice}
                                        </div>
                                    )}
                                </CardContent>
                            </Card>
                        )}

                        {pendingSub && pendingSub.plan_id !== 'free' && (
                            <Card className="mt-6 border-amber-500/40">
                                <CardContent className="pt-6">
                                    <div className="flex items-center justify-between flex-wrap gap-4">
                                        <div>
                                            <p className="text-sm text-muted-foreground">Assinatura a ativar</p>
                                            <p className="font-medium">{pendingSub.plan?.name || pendingSub.plan_id}</p>
                                        </div>
                                        <div>
                                            <p className="text-sm text-muted-foreground">Método de pagamento</p>
                                            <p className="font-medium capitalize">{pendingSub.payment_method?.replace('_', ' ')}</p>
                                        </div>
                                        <div>
                                            <p className="text-sm text-muted-foreground">Status</p>
                                            <p className="font-medium">Aguardando compensação bancária</p>
                                        </div>
                                    </div>
                                </CardContent>
                            </Card>
                        )}

                        {pendingCharge && (
                            <PendingChargeCard
                                charge={pendingCharge}
                                planName={pendingSub?.plan?.name || pendingSub?.plan_id || pendingCharge.plan_id}
                                onRefresh={fetchData}
                            />
                        )}
                    </div>
                </div>
            </BaseLayout>

            <PaymentDialog
                open={paymentDialogOpen}
                onOpenChange={setPaymentDialogOpen}
                plan={selectedPlan}
                onSuccess={fetchData}
                payerFullName={payerFullName}
                payerCpf={payerCpf}
            />

            <Dialog open={pendingCancelDialogOpen} onOpenChange={setPendingCancelDialogOpen}>
                <DialogContent className="sm:max-w-md">
                    <DialogHeader>
                        <DialogTitle>Cancelar pendência?</DialogTitle>
                        <DialogDescription>
                            Se você continuar, esta pendência será cancelada. Mesmo que o boleto e/ou o PIX
                            seja compensado depois disso, o seu plano não entrará em vigor.
                        </DialogDescription>
                    </DialogHeader>
                    <DialogFooter className="flex gap-2 sm:gap-2">
                        <Button
                            variant="outline"
                            onClick={() => setPendingCancelDialogOpen(false)}
                        >
                            Não
                        </Button>
                        <Button
                            variant="destructive"
                            onClick={handleConfirmPendingCancel}
                        >
                            Sim, cancelar
                        </Button>
                    </DialogFooter>
                </DialogContent>
            </Dialog>

            <Dialog
                open={cancelDialogOpen}
                onOpenChange={(open) => {
                    if (!cancelLoading) {
                        setCancelDialogOpen(open);
                        if (!open) setCancelError('');
                    }
                }}
            >
                <DialogContent className="sm:max-w-md">
                    <DialogHeader>
                        <DialogTitle>Cancelar assinatura?</DialogTitle>
                        <DialogDescription>
                            {isCardSubscription
                                ? 'Escolha como deseja encerrar sua assinatura paga com cartão.'
                                : 'Como o pagamento foi feito via PIX ou boleto, o cancelamento só pode acontecer ao final do período atual.'}
                        </DialogDescription>
                    </DialogHeader>

                    {isCardSubscription ? (
                        <div className="space-y-3 text-sm text-muted-foreground">
                            <p>
                                Se você escolher receber o valor proporcional, a assinatura será encerrada agora e o sistema solicitará um estorno proporcional do período restante.
                            </p>
                            <p>
                                Se preferir permanecer ativo até o fim do período, vamos apenas programar o cancelamento e não enviaremos novas cobranças.
                            </p>
                        </div>
                    ) : (
                        <div className="space-y-3 text-sm text-muted-foreground">
                            <p>
                                Vamos programar o cancelamento para o final do período já pago e nenhuma cobrança futura será enviada.
                            </p>
                            <p>
                                Até lá, sua assinatura continuará ativa normalmente.
                            </p>
                        </div>
                    )}

                    {cancelError && (
                        <div className="rounded-lg bg-destructive/10 p-3 text-sm text-destructive">
                            {cancelError}
                        </div>
                    )}

                    <DialogFooter className="flex gap-2 sm:gap-2">
                        <Button
                            variant="outline"
                            onClick={() => setCancelDialogOpen(false)}
                            disabled={cancelLoading}
                        >
                            Não
                        </Button>

                        {isCardSubscription ? (
                            <>
                                <Button
                                    variant="outline"
                                    onClick={() => handleConfirmCancellation('end_of_period')}
                                    disabled={cancelLoading}
                                >
                                    {cancelLoading ? 'Processando...' : 'Manter até o fim'}
                                </Button>
                                <Button
                                    variant="destructive"
                                    onClick={() => handleConfirmCancellation('refund_prorated')}
                                    disabled={cancelLoading}
                                >
                                    {cancelLoading ? 'Processando...' : 'Receber proporcional'}
                                </Button>
                            </>
                        ) : (
                            <Button
                                variant="destructive"
                                onClick={() => handleConfirmCancellation('end_of_period')}
                                disabled={cancelLoading}
                            >
                                {cancelLoading ? 'Processando...' : 'Sim, programar cancelamento'}
                            </Button>
                        )}
                    </DialogFooter>
                </DialogContent>
            </Dialog>

            <Dialog
                open={revertCancelDialogOpen}
                onOpenChange={(open) => {
                    if (!revertCancelLoading) {
                        setRevertCancelDialogOpen(open);
                        if (!open) setRevertCancelError('');
                    }
                }}
            >
                <DialogContent className="sm:max-w-md">
                    <DialogHeader>
                        <DialogTitle>Cancelar a solicitação de cancelamento?</DialogTitle>
                        <DialogDescription>
                            Se você continuar, a programação de cancelamento será revertida e a sua assinatura poderá renovar normalmente.
                        </DialogDescription>
                    </DialogHeader>

                    {revertCancelError && (
                        <div className="rounded-lg bg-destructive/10 p-3 text-sm text-destructive">
                            {revertCancelError}
                        </div>
                    )}

                    <DialogFooter className="flex gap-2 sm:gap-2">
                        <Button
                            variant="outline"
                            onClick={() => setRevertCancelDialogOpen(false)}
                            disabled={revertCancelLoading}
                        >
                            Não
                        </Button>
                        <Button
                            onClick={handleConfirmRevertCancellation}
                            disabled={revertCancelLoading}
                        >
                            {revertCancelLoading ? 'Processando...' : 'Sim'}
                        </Button>
                    </DialogFooter>
                </DialogContent>
            </Dialog>
        </>
    );
};

const formatDateTime = (value) => {
    if (!value) return "—";
    return new Date(value).toLocaleString('pt-BR');
};

const PendingChargeCard = ({ charge, planName, onRefresh }) => {
    const [chargeState, setChargeState] = useState(charge);
    const [copied, setCopied] = useState(false);

    useEffect(() => {
        setChargeState(charge);
    }, [charge]);

    useEffect(() => {
        if (!chargeState?.provider_payment_id) return;
        if (String(chargeState.status).toLowerCase() !== 'pending') return;

        const interval = setInterval(async () => {
            try {
                const res = await axiosInstance.get(`/subscription/payment-status/${chargeState.provider_payment_id}`);
                const nextStatus = String(res.data?.status || '').toLowerCase();
                if (nextStatus === 'approved') {
                    await onRefresh();
                }
            } catch {
                // ignore transient polling errors
            }
        }, chargeState.payment_method === 'boleto' ? 10000 : 5000);

        return () => clearInterval(interval);
    }, [chargeState?.provider_payment_id, chargeState?.status, chargeState?.payment_method, onRefresh]);

    const handleCopy = (value) => {
        navigator.clipboard.writeText(value || '');
        setCopied(true);
        setTimeout(() => setCopied(false), 2000);
    };

    const title = chargeState?.charge_kind === 'Renewal'
        ? 'Cobrança pendente de renovação'
        : 'Cobrança pendente da assinatura';

    return (
        <Card className="mt-6 border-amber-500/40">
            <CardHeader>
                <div className="flex items-center justify-between gap-3 flex-wrap">
                    <div>
                        <CardTitle className="text-base">{title}</CardTitle>
                        <CardDescription>{planName}</CardDescription>
                    </div>
                    <Badge variant="outline" className="border-amber-500 text-amber-700">
                        {String(chargeState?.payment_method || '').replace('_', ' ')}
                    </Badge>
                </div>
            </CardHeader>
            <CardContent className="space-y-4">
                <div className="grid gap-4 md:grid-cols-3">
                    <div>
                        <p className="text-sm text-muted-foreground">Status</p>
                        <p className="font-medium">{chargeState?.status}</p>
                    </div>
                    <div>
                        <p className="text-sm text-muted-foreground">Valor</p>
                        <p className="font-medium">
                            {typeof chargeState?.amount === 'number'
                                ? `R$ ${chargeState.amount.toFixed(2).replace('.', ',')}`
                                : '—'}
                        </p>
                    </div>
                    <div>
                        <p className="text-sm text-muted-foreground">Vencimento</p>
                        <p className="font-medium">{formatDateTime(chargeState?.due_at)}</p>
                    </div>
                </div>

                {chargeState?.pix_qr_code_base64 && (
                    <div className="space-y-3">
                        <div className="flex justify-center">
                            <img
                                src={`data:image/png;base64,${chargeState.pix_qr_code_base64}`}
                                alt="QR Code PIX"
                                className="w-52 h-52 rounded-lg border"
                            />
                        </div>
                        {chargeState?.pix_qr_code && (
                            <div className="space-y-2">
                                <Label className="text-muted-foreground text-xs">PIX Copia e Cola</Label>
                                <div className="flex gap-2">
                                    <Input value={chargeState.pix_qr_code} readOnly className="text-xs font-mono" />
                                    <Button variant="outline" size="icon" onClick={() => handleCopy(chargeState.pix_qr_code)} className="shrink-0">
                                        {copied ? <Check className="h-4 w-4" /> : <Copy className="h-4 w-4" />}
                                    </Button>
                                </div>
                            </div>
                        )}
                    </div>
                )}

                {chargeState?.boleto_barcode_image_base64 && (
                    <div className="space-y-3">
                        <Label className="text-muted-foreground text-xs">Código de barras para escaneamento</Label>
                        <div className="rounded-lg border bg-white p-3 sm:p-4">
                            <img
                                src={`data:image/png;base64,${chargeState.boleto_barcode_image_base64}`}
                                alt="Código de barras do boleto"
                                className="block w-full h-auto mx-auto"
                                style={{ imageRendering: 'pixelated' }}
                            />
                        </div>
                    </div>
                )}

                {chargeState?.boleto_digitable_line && (
                    <div className="space-y-2">
                        <Label className="text-muted-foreground text-xs">Linha digitável</Label>
                        <div className="flex gap-2">
                            <Input value={chargeState.boleto_digitable_line} readOnly className="text-xs font-mono" />
                            <Button
                                variant="outline"
                                size="icon"
                                onClick={() => handleCopy(chargeState.boleto_digitable_line)}
                                className="shrink-0"
                            >
                                {copied ? <Check className="h-4 w-4" /> : <Copy className="h-4 w-4" />}
                            </Button>
                        </div>
                    </div>
                )}

                {chargeState?.boleto_url && (
                    <Button
                        variant="outline"
                        className="w-full"
                        onClick={() => window.open(chargeState.boleto_url, '_blank')}
                    >
                        <ExternalLink className="h-4 w-4 mr-2" /> Abrir boleto
                    </Button>
                )}

                {String(chargeState?.status).toLowerCase() === 'pending' && (
                    <div className="flex items-center gap-2 text-sm text-muted-foreground">
                        <Loader2 className="h-4 w-4 animate-spin" />
                        Aguardando pagamento...
                    </div>
                )}
            </CardContent>
        </Card>
    );
};


// ======================== PAYMENT DIALOG ========================

const PaymentDialog = ({ open, onOpenChange, plan, onSuccess, payerFullName, payerCpf }) => {
    if (!plan) return null;

    return (
        <Dialog open={open} onOpenChange={onOpenChange}>
            <DialogContent className="w-[96vw] max-w-[96vw] sm:max-w-3xl lg:max-w-5xl max-h-[90vh] overflow-y-auto">
                <DialogHeader>
                    <DialogTitle>Assinar {plan.name}</DialogTitle>
                    <DialogDescription>R${plan.price?.toFixed(2).replace('.', ',')} /mês</DialogDescription>
                </DialogHeader>

                <Tabs defaultValue="card" className="mt-2">
                    <TabsList className="grid w-full grid-cols-3">
                        <TabsTrigger value="card" className="gap-1.5">
                            <CreditCard className="h-3.5 w-3.5" /> Cartão
                        </TabsTrigger>
                        <TabsTrigger value="pix" className="gap-1.5">
                            <QrCode className="h-3.5 w-3.5" /> PIX
                        </TabsTrigger>
                        <TabsTrigger value="boleto" className="gap-1.5">
                            <Barcode className="h-3.5 w-3.5" /> Boleto
                        </TabsTrigger>
                    </TabsList>

                    <TabsContent value="card">
                        <CardPaymentForm
                            plan={plan}
                            onSuccess={onSuccess}
                            onClose={() => onOpenChange(false)}
                            payerCpf={payerCpf}
                        />
                    </TabsContent>
                    <TabsContent value="pix">
                        <PixPaymentForm
                            plan={plan}
                            onSuccess={onSuccess}
                            onClose={() => onOpenChange(false)}
                            payerFullName={payerFullName}
                            payerCpf={payerCpf}
                        />
                    </TabsContent>
                    <TabsContent value="boleto">
                        <BoletoPaymentForm
                            plan={plan}
                            onSuccess={onSuccess}
                            onClose={() => onOpenChange(false)}
                            payerFullName={payerFullName}
                            payerCpf={payerCpf}
                        />
                    </TabsContent>
                </Tabs>
            </DialogContent>
        </Dialog>
    );
};


// ======================== CARD PAYMENT ========================

const CardPaymentForm = ({ plan, onSuccess, onClose, payerCpf }) => {
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState('');
    const [success, setSuccess] = useState(false);
    const [processingMessage, setProcessingMessage] = useState('');
    const [delayedProcessing, setDelayedProcessing] = useState(false);
    const [mpReady, setMpReady] = useState(false);
    const [documentCpf, setDocumentCpf] = useState("");
    const [cardType, setCardType] = useState("credit_card");
    const [expirationDate, setExpirationDate] = useState("");
    const [cardNumber, setCardNumber] = useState("");

    // Load Mercado Pago JS SDK
    useEffect(() => {
        const publicKey = import.meta.env.VITE_MERCADO_PAGO_PUBLIC_KEY;
        if (!publicKey) { setError('Chave pública do Mercado Pago não configurada.'); return; }

        if (window.MercadoPago) { setMpReady(true); return; }

        const script = document.createElement('script');
        script.src = 'https://sdk.mercadopago.com/js/v2';
        script.onload = () => setMpReady(true);
        document.body.appendChild(script);

        return () => { /* script stays loaded */ };
    }, []);

    useEffect(() => {
        setDocumentCpf(sanitizeCpf(payerCpf));
    }, [payerCpf]);

    useEffect(() => {
        if (!delayedProcessing) return;

        const interval = setInterval(async () => {
            try {
                const overview = await onSuccess();
                if (getPlanSyncState(overview, plan.id).isActive) {
                    setDelayedProcessing(false);
                    setProcessingMessage('');
                    setSuccess(true);
                }
            } catch {
                // ignore transient sync errors after timeout
            }
        }, 5000);

        return () => clearInterval(interval);
    }, [delayedProcessing, onSuccess, plan.id]);

    const parseExpirationDate = (value) => {
        const digits = (value || "").replace(/\D/g, "").slice(0, 4);
        if (digits.length !== 4) return { month: "", year: "" };
        return {
            month: digits.slice(0, 2),
            year: `20${digits.slice(2, 4)}`
        };
    };

    const isValidExpirationDate = (value) => {
        const digits = (value || "").replace(/\D/g, "").slice(0, 4);
        if (digits.length !== 4) return false;

        const month = Number(digits.slice(0, 2));
        const year = Number(`20${digits.slice(2, 4)}`);
        if (!Number.isInteger(month) || month < 1 || month > 12) return false;

        const currentYear = new Date().getFullYear();
        return year >= currentYear && year <= currentYear + 8;
    };

    const handleExpirationDateChange = (value) => {
        const digits = (value || "").replace(/\D/g, "").slice(0, 4);
        const month = digits.slice(0, 2);
        const yearDigits = digits.slice(2, 4);
        const currentYear = new Date().getFullYear();
        const minYear = String(currentYear).slice(2);
        const maxYear = String(currentYear + 8).slice(2);

        if (digits.length >= 2 && Number(month) > 12) {
            setExpirationDate(digits.slice(0, 1));
            return;
        }

        if (yearDigits.length === 2) {
            const fullYear = Number(`20${yearDigits}`);
            if (yearDigits < minYear || yearDigits > maxYear || fullYear < currentYear || fullYear > currentYear + 8) {
                setExpirationDate(`${month}/`);
                return;
            }
        }

        setExpirationDate(digits.length > 2 ? `${month}/${yearDigits}` : digits);
    };

    const handleCardNumberChange = (value) => {
        const digits = (value || "").replace(/\D/g, "").slice(0, 16);
        const formatted = digits.match(/.{1,4}/g)?.join(" ") || "";
        setCardNumber(formatted);
    };

    const handleSubmit = async (e) => {
        e.preventDefault();
        setError('');
        setDelayedProcessing(false);
        setProcessingMessage(PAYMENT_PROCESSING_MESSAGE);
        const form = e.target;
        const docNumber = sanitizeCpf(form.docNumber.value || documentCpf);
        if (!isValidCpf(docNumber)) {
            setProcessingMessage('');
            setError('CPF inválido. Verifique os 11 dígitos antes de continuar.');
            return;
        }

        setLoading(true);

        try {
            const publicKey = import.meta.env.VITE_MERCADO_PAGO_PUBLIC_KEY;
            const mp = new window.MercadoPago(publicKey);

            const cardNumber = form.cardNumber.value.replace(/\s/g, '');
            const expirationValue = form.expirationDate.value || expirationDate;
            if (!isValidExpirationDate(expirationValue)) {
                setProcessingMessage('');
                setError('Informe a validade no formato MM/YY, com mês entre 01 e 12 e ano dentro da faixa permitida.');
                return;
            }
            const { month: expMonth, year: expYear } = parseExpirationDate(expirationValue);
            const cvv = form.cvv.value;
            const cardholderName = form.cardholderName.value;

            // Detectar payment_method_id pelo BIN (primeiros 6 dígitos)
            const bin = cardNumber.substring(0, 6);
            let paymentMethodId = '';
            let issuerId = '';
            try {
                const pmResponse = await mp.getPaymentMethods({ bin });
                if (pmResponse.results && pmResponse.results.length > 0) {
                    paymentMethodId = pmResponse.results[0].id;
                    if (pmResponse.results[0].issuer?.id) {
                        issuerId = String(pmResponse.results[0].issuer.id);
                    }
                }
            } catch (pmErr) {
                console.warn('Não foi possível detectar payment method pelo BIN, tentando sem:', pmErr);
            }

            if (!paymentMethodId) {
                setProcessingMessage('');
                setError('Não foi possível identificar a bandeira do cartão pelos primeiros dígitos informados. Confira o número do cartão e tente novamente.');
                return;
            }

            const tokenResponse = await mp.createCardToken({
                cardNumber,
                cardholderName,
                cardExpirationMonth: expMonth,
                cardExpirationYear: expYear,
                securityCode: cvv,
                identificationType: 'CPF',
                identificationNumber: docNumber
            });

            const result = await axiosInstance.post('/subscription/card', {
                plan_id: plan.id,
                card_token: tokenResponse.id,
                payment_method_id: paymentMethodId,
                card_type: cardType,
                issuer_id: issuerId,
                installments: 1,
                payer_cpf: docNumber
            }, PAYMENT_REQUEST_CONFIG);

            if (result.data.status === 'approved') {
                setProcessingMessage('');
                setSuccess(true);
                await onSuccess();
            } else {
                setProcessingMessage('');
                const status = result.data?.status || 'desconhecido';
                const detail = result.data?.status_detail || 'sem detalhe retornado pelo provedor';
                setError(`Pagamento ${status}: ${detail}`);
            }
        } catch (err) {
            if (isTimeoutError(err)) {
                setError('');
                setDelayedProcessing(true);
                setProcessingMessage(PAYMENT_TIMEOUT_MESSAGE);
                await onSuccess();
                return;
            }
            setProcessingMessage('');
            setError(extractErrorMessage(err, 'Erro ao processar pagamento.'));
        } finally {
            setLoading(false);
        }
    };

    if (success) {
        return (
            <div className="py-8 text-center space-y-4">
                <div className="mx-auto w-12 h-12 rounded-full bg-primary/10 flex items-center justify-center">
                    <Check className="h-6 w-6 text-primary" />
                </div>
                <p className="font-medium">Assinatura ativada com sucesso!</p>
                <Button onClick={onClose}>Fechar</Button>
            </div>
        );
    }

    if (delayedProcessing) {
        return (
            <div className="py-8 space-y-4">
                <div className="flex justify-center">
                    <Loader2 className="h-8 w-8 animate-spin text-amber-600" />
                </div>
                <div className="rounded-lg border border-amber-500/40 bg-amber-50 px-4 py-3 text-sm text-amber-900">
                    <p>{processingMessage}</p>
                    <p className="mt-2 text-xs text-amber-800">{PAYMENT_TIMEOUT_FOLLOW_UP_MESSAGE}</p>
                </div>
                <Button onClick={onClose} className="w-full">Fechar e acompanhar</Button>
            </div>
        );
    }

    return (
        <form onSubmit={handleSubmit} className="space-y-4 pt-2">
            <div className="space-y-2">
                <Label>Tipo de cartão</Label>
                <Tabs value={cardType} onValueChange={setCardType} className="w-full">
                    <TabsList className="grid w-full grid-cols-2">
                        <TabsTrigger value="credit_card">Cartão de crédito</TabsTrigger>
                        <TabsTrigger value="debit_card">Cartão de débito</TabsTrigger>
                    </TabsList>
                </Tabs>
                <p className="text-xs text-muted-foreground">
                    {cardType === "debit_card"
                        ? "Você está usando a modalidade Cartão de débito."
                        : "Você está usando a modalidade Cartão de crédito."}
                </p>
            </div>

            <div className="space-y-2">
                <Label htmlFor="cardNumber">Número do cartão</Label>
                <Input
                    id="cardNumber"
                    name="cardNumber"
                    value={cardNumber}
                    onChange={(e) => handleCardNumberChange(e.target.value)}
                    placeholder="0000 0000 0000 0000"
                    required
                    disabled={!mpReady}
                    inputMode="numeric"
                    maxLength={19}
                />
            </div>
            <div className="grid grid-cols-3 gap-3">
                <div className="space-y-2 col-span-2">
                    <Label htmlFor="expirationDate">Validade</Label>
                    <Input
                        id="expirationDate"
                        name="expirationDate"
                        value={expirationDate}
                        onChange={(e) => handleExpirationDateChange(e.target.value)}
                        placeholder="MM/YY"
                        maxLength={5}
                        required
                        disabled={!mpReady}
                        inputMode="numeric"
                    />
                </div>
                <div className="space-y-2">
                    <Label htmlFor="cvv">Código de Segurança (CVC)</Label>
                    <Input id="cvv" name="cvv" placeholder="123" maxLength={4} required disabled={!mpReady} />
                </div>
            </div>
            <div className="space-y-2">
                <Label htmlFor="cardholderName">Nome Impresso no cartão</Label>
                <Input id="cardholderName" name="cardholderName" placeholder="Nome completo" required disabled={!mpReady} />
            </div>
            <div className="space-y-2">
                <Label htmlFor="docNumber">CPF</Label>
                <Input
                    id="docNumber"
                    name="docNumber"
                    value={formatCpf(documentCpf)}
                    onChange={(e) => setDocumentCpf(sanitizeCpf(e.target.value))}
                    placeholder="000.000.000-00"
                    required
                    disabled={!mpReady}
                    inputMode="numeric"
                />
            </div>

            {error && (
                <div className="p-3 rounded-lg bg-destructive/10 text-destructive text-sm">{error}</div>
            )}

            {loading && processingMessage && (
                <div className="rounded-lg border border-amber-500/40 bg-amber-50 px-4 py-3 text-sm text-amber-900">
                    {processingMessage}
                </div>
            )}

            <Button type="submit" className="w-full" disabled={loading || !mpReady}>
                {loading ? (
                    <><Loader2 className="h-4 w-4 animate-spin mr-2" /> Processando...</>
                ) : (
                    <>Pagar R${plan.price?.toFixed(2).replace('.', ',')}</>
                )}
            </Button>
        </form>
    );
};


// ======================== PIX PAYMENT ========================

const PixPaymentForm = ({ plan, onSuccess, onClose, payerFullName, payerCpf }) => {
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState('');
    const [pixData, setPixData] = useState(null);
    const [polling, setPolling] = useState(false);
    const [processingMessage, setProcessingMessage] = useState('');
    const [delayedProcessing, setDelayedProcessing] = useState(false);
    const [copied, setCopied] = useState(false);
    const [documentCpf, setDocumentCpf] = useState("");

    useEffect(() => {
        setDocumentCpf(sanitizeCpf(payerCpf));
    }, [payerCpf]);

    useEffect(() => {
        if (!delayedProcessing) return;

        const interval = setInterval(async () => {
            try {
                const overview = await onSuccess();
                if (getPlanSyncState(overview, plan.id).isActive) {
                    setDelayedProcessing(false);
                    setProcessingMessage('');
                    setPixData(prev => ({ ...(prev || {}), status: 'approved' }));
                }
            } catch {
                // ignore transient sync errors after timeout
            }
        }, 5000);

        return () => clearInterval(interval);
    }, [delayedProcessing, onSuccess, plan.id]);

    const handleSubmit = async (e) => {
        e.preventDefault();
        setError('');
        setDelayedProcessing(false);
        setProcessingMessage(PAYMENT_PROCESSING_MESSAGE);
        const form = e.target;
        const normalizedCpf = sanitizeCpf(form.cpf.value || documentCpf);
        if (!isValidCpf(normalizedCpf)) {
            setProcessingMessage('');
            setError('CPF inválido. Verifique os 11 dígitos antes de continuar.');
            return;
        }

        setLoading(true);

        try {
            const { firstName, lastName } = splitFullName(payerFullName);
            const result = await axiosInstance.post('/subscription/pix', {
                plan_id: plan.id,
                payer_first_name: firstName,
                payer_last_name: lastName,
                payer_cpf: normalizedCpf
            }, PAYMENT_REQUEST_CONFIG);

            setPixData(result.data);
            setProcessingMessage('');

            if (result.data.status === 'approved') {
                await onSuccess();
            } else {
                setPolling(true);
            }
        } catch (err) {
            if (isTimeoutError(err)) {
                setError('');
                setDelayedProcessing(true);
                setProcessingMessage(PAYMENT_TIMEOUT_MESSAGE);
                await onSuccess();
                return;
            }
            setProcessingMessage('');
            setError(extractErrorMessage(err, 'Erro ao gerar PIX.'));
        } finally {
            setLoading(false);
        }
    };

    // Polling for PIX payment status
    useEffect(() => {
        if (!polling || !pixData?.payment_id) return;

        const interval = setInterval(async () => {
            try {
                const res = await axiosInstance.get(`/subscription/payment-status/${pixData.payment_id}`);
                if (res.data.status === 'approved') {
                    setPolling(false);
                    setPixData(prev => ({ ...prev, status: 'approved' }));
                    await onSuccess();
                }
            } catch { /* ignore */ }
        }, 5000);

        return () => clearInterval(interval);
    }, [polling, pixData?.payment_id, onSuccess]);

    const handleCopy = () => {
        navigator.clipboard.writeText(pixData?.pix_qr_code || '');
        setCopied(true);
        setTimeout(() => setCopied(false), 2000);
    };

    if (pixData?.status === 'approved') {
        return (
            <div className="py-8 text-center space-y-4">
                <div className="mx-auto w-12 h-12 rounded-full bg-primary/10 flex items-center justify-center">
                    <Check className="h-6 w-6 text-primary" />
                </div>
                <p className="font-medium">Pagamento aprovado!</p>
                <Button onClick={onClose}>Fechar</Button>
            </div>
        );
    }

    if (delayedProcessing) {
        return (
            <div className="py-8 space-y-4">
                <div className="flex justify-center">
                    <Loader2 className="h-8 w-8 animate-spin text-amber-600" />
                </div>
                <div className="rounded-lg border border-amber-500/40 bg-amber-50 px-4 py-3 text-sm text-amber-900">
                    <p>{processingMessage}</p>
                    <p className="mt-2 text-xs text-amber-800">{PAYMENT_TIMEOUT_FOLLOW_UP_MESSAGE}</p>
                </div>
                <Button onClick={onClose} className="w-full">Fechar e acompanhar</Button>
            </div>
        );
    }

    if (pixData) {
        return (
            <div className="space-y-4 pt-2">
                <div className="text-center space-y-4">
                    {pixData.pix_qr_code_base64 && (
                        <div className="flex justify-center">
                            <img
                                src={`data:image/png;base64,${pixData.pix_qr_code_base64}`}
                                alt="QR Code PIX"
                                className="w-52 h-52 rounded-lg border"
                            />
                        </div>
                    )}

                    {pixData.pix_qr_code && (
                        <div className="space-y-2">
                            <Label className="text-muted-foreground text-xs">PIX Copia e Cola</Label>
                            <div className="flex gap-2">
                                <Input value={pixData.pix_qr_code} readOnly className="text-xs font-mono" />
                                <Button variant="outline" size="icon" onClick={handleCopy} className="shrink-0">
                                    {copied ? <Check className="h-4 w-4" /> : <Copy className="h-4 w-4" />}
                                </Button>
                            </div>
                        </div>
                    )}

                    {polling && (
                        <div className="flex items-center justify-center gap-2 text-sm text-muted-foreground">
                            <Loader2 className="h-4 w-4 animate-spin" />
                            Aguardando pagamento...
                        </div>
                    )}
                </div>
            </div>
        );
    }

    return (
        <form onSubmit={handleSubmit} className="space-y-4 pt-2">
            <div className="space-y-2">
                <Label htmlFor="cpf">CPF</Label>
                <Input
                    id="cpf"
                    name="cpf"
                    value={formatCpf(documentCpf)}
                    onChange={(e) => setDocumentCpf(sanitizeCpf(e.target.value))}
                    placeholder="000.000.000-00"
                    required
                    inputMode="numeric"
                />
            </div>

            {error && (
                <div className="p-3 rounded-lg bg-destructive/10 text-destructive text-sm">{error}</div>
            )}

            {loading && processingMessage && (
                <div className="rounded-lg border border-amber-500/40 bg-amber-50 px-4 py-3 text-sm text-amber-900">
                    {processingMessage}
                </div>
            )}

            <Button type="submit" className="w-full" disabled={loading}>
                {loading ? (
                    <><Loader2 className="h-4 w-4 animate-spin mr-2" /> Gerando PIX...</>
                ) : (
                    <>Gerar QR Code PIX</>
                )}
            </Button>
        </form>
    );
};


// ======================== BOLETO PAYMENT ========================

const BoletoPaymentForm = ({ plan, onSuccess, onClose, payerFullName, payerCpf }) => {
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState('');
    const [boletoData, setBoletoData] = useState(null);
    const [polling, setPolling] = useState(false);
    const [processingMessage, setProcessingMessage] = useState('');
    const [delayedProcessing, setDelayedProcessing] = useState(false);
    const [copied, setCopied] = useState(false);
    const [documentCpf, setDocumentCpf] = useState("");

    useEffect(() => {
        setDocumentCpf(sanitizeCpf(payerCpf));
    }, [payerCpf]);

    useEffect(() => {
        if (!delayedProcessing) return;

        const interval = setInterval(async () => {
            try {
                const overview = await onSuccess();
                if (getPlanSyncState(overview, plan.id).isActive) {
                    setDelayedProcessing(false);
                    setProcessingMessage('');
                    setBoletoData(prev => ({ ...(prev || {}), status: 'approved' }));
                }
            } catch {
                // ignore transient sync errors after timeout
            }
        }, 5000);

        return () => clearInterval(interval);
    }, [delayedProcessing, onSuccess, plan.id]);

    const handleSubmit = async (e) => {
        e.preventDefault();
        setError('');
        setDelayedProcessing(false);
        setProcessingMessage(PAYMENT_PROCESSING_MESSAGE);
        const form = e.target;
        const normalizedCpf = sanitizeCpf(form.cpf.value || documentCpf);
        if (!isValidCpf(normalizedCpf)) {
            setProcessingMessage('');
            setError('CPF inválido. Verifique os 11 dígitos antes de continuar.');
            return;
        }

        setLoading(true);

        try {
            const { firstName, lastName } = splitFullName(payerFullName);
            const result = await axiosInstance.post('/subscription/boleto', {
                plan_id: plan.id,
                payer_first_name: firstName,
                payer_last_name: lastName,
                payer_cpf: normalizedCpf
            }, PAYMENT_REQUEST_CONFIG);

            setBoletoData(result.data);
            setProcessingMessage('');
            setPolling(true);
        } catch (err) {
            if (isTimeoutError(err)) {
                setError('');
                setDelayedProcessing(true);
                setProcessingMessage(PAYMENT_TIMEOUT_MESSAGE);
                await onSuccess();
                return;
            }
            setProcessingMessage('');
            setError(extractErrorMessage(err, 'Erro ao gerar boleto.'));
        } finally {
            setLoading(false);
        }
    };

    // Polling for boleto payment status
    useEffect(() => {
        if (!polling || !boletoData?.payment_id) return;

        const interval = setInterval(async () => {
            try {
                const res = await axiosInstance.get(`/subscription/payment-status/${boletoData.payment_id}`);
                if (res.data.status === 'approved') {
                    setPolling(false);
                    setBoletoData(prev => ({ ...prev, status: 'approved' }));
                    await onSuccess();
                }
            } catch { /* ignore */ }
        }, 10000);

        return () => clearInterval(interval);
    }, [polling, boletoData?.payment_id, onSuccess]);

    const handleCopy = (value) => {
        navigator.clipboard.writeText(value || '');
        setCopied(true);
        setTimeout(() => setCopied(false), 2000);
    };

    if (boletoData?.status === 'approved') {
        return (
            <div className="py-8 text-center space-y-4">
                <div className="mx-auto w-12 h-12 rounded-full bg-primary/10 flex items-center justify-center">
                    <Check className="h-6 w-6 text-primary" />
                </div>
                <p className="font-medium">Pagamento aprovado!</p>
                <Button onClick={onClose}>Fechar</Button>
            </div>
        );
    }

    if (delayedProcessing) {
        return (
            <div className="py-8 space-y-4">
                <div className="flex justify-center">
                    <Loader2 className="h-8 w-8 animate-spin text-amber-600" />
                </div>
                <div className="rounded-lg border border-amber-500/40 bg-amber-50 px-4 py-3 text-sm text-amber-900">
                    <p>{processingMessage}</p>
                    <p className="mt-2 text-xs text-amber-800">{PAYMENT_TIMEOUT_FOLLOW_UP_MESSAGE}</p>
                </div>
                <Button onClick={onClose} className="w-full">Fechar e acompanhar</Button>
            </div>
        );
    }

    if (boletoData) {
        return (
            <div className="space-y-4 pt-2">
                {boletoData.boleto_barcode_image_base64 && (
                    <div className="space-y-2">
                        <Label className="text-muted-foreground text-xs">Código de barras para escaneamento</Label>
                        <div className="rounded-lg border bg-white p-3 sm:p-4">
                            <img
                                src={`data:image/png;base64,${boletoData.boleto_barcode_image_base64}`}
                                alt="Código de barras do boleto"
                                className="block w-full h-auto mx-auto"
                                style={{ imageRendering: 'pixelated' }}
                            />
                        </div>
                        <p className="text-[11px] text-muted-foreground">
                            Se o banco não reconhecer de primeira, aumente o brilho da tela e mantenha o celular paralelo ao código.
                        </p>
                    </div>
                )}

                {boletoData.boleto_digitable_line && (
                    <div className="space-y-2">
                        <Label className="text-muted-foreground text-xs">Linha digitável</Label>
                        <div className="flex gap-2">
                            <Input value={boletoData.boleto_digitable_line} readOnly className="text-xs font-mono" />
                            <Button
                                variant="outline"
                                size="icon"
                                onClick={() => handleCopy(boletoData.boleto_digitable_line)}
                                className="shrink-0"
                            >
                                {copied ? <Check className="h-4 w-4" /> : <Copy className="h-4 w-4" />}
                            </Button>
                        </div>
                    </div>
                )}

                {boletoData.boleto_url && (
                    <Button
                        variant="outline"
                        className="w-full"
                        onClick={() => window.open(boletoData.boleto_url, '_blank')}
                    >
                        <ExternalLink className="h-4 w-4 mr-2" /> Abrir boleto
                    </Button>
                )}

                {polling && (
                    <div className="flex items-center justify-center gap-2 text-sm text-muted-foreground">
                        <Loader2 className="h-4 w-4 animate-spin" />
                        Aguardando pagamento...
                    </div>
                )}
            </div>
        );
    }

    return (
        <form onSubmit={handleSubmit} className="space-y-4 pt-2">
            <div className="space-y-2">
                <Label htmlFor="cpf">CPF</Label>
                <Input
                    id="cpf"
                    name="cpf"
                    value={formatCpf(documentCpf)}
                    onChange={(e) => setDocumentCpf(sanitizeCpf(e.target.value))}
                    placeholder="000.000.000-00"
                    required
                    inputMode="numeric"
                />
            </div>

            {error && (
                <div className="p-3 rounded-lg bg-destructive/10 text-destructive text-sm">{error}</div>
            )}

            {loading && processingMessage && (
                <div className="rounded-lg border border-amber-500/40 bg-amber-50 px-4 py-3 text-sm text-amber-900">
                    {processingMessage}
                </div>
            )}

            <Button type="submit" className="w-full" disabled={loading}>
                {loading ? (
                    <><Loader2 className="h-4 w-4 animate-spin mr-2" /> Gerando boleto...</>
                ) : (
                    <>Gerar boleto</>
                )}
            </Button>
        </form>
    );
};


export default SubscriptionPage;
