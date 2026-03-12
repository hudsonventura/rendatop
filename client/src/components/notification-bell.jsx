import * as React from "react"
import { Bell } from "lucide-react"
import { Link } from "react-router-dom"
import axiosInstance from "@/utils/axiosConfig"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import {
    Popover,
    PopoverContent,
    PopoverTrigger,
} from "@/components/ui/popover"

function formatDateTime(dateStr) {
    return new Date(dateStr).toLocaleString("pt-BR", {
        day: "2-digit",
        month: "2-digit",
        year: "numeric",
        hour: "2-digit",
        minute: "2-digit",
    })
}

export function NotificationBell() {
    const [open, setOpen] = React.useState(false)
    const [notifications, setNotifications] = React.useState([])
    const [unreadCount, setUnreadCount] = React.useState(0)
    const [loading, setLoading] = React.useState(false)

    const load = React.useCallback(async () => {
        setLoading(true)
        try {
            const [itemsRes, unreadRes] = await Promise.all([
                axiosInstance.get("/Notifications?limit=30&unread_only=true"),
                axiosInstance.get("/Notifications/UnreadCount"),
            ])
            setNotifications(itemsRes.data ?? [])
            setUnreadCount(unreadRes?.data?.unread_count ?? 0)
        } finally {
            setLoading(false)
        }
    }, [])

    React.useEffect(() => {
        load()
        const interval = setInterval(load, 60000)
        return () => clearInterval(interval)
    }, [load])

    React.useEffect(() => {
        if (open) load()
    }, [open, load])

    const markAsRead = async (id) => {
        const response = await axiosInstance.post(`/Notifications/${id}/Read`)
        setUnreadCount(response?.data?.unread_count ?? unreadCount)
        setNotifications((prev) => prev.filter((n) => n.id !== id))
    }

    const markAllAsRead = async () => {
        const response = await axiosInstance.post("/Notifications/ReadAll")
        setUnreadCount(response?.data?.unread_count ?? 0)
        setNotifications([])
    }

    return (
        <Popover open={open} onOpenChange={setOpen}>
            <PopoverTrigger asChild>
                <Button variant="outline" size="icon" className="relative cursor-pointer">
                    <Bell className="h-[1.1rem] w-[1.1rem]" />
                    {unreadCount > 0 && (
                        <span className="absolute -top-1 -right-1">
                            <Badge className="h-5 min-w-5 px-1 text-[10px] leading-none bg-red-600 text-white border-red-600">
                                {unreadCount > 99 ? "99+" : unreadCount}
                            </Badge>
                        </span>
                    )}
                    <span className="sr-only">Notificações</span>
                </Button>
            </PopoverTrigger>
            <PopoverContent align="end" className="w-[360px] p-0">
                <div className="flex items-center justify-between border-b px-3 py-2">
                    <div className="text-sm font-medium">Notificações</div>
                    <div className="flex items-center gap-1">
                        <Button
                            type="button"
                            variant="ghost"
                            size="sm"
                            className="h-8 px-2 text-xs"
                            onClick={markAllAsRead}
                            disabled={unreadCount === 0}
                        >
                            Marcar todas
                        </Button>
                        <Button asChild type="button" variant="ghost" size="sm" className="h-8 px-2 text-xs">
                            <Link to="/notifications" onClick={() => setOpen(false)}>
                                Ver todas
                            </Link>
                        </Button>
                    </div>
                </div>

                <div className="max-h-[380px] overflow-y-auto">
                    {loading && notifications.length === 0 ? (
                        <div className="px-3 py-4 text-sm text-muted-foreground">Carregando...</div>
                    ) : notifications.length === 0 ? (
                        <div className="px-3 py-4 text-sm text-muted-foreground">Sem notificações não lidas.</div>
                    ) : (
                        notifications.map((notification) => (
                            <button
                                key={notification.id}
                                type="button"
                                className="w-full border-b bg-blue-500/5 px-3 py-3 text-left transition-colors hover:bg-muted/50"
                                onClick={() => markAsRead(notification.id)}
                            >
                                <div className="flex items-start justify-between gap-2">
                                    <p className="text-sm font-medium">{notification.title}</p>
                                    <span className="mt-1 h-2 w-2 rounded-full bg-blue-600" />
                                </div>
                                <p className="mt-1 whitespace-pre-line text-xs text-muted-foreground">
                                    {notification.message}
                                </p>
                                <p className="mt-2 text-[11px] text-muted-foreground">
                                    {formatDateTime(notification.created_at)}
                                </p>
                            </button>
                        ))
                    )}
                </div>
            </PopoverContent>
        </Popover>
    )
}
