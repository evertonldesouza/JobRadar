'use strict';

/* Base da API (fixa por segurança: nunca aceite a URL da API via query/localStorage).
   Para desenvolvimento local, troque esta constante para http://localhost:5225. */
const API = 'https://jobradar-api-rgcy.onrender.com';

const PAGE_SIZE = 20;
const GUEST_FAVORITES_KEY = 'jobradar_guest_favorites';

let token = localStorage.getItem('jobradar_token');
let refreshToken = localStorage.getItem('jobradar_refresh');
let userName = localStorage.getItem('jobradar_user') || '';
let favorites = [];
let favoriteJobs = [];
let guestFavorites = loadGuestFavorites();
let currentPage = 1;
let totalPages = 1;
let totalCount = 0;
let isLoading = false;
let debounceTimer = null;

const $ = (id) => document.getElementById(id);
const grid = $('jobs-grid');

/* ---------- utilidades ---------- */

/** Vagas salvas por quem ainda não entrou (vivem só neste navegador). */
function loadGuestFavorites() {
    try {
        const raw = JSON.parse(localStorage.getItem(GUEST_FAVORITES_KEY) || '[]');
        return Array.isArray(raw) ? raw.filter((job) => job && job.id).slice(0, 100) : [];
    } catch { return []; }
}

function saveGuestFavorites() {
    try {
        localStorage.setItem(GUEST_FAVORITES_KEY, JSON.stringify(guestFavorites.slice(0, 100)));
    } catch { /* navegação privada: mantém só em memória */ }
}

/** Só aceita http(s): evita que um link javascript:/data: vindo da API seja clicável. */
function safeUrl(value) {
    try {
        const url = new URL(String(value || ''));
        return url.protocol === 'https:' || url.protocol === 'http:' ? url.href : null;
    } catch { return null; }
}

/** Escreve o texto destacando o termo buscado — via nós de texto (sem innerHTML). */
function appendHighlighted(container, text, term) {
    const value = String(text ?? '');
    const needle = String(term || '').trim();
    if (!needle) { container.textContent = value; return; }

    const haystack = value.toLowerCase();
    const lower = needle.toLowerCase();
    let index = 0;
    let found = haystack.indexOf(lower);

    if (found === -1) { container.textContent = value; return; }

    while (found !== -1) {
        if (found > index) container.appendChild(document.createTextNode(value.slice(index, found)));
        const mark = document.createElement('mark');
        mark.className = 'hl';
        mark.textContent = value.slice(found, found + needle.length);
        container.appendChild(mark);
        index = found + needle.length;
        found = haystack.indexOf(lower, index);
    }
    if (index < value.length) container.appendChild(document.createTextNode(value.slice(index)));
}
function el(tag, className, text) {
    const node = document.createElement(tag);
    if (className) node.className = className;
    if (text !== undefined) node.textContent = text;
    return node;
}

function toast(message) {
    const box = $('toast');
    box.textContent = message;
    box.hidden = false;
    clearTimeout(toast._timer);
    toast._timer = setTimeout(() => { box.hidden = true; }, 3600);
}

function isTokenExpired(jwt) {
    try {
        const payload = JSON.parse(atob(jwt.split('.')[1].replace(/-/g, '+').replace(/_/g, '/')));
        return typeof payload.exp === 'number' && payload.exp * 1000 <= Date.now();
    } catch { return true; }
}

// Access expirado não encerra a sessão: tentamos renovar com o refresh token na inicialização.
if (token && isTokenExpired(token)) token = null;

function fetchWithTimeout(url, ms, options = {}) {
    const ctrl = new AbortController();
    const id = setTimeout(() => ctrl.abort(), ms);
    return fetch(url, { ...options, signal: ctrl.signal }).finally(() => clearTimeout(id));
}

function authHeaders() {
    return token ? { Authorization: `Bearer ${token}` } : {};
}

async function readError(res) {
    try {
        const data = await res.json();
        if (data.errors) return Object.values(data.errors).flat().join(' ');
        return data.detail || data.title || `Erro ${res.status}`;
    } catch { return `Erro ${res.status}`; }
}

