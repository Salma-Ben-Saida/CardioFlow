/* ============================================================
 * Administration — CathLab (vanilla JS, no framework)
 * ============================================================ */

import { get as apiGet, post as apiPost, put as apiPut, del as apiDelete } from '../../assets/js/api/client.js';
import { ingestPacsStudy } from '../../assets/js/api/pacs.js';
import {
  getPacsOverview, ingestAllPacs, getArchive,
  archiveStudy, restoreStudy, purgeStudy,
  archivePatient, restorePatient, purgePatient,
} from '../../assets/js/api/admin.js';
import { confirmDialog } from '../../assets/js/components/confirm.js';
import { showToast } from '../../assets/js/components/toast.js';
/* ---------- API config ---------- */
const API = {
  movements: (page = 1, pageSize = 50) => `/api/admin/movements?page=${page}&pageSize=${pageSize}`,
  users: (page = 1, pageSize = 50) => `/api/user?page=${page}&pageSize=${pageSize}`,
  createUser: '/api/user',
  reports: (doctorId) => `/api/reports${doctorId ? `?doctorId=${encodeURIComponent(doctorId)}` : ''}`,
  audits: (page = 1, pageSize = 50) => `/api/admin/audits?page=${page}&pageSize=${pageSize}`,
  logs: (page = 1, pageSize = 50) => `/api/admin/logs?page=${page}&pageSize=${pageSize}`,
  patientsCount: '/api/patients?page=1&pageSize=1',
  pharmacyLowStock: '/api/pharmacy/products/low-stock',
};

/* ---------- Access control ---------- */
const ALLOWED_ROLES = ['Admin'];
function getCurrentUser() {
  try {
    return JSON.parse(sessionStorage.getItem('user'));
  } catch {
    return null;
  }
}
function guardAccess() {
  const user = getCurrentUser();
  if (!user || !ALLOWED_ROLES.includes(user.role)) {
    window.location.href = '/frontend/pages/login.html';
    return false;
  }
  return true;
}

/* ---------- State ---------- */
const state = {
  currentView: 'dashboard',
  selectedUserForEdit: null,
  selectedUserForDelete: null,
  pendingArchive: null,
  pendingPurge: null,
};
/* ---------- DOM helpers ---------- */
const $ = (sel, root = document) => root.querySelector(sel);
const $$ = (sel, root = document) => Array.from(root.querySelectorAll(sel));

/* ---------- Utils ---------- */
function escapeHtml(s) {
  if (s == null) return '';
  return String(s).replace(/[&<>"']/g, (c) => ({
    '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;',
  }[c]));
}
function fmtDateTime(d) {
  if (!d) return '—';
  const dt = new Date(d);
  if (isNaN(dt)) return '—';
  return dt.toLocaleString('fr-FR', { dateStyle: 'short', timeStyle: 'short' });
}
function normalizeArray(data) {
  if (Array.isArray(data)) return data;
  if (data && Array.isArray(data.items)) return data.items;
  if (data && Array.isArray(data.Items)) return data.Items;
  return [];
}

/* ---------- Toast ---------- */
function toast(msg, type = 'success') { showToast(msg, type); }

/* ---------- Table helpers ---------- */
function setTableLoading(table, colspan, msg = 'Chargement…') {
  table.querySelector('tbody').innerHTML = `<tr class="row-loading"><td colspan="${colspan}">${msg}</td></tr>`;
}
function setTableError(table, colspan, msg) {
  table.querySelector('tbody').innerHTML = `<tr class="row-error"><td colspan="${colspan}">Erreur : ${escapeHtml(msg)}</td></tr>`;
}
function setTableEmpty(table, colspan, msg = 'Aucune donnée.') {
  table.querySelector('tbody').innerHTML = `<tr class="empty-row"><td colspan="${colspan}">${msg}</td></tr>`;
}

/* ---------- Navigation ---------- */
const VIEW_META = {
  dashboard: { title: 'Tableau de bord', sub: "Vue d'ensemble administrateur" },
  movements: { title: 'Mouvements de stock', sub: 'Historique des entrées/sorties' },
  users: { title: 'Utilisateurs', sub: 'Gestion des comptes' },
  reports: { title: 'Rapports', sub: 'Comptes-rendus de procédure' },
  audits: { title: 'Audits', sub: "Piste d'audit des actions" },
  logs: { title: 'Logs utilisateur', sub: 'Journal des connexions et actions' },
    pacs: { title: 'Études PACS', sub: 'Orthanc → CathLab : ingestion manuelle et état de synchronisation' },
  archive: { title: 'Archives', sub: 'Patients et études archivés : restauration ou suppression définitive' },
};

