/**
 * Pagination Component
 */

export function createPagination(options) {
    const { currentPage = 1, totalPages = 1, totalItems = 0, pageSize = 20, onChange = () => {} } = options;
    let page = currentPage;

    function getPageNumbers(current, total) {
        const pages = [];
        if (total <= 7) { for (let i = 1; i <= total; i++) pages.push(i); }
        else {
            pages.push(1);
            let start = Math.max(2, current - 1), end = Math.min(total - 1, current + 1);
            if (start > 2) pages.push('...');
            for (let i = start; i <= end; i++) pages.push(i);
            if (end < total - 1) pages.push('...');
            pages.push(total);
        }
        return pages;
    }

    function render() {
        const startItem = totalItems > 0 ? ((page - 1) * pageSize) + 1 : 0;
        const endItem = Math.min(page * pageSize, totalItems);
        let html = `<div class="pagination"><div class="pagination-info">Showing <strong>${startItem}</strong> - <strong>${endItem}</strong> of <strong>${totalItems}</strong> results</div><div class="pagination-controls">`;
        html += `<button class="pagination-btn ${page <= 1 ? 'disabled' : ''}" data-page="prev" ${page <= 1 ? 'disabled' : ''}><svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="15 18 9 12 15 6"></polyline></svg></button>`;
        getPageNumbers(page, totalPages).forEach(p => {
            if (p === '...') html += `<span class="pagination-ellipsis">...</span>`;
            else html += `<button class="pagination-btn ${p === page ? 'active' : ''}" data-page="${p}">${p}</button>`;
        });
        html += `<button class="pagination-btn ${page >= totalPages ? 'disabled' : ''}" data-page="next" ${page >= totalPages ? 'disabled' : ''}><svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="9 18 15 12 9 6"></polyline></svg></button>`;
        html += `</div></div>`;
        return html;
    }

    function goToPage(newPage) {
        if (newPage === 'prev') page = Math.max(1, page - 1);
        else if (newPage === 'next') page = Math.min(totalPages, page + 1);
        else page = parseInt(newPage, 10);
        onChange(page);
    }

    function attachEvents(container) {
        container.querySelectorAll('.pagination-btn[data-page]').forEach(btn => {
            btn.addEventListener('click', (e) => { e.preventDefault(); goToPage(btn.dataset.page); });
        });
    }

    function renderTo(target) {
        const container = typeof target === 'string' ? document.getElementById(target) : target;
        if (!container) return;
        container.innerHTML = render();
        attachEvents(container);
        if (!document.getElementById('pagination-styles')) {
            const style = document.createElement('style');
            style.id = 'pagination-styles';
            style.textContent = `.pagination{display:flex;align-items:center;justify-content:space-between;padding:1rem;flex-wrap:wrap;gap:1rem}.pagination-info{font-size:0.875rem;color:#4b5563}.pagination-controls{display:flex;align-items:center;gap:4px}.pagination-btn{display:inline-flex;align-items:center;justify-content:center;min-width:36px;height:36px;padding:0 8px;font-size:0.875rem;font-weight:500;color:#374151;background:#fff;border:1px solid #d1d5db;border-radius:0.375rem;cursor:pointer}.pagination-btn:hover:not(:disabled){background:#f9fafb}.pagination-btn.active{background:#2563EB;border-color:#2563EB;color:#fff}.pagination-btn:disabled{opacity:0.5;cursor:not-allowed}.pagination-ellipsis{display:inline-flex;align-items:center;justify-content:center;min-width:36px;height:36px;color:#6b7280}`;
            document.head.appendChild(style);
        }
    }

    return { render, renderTo, goToPage, getCurrentPage: () => page };
}

export default { create: createPagination };
