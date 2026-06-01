import * as React from "react"
import { Separator } from "@/components/ui/separator"
import { SidebarTrigger } from "@/components/ui/sidebar"
import { ModeToggle } from "@/components/mode-toggle"
import { NotificationBell } from "@/components/notification-bell"

export function SiteHeader() {
    const environment = (import.meta.env.VITE_ENVIRONMENT || "").trim().toUpperCase()
    const isProduction = environment === "PROD"

    return (
        <header className={`flex h-(--header-height) shrink-0 items-center gap-2 border-b transition-[width,height] ease-linear group-has-data-[collapsible=icon]/sidebar-wrapper:h-(--header-height) ${
            isProduction ? "" : "border-yellow-300 bg-yellow-100 text-yellow-950 dark:border-yellow-500/60 dark:bg-yellow-500/20 dark:text-yellow-50"
        }`}>
            <div className="flex w-full items-center gap-1 px-4 py-3 lg:gap-2 lg:px-6">
                <SidebarTrigger className="-ml-1" />
                <Separator
                    orientation="vertical"
                    className="mx-2 data-[orientation=vertical]:h-4"
                />
                <div className="flex-1">
                    <h2 className="text-lg font-semibold">RendaTop</h2>
                </div>
                <div className="ml-auto flex items-center gap-2">
                    <NotificationBell />
                    <ModeToggle />
                </div>
            </div>
        </header>
    )
}