function switchView(view) {
  state.currentView = view;
  $$('.nav-item[data-view]').forEach((n) => n.classList.toggle('active', n.dataset.view === view));
  $$('.view').forEach((v) => v.classList.add('hidden'));
  $(`#view-${view}`)?.classList.remove('hidden');
  const meta = VIEW_META[view] || { title: view, sub: '' };
  $('#page-title').textContent = meta.title;
  $('#page-subtitle').textContent = meta.sub;
  ({
    dashboard: loadDashboard,
    movements: loadMovements,
    users: loadUsers,
    reports: loadReports,
    audits: loadAudits,
    logs: loadLogs,
    pacs: loadPacs,
    archive: loadArchive,
  }[view]?.());
}
// Number of rows previewed on the dashboard (tuned so that reports + PACS ≈ movements in height)
const DASH_MOVEMENTS = 7;
const DASH_REPORTS = 2;
const DASH_PACS = 2;
/* ---------- Dashboard ---------- */
async function loadDashboard() {
  const tables = {
    movements: $('#dash-movements-table'),
    reports: $('#dash-reports-table'),
    audits: $('#dash-audits-table'),
    logs: $('#dash-logs-table'),
    pacs: $('#dash-pacs-table'),
  };
  setTableLoading(tables.movements, 5);
  setTableLoading(tables.reports, 4);
  setTableLoading(tables.audits, 4);
  setTableLoading(tables.logs, 4);
  setTableLoading(tables.pacs, 3);
  $('#module-pharmacy-stat').textContent = 'Chargement…';
  $('#module-patients-stat').textContent = 'Chargement…';

  // Movements
  try {
    const data = await apiGet(API.movements(1, 5));
    const arr = normalizeArray(data);
    const total = data?.totalCount ?? data?.total ?? arr.length;
    $('#kpi-movements').textContent = total;
    if (!arr.length) setTableEmpty(tables.movements, 5, 'Aucun mouvement récent.');
    else tables.movements.querySelector('tbody').innerHTML = arr.map(movementRow).join('');
  } catch (e) {
    $('#kpi-movements').textContent = '—';
    setTableError(tables.movements, 5, e.message);
  }

  // Reports
  try {
    const data = await apiGet(API.reports());
    const arr = normalizeArray(data);
    const pending = arr.filter((r) => !r.isConfirmed).length;
    $('#kpi-reports-pending').textContent = pending;
    const preview = arr.slice(0, 5);
    if (!preview.length) setTableEmpty(tables.reports, 4, 'Aucun rapport.');
    else {
      tables.reports.querySelector('tbody').innerHTML = preview.map(reportRow).join('');
      tables.reports.querySelectorAll('tbody tr.clickable-row').forEach((row) => {
        row.addEventListener('click', () => {
          const reportId = row.dataset.reportId;
          const cathStudyId = row.dataset.cathStudyId;
          const patientId = row.dataset.patientId;
          if (!cathStudyId || !patientId) return;
          const url = `/frontend/pages/patients/coronary-map/coronaryMapping.html?cathStudyId=${encodeURIComponent(cathStudyId)}&patientId=${encodeURIComponent(patientId)}${reportId ? `&reportId=${encodeURIComponent(reportId)}` : ''}`;
          window.location.href = url;
        });
      });
    }
  } catch (e) {
    $('#kpi-reports-pending').textContent = '—';
    setTableError(tables.reports, 4, e.message);
  }

  // Users (active count only, for the KPI)
  try {
    const data = await apiGet(API.users(1, 100));
    const arr = normalizeArray(data);
    $('#kpi-users').textContent = arr.filter((u) => u.isActive).length;
  } catch {
    $('#kpi-users').textContent = '—';
  }

  // Audits
  try {
    const data = await apiGet(API.audits(1, 5));
    const arr = normalizeArray(data);
    const total = data?.totalCount ?? data?.total ?? arr.length;
    $('#kpi-audits').textContent = total;
    if (!arr.length) setTableEmpty(tables.audits, 4, 'Aucun audit.');
    else tables.audits.querySelector('tbody').innerHTML = arr.map(auditRow).join('');
  } catch (e) {
    $('#kpi-audits').textContent = '—';
    setTableError(tables.audits, 4, e.message);
  }

  // Logs
  try {
    const data = await apiGet(API.logs(1, 5));
    const arr = normalizeArray(data);
    if (!arr.length) setTableEmpty(tables.logs, 4, 'Aucun log.');
    else tables.logs.querySelector('tbody').innerHTML = arr.map(logRow).join('');
  } catch (e) {
    setTableError(tables.logs, 4, e.message);
  }

  // Pharmacy module stat
  try {
    const data = await apiGet(API.pharmacyLowStock);
    const arr = normalizeArray(data);
    $('#module-pharmacy-stat').textContent = arr.length
      ? `${arr.length} produit${arr.length > 1 ? 's' : ''} en stock faible`
      : 'Stock au niveau normal';
  } catch {
    $('#module-pharmacy-stat').textContent = 'Ouvrir le module';
  }

  // Patients module stat
  try {
    const data = await apiGet(API.patientsCount);
    const total = data.totalCount ?? data.total ?? normalizeArray(data).length;
    $('#module-patients-stat').textContent = `${total} patient${total > 1 ? 's' : ''} enregistré${total > 1 ? 's' : ''}`;
  } catch {
    $('#module-patients-stat').textContent = 'Ouvrir le module';
  }
    // PACS studies preview (new studies come first)
  try {
    const arr = normalizeArray(await getPacsOverview());
    const preview = arr.slice(0, DASH_PACS);
    if (!preview.length) setTableEmpty(tables.pacs, 3, 'Aucune étude dans Orthanc.');
    else tables.pacs.querySelector('tbody').innerHTML = preview.map(dashPacsRow).join('');
  } catch (e) {
    setTableError(tables.pacs, 3, e.message);
  }
}
function dashPacsRow(s) {
  const st = PACS_STATE[s.state] || { label: s.state, pill: 'pill-grey' };
  return `<tr>
    <td>${escapeHtml(s.accessionNumber)}</td>
    <td>${escapeHtml(s.patientName || '—')}</td>
    <td><span class="pill ${st.pill}">${st.label}</span></td>
  </tr>`;
}
function movementRow(m) {
  return `<tr>
    <td>${fmtDateTime(m.movedAt)}</td>
    <td>${escapeHtml(m.productName || '—')}</td>
    <td>${movementTypeCell(m.movementType)}</td>
    <td>${m.quantity ?? '—'}</td>
    <td>${escapeHtml(m.actorName || m.createdBy || '—')}</td>
  </tr>`;
}

