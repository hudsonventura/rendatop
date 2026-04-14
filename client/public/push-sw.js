self.addEventListener("push", (event) => {
    let payload = {}

    if (event.data) {
        try {
            payload = event.data.json()
        } catch {
            payload = { body: event.data.text() }
        }
    }

    const title = payload.title || "RendaTop"
    const options = {
        body: payload.body || "",
        icon: new URL("icon.png", self.registration.scope).toString(),
        badge: new URL("favicon.svg", self.registration.scope).toString(),
        data: {
            url: payload.url || self.registration.scope
        },
        tag: payload.tag || undefined
    }

    event.waitUntil(self.registration.showNotification(title, options))
})

self.addEventListener("notificationclick", (event) => {
    event.notification.close()

    const targetUrl = event.notification.data?.url || self.registration.scope

    event.waitUntil(
        clients.matchAll({ type: "window", includeUncontrolled: true }).then((clientList) => {
            for (const client of clientList) {
                if (client.url === targetUrl && "focus" in client) {
                    return client.focus()
                }
            }

            if (clients.openWindow) {
                return clients.openWindow(targetUrl)
            }

            return undefined
        })
    )
})
