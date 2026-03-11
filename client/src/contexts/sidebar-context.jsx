import * as React from "react"

const SidebarContext = React.createContext(null)

export function SidebarConfigProvider({ children }) {
    const [config, setConfig] = React.useState({
        variant: "inset",
        collapsible: "icon",
        side: "left"
    })

    const updateConfig = React.useCallback((newConfig) => {
        setConfig(prev => ({ ...prev, ...newConfig }))
    }, [])

    return (
        <SidebarContext.Provider value={{ config, updateConfig }}>
            {children}
        </SidebarContext.Provider>
    )
}

export function useSidebarConfig() {
    const context = React.useContext(SidebarContext)
    if (!context) {
        throw new Error("useSidebarConfig must be used within a SidebarConfigProvider")
    }
    return context
}

export { SidebarContext }
