import * as React from "react"
import axiosInstance from "@/utils/axiosConfig"

const ACTIVE_WALLET_KEY = "rendatop.active_wallet_id"

const WalletContext = React.createContext({
    wallets: [],
    activeWalletId: "",
    activeWallet: null,
    loading: false,
    canCreate: false,
    restrictionMessage: "",
    setActiveWalletId: () => {},
    refreshWallets: () => {},
})

export function WalletProvider({ children }) {
    const [wallets, setWallets] = React.useState([])
    const [activeWalletId, setActiveWalletIdState] = React.useState(() => localStorage.getItem(ACTIVE_WALLET_KEY) || "")
    const [loading, setLoading] = React.useState(true)
    const [canCreate, setCanCreate] = React.useState(false)
    const [restrictionMessage, setRestrictionMessage] = React.useState("")

    const refreshWallets = React.useCallback(() => {
        setLoading(true)
        return axiosInstance
            .get("/Wallets")
            .then((response) => {
                const data = response?.data || {}
                const enabledWallets = (data.items || []).filter((wallet) => wallet.enabled)
                const stored = localStorage.getItem(ACTIVE_WALLET_KEY)
                const nextActive = enabledWallets.some((wallet) => wallet.id === stored)
                    ? stored
                    : data.active_wallet_id || enabledWallets[0]?.id || ""

                setWallets(data.items || [])
                setCanCreate(Boolean(data.can_create))
                setRestrictionMessage(data.restriction_message || "")
                setActiveWalletIdState(nextActive)
                if (nextActive) localStorage.setItem(ACTIVE_WALLET_KEY, nextActive)
                else localStorage.removeItem(ACTIVE_WALLET_KEY)
            })
            .catch(() => {
                setWallets([])
                setCanCreate(false)
                setRestrictionMessage("")
            })
            .finally(() => setLoading(false))
    }, [])

    React.useEffect(() => {
        refreshWallets()
    }, [refreshWallets])

    const setActiveWalletId = React.useCallback((walletId) => {
        setActiveWalletIdState(walletId)
        if (walletId) localStorage.setItem(ACTIVE_WALLET_KEY, walletId)
        else localStorage.removeItem(ACTIVE_WALLET_KEY)
    }, [])

    const activeWallet = React.useMemo(
        () => wallets.find((wallet) => wallet.id === activeWalletId) || null,
        [wallets, activeWalletId]
    )

    return (
        <WalletContext.Provider value={{
            wallets,
            activeWalletId,
            activeWallet,
            loading,
            canCreate,
            restrictionMessage,
            setActiveWalletId,
            refreshWallets,
        }}>
            {children}
        </WalletContext.Provider>
    )
}

export function useWallet() {
    return React.useContext(WalletContext)
}

export function walletParams(activeWalletId) {
    return activeWalletId ? { wallet_id: activeWalletId } : {}
}
