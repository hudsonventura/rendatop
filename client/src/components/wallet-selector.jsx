import * as React from "react"
import { Plus, WalletCards } from "lucide-react"
import axiosInstance from "@/utils/axiosConfig"
import { useWallet } from "@/contexts/wallet-context"
import { Button } from "@/components/ui/button"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Tooltip, TooltipContent, TooltipTrigger } from "@/components/ui/tooltip"
import { UpgradePlanModal } from "@/components/upgrade-plan-modal"
import { cn } from "@/lib/utils"

const upgradeMessage = "Apenas usuarios de planos pagos podem criar mais carteiras e acessar limites extendidos."

export function WalletSelector({ className, triggerClassName }) {
    const {
        wallets,
        activeWalletId,
        loading,
        canCreate,
        restrictionMessage,
        setActiveWalletId,
        refreshWallets,
    } = useWallet()
    const [open, setOpen] = React.useState(false)
    const [name, setName] = React.useState("")
    const [error, setError] = React.useState("")
    const [upgradeOpen, setUpgradeOpen] = React.useState(false)
    const [saving, setSaving] = React.useState(false)

    const enabledWallets = wallets.filter((wallet) => wallet.enabled)

    const handleCreate = (event) => {
        event.preventDefault()
        setError("")
        setSaving(true)

        axiosInstance
            .post("/Wallets", { name })
            .then((response) => {
                const wallet = response?.data
                setOpen(false)
                setName("")
                return refreshWallets().then(() => {
                    if (wallet?.id) setActiveWalletId(wallet.id)
                })
            })
            .catch((err) => {
                const message = err?.response?.data?.message || err?.response?.data || "Não foi possível criar a carteira."
                setError(message)
                if (String(message).toLowerCase().includes("plano") || String(message).toLowerCase().includes("limite")) {
                    setOpen(false)
                    setUpgradeOpen(true)
                }
            })
            .finally(() => setSaving(false))
    }

    return (
        <div className={cn("flex items-center gap-1", className)}>
            <Select value={activeWalletId} onValueChange={setActiveWalletId} disabled={loading || enabledWallets.length === 0}>
                <SelectTrigger className={cn("h-9 w-[190px] max-w-[46vw]", triggerClassName)}>
                    <WalletCards className="mr-2 h-4 w-4 shrink-0" />
                    <SelectValue placeholder="Carteira" />
                </SelectTrigger>
                <SelectContent>
                    {enabledWallets.map((wallet) => (
                        <SelectItem key={wallet.id} value={wallet.id}>{wallet.name}</SelectItem>
                    ))}
                    {wallets.filter((wallet) => !wallet.enabled).map((wallet) => (
                        <SelectItem key={wallet.id} value={wallet.id} disabled>
                            {wallet.name} indisponível
                        </SelectItem>
                    ))}
                </SelectContent>
            </Select>

            <Tooltip>
                <TooltipTrigger asChild>
                    <Button
                        type="button"
                        variant="outline"
                        size="icon"
                        className="h-9 w-9"
                        onClick={() => {
                            setError("")
                            if (!canCreate) {
                                setUpgradeOpen(true)
                                return
                            }
                            setOpen(true)
                        }}
                        disabled={loading}
                    >
                        <Plus className="h-4 w-4" />
                        <span className="sr-only">Nova carteira</span>
                    </Button>
                </TooltipTrigger>
                <TooltipContent>{canCreate ? "Nova carteira" : restrictionMessage || "Limite de carteiras atingido"}</TooltipContent>
            </Tooltip>

            <Dialog open={open} onOpenChange={setOpen}>
                <DialogContent className="sm:max-w-sm">
                    <form onSubmit={handleCreate} className="space-y-4">
                        <DialogHeader>
                            <DialogTitle>Nova carteira</DialogTitle>
                        </DialogHeader>
                        <div className="space-y-2">
                            <Label htmlFor="wallet-name">Nome</Label>
                            <Input
                                id="wallet-name"
                                value={name}
                                onChange={(event) => setName(event.target.value)}
                                placeholder="Carteira de longo prazo"
                                autoFocus
                            />
                            {error && <p className="text-sm text-destructive">{error}</p>}
                        </div>
                        <DialogFooter>
                            <Button type="button" variant="outline" onClick={() => setOpen(false)}>Cancelar</Button>
                            <Button type="submit" disabled={saving || !name.trim()}>
                                {saving ? "Criando..." : "Criar"}
                            </Button>
                        </DialogFooter>
                    </form>
                </DialogContent>
            </Dialog>

            <UpgradePlanModal
                open={upgradeOpen}
                onOpenChange={setUpgradeOpen}
                message={restrictionMessage || upgradeMessage}
            />
        </div>
    )
}