function reportRow(r) {
  return `<tr data-report-id="${r.id || ''}" data-cath-study-id="${r.cathStudyId || ''}" data-patient-id="${r.patientId || ''}" class="clickable-row" style="cursor:pointer;">
    <td>${escapeHtml(r.patientName)}</td>
    <td>${escapeHtml(r.doctorName || '—')}</td>
    <td>${fmtDateTime(r.reportGeneratedAt)}</td>
    <td>${r.isConfirmed ? '<span class="pill pill-green">Confirmé</span>' : '<span class="pill pill-amber">Brouillon</span>'}</td>
  </tr>`;
}

function auditRow(a) {
  return `<tr>
    <td>${fmtDateTime(a.occurredAt)}</td>
    <td>${escapeHtml(a.actorName || a.username || '—')}</td>
    <td>${actionCell(a.action)}</td>
    <td>${entityCell(a.entityType)}</td>
  </tr>`;
}

function logRow(l) {
  return `<tr>
    <td>${fmtDateTime(l.occurredAt)}</td>
    <td>${escapeHtml(l.username)}</td>
    <td>${actionCell(l.action)}</td>
    <td>${escapeHtml(l.userAgent || '—')}</td>
  </tr>`;
}

/* ---------- Stock movements ---------- */
const STOCK_MOVEMENT_TYPES = {
  Reception:   { label: 'Réception',    pill: 'pill-purple' },
  Consumption: { label: 'Consommation', pill: 'pill-orange' },
  Return:      { label: 'Retour',       pill: 'pill-blue'   },
  Waste:       { label: 'Rebut',        pill: 'pill-red'    },
};

function movementTypeCell(type) {
  const key = typeof type === 'string' ? type.trim() : '';
  const m = STOCK_MOVEMENT_TYPES[key] || STOCK_MOVEMENT_TYPES[key.charAt(0).toUpperCase() + key.slice(1)];
  return m
    ? `<span class="pill ${m.pill}">${m.label}</span>`
    : `<span class="pill pill-grey">${escapeHtml(type || '—')}</span>`;
}

async function loadMovements() {
  const table = $('#movements-table');
  setTableLoading(table, 6);
  try {
    const data = await apiGet(API.movements(1, 100));
    const arr = normalizeArray(data);
    if (!arr.length) return setTableEmpty(table, 6, 'Aucun mouvement.');
    table.querySelector('tbody').innerHTML = arr.map((m) => `<tr>
      <td>${fmtDateTime(m.movedAt)}</td>
      <td>${escapeHtml(m.productName || '—')}</td>
      <td>${movementTypeCell(m.movementType)}</td>
      <td>${m.quantity ?? '—'}</td>
      <td>${escapeHtml(m.actorName || m.createdBy || '—')}</td>
    </tr>`).join('');
  } catch (e) {
    setTableError(table, 6, e.message);
    toast('Erreur lors du chargement des mouvements', 'error');
  }
}

/* ---------- Users ---------- */
async function loadUsers() {
  const table = $('#users-table');
  setTableLoading(table, 6);
  try {
    const data = await apiGet(API.users());
    const arr = normalizeArray(data);
    if (!arr.length) return setTableEmpty(table, 6, 'Aucun utilisateur.');
    table.querySelector('tbody').innerHTML = arr.map((u) => `<tr>
      <td>${escapeHtml(u.fullName)}</td>
      <td>${escapeHtml(u.username)}</td>
      <td>${escapeHtml(u.role)}</td>
      <td>${u.isActive ? '<span class="pill pill-green">Actif</span>' : '<span class="pill pill-grey">Inactif</span>'}</td>
      <td>${fmtDateTime(u.createdAt)}</td>
      <td>
        <div style="display:flex;gap:8px;align-items:center;">
          <button class="icon-btn" data-action="edit-user" data-user-id="${u.id}" title="Modifier" aria-label="Modifier l'utilisateur">✏️</button>
          <button class="icon-btn" data-action="delete-user" data-user-id="${u.id}" title="Supprimer" aria-label="Supprimer l'utilisateur">🗑️</button>
        </div>
      </td>
    </tr>`).join('');

    table.querySelectorAll('[data-action="edit-user"]').forEach((btn) => {
      btn.addEventListener('click', () => openEditUserModal(btn.dataset.userId));
    });
    table.querySelectorAll('[data-action="delete-user"]').forEach((btn) => {
      btn.addEventListener('click', () => openDeleteUserModal(btn.dataset.userId));
    });
  } catch (e) {
    setTableError(table, 6, e.message);
    toast('Erreur lors du chargement des utilisateurs', 'error');
  }
}

