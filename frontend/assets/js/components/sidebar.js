/**
 * Sidebar Component
 */

const navItems = [
    { section: 'main', title: 'Main', items: [
        { label: 'Dashboard', icon: '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="3" y="3" width="7" height="7" rx="1"/><rect x="14" y="3" width="7" height="7" rx="1"/><rect x="3" y="14" width="7" height="7" rx="1"/><rect x="14" y="14" width="7" height="7" rx="1"/></svg>', href: '/frontend/pages/dashboard/index.html', match: ['/frontend/pages/dashboard/'] },
    ]},
    { section: 'patients', title: 'Patients', items: [
        { label: 'Patient List', icon: '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"/><circle cx="9" cy="7" r="4"/><path d="M23 21v-2a4 4 0 0 0-3-3.87"/><path d="M16 3.13a4 4 0 0 1 0 7.75"/></svg>', href: '/frontend/pages/patients/list.html', match: ['/frontend/pages/patients/list.html', '/frontend/pages/patients/detail.html'] },
        { label: 'New Admission', icon: '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M16 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"/><circle cx="8.5" cy="7" r="4"/><line x1="20" y1="8" x2="20" y2="14"/><line x1="23" y1="11" x2="17" y2="11"/></svg>', href: '/frontend/pages/patients/admission.html', match: ['/frontend/pages/patients/admission.html'] },
    ]},
];

function isActivePage(matchPatterns) {
    const currentPath = window.location.pathname;
    return matchPatterns.some(pattern => currentPath.includes(pattern));
}

export function renderSidebar(containerId = 'sidebar-container') {
    const container = document.getElementById(containerId);
    if (!container) return;

    let html = `<div class="sidebar" id="sidebar">
        <div class="sidebar-header">
            <div class="sidebar-logo"><svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor"><path d="M12 2L2 7l10 5 10-5-10-5zM2 17l10 5 10-5M2 12l10 5 10-5"/></svg></div>
            <div class="sidebar-brand"><div class="sidebar-brand-title">CathLab</div><div class="sidebar-brand-subtitle">Management System</div></div>
        </div>
        <nav class="sidebar-nav">`;

    navItems.forEach(section => {
        html += `<div class="nav-section"><div class="nav-section-title">${section.title}</div>`;
        section.items.forEach(item => {
            const isActive = isActivePage(item.match) ? 'active' : '';
            html += `<a href="${item.href}" class="nav-item ${isActive}"><span class="nav-item-icon">${item.icon}</span><span class="nav-item-text">${item.label}</span></a>`;
        });
        html += `</div>`;
    });

    html += `</nav>
        <div class="sidebar-footer">
            <button class="sidebar-toggle" onclick="toggleSidebar()">
                <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="11 17 6 12 11 7"></polyline><polyline points="18 17 13 12 18 7"></polyline></svg>
                <span class="sidebar-toggle-text">Collapse</span>
            </button>
        </div>
    </div>`;

    container.innerHTML = html;

    const collapsed = localStorage.getItem('sidebarCollapsed') === 'true';
    if (collapsed) document.getElementById('sidebar')?.classList.add('collapsed');
}

window.toggleSidebar = function() {
    const sidebar = document.getElementById('sidebar');
    if (sidebar) {
        sidebar.classList.toggle('collapsed');
        localStorage.setItem('sidebarCollapsed', sidebar.classList.contains('collapsed'));
    }
};

export default { renderSidebar };
