import * as React from "react"
import { Moon, Sun } from "lucide-react"
import { Button } from "@/components/ui/button"
import { useTheme } from "@/hooks/use-theme"

export function ModeToggle({ variant = "outline" }) {
    const { theme, setTheme } = useTheme()

    const [isDarkMode, setIsDarkMode] = React.useState(false)

    React.useEffect(() => {
        const updateMode = () => {
            if (theme === "dark") {
                setIsDarkMode(true)
            } else if (theme === "light") {
                setIsDarkMode(false)
            } else {
                setIsDarkMode(window.matchMedia("(prefers-color-scheme: dark)").matches)
            }
        }

        updateMode()

        const mediaQuery = window.matchMedia("(prefers-color-scheme: dark)")
        mediaQuery.addEventListener("change", updateMode)

        return () => mediaQuery.removeEventListener("change", updateMode)
    }, [theme])

    const handleToggle = () => {
        setTheme(isDarkMode ? "light" : "dark")
    }

    return (
        <Button
            variant={variant}
            size="icon"
            onClick={handleToggle}
            className="cursor-pointer relative overflow-hidden"
        >
            {isDarkMode ? (
                <Sun className="h-[1.2rem] w-[1.2rem] transition-transform duration-300 rotate-0 scale-100" />
            ) : (
                <Moon className="h-[1.2rem] w-[1.2rem] transition-transform duration-300 rotate-0 scale-100" />
            )}
            <span className="sr-only">
                Switch to {isDarkMode ? "light" : "dark"} mode
            </span>
        </Button>
    )
}
