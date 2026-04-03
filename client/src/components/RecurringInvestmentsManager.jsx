import React, { useEffect, useMemo, useState } from "react"
import axiosInstance from "@/utils/axiosConfig"
import { getCachedBanks, primeBanksCache } from "@/utils/banksCache"
import BankCombobox from "@/components/BankCombobox"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle, DialogTrigger } from "@/components/ui/dialog"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Checkbox } from "@/components/ui/checkbox"
import { Switch } from "@/components/ui/switch"
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert"
import { Badge } from "@/components/ui/badge"
import { Skeleton } from "@/components/ui/skeleton"
import { AlertCircle, Plus, Repeat, Trash2, Pencil } from "lucide-react"

const weekDays = [
    { value: 0, label: "Domingo" },
    { value: 1, label: "Segunda-feira" },
    { value: 2, label: "Terça-feira" },
    { value: 3, label: "Quarta-feira" },
    { value: 4, label: "Quinta-feira" },
    { value: 5, label: "Sexta-feira" },
    { value: 6, label: "Sábado" },
]

const monthsList = [
    { value: 1, label: "Jan" },
    { value: 2, label: "Fev" },
    { value: 3, label: "Mar" },
    { value: 4, label: "Abr" },
    { value: 5, label: "Mai" },
    { value: 6, label: "Jun" },
    { value: 7, label: "Jul" },
    { value: 8, label: "Ago" },
    { value: 9, label: "Set" },
    { value: 10, label: "Out" },
    { value: 11, label: "Nov" },
    { value: 12, label: "Dez" },
]

const allMonthValues = monthsList.map((month) => month.value)

const defaultForm = {
    title: "",
    bank_code: undefined,
    value: "",
    index: "0",
    index_percent: "",
    taxes: true,
    liquidity_daily: false,
    duration_days: "",
    frequency: "1",
    weekdays: [],
    day_of_month: "1",
    months: allMonthValues,
    active: true,
}

function formatCurrency(value) {
    return Number(value || 0).toLocaleString("pt-BR", {
        style: "currency",
        currency: "BRL",
    })
}

function formatDate(value) {
    if (!value) return "Ainda não gerado"
    const date = new Date(value)
    return Number.isNaN(date.getTime()) ? "Ainda não gerado" : date.toLocaleDateString("pt-BR")
}

function getIndexLabel(index, percent) {
    const formatted = Number(percent || 0).toLocaleString("pt-BR", {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2,
    })

    if (Number(index) === 0) return `${formatted}% CDI`
    if (Number(index) === 1) return `IPCA + ${formatted}%`
    if (Number(index) === 3) return `CDI + ${formatted}% a.a.`
    return `${formatted}% a.a.`
}

function getFrequencyLabel(item) {
    if (item.frequency === 0) {
        const days = (item.weekdays || [])
            .map(dayValue => weekDays.find(entry => entry.value === dayValue)?.label)
            .filter(Boolean)
            .join(", ")
        return `Semanal · ${days || "Nenhum dia selecionado"}`
    }

    const months = (item.months || [])
        .map(month => monthsList.find(entry => entry.value === month)?.label)
        .filter(Boolean)
        .join(", ")

    return `Mensal · dia ${item.day_of_month} · ${months || "Sem meses"}`
}

function buildFormFromItem(item) {
    return {
        title: item.title || "",
        bank_code: item.bank_code,
        value: String(item.value ?? ""),
        index: String(item.index ?? 0),
        index_percent: String(item.index_percent ?? ""),
        taxes: Boolean(item.taxes),
        liquidity_daily: Boolean(item.liquidity_daily),
        duration_days: item.duration_days ? String(item.duration_days) : "",
        frequency: String(item.frequency ?? 1),
        weekdays: item.weekdays?.length ? item.weekdays : [],
        day_of_month: String(item.day_of_month ?? 1),
        months: item.months?.length ? item.months : allMonthValues,
        active: Boolean(item.active),
    }
}

