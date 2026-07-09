import { useEffect, useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { Check, Clock3, XCircle } from 'lucide-react';
import { BaseLayout } from "@/components/layouts/base-layout";
import Logged from "@/components/Logged";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import axiosInstance from "@/utils/axiosConfig";

function getStatusCopy(status) {
    const normalized = String(status || '').toLowerCase();

    if (normalized === 'approved' || normalized === 'authorized') {
        return {
            icon: Check,
            title: 'Pagamento aprovado',
            description: 'O Mercado Pago informou que o pagamento foi confirmado. Estamos sincronizando sua assinatura.',
        };
    }

    if (normalized === 'pending' || normalized === 'in_process') {
        return {
            icon: Clock3,
            title: 'Pagamento em processamento',
            description: 'Seu retorno foi recebido, mas a ativação final depende da confirmação oficial do Mercado Pago pelo webhook.',
        };
    }

    return {
        icon: XCircle,
        title: 'Pagamento não concluído',
        description: 'O checkout foi encerrado sem aprovação imediata. Você pode voltar para a tela de assinatura e tentar novamente.',
    };
}

export default function SubscriptionMercadoPagoReturnPage() {
    const [searchParams] = useSearchParams();
    const [resolvedStatus, setResolvedStatus] = useState(searchParams.get('status') || '');
    const [syncMessage, setSyncMessage] = useState('Atualizando o status local da assinatura...');
    const status = searchParams.get('status');
    const externalReference = searchParams.get('external_reference');
    const paymentId = searchParams.get('payment_id');
    const preapprovalId = searchParams.get('preapproval_id');
    const preferenceId = searchParams.get('preference_id');
    const { icon: Icon, title, description } = getStatusCopy(resolvedStatus || status);

    useEffect(() => {
        let active = true;

        const sync = async () => {
            try {
                const reference = paymentId || preapprovalId || preferenceId || externalReference;

                if (reference) {
                    const { data } = await axiosInstance.get(`/subscription/payment-status/${reference}`);
                    if (active) {
                        setResolvedStatus(String(data?.status || ''));
                        setSyncMessage('Status local atualizado com a resposta mais recente do Mercado Pago.');
                    }
                    return;
                }

                const { data } = await axiosInstance.get('/subscription/overview');
                if (active) {
                    const overviewStatus = data?.pending_charge?.status === 1
                        ? 'approved'
                        : data?.pending_charge?.status === 2
                            ? 'rejected'
                            : '';
                    if (overviewStatus) setResolvedStatus(overviewStatus);
                    setSyncMessage('Sua assinatura foi recarregada. Se o pagamento seguir pendente, aguarde a confirmação do webhook.');
                }
            } catch {
                if (active) setSyncMessage('Não foi possível sincronizar agora, mas o webhook continuará tentando confirmar a cobrança.');
            }
        };

        sync();
        return () => {
            active = false;
        };
    }, [externalReference, paymentId, preferenceId, preapprovalId]);

    return (
        <>
            <Logged />
            <BaseLayout title="Retorno do pagamento" description="Acompanhamento do checkout do Mercado Pago">
                <div className="mx-auto max-w-2xl px-4 lg:px-6">
                    <Card>
                        <CardHeader>
                            <div className="mb-4 flex h-12 w-12 items-center justify-center rounded-full bg-primary/10 text-primary">
                                <Icon className="h-6 w-6" />
                            </div>
                            <CardTitle>{title}</CardTitle>
                            <CardDescription>{description}</CardDescription>
                        </CardHeader>
                        <CardContent className="space-y-4">
                            <div className="rounded-lg border bg-muted/30 p-4 text-sm text-muted-foreground">
                                {syncMessage}
                            </div>

                            {(externalReference || preapprovalId || preferenceId) && (
                                <div className="text-xs text-muted-foreground">
                                    Referência da tentativa: {externalReference || preapprovalId || preferenceId}
                                </div>
                            )}

                            <div className="flex flex-wrap gap-3">
                                <Button asChild>
                                    <Link to="/subscription">Voltar para assinatura</Link>
                                </Button>
                            </div>
                        </CardContent>
                    </Card>
                </div>
            </BaseLayout>
        </>
    );
}
