/* ============================================================
 * Pharmacie — CathLab  (vanilla JS, no framework)
 * ============================================================ */

import { get as apiGetClient, post as apiPostClient, put as apiPutClient } from '../../assets/js/api/client.js';

/* ---------- API config ---------- */
const API_BASE = 'http://localhost:5152/api';

const API = {
  base: API_BASE,
  itemsReservedAll: () => `${API_BASE}/pharmacy/items/reserved`,
  itemsReserved: (studyId) =>
    `${API_BASE}/pharmacy/items/reserved/${encodeURIComponent(studyId)}`,
  itemsAvailable: (deviceType) =>
    `${API_BASE}/pharmacy/items/available${deviceType ? `?deviceType=${encodeURIComponent(deviceType)}` : ''}`,
  // All physical items regardless of status, optionally filtered by status and/or device type.
  itemsAll: (status, deviceType) => {
    const params = new URLSearchParams();
    if (status) params.set('status', status);
    if (deviceType) params.set('deviceType', deviceType);
    const qs = params.toString();
    return `${API_BASE}/pharmacy/items${qs ? `?${qs}` : ''}`;
  },
  products: (page = 1, pageSize = 200) =>
    `${API_BASE}/pharmacy/products?page=${page}&pageSize=${pageSize}`,
  createProduct: `${API_BASE}/pharmacy/products`,
  lowStock: `${API_BASE}/pharmacy/products/low-stock`,
  receiveItem: `${API_BASE}/pharmacy/items/receive`,
  expiring: (withinDays) =>
    `${API_BASE}/pharmacy/items/expiring?withinDays=${withinDays}`,
  waste: (id) => `${API_BASE}/pharmacy/items/${id}/waste`,
  release: (id) => `${API_BASE}/pharmacy/items/${id}/release`,
};

/* ---------- State ---------- */
const state = {
  products: [],
  currentView: 'dashboard',
  wasteItemId: null,
  selectedItemForEdit: null,
  selectedItemForDelete: null,
  selectedProductForEdit: null,
  selectedProductForDelete: null,
};

/* ---------- DOM helpers ---------- */
const $ = (sel, root = document) => root.querySelector(sel);
const $$ = (sel, root = document) => Array.from(root.querySelectorAll(sel));

/* ---------- Utils ---------- */
function fmtDate(d) {
  if (!d) return '—';
  const dt = new Date(d);
  if (isNaN(dt)) return '—';
  return dt.toLocaleDateString('fr-FR', { day: '2-digit', month: '2-digit', year: 'numeric' });
}
function escapeHtml(s) {
  if (s == null) return '';
  return String(s).replace(/[&<>"']/g, (c) => ({
    '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;',
  }[c]));
}
function daysUntil(d) {
  if (!d) return null;
  const dt = new Date(d);
  if (isNaN(dt)) return null;
  return Math.ceil((dt - Date.now()) / 86400000);
}