async function saveUser() {
  const body = {
    fullName: $('#u-fullname').value.trim(),
    username: $('#u-name').value.trim(),
    password: $('#u-password').value,
    role: $('#u-role').value,
  };
  if (!body.fullName || !body.username || !body.password) {
    toast('Tous les champs sont requis', 'error');
    return;
  }
  try {
    await apiPost(API.createUser, body);
    closeModal('modal-user');
    toast('Utilisateur créé avec succès');
    loadUsers();
  } catch (e) {
    toast(`Erreur : ${e.message}`, 'error');
  }
}

async function openEditUserModal(userId) {
  try {
    const user = await apiGet(`${API.users(1, 100)}`);
    const list = normalizeArray(user);
    const item = list.find((u) => u.id === userId);
    if (!item) {
      toast('Utilisateur introuvable.', 'error');
      return;
    }

    state.selectedUserForEdit = item;
    $('#eu-fullname').value = item.fullName || '';
    $('#eu-name').value = item.username || '';
    $('#eu-password').value = '';
    $('#eu-role').value = item.role || 'Medecin';
    $('#eu-active').value = String(Boolean(item.isActive));
    openModal('modal-edit-user');
  } catch (e) {
    toast(`Erreur : ${e.message}`, 'error');
  }
}

async function saveEditedUser() {
  if (!state.selectedUserForEdit) return;

  const payload = {
    fullName: $('#eu-fullname').value.trim(),
    username: $('#eu-name').value.trim(),
    password: $('#eu-password').value.trim() || undefined,
    role: $('#eu-role').value,
    isActive: $('#eu-active').value === 'true'
  };

  if (!payload.fullName || !payload.username) {
    toast('Nom complet et identifiant sont requis.', 'error');
    return;
  }

  try {
    await apiPut(`/api/user/${state.selectedUserForEdit.id}`, payload);
    closeModal('modal-edit-user');
    state.selectedUserForEdit = null;
    toast('Utilisateur modifié avec succès');
    loadUsers();
  } catch (e) {
    toast(`Erreur : ${e.message}`, 'error');
  }
}

async function openDeleteUserModal(userId) {
  try {
    const response = await apiGet(API.users(1, 100));
    const users = normalizeArray(response);
    const item = users.find((u) => u.id === userId);
    if (!item) {
      toast('Utilisateur introuvable.', 'error');
      return;
    }

    state.selectedUserForDelete = item;
    $('#delete-user-message').textContent = `Voulez-vous vraiment supprimer ${item.fullName || item.username} ?`;
    openModal('modal-delete-user');
  } catch (e) {
    toast(`Erreur : ${e.message}`, 'error');
  }
}

async function confirmDeleteUser() {
  if (!state.selectedUserForDelete) return;

  try {
  await apiDelete(`/api/user/${state.selectedUserForDelete.id}`);
  closeModal('modal-delete-user');
  state.selectedUserForDelete = null;
  toast('Utilisateur supprimé avec succès');
  loadUsers();
} catch (e) {
  toast(`Erreur : ${e.message}`, 'error');
}
}
/* ---------- Reports ---------- */
async function loadReports() {
  const table = $('#reports-table');
  setTableLoading(table, 5);
  try {
    state.reports = normalizeArray(await apiGet(API.reports()));
    renderReports();
  } catch (e) {
    setTableError(table, 5, e.message);
    toast('Erreur lors du chargement des rapports', 'error');
  }
}

function renderReports() {
  const table = $('#reports-table');
  const q = ($('#reports-search').value || '').trim().toLowerCase();
  const st = $('#reports-status').value;
  const arr = (state.reports || []).filter((r) =>
    (!st || (st === 'confirmed') === !!r.isConfirmed) &&
    (!q || `${r.patientName} ${r.doctorName}`.toLowerCase().includes(q)));
  if (!arr.length) return setTableEmpty(table, 5, 'Aucun rapport.');
  table.querySelector('tbody').innerHTML = arr.map((r) => `<tr data-report-id="${r.id || ''}" data-cath-study-id="${r.cathStudyId || ''}" data-patient-id="${r.patientId || ''}" class="clickable-row" style="cursor:pointer;">
    <td>${escapeHtml(r.patientName)}</td>
    <td>${escapeHtml(r.doctorName || '—')}</td>
    <td>${fmtDateTime(r.reportGeneratedAt)}</td>
    <td>${r.isConfirmed ? '<span class="pill pill-green">Confirmé</span>' : '<span class="pill pill-amber">Brouillon</span>'}</td>
    <td><div style="display:flex;gap:8px;align-items:center;">
      <button class="icon-btn" data-action="edit-report" title="Modifier">✏️</button>
      <button class="icon-btn" data-action="delete-report" title="Supprimer">🗑️</button>
    </div></td></tr>`).join('');
  table.querySelectorAll('tbody tr.clickable-row').forEach((row) => {
    row.addEventListener('click', () => openReportInMapping(row, false));
    row.querySelector('[data-action="edit-report"]').addEventListener('click', (ev) => { ev.stopPropagation(); openReportInMapping(row, true); });
    row.querySelector('[data-action="delete-report"]').addEventListener('click', (ev) => { ev.stopPropagation(); openDeleteReportModal(row.dataset.reportId); });
  });
}

