import React from "react"
import { CheckCircle2, AlertCircle } from "lucide-react"
import { getPasswordRequirementChecks } from "@/utils/passwordPolicy"

export function PasswordRequirements({ password, confirmPassword = "", visible }) {
    if (!visible) return null

    const requirements = getPasswordRequirementChecks(password, confirmPassword)

    return (
        <div className="rounded-md border bg-muted/30 p-3">
            <p className="text-xs font-medium text-foreground">Sua senha deve conter:</p>
            <ul className="mt-2 space-y-2 text-xs">
                {requirements.map((requirement) => (
                    <li
                        key={requirement.id}
                        className={`flex items-start gap-2 ${requirement.met ? "text-green-700" : "text-red-600"}`}
                    >
                        {requirement.met ? (
                            <CheckCircle2 className="mt-0.5 h-3.5 w-3.5 shrink-0" />
                        ) : (
                            <AlertCircle className="mt-0.5 h-3.5 w-3.5 shrink-0" />
                        )}
                        <span>{requirement.label}</span>
                    </li>
                ))}
            </ul>
        </div>
    )
}
