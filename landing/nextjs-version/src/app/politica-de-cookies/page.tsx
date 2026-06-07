import type { Metadata } from "next"
import { LegalDocumentPage } from "@/components/legal-document-page"
import { legalDocuments } from "@/content/legal"
import { createPageMetadata } from "@/lib/seo"

export const metadata: Metadata = createPageMetadata({
  title: "Politica de Cookies | RendaTop",
  description:
    "Entenda como o RendaTop usa cookies, armazenamento local e tecnologias semelhantes.",
  path: "/politica-de-cookies",
  keywords: ["politica de cookies", "cookies", "armazenamento local"],
})

export default function CookiesPolicyPage() {
  return <LegalDocumentPage {...legalDocuments.cookies} />
}
