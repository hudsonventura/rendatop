function getTodayAtMidnight() {
    const today = new Date()
    today.setHours(0, 0, 0, 0)
    return today
}

function formatDecimalDisplay(value) {
    if (value === null || value === undefined || value === "") return ""

    const [int, dec = ""] = String(value).split(".")
    const intFormatted = int.replace(/\B(?=(\d{3})+(?!\d))/g, ".")
    return dec ? `${intFormatted},${dec.substring(0, 2)}` : intFormatted
}

export function getReinvestmentInitialValues(investment) {
    return {
        source_investment_id: investment.id,
        title: investment.title ?? "",
        date_buy: getTodayAtMidnight(),
        due_date: undefined,
        liquidez_diaria: false,
        taxes: investment.taxes ?? true,
        bank_code: investment.bank?.code ?? investment.bank_code ?? undefined,
        value: formatDecimalDisplay(investment.calculated?.[0]?.value_liq ?? ""),
        index: investment.index === "CDI" ? "0" : investment.index === "IPCA_MAIS" ? "1" : investment.index === "PERCENT_YEAR" ? "2" : investment.index === "CDI_MAIS" ? "3" : "",
        index_percent: formatDecimalDisplay(investment.index_percent ?? ""),
    }
}
