import { ApiError } from "../../types/apiError";
import { authFetch } from "./authFetch";

export const httpClient = {
    fetch,
    get,
    post,
    put,
    delete: _delete,
    download,
    patch,
};

export async function fetch<T>(endpoint: RequestInfo, init: RequestInit & { notifyOnError?: boolean }): Promise<T> {
    const { notifyOnError, ...config } = init;
    try {
        const response = await authFetch(endpoint, config);
        if (response.ok) {
            // If there is no body, return empty array
            return await response.json().catch(() => ({}));
        } else {
            const errorMessage = (await response.text()) || response.status.toString();
            console.error(`HTTP ${response.status}: ${errorMessage}`, response);
            if (notifyOnError || notifyOnError === undefined) notifyError(errorMessage);
            return Promise.reject(new Error(errorMessage));
        }
    } catch (e: unknown) {
        if (e instanceof Error) {
            console.error(e.message);
            if (notifyOnError || notifyOnError === undefined) notifyError(e.message);
            return Promise.reject(e);
        } else {
            console.error(e || "Unknown error");
            if (notifyOnError || notifyOnError === undefined) notifyError(String(e));
            return Promise.reject(new Error(String(e)));
        }
    }
}

export async function fetchRaw(
    endpoint: RequestInfo,
    init: RequestInit & { notifyOnError?: boolean }
): Promise<Response> {
    const { notifyOnError, ...config } = init;
    try {
        return await authFetch(endpoint, config);
    } catch (e: unknown) {
        if (e instanceof Error) {
            console.error(e.message);
            if (notifyOnError || notifyOnError === undefined) notifyError(e.message);
            return Promise.reject(e);
        } else {
            console.error(e || "Unknown error");
            if (notifyOnError || notifyOnError === undefined) notifyError(String(e));
            return Promise.reject(new Error(String(e)));
        }
    }
}

async function get<T>(path: string, config?: RequestInit & { notifyOnError?: boolean }): Promise<T> {
    const init = { method: "GET", ...config };
    return fetch<T>(path, init);
}

async function post<T, U>(path: string, body?: T, config?: RequestInit & { notifyOnError?: boolean }): Promise<U> {
    const init = { method: "POST", body: JSON.stringify(body), ...config };
    return fetch<U>(path, init);
}

async function put<T, U>(path: string, body?: T, config?: RequestInit & { notifyOnError?: boolean }): Promise<U> {
    const init = { method: "PUT", body: JSON.stringify(body), ...config };
    return fetch<U>(path, init);
}

async function _delete<T>(path: string, config?: RequestInit & { notifyOnError?: boolean }): Promise<T> {
    const init = { method: "DELETE", ...config };
    return fetch<T>(path, init);
}

async function download(
    path: string,
    fileName: string,
    config?: RequestInit & { notifyOnError?: boolean }
): Promise<void> {
    const init = { method: "GET", ...config };
    const response = await fetchRaw(path, init);
    const blob = await response.blob();

    const url = window.URL.createObjectURL(new Blob([blob]));
    const link = document.createElement("a");
    link.href = url;
    link.setAttribute("download", fileName);

    // Append to html link element page
    document.body.appendChild(link);

    // Start download
    link.click();

    // Clean up and remove the link
    link.parentNode?.removeChild(link);
}

async function patch<T, U>(path: string, body: T, config?: RequestInit & { notifyOnError?: boolean }): Promise<U> {
    const init = { method: "PATCH", body: JSON.stringify(body), ...config };
    return fetch<U>(path, init);
}

function notifyError(_message: string) {
    // TO DO
}

/**
 * Parse an exception from a httpClient operation.
 * @param {any} ex Exception
 * @param {function} t i18n translator function
 * @returns {string[]} List to messages
 */
// eslint-disable-next-line @typescript-eslint/no-explicit-any
export function parseHttpException(ex: any, t: (key: string) => string): string[] {
    if (ex instanceof Error && ex.message.startsWith("{")) {
        // Parse submission errors
        const error = JSON.parse(ex.message);
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        if ("errors" in (error as any)) {
            const errors = (error as ApiError).errors;
            const messages = Object.entries(errors)?.map((field) => errors[field[0]].join(", "));
            return messages;
        }
    } else if (ex instanceof Error && ex.message === "403") {
        return [t("common.forbidden")];
    }
    return [t("common.error")];
}
