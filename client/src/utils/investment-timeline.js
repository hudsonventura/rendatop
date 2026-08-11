const DAY_MS = 24 * 60 * 60 * 1000

export function startOfDay(date) {
    const normalized = new Date(date)
    normalized.setHours(0, 0, 0, 0)
    return normalized
}

export function addDays(date, days) {
    const next = new Date(date)
    next.setDate(next.getDate() + days)
    return next
}

export function diffInDays(start, end) {
    return Math.floor((startOfDay(end).getTime() - startOfDay(start).getTime()) / DAY_MS)
}

function getOriginalInvestedValue(investment) {
    return Number(investment.value ?? 0)
}

function getCurrentLiquidValueWithoutRedemptions(investment) {
    const currentCalculated = investment.calculated?.[0]
    return Number(currentCalculated?.value_liq ?? investment.value ?? 0)
}

function getFinalLiquidValueWithoutRedemptions(investment) {
    const finalCalculated = investment.calculated?.[1]
    return Number(finalCalculated?.value_liq ?? getCurrentLiquidValueWithoutRedemptions(investment))
}

function interpolateValue(startValue, endValue, startDate, endDate, targetDate) {
    const totalDays = diffInDays(startDate, endDate)
    if (totalDays <= 0) {
        return endValue
    }

    const elapsedDays = diffInDays(startDate, targetDate)
    const progress = Math.min(1, Math.max(0, elapsedDays / totalDays))
    return startValue + ((endValue - startValue) * progress)
}

function getBaseInvestmentValueAtDate(investment, date, options = {}) {
    const { keepMaturedValue = false } = options
    const initialValue = getOriginalInvestedValue(investment)
    if (initialValue <= 0 || !investment.date_buy) return 0

    const startDate = startOfDay(new Date(investment.date_buy))
    const targetDate = startOfDay(date)
    if (Number.isNaN(startDate.getTime()) || targetDate < startDate) {
        return 0
    }

    const today = startOfDay(new Date())
    const finishDate = investment.due_date ? startOfDay(new Date(investment.due_date)) : null
    const currentFullValue = getCurrentLiquidValueWithoutRedemptions(investment)
    const finalFullValue = getFinalLiquidValueWithoutRedemptions(investment)

    if (finishDate && !Number.isNaN(finishDate.getTime())) {
        if (targetDate > finishDate) {
            return keepMaturedValue ? finalFullValue : 0
        }

        if (finishDate <= today) {
            return interpolateValue(initialValue, finalFullValue, startDate, finishDate, targetDate)
        }
    }

    const anchors = [
        { date: startDate, value: initialValue },
    ]

    if (today >= startDate) {
        anchors.push({
            date: today,
            value: currentFullValue,
        })
    }

    if (finishDate && !Number.isNaN(finishDate.getTime()) && finishDate > today) {
        anchors.push({
            date: finishDate,
            value: finalFullValue,
        })
    }

    const dedupedAnchors = anchors.filter((anchor, index, array) => {
        if (index === 0) return true
        return anchor.date.getTime() !== array[index - 1].date.getTime()
    })

    if (dedupedAnchors.length === 1) {
        return dedupedAnchors[0].value
    }

    for (let index = 1; index < dedupedAnchors.length; index += 1) {
        const previous = dedupedAnchors[index - 1]
        const current = dedupedAnchors[index]

        if (targetDate <= current.date) {
            return interpolateValue(previous.value, current.value, previous.date, current.date, targetDate)
        }
    }

    return dedupedAnchors[dedupedAnchors.length - 1].value
}

export function getInvestmentLiquidValueAtDate(investment, date) {
    const targetDate = startOfDay(date)
    let remainingShare = 1

    const redemptions = [...(investment.redemptions ?? [])]
        .filter((redemption) => redemption?.date && Number(redemption?.value ?? 0) > 0)
        .map((redemption) => ({
            date: startOfDay(new Date(redemption.date)),
            value: Number(redemption.value ?? 0),
        }))
        .filter((redemption) => !Number.isNaN(redemption.date.getTime()) && redemption.date <= targetDate)
        .sort((left, right) => left.date.getTime() - right.date.getTime())

    for (const redemption of redemptions) {
        const investmentValueBeforeRedemption = getBaseInvestmentValueAtDate(investment, redemption.date) * remainingShare
        if (investmentValueBeforeRedemption <= 0) {
            continue
        }

        const ratio = Math.min(1, Math.max(0, redemption.value / investmentValueBeforeRedemption))
        remainingShare *= (1 - ratio)
    }

    return getBaseInvestmentValueAtDate(investment, targetDate) * remainingShare
}

export function getInvestmentLiquidValueWithoutRedemptionsAtDate(investment, date) {
    return getBaseInvestmentValueAtDate(investment, date, { keepMaturedValue: true })
}

