function getRouterBasename() {
    const frontendUrl = (import.meta.env.VITE_FRONTEND_URL || "").trim();

    if (!frontendUrl) {
        return "/";
    }

    try {
        const url = new URL(frontendUrl);
        return url.pathname.replace(/\/+$/, "") || "/";
    } catch {
        return "/";
    }
}

const ROUTER_BASENAME = getRouterBasename();

export function appPath(path = "/") {
    if (/^https?:\/\//i.test(path)) {
        return path;
    }

    const normalizedPath = path.startsWith("/") ? path : `/${path}`;

    if (ROUTER_BASENAME === "/") {
        return normalizedPath;
    }

    return `${ROUTER_BASENAME}${normalizedPath}`;
}

export { ROUTER_BASENAME };
