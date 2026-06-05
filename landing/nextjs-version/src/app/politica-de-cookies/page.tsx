import type { Metadata } from "next"
import { LegalDocumentPage } from "@/components/legal-document-page"
import { legalDocuments } from "@/content/legal"

export const metadata: Metadata = {
  title: "Politica de Cookies | RendaTop",
  description:
    "Entenda como o RendaTop usa cookies, armazenamento local e tecnologias semelhantes.",
}

export default function CookiesPolicyPage() {
  return <LegalDocumentPage {...legalDocuments.cookies} />
}
