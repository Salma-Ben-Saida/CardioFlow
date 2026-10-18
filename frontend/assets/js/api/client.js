/**
 * API Client - Base fetch wrapper with JWT header injection
 */

const API_BASE_URL = 'http://localhost:5152';
let authToken = sessionStorage.getItem('authToken') || null;

export function setToken(token) {
    authToken = token;
    if (token) {
        sessionStorage.setItem('authToken', token);
    } else {
        sessionStorage.removeItem('authToken');
    }
}
export function getToken() { return authToken; }
export function clearToken() { setToken(null); }

export async function apiRequest(endpoint, options = {}) {
    const url = `${API_BASE_URL}${endpoint}`;
    const headers = { 'Content-Type': 'application/json', ...options.headers };
    if (authToken) headers['Authorization'] = `Bearer ${authToken}`;

    try {
        const response = await fetch(url, { ...options, headers });

        if (response.status === 401) {
            clearToken();
            window.location.href = '/frontend/pages/login.html';
            throw new Error('Unauthorized');
        }
        if (response.status === 403) {
            throw new Error("Accès refusé : vous n'avez pas les droits nécessaires.");
        }
        if (response.status === 204 || response.status === 205) {
            return null;
        }
        if (!response.ok) {
            let errorMessage = `HTTP Error: ${response.status}`;
            try {
                const errorData = await response.json();
                errorMessage = errorData.message || errorData.error || errorMessage;
            } catch {}
            const err = new Error(errorMessage);
            err.status = response.status;
            throw err;
        }
        const contentType = response.headers.get('content-type') || '';
        return contentType.includes('application/json') ? await response.json() : await response.text();
    } catch (error) {
        if (error.name === 'TypeError' && error.message === 'Failed to fetch') {
            throw new Error('Network error. Please check your connection.');
        }
        throw error;
    }
}

export async function get(endpoint, params = {}) {
    const cleanParams = Object.fromEntries(
        Object.entries(params).filter(([, v]) => v !== undefined && v !== null && v !== '')
    );
    const queryString = new URLSearchParams(cleanParams).toString();
    const url = queryString ? `${endpoint}?${queryString}` : endpoint;
    return apiRequest(url, { method: 'GET' });
}

export async function post(endpoint, data = {}) {
    return apiRequest(endpoint, { method: 'POST', body: JSON.stringify(data) });
}

export async function put(endpoint, data = {}) {
    return apiRequest(endpoint, { method: 'PUT', body: JSON.stringify(data) });
}

export default { get, post, put, setToken, getToken, clearToken };