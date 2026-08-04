/* ============================================================
 * Administration — CathLab (vanilla JS, no framework)
 * ============================================================ */

import { get as apiGet, post as apiPost } from '../../assets/js/api/client.js';

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
const state = { currentView: 'dashboard' };

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
function toast(msg, type = 'success') {
  const el = document.createElement('div');
  el.className = `toast ${type}`;
  el.textContent = msg;
  $('#toast-container').appendChild(el);
  setTimeout(() => {
    el.classList.add('fade');
    setTimeout(() => el.remove(), 200);
  }, 3000);
}

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
  }[view]?.());
}

/* ---------- Dashboard ---------- */
async function loadDashboard() {
  const tables = {
    movements: $('#dash-movements-table'),
    reports: $('#dash-reports-table'),
    audits: $('#dash-audits-table'),
    logs: $('#dash-logs-table'),
  };
  setTableLoading(tables.movements, 5);
  setTableLoading(tables.reports, 4);
  setTableLoading(tables.audits, 4);
  setTableLoading(tables.logs, 3);
  $('#module-pharmacy-stat').textContent = 'Chargement…';
  $('#module-patients-stat').textContent = 'Chargement…';

  // Movements
  try {
    const data = await apiGet(API.movements(1, 5));
    const arr = normalizeArray(data);
    $('#kpi-movements').textContent = data.total ?? arr.length;
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
    else tables.reports.querySelector('tbody').innerHTML = preview.map(reportRow).join('');
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
    $('#kpi-audits').textContent = data.total ?? arr.length;
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
    if (!arr.length) setTableEmpty(tables.logs, 3, 'Aucun log.');
    else tables.logs.querySelector('tbody').innerHTML = arr.map(logRow).join('');
  } catch (e) {
    setTableError(tables.logs, 3, e.message);
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
}

function movementRow(m) {
  return `<tr>
    <td>${fmtDateTime(m.movedAt)}</td>
    <td>${escapeHtml(m.productName)}</td>
    <td>${escapeHtml(m.movementType)}</td>
    <td>${m.quantity ?? '—'}</td>
    <td>${escapeHtml(m.createdBy || '—')}</td>
  </tr>`;
}

function reportRow(r) {
  return `<tr>
    <td>${escapeHtml(r.patientName)}</td>
    <td>${escapeHtml(r.doctorName || '—')}</td>
    <td>${fmtDateTime(r.reportGeneratedAt)}</td>
    <td>${r.isConfirmed ? '<span class="pill pill-green">Confirmé</span>' : '<span class="pill pill-amber">Brouillon</span>'}</td>
  </tr>`;
}

function auditRow(a) {
  return `<tr>
    <td>${fmtDateTime(a.occurredAt)}</td>
    <td>${escapeHtml(a.actorName)}</td>
    <td>${escapeHtml(a.action)}</td>
    <td>${escapeHtml(a.entityType)}</td>
  </tr>`;
}

function logRow(l) {
  return `<tr>
    <td>${fmtDateTime(l.occurredAt)}</td>
    <td>${escapeHtml(l.username)}</td>
    <td>${escapeHtml(l.action)}</td>
  </tr>`;
}

/* ---------- Stock movements ---------- */
async function loadMovements() {
  const table = $('#movements-table');
  setTableLoading(table, 6);
  try {
    const data = await apiGet(API.movements(1, 100));
    const arr = normalizeArray(data);
    if (!arr.length) return setTableEmpty(table, 6, 'Aucun mouvement.');
    table.querySelector('tbody').innerHTML = arr.map((m) => `<tr>
      <td>${fmtDateTime(m.movedAt)}</td>
      <td>${escapeHtml(m.productName)}</td>
      <td>${escapeHtml(m.movementType)}</td>
      <td>${m.quantity ?? '—'}</td>
      <td>${escapeHtml(m.referenceNote || '—')}</td>
      <td>${escapeHtml(m.createdBy || '—')}</td>
    </tr>`).join('');
  } catch (e) {
    setTableError(table, 6, e.message);
    toast('Erreur lors du chargement des mouvements', 'error');
  }
}

/* ---------- Users ---------- */
async function loadUsers() {
  const table = $('#users-table');
  setTableLoading(table, 5);
  try {
    const data = await apiGet(API.users());
    const arr = normalizeArray(data);
    if (!arr.length) return setTableEmpty(table, 5, 'Aucun utilisateur.');
    table.querySelector('tbody').innerHTML = arr.map((u) => `<tr>
      <td>${escapeHtml(u.fullName)}</td>
      <td>${escapeHtml(u.username)}</td>
      <td>${escapeHtml(u.role)}</td>
      <td>${u.isActive ? '<span class="pill pill-green">Actif</span>' : '<span class="pill pill-grey">Inactif</span>'}</td>
      <td>${fmtDateTime(u.createdAt)}</td>
    </tr>`).join('');
  } catch (e) {
    setTableError(table, 5, e.message);
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

/* ---------- Reports ---------- */
async function loadReports() {
  const table = $('#reports-table');
  setTableLoading(table, 4);
  try {
    const data = await apiGet(API.reports());
    const arr = normalizeArray(data);
    if (!arr.length) return setTableEmpty(table, 4, 'Aucun rapport.');
    table.querySelector('tbody').innerHTML = arr.map((r) => `<tr>
      <td>${escapeHtml(r.patientName)}</td>
      <td>${escapeHtml(r.doctorName)}</td>
      <td>${fmtDateTime(r.reportGeneratedAt)}</td>
      <td>${r.isConfirmed ? '<span class="pill pill-green">Confirmé</span>' : '<span class="pill pill-amber">Brouillon</span>'}</td>
    </tr>`).join('');
  } catch (e) {
    setTableError(table, 4, e.message);
    toast('Erreur lors du chargement des rapports', 'error');
  }
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
      <td>${escapeHtml(a.action)}</td>
      <td>${escapeHtml(a.entityType)}</td>
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
  setTableLoading(table, 4);
  try {
    const data = await apiGet(API.logs());
    const arr = normalizeArray(data);
    if (!arr.length) return setTableEmpty(table, 4, 'Aucun log.');
    table.querySelector('tbody').innerHTML = arr.map((l) => `<tr>
      <td>${fmtDateTime(l.occurredAt)}</td>
      <td>${escapeHtml(l.username)}</td>
      <td>${escapeHtml(l.action)}</td>
      <td>${escapeHtml(l.ipAddress || '—')}</td>
    </tr>`).join('');
  } catch (e) {
    setTableError(table, 4, e.message);
    toast('Erreur lors du chargement des logs', 'error');
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