/* ---------- Status helpers ---------- */
function statusPill(status) {
  const map = {
    InStock: { cls: 'pill-green', label: 'En stock' },
    Reserved: { cls: 'pill-blue', label: 'Réservé' },
    Consumed: { cls: 'pill-purple', label: 'Consommé' },
    Wasted: { cls: 'pill-red', label: 'Rebut' },
    Expired: { cls: 'pill-grey', label: 'Expiré' },
  };
  const s = map[status] || { cls: 'pill-grey', label: status || '—' };
  return `<span class="pill ${s.cls}">${escapeHtml(s.label)}</span>`;
}
function deviceTypePill(t) {
  if (t === 'Stent') return `<span class="pill pill-purple">Stent</span>`;
  if (t === 'Ballon') return `<span class="pill pill-amber">Ballon</span>`;
  return escapeHtml(t || '—');
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

/* ---------- Fetch wrapper ---------- */
function toApiEndpoint(url) {
  const origin = 'http://localhost:5152';
  if (url.startsWith(origin)) return url.slice(origin.length);
  return url;
}

async function apiGet(url) {
  return apiGetClient(toApiEndpoint(url));
}
async function apiPost(url, body) {
  return apiPostClient(toApiEndpoint(url), body);
}
async function apiPut(url, body) {
  return apiPutClient(toApiEndpoint(url), body);
}

/* ---------- Table helpers ---------- */
function setTableLoading(table, colspan, msg = 'Chargement…') {
  const tb = table.querySelector('tbody');
  tb.innerHTML = `<tr class="row-loading"><td colspan="${colspan}">${msg}</td></tr>`;
}
function setTableError(table, colspan, msg) {
  const tb = table.querySelector('tbody');
  tb.innerHTML = `<tr class="row-error"><td colspan="${colspan}">Erreur : ${escapeHtml(msg)}</td></tr>`;
}
function setTableEmpty(table, colspan, msg = 'Aucune donnée.') {
  const tb = table.querySelector('tbody');
  tb.innerHTML = `<tr class="empty-row"><td colspan="${colspan}">${msg}</td></tr>`;
}

/* ---------- Navigation ---------- */
const VIEW_META = {
  dashboard:    { title: 'Tableau de bord',          sub: "Vue d'ensemble du stock et des alertes" },
  products:     { title: 'Produits',                 sub: 'Catalogue des produits référencés' },
  items:        { title: 'Tous les articles',        sub: 'Toutes les unités physiques, quel que soit leur statut' },
  lowstock:     { title: 'Stock faible',              sub: 'Produits sous le seuil minimum' },
  expiring:     { title: 'Péremptions proches',       sub: 'Articles expirant sous peu' },
  reservations: { title: 'Réservations par étude',    sub: 'Articles réservés pour une étude' },
};

function switchView(view) {
  state.currentView = view;
  $$('.nav-item').forEach((n) => n.classList.toggle('active', n.dataset.view === view));
  $$('.view').forEach((v) => v.classList.add('hidden'));
  $(`#view-${view}`).classList.remove('hidden');
  const meta = VIEW_META[view] || { title: view, sub: '' };
  $('#page-title').textContent = meta.title;
  $('#page-subtitle').textContent = meta.sub;
  // load
  ({
    dashboard: loadDashboard,
    products: loadProducts,
    items: loadItems,
    lowstock: loadLowStock,
    expiring: loadExpiring,
    reservations: loadReservations,
  }[view]?.());
}

/* ---------- Dashboard ---------- */
async function loadDashboard() {
  const kpiRow = $('#kpi-row');
  kpiRow.innerHTML = `
    <div class="kpi"><div class="kpi-icon purple">${iconBox()}</div><div class="kpi-meta"><span class="kpi-value">…</span><span class="kpi-label">Produits référencés</span></div></div>
    <div class="kpi"><div class="kpi-icon green">${iconCheck()}</div><div class="kpi-meta"><span class="kpi-value">…</span><span class="kpi-label">Articles en stock</span></div></div>
    <div class="kpi"><div class="kpi-icon amber">${iconWarn()}</div><div class="kpi-meta"><span class="kpi-value">…</span><span class="kpi-label">En stock faible</span></div></div>
    <div class="kpi"><div class="kpi-icon red">${iconClock()}</div><div class="kpi-meta"><span class="kpi-value">…</span><span class="kpi-label">Péremption ≤30j</span></div></div>
  `;

  const lsTable = $('#dash-lowstock-table');
  const exTable = $('#dash-expiring-table');
  setTableLoading(lsTable, 5);
  setTableLoading(exTable, 4);

  // KPIs
  let productsCount = 0, itemsCount = 0, lowCount = 0, expCount = 0;

  // products
  try {
    const data = await apiGet(API.products());
    const arr = Array.isArray(data)
      ? data
      : (data.items || data.Items || []);
    productsCount = data.totalCount ?? data.total ?? arr.length;
    state.products = arr;
  } catch (e) { /* keep 0 */ }

  // items available (InStock only, for the KPI headline)
  try {
    const items = await apiGet(API.itemsAvailable());
    itemsCount = items.length;
  } catch {}

  // low stock
  try {
    const ls = await apiGet(API.lowStock);
    lowCount = Array.isArray(ls) ? ls.length : (ls.items?.length || 0);
    renderLowStockPreview(ls);
    updateBadge('badge-lowstock', lowCount);
  } catch (e) {
    setTableError(lsTable, 5, e.message);
  }

  // expiring
  try {
    const ex = await apiGet(API.expiring(30));
    expCount = Array.isArray(ex) ? ex.length : (ex.items?.length || 0);
    renderExpiringPreview(ex);
    updateBadge('badge-expiring', expCount);
  } catch (e) {
    setTableError(exTable, 4, e.message);
  }

  // fill KPIs
  const vals = kpiRow.querySelectorAll('.kpi-value');
  vals[0].textContent = productsCount;
  vals[1].textContent = itemsCount;
  vals[2].textContent = lowCount;
  vals[3].textContent = expCount;
}

function renderLowStockPreview(data) {
  const arr = normalizeArray(data);
  const table = $('#dash-lowstock-table');
  if (!arr.length) return setTableEmpty(table, 5, 'Aucun produit en stock faible.');
  const rows = arr.slice(0, 5).map((p) => {
    const cur = p.currentStock ?? 0;
    const min = p.minimumStockLevel ?? 0;
    const manque = Math.max(0, min - cur);
    return `<tr>
      <td>${escapeHtml(p.productName || p.name)}</td>
      <td>${deviceTypePill(p.deviceType)}</td>
      <td>${cur}</td>
      <td>${min}</td>
      <td><span class="pill pill-red">${manque}</span></td>
    </tr>`;
  }).join('');
  table.querySelector('tbody').innerHTML = rows;
}
function renderExpiringPreview(data) {
  const arr = normalizeArray(data);
  const table = $('#dash-expiring-table');
  if (!arr.length) return setTableEmpty(table, 4, 'Aucune péremption proche.');
  const rows = arr.slice(0, 5).map((it) => {
    const d = daysUntil(it.expiryDate);
    return `<tr>
      <td>${escapeHtml(it.productName)}</td>
      <td>${escapeHtml(it.lotNumber || '—')}</td>
      <td>${fmtDate(it.expiryDate)}</td>
      <td><span class="pill pill-red">${d != null ? d + ' j' : '—'}</span></td>
    </tr>`;
  }).join('');
  table.querySelector('tbody').innerHTML = rows;
}

/* ---------- Products ---------- */
async function loadProducts() {
  const table = $('#products-table');
  setTableLoading(table, 8);
  try {
    const data = await apiGet(API.products());
    const arr = normalizeArray(data);
    let filtered = arr;
    const typeFilter = $('#products-type-filter')?.value || '';
    if (typeFilter) {
      filtered = filtered.filter((p) => (p.deviceType || '').toString() === typeFilter);
    }
    state.products = arr;
    if (!filtered.length) return setTableEmpty(table, 8, 'Aucun produit référencé.');
    const rows = filtered.map((p) => `<tr>
      <td>${escapeHtml(p.productName || p.name)}</td>
      <td>${escapeHtml(p.reference || '—')}</td>
      <td>${deviceTypePill(p.deviceType)}</td>
      <td>${escapeHtml(p.manufacturer || '—')}</td>
      <td>${p.currentStock ?? 0}</td>
      <td>${p.minimumStockLevel ?? 0}</td>
      <td>${escapeHtml(p.unitOfMeasure || '—')}</td>
      <td>
        <div style="display:flex;gap:8px;align-items:center;justify-content:flex-end;">
          <button class="icon-btn" data-action="edit-product" data-product-id="${p.id}" title="Modifier" aria-label="Modifier le produit">✏️</button>
          <button class="icon-btn" data-action="delete-product" data-product-id="${p.id}" title="Supprimer" aria-label="Supprimer le produit">🗑️</button>
        </div>
      </td>
    </tr>`).join('');
    table.querySelector('tbody').innerHTML = rows;

    table.querySelectorAll('[data-action="edit-product"]').forEach((btn) => {
      btn.addEventListener('click', () => openEditProductModal(btn.dataset.productId));
    });
    table.querySelectorAll('[data-action="delete-product"]').forEach((btn) => {
      btn.addEventListener('click', () => openDeleteProductModal(btn.dataset.productId));
    });
  } catch (e) {
    setTableError(table, 8, e.message);
    toast('Erreur lors du chargement des produits', 'error');
  }
}

/* ---------- Items (all statuses) ---------- */
let currentDeviceFilter = '';
let currentStatusFilter = '';
async function loadItems() {
  const table = $('#items-table');
  setTableLoading(table, 8);
  try {
    const items = await apiGet(API.itemsAll(currentStatusFilter, currentDeviceFilter));
    const arr = normalizeArray(items);
    if (!arr.length) return setTableEmpty(table, 8, 'Aucun article ne correspond à ce filtre.');
    const rows = arr.map((it) => `<tr>
      <td>${escapeHtml(it.productName)}</td>
      <td>${deviceTypePill(it.deviceType)}</td>
      <td>${escapeHtml(it.lotNumber || '—')}</td>
      <td>${escapeHtml(it.barcode || '—')}</td>
      <td>${fmtDate(it.expiryDate)}</td>
      <td>${statusPill(it.status)}</td>
      <td>${escapeHtml(it.reservedForCathStudyId || '—')}</td>
      <td>
        <div style="display:flex;gap:8px;align-items:center;justify-content:flex-end;">
          <button class="icon-btn" data-action="edit-item" data-item-id="${it.id}" title="Modifier" aria-label="Modifier l'article">✏️</button>
          <button class="icon-btn" data-action="delete-item" data-item-id="${it.id}" title="Supprimer" aria-label="Supprimer l'article">🗑️</button>
          ${it.status === 'Reserved' ? `<button class="btn-link" data-release="${it.id}">Libérer</button>` : ''}
          ${it.status === 'InStock' || it.status === 'Reserved' ? `<button class="icon-btn" title="Mettre au rebut" data-waste="${it.id}">${iconTrash()}</button>` : ''}
        </div>
      </td>
    </tr>`).join('');
    table.querySelector('tbody').innerHTML = rows;

    table.querySelectorAll('[data-action="edit-item"]').forEach((btn) => {
      btn.addEventListener('click', () => openEditItemModal(btn.dataset.itemId));
    });
    table.querySelectorAll('[data-action="delete-item"]').forEach((btn) => {
      btn.addEventListener('click', () => openDeleteItemModal(btn.dataset.itemId));
    });
  } catch (e) {
    setTableError(table, 8, e.message);
    toast('Erreur lors du chargement des articles', 'error');
  }
}

/* ---------- Low stock ---------- */
async function loadLowStock() {
  const table = $('#lowstock-table');
  setTableLoading(table, 6);
  try {
    const data = await apiGet(API.lowStock);
    const arr = normalizeArray(data);
    if (!arr.length) return setTableEmpty(table, 6, 'Aucun produit en stock faible.');
    const rows = arr.map((p) => {
      const cur = p.currentStock ?? 0;
      const min = p.minimumStockLevel ?? 0;
      const manque = Math.max(0, min - cur);
      return `<tr>
        <td>${escapeHtml(p.productName || p.name)}</td>
        <td>${escapeHtml(p.reference || '—')}</td>
        <td>${deviceTypePill(p.deviceType)}</td>
        <td>${cur}</td>
        <td>${min}</td>
        <td><span class="pill pill-red">${manque}</span></td>
      </tr>`;
    }).join('');
    table.querySelector('tbody').innerHTML = rows;
  } catch (e) {
    setTableError(table, 6, e.message);
    toast('Erreur lors du chargement du stock faible', 'error');
  }
}

/* ---------- Expiring ---------- */
let expiringWindow = 30;
async function loadExpiring() {
  const table = $('#expiring-table');
  setTableLoading(table, 5);
  try {
    const data = await apiGet(API.expiring(expiringWindow));
    const arr = normalizeArray(data);
    if (!arr.length) return setTableEmpty(table, 5, 'Aucune péremption dans cette fenêtre.');
    const rows = arr.map((it) => {
      const d = daysUntil(it.expiryDate);
      return `<tr>
        <td>${escapeHtml(it.productName)}</td>
        <td>${deviceTypePill(it.deviceType)}</td>
        <td>${escapeHtml(it.lotNumber || '—')}</td>
        <td>${fmtDate(it.expiryDate)}</td>
        <td><span class="pill pill-red">${d != null ? d + ' j' : '—'}</span></td>
      </tr>`;
    }).join('');
    table.querySelector('tbody').innerHTML = rows;
  } catch (e) {
    setTableError(table, 5, e.message);
    toast('Erreur lors du chargement des péremptions', 'error');
  }
}

/* ---------- Reservations ---------- */
async function loadReservations() {
  const table = $('#reservations-table');
  setTableLoading(table, 7);
  try {
    const data = await apiGet(API.itemsReservedAll());
    renderReservations(data);
  } catch (e) {
    setTableError(table, 7, e.message);
  }
}
function renderReservations(data) {
  const arr = normalizeArray(data);
  const table = $('#reservations-table');
  if (!arr.length) return setTableEmpty(table, 7, 'Aucun article réservé actuellement.');
  const rows = arr.map((it) => `<tr>
    <td>${escapeHtml(it.productName)}</td>
    <td>${deviceTypePill(it.deviceType)}</td>
    <td>${escapeHtml(it.lotNumber || '—')}</td>
    <td>${escapeHtml(it.barcode || '—')}</td>
    <td>${fmtDate(it.expiryDate)}</td>
    <td>${statusPill(it.status)}</td>
    <td>${fmtDate(it.reservedAt)}</td>
  </tr>`).join('');
  table.querySelector('tbody').innerHTML = rows;
}
async function searchReservations() {
  const input = $('#study-id-input');
  const table = $('#reservations-table');
  const id = input.value.trim();
  if (!id) { loadReservations(); return; } // empty search = show all again
  setTableLoading(table, 7);
  try {
    const data = await apiGet(API.itemsReserved(id));
    renderReservations(data);
  } catch (e) {
    setTableError(table, 7, e.message);
    toast('Erreur lors de la recherche des réservations', 'error');
  }
}

/* ---------- Modals ---------- */
function openModal(id) { $(`#${id}`).classList.remove('hidden'); }
function closeModal(id) { $(`#${id}`).classList.add('hidden'); }

function openProductModal() {
  $('#form-product').reset();
  $('#p-min').value = 5;
  openModal('modal-product');
}
async function saveProduct() {
  const body = {
    productName: $('#p-name').value.trim(),
    reference: $('#p-ref').value.trim(),
    deviceType: $('#p-type').value,
    manufacturer: $('#p-manu').value.trim() || null,
    unitOfMeasure: $('#p-unit').value.trim() || null,
    minimumStockLevel: parseInt($('#p-min').value, 10) || 0,
  };
  try {
    await apiPost(API.createProduct, body);
    closeModal('modal-product');
    toast('Produit créé avec succès');
    loadProducts();
  } catch (e) {
    toast(`Erreur : ${e.message}`, 'error');
  }
}

async function openReceiveModal() {
  $('#form-receive').reset();
  const sel = $('#r-product');
  sel.innerHTML = '<option value="">— Sélectionner —</option>' +
    state.products.map((p) => `<option value="${escapeHtml(p.id)}">${escapeHtml(p.productName || p.name)} (${escapeHtml(p.deviceType || '')})</option>`).join('');
  if (!state.products.length) {
    try {
      const data = await apiGet(API.products());
      state.products = normalizeArray(data);
      sel.innerHTML = '<option value="">— Sélectionner —</option>' +
        state.products.map((p) => `<option value="${escapeHtml(p.id)}">${escapeHtml(p.productName || p.name)} (${escapeHtml(p.deviceType || '')})</option>`).join('');
    } catch {}
  }
  openModal('modal-receive');
}
async function saveReceive() {
  const body = {
    productId: $('#r-product').value,
    barcode: $('#r-barcode').value.trim(),
    lotNumber: $('#r-lot').value.trim(),
    expiryDate: $('#r-expiry').value,
    serialNumber: $('#r-serial').value.trim() || null,
  };
  if (!body.productId) { toast('Sélectionnez un produit', 'error'); return; }
  try {
    await apiPost(API.receiveItem, body);
    closeModal('modal-receive');
    toast('Article réceptionné avec succès');
    loadItems();
  } catch (e) {
    toast(`Erreur : ${e.message}`, 'error');
  }
}

async function openEditProductModal(productId) {
  try {
    const data = await apiGet(API.products());
    const arr = normalizeArray(data);
    const product = arr.find((p) => p.id === productId);
    if (!product) {
      toast('Produit introuvable.', 'error');
      return;
    }

    state.selectedProductForEdit = product;
    $('#ep-name').value = product.productName || '';
    $('#ep-ref').value = product.reference || '';
    $('#ep-type').value = product.deviceType || 'Stent';
    $('#ep-manu').value = product.manufacturer || '';
    $('#ep-unit').value = product.unitOfMeasure || '';
    $('#ep-min').value = product.minimumStockLevel ?? 0;
    openModal('modal-edit-product');
  } catch (e) {
    toast(`Erreur : ${e.message}`, 'error');
  }
}

async function saveEditedProduct() {
  if (!state.selectedProductForEdit) return;

  const payload = {
    productName: $('#ep-name').value.trim(),
    reference: $('#ep-ref').value.trim(),
    deviceType: $('#ep-type').value,
    manufacturer: $('#ep-manu').value.trim() || null,
    unitOfMeasure: $('#ep-unit').value.trim() || null,
    minimumStockLevel: Number($('#ep-min').value || 0),
  };

  if (!payload.productName || !payload.reference) {
    toast('Nom et référence sont requis.', 'error');
    return;
  }

  try {
    await apiPut(`/api/pharmacy/products/${state.selectedProductForEdit.id}`, payload);
    closeModal('modal-edit-product');
    state.selectedProductForEdit = null;
    toast('Produit modifié avec succès');
    loadProducts();
  } catch (e) {
    toast(`Erreur : ${e.message}`, 'error');
  }
}

async function openDeleteProductModal(productId) {
  try {
    const data = await apiGet(API.products());
    const arr = normalizeArray(data);
    const product = arr.find((p) => p.id === productId);
    if (!product) {
      toast('Produit introuvable.', 'error');
      return;
    }

    state.selectedProductForDelete = product;
    $('#delete-product-message').textContent = `Voulez-vous vraiment supprimer ${product.productName || 'ce produit'} (${product.reference || 'sans référence'}) ?`;
    openModal('modal-delete-product');
  } catch (e) {
    toast(`Erreur : ${e.message}`, 'error');
  }
}

async function confirmDeleteProduct() {
  if (!state.selectedProductForDelete) return;

  try {
    const response = await fetch(`http://localhost:5152/api/pharmacy/products/${state.selectedProductForDelete.id}`, {
      method: 'DELETE',
      headers: {
        'Authorization': `Bearer ${sessionStorage.getItem('authToken') || ''}`,
        'Content-Type': 'application/json'
      }
    });

    if (!response.ok) {
      let message = 'Erreur lors de la suppression.';
      try {
        const data = await response.json();
        message = data.message || message;
      } catch {}
      throw new Error(message);
    }

    closeModal('modal-delete-product');
    state.selectedProductForDelete = null;
    toast('Produit supprimé avec succès');
    loadProducts();
  } catch (e) {
    toast(`Erreur : ${e.message}`, 'error');
  }
}

function openWasteModal(itemId) {
  state.wasteItemId = itemId;
  $('#waste-item-label').textContent = `Article #${itemId}`;
  $('#w-note').value = '';
  openModal('modal-waste');
}
async function saveWaste() {
  const note = $('#w-note').value.trim();
  try {
    await apiPost(API.waste(state.wasteItemId), { note });
    closeModal('modal-waste');
    toast('Article mis au rebut');
    loadItems();
  } catch (e) {
    toast(`Erreur : ${e.message}`, 'error');
  }
}

async function openEditItemModal(itemId) {
  try {
    const items = await apiGet(API.itemsAll('', currentDeviceFilter));
    const arr = normalizeArray(items);
    const item = arr.find((i) => i.id === itemId);
    if (!item) {
      toast('Article introuvable.', 'error');
      return;
    }

    state.selectedItemForEdit = item;
    $('#ei-barcode').value = item.barcode || '';
    $('#ei-lot').value = item.lotNumber || '';
    $('#ei-expiry').value = item.expiryDate || '';
    $('#ei-serial').value = item.serialNumber || '';
    $('#ei-status').value = item.status || 'InStock';
    openModal('modal-edit-item');
  } catch (e) {
    toast(`Erreur : ${e.message}`, 'error');
  }
}

async function saveEditedItem() {
  if (!state.selectedItemForEdit) return;

  const payload = {
    barcode: $('#ei-barcode').value.trim(),
    lotNumber: $('#ei-lot').value.trim() || null,
    expiryDate: $('#ei-expiry').value || null,
    serialNumber: $('#ei-serial').value.trim() || null,
    status: $('#ei-status').value,
  };

  if (!payload.barcode || !payload.expiryDate) {
    toast('Code-barres et date de péremption sont requis.', 'error');
    return;
  }

  try {
    await apiPut(`/api/pharmacy/items/${state.selectedItemForEdit.id}`, payload);
    closeModal('modal-edit-item');
    state.selectedItemForEdit = null;
    toast('Article modifié avec succès');
    loadItems();
  } catch (e) {
    toast(`Erreur : ${e.message}`, 'error');
  }
}

async function openDeleteItemModal(itemId) {
  try {
    const items = await apiGet(API.itemsAll('', currentDeviceFilter));
    const arr = normalizeArray(items);
    const item = arr.find((i) => i.id === itemId);
    if (!item) {
      toast('Article introuvable.', 'error');
      return;
    }

    state.selectedItemForDelete = item;
    $('#delete-item-message').textContent = `Voulez-vous vraiment supprimer ${item.productName || 'cet article'} (${item.barcode || 'sans code-barres'}) ?`;
    openModal('modal-delete-item');
  } catch (e) {
    toast(`Erreur : ${e.message}`, 'error');
  }
}

async function confirmDeleteItem() {
  if (!state.selectedItemForDelete) return;

  try {
    const response = await fetch(`http://localhost:5152/api/pharmacy/items/${state.selectedItemForDelete.id}`, {
      method: 'DELETE',
      headers: {
        'Authorization': `Bearer ${sessionStorage.getItem('authToken') || ''}`,
        'Content-Type': 'application/json'
      }
    });

    if (!response.ok) {
      let message = 'Erreur lors de la suppression.';
      try {
        const data = await response.json();
        message = data.message || message;
      } catch {}
      throw new Error(message);
    }

    closeModal('modal-delete-item');
    state.selectedItemForDelete = null;
    toast('Article supprimé avec succès');
    loadItems();
  } catch (e) {
    toast(`Erreur : ${e.message}`, 'error');
  }
}

async function releaseItem(itemId) {
  try {
    await apiPost(API.release(itemId), {});
    toast('Article libéré, retour au stock');
    loadItems();
  } catch (e) {
    toast(`Erreur : ${e.message}`, 'error');
  }
}

/* ---------- Badges ---------- */
function updateBadge(id, count) {
  const el = $(`#${id}`);
  if (!el) return;
  el.textContent = count;
  el.classList.toggle('zero', count === 0);
}

/* ---------- Icons (inline SVG strings) ---------- */
function iconBox()   { return `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M21 8a2 2 0 0 0-1-1.7l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.7l7 4a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16Z"/><path d="m3.3 7 8.7 5 8.7-5"/><path d="M12 22V12"/></svg>`; }
function iconCheck() { return `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"/><path d="m9 11 3 3L22 4"/></svg>`; }
function iconWarn()  { return `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M10.3 3.9 1.8 18a2 2 0 0 0 1.7 3h17a2 2 0 0 0 1.7-3L13.7 3.9a2 2 0 0 0-3.4 0Z"/><path d="M12 9v4"/><path d="M12 17h.01"/></svg>`; }
function iconClock() { return `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="9"/><path d="M12 7v5l3 2"/></svg>`; }
function iconTrash() { return `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M3 6h18"/><path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6"/><path d="M8 6V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"/><path d="M10 11v6"/><path d="M14 11v6"/></svg>`; }

/* ---------- Helpers ---------- */
function normalizeArray(data) {
  if (Array.isArray(data)) return data;
  if (data && Array.isArray(data.items)) return data.items;
  if (data && Array.isArray(data.Items)) return data.Items;
  return [];
}

/* ---------- Global search (client-side filter on current view's first table) ---------- */
function applyGlobalSearch(q) {
  const view = $(`#view-${state.currentView}`);
  if (!view) return;
  const table = view.querySelector('.data-table');
  if (!table) return;
  const term = q.toLowerCase().trim();
  $$('tbody tr', table).forEach((tr) => {
    if (tr.classList.contains('row-loading') || tr.classList.contains('row-error') || tr.classList.contains('empty-row')) return;
    tr.style.display = !term || tr.textContent.toLowerCase().includes(term) ? '' : 'none';
  });
}

/* ---------- Wire up events ---------- */
function init() {
  // nav
  $$('.nav-item').forEach((n) => n.addEventListener('click', (e) => {
    e.preventDefault();
    switchView(n.dataset.view);
  }));
  // panel links
  $$('.panel-link').forEach((l) => l.addEventListener('click', (e) => {
    e.preventDefault();
    switchView(l.dataset.goto);
  }));

  // products
  $('#btn-new-product').addEventListener('click', openProductModal);
  $('#btn-save-product').addEventListener('click', saveProduct);
  $('#products-type-filter').addEventListener('change', () => loadProducts());
  $('#btn-save-edit-product').addEventListener('click', saveEditedProduct);
  $('#btn-confirm-delete-product').addEventListener('click', confirmDeleteProduct);

  // items
  $('#btn-receive').addEventListener('click', openReceiveModal);
  $('#btn-save-receive').addEventListener('click', saveReceive);
  $('#items-filter').addEventListener('change', (e) => {
    currentDeviceFilter = e.target.value;
    loadItems();
  });
  $('#items-status-filter').addEventListener('change', (e) => {
    currentStatusFilter = e.target.value;
    loadItems();
  });
  // waste + release via delegation
  $('#items-table').addEventListener('click', (e) => {
    const wasteBtn = e.target.closest('[data-waste]');
    if (wasteBtn) { openWasteModal(wasteBtn.dataset.waste); return; }

    const releaseBtn = e.target.closest('[data-release]');
    if (releaseBtn) releaseItem(releaseBtn.dataset.release);
  });
  $('#btn-save-waste').addEventListener('click', saveWaste);
  $('#btn-save-edit-item').addEventListener('click', saveEditedItem);
  $('#btn-confirm-delete-item').addEventListener('click', confirmDeleteItem);

  // expiring window
  $('#expiring-window').addEventListener('change', (e) => {
    expiringWindow = parseInt(e.target.value, 10);
    loadExpiring();
  });

  // reservations
  $('#btn-search-study').addEventListener('click', searchReservations);
  $('#study-id-input').addEventListener('keydown', (e) => {
    if (e.key === 'Enter') { e.preventDefault(); searchReservations(); }
  });

  // modal close
  $$('[data-close-modal]').forEach((b) => b.addEventListener('click', () => closeModal(b.dataset.closeModal)));
  $$('.modal-overlay').forEach((o) => o.addEventListener('click', (e) => {
    if (e.target === o) o.classList.add('hidden');
  }));

  // global search
  $('#global-search').addEventListener('input', (e) => applyGlobalSearch(e.target.value));

  // initial load
  switchView('dashboard');
}
/* ---------- Access control ---------- */
const ALLOWED_ROLES = ['Admin', 'ResponsablePharmacie'];

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
window.logoutUser = async function() {
  const confirmed = window.confirm('Êtes-vous sûr de vouloir vous déconnecter ?');
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

document.addEventListener('DOMContentLoaded', () => {
  if (guardAccess()) init();
});