function updateURL() {
    const params = new URLSearchParams(location.search);
    const tech = $('filter-tech').value.trim();
    const loc = $('filter-location').value.trim();
    const src = $('filter-source').value;
    tech ? params.set('tech', tech) : params.delete('tech');
    loc ? params.set('loc', loc) : params.delete('loc');
    src ? params.set('source', src) : params.delete('source');
    if (currentPage > 1) params.set('page', currentPage); else params.delete('page');
    const qs = params.toString();
    history.replaceState({}, '', qs ? `${location.pathname}?${qs}` : location.pathname);
}

/* ---------- estado visual ---------- */
function skeletonCards(count = 6) {
    grid.replaceChildren();
    for (let i = 0; i < count; i++) {
        const card = el('div', 'job-card skeleton-card');
        card.setAttribute('aria-hidden', 'true');
        card.innerHTML = `<div class="skeleton-line skeleton-title"></div>
            <div class="skeleton-line skeleton-company"></div>
            <div class="skeleton-line skeleton-location"></div>
            <div class="skeleton-tags"><span class="skeleton-tag"></span><span class="skeleton-tag"></span><span class="skeleton-tag"></span></div>
            <div class="skeleton-line skeleton-footer"></div>`;
        grid.appendChild(card);
    }
}

function showEmptyState() {
    grid.replaceChildren();
    const box = el('div', 'empty-state');
    const icon = el('i', 'fas fa-search-minus');
    icon.setAttribute('aria-hidden', 'true');
    box.append(icon, el('p', null, 'Nenhuma vaga encontrada'), el('p', 'loading-sub', 'Tente ajustar os filtros de tecnologia ou localização.'));
    grid.appendChild(box);
}

function showErrorState(onRetry) {
    grid.replaceChildren();
    const box = el('div', 'loading loading-error');
    box.append(el('p', null, 'Não foi possível carregar as vagas agora.'),
        el('p', 'loading-sub', 'O servidor pode estar acordando (plano gratuito). Tente novamente em instantes.'));
    const btn = el('button', 'btn-primary', 'Tentar novamente');
    btn.type = 'button';
    btn.addEventListener('click', onRetry);
    box.appendChild(btn);
    grid.appendChild(box);
}

function renderJobCount(showing) {
    const counter = $('job-counter');
    if (totalCount === 0) { counter.textContent = 'Nenhuma vaga encontrada'; return; }
    counter.textContent = `Exibindo ${showing} de ${totalCount} vagas`;
}

function renderLoadMore() {
    const container = $('load-more-container');
    container.replaceChildren();
    if (currentPage >= totalPages || totalPages === 0) return;

    const showing = grid.querySelectorAll('.job-card:not(.skeleton-card)').length;
    const btn = el('button', 'btn-outline btn-load-more', `Carregar mais (${showing} de ${totalCount})`);
    btn.type = 'button';
    btn.id = 'btn-load-more';
    btn.addEventListener('click', () => fetchJobs(true));
    container.appendChild(btn);
}

/* ---------- cards (sem innerHTML com dados da API: proteção contra XSS) ---------- */
const TAG_CLASSES = {
    'c#': 'tag-csharp', '.net': 'tag-dotnet', python: 'tag-python', javascript: 'tag-javascript',
    js: 'tag-js', typescript: 'tag-typescript', ts: 'tag-ts', react: 'tag-react', java: 'tag-java',
    go: 'tag-go', docker: 'tag-docker', sql: 'tag-sql'
};
const SOURCE_DESCRIPTIONS = {
    RemoteOK: 'Vagas remotas de tecnologia via RemoteOK', Remotive: 'Vagas remotas curadas via Remotive',
    CareerNest: 'Vagas globais agregadas via CareerNest', JobsBase: 'Vagas para builders via JobsBase',
    Adzuna: 'Vagas de tecnologia via API Adzuna'
};

function favoriteIds() {
    return token ? favorites : guestFavorites.map((job) => job.id);
}

function isFavorite(jobId) {
    return favoriteIds().includes(jobId);
}

