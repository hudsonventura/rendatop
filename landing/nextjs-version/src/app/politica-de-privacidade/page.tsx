import type { Metadata } from "next"
import { LegalDocumentPage } from "@/components/legal-document-page"
import { legalDocuments } from "@/content/legal"

export const metadata: Metadata = {
  title: "Politica de Privacidade | RendaTop",
  description:
    "Saiba como o RendaTop trata dados pessoais, quais informacoes usa e quais direitos voce pode exercer.",
}

export default function PrivacyPolicyPage() {
  return <LegalDocumentPage {...legalDocuments.privacy} />
}
