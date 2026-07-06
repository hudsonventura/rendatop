import * as React from "react"
import { useState } from "react"
import {
    TrendingUp,
    Wallet,
    Repeat,
    LogOut,
    ChevronsUpDown,
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
    SidebarSeparator,
    SidebarMenu,
    SidebarMenuButton,
    SidebarMenuItem,
    SidebarGroup,
    SidebarGroupLabel,
    useSidebar,
} from "@/components/ui/sidebar"
import {
    DropdownMenu,
    DropdownMenuContent,
    DropdownMenuGroup,
    DropdownMenuItem,
    DropdownMenuLabel,
    DropdownMenuSeparator,
    DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"

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
import { WalletSelector } from "@/components/wallet-selector"

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
        title: "Investimentos Recorrentes",
        url: "/investimentos-recorrentes",
        icon: Repeat,
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
        title: "Atendimento",
        url: "/atendimento",
        icon: LifeBuoy,
    },
]

export function AppSidebar({ ...props }) {
    const { isMobile } = useSidebar()
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
        e?.preventDefault?.()
        setShowLogoutDialog(true)
    }

    const handleConfirmLogout = () => {
        setShowLogoutDialog(false)
        navigate('/logout')
    }

    const userMenuItems = [
        {
            title: "Assinatura",
            url: "/subscription",
            icon: CreditCard,
        },
        {
            title: "Configurações",
            url: "/settings",
            icon: Settings,
        },
        {
            title: "Notificações",
            url: "/notifications",
            icon: Bell,
        },
    ]

    const userInitials = React.useMemo(() => {
        const source = (userName || userEmail || "U").trim()
        if (!source) return "U"

        const parts = source.split(/\s+/).filter(Boolean)
        return parts.slice(0, 2).map((part) => part[0]?.toUpperCase() ?? "").join("") || "U"
    }, [userEmail, userName])

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
                    <div className="px-2 pt-1 group-data-[collapsible=icon]:hidden">
                        <WalletSelector
                            className="w-full"
                            triggerClassName="w-full max-w-none"
                        />
                    </div>
                    <SidebarSeparator className="group-data-[collapsible=icon]:hidden" />
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
                            <DropdownMenu>
                                <DropdownMenuTrigger asChild>
                                    <SidebarMenuButton
                                        size="lg"
                                        className="data-[state=open]:bg-sidebar-accent data-[state=open]:text-sidebar-accent-foreground"
                                    >
                                        <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-primary/10 text-primary">
                                            <span className="text-xs font-semibold">{userInitials}</span>
                                        </div>
                                        <div className="grid flex-1 text-left text-sm leading-tight">
                                            <span className="truncate font-medium">{userName}</span>
                                            <span className="text-muted-foreground truncate text-xs">
                                                {userEmail}
                                            </span>
                                        </div>
                                        <ChevronsUpDown className="ml-auto size-4" />
                                    </SidebarMenuButton>
                                </DropdownMenuTrigger>
                                <DropdownMenuContent
                                    className="w-[--radix-dropdown-menu-trigger-width] min-w-56 rounded-lg"
                                    side={isMobile ? "bottom" : "right"}
                                    align="end"
                                    sideOffset={4}
                                >
                                    <DropdownMenuLabel className="p-0 font-normal">
                                        <div className="flex items-center gap-2 px-1 py-1.5 text-left text-sm">
                                            <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-primary/10 text-primary">
                                                <span className="text-xs font-semibold">{userInitials}</span>
                                            </div>
                                            <div className="grid flex-1 text-left text-sm leading-tight">
                                                <span className="truncate font-medium">{userName}</span>
                                                <span className="text-muted-foreground truncate text-xs">
                                                    {userEmail}
                                                </span>
                                            </div>
                                        </div>
                                    </DropdownMenuLabel>
                                    <DropdownMenuSeparator />
                                    <DropdownMenuGroup>
                                        {userMenuItems.map((item) => (
                                            <DropdownMenuItem
                                                key={item.url}
                                                onSelect={() => navigate(item.url)}
                                            >
                                                <item.icon className="size-4" />
                                                <span>{item.title}</span>
                                            </DropdownMenuItem>
                                        ))}
                                    </DropdownMenuGroup>
                                    <DropdownMenuSeparator />
                                    <DropdownMenuItem
                                        variant="destructive"
                                        onSelect={handleLogoutClick}
                                    >
                                        <LogOut className="size-4" />
                                        <span>Sair</span>
                                    </DropdownMenuItem>
                                </DropdownMenuContent>
                            </DropdownMenu>
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