function syncFavoriteButton(btn, active) {
    btn.classList.toggle('active', active);
    btn.setAttribute('aria-pressed', String(active));
    btn.setAttribute('aria-label', active ? 'Remover dos favoritos' : 'Salvar nos favoritos');
    const icon = btn.querySelector('i');
    if (icon) icon.className = `${active ? 'fas' : 'far'} fa-star`;
}

function renderJobCard(job, index = 0, term = '') {
    const hasFavorite = isFavorite(job.id);
    const card = el('article', 'job-card fade-in-up');
    card.style.animationDelay = `${index * 40}ms`;

    const header = el('div', 'job-card-header');
    const link = el('a', 'job-title');
    link.href = `job.html?id=${encodeURIComponent(job.id)}`;
    appendHighlighted(link, job.title || 'Vaga sem título', term);
    header.appendChild(link);

    const favBtn = el('button', `btn-favorite${hasFavorite ? ' active' : ''}`);
    favBtn.type = 'button';
    favBtn.setAttribute('aria-pressed', String(hasFavorite));
    favBtn.setAttribute('aria-label', hasFavorite ? 'Remover dos favoritos' : 'Salvar nos favoritos');
    const star = el('i', `${hasFavorite ? 'fas' : 'far'} fa-star`);
    star.setAttribute('aria-hidden', 'true');
    favBtn.appendChild(star);
    favBtn.addEventListener('click', () => toggleFavorite(job, favBtn));
    header.appendChild(favBtn);

    const meta = el('div', 'job-meta');
    const company = el('div', 'job-company');
    company.append(Object.assign(el('i', 'fas fa-building'), { ariaHidden: 'true' }), document.createTextNode(` ${job.company || 'Empresa não informada'}`));
    const locationLine = el('div', 'job-location');
    locationLine.append(Object.assign(el('i', 'fas fa-map-marker-alt'), { ariaHidden: 'true' }), document.createTextNode(` ${job.location || 'Remoto'}`));
    meta.append(company, locationLine);

    const tags = el('div', 'job-tags');
    (job.technologies || []).slice(0, 5).forEach((tech) => {
        const tag = el('span', `tag ${TAG_CLASSES[String(tech).toLowerCase()] || 'tag-default'}`);
        appendHighlighted(tag, String(tech), term);
        tags.appendChild(tag);
    });

    const footer = el('div', 'job-footer');
    const metaBox = el('div', 'job-footer-meta');
    const date = job.publishedAt ? new Date(job.publishedAt).toLocaleDateString('pt-BR') : '';
    const badge = el('span', 'source-badge', job.source || '');
    badge.dataset.source = job.source || '';
    badge.title = SOURCE_DESCRIPTIONS[job.source] || `Fonte: ${job.source || ''}`;
    metaBox.append(el('span', null, date), badge);
    footer.appendChild(metaBox);

    const openUrl = safeUrl(job.url);
    if (openUrl) {
        const apply = el('a', 'btn-apply');
        apply.href = openUrl;
        apply.target = '_blank';
        apply.rel = 'noopener noreferrer';
        apply.setAttribute('aria-label', `Abrir vaga ${job.title || ''} no site original (nova aba)`);
        apply.append(document.createTextNode('Abrir vaga '));
        const applyIcon = el('i', 'fas fa-arrow-up-right-from-square');
        applyIcon.setAttribute('aria-hidden', 'true');
        apply.appendChild(applyIcon);
        footer.appendChild(apply);
    }

    card.append(header, meta, tags, footer);
    return card;
}

/* ---------- vagas ---------- */
async function waitForJobsJson(url, budgetMs = 3 * 60 * 1000) {
    const deadline = Date.now() + budgetMs;
    let attempt = 0;
    while (Date.now() < deadline) {
        attempt++;
        try {
            const res = await fetchWithTimeout(url, 60_000);
            if (res.ok) {
                const data = await res.json();
                if (data && Array.isArray(data.items)) return data;
            }
        } catch { /* servidor acordando: tenta de novo */ }
        await new Promise((r) => setTimeout(r, Math.min(3000 * attempt, 15000)));
    }
    return null;
}

