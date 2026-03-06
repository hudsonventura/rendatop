import * as React from "react"
import { AppSidebar } from "@/components/app-sidebar"
import { SiteHeader } from "@/components/site-header"
import { useSidebarConfig } from "@/contexts/sidebar-context"
import {
    SidebarInset,
    SidebarProvider,
} from "@/components/ui/sidebar"

export function BaseLayout({ children, title, description }) {
    const { config } = useSidebarConfig()

    return (
        <SidebarProvider
            style={{
                "--sidebar-width": "16rem",
                "--sidebar-width-icon": "3rem",
                "--header-height": "calc(var(--spacing) * 14)",
            }}
            className={config.collapsible === "none" ? "sidebar-none-mode" : ""}
        >
            <AppSidebar
                variant={config.variant}
                collapsible={config.collapsible}
                side={config.side}
            />
            <SidebarInset>
                <SiteHeader />
                <div className="flex flex-1 flex-col">
                    <div className="@container/main flex flex-1 flex-col gap-2">
                        <div className="flex flex-col gap-4 py-4 md:gap-6 md:py-6">
                            {title && (
                                <div className="px-4 lg:px-6">
                                    <div className="flex flex-col gap-2">
                                        <h1 className="text-2xl font-bold tracking-tight">{title}</h1>
                                        {description && (
                                            <p className="text-muted-foreground">{description}</p>
                                        )}
                                    </div>
                                </div>
                            )}
                            {children}
                        </div>
                    </div>
                </div>
            </SidebarInset>
        </SidebarProvider>
    )
}
