/**
 * Role -> landing page routing, shared between login.html and sidebar.js
 * so the two never drift apart.
    */
export const ROLE_DASHBOARDS = {
    Admin: '/frontend/pages/Admin/index.html',
    ResponsablePharmacie: '/frontend/pages/PharmacyManagement/dashboard.html',
    Medecin: '/frontend/pages/dashboard/index.html',
    Secretaire: '/frontend/pages/dashboard/secretaire/index.html',
};

export function getDashboardForRole(role) {
    return ROLE_DASHBOARDS[role] ;
}