/**
 * Role -> landing page routing, shared between login.html and sidebar.js
 * so the two never drift apart.
 *
 * NOTE: 'Doctor' and 'Medecin' are both mapped here defensively — the JWT
 * role claim and the [Authorize(Roles=...)] attributes elsewhere in the
 * codebase (e.g. ReportsController) don't agree on which spelling is used.
 * Worth confirming the real value against your User/role enum and
 * collapsing this back to one key once that's settled.
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