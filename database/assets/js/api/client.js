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
        if (response.status === 401) { clearToken(); window.location.href = 'pages/login.html'; throw new Error('Unauthorized'); }
        if (!response.ok) { let msg = `HTTP Error: ${response.status}`; try { const d = await response.json(); msg = d.message || msg; } catch {} throw new Error(msg); }
        return await response.json();
    } catch (error) {
        if (error.name === 'TypeError' && error.message === 'Failed to fetch') throw new Error('Network error. Please check your connection.');
        throw error;
    }
}

export async function get(endpoint, params = {}) {
    const qs = new URLSearchParams(params).toString();
    return apiRequest(qs ? `${endpoint}?${qs}` : endpoint, { method: 'GET' });
}

export async function post(endpoint, data = {}) {
    return apiRequest(endpoint, { method: 'POST', body: JSON.stringify(data) });
}

export default { get, post, setToken, getToken, clearToken };
