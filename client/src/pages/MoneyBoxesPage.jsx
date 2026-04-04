import React, { useEffect, useState } from "react"
import Logged from "@/components/Logged"
import { BaseLayout } from "@/components/layouts/base-layout"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert"
import { Badge } from "@/components/ui/badge"
import { Skeleton } from "@/components/ui/skeleton"
import { fetchMoneyBoxesOverview } from "@/utils/money-boxes"
import axiosInstance from "@/utils/axiosConfig"
import { AlertCircle, PiggyBank, Pencil, Plus, Trash2 } from "lucide-react"

const defaultForm = {
    name: "",
}

function formatCurrency(value) {
    return Number(value || 0).toLocaleString("pt-BR", {
        style: "currency",
        currency: "BRL",
    })
}

export default function MoneyBoxesPage() {
    const [loading, setLoading] = useState(true)
    const [saving, setSaving] = useState(false)
    const [error, setError] = useState("")
    const [overview, setOverview] = useState(null)
    const [open, setOpen] = useState(false)
    const [deleteOpen, setDeleteOpen] = useState(false)
    const [editingItem, setEditingItem] = useState(null)
    const [selectedItem, setSelectedItem] = useState(null)
    const [form, setForm] = useState(defaultForm)

    const loadData = () => {
        setLoading(true)
        setError("")

        fetchMoneyBoxesOverview()
            .then((data) => {
                setOverview(data)
            })
            .catch(() => {
                setError("Não foi possível carregar seus cofrinhos.")
            })
            .finally(() => {
                setLoading(false)
            })
    }

    useEffect(() => {
        loadData()
    }, [])

    const openCreate = () => {
        setEditingItem(null)
        setForm(defaultForm)
        setOpen(true)
    }

    const openEdit = (item) => {
        setEditingItem(item)
        setForm({ name: item.name ?? "" })
        setOpen(true)
    }

    const openDelete = (item) => {
        setSelectedItem(item)
        setDeleteOpen(true)
    }

    const handleSave = async (event) => {
        event.preventDefault()
        setSaving(true)
        setError("")

        try {
            if (editingItem) {
                await axiosInstance.patch(`/MoneyBoxes/${editingItem.id}`, form)
            } else {
                await axiosInstance.post("/MoneyBoxes", form)
            }

            setOpen(false)
            setEditingItem(null)
            setForm(defaultForm)
            loadData()
        } catch (err) {
            const message = typeof err?.response?.data === "string"
                ? err.response.data
                : "Não foi possível salvar o cofrinho."
            setError(message)
        } finally {
            setSaving(false)
        }
    }

    const handleDelete = async () => {
        if (!selectedItem) return
        setSaving(true)
        setError("")

        try {
            await axiosInstance.delete(`/MoneyBoxes/${selectedItem.id}`)
            setDeleteOpen(false)
            setSelectedItem(null)
            loadData()
        } catch (err) {
            const message = typeof err?.response?.data === "string"
                ? err.response.data
                : "Não foi possível excluir o cofrinho."
            setError(message)
        } finally {
            setSaving(false)
        }
    }

    return (
        <>
            <Logged />
            <BaseLayout title="Cofrinhos" description="Organize seus investimentos em objetivos personalizados">
                <div className="space-y-6 px-4 lg:px-6">
                    <Card>
                        <CardHeader>
                            <CardTitle className="flex items-center gap-2">
                                <PiggyBank className="h-5 w-5" />
                                O que e um cofrinho?
                            </CardTitle>
                            <CardDescription>
                                Cofrinhos são categorias livres para você agrupar investimentos por objetivo, como viagem, reserva de emergência, aquisição de bens ou casa propria.
                            </CardDescription>
                        </CardHeader>
                    </Card>

                    {error && (
                        <Alert variant="destructive">
                            <AlertCircle className="h-4 w-4" />
                            <AlertTitle>Erro</AlertTitle>
                            <AlertDescription>{error}</AlertDescription>
                        </Alert>
                    )}

                    {!loading && overview?.restriction_message && (
                        <Alert>
                            <PiggyBank className="h-4 w-4" />
                            <AlertTitle>Regras do plano</AlertTitle>
                            <AlertDescription>{overview.restriction_message}</AlertDescription>
                        </Alert>
                    )}

                    <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
                        <div className="flex items-center gap-2">
                            <h2 className="text-lg font-semibold tracking-tight">Seus cofrinhos</h2>
                            {!loading && (
                                <Badge variant="outline">
                                    {overview?.limit ? `${overview?.count ?? 0}/${overview.limit}` : `${overview?.count ?? 0} ilimitado`}
                                </Badge>
                            )}
                        </div>
                        <Button onClick={openCreate} disabled={loading || !overview?.can_create}>
                            <Plus className="mr-2 h-4 w-4" />
                            Novo cofrinho
                        </Button>
                    </div>

                    {loading ? (
                        <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
                            {Array.from({ length: 3 }).map((_, index) => (
                                <Card key={index}>
                                    <CardHeader className="space-y-2">
                                        <Skeleton className="h-5 w-40" />
                                        <Skeleton className="h-4 w-56" />
                                    </CardHeader>
                                    <CardContent>
                                        <Skeleton className="h-9 w-full" />
                                    </CardContent>
                                </Card>
                            ))}
                        </div>
                    ) : overview?.items?.length ? (
                        <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
                            {overview.items.map((item) => (
                                <Card key={item.id}>
                                    <CardHeader>
                                        <CardTitle className="flex items-center gap-2 text-base">
                                            <PiggyBank className="h-4 w-4" />
                                            {item.name}
                                        </CardTitle>
                                        <CardDescription>
                                            Criado em {new Date(item.created_at).toLocaleDateString("pt-BR")}
                                        </CardDescription>
                                    </CardHeader>
                                    <CardContent className="space-y-4">
                                        <div className="rounded-lg border bg-muted/30 p-3">
                                            <p className="text-xs uppercase tracking-wide text-muted-foreground">
                                                Valor liquido total
                                            </p>
                                            <p className="mt-1 text-xl font-semibold">
                                                {formatCurrency(item.total_liquid_value)}
                                            </p>
                                        </div>
                                        <div className="flex gap-2">
                                            <Button variant="outline" className="flex-1" onClick={() => openEdit(item)}>
                                                <Pencil className="mr-2 h-4 w-4" />
                                                Editar
                                            </Button>
                                            <Button variant="destructive" className="flex-1" onClick={() => openDelete(item)}>
                                                <Trash2 className="mr-2 h-4 w-4" />
                                                Excluir
                                            </Button>
                                        </div>
                                    </CardContent>
                                </Card>
                            ))}
                        </div>
                    ) : (
                        <Card>
                            <CardContent className="py-10 text-center text-sm text-muted-foreground">
                                Nenhum cofrinho criado ainda. Crie o primeiro para organizar sua carteira por objetivos.
                            </CardContent>
                        </Card>
                    )}
                </div>
            </BaseLayout>

            <Dialog open={open} onOpenChange={setOpen}>
                <DialogContent className="sm:max-w-md">
                    <DialogHeader>
                        <DialogTitle>{editingItem ? "Editar cofrinho" : "Novo cofrinho"}</DialogTitle>
                        <DialogDescription>
                            Escolha um nome simples para representar esse objetivo na sua carteira.
                        </DialogDescription>
                    </DialogHeader>
                    <form onSubmit={handleSave} className="space-y-4">
                        <div className="space-y-2">
                            <Label htmlFor="moneyBoxName">Nome</Label>
                            <Input
                                id="moneyBoxName"
                                value={form.name}
                                onChange={(event) => setForm({ name: event.target.value })}
                                placeholder="Ex.: Reserva de emergencia"
                            />
                        </div>
                        <DialogFooter>
                            <Button type="button" variant="outline" onClick={() => setOpen(false)}>
                                Cancelar
                            </Button>
                            <Button type="submit" disabled={saving}>
                                {saving ? "Salvando..." : editingItem ? "Salvar" : "Criar cofrinho"}
                            </Button>
                        </DialogFooter>
                    </form>
                </DialogContent>
            </Dialog>

            <Dialog open={deleteOpen} onOpenChange={setDeleteOpen}>
                <DialogContent className="sm:max-w-sm">
                    <DialogHeader>
                        <DialogTitle>Excluir cofrinho?</DialogTitle>
                        <DialogDescription>
                            Ao excluir este cofrinho, os investimentos vinculados continuam existindo e apenas perdem esse vínculo.
                        </DialogDescription>
                    </DialogHeader>
                    <DialogFooter>
                        <Button type="button" variant="outline" onClick={() => setDeleteOpen(false)}>
                            Cancelar
                        </Button>
                        <Button type="button" variant="destructive" disabled={saving} onClick={handleDelete}>
                            {saving ? "Excluindo..." : "Excluir"}
                        </Button>
                    </DialogFooter>
                </DialogContent>
            </Dialog>
        </>
    )
}
