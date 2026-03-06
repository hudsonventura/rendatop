import * as React from "react"

const initialState = {
    theme: "system",
    setTheme: () => null,
}

export const ThemeProviderContext = React.createContext(initialState)
