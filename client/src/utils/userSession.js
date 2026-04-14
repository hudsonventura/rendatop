export const USER_TYPE_STORAGE_KEY = "user_type"
export const USER_TYPE_COMMON = "Common"
export const USER_TYPE_ADMIN = "Admin"

export function persistSessionUser({ name, email, user_type }) {
    if (name) {
        sessionStorage.setItem("name", name)
    }

    if (email) {
        sessionStorage.setItem("email", email)
    }

    if (user_type) {
        sessionStorage.setItem(USER_TYPE_STORAGE_KEY, String(user_type))
    }
}

export function getStoredUserType() {
    return sessionStorage.getItem(USER_TYPE_STORAGE_KEY) || ""
}

export function isAdminUserType(userType) {
    return String(userType || "").trim().toLowerCase() === USER_TYPE_ADMIN.toLowerCase()
}
