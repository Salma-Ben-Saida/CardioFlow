/**
 * Role -> landing page routing, shared between login.html and sidebar.js
 * so the two never drift apart.
 */
export const ROLE_DASHBOARDS = {
    Admin: '/frontend/pages/Admin/index.html',
    ResponsablePharmacie: '/frontend/pages/PharmacyManagement/dashboard.html',
    Medecin: '/frontend/pages/dashboard/index.html',
    Secretaire: '/frontend/pages/secretaire/index.html',
};

export function getDashboardForRole(role) {
    if (!role) return '/frontend/pages/login.html';
    return ROLE_DASHBOARDS[role] || '/frontend/pages/dashboard/index.html';
}