function openReportInMapping(row, editMode) {
  const { cathStudyId, patientId, reportId } = row.dataset;
  if (!cathStudyId || !patientId) return;
  const qs = new URLSearchParams({ cathStudyId, patientId });
  if (reportId) qs.set('reportId', reportId);
  if (editMode) qs.set('mode', 'edit');
  window.location.href = `/frontend/pages/patients/coronary-map/coronaryMapping.html?${qs}`;
}

function openDeleteReportModal(reportId) {
  const item = (state.reports || []).find((r) => r.id === reportId);
  if (!item) { toast('Rapport introuvable.', 'error'); return; }
  state.selectedReportForDelete = item;
  $('#delete-report-message').textContent = item.isConfirmed
    ? `Le rapport CONFIRMÉ de ${item.patientName || 'ce patient'} sera supprimé avec ses lésions et traitements. Les dispositifs déjà consommés restent déduits du stock.`
    : `Voulez-vous vraiment supprimer le brouillon de ${item.patientName || 'ce patient'} ? Les dispositifs réservés retourneront en stock.`;
  openModal('modal-delete-report');
}
async function confirmDeleteReport() {
  const r = state.selectedReportForDelete;
  if (!r) return;
  const btn = $('#btn-confirm-delete-report');
  btn.disabled = true;
  try {
    await apiDelete(`/api/procedures/reports/${r.id}`);
    closeModal('modal-delete-report');
    state.selectedReportForDelete = null;
    toast('Rapport supprimé avec succès');
    loadReports();
  } catch (e) { toast(`Erreur : ${e.message}`, 'error'); }
  finally { btn.disabled = false; }
}

/* ---------- Audits ---------- */
async function loadAudits() {
  const table = $('#audits-table');
  setTableLoading(table, 5);
  try {
    const data = await apiGet(API.audits());
    const arr = normalizeArray(data);
    if (!arr.length) return setTableEmpty(table, 5, 'Aucun audit.');
    table.querySelector('tbody').innerHTML = arr.map((a) => `<tr>
      <td>${fmtDateTime(a.occurredAt)}</td>
      <td>${escapeHtml(a.actorName)}</td>
      <td>${actionCell(a.action)}</td>
      <td>${entityCell(a.entityType)}</td>
      <td>${escapeHtml(a.detail || '—')}</td>
    </tr>`).join('');
  } catch (e) {
    setTableError(table, 5, e.message);
    toast('Erreur lors du chargement des audits', 'error');
  }
}

/* ---------- User activity logs ---------- */
async function loadLogs() {
  const table = $('#logs-table');
  setTableLoading(table, 5);
  try {
    const data = await apiGet(API.logs());
    const arr = normalizeArray(data);
    if (!arr.length) return setTableEmpty(table, 5, 'Aucun log.');
    table.querySelector('tbody').innerHTML = arr.map((l) => `<tr>
      <td>${fmtDateTime(l.occurredAt)}</td>
      <td>${escapeHtml(l.username)}</td>
      <td>${actionCell(l.action)}</td>
      <td>${escapeHtml(l.ipAddress || '—')}</td>
      <td>${escapeHtml(l.userAgent || '—')}</td>
    </tr>`).join('');
  } catch (e) {
    setTableError(table, 5, e.message);
    toast('Erreur lors du chargement des logs', 'error');
  }
}
/* ---------- Audit action labels ---------- */
const AUDIT_ACTIONS = {
  // Données
  Create:       { label: 'Création',               pill: 'pill-green'  },
  Update:       { label: 'Modification',           pill: 'pill-blue'   },
  Delete:       { label: 'Suppression',            pill: 'pill-red'    },
  Confirm:      { label: 'Confirmation',           pill: 'pill-green'  },
  // Archivage
  Archive:      { label: 'Archivage',              pill: 'pill-amber'  },
  Restore:      { label: 'Restauration',           pill: 'pill-green'  },
  Purge:        { label: 'Suppression définitive', pill: 'pill-red'    },
  // Pharmacie / stock
  Receive:      { label: 'Réception',              pill: 'pill-purple' },
  ReceiveBatch: { label: 'Réception (lot)',        pill: 'pill-purple' },
  Consume:      { label: 'Consommation',           pill: 'pill-purple' },
  Waste:        { label: 'Rebut',                  pill: 'pill-red'    },
  // Connexions (vue « Logs utilisateur »)
  LoginSuccess: { label: 'Connexion réussie',      pill: 'pill-green'  },
  LoginFailed:  { label: 'Échec de connexion',     pill: 'pill-red'    },
  Logout:       { label: 'Déconnexion',            pill: 'pill-grey'   },
  // Planning des interventions
  Schedule:     { label: 'Planification',          pill: 'pill-blue'   },
  Cancel:       { label: 'Annulation',             pill: 'pill-orange' },
};