export default function RecurringInvestmentsManager() {
    const [loading, setLoading] = useState(true)
    const [saving, setSaving] = useState(false)
    const [error, setError] = useState("")
    const [items, setItems] = useState([])
    const [enabled, setEnabled] = useState(false)
    const [banks, setBanks] = useState([])
    const [open, setOpen] = useState(false)
    const [editingItem, setEditingItem] = useState(null)
    const [form, setForm] = useState(defaultForm)

    const frequency = Number(form.frequency)

    const loadData = () => {
        setLoading(true)
        setError("")

        Promise.all([
            axiosInstance.get("/Investments/Recurring"),
            getCachedBanks().then((response) => {
                primeBanksCache(response)
                return response
            }),
        ])
            .then(([recurringResponse, banksResponse]) => {
                const data = recurringResponse?.data || {}
                setItems(data.items || [])
                setEnabled(Boolean(data.recurring_investments_enabled))
                setBanks(banksResponse || [])
            })
            .catch((err) => {
                console.error(err)
                setError("Não foi possível carregar os investimentos recorrentes.")
            })
            .finally(() => {
                setLoading(false)
            })
    }

    useEffect(() => {
        loadData()
    }, [])

    const sortedItems = useMemo(
        () => [...items].sort((a, b) => Number(b.active) - Number(a.active) || a.title.localeCompare(b.title)),
        [items]
    )

    const resetForm = () => {
        setForm(defaultForm)
        setEditingItem(null)
    }

    const openCreate = () => {
        resetForm()
        setOpen(true)
    }

    const openEdit = (item) => {
        setForm(buildFormFromItem(item))
        setEditingItem(item)
        setOpen(true)
    }

    const handleMonthsToggle = (month) => {
        setForm((current) => {
            const exists = current.months.includes(month)
            return {
                ...current,
                months: exists
                    ? current.months.filter((item) => item !== month)
                    : [...current.months, month].sort((a, b) => a - b),
            }
        })
    }

    const handleWeekdayToggle = (weekday) => {
        setForm((current) => {
            const exists = current.weekdays.includes(weekday)
            return {
                ...current,
                weekdays: exists
                    ? current.weekdays.filter((item) => item !== weekday)
                    : [...current.weekdays, weekday].sort((a, b) => a - b),
            }
        })
    }

    const validateForm = () => {
        if (!form.title.trim()) return "Título é obrigatório."
        if (!form.bank_code) return "Selecione um banco."
        if (!form.value || Number(form.value) <= 0) return "Informe um valor de investimento maior que zero."
        if (!form.index_percent || Number(form.index_percent) < 0) return "Informe um valor válido para o indexador."
        if (!form.liquidity_daily && (!form.duration_days || Number(form.duration_days) <= 0)) {
            return "Informe a duração em dias quando não houver liquidez diária."
        }
        if (frequency === 0 && !form.weekdays.length) {
            return "Selecione pelo menos um dia da semana."
        }
        if (frequency === 1) {
            if (!form.day_of_month || Number(form.day_of_month) < 1 || Number(form.day_of_month) > 31) {
                return "Informe um dia do mês entre 1 e 31."
            }
            if (!form.months.length) {
                return "Selecione pelo menos um mês."
            }
        }
        return ""
    }

    const handleSubmit = (event) => {
        event.preventDefault()
        setError("")

        const validationError = validateForm()
        if (validationError) {
            setError(validationError)
            return
        }

        const payload = {
            title: form.title.trim(),
            bank_code: Number(form.bank_code),
            value: Number(form.value),
            index: Number(form.index),
            index_percent: Number(form.index_percent),
            index_value: 0,
            taxes: form.taxes,
            liquidity_daily: form.liquidity_daily,
            duration_days: form.liquidity_daily ? null : Number(form.duration_days),
            frequency,
            weekdays: frequency === 0 ? form.weekdays : [],
            day_of_month: frequency === 1 ? Number(form.day_of_month) : null,
            months: frequency === 1 ? form.months : [],
            active: form.active,
        }

        setSaving(true)
        const request = editingItem
            ? axiosInstance.patch(`/Investments/Recurring/${editingItem.id}`, payload)
            : axiosInstance.post("/Investments/Recurring", payload)

        request
            .then(() => {
                setOpen(false)
                resetForm()
                loadData()
            })
            .catch((err) => {
                setError(typeof err?.response?.data === "string"
                    ? err.response.data
                    : "Não foi possível salvar a recorrência.")
            })
            .finally(() => {
                setSaving(false)
            })
    }

    const handleToggleActive = (item, active) => {
        setError("")
        axiosInstance
            .patch(`/Investments/Recurring/${item.id}/active`, { active })
            .then(() => loadData())
            .catch((err) => {
                setError(typeof err?.response?.data === "string"
                    ? err.response.data
                    : "Não foi possível atualizar a recorrência.")
            })
    }

    const handleDelete = (item) => {
        if (!window.confirm(`Deseja excluir a recorrência "${item.title}"?`)) return

        setError("")
        axiosInstance
            .delete(`/Investments/Recurring/${item.id}`)
            .then(() => loadData())
            .catch((err) => {
                setError(typeof err?.response?.data === "string"
                    ? err.response.data
                    : "Não foi possível excluir a recorrência.")
            })
    }

    return (
        <div className="space-y-6">
            {!enabled && (
                <Alert>
                    <Repeat className="h-4 w-4" />
                    <AlertTitle>Recurso Premium</AlertTitle>
                    <AlertDescription>
                        Investimentos recorrentes exigem um plano pago ativo. Se você já tinha recorrências cadastradas, elas continuam visíveis aqui, mas a geração automática fica pausada sem um plano elegível.
                    </AlertDescription>
                </Alert>
            )}

            {error && (
                <Alert variant="destructive">
                    <AlertCircle className="h-4 w-4" />
                    <AlertTitle>Erro</AlertTitle>
                    <AlertDescription>{error}</AlertDescription>
                </Alert>
            )}

            <div className="flex items-center justify-between gap-3">
                <div>
                    <h3 className="text-lg font-semibold">Investimentos recorrentes</h3>
                    <p className="text-sm text-muted-foreground">
                        Cadastre modelos semanais ou mensais para gerar investimentos automaticamente todos os dias às 06:00 UTC.
                    </p>
                </div>

                <Dialog open={open} onOpenChange={setOpen}>
                    <DialogTrigger asChild>
                        <Button type="button" size="sm" onClick={openCreate} disabled={!enabled}>
                            <Plus className="mr-1 h-4 w-4" />
                            Nova recorrência
                        </Button>
                    </DialogTrigger>
                    <DialogContent className="w-[95vw] sm:max-w-4xl max-h-[90vh] overflow-y-auto">
                        <DialogHeader>
                            <DialogTitle>{editingItem ? "Editar recorrência" : "Nova recorrência"}</DialogTitle>
                            <DialogDescription>
                                Configure quando e como o investimento será criado automaticamente.
                            </DialogDescription>
                        </DialogHeader>

                        <form onSubmit={handleSubmit} className="space-y-6">
                            <div className="space-y-2">
                                <Label htmlFor="title">Título</Label>
                                <Input
                                    id="title"
                                    value={form.title}
                                    onChange={(event) => setForm((current) => ({ ...current, title: event.target.value }))}
                                    placeholder="Ex.: Aporte semanal CDI"
                                />
                            </div>

                            <div className="grid gap-4 md:grid-cols-2">
                                <div className="space-y-2">
                                    <Label>Banco</Label>
                                    <BankCombobox
                                        banks={banks}
                                        value={form.bank_code}
                                        onChange={(value) => setForm((current) => ({ ...current, bank_code: value }))}
                                    />
                                </div>

                                <div className="space-y-2">
                                        <Label htmlFor="frequency">Frequência</Label>
                                        <Select
                                            value={form.frequency}
                                            onValueChange={(value) => setForm((current) => ({
                                                ...current,
                                                frequency: value,
                                                weekdays: value === "0" ? [] : current.weekdays,
                                                months: value === "1" && (!current.months?.length)
                                                    ? allMonthValues
                                                    : current.months,
                                            }))}
                                        >
                                        <SelectTrigger className="w-full">
                                            <SelectValue placeholder="Selecione a frequência" />
                                        </SelectTrigger>
                                        <SelectContent>
                                            <SelectItem value="0">Semanal</SelectItem>
                                            <SelectItem value="1">Mensal</SelectItem>
                                        </SelectContent>
                                    </Select>
                                </div>
                            </div>

                            <div className="grid gap-4 md:grid-cols-3">
                                <div className="space-y-2">
                                    <Label htmlFor="value">Valor do investimento</Label>
                                    <Input
                                        id="value"
                                        type="number"
                                        min="0"
                                        step="0.01"
                                        value={form.value}
                                        onChange={(event) => setForm((current) => ({ ...current, value: event.target.value }))}
                                    />
                                </div>

                                <div className="space-y-2">
                                    <Label htmlFor="index">Indexador</Label>
                                    <Select
                                        value={form.index}
                                        onValueChange={(value) => setForm((current) => ({ ...current, index: value }))}
                                    >
                                        <SelectTrigger className="w-full">
                                            <SelectValue placeholder="Selecione o indexador" />
                                        </SelectTrigger>
                                        <SelectContent>
                                            <SelectItem value="0">CDI</SelectItem>
                                            <SelectItem value="1">IPCA+</SelectItem>
                                            <SelectItem value="2">% ao ano</SelectItem>
                                            <SelectItem value="3">CDI + %a.a.</SelectItem>
                                        </SelectContent>
                                    </Select>
                                </div>

                                <div className="space-y-2">
                                    <Label htmlFor="index_percent">Valor do indexador</Label>
                                    <Input
                                        id="index_percent"
                                        type="number"
                                        min="0"
                                        step="0.01"
                                        value={form.index_percent}
                                        onChange={(event) => setForm((current) => ({ ...current, index_percent: event.target.value }))}
                                    />
                                </div>
                            </div>

                            <div className="grid gap-4 md:grid-cols-3">
                                <div className="space-y-2">
                                    <Label htmlFor="duration_days">Duração em dias</Label>
                                    <Input
                                        id="duration_days"
                                        type="number"
                                        min="1"
                                        step="1"
                                        value={form.duration_days}
                                        disabled={form.liquidity_daily}
                                        onChange={(event) => setForm((current) => ({ ...current, duration_days: event.target.value }))}
                                    />
                                </div>

                                <label className="flex items-end gap-2 rounded-md border p-3 text-sm">
                                    <Checkbox
                                        checked={form.liquidity_daily}
                                        onCheckedChange={(checked) => setForm((current) => ({
                                            ...current,
                                            liquidity_daily: Boolean(checked),
                                            duration_days: checked ? "" : current.duration_days,
                                        }))}
                                    />
                                    Liquidez diária
                                </label>

                                <label className="flex items-end gap-2 rounded-md border p-3 text-sm">
                                    <Checkbox
                                        checked={form.taxes}
                                        onCheckedChange={(checked) => setForm((current) => ({ ...current, taxes: Boolean(checked) }))}
                                    />
                                    Possui impostos
                                </label>
                            </div>

                            {frequency === 0 ? (
                                <div className="space-y-4 rounded-lg border p-4">
                                    <div className="space-y-2">
                                        <Label>Dias da semana</Label>
                                        <div className="grid grid-cols-2 gap-3 md:grid-cols-4">
                                            {weekDays.map((day) => (
                                                <label key={day.value} className="flex items-center gap-2 rounded-md border p-3 text-sm">
                                                    <Checkbox
                                                        checked={form.weekdays.includes(day.value)}
                                                        onCheckedChange={() => handleWeekdayToggle(day.value)}
                                                    />
                                                    {day.label}
                                                </label>
                                            ))}
                                        </div>
                                        <p className="text-xs text-muted-foreground">
                                            Selecione um ou mais dias da semana para a geração automática.
                                        </p>
                                    </div>
                                </div>
                            ) : (
                                <div className="space-y-4 rounded-lg border p-4">
                                    <div className="space-y-2">
                                        <Label htmlFor="day_of_month">Dia do mês</Label>
                                        <Input
                                            id="day_of_month"
                                            type="number"
                                            min="1"
                                            max="31"
                                            value={form.day_of_month}
                                            onChange={(event) => setForm((current) => ({ ...current, day_of_month: event.target.value }))}
                                        />
                                        <p className="text-xs text-muted-foreground">
                                            Se o mês selecionado não tiver esse dia, a geração acontecerá no último dia disponível.
                                        </p>
                                    </div>

                                    <div className="space-y-2">
                                        <Label>Meses da recorrência</Label>
                                        <div className="grid grid-cols-3 gap-3 md:grid-cols-4">
                                            {monthsList.map((month) => (
                                                <label key={month.value} className="flex items-center gap-2 rounded-md border p-3 text-sm">
                                                    <Checkbox
                                                        checked={form.months.includes(month.value)}
                                                        onCheckedChange={() => handleMonthsToggle(month.value)}
                                                    />
                                                    {month.label}
                                                </label>
                                            ))}
                                        </div>
                                    </div>
                                </div>
                            )}

                            <label className="flex items-center justify-between rounded-md border p-4 text-sm">
                                <div>
                                    <div className="font-medium">Recorrência ativa</div>
                                    <div className="text-muted-foreground">
                                        Quando ativa, o serviço diário poderá gerar o investimento automaticamente.
                                    </div>
                                </div>
                                <Switch
                                    checked={form.active}
                                    onCheckedChange={(checked) => setForm((current) => ({ ...current, active: Boolean(checked) }))}
                                />
                            </label>

                            <div className="flex justify-end gap-2">
                                <Button
                                    type="button"
                                    variant="outline"
                                    onClick={() => {
                                        setOpen(false)
                                        resetForm()
                                    }}
                                >
                                    Cancelar
                                </Button>
                                <Button type="submit" disabled={saving}>
                                    {saving ? "Salvando..." : editingItem ? "Salvar alterações" : "Criar recorrência"}
                                </Button>
                            </div>
                        </form>
                    </DialogContent>
                </Dialog>
            </div>

            {loading ? (
                <div className="grid gap-4">
                    {Array.from({ length: 3 }).map((_, index) => (
                        <Skeleton key={index} className="h-40 w-full rounded-xl" />
                    ))}
                </div>
            ) : sortedItems.length === 0 ? (
                <Card>
                    <CardHeader>
                        <CardTitle>Nenhuma recorrência cadastrada</CardTitle>
                        <CardDescription>
                            Crie uma recorrência semanal ou mensal para gerar investimentos automaticamente sem usar IA.
                        </CardDescription>
                    </CardHeader>
                </Card>
            ) : (
                <div className="grid gap-4">
                    {sortedItems.map((item) => (
                        <Card key={item.id}>
                            <CardHeader className="gap-3 md:flex-row md:items-start md:justify-between">
                                <div className="space-y-2">
                                    <div className="flex flex-wrap items-center gap-2">
                                        <CardTitle className="text-lg">{item.title}</CardTitle>
                                        <Badge variant={item.active ? "default" : "secondary"}>
                                            {item.active ? "Ativa" : "Pausada"}
                                        </Badge>
                                        {!enabled && (
                                            <Badge variant="outline">Plano pago inativo</Badge>
                                        )}
                                    </div>
                                    <CardDescription>{getFrequencyLabel(item)}</CardDescription>
                                </div>

                                <div className="flex items-center gap-2">
                                    <Button type="button" variant="outline" size="sm" onClick={() => openEdit(item)}>
                                        <Pencil className="mr-1 h-4 w-4" />
                                        Editar
                                    </Button>
                                    <Button type="button" variant="outline" size="sm" onClick={() => handleDelete(item)}>
                                        <Trash2 className="mr-1 h-4 w-4" />
                                        Excluir
                                    </Button>
                                </div>
                            </CardHeader>
                            <CardContent className="space-y-4">
                                <div className="grid gap-4 md:grid-cols-3">
                                    <div>
                                        <p className="text-xs text-muted-foreground">Banco</p>
                                        <p className="font-medium">{item.bank_name}</p>
                                    </div>
                                    <div>
                                        <p className="text-xs text-muted-foreground">Aplicação</p>
                                        <p className="font-medium">{formatCurrency(item.value)}</p>
                                    </div>
                                    <div>
                                        <p className="text-xs text-muted-foreground">Indexador</p>
                                        <p className="font-medium">{getIndexLabel(item.index, item.index_percent)}</p>
                                    </div>
                                </div>

                                <div className="grid gap-4 md:grid-cols-3">
                                    <div>
                                        <p className="text-xs text-muted-foreground">Duração</p>
                                        <p className="font-medium">
                                            {item.liquidity_daily ? "Liquidez diária" : `${item.duration_days} dia(s)`}
                                        </p>
                                    </div>
                                    <div>
                                        <p className="text-xs text-muted-foreground">Próxima geração</p>
                                        <p className="font-medium">{formatDate(item.next_occurrence_at)}</p>
                                    </div>
                                    <div>
                                        <p className="text-xs text-muted-foreground">Última geração</p>
                                        <p className="font-medium">{formatDate(item.last_generated_at)}</p>
                                    </div>
                                </div>

                                <div className="flex items-center justify-between rounded-md border p-3">
                                    <div>
                                        <p className="text-sm font-medium">Geração automática</p>
                                        <p className="text-xs text-muted-foreground">
                                            O serviço em background roda todos os dias às 06:00 UTC.
                                        </p>
                                    </div>
                                    <Switch
                                        checked={item.active}
                                        onCheckedChange={(checked) => handleToggleActive(item, checked)}
                                    />
                                </div>
                            </CardContent>
                        </Card>
                    ))}
                </div>
            )}
        </div>
    )
}
