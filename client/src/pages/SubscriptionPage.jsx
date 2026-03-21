import React, { useState, useEffect, useCallback } from 'react';
import { Card, CardHeader, CardTitle, CardDescription, CardContent, CardFooter } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Separator } from "@/components/ui/separator";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription } from "@/components/ui/dialog";
import { Skeleton } from "@/components/ui/skeleton";
import { Check, CreditCard, QrCode, Barcode, Loader2, Crown, Sparkles, Copy, X, ExternalLink } from "lucide-react";
import { SidebarInset, SidebarProvider } from "@/components/ui/sidebar";
import { AppSidebar } from "@/components/app-sidebar";
import { SiteHeader } from "@/components/site-header";
import Logged from "@/components/Logged";
import axiosInstance from "@/utils/axiosConfig";

const SubscriptionPage = () => {
    const [plans, setPlans] = useState([]);
    const [currentSub, setCurrentSub] = useState(null);
    const [loading, setLoading] = useState(true);
    const [selectedPlan, setSelectedPlan] = useState(null);
    const [paymentDialogOpen, setPaymentDialogOpen] = useState(false);

    const fetchData = useCallback(async () => {
        setLoading(true);
        try {
            const [plansRes, subRes] = await Promise.all([
                axiosInstance.get('/plans'),
                axiosInstance.get('/subscription').catch(() => ({ data: null }))
            ]);
            setPlans(plansRes.data);
            setCurrentSub(subRes.data);
        } catch (err) {
            console.error(err);
        } finally {
            setLoading(false);
        }
    }, []);

    useEffect(() => { fetchData(); }, [fetchData]);

    const handleSelectPlan = (plan) => {
        if (plan.price <= 0) return;
        if (currentSub?.plan_id === plan.id && currentSub?.status === 'Active') return;
        setSelectedPlan(plan);
        setPaymentDialogOpen(true);
    };

    const handleCancelSubscription = async () => {
        try {
            await axiosInstance.post('/subscription/cancel');
            await fetchData();
        } catch (err) {
            console.error(err);
        }
    };

    const currentPlanId = (currentSub?.status === 'Active' || currentSub?.status === 'PendingPayment')
        ? currentSub?.plan_id
        : 'free';

    const planIcons = { free: null, plus: Sparkles, pro: Crown };

    return (
        <>
            <Logged />
            <SidebarProvider>
                <AppSidebar />
                <SidebarInset>
                    <SiteHeader />
                    <div className="flex-1 p-4 md:p-6 max-w-5xl mx-auto w-full">
                        <div className="mb-8">
                            <h1 className="text-2xl font-bold tracking-tight">Assinatura</h1>
                            <p className="text-muted-foreground mt-1">Escolha o plano ideal para você</p>
                        </div>

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
                                    const PlanIcon = planIcons[plan.id];
                                    const isPopular = plan.id === 'plus';

                                    return (
                                        <Card
                                            key={plan.id}
                                            className={`relative flex flex-col transition-all ${
                                                isActive
                                                    ? 'border-primary ring-1 ring-primary/20'
                                                    : 'hover:border-primary/40'
                                            }`}
                                        >
                                            {isPopular && !isActive && (
                                                <div className="absolute -top-3 left-1/2 -translate-x-1/2">
                                                    <Badge className="text-xs">Mais popular</Badge>
                                                </div>
                                            )}

                                            {isActive && (
                                                <div className="absolute -top-3 left-1/2 -translate-x-1/2">
                                                    <Badge variant="outline" className="text-xs border-primary text-primary bg-primary/5">
                                                        <Check className="h-3 w-3 mr-1" /> Atual
                                                    </Badge>
                                                </div>
                                            )}

                                            <CardHeader className="pb-4 pt-6">
                                                <div className="flex items-center gap-2">
                                                    {PlanIcon && <PlanIcon className="h-5 w-5 text-primary" />}
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

                        {currentSub && currentSub.status === 'Active' && currentSub.plan_id !== 'free' && (
                            <Card className="mt-6">
                                <CardContent className="pt-6">
                                    <div className="flex items-center justify-between flex-wrap gap-4">
                                        <div>
                                            <p className="text-sm text-muted-foreground">Assinatura ativa</p>
                                            <p className="font-medium">{currentSub.plan?.name || currentSub.plan_id}</p>
                                        </div>
                                        <div>
                                            <p className="text-sm text-muted-foreground">Método de pagamento</p>
                                            <p className="font-medium capitalize">{currentSub.payment_method?.replace('_', ' ')}</p>
                                        </div>
                                        <div>
                                            <p className="text-sm text-muted-foreground">Próximo vencimento</p>
                                            <p className="font-medium">
                                                {new Date(currentSub.current_period_end).toLocaleDateString('pt-BR')}
                                            </p>
                                        </div>
                                    </div>
                                </CardContent>
                            </Card>
                        )}
                    </div>
                </SidebarInset>
            </SidebarProvider>

            <PaymentDialog
                open={paymentDialogOpen}
                onOpenChange={setPaymentDialogOpen}
                plan={selectedPlan}
                onSuccess={fetchData}
            />
        </>
    );
};


// ======================== PAYMENT DIALOG ========================

const PaymentDialog = ({ open, onOpenChange, plan, onSuccess }) => {
    if (!plan) return null;

    return (
        <Dialog open={open} onOpenChange={onOpenChange}>
            <DialogContent className="sm:max-w-lg max-h-[90vh] overflow-y-auto">
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
                        <CardPaymentForm plan={plan} onSuccess={onSuccess} onClose={() => onOpenChange(false)} />
                    </TabsContent>
                    <TabsContent value="pix">
                        <PixPaymentForm plan={plan} onSuccess={onSuccess} onClose={() => onOpenChange(false)} />
                    </TabsContent>
                    <TabsContent value="boleto">
                        <BoletoPaymentForm plan={plan} onSuccess={onSuccess} onClose={() => onOpenChange(false)} />
                    </TabsContent>
                </Tabs>
            </DialogContent>
        </Dialog>
    );
};


// ======================== CARD PAYMENT ========================

const CardPaymentForm = ({ plan, onSuccess, onClose }) => {
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState('');
    const [success, setSuccess] = useState(false);
    const [mpReady, setMpReady] = useState(false);

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

    const handleSubmit = async (e) => {
        e.preventDefault();
        setError('');
        setLoading(true);

        try {
            const publicKey = import.meta.env.VITE_MERCADO_PAGO_PUBLIC_KEY;
            const mp = new window.MercadoPago(publicKey);

            const form = e.target;
            const cardNumber = form.cardNumber.value.replace(/\s/g, '');
            const expMonth = form.expMonth.value;
            const expYear = form.expYear.value;
            const cvv = form.cvv.value;
            const cardholderName = form.cardholderName.value;
            const docNumber = form.docNumber.value;

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
                issuer_id: issuerId,
                installments: 1
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
                <Label htmlFor="cardNumber">Número do cartão</Label>
                <Input id="cardNumber" name="cardNumber" placeholder="0000 0000 0000 0000" required disabled={!mpReady} />
            </div>
            <div className="grid grid-cols-3 gap-3">
                <div className="space-y-2">
                    <Label htmlFor="expMonth">Mês</Label>
                    <Input id="expMonth" name="expMonth" placeholder="MM" maxLength={2} required disabled={!mpReady} />
                </div>
                <div className="space-y-2">
                    <Label htmlFor="expYear">Ano</Label>
                    <Input id="expYear" name="expYear" placeholder="AAAA" maxLength={4} required disabled={!mpReady} />
                </div>
                <div className="space-y-2">
                    <Label htmlFor="cvv">CVV</Label>
                    <Input id="cvv" name="cvv" placeholder="123" maxLength={4} required disabled={!mpReady} />
                </div>
            </div>
            <div className="space-y-2">
                <Label htmlFor="cardholderName">Nome no cartão</Label>
                <Input id="cardholderName" name="cardholderName" placeholder="Nome completo" required disabled={!mpReady} />
            </div>
            <div className="space-y-2">
                <Label htmlFor="docNumber">CPF</Label>
                <Input id="docNumber" name="docNumber" placeholder="000.000.000-00" required disabled={!mpReady} />
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

const PixPaymentForm = ({ plan, onSuccess, onClose }) => {
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState('');
    const [pixData, setPixData] = useState(null);
    const [polling, setPolling] = useState(false);
    const [copied, setCopied] = useState(false);

    const handleSubmit = async (e) => {
        e.preventDefault();
        setError('');
        setLoading(true);

        try {
            const form = e.target;
            const result = await axiosInstance.post('/subscription/pix', {
                plan_id: plan.id,
                payer_first_name: form.firstName.value,
                payer_last_name: form.lastName.value,
                payer_cpf: form.cpf.value.replace(/\D/g, '')
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
            <div className="grid grid-cols-2 gap-3">
                <div className="space-y-2">
                    <Label htmlFor="firstName">Nome</Label>
                    <Input id="firstName" name="firstName" placeholder="Nome" required />
                </div>
                <div className="space-y-2">
                    <Label htmlFor="lastName">Sobrenome</Label>
                    <Input id="lastName" name="lastName" placeholder="Sobrenome" required />
                </div>
            </div>
            <div className="space-y-2">
                <Label htmlFor="cpf">CPF</Label>
                <Input id="cpf" name="cpf" placeholder="000.000.000-00" required />
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

const BoletoPaymentForm = ({ plan, onSuccess, onClose }) => {
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState('');
    const [boletoData, setBoletoData] = useState(null);
    const [polling, setPolling] = useState(false);
    const [copied, setCopied] = useState(false);

    const handleSubmit = async (e) => {
        e.preventDefault();
        setError('');
        setLoading(true);

        try {
            const form = e.target;
            const result = await axiosInstance.post('/subscription/boleto', {
                plan_id: plan.id,
                payer_first_name: form.firstName.value,
                payer_last_name: form.lastName.value,
                payer_cpf: form.cpf.value.replace(/\D/g, '')
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

    const handleCopy = () => {
        navigator.clipboard.writeText(boletoData?.boleto_barcode_content || '');
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
                {boletoData.boleto_barcode_content && (
                    <div className="space-y-2">
                        <Label className="text-muted-foreground text-xs">Código de barras</Label>
                        <div className="flex gap-2">
                            <Input value={boletoData.boleto_barcode_content} readOnly className="text-xs font-mono" />
                            <Button variant="outline" size="icon" onClick={handleCopy} className="shrink-0">
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
            <div className="grid grid-cols-2 gap-3">
                <div className="space-y-2">
                    <Label htmlFor="firstName">Nome</Label>
                    <Input id="firstName" name="firstName" placeholder="Nome" required />
                </div>
                <div className="space-y-2">
                    <Label htmlFor="lastName">Sobrenome</Label>
                    <Input id="lastName" name="lastName" placeholder="Sobrenome" required />
                </div>
            </div>
            <div className="space-y-2">
                <Label htmlFor="cpf">CPF</Label>
                <Input id="cpf" name="cpf" placeholder="000.000.000-00" required />
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
