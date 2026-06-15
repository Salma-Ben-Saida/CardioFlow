/**
 * Utility functions
 */

export function formatDate(date) {
    if (!date) return '-';
    const d = typeof date === 'string' ? new Date(date) : date;
    if (isNaN(d.getTime())) return '-';
    const day = String(d.getDate()).padStart(2, '0');
    const month = String(d.getMonth() + 1).padStart(2, '0');
    const year = d.getFullYear();
    return `${day}/${month}/${year}`;
}

export function formatDateTime(date) {
    if (!date) return '-';
    const d = typeof date === 'string' ? new Date(date) : date;
    if (isNaN(d.getTime())) return '-';
    return `${formatDate(d)} ${String(d.getHours()).padStart(2, '0')}:${String(d.getMinutes()).padStart(2, '0')}`;
}

export function calculateAge(birthDate) {
    if (!birthDate) return null;
    const birth = typeof birthDate === 'string' ? new Date(birthDate.split('T')[0]) : birthDate;
    if (!birth || isNaN(birth.getTime())) return null;
    const today = new Date();
    let age = today.getFullYear() - birth.getFullYear();
    const monthDiff = today.getMonth() - birth.getMonth();
    if (monthDiff < 0 || (monthDiff === 0 && today.getDate() < birth.getDate())) age--;
    return age;
}

export function formatSex(sex) {
    if (!sex) return '-';
    return sex.toUpperCase() === 'M' ? 'Homme' : 'Femme';
}

export function formatStatusBadge(isIncomplete) {
    return isIncomplete ? `<span class="badge badge-warning">Incomplete</span>` : `<span class="badge badge-success">Complete</span>`;
}

export function getQueryParam(param) {
    const params = new URLSearchParams(window.location.search);
    if (param) return params.get(param);
    const result = {};
    params.forEach((value, key) => { result[key] = value; });
    return result;
}

export function setQueryParams(params) {
    const url = new URL(window.location.href);
    Object.entries(params).forEach(([key, value]) => {
        if (value !== null && value !== undefined && value !== '') url.searchParams.set(key, value);
        else url.searchParams.delete(key);
    });
    window.history.replaceState({}, '', url.toString());
}

export function debounce(func, wait) {
    let timeout;
    return function(...args) {
        clearTimeout(timeout);
        timeout = setTimeout(() => func(...args), wait);
    };
}

export function showLoading(container, message = 'Loading...') {
    const el = typeof container === 'string' ? document.getElementById(container) : container;
    if (el) el.innerHTML = `<div style="display:flex;flex-direction:column;align-items:center;justify-content:center;padding:2rem;"><div class="spinner"></div><p style="margin-top:0.75rem;color:#6b7280;font-size:0.875rem;">${message}</p></div>`;
}

export function showEmptyState(container, message, icon = 'file') {
    const el = typeof container === 'string' ? document.getElementById(container) : container;
    const icons = {
        file: `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/><polyline points="14 2 14 8 20 8"/></svg>`,
        user: `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5"><path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"/><circle cx="9" cy="7" r="4"/></svg>`,
        search: `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5"><circle cx="11" cy="11" r="8"/><line x1="21" y1="21" x2="16.65" y2="16.65"/></svg>`
    };
    if (el) el.innerHTML = `<div class="empty-state"><div class="empty-state-icon">${icons[icon] || icons.file}</div><p class="empty-state-description">${message}</p></div>`;
}

export default { formatDate, formatDateTime, calculateAge, formatSex, formatStatusBadge, getQueryParam, setQueryParams, debounce, showLoading, showEmptyState };
