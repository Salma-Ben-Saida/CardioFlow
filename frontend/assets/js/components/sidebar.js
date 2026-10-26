/**
 * Sidebar Component
 */
import { getDashboardForRole } from '../rolesRoutes.js';
import { confirmDialog } from './confirm.js';

const currentUser = JSON.parse(sessionStorage.getItem('user') || 'null');
const currentRole = currentUser?.role;

// `roles: null` (or omitted) means visible to every authenticated role.
// Otherwise the item only shows if currentRole is in the list.
const navItems = [
    {
        section: 'main',
        title: 'Principal',
        items: [
            {
                label: 'Tableau de bord',
                icon: '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="3" y="3" width="7" height="7" rx="1"/><rect x="14" y="3" width="7" height="7" rx="1"/><rect x="3" y="14" width="7" height="7" rx="1"/><rect x="14" y="14" width="7" height="7" rx="1"/></svg>',
                href: getDashboardForRole(currentRole),
                match: ['/frontend/pages/dashboard/', '/frontend/pages/secretaire/'],
                roles: null
            }
        ]
    },
    {
        section: 'planning',
        title: 'Interventions',
        items: [
            {
                label: 'Planning des interventions',
                icon: '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="3" y="4" width="18" height="18" rx="2"/><line x1="16" y1="2" x2="16" y2="6"/><line x1="8" y1="2" x2="8" y2="6"/><line x1="3" y1="10" x2="21" y2="10"/></svg>',
                href: '/frontend/pages/studies/index.html',
                match: ['/frontend/pages/studies/'],
                roles: ['Medecin', 'Secretaire', 'Admin']
            }
        ]
    },
    {
        section: 'patients',
        title: 'Patients',
        items: [
            {
                label: 'Liste des patients',
                icon: '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"/><circle cx="9" cy="7" r="4"/><path d="M23 21v-2a4 4 0 0 0-3-3.87"/><path d="M16 3.13a4 4 0 0 1 0 7.75"/></svg>',
                href: '/frontend/pages/patients/list.html',
                match: ['/frontend/pages/patients/list.html', '/frontend/pages/patients/detail.html'],
                roles: null
            }
        ]
    },
    {
        section: 'pacs',
        title: 'Imagerie',
        items: [
            {
                label: 'Études PACS',
                icon: '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="3" y="3" width="18" height="18" rx="2"/><circle cx="8.5" cy="8.5" r="1.5"/><path d="M21 15l-5-5L5 21"/></svg>',
                href: '/frontend/pages/pacs/index.html',
                match: ['/frontend/pages/pacs/'],
                roles: ['Medecin', 'Secretaire']
            }
        ]
    }
];

function isActivePage(matchPatterns) {
    const currentPath = window.location.pathname;
    return matchPatterns.some(pattern => currentPath.includes(pattern));
}

function isVisible(item) {
    return !item.roles || item.roles.includes(currentRole);
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
        const visibleItems = section.items.filter(isVisible);
        if (visibleItems.length === 0) return;
        html += `<div class="nav-section">${visibleItems.length > 1 ? `<div class="nav-section-title">${section.title}</div>` : ''}`;
        visibleItems.forEach(item => {
            const isActive = isActivePage(item.match) ? 'active' : '';
            html += `<a href="${item.href}" class="nav-item ${isActive}"><span class="nav-item-icon">${item.icon}</span><span class="nav-item-text">${item.label}</span></a>`;
        });
        html += `</div>`;
    });

    html += `</nav>
        <div class="sidebar-footer">
            <button class="sidebar-toggle" onclick="toggleSidebar()" type="button">
                <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="11 17 6 12 11 7"></polyline><polyline points="18 17 13 12 18 7"></polyline></svg>
                <span class="sidebar-toggle-text">Réduire</span>
            </button>
            <button class="sidebar-logout" onclick="confirmLogout()" type="button">
                <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4"></path><path d="M16 17l5-5-5-5"></path><path d="M21 12H9"></path></svg>
                <span class="sidebar-logout-text">Déconnexion</span>
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

window.logoutUser = async function() {
    const confirmed = await confirmDialog({ title: 'Déconnexion', message: 'Êtes-vous sûr de vouloir vous déconnecter ?', confirmText: 'Se déconnecter' });
if (!confirmed) return;


    const token = sessionStorage.getItem('authToken');
    try {
        if (token) {
            await fetch('http://localhost:5152/api/Auth/logout', {
                method: 'POST',
                headers: {
                    'Authorization': `Bearer ${token}`,
                    'Content-Type': 'application/json',
                },
            });
        }
    } catch (error) {
        console.warn('Logout audit call failed:', error);
    } finally {
        sessionStorage.removeItem('authToken');
        sessionStorage.removeItem('user');
        localStorage.removeItem('sidebarCollapsed');
        window.location.href = '/frontend/pages/login.html';
    }
};

window.confirmLogout = window.logoutUser;

export default { renderSidebar };