export const INVESTMENT_TYPE_NONE = "__none__"

export const INVESTMENT_TYPE_OPTIONS = [
    { value: "CDB", label: "CDB" },
    { value: "RDB", label: "RDB" },
    { value: "LCI", label: "LCI" },
    { value: "LCA", label: "LCA" },
    { value: "RCI", label: "RCI" },
    { value: "RCA", label: "RCA" },
    { value: "Tesouro", label: "Tesouro" },
    { value: "Debentures", label: "Debêntures" },
    { value: "TitulosPublicos", label: "Títulos públicos" },
    { value: "CRI", label: "CRI" },
    { value: "CRA", label: "CRA" },
]

const TYPE_KEYWORDS = [
    { value: "CDB", terms: [["cdb"]] },
    { value: "RDB", terms: [["rdb"]] },
    { value: "LCI", terms: [["lci"]] },
    { value: "LCA", terms: [["lca"]] },
    { value: "RCI", terms: [["rci"]] },
    { value: "RCA", terms: [["rca"]] },
    { value: "Tesouro", terms: [["tesouro"]] },
    { value: "Debentures", terms: [["debentures"], ["debenture"]] },
    { value: "TitulosPublicos", terms: [["titulos", "publicos"], ["titulo", "publico"], ["titulos"], ["titulo"]] },
    { value: "CRI", terms: [["cri"]] },
    { value: "CRA", terms: [["cra"]] },
]

function normalizeWord(value) {
    return String(value ?? "")
        .normalize("NFD")
        .replace(/[\u0300-\u036f]/g, "")
        .toLowerCase()
}

function splitWords(value) {
    return normalizeWord(value)
        .split(/[^a-z0-9]+/)
        .filter(Boolean)
}

function wordsMatch(words, candidateWords) {
    if (candidateWords.length === 1) {
        return words.some((word) => word === candidateWords[0])
    }

    for (let index = 0; index <= words.length - candidateWords.length; index += 1) {
        const matches = candidateWords.every((candidateWord, offset) => words[index + offset] === candidateWord)
        if (matches) return true
    }

    return false
}

export function detectInvestmentTypeFromTitle(title) {
    const words = splitWords(title)
    if (!words.length) return undefined

    const match = TYPE_KEYWORDS.find((typeRule) =>
        typeRule.terms.some((termWords) => wordsMatch(words, termWords))
    )

    return match?.value
}

export function getInvestmentTypeLabel(type) {
    return INVESTMENT_TYPE_OPTIONS.find((option) => option.value === type)?.label ?? ""
}