// Unknown actions still get a (grey) pill, showing the raw value
function actionCell(action) {
  const m = AUDIT_ACTIONS[action];
  return m
    ? `<span class="pill ${m.pill}">${m.label}</span>`
    : `<span class="pill pill-grey">${escapeHtml(action || '—')}</span>`;
}

const ENTITY_LABELS = {
  Patient:          'Patient',
  CathStudy:        'Étude',
  ProcedureReport:  'Rapport de procédure',
  InventoryProduct: 'Produit (pharmacie)',
  InventoryItem:    'Dispositif (pharmacie)',
  AppUser:          'Utilisateur',
};
function entityCell(type) {
  return escapeHtml(ENTITY_LABELS[type] || type || '—');
}

/* ---------- Shared helpers (PACS + archive) ---------- */
function fmtDate(d) {
  if (!d) return '—';
  const dt = typeof d === 'string' && d.length === 10 ? new Date(`${d}T00:00:00`) : new Date(d);
  return isNaN(dt) ? '—' : dt.toLocaleDateString('fr-FR');
}

// Disables the button while the action runs; re-enables it only if the action failed
// (on success the table is reloaded, which replaces the button).
async function withBusy(btn, fn) {
  btn.disabled = true;
  try {
    await fn();
  } catch (e) {
    toast(`Erreur : ${e.message}`, 'error');
    btn.disabled = false;
  }
}

/* ---------- PACS studies (Orthanc → CathLab) ---------- */
const PACS_STATE = {
  New: { label: 'Nouvelle', pill: 'pill-blue' },
  Ingested: { label: 'Ingérée', pill: 'pill-green' },
  Archived: { label: 'Archivée', pill: 'pill-amber' },
  PatientArchived: { label: 'Patient archivé', pill: 'pill-grey' },
};

function pacsActions(s) {
  const acc = escapeHtml(s.accessionNumber);
  switch (s.state) {
    case 'New':
      return `<button class="btn btn-primary btn-sm" data-pacs-action="ingest" data-accession="${acc}">Ingérer</button>`;
    case 'Ingested':
      return `<a class="btn btn-ghost btn-sm" href="/frontend/pages/patients/detail.html?id=${encodeURIComponent(s.patientId)}">Voir le patient</a>
        <button class="btn btn-danger btn-sm" data-pacs-action="archive-study" data-id="${s.cathStudyId}" data-accession="${acc}">Archiver</button>`;
    case 'Archived':
      return `<button class="btn btn-ghost btn-sm" data-pacs-action="restore-study" data-id="${s.cathStudyId}">Restaurer l'étude</button>`;
    case 'PatientArchived':
      return `<button class="btn btn-ghost btn-sm" data-pacs-action="restore-patient" data-id="${s.patientId}">Restaurer le patient</button>`;
    default:
      return '';
  }
}

function pacsRow(s) {
  const st = PACS_STATE[s.state] || { label: s.state, pill: 'pill-grey' };
  return `<tr>
    <td>${escapeHtml(s.accessionNumber)}</td>
    <td>${escapeHtml(s.patientName || '—')}<span class="cell-sub">${escapeHtml(s.patientDicomId || '')}</span></td>
    <td>${fmtDate(s.studyDate)}</td>
    <td>${escapeHtml(s.modality || '—')}</td>
    <td><span class="pill ${st.pill}">${st.label}</span></td>
    <td><div class="row-actions">${pacsActions(s)}</div></td>
  </tr>`;
}

function renderPacsSummary(arr) {
  const count = (key) => arr.filter((s) => s.state === key).length;
  const nNew = count('New');
  $('#pacs-summary').innerHTML = Object.entries(PACS_STATE)
    .map(([key, m]) => `<span class="pill ${m.pill}">${m.label} : ${count(key)}</span>`)
    .join('');
  const btn = $('#btn-pacs-ingest-all');
  btn.disabled = nNew === 0;
  btn.textContent = nNew ? `Ingérer ${nNew === 1 ? 'la nouvelle étude' : `les ${nNew} nouvelles`}` : 'Aucune nouvelle étude';
}

async function loadPacs() {
  const table = $('#pacs-table');
  setTableLoading(table, 6);
  try {
    const arr = normalizeArray(await getPacsOverview());
    renderPacsSummary(arr);
    if (!arr.length) return setTableEmpty(table, 6, 'Aucune étude dans Orthanc.');
    table.querySelector('tbody').innerHTML = arr.map(pacsRow).join('');
  } catch (e) {
    setTableError(table, 6, e.message);
    toast('Erreur lors du chargement des études PACS', 'error');
  }
}

async function ingestAllNew() {
  const btn = $('#btn-pacs-ingest-all');
  btn.disabled = true;
  btn.textContent = 'Ingestion en cours…';
  try {
    const results = normalizeArray(await ingestAllPacs());
    const created = results.filter((r) => r.succeeded && !r.wasAlreadyIngested && !r.wasArchived).length;
    const skipped = results.filter((r) => r.wasArchived).length;
    const failed = results.filter((r) => !r.succeeded).length;
    toast(
      `${created} étude(s) ingérée(s)` +
        (skipped ? `, ${skipped} archivée(s) ignorée(s)` : '') +
        (failed ? `, ${failed} échec(s)` : ''),
      failed ? 'error' : 'success'
    );
  } catch (e) {
    toast(`Erreur : ${e.message}`, 'error');
  }
  await loadPacs(); // recomputes the button label and state
}

