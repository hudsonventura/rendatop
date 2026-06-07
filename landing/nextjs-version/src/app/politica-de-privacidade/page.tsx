import type { Metadata } from "next"
import { LegalDocumentPage } from "@/components/legal-document-page"
import { legalDocuments } from "@/content/legal"
import { createPageMetadata } from "@/lib/seo"

export const metadata: Metadata = createPageMetadata({
  title: "Politica de Privacidade | RendaTop",
  description:
    "Saiba como o RendaTop trata dados pessoais, quais informacoes usa e quais direitos voce pode exercer.",
  path: "/politica-de-privacidade",
  keywords: ["politica de privacidade", "lgpd", "dados pessoais"],
})

export default function PrivacyPolicyPage() {
  return <LegalDocumentPage {...legalDocuments.privacy} />
}
