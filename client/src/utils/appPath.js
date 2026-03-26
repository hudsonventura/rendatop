const ROUTER_BASENAME = (import.meta.env.VITE_ROUTER_BASENAME || "/app").replace(/\/+$/, "") || "/";

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
