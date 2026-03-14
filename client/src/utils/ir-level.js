const IR_STYLE_BY_LEVEL = {
    red: "bg-red-500/10 text-red-700 dark:text-red-400 border-red-500/20",
    orange: "bg-orange-500/10 text-orange-700 dark:text-orange-400 border-orange-500/20",
    yellow: "bg-yellow-500/10 text-yellow-700 dark:text-yellow-400 border-yellow-500/20",
    green: "bg-green-500/10 text-green-700 dark:text-green-400 border-green-500/20",
    blue: "bg-blue-500/10 text-blue-700 dark:text-blue-400 border-blue-500/20",
}

export function resolveIrLevel(irPercent) {
    const ir = Number(irPercent ?? 0)
    if (ir >= 22.5) return "red"
    if (ir >= 20) return "orange"
    if (ir >= 17.5) return "yellow"
    if (ir >= 15) return "green"
    return "blue"
}

export function formatIrPercent(irPercent) {
    const ir = Number(irPercent ?? 0)
    return ir.toLocaleString("pt-BR", {
        minimumFractionDigits: Number.isInteger(ir) ? 0 : 1,
        maximumFractionDigits: 1,
    })
}

export function getIrBadgeClass(irPercent) {
    return IR_STYLE_BY_LEVEL[resolveIrLevel(irPercent)]
}

export function getIrTextClass(irPercent) {
    const badgeClass = getIrBadgeClass(irPercent)
    return badgeClass
        .split(" ")
        .filter((token) => !token.startsWith("bg-") && !token.startsWith("border-"))
        .join(" ")
}

export function getIrBadgeLabel(irPercent) {
    const ir = Number(irPercent ?? 0)
    if (ir <= 0) return "Isento IR"
    return `IR ${formatIrPercent(ir)}%`
}
