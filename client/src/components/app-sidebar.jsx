import * as React from "react"
import { useState } from "react"
import {
    TrendingUp,
    Wallet,
    LogOut,
    CircleUser,
    CalendarDays,
    Settings,
    Bell,
    CreditCard,
    PiggyBank,
    ShieldCheck,
    LifeBuoy,
    Newspaper,
} from "lucide-react"
import { Link, useLocation, useNavigate } from "react-router-dom"

import {
    Sidebar,
    SidebarContent,
    SidebarFooter,
    SidebarHeader,
    SidebarMenu,
    SidebarMenuButton,
    SidebarMenuItem,
    SidebarGroup,
    SidebarGroupLabel,
} from "@/components/ui/sidebar"

import {
    Dialog,
    DialogContent,
    DialogDescription,
    DialogFooter,
    DialogHeader,
    DialogTitle,
} from "@/components/ui/dialog"

import { Button } from "@/components/ui/button"
import axiosInstance from "@/utils/axiosConfig"
import { getStoredUserType, isAdminUserType, persistSessionUser } from "@/utils/userSession"

const navItems = [
    {
        title: "Dashboard",
        url: "/home",
        icon: TrendingUp,
    },
    {
        title: "Meus Investimentos",
        url: "/meus-investimentos",
        icon: Wallet,
    },
    {
        title: "Cofrinhos",
        url: "/cofrinhos",
        icon: PiggyBank,
    },
    {
        title: "Calendário",
        url: "/calendar",
        icon: CalendarDays,
    },
    {
        title: "Notificações",
        url: "/notifications",
        icon: Bell,
    },
    {
        title: "Configurações",
        url: "/settings",
        icon: Settings,
    },
    {
        title: "Assinatura",
        url: "/subscription",
        icon: CreditCard,
    },
    {
        title: "Atendimento",
        url: "/atendimento",
        icon: LifeBuoy,
    },
]

export function AppSidebar({ ...props }) {
    const location = useLocation()
    const navigate = useNavigate()
    const [userName, setUserName] = useState(() => sessionStorage.getItem('name') || 'Usuário')
    const [userEmail, setUserEmail] = useState(() => sessionStorage.getItem('email') || '')
    const [userType, setUserType] = useState(() => getStoredUserType())

    const [showLogoutDialog, setShowLogoutDialog] = useState(false)

    React.useEffect(() => {
        const storedName = sessionStorage.getItem("name") || ""
        const storedEmail = sessionStorage.getItem("email") || ""
        const storedUserType = getStoredUserType()

        if (storedName) {
            setUserName(storedName)
        }

        if (storedEmail) {
            setUserEmail(storedEmail)
        }

        if (storedUserType) {
            setUserType(storedUserType)
        }

        if (storedName && storedEmail && storedUserType) {
            return
        }

        let cancelled = false

        axiosInstance
            .get("/User/Settings")
            .then((response) => {
                if (cancelled) return

                const data = response?.data || {}
                const nextName = data.name || "Usuário"
                const nextEmail = data.email || ""
                const nextUserType = data.user_type || ""

                setUserName(nextName)
                setUserEmail(nextEmail)
                setUserType(nextUserType)

                persistSessionUser({
                    name: data.name,
                    email: data.email,
                    user_type: nextUserType,
                })
            })
            .catch(() => {
                if (cancelled) return
            })

        return () => {
            cancelled = true
        }
    }, [])

    const handleLogoutClick = (e) => {
        e.preventDefault()
        setShowLogoutDialog(true)
    }

    const handleConfirmLogout = () => {
        setShowLogoutDialog(false)
        navigate('/logout')
    }

    const items = isAdminUserType(userType)
        ? [
            ...navItems,
            {
                title: "Admin",
                url: "/admin",
                icon: ShieldCheck,
            },
            {
                title: "Blog",
                url: "/admin/blog",
                icon: Newspaper,
            },
        ]
        : navItems

    return (
        <>
            <Sidebar {...props}>
                <SidebarHeader>
                    <SidebarMenu>
                        <SidebarMenuItem>
                            <SidebarMenuButton size="lg" asChild className="overflow-visible">
                                <Link to="/home">
                                    <div className="flex aspect-square size-7 min-h-7 min-w-7 shrink-0 items-center justify-center overflow-hidden rounded-md">
                                        <img src="/favicon.svg" alt="RendaTop" className="size-7 min-h-7 min-w-7 shrink-0" />
                                    </div>
                                    <div className="grid flex-1 text-left text-sm leading-tight">
                                        <span className="truncate font-medium">RendaTop</span>
                                        <span className="truncate text-xs">Gestão de Investimentos</span>
                                    </div>
                                </Link>
                            </SidebarMenuButton>
                        </SidebarMenuItem>
                    </SidebarMenu>
                </SidebarHeader>
                <SidebarContent>
                    <SidebarGroup>
                        <SidebarGroupLabel>Menu</SidebarGroupLabel>
                        <SidebarMenu>
                            {items.map((item) => (
                                <SidebarMenuItem key={item.title}>
                                    <SidebarMenuButton
                                        asChild
                                        tooltip={item.title}
                                        className="cursor-pointer"
                                        isActive={location.pathname === item.url}
                                    >
                                        <Link to={item.url}>
                                            {item.icon && <item.icon />}
                                            <span>{item.title}</span>
                                        </Link>
                                    </SidebarMenuButton>
                                </SidebarMenuItem>
                            ))}
                        </SidebarMenu>
                    </SidebarGroup>
                </SidebarContent>
                <SidebarFooter>
                    <SidebarMenu>
                        <SidebarMenuItem>
                            <SidebarMenuButton size="lg" className="cursor-default">
                                <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-primary/10">
                                    <CircleUser className="size-5" />
                                </div>
                                <div className="grid flex-1 text-left text-sm leading-tight">
                                    <span className="truncate font-medium">{userName}</span>
                                    <span className="text-muted-foreground truncate text-xs">
                                        {userEmail}
                                    </span>
                                </div>
                            </SidebarMenuButton>
                        </SidebarMenuItem>
                        <SidebarMenuItem>
                            <SidebarMenuButton
                                className="cursor-pointer text-muted-foreground hover:text-destructive"
                                onClick={handleLogoutClick}
                            >
                                <LogOut className="size-4" />
                                <span>Sair</span>
                            </SidebarMenuButton>
                        </SidebarMenuItem>
                    </SidebarMenu>
                </SidebarFooter>
            </Sidebar>

            {/* Logout confirmation dialog */}
            <Dialog open={showLogoutDialog} onOpenChange={setShowLogoutDialog}>
                <DialogContent className="sm:max-w-sm">
                    <DialogHeader>
                        <DialogTitle className="flex items-center gap-2">
                            <LogOut className="h-5 w-5 text-destructive" />
                            Sair da conta
                        </DialogTitle>
                        <DialogDescription>
                            Tem certeza que deseja sair? Você precisará fazer login novamente para acessar sua conta.
                        </DialogDescription>
                    </DialogHeader>
                    <DialogFooter className="flex gap-2 sm:gap-2">
                        <Button
                            variant="outline"
                            className="flex-1"
                            onClick={() => setShowLogoutDialog(false)}
                        >
                            Cancelar
                        </Button>
                        <Button
                            variant="destructive"
                            className="flex-1"
                            onClick={handleConfirmLogout}
                        >
                            <LogOut className="h-4 w-4 mr-1" />
                            Sair
                        </Button>
                    </DialogFooter>
                </DialogContent>
            </Dialog>
        </>
    )
}
