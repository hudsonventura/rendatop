import type { Metadata } from "next"
import { LegalDocumentPage } from "@/components/legal-document-page"
import { legalDocuments } from "@/content/legal"
import { createPageMetadata } from "@/lib/seo"

export const metadata: Metadata = createPageMetadata({
  title: "Termos de Uso | RendaTop",
  description:
    "Leia as regras gerais de uso da plataforma RendaTop, responsabilidades do usuario e limitacoes do servico.",
  path: "/termos-de-uso",
  keywords: ["termos de uso", "regras da plataforma", "condicoes de uso"],
})

export default function TermsOfUsePage() {
  return <LegalDocumentPage {...legalDocuments.terms} />
}
