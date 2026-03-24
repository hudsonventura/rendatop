export function sanitizeCpf(value) {
    return (value || "").replace(/\D/g, "").slice(0, 11);
}

export function formatCpf(value) {
    const digits = sanitizeCpf(value);
    if (!digits) return "";

    const part1 = digits.slice(0, 3);
    const part2 = digits.slice(3, 6);
    const part3 = digits.slice(6, 9);
    const part4 = digits.slice(9, 11);

    if (digits.length <= 3) return part1;
    if (digits.length <= 6) return `${part1}.${part2}`;
    if (digits.length <= 9) return `${part1}.${part2}.${part3}`;
    return `${part1}.${part2}.${part3}-${part4}`;
}

export function isValidCpf(value) {
    const cpf = sanitizeCpf(value);
    if (cpf.length !== 11) return false;
    if (/^(\d)\1{10}$/.test(cpf)) return false;

    const calcDigit = (base, factor) => {
        const sum = base
            .split("")
            .reduce((acc, digit) => acc + Number(digit) * factor--, 0);
        const remainder = (sum * 10) % 11;
        return remainder === 10 ? 0 : remainder;
    };

    const digit1 = calcDigit(cpf.slice(0, 9), 10);
    const digit2 = calcDigit(cpf.slice(0, 10), 11);

    return cpf[9] === String(digit1) && cpf[10] === String(digit2);
}
