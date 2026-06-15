/**
 * Utility functions
 */
export function formatDate(date) {
    if (!date) return '-';
    const d = typeof date === 'string' ? new Date(date) : date;
    if (isNaN(d.getTime())) return '-';
    return `${String(d.getDate()).padStart(2, '0')}/${String(d.getMonth() + 1).padStart(2, '0')}/${d.getFullYear()}`;
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
    return isIncomplete ? '<span class="badge badge-warning">Incomplete</span>' : '<span class="badge badge-success">Complete</span>';
}

export function getQueryParam(param) {
    const params = new URLSearchParams(window.location.search);
    if (param) return params.get(param);
    const result = {};
    params.forEach((v, k) => { result[k] = v; });
    return result;
}

export function setQueryParams(params) {
    const url = new URL(window.location.href);
    Object.entries(params).forEach(([k, v]) => {
        if (v !== null && v !== undefined && v !== '') url.searchParams.set(k, v);
        else url.searchParams.delete(k);
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

export function showEmptyState(container, message) {
    const el = typeof container === 'string' ? document.getElementById(container) : container;
    if (el) el.innerHTML = `<div class="empty-state"><div class="empty-state-icon"><svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/><polyline points="14 2 14 8 20 8"/></svg></div><p class="empty-state-description">${message}</p></div>`;
}

export default { formatDate, formatDateTime, calculateAge, formatSex, formatStatusBadge, getQueryParam, setQueryParams, debounce, showEmptyState };
