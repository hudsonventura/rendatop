export const PASSWORD_POLICY_MESSAGE =
    "A senha deve ter no mínimo 9 caracteres, incluindo pelo menos 1 letra, 1 número e 1 caractere especial."

export const getPasswordRequirementChecks = (password, confirmPassword = "") => {
    const value = String(password || "")
    const confirmation = String(confirmPassword || "")

    return [
        {
            id: "length",
            label: "ao menos 9 caracteres",
            met: value.length >= 9,
        },
        {
            id: "letter",
            label: "ao menos uma letra",
            met: /[A-Za-z]/.test(value),
        },
        {
            id: "number",
            label: "ao menos 1 numero",
            met: /\d/.test(value),
        },
        {
            id: "special",
            label: "ao menos um caracter especial (!@#$%*,./<>)",
            met: /[^A-Za-z0-9]/.test(value),
        },
        {
            id: "confirmation",
            label: "a confirmacao da senha deve coincidir com a senha",
            met: value.length > 0 && confirmation.length > 0 && value === confirmation,
        },
    ]
}

export const isStrongPassword = (password) =>
    getPasswordRequirementChecks(password, password).slice(0, 4).every((item) => item.met)

export const getPasswordValidationMessage = (password) =>
    isStrongPassword(password) ? "" : PASSWORD_POLICY_MESSAGE