async function onPacsAction(e) {
  const btn = e.target.closest('[data-pacs-action]');
  if (!btn || btn.disabled) return;
  const { pacsAction, id, accession } = btn.dataset;

  switch (pacsAction) {
    case 'ingest':
      return withBusy(btn, async () => {
        const r = await ingestPacsStudy(accession, null); // null = no manual override of patient data
        if (r.wasArchived) toast(r.info || 'Étude archivée : non ingérée', 'error');
        else if (r.wasAlreadyIngested) toast('Étude déjà ingérée');
        else toast(r.patientIsIncomplete ? 'Étude ingérée — dossier patient incomplet' : 'Étude ingérée');
        await loadPacs();
      });
    case 'archive-study':
      return openArchiveModal({ kind: 'study', id, label: `l'étude ${accession}` });
    case 'restore-study':
      return withBusy(btn, async () => {
        await restoreStudy(id);
        toast('Étude restaurée');
        await loadPacs();
      });
    case 'restore-patient':
      return withBusy(btn, async () => {
        await restorePatient(id);
        toast('Patient restauré');
        await loadPacs();
      });
  }
}

/* ---------- Archive modal ---------- */
function openArchiveModal(target) {
  state.pendingArchive = target;
  $('#archive-title').textContent = target.kind === 'patient' ? 'Archiver le patient' : "Archiver l'étude";
  $('#archive-message').textContent =
    `Vous allez archiver ${target.label}. ` +
    (target.kind === 'patient' ? 'Toutes ses études seront archivées avec lui. ' : '') +
    'Les données ne seront plus visibles et ne seront pas ré-ingérées automatiquement. ' +
    'Elles pourront être restaurées depuis « Archives ».';
  $('#archive-reason').value = '';
  openModal('modal-archive');
  $('#archive-reason').focus();
}

async function confirmArchive() {
  const t = state.pendingArchive;
  if (!t) return;
  const reason = $('#archive-reason').value.trim();
  if (reason.length < 3) {
    toast("Un motif d'au moins 3 caractères est obligatoire.", 'error');
    return;
  }
  const btn = $('#btn-confirm-archive');
  btn.disabled = true;
  try {
    await (t.kind === 'patient' ? archivePatient(t.id, reason) : archiveStudy(t.id, reason));
    closeModal('modal-archive');
    state.pendingArchive = null;
    toast(t.kind === 'patient' ? 'Patient archivé' : 'Étude archivée');
    if (state.currentView === 'pacs') loadPacs();
  } catch (e) {
    toast(`Erreur : ${e.message}`, 'error');
  } finally {
    btn.disabled = false;
  }
}

/* ---------- Archive view ---------- */
function archivedPatientRow(p) {
  return `<tr>
    <td>${escapeHtml(p.fullName)}<span class="cell-sub">${escapeHtml(p.conquestPatientID || '')}</span></td>
    <td>${p.studyCount ?? 0}</td>
    <td>${fmtDateTime(p.deletedAt)}</td>
    <td>${escapeHtml(p.deletedByName || '—')}</td>
    <td>${escapeHtml(p.deleteReason || '—')}</td>
    <td><div class="row-actions">
      <button class="btn btn-ghost btn-sm" data-archive-action="restore-patient" data-id="${p.id}">Restaurer</button>
      <button class="btn btn-danger btn-sm" data-archive-action="purge-patient" data-id="${p.id}" data-label="le patient ${escapeHtml(p.fullName)}">Supprimer définitivement</button>
    </div></td>
  </tr>`;
}

function archivedStudyRow(s) {
  const restore = s.patientArchived
    ? `<button class="btn btn-ghost btn-sm" disabled title="Restaurez d'abord le patient">Restaurer</button>`
    : `<button class="btn btn-ghost btn-sm" data-archive-action="restore-study" data-id="${s.id}">Restaurer</button>`;
  return `<tr>
    <td>${escapeHtml(s.accessionNumber)}${s.deletedWithPatient ? '<span class="cell-sub">Archivée avec le patient</span>' : ''}</td>
    <td>${escapeHtml(s.patientName || '—')}${s.patientArchived ? '<span class="cell-sub">Patient archivé</span>' : ''}</td>
    <td>${fmtDate(s.studyDate)}</td>
    <td>${fmtDateTime(s.deletedAt)}</td>
    <td>${escapeHtml(s.deletedByName || '—')}</td>
    <td>${escapeHtml(s.deleteReason || '—')}</td>
    <td><div class="row-actions">
      ${restore}
      <button class="btn btn-danger btn-sm" data-archive-action="purge-study" data-id="${s.id}" data-label="l'étude ${escapeHtml(s.accessionNumber)}">Supprimer définitivement</button>
    </div></td>
  </tr>`;
}