async function fetchJobs(append = false) {
    if (isLoading) return;
    isLoading = true;
    if (append) currentPage++; else currentPage = 1;
    updateURL();

    const params = new URLSearchParams();
    const tech = $('filter-tech').value.trim();
    const loc = $('filter-location').value.trim();
    const src = $('filter-source').value;
    if (tech) params.append('technology', tech);
    if (loc) params.append('location', loc);
    if (src) params.append('source', src);
    params.append('page', String(currentPage));
    params.append('pageSize', String(PAGE_SIZE));
    const url = `${API}/api/Jobs?${params}`;

    try {
        if (!append) skeletonCards();
        let res;
        try {
            res = await fetchWithTimeout(url, 45_000);
        } catch {
            const data = await waitForJobsJson(url); // cold start do Render
            if (!data) throw new Error('indisponível');
            await applyJobsPage(data, append);
            return;
        }
        if (!res.ok) throw new Error(`HTTP ${res.status}`);
        await applyJobsPage(await res.json(), append);
    } catch {
        if (!append) showErrorState(() => fetchJobs(false));
        else toast('Não foi possível carregar mais vagas.');
    } finally {
        isLoading = false;
        renderLoadMore();
    }
}

async function applyJobsPage(data, append) {
    totalPages = data.totalPages || 1;
    totalCount = data.totalCount || 0;
    await refreshFavorites();
    const term = $('filter-tech').value.trim();
    if (!append) grid.replaceChildren();
    if (!append && data.items.length === 0) { showEmptyState(); renderJobCount(0); return; }
    data.items.forEach((job, i) => grid.appendChild(renderJobCard(job, i, term)));
    renderJobCount(grid.querySelectorAll('.job-card:not(.skeleton-card)').length);
}

/* ---------- favoritos (conta + visitante) ---------- */
async function refreshFavorites(retried = false) {
    if (!token) {
        renderFavoritesSection();
        return;
    }
    try {
        const res = await fetch(`${API}/api/Favorites`, { headers: authHeaders() });
        if (res.status === 401) {
            if (!retried && await tryRefresh()) return refreshFavorites(true);
            return;
        }
        if (!res.ok) return;
        favoriteJobs = await res.json();
        favorites = favoriteJobs.map((job) => job.id);
        renderFavoritesSection();
    } catch { /* mantém a listagem utilizável sem favoritos */ }
}

function renderFavoritesSection() {
    const jobs = token ? favoriteJobs : guestFavorites;
    const favGrid = $('favorites-grid');
    favGrid.replaceChildren();
    jobs.forEach((job, i) => favGrid.appendChild(renderJobCard(job, i)));

    const empty = $('favorites-empty');
    empty.textContent = token
        ? 'Você ainda não favoritou nenhuma vaga. Toque na estrela de um card para salvar aqui.'
        : 'Toque na estrela de uma vaga para salvá-la aqui — ao entrar, seus favoritos vão junto para sua conta.';
    empty.hidden = jobs.length > 0;

    // Visitante sem favoritos não vê uma seção vazia.
    $('favorites-section').hidden = !token && jobs.length === 0;
}

async function toggleFavorite(job, btn) {
    const currentlyFav = isFavorite(job.id);
    btn.disabled = true;

    if (!token) {
        guestFavorites = currentlyFav
            ? guestFavorites.filter((item) => item.id !== job.id)
            : [...guestFavorites, job];
        saveGuestFavorites();
        syncFavoriteButton(btn, !currentlyFav);
        renderFavoritesSection();
        toast(currentlyFav
            ? 'Removido dos favoritos.'
            : 'Salvo neste navegador. Entre para levar seus favoritos para a conta.');
        btn.disabled = false;
        return;
    }

    try {
        const res = await fetch(`${API}/api/Favorites/${job.id}`, {
            method: currentlyFav ? 'DELETE' : 'POST', headers: authHeaders()
        });
        if (!res.ok && res.status !== 204) throw new Error(await readError(res));
        syncFavoriteButton(btn, !currentlyFav);
        toast(currentlyFav ? 'Removido dos favoritos.' : 'Salvo nos favoritos!');
        await refreshFavorites();
    } catch (err) {
        toast(err.message || 'Não foi possível atualizar o favorito.');
    } finally {
        btn.disabled = false;
    }
}

