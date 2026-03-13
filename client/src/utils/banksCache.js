import axiosInstance from "@/utils/axiosConfig"

let banksCache = null
let banksRequest = null

export function getCachedBanks() {
    if (banksCache) {
        return Promise.resolve(banksCache)
    }

    if (!banksRequest) {
        banksRequest = axiosInstance.get("/Banks").then((response) => {
            banksCache = response.data
            return banksCache
        }).finally(() => {
            banksRequest = null
        })
    }

    return banksRequest
}

export function primeBanksCache(banks) {
    banksCache = banks
}
