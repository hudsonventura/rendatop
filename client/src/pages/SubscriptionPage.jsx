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

const SubscriptionPage = () => {
    const [plans, setPlans] = useState([]);
    const [activeSub, setActiveSub] = useState(null);
    const [pendingSub, setPendingSub] = useState(null);
    const [payerFullName, setPayerFullName] = useState("");
    const [payerCpf, setPayerCpf] = useState("");
    const [loading, setLoading] = useState(true);
    const [selectedPlan, setSelectedPlan] = useState(null);
    const [paymentDialogOpen, setPaymentDialogOpen] = useState(false);
    const [pendingCancelDialogOpen, setPendingCancelDialogOpen] = useState(false);

    const fetchData = useCallback(async () => {
        setLoading(true);
        try {
            const [plansRes, subRes, settingsRes] = await Promise.all([
                axiosInstance.get('/plans'),
                axiosInstance.get('/subscription/overview').catch(() => ({ data: null })),
                axiosInstance.get('/User/Settings').catch(() => ({ data: null }))
            ]);
            setPlans(plansRes.data);
            setActiveSub(subRes.data?.active_subscription || null);
            setPendingSub(subRes.data?.pending_subscription || null);
            setPayerFullName(settingsRes?.data?.name || sessionStorage.getItem('name') || "");
            setPayerCpf(settingsRes?.data?.cpf || "");
        } catch (err) {
            console.error(err);
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
        try {
            await axiosInstance.post('/subscription/cancel-active');
            await fetchData();
        } catch (err) {
            console.error(err);
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

    const currentPlanId = activeSub?.plan_id || 'free';
    const pendingPlanId = pendingSub?.plan_id || null;

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
                                                            onClick={handleCancelSubscription}
                                                        >
                                                            Cancelar assinatura
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
        </>
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
        const form = e.target;
        const docNumber = sanitizeCpf(form.docNumber.value || documentCpf);
        if (!isValidCpf(docNumber)) {
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
            });

            if (result.data.status === 'approved') {
                setSuccess(true);
                await onSuccess();
            } else {
                setError(`Pagamento ${result.data.status}: ${result.data.status_detail}`);
            }
        } catch (err) {
            const msg = err?.response?.data?.Message || err?.response?.data || err.message || 'Erro ao processar pagamento.';
            setError(typeof msg === 'string' ? msg : 'Erro ao processar pagamento.');
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
    const [copied, setCopied] = useState(false);
    const [documentCpf, setDocumentCpf] = useState("");

    useEffect(() => {
        setDocumentCpf(sanitizeCpf(payerCpf));
    }, [payerCpf]);

    const handleSubmit = async (e) => {
        e.preventDefault();
        setError('');
        const form = e.target;
        const normalizedCpf = sanitizeCpf(form.cpf.value || documentCpf);
        if (!isValidCpf(normalizedCpf)) {
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
            });

            setPixData(result.data);

            if (result.data.status === 'approved') {
                await onSuccess();
            } else {
                setPolling(true);
            }
        } catch (err) {
            const msg = err?.response?.data?.Message || err?.response?.data || err.message || 'Erro ao gerar PIX.';
            setError(typeof msg === 'string' ? msg : 'Erro ao gerar PIX.');
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
    const [copied, setCopied] = useState(false);
    const [documentCpf, setDocumentCpf] = useState("");

    useEffect(() => {
        setDocumentCpf(sanitizeCpf(payerCpf));
    }, [payerCpf]);

    const handleSubmit = async (e) => {
        e.preventDefault();
        setError('');
        const form = e.target;
        const normalizedCpf = sanitizeCpf(form.cpf.value || documentCpf);
        if (!isValidCpf(normalizedCpf)) {
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
            });

            setBoletoData(result.data);
            setPolling(true);
        } catch (err) {
            const msg = err?.response?.data?.Message || err?.response?.data || err.message || 'Erro ao gerar boleto.';
            setError(typeof msg === 'string' ? msg : 'Erro ao gerar boleto.');
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