/** Leva os favoritos salvos no navegador para a conta, logo após entrar. */
async function mergeGuestFavorites() {
    if (!token || guestFavorites.length === 0) return;

    try {
        const res = await fetch(`${API}/api/Favorites/merge`, {
            method: 'POST',
            headers: { ...authHeaders(), 'Content-Type': 'application/json' },
            body: JSON.stringify({ jobIds: guestFavorites.map((job) => job.id) })
        });
        if (res.status === 401 || !res.ok) return;

        const total = guestFavorites.length;
        guestFavorites = [];
        saveGuestFavorites();
        toast(total === 1
            ? '1 favorito salvo aqui foi sincronizado com sua conta.'
            : `${total} favoritos salvos aqui foram sincronizados com sua conta.`);
    } catch { /* tenta de novo na próxima visita */ }
}

/* ---------- alertas ---------- */
async function fetchAlerts() {
    if (!token) return;
    try {
        const res = await fetch(`${API}/api/Alerts`, { headers: authHeaders() });
        if (!res.ok) return;
        const alerts = await res.json();
        const list = $('alerts-list');
        list.replaceChildren();
        if (alerts.length === 0) {
            list.appendChild(el('li', 'loading-sub', 'Nenhum alerta criado ainda.'));
            return;
        }
        alerts.forEach((alert) => {
            const item = el('li', 'alert-item');
            const label = [alert.technology, alert.location, alert.source].filter(Boolean).join(' · ') || 'Todas as vagas';
            item.appendChild(el('span', null, label));
            const del = el('button', 'btn-outline btn-alert-delete', 'Excluir');
            del.type = 'button';
            del.setAttribute('aria-label', `Excluir alerta ${label}`);
            del.addEventListener('click', async () => {
                const r = await fetch(`${API}/api/Alerts/${alert.id}`, { method: 'DELETE', headers: authHeaders() });
                if (r.ok || r.status === 204) { toast('Alerta excluído.'); fetchAlerts(); }
                else toast(await readError(r));
            });
            item.appendChild(del);
            list.appendChild(item);
        });
    } catch { /* silencioso */ }
}

/* ---------- autenticação ---------- */
function setSession(data) {
    token = data.token;
    refreshToken = data.refreshToken || refreshToken;
    userName = data.name || userName || '';
    localStorage.setItem('jobradar_token', token);
    if (refreshToken) localStorage.setItem('jobradar_refresh', refreshToken);
    localStorage.setItem('jobradar_user', userName);
}

/** Renova o access token usando o refresh token (rotação no servidor). */
async function tryRefresh() {
    const stored = refreshToken || localStorage.getItem('jobradar_refresh');
    if (!stored) return false;
    try {
        const res = await fetch(`${API}/api/Auth/refresh`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ refreshToken: stored })
        });
        if (!res.ok) return false;
        setSession(await res.json());
        return true;
    } catch { return false; }
}

function updateAuthSection() {
    const section = $('auth-section');
    section.replaceChildren();
    if (token) {
        const chip = el('span', 'user-chip', userName ? `Olá, ${userName.split(' ')[0]}` : 'Logado');
        const logoutBtn = el('button', 'btn-outline', 'Sair');
        logoutBtn.type = 'button';
        logoutBtn.addEventListener('click', () => logout());
        section.append(chip, logoutBtn);
        $('favorites-section').hidden = false;
        $('alerts-section').hidden = false;
    } else {
        const loginBtn = el('button', 'btn-outline', 'Entrar / Criar conta');
        loginBtn.type = 'button';
        loginBtn.id = 'btn-login';
        loginBtn.addEventListener('click', openAuthDialog);
        section.appendChild(loginBtn);
        $('alerts-section').hidden = true;
        renderFavoritesSection();
    }
}

async function logout(message) {
    const access = token;
    const refresh = refreshToken;
    token = null;
    refreshToken = null;
    userName = '';
    favorites = [];
    favoriteJobs = [];
    localStorage.removeItem('jobradar_token');
    localStorage.removeItem('jobradar_refresh');
    localStorage.removeItem('jobradar_user');
    updateAuthSection();
    renderFavoritesSection();
    if (message) toast(message);
    fetchJobs(false);

    if (access) {
        try {
            await fetch(`${API}/api/Auth/logout`, {
                method: 'POST',
                headers: { Authorization: `Bearer ${access}`, 'Content-Type': 'application/json' },
                body: JSON.stringify({ refreshToken: refresh })
            });
        } catch { /* sessão local já encerrada */ }
    }
}

function openAuthDialog() {
    const dialog = $('auth-dialog');
    if (typeof dialog.showModal === 'function') dialog.showModal();
    else dialog.setAttribute('open', '');
}

function closeAuthDialog() {
    const dialog = $('auth-dialog');
    if (typeof dialog.close === 'function') dialog.close();
    else dialog.removeAttribute('open');
}

function showAuthForm(name) {
    ['form-login', 'form-register', 'form-forgot', 'form-reset'].forEach((id) => { $(id).hidden = id !== `form-${name}`; });
    const isTab = name === 'login' || name === 'register';
    $('tab-login').classList.toggle('active', name === 'login');
    $('tab-register').classList.toggle('active', name === 'register');
    $('tab-login').setAttribute('aria-selected', String(name === 'login'));
    $('tab-register').setAttribute('aria-selected', String(name === 'register'));
    document.querySelector('.auth-tabs').style.display = isTab ? '' : 'none';
    $('auth-title').textContent = { login: 'Acesse sua conta', register: 'Crie sua conta', forgot: 'Recuperar senha', reset: 'Criar nova senha' }[name];
}

function showAuthTab(register) { showAuthForm(register ? 'register' : 'login'); }

let pendingResetToken = null;

async function submitAuth(formId, errorId, path, payload) {
    const errorBox = $(errorId);
    errorBox.hidden = true;
    try {
        const res = await fetch(`${API}${path}`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });
        if (!res.ok) throw new Error(await readError(res));
        setSession(await res.json());
        closeAuthDialog();
        updateAuthSection();
        toast(`Bem-vindo${userName ? `, ${userName.split(' ')[0]}` : ''}!`);
        await mergeGuestFavorites();
        await refreshFavorites();
        await fetchAlerts();
        fetchJobs(false);
    } catch (err) {
        errorBox.textContent = err.message || 'Não foi possível concluir. Tente novamente.';
        errorBox.hidden = false;
    }
}

