import React, { useEffect, useState } from "react"
import Logged from "@/components/Logged"
import { BaseLayout } from "@/components/layouts/base-layout"
import axiosInstance from "@/utils/axiosConfig"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import {
    Table,
    TableBody,
    TableCell,
    TableHead,
    TableHeader,
    TableRow,
} from "@/components/ui/table"

function formatDateTime(dateStr) {
    return new Date(dateStr).toLocaleString("pt-BR", {
        day: "2-digit",
        month: "2-digit",
        year: "numeric",
        hour: "2-digit",
        minute: "2-digit",
    })
}

export default function NotificationsPage() {
    const [notifications, setNotifications] = useState([])
    const [loading, setLoading] = useState(true)

    const load = () => {
        setLoading(true)
        axiosInstance
            .get("/Notifications?limit=100")
            .then((response) => {
                setNotifications(response.data ?? [])
            })
            .finally(() => {
                setLoading(false)
            })
    }

    useEffect(() => {
        load()
    }, [])

    const markAsRead = async (id, isRead) => {
        if (isRead) return
        await axiosInstance.post(`/Notifications/${id}/Read`)
        setNotifications((prev) =>
            prev.map((item) =>
                item.id === id ? { ...item, is_read: true, read_at: new Date().toISOString() } : item
            )
        )
    }

    const markAllAsRead = async () => {
        await axiosInstance.post("/Notifications/ReadAll")
        setNotifications((prev) => prev.map((item) => ({ ...item, is_read: true, read_at: new Date().toISOString() })))
    }

    const unread = notifications.filter((item) => !item.is_read).length

    return (
        <>
            <Logged />
            <BaseLayout title="Notificações" description="Histórico completo de notificações da sua conta">
                <div className="px-4 lg:px-6 space-y-4">
                    <div className="flex items-center justify-between">
                        <div className="flex items-center gap-2">
                            <Badge variant="secondary">Não lidas: {unread}</Badge>
                            <Badge variant="outline">Total: {notifications.length}</Badge>
                        </div>
                        <div className="flex items-center gap-2">
                            <Button type="button" variant="outline" onClick={load}>
                                Atualizar
                            </Button>
                            <Button type="button" onClick={markAllAsRead} disabled={unread === 0}>
                                Marcar todas como lidas
                            </Button>
                        </div>
                    </div>

                    <div className="overflow-hidden rounded-lg border">
                        <Table>
                            <TableHeader>
                                <TableRow>
                                    <TableHead>Status</TableHead>
                                    <TableHead>Título</TableHead>
                                    <TableHead>Mensagem</TableHead>
                                    <TableHead>Data</TableHead>
                                    <TableHead className="text-right">Ação</TableHead>
                                </TableRow>
                            </TableHeader>
                            <TableBody>
                                {loading ? (
                                    <TableRow>
                                        <TableCell colSpan={5} className="text-center text-muted-foreground">
                                            Carregando notificações...
                                        </TableCell>
                                    </TableRow>
                                ) : notifications.length === 0 ? (
                                    <TableRow>
                                        <TableCell colSpan={5} className="text-center text-muted-foreground">
                                            Nenhuma notificação encontrada.
                                        </TableCell>
                                    </TableRow>
                                ) : (
                                    notifications.map((notification) => (
                                        <TableRow key={notification.id}>
                                            <TableCell>
                                                {notification.is_read ? (
                                                    <Badge variant="outline">Lida</Badge>
                                                ) : (
                                                    <Badge className="bg-blue-600 text-white border-blue-600">Não lida</Badge>
                                                )}
                                            </TableCell>
                                            <TableCell className="font-medium">{notification.title}</TableCell>
                                            <TableCell className="whitespace-pre-line text-muted-foreground">
                                                {notification.message}
                                            </TableCell>
                                            <TableCell>{formatDateTime(notification.created_at)}</TableCell>
                                            <TableCell className="text-right">
                                                <Button
                                                    type="button"
                                                    size="sm"
                                                    variant="outline"
                                                    disabled={notification.is_read}
                                                    onClick={() => markAsRead(notification.id, notification.is_read)}
                                                >
                                                    Marcar lida
                                                </Button>
                                            </TableCell>
                                        </TableRow>
                                    ))
                                )}
                            </TableBody>
                        </Table>
                    </div>
                </div>
            </BaseLayout>
        </>
    )
}
