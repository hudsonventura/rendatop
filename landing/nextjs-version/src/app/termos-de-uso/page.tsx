import type { Metadata } from "next"
import { LegalDocumentPage } from "@/components/legal-document-page"
import { legalDocuments } from "@/content/legal"

export const metadata: Metadata = {
  title: "Termos de Uso | RendaTop",
  description:
    "Leia as regras gerais de uso da plataforma RendaTop, responsabilidades do usuario e limitacoes do servico.",
}

export default function TermsOfUsePage() {
  return <LegalDocumentPage {...legalDocuments.terms} />
}
