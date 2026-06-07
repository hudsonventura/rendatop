import type { StructuredDataValue } from '@/lib/seo'

type StructuredDataProps = {
  data: StructuredDataValue
}

export function StructuredData({ data }: StructuredDataProps) {
  return (
    <script
      type="application/ld+json"
      dangerouslySetInnerHTML={{ __html: JSON.stringify(data) }}
    />
  )
}
