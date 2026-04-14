import axiosInstance from "@/utils/axiosConfig"
import { appPath } from "@/utils/appPath"

const PUBLIC_KEY_ENDPOINT = "/User/Settings/BrowserPush/PublicKey"
const SUBSCRIBE_ENDPOINT = "/User/Settings/BrowserPush/Subscribe"
const UNSUBSCRIBE_ENDPOINT = "/User/Settings/BrowserPush/Unsubscribe"

export function isBrowserPushSupported() {
    return typeof window !== "undefined" &&
        "Notification" in window &&
        "serviceWorker" in navigator &&
        "PushManager" in window
}

export async function getBrowserPushServerConfiguration() {
    const response = await axiosInstance.get(PUBLIC_KEY_ENDPOINT)
    return response?.data || { enabled: false, public_key: null }
}

export async function getCurrentBrowserPushSubscription() {
    if (!isBrowserPushSupported()) {
        return null
    }

    const registration = await navigator.serviceWorker.getRegistration(appPath("/"))
    if (!registration) {
        return null
    }

    return registration.pushManager.getSubscription()
}

export async function subscribeCurrentBrowserToPush() {
    if (!isBrowserPushSupported()) {
        throw new Error("Seu navegador não suporta notificações push.")
    }

    const config = await getBrowserPushServerConfiguration()
    if (!config.enabled || !config.public_key) {
        throw new Error("Notificações no navegador ainda não estão configuradas no servidor.")
    }

    const permission = Notification.permission === "granted"
        ? "granted"
        : await Notification.requestPermission()

    if (permission !== "granted") {
        throw new Error("Permissão de notificações do navegador não foi concedida.")
    }

    const registration = await navigator.serviceWorker.register(appPath("/push-sw.js"), {
        scope: appPath("/")
    })

    let subscription = await registration.pushManager.getSubscription()
    if (!subscription) {
        subscription = await registration.pushManager.subscribe({
            userVisibleOnly: true,
            applicationServerKey: base64UrlToUint8Array(config.public_key)
        })
    }

    await axiosInstance.post(SUBSCRIBE_ENDPOINT, toSubscriptionPayload(subscription))
    return subscription
}

export async function unsubscribeCurrentBrowserFromPush() {
    if (!isBrowserPushSupported()) {
        return
    }

    const registration = await navigator.serviceWorker.getRegistration(appPath("/"))
    if (!registration) {
        return
    }

    const subscription = await registration.pushManager.getSubscription()
    if (!subscription) {
        return
    }

    await axiosInstance.post(UNSUBSCRIBE_ENDPOINT, { endpoint: subscription.endpoint })
    await subscription.unsubscribe()
}

export async function syncCurrentBrowserPushSubscription() {
    const subscription = await getCurrentBrowserPushSubscription()
    if (!subscription) {
        return false
    }

    const config = await getBrowserPushServerConfiguration()
    if (!config.enabled) {
        return false
    }

    await axiosInstance.post(SUBSCRIBE_ENDPOINT, toSubscriptionPayload(subscription))
    return true
}

function toSubscriptionPayload(subscription) {
    return {
        endpoint: subscription.endpoint,
        p256dh: arrayBufferToBase64(subscription.getKey("p256dh")),
        auth: arrayBufferToBase64(subscription.getKey("auth")),
        user_agent: navigator.userAgent
    }
}

function arrayBufferToBase64(buffer) {
    if (!buffer) {
        return ""
    }

    const bytes = new Uint8Array(buffer)
    let binary = ""
    bytes.forEach((byte) => {
        binary += String.fromCharCode(byte)
    })

    return btoa(binary)
}

function base64UrlToUint8Array(base64UrlString) {
    const padding = "=".repeat((4 - (base64UrlString.length % 4)) % 4)
    const base64 = (base64UrlString + padding)
        .replace(/-/g, "+")
        .replace(/_/g, "/")

    const rawData = atob(base64)
    const outputArray = new Uint8Array(rawData.length)

    for (let i = 0; i < rawData.length; i += 1) {
        outputArray[i] = rawData.charCodeAt(i)
    }

    return outputArray
}
