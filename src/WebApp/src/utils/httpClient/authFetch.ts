import { Auth } from "../auth/auth";

/**
 * Performs an authenticated REST call.
 * @param {RequestInfo} endpoint The endpoint to call.
 * @param {RequestInit} init The request options.
 * @returns {Response} The response.
 */
export async function authFetch(endpoint: RequestInfo, init: RequestInit): Promise<Response> {
    const { body, ...customConfig } = init;

    const token = Auth.msalInstance.getAllAccounts()?.length > 0 ? await Auth.getAccessTokenAsync() : undefined;
    const headers: Record<string, string> = { "content-type": "application/json" };
    if (token) headers.Authorization = `Bearer ${token}`;

    const config: RequestInit = {
        ...customConfig,
        headers: {
            ...headers,
            ...customConfig.headers,
        },
    };
    if (body) config.body = body;

    return window.fetch(endpoint, config);
}
