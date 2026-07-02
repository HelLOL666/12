/* ===== Configuration ===== */
const API_URL = '/api';

/* ===== State ===== */
let accessToken = localStorage.getItem('pdm-access-token');
let refreshToken = localStorage.getItem('pdm-refresh-token');
let currentUser = null;
let documents = [];
let users = [];
let currentDoc = null;
let currentDocComments = [];
let zoom = 100;
let theme = localStorage.getItem('pdm-theme') || 'light';
let msgTimer;
let docPage = 1;
let docTotalPages = 1;
let docSearch = '';

const $ = (id) => document.getElementById(id);

/* ===== API Helper ===== */
async function api(path, options = {}) {
  const headers = options.headers || {};
  if (accessToken) headers['Authorization'] = `Bearer ${accessToken}`;
  if (!(options.body instanceof FormData)) {
    headers['Content-Type'] = 'application/json';
  }

  let res = await fetch(`${API_URL}${path}`, { ...options, headers });

  if (res.status === 401 && refreshToken) {
    const refreshed = await tryRefresh();
    if (refreshed) {
      headers['Authorization'] = `Bearer ${accessToken}`;
      res = await fetch(`${API_URL}${path}`, { ...options, headers });
    } else {
      doLogout();
      return null;
    }
  }

  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error(err.error || `HTTP ${res.status}`);
  }
  return res.json();
}

async function tryRefresh() {
  try {
    const res = await fetch(`${API_URL}/auth/refresh`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ accessToken, refreshToken }),
    });
    if (!res.ok) return false;
    const data = await res.json();
    if (data.success && data.data) {
      setTokens(data.data.accessToken, data.data.refreshToken);
      return true;
    }
    return false;
  } catch {
    return false;
  }
}

function setTokens(access, refresh) {
  accessToken = access;
  refreshToken = refresh;
  localStorage.setItem('pdm-access-token', access);
  localStorage.setItem('pdm-refresh-token', refresh);
}

function clearTokens() {
  accessToken = null;
  refreshToken = null;
  localStorage.removeItem('pdm-access-token');
  localStorage.removeItem('pdm-refresh-token');
}

/* ===== Init ===== */
function init() {
  setTheme(theme);
  bindEvents();
  if (accessToken) {
    fetchMe().then((ok) => { if (ok) showApp(); });
  }
}

async function fetchMe() {
  try {
    const data = await api('/auth/me');
    if (data && data.success && data.data) {
      currentUser = data.data;
      return true;
    }
    return false;
  } catch {
    clearTokens();
    return false;
  }
}

/* ===== Auth ===== */
async function doLogin(username, password) {
  try {
    const res = await fetch(`${API_URL}/auth/login`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ username, password }),
    });
    const data = await res.json();
    if (data.success && data.data) {
      setTokens(data.data.accessToken, data.data.refreshToken);
      const ok = await fetchMe();
      if (ok) showApp();
      return null;
    }
    return data.error || 'Ошибка авторизации';
  } catch (e) {
    return 'Ошибка подключения к серверу';
  }
}

async function doLogout() {
  try { await api('/auth/logout', { method: 'POST' }); } catch {}
  clearTokens();
  currentUser = null;
  $('app').classList.add('hidden');
  $('login-screen').classList.remove('hidden');
}

/* ===== Permissions ===== */
function hasPermission(perm) {
  if (!currentUser) return false;
  return (currentUser.permissions & perm) === perm;
}

const PERM = { View: 1, Download: 2, Upload: 4, Delete: 8, ManageUsers: 16 };

/* ===== Theme ===== */
function setTheme(value) {
  theme = value;
  document.documentElement.setAttribute('data-theme', value);
  localStorage.setItem('pdm-theme', value);
  document.querySelectorAll('.btn-option').forEach((b) => {
    b.classList.toggle('active', b.dataset.theme === value);
  });
}

/* ===== App Show ===== */
function showApp() {
  $('login-screen').classList.add('hidden');
  $('app').classList.remove('hidden');

  const isAdmin = hasPermission(PERM.ManageUsers);
  document.querySelectorAll('.admin-only').forEach((el) => {
    el.classList.toggle('hidden', !isAdmin);
  });

  // Show/hide upload button based on permission
  const addBtn = $('add-doc-btn');
  if (addBtn) addBtn.classList.toggle('hidden', !hasPermission(PERM.Upload));

  navigate('documents');
}

/* ===== Navigation ===== */
function navigate(page) {
  document.querySelectorAll('.page').forEach((p) => p.classList.remove('active'));
  const target = $(`page-${page}`);
  if (target) target.classList.add('active');
  document.querySelectorAll('.nav-link[data-page]').forEach((l) => {
    l.classList.toggle('active', l.dataset.page === page);
  });
  $('header-nav').classList.remove('open');
  if (page === 'documents') fetchDocuments();
  if (page === 'users') fetchUsers();
  if (page === 'audit') fetchAuditLogs();
}

/* ===== Documents ===== */
async function fetchDocuments() {
  try {
    const params = new URLSearchParams({ page: docPage, pageSize: 20 });
    if (docSearch) params.set('search', docSearch);
    const data = await api(`/documents?${params}`);
    if (data && data.success && data.data) {
      documents = data.data.items;
      docTotalPages = Math.ceil(data.data.totalCount / data.data.pageSize) || 1;
      renderDocuments();
    }
  } catch (e) {
    showMsg('Ошибка загрузки документов');
  }
}

function renderDocuments() {
  const tbody = $('documents-tbody');
  tbody.innerHTML = documents.length
    ? documents.map((d) => `
        <tr>
          <td>${esc(d.number)}</td>
          <td>${esc(d.title)}</td>
          <td>v${d.currentVersion}</td>
          <td>${formatDate(d.updatedAt)}</td>
          <td><button type="button" class="link" data-open="${d.id}">Открыть</button></td>
        </tr>`).join('')
    : '<tr class="empty"><td colspan="5">Ничего не найдено</td></tr>';

  renderPagination();
}

function renderPagination() {
  let pag = $('doc-pagination');
  if (!pag) {
    pag = document.createElement('div');
    pag.id = 'doc-pagination';
    pag.className = 'pagination';
    $('page-documents').appendChild(pag);
  }
  if (docTotalPages <= 1) { pag.innerHTML = ''; return; }
  let html = '';
  for (let i = 1; i <= Math.min(docTotalPages, 10); i++) {
    html += `<button type="button" class="btn-sm${i === docPage ? ' active' : ''}" data-page-num="${i}">${i}</button> `;
  }
  pag.innerHTML = html;
}

/* ===== Document Detail ===== */
async function openDocument(id) {
  try {
    const data = await api(`/documents/${id}`);
    if (!data || !data.success || !data.data) return;
    currentDoc = data.data;
    zoom = 100;
    renderViewer();
    fetchComments(id);
    document.querySelectorAll('.page').forEach((p) => p.classList.remove('active'));
    $('page-viewer').classList.add('active');
  } catch (e) {
    showMsg('Ошибка загрузки документа');
  }
}

function renderViewer() {
  const doc = currentDoc;
  if (!doc) return;

  $('viewer-info').innerHTML = `
    <dl>
      <div><dt>Название</dt><dd>${esc(doc.title)}</dd></div>
      <div><dt>Номер</dt><dd>${esc(doc.number)}</dd></div>
      <div><dt>Версия</dt><dd>v${doc.versions.length > 0 ? doc.versions[0].versionNumber : '?'}</dd></div>
      <div><dt>Автор</dt><dd>${esc(doc.authorName)}</dd></div>
      <div><dt>Создан</dt><dd>${formatDate(doc.createdAt)}</dd></div>
      <div><dt>Обновлён</dt><dd>${formatDate(doc.updatedAt)}</dd></div>
    </dl>`;

  const latest = doc.versions[0];
  if (latest && latest.hasPdfPreview) {
    $('viewer-pdf').innerHTML = `<iframe src="${API_URL}/documents/versions/${latest.id}/preview" style="width:100%;height:70vh;border:none;"></iframe>`;
  } else {
    $('viewer-pdf').innerHTML = `
      <div class="pdf-page" id="pdf-page">
        <h3>${esc(doc.number)}</h3>
        <p class="sub">${esc(doc.title)}</p>
        <div class="line"></div><div class="line w80"></div><div class="line w60"></div>
        <p style="text-align:center;color:var(--muted);margin-top:40px;">Предпросмотр недоступен для данного формата</p>
      </div>`;
  }
  updateZoom();

  // Show/hide buttons based on permissions
  $('viewer-download').classList.toggle('hidden', !hasPermission(PERM.Download));
  const delBtn = $('viewer-delete');
  if (delBtn) delBtn.classList.toggle('hidden', !hasPermission(PERM.Delete));

  renderVersions();
}

function renderVersions() {
  const container = $('viewer-versions');
  if (!container || !currentDoc) return;
  const versions = currentDoc.versions;
  container.innerHTML = versions.length ? `
    <table class="table">
      <thead><tr><th>Версия</th><th>Файл</th><th>Размер</th><th>Загрузил</th><th>Дата</th><th></th></tr></thead>
      <tbody>${versions.map((v) => `
        <tr>
          <td>v${v.versionNumber}</td>
          <td>${esc(v.originalFileName)}</td>
          <td>${(v.fileSize / 1024 / 1024).toFixed(2)} МБ</td>
          <td>${esc(v.uploadedBy)}</td>
          <td>${formatDate(v.uploadedAt)}</td>
          <td>${hasPermission(PERM.Download) ? `<a href="${API_URL}/documents/versions/${v.id}/download" class="link">Скачать</a>` : ''}</td>
        </tr>`).join('')}
      </tbody>
    </table>` : '<p style="color:var(--muted)">Нет версий</p>';
}

/* ===== Comments ===== */
async function fetchComments(docId) {
  try {
    const data = await api(`/documents/${docId}/comments`);
    if (data && data.success && data.data) {
      currentDocComments = data.data;
      renderComments();
    }
  } catch {}
}

function renderComments() {
  const container = $('viewer-comments');
  if (!container) return;
  const list = currentDocComments;
  container.innerHTML = `
    <div class="comment-form">
      <textarea id="comment-text" placeholder="Написать замечание..." rows="3"></textarea>
      <button type="button" class="btn" id="send-comment-btn">Отправить</button>
    </div>
    ${list.length ? list.map((c) => `
      <div class="comment-item">
        <div class="comment-meta"><strong>${esc(c.authorName)}</strong> — ${formatDateTime(c.createdAt)}</div>
        <div class="comment-text">${esc(c.text)}</div>
      </div>`).join('') : '<p style="color:var(--muted)">Замечаний нет</p>'}`;
}

async function sendComment() {
  const text = $('comment-text')?.value?.trim();
  if (!text || !currentDoc) return;
  try {
    const data = await api(`/documents/${currentDoc.id}/comments`, {
      method: 'POST',
      body: JSON.stringify({ text }),
    });
    if (data && data.success) {
      showMsg('Замечание добавлено');
      fetchComments(currentDoc.id);
    }
  } catch (e) {
    showMsg('Ошибка отправки замечания');
  }
}

/* ===== Upload Document ===== */
async function uploadDocument(formData) {
  try {
    const headers = {};
    if (accessToken) headers['Authorization'] = `Bearer ${accessToken}`;
    const res = await fetch(`${API_URL}/documents/upload`, {
      method: 'POST', headers, body: formData,
    });
    const data = await res.json();
    if (data.success) {
      showMsg('Документ загружен');
      closeModal();
      fetchDocuments();
    } else {
      showMsg(data.error || 'Ошибка загрузки');
    }
  } catch (e) {
    showMsg('Ошибка загрузки файла');
  }
}

/* ===== Delete Document ===== */
async function deleteDocument() {
  if (!currentDoc) return;
  if (!confirm('Удалить документ?')) return;
  try {
    await api(`/documents/${currentDoc.id}`, { method: 'DELETE' });
    showMsg('Документ удалён');
    navigate('documents');
  } catch (e) {
    showMsg('Ошибка удаления');
  }
}

/* ===== Users ===== */
async function fetchUsers() {
  try {
    const data = await api('/users');
    if (data && data.success && data.data) {
      users = data.data.items;
      renderUsers();
    }
  } catch (e) {
    showMsg('Ошибка загрузки пользователей');
  }
}

function renderUsers() {
  $('users-tbody').innerHTML = users.length
    ? users.map((u) => `
        <tr>
          <td>${esc(u.fullName)}</td>
          <td>${u.role}</td>
          <td>${u.isActive ? 'Активен' : 'Неактивен'}</td>
          <td>
            <button type="button" class="link" data-user="${u.id}">Изменить</button>
            <button type="button" class="link" data-pass="${u.id}" style="margin-left:8px">Пароль</button>
            <button type="button" class="link" data-del-user="${u.id}" style="margin-left:8px;color:#c00">Удалить</button>
          </td>
        </tr>`).join('')
    : '<tr class="empty"><td colspan="4">Нет пользователей</td></tr>';
}

async function createUser(formData) {
  try {
    const perms = calcPermissions(formData);
    const data = await api('/users', {
      method: 'POST',
      body: JSON.stringify({
        username: formData.get('username'),
        fullName: formData.get('fullName'),
        password: formData.get('password'),
        roleId: parseInt(formData.get('roleId')),
        permissions: perms,
      }),
    });
    if (data && data.success) {
      showMsg('Пользователь создан');
      closeModal();
      fetchUsers();
    } else {
      showMsg(data?.error || 'Ошибка');
    }
  } catch (e) {
    showMsg('Ошибка создания пользователя');
  }
}

async function updateUser(userId, formData) {
  try {
    const perms = calcPermissions(formData);
    const data = await api(`/users/${userId}`, {
      method: 'PUT',
      body: JSON.stringify({
        fullName: formData.get('fullName'),
        roleId: parseInt(formData.get('roleId')),
        permissions: perms,
        isActive: formData.get('isActive') === 'on',
      }),
    });
    if (data && data.success) {
      showMsg('Сохранено');
      closeModal();
      fetchUsers();
    } else {
      showMsg(data?.error || 'Ошибка');
    }
  } catch (e) {
    showMsg('Ошибка сохранения');
  }
}

async function changePassword(userId) {
  const pass = prompt('Новый пароль (мин. 8 символов):');
  if (!pass || pass.length < 8) return;
  try {
    await api(`/users/${userId}/password`, {
      method: 'PUT',
      body: JSON.stringify({ newPassword: pass }),
    });
    showMsg('Пароль изменён');
  } catch (e) {
    showMsg('Ошибка смены пароля');
  }
}

async function deleteUser(userId) {
  if (!confirm('Деактивировать пользователя?')) return;
  try {
    await api(`/users/${userId}`, { method: 'DELETE' });
    showMsg('Пользователь деактивирован');
    fetchUsers();
  } catch (e) {
    showMsg('Ошибка');
  }
}

function calcPermissions(formData) {
  let perms = 0;
  if (formData.get('perm-view')) perms |= PERM.View;
  if (formData.get('perm-download')) perms |= PERM.Download;
  if (formData.get('perm-upload')) perms |= PERM.Upload;
  if (formData.get('perm-delete')) perms |= PERM.Delete;
  if (formData.get('perm-manage')) perms |= PERM.ManageUsers;
  return perms;
}

/* ===== Audit Log ===== */
async function fetchAuditLogs() {
  try {
    const data = await api('/auditlogs?page=1&pageSize=50');
    if (data && data.success && data.data) {
      renderAuditLogs(data.data.items);
    }
  } catch (e) {
    showMsg('Ошибка загрузки журнала');
  }
}

function renderAuditLogs(logs) {
  const tbody = $('audit-tbody');
  if (!tbody) return;
  tbody.innerHTML = logs.length
    ? logs.map((l) => `
        <tr>
          <td>${formatDateTime(l.timestamp)}</td>
          <td>${esc(l.username)}</td>
          <td>${esc(l.action)}</td>
          <td>${esc(l.details)}</td>
          <td>${esc(l.ipAddress)}</td>
        </tr>`).join('')
    : '<tr class="empty"><td colspan="5">Нет записей</td></tr>';
}

/* ===== Zoom ===== */
function updateZoom() {
  const page = $('pdf-page');
  if (page) page.style.transform = `scale(${zoom / 100})`;
  $('zoom-label').textContent = `${zoom}%`;
}

/* ===== UI Helpers ===== */
function showMsg(text) {
  const el = $('msg');
  el.textContent = text;
  el.classList.remove('hidden');
  clearTimeout(msgTimer);
  msgTimer = setTimeout(() => el.classList.add('hidden'), 2500);
}

function openModal(title, body, actions) {
  $('modal-title').textContent = title;
  $('modal-body').innerHTML = body;
  $('modal-actions').innerHTML = actions;
  $('modal').classList.remove('hidden');
}

function closeModal() {
  $('modal').classList.add('hidden');
}

function esc(str) {
  if (!str) return '';
  return str.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
}

function formatDate(dateStr) {
  if (!dateStr) return '';
  return new Date(dateStr).toLocaleDateString('ru-RU');
}

function formatDateTime(dateStr) {
  if (!dateStr) return '';
  return new Date(dateStr).toLocaleString('ru-RU');
}

