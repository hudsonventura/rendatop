export function getIofBadgeClass(iofPercent) {
    const iof = Number(iofPercent ?? 0)
    if (iof > 0) {
        return "bg-red-500/10 text-red-700 dark:text-red-400 border-red-500/20"
    }
    return "bg-green-500/10 text-green-700 dark:text-green-400 border-green-500/20"
}
