"use client"

import { Monitor, ImageIcon } from "lucide-react"
import { Badge } from "@/components/ui/badge"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { cn } from "@/lib/utils"

type ScreenshotPlaceholderProps = {
  title: string
  subtitle: string
  caption: string
  badges?: string[]
  className?: string
}

export function ScreenshotPlaceholder({
  title,
  subtitle,
  caption,
  badges = [],
  className,
}: ScreenshotPlaceholderProps) {
  return (
    <Card className={cn("overflow-hidden border bg-card/90 shadow-2xl", className)}>
      <CardHeader className="border-b bg-muted/40">
        <div className="flex items-center justify-between gap-3">
          <div className="flex items-center gap-2">
            <div className="flex size-9 items-center justify-center rounded-lg bg-primary/10 text-primary">
              <Monitor className="size-4" />
            </div>
            <div>
              <CardTitle className="text-base">{title}</CardTitle>
              <p className="text-sm text-muted-foreground">{subtitle}</p>
            </div>
          </div>
          <Badge variant="outline">Screenshot</Badge>
        </div>
      </CardHeader>
      <CardContent className="p-0">
        <div className="bg-gradient-to-br from-background via-muted/20 to-primary/5 p-6">
          <div className="rounded-xl border border-dashed border-border/80 bg-background/80 p-6">
            <div className="flex min-h-[280px] flex-col justify-between rounded-lg border bg-muted/20 p-5 sm:min-h-[340px]">
              <div className="flex items-start justify-between gap-3">
                <div>
                  <p className="text-sm font-medium">{title}</p>
                  <p className="mt-1 max-w-md text-sm text-muted-foreground">{caption}</p>
                </div>
                <ImageIcon className="size-5 text-muted-foreground" />
              </div>

              <div className="grid gap-3 sm:grid-cols-3">
                {badges.map((badge) => (
                  <div
                    key={badge}
                    className="rounded-lg border bg-background/80 px-3 py-2 text-sm text-muted-foreground"
                  >
                    {badge}
                  </div>
                ))}
              </div>

              <div className="rounded-lg border border-dashed bg-background/60 px-4 py-3 text-sm text-muted-foreground">
                Espaco reservado para inserir a captura real desta tela do app.
              </div>
            </div>
          </div>
        </div>
      </CardContent>
    </Card>
  )
}
