const API_BASE_URL = import.meta.env.VITE_API_URL || "ENV NAO ENCONTRADO";
if (API_BASE_URL === "ENV NAO ENCONTRADO") {
    throw new Error("VITE_API_URL não encontrado cheque o .env");
}

interface RequestOptions extends Omit<RequestInit, "body"> {
    body?: unknown;
}

class HttpClient {
    private baseUrl: string;

    constructor(baseUrl: string) {
        this.baseUrl = baseUrl;
    }

    private getHeaders(): HeadersInit {
        return {
            "Content-Type": "application/json",
        };
    }

    private async request<T>(
        endpoint: string,
        options: RequestOptions = {}
    ): Promise<T> {
        const { body, ...rest } = options;

        const response = await fetch(`${this.baseUrl}${endpoint}`, {
            ...rest,
            headers: {
                ...this.getHeaders(),
                ...(rest.headers || {}),
            },
            credentials: "include",
            body: body ? JSON.stringify(body) : undefined,
        });

        if (!response.ok) {
            const errorBody = await response.text();
            throw new HttpError(response.status, response.statusText, errorBody);
        }

        // Se a resposta não tem conteúdo (204 No Content)
        if (response.status === 204) {
            return undefined as T;
        }

        const contentType = response.headers.get("content-type");
        if (contentType && contentType.includes("application/json")) {
            return response.json();
        }

        return response.text() as T;
    }

    async get<T>(endpoint: string, options?: RequestOptions): Promise<T> {
        return this.request<T>(endpoint, { ...options, method: "GET" });
    }

    async post<T>(
        endpoint: string,
        body?: unknown,
        options?: RequestOptions
    ): Promise<T> {
        return this.request<T>(endpoint, { ...options, method: "POST", body });
    }

    async put<T>(
        endpoint: string,
        body?: unknown,
        options?: RequestOptions
    ): Promise<T> {
        return this.request<T>(endpoint, { ...options, method: "PUT", body });
    }

    async patch<T>(
        endpoint: string,
        body?: unknown,
        options?: RequestOptions
    ): Promise<T> {
        return this.request<T>(endpoint, { ...options, method: "PATCH", body });
    }

    async delete<T>(endpoint: string, options?: RequestOptions): Promise<T> {
        return this.request<T>(endpoint, { ...options, method: "DELETE" });
    }
}

export class HttpError extends Error {
    status: number;
    statusText: string;
    body: string;

    constructor(status: number, statusText: string, body: string) {
        super(`HTTP ${status}: ${statusText}`);
        this.name = "HttpError";
        this.status = status;
        this.statusText = statusText;
        this.body = body;
    }
}

export const httpClient = new HttpClient(API_BASE_URL);
