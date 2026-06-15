/**
 * API Client - Base fetch wrapper with JWT header injection
 */

const API_BASE_URL = 'http://localhost:5152';
let authToken = null;

export function setToken(token) { authToken = token; }
export function getToken() { return authToken; }
export function clearToken() { authToken = null; }

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
        if (!response.ok) {
            let errorMessage = `HTTP Error: ${response.status}`;
            try { const errorData = await response.json(); errorMessage = errorData.message || errorMessage; } catch {}
            throw new Error(errorMessage);
        }
        return await response.json();
    } catch (error) {
        if (error.name === 'TypeError' && error.message === 'Failed to fetch') {
            throw new Error('Network error. Please check your connection.');
        }
        throw error;
    }
}

export async function get(endpoint, params = {}) {
    const queryString = new URLSearchParams(params).toString();
    const url = queryString ? `${endpoint}?${queryString}` : endpoint;
    return apiRequest(url, { method: 'GET' });
}

export async function post(endpoint, data = {}) {
    return apiRequest(endpoint, { method: 'POST', body: JSON.stringify(data) });
}

export default { get, post, setToken, getToken, clearToken };