/* ---------- eventos ---------- */
$('filters-form').addEventListener('submit', (event) => {
    event.preventDefault();
    clearTimeout(debounceTimer);
    fetchJobs(false);
});
$('btn-clear').addEventListener('click', () => {
    $('filter-tech').value = '';
    $('filter-location').value = '';
    $('filter-source').value = '';
    fetchJobs(false);
});
['filter-tech', 'filter-location'].forEach((id) => $(id).addEventListener('input', () => {
    clearTimeout(debounceTimer);
    debounceTimer = setTimeout(() => fetchJobs(false), 400);
}));
$('filter-source').addEventListener('change', () => fetchJobs(false));
$('btn-login')?.addEventListener('click', openAuthDialog);
$('auth-close').addEventListener('click', closeAuthDialog);
$('tab-login').addEventListener('click', () => showAuthTab(false));
$('tab-register').addEventListener('click', () => showAuthTab(true));
$('btn-google').addEventListener('click', () => { location.href = `${API}/api/Auth/google`; });
$('form-login').addEventListener('submit', (event) => {
    event.preventDefault();
    submitAuth('form-login', 'login-error', '/api/Auth/login', {
        email: $('login-email').value.trim(), password: $('login-password').value
    });
});
$('link-forgot').addEventListener('click', () => showAuthForm('forgot'));
$('link-back-login').addEventListener('click', () => showAuthForm('login'));
$('form-forgot').addEventListener('submit', async (event) => {
    event.preventDefault();
    const errorBox = $('forgot-error');
    errorBox.hidden = true;
    try {
        const res = await fetch(`${API}/api/Auth/forgot-password`, {
            method: 'POST', headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ email: $('forgot-email').value.trim() })
        });
        if (!res.ok && res.status !== 204) throw new Error(await readError(res));
        closeAuthDialog();
        toast('Se o email tiver conta, enviamos um link de recuperação. Confira sua caixa de entrada.');
    } catch (err) {
        errorBox.textContent = err.message || 'Não foi possível enviar. Tente novamente.';
        errorBox.hidden = false;
    }
});
$('form-reset').addEventListener('submit', async (event) => {
    event.preventDefault();
    const errorBox = $('reset-error');
    errorBox.hidden = true;
    try {
        const res = await fetch(`${API}/api/Auth/reset-password`, {
            method: 'POST', headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                email: $('reset-email').value.trim(),
                token: pendingResetToken,
                newPassword: $('reset-password').value
            })
        });
        if (!res.ok && res.status !== 204) throw new Error(await readError(res));
        pendingResetToken = null;
        closeAuthDialog();
        toast('Senha redefinida! Entre com a nova senha.');
        openAuthDialog();
        showAuthForm('login');
    } catch (err) {
        errorBox.textContent = err.message || 'Não foi possível redefinir. Solicite um novo link.';
        errorBox.hidden = false;
    }
});
$('form-register').addEventListener('submit', (event) => {
    event.preventDefault();
    submitAuth('form-register', 'register-error', '/api/Auth/register', {
        name: $('register-name').value.trim(), email: $('register-email').value.trim(), password: $('register-password').value
    });
});
$('alert-form').addEventListener('submit', async (event) => {
    event.preventDefault();
    const payload = {
        technology: $('alert-tech').value.trim() || null,
        location: $('alert-location').value.trim() || null,
        source: $('alert-source').value || null
    };
    try {
        const res = await fetch(`${API}/api/Alerts`, {
            method: 'POST', headers: { ...authHeaders(), 'Content-Type': 'application/json' }, body: JSON.stringify(payload)
        });
        if (!res.ok) throw new Error(await readError(res));
        toast('Alerta criado! Você será avisado sobre novas vagas compatíveis.');
        event.target.reset();
        fetchAlerts();
    } catch (err) { toast(err.message || 'Não foi possível criar o alerta.'); }
});

/* ---------- início ---------- */
const initialParams = new URLSearchParams(location.search);
$('filter-tech').value = initialParams.get('tech') || '';
$('filter-location').value = initialParams.get('loc') || '';
$('filter-source').value = initialParams.get('source') || '';
currentPage = Math.max(parseInt(initialParams.get('page') || '1', 10) || 1, 1);

// Retorno do login com Google: tokens chegam no fragmento (#token=...), que
// não é enviado a servidores. Lemos, salvamos e limpamos a URL na hora.
const fragmentParams = new URLSearchParams(location.hash.replace(/^#/, ''));
const redirectedToken = fragmentParams.get('token') || initialParams.get('token');
if (fragmentParams.get('authError')) {
    toast('Não foi possível entrar com o Google. Tente novamente ou use email e senha.');
}
if (redirectedToken) {
    token = redirectedToken;
    localStorage.setItem('jobradar_token', redirectedToken);
    const redirectedRefresh = fragmentParams.get('refreshToken');
    if (redirectedRefresh) { refreshToken = redirectedRefresh; localStorage.setItem('jobradar_refresh', redirectedRefresh); }
    toast('Login realizado com sucesso!');
}
const resetTokenParam = fragmentParams.get('resetToken');
if (resetTokenParam) pendingResetToken = resetTokenParam;
if (location.hash || initialParams.has('token')) {
    const clean = new URLSearchParams(location.search);
    clean.delete('token');
    const qs = clean.toString();
    history.replaceState({}, '', location.pathname + (qs ? `?${qs}` : ''));
}

(async () => {
    if (!token && (refreshToken || localStorage.getItem('jobradar_refresh'))) {
        await tryRefresh();
    }
    updateAuthSection();
    if (resetTokenParam) {
        openAuthDialog();
        showAuthForm('reset');
        toast('Crie sua nova senha para concluir a recuperação.');
    }
    if (token) {
        await mergeGuestFavorites();
        fetchAlerts();
    }
    refreshFavorites();
    fetchJobs(false);
})();