async function loadArchive() {
  const pTable = $('#archive-patients-table');
  const sTable = $('#archive-studies-table');
  setTableLoading(pTable, 6);
  setTableLoading(sTable, 7);
  try {
    const data = await getArchive();
    const patients = data?.patients ?? [];
    const studies = data?.studies ?? [];
    if (patients.length) pTable.querySelector('tbody').innerHTML = patients.map(archivedPatientRow).join('');
    else setTableEmpty(pTable, 6, 'Aucun patient archivé.');
    if (studies.length) sTable.querySelector('tbody').innerHTML = studies.map(archivedStudyRow).join('');
    else setTableEmpty(sTable, 7, 'Aucune étude archivée.');
  } catch (e) {
    setTableError(pTable, 6, e.message);
    setTableError(sTable, 7, e.message);
    toast('Erreur lors du chargement des archives', 'error');
  }
}

async function onArchiveAction(e) {
  const btn = e.target.closest('[data-archive-action]');
  if (!btn || btn.disabled) return;
  const { archiveAction, id, label } = btn.dataset;

  switch (archiveAction) {
    case 'restore-study':
      return withBusy(btn, async () => {
        await restoreStudy(id);
        toast('Étude restaurée');
        await loadArchive();
      });
    case 'restore-patient':
      return withBusy(btn, async () => {
        await restorePatient(id);
        toast('Patient restauré');
        await loadArchive();
      });
    case 'purge-study':
      return openPurgeModal({ kind: 'study', id, label: label || 'cette étude' });
    case 'purge-patient':
      return openPurgeModal({ kind: 'patient', id, label: label || 'ce patient' });
  }
}

/* ---------- Purge modal (type-to-confirm) ---------- */
const PURGE_WORD = 'SUPPRIMER';

function openPurgeModal(target) {
  state.pendingPurge = target;
  $('#purge-message').textContent =
    `Vous allez supprimer définitivement ${target.label}` +
    (target.kind === 'patient'
      ? ', ainsi que toutes ses études, rapports et traitements. '
      : ', ainsi que son rapport et ses traitements. ') +
    'Les dispositifs réservés seront remis en stock. Action irréversible : ' +
    'si les études existent encore dans Orthanc, elles pourront être ré-ingérées, mais sans aucune information saisie.';
  $('#purge-confirm').value = '';
  $('#btn-confirm-purge').disabled = true;
  openModal('modal-purge');
  $('#purge-confirm').focus();
}

async function confirmPurge() {
  const t = state.pendingPurge;
  if (!t || $('#purge-confirm').value.trim() !== PURGE_WORD) return;
  const btn = $('#btn-confirm-purge');
  btn.disabled = true;
  try {
    await (t.kind === 'patient' ? purgePatient(t.id) : purgeStudy(t.id));
    closeModal('modal-purge');
    state.pendingPurge = null;
    toast('Supprimé définitivement');
    loadArchive();
  } catch (e) {
    toast(`Erreur : ${e.message}`, 'error');
    btn.disabled = false;
  }
}
/* ---------- Modals ---------- */
function openModal(id) { $(`#${id}`).classList.remove('hidden'); }
function closeModal(id) { $(`#${id}`).classList.add('hidden'); }
function openUserModal() {
  $('#form-user').reset();
  openModal('modal-user');
}

/* ---------- Init ---------- */
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

function init() {
  const user = getCurrentUser();
  $('#user-name').textContent = user?.fullName || user?.username || '—';
  $('#user-avatar').textContent = (user?.fullName || user?.username || '??').slice(0, 2).toUpperCase();

  // nav
  $$('.nav-item[data-view]').forEach((n) => n.addEventListener('click', (e) => {
    e.preventDefault();
    switchView(n.dataset.view);
  }));
  $$('.panel-link').forEach((l) => l.addEventListener('click', (e) => {
    e.preventDefault();
    switchView(l.dataset.goto);
  }));

  // users
  $('#btn-new-user').addEventListener('click', openUserModal);
  $('#btn-save-user').addEventListener('click', saveUser);
  $('#btn-save-edit-user').addEventListener('click', saveEditedUser);
  $('#btn-confirm-delete-user').addEventListener('click', confirmDeleteUser);
  $('#btn-confirm-delete-report').addEventListener('click', confirmDeleteReport);


// reports
$('#reports-search').addEventListener('input', renderReports);
$('#reports-status').addEventListener('change', renderReports);

  // PACS studies + archive  
  $('#btn-pacs-refresh').addEventListener('click', loadPacs);
  $('#btn-pacs-ingest-all').addEventListener('click', ingestAllNew);
  $('#pacs-table').addEventListener('click', onPacsAction);
  $('#archive-patients-table').addEventListener('click', onArchiveAction);
  $('#archive-studies-table').addEventListener('click', onArchiveAction);
  $('#btn-confirm-archive').addEventListener('click', confirmArchive);
  $('#btn-confirm-purge').addEventListener('click', confirmPurge);
  $('#purge-confirm').addEventListener('input', (e) => {
    $('#btn-confirm-purge').disabled = e.target.value.trim() !== PURGE_WORD;
  });

  // modal close
  $$('[data-close-modal]').forEach((b) => b.addEventListener('click', () => closeModal(b.dataset.closeModal)));
  $$('.modal-overlay').forEach((o) => o.addEventListener('click', (e) => {
    if (e.target === o) o.classList.add('hidden');
  }));

  // initial load
  switchView('dashboard');
}

document.addEventListener('DOMContentLoaded', () => {
  if (guardAccess()) init();
});