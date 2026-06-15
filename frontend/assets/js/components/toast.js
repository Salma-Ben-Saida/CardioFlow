/**
 * Toast Component
 */

export const ToastType = { SUCCESS: 'success', ERROR: 'error', WARNING: 'warning', INFO: 'info' };

const toastIcons = {
    success: `<svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"/><polyline points="22 4 12 14.01 9 11.01"/></svg>`,
    error: `<svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="12" cy="12" r="10"/><line x1="15" y1="9" x2="9" y2="15"/><line x1="9" y1="9" x2="15" y2="15"/></svg>`,
    warning: `<svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z"/><line x1="12" y1="9" x2="12" y2="13"/><line x1="12" y1="17" x2="12.01" y2="17"/></svg>`,
    info: `<svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="12" cy="12" r="10"/><line x1="12" y1="16" x2="12" y2="12"/><line x1="12" y1="8" x2="12.01" y2="8"/></svg>`
};

const borderColors = { success: '#10B981', error: '#EF4444', warning: '#F59E0B', info: '#2563EB' };

function ensureContainer() {
    let container = document.getElementById('toasts-container');
    if (!container) {
        container = document.createElement('div');
        container.id = 'toasts-container';
        container.style.cssText = 'position:fixed;top:20px;right:20px;z-index:10000;display:flex;flex-direction:column;gap:10px;max-width:400px;pointer-events:none;';
        document.body.appendChild(container);
    }
    return container;
}

function addStyles() {
    if (document.getElementById('toast-styles')) return;
    const style = document.createElement('style');
    style.id = 'toast-styles';
    style.textContent = `
        @keyframes slideIn { from { transform: translateX(100%); opacity: 0; } to { transform: translateX(0); opacity: 1; } }
        @keyframes slideOut { from { transform: translateX(0); opacity: 1; } to { transform: translateX(100%); opacity: 0; } }
        .toast-content { flex: 1; } .toast-message { font-size: 14px; color: #1f2937; margin: 0; }
        .toast-close { background: none; border: none; cursor: pointer; padding: 4px; color: #9ca3af; display: flex; }
        .toast-close:hover { color: #4b5563; }
        .toast-icon { flex-shrink: 0; }
        .toast-success .toast-icon { color: #10B981; } .toast-error .toast-icon { color: #EF4444; }
        .toast-warning .toast-icon { color: #F59E0B; } .toast-info .toast-icon { color: #2563EB; }
    `;
    document.head.appendChild(style);
}

export function showToast(message, type = ToastType.INFO, duration = 5000) {
    addStyles();
    const container = ensureContainer();
    const toast = document.createElement('div');
    toast.className = `toast toast-${type}`;
    toast.style.cssText = `display:flex;align-items:flex-start;gap:12px;padding:16px;background:#fff;border-radius:8px;box-shadow:0 10px 15px -3px rgba(0,0,0,0.1);pointer-events:auto;animation:slideIn 0.3s ease-out;border-left:4px solid ${borderColors[type] || borderColors.info};`;

    const div = document.createElement('div');
    div.textContent = message;
    toast.innerHTML = `<span class="toast-icon">${toastIcons[type] || toastIcons.info}</span><div class="toast-content"><p class="toast-message">${div.innerHTML}</p></div><button class="toast-close" aria-label="Close"><svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><line x1="18" y1="6" x2="6" y2="18"></line><line x1="6" y1="6" x2="18" y2="18"></line></svg></button>`;

    container.appendChild(toast);
    toast.querySelector('.toast-close').addEventListener('click', () => dismissToast(toast));
    if (duration > 0) setTimeout(() => dismissToast(toast), duration);
    return toast;
}

function dismissToast(toast) {
    toast.style.animation = 'slideOut 0.3s ease-out forwards';
    setTimeout(() => toast.parentNode?.removeChild(toast), 300);
}

export function success(msg) { return showToast(msg, ToastType.SUCCESS); }
export function error(msg) { return showToast(msg, ToastType.ERROR, 8000); }
export function warning(msg) { return showToast(msg, ToastType.WARNING); }
export function info(msg) { return showToast(msg, ToastType.INFO); }

export default { show: showToast, success, error, warning, info, ToastType };