/* ===== Event Binding ===== */
function bindEvents() {
  // Login
  $('login-form').addEventListener('submit', async (e) => {
    e.preventDefault();
    const form = e.target;
    const username = form.querySelector('input[type="text"]').value.trim();
    const password = form.querySelector('input[type="password"]').value.trim();
    const btn = form.querySelector('button[type="submit"]');
    btn.disabled = true;
    btn.textContent = 'Вход...';
    const err = await doLogin(username, password);
    if (err) {
      showMsg(err);
      btn.disabled = false;
      btn.textContent = 'Войти';
    }
  });

  // Logout
  $('logout-btn').addEventListener('click', doLogout);
  $('home-btn').addEventListener('click', () => navigate('documents'));

  // Navigation
  document.querySelectorAll('[data-page]').forEach((el) => {
    el.addEventListener('click', (e) => {
      e.preventDefault();
      navigate(el.dataset.page);
    });
  });

  // Mobile menu
  $('menu-btn').addEventListener('click', () => {
    $('header-nav').classList.toggle('open');
  });

  // Document search (debounced)
  let searchTimeout;
  $('doc-search').addEventListener('input', (e) => {
    clearTimeout(searchTimeout);
    searchTimeout = setTimeout(() => {
      docSearch = e.target.value.trim();
      docPage = 1;
      fetchDocuments();
    }, 400);
  });

  // Add document button
  $('add-doc-btn').addEventListener('click', () => {
    openModal(
      'Загрузить документ',
      '<label>Номер документа</label><input id="new-number" placeholder="СБ.001.01">' +
      '<label>Название</label><input id="new-name" placeholder="Сборочный чертёж">' +
      '<label>Файл (PDF, CDW, SPW, M3D, DXF)</label><input type="file" id="new-file" accept=".pdf,.cdw,.spw,.m3d,.dxf">',
      '<button type="button" class="btn" data-act="cancel">Отмена</button>' +
      '<button type="button" class="btn" data-act="upload-doc">Загрузить</button>'
    );
  });

  // Open document
  $('documents-tbody').addEventListener('click', (e) => {
    const btn = e.target.closest('[data-open]');
    if (btn) openDocument(btn.dataset.open);
  });

  // Pagination
  document.addEventListener('click', (e) => {
    const btn = e.target.closest('[data-page-num]');
    if (btn) {
      docPage = parseInt(btn.dataset.pageNum);
      fetchDocuments();
    }
  });

  // Viewer back
  $('viewer-back').addEventListener('click', () => navigate('documents'));

  // Viewer download
  $('viewer-download').addEventListener('click', () => {
    if (!currentDoc || !currentDoc.versions.length) return;
    const latest = currentDoc.versions[0];
    window.open(`${API_URL}/documents/versions/${latest.id}/download`, '_blank');
  });

  // Viewer delete
  const delBtn = $('viewer-delete');
  if (delBtn) delBtn.addEventListener('click', deleteDocument);

  // Zoom
  $('zoom-in').addEventListener('click', () => {
    zoom = Math.min(zoom + 25, 200);
    updateZoom();
  });
  $('zoom-out').addEventListener('click', () => {
    zoom = Math.max(zoom - 25, 50);
    updateZoom();
  });

  // Viewer tabs
  document.addEventListener('click', (e) => {
    const tab = e.target.closest('[data-viewer-tab]');
    if (!tab) return;
    document.querySelectorAll('[data-viewer-tab]').forEach((t) => t.classList.remove('active'));
    tab.classList.add('active');
    document.querySelectorAll('.viewer-tab-content').forEach((c) => c.classList.add('hidden'));
    const target = $(`viewer-${tab.dataset.viewerTab}`);
    if (target) target.classList.remove('hidden');
  });

  // Send comment
  document.addEventListener('click', (e) => {
    if (e.target.id === 'send-comment-btn') sendComment();
  });

  // Users table actions
  $('users-tbody').addEventListener('click', (e) => {
    const editBtn = e.target.closest('[data-user]');
    const passBtn = e.target.closest('[data-pass]');
    const delBtn = e.target.closest('[data-del-user]');

    if (editBtn) {
      const user = users.find((u) => u.id === editBtn.dataset.user);
      if (!user) return;
      openModal(
        'Изменить пользователя',
        `<label>ФИО</label><input id="edit-name" value="${esc(user.fullName)}">` +
        `<label>Роль</label><select id="edit-role">
          <option value="1"${user.role === 'Administrator' ? ' selected' : ''}>Администратор</option>
          <option value="2"${user.role !== 'Administrator' ? ' selected' : ''}>Пользователь</option>
        </select>` +
        `<label style="margin-top:12px;font-size:0.8rem;color:var(--muted)">Права:</label>
        <div style="display:flex;flex-wrap:wrap;gap:8px;margin-top:4px;">
          <label><input type="checkbox" id="ep-view" ${(user.permissions & PERM.View) ? 'checked' : ''}> Просмотр</label>
          <label><input type="checkbox" id="ep-download" ${(user.permissions & PERM.Download) ? 'checked' : ''}> Скачивание</label>
          <label><input type="checkbox" id="ep-upload" ${(user.permissions & PERM.Upload) ? 'checked' : ''}> Загрузка</label>
          <label><input type="checkbox" id="ep-delete" ${(user.permissions & PERM.Delete) ? 'checked' : ''}> Удаление</label>
          <label><input type="checkbox" id="ep-manage" ${(user.permissions & PERM.ManageUsers) ? 'checked' : ''}> Управление</label>
        </div>` +
        `<label><input type="checkbox" id="ep-active" ${user.isActive ? 'checked' : ''}> Активен</label>`,
        `<button type="button" class="btn" data-act="cancel">Отмена</button>
         <button type="button" class="btn" data-act="save-user" data-user="${user.id}">Сохранить</button>`
      );
    }
    if (passBtn) changePassword(passBtn.dataset.pass);
    if (delBtn) deleteUser(delBtn.dataset.delUser);
  });

  // Create user button
  const createUserBtn = $('create-user-btn');
  if (createUserBtn) {
    createUserBtn.addEventListener('click', () => {
      openModal(
        'Создать пользователя',
        '<label>Логин</label><input id="cu-username" placeholder="username">' +
        '<label>ФИО</label><input id="cu-fullname" placeholder="Иванов Иван Иванович">' +
        '<label>Пароль</label><input type="password" id="cu-password" placeholder="Мин. 8 символов">' +
        '<label>Роль</label><select id="cu-role"><option value="1">Администратор</option><option value="2" selected>Пользователь</option></select>' +
        `<label style="margin-top:12px;font-size:0.8rem;color:var(--muted)">Права:</label>
        <div style="display:flex;flex-wrap:wrap;gap:8px;margin-top:4px;">
          <label><input type="checkbox" id="cp-view" checked> Просмотр</label>
          <label><input type="checkbox" id="cp-download" checked> Скачивание</label>
          <label><input type="checkbox" id="cp-upload"> Загрузка</label>
          <label><input type="checkbox" id="cp-delete"> Удаление</label>
          <label><input type="checkbox" id="cp-manage"> Управление</label>
        </div>`,
        '<button type="button" class="btn" data-act="cancel">Отмена</button>' +
        '<button type="button" class="btn" data-act="create-user">Создать</button>'
      );
    });
  }

  // Theme buttons
  document.querySelectorAll('.btn-option').forEach((btn) => {
    btn.addEventListener('click', () => setTheme(btn.dataset.theme));
  });

  // Modal backdrop close
  $('modal').addEventListener('click', (e) => {
    if (e.target.id === 'modal') closeModal();
  });

  // Modal actions
  $('modal-actions').addEventListener('click', (e) => {
    const btn = e.target.closest('[data-act]');
    if (!btn) return;

    if (btn.dataset.act === 'cancel') {
      closeModal();
      return;
    }

    if (btn.dataset.act === 'upload-doc') {
      const number = $('new-number').value.trim();
      const name = $('new-name').value.trim();
      const fileInput = $('new-file');
      if (!number || !name || !fileInput.files[0]) {
        showMsg('Заполните все поля и выберите файл');
        return;
      }
      const formData = new FormData();
      formData.append('number', number);
      formData.append('title', name);
      formData.append('file', fileInput.files[0]);
      uploadDocument(formData);
      return;
    }

    if (btn.dataset.act === 'save-user') {
      const userId = btn.dataset.user;
      const formData = new FormData();
      formData.set('fullName', $('edit-name').value.trim());
      formData.set('roleId', $('edit-role').value);
      formData.set('isActive', $('ep-active').checked ? 'on' : '');
      formData.set('perm-view', $('ep-view').checked ? 'on' : '');
      formData.set('perm-download', $('ep-download').checked ? 'on' : '');
      formData.set('perm-upload', $('ep-upload').checked ? 'on' : '');
      formData.set('perm-delete', $('ep-delete').checked ? 'on' : '');
      formData.set('perm-manage', $('ep-manage').checked ? 'on' : '');
      updateUser(userId, formData);
      return;
    }

    if (btn.dataset.act === 'create-user') {
      const formData = new FormData();
      formData.set('username', $('cu-username').value.trim());
      formData.set('fullName', $('cu-fullname').value.trim());
      formData.set('password', $('cu-password').value);
      formData.set('roleId', $('cu-role').value);
      formData.set('perm-view', $('cp-view').checked ? 'on' : '');
      formData.set('perm-download', $('cp-download').checked ? 'on' : '');
      formData.set('perm-upload', $('cp-upload').checked ? 'on' : '');
      formData.set('perm-delete', $('cp-delete').checked ? 'on' : '');
      formData.set('perm-manage', $('cp-manage').checked ? 'on' : '');
      if (!formData.get('username') || !formData.get('fullName') || !formData.get('password')) {
        showMsg('Заполните все поля');
        return;
      }
      createUser(formData);
      return;
    }
  });
}

document.addEventListener('DOMContentLoaded', init);
