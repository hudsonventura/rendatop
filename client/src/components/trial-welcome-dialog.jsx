import { useCallback, useEffect, useState } from "react"
import { Crown, Sparkles } from "lucide-react"

import { Button } from "@/components/ui/button"
import {
    Dialog,
    DialogContent,
    DialogDescription,
    DialogFooter,
    DialogHeader,
    DialogTitle,
} from "@/components/ui/dialog"
import axiosInstance from "@/utils/axiosConfig"

function formatExpirationDate(value) {
    if (!value) return ""

    return new Intl.DateTimeFormat("pt-BR", {
        day: "2-digit",
        month: "long",
        year: "numeric",
    }).format(new Date(value))
}

export function TrialWelcomeDialog() {
    const [trial, setTrial] = useState(null)
    const [open, setOpen] = useState(false)
    const [acknowledging, setAcknowledging] = useState(false)

    useEffect(() => {
        let active = true

        axiosInstance.get("/subscription/trial-welcome")
            .then(({ data }) => {
                if (!active || !data?.show) return
                setTrial(data)
                setOpen(true)
            })
            .catch(() => {
                // O aviso não deve impedir o carregamento do sistema.
            })

        return () => {
            active = false
        }
    }, [])

    const acknowledge = useCallback(async () => {
        if (acknowledging) return

        setOpen(false)
        setAcknowledging(true)

        try {
            await axiosInstance.post("/subscription/trial-welcome/acknowledge")
        } catch {
            // O fechamento do aviso não deve bloquear a navegação do usuário.
        } finally {
            setAcknowledging(false)
        }
    }, [acknowledging])

    return (
        <Dialog
            open={open}
            onOpenChange={(nextOpen) => {
                if (!nextOpen) acknowledge()
            }}
        >
            <DialogContent className="overflow-hidden p-0 sm:max-w-md">
                <div className="bg-gradient-to-br from-amber-50 via-background to-background px-6 pb-2 pt-7 dark:from-amber-950/30">
                    <div className="mb-5 flex h-12 w-12 items-center justify-center rounded-full bg-amber-100 text-amber-700 dark:bg-amber-900/50 dark:text-amber-300">
                        <Crown className="h-6 w-6" />
                    </div>
                    <DialogHeader>
                        <DialogTitle className="text-xl">Sua degustação está liberada!</DialogTitle>
                        <DialogDescription className="pt-1 text-sm leading-relaxed">
                            Você ganhou <strong className="text-foreground">30 dias do plano {trial?.plan_name}</strong> para aproveitar todas as funcionalidades do RendaTop.
                        </DialogDescription>
                    </DialogHeader>
                </div>

                <div className="space-y-4 px-6 pb-6">
                    <div className="flex items-start gap-3 rounded-lg border bg-muted/40 p-4">
                        <Sparkles className="mt-0.5 h-5 w-5 shrink-0 text-amber-600" />
                        <div className="space-y-1 text-sm">
                            <p className="font-medium">Acesso completo já disponível</p>
                            <p className="text-muted-foreground">
                                Sua degustação ficará ativa até {formatExpirationDate(trial?.expires_at)} e será encerrada automaticamente, sem cobrança.
                            </p>
                        </div>
                    </div>

                    <DialogFooter>
                        <Button className="w-full" onClick={acknowledge} disabled={acknowledging}>
                            Começar a aproveitar
                        </Button>
                    </DialogFooter>
                </div>
            </DialogContent>
        </Dialog>
    )
}
