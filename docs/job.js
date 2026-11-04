'use strict';

/* Página de detalhes de uma vaga. Mesma base de API da listagem (fixa por segurança). */
const API = 'https://jobradar-api-rgcy.onrender.com';
const GUEST_FAVORITES_KEY = 'jobradar_guest_favorites';
const TOKEN_KEY = 'jobradar_token';
const REFRESH_KEY = 'jobradar_refresh';

const $ = (id) => document.getElementById(id);

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

let token = localStorage.getItem(TOKEN_KEY);
let refreshToken = localStorage.getItem(REFRESH_KEY);
let currentJob = null;

/* ---------- utilidades ---------- */
function el(tag, className, text) {
    const node = document.createElement(tag);
    if (className) node.className = className;
    if (text !== undefined) node.textContent = text;
    return node;
}

function icon(className) {
    const node = el('i', className);
    node.setAttribute('aria-hidden', 'true');
    return node;
}

function toast(message) {
    const box = $('toast');
    box.textContent = message;
    box.hidden = false;
    clearTimeout(toast._timer);
    toast._timer = setTimeout(() => { box.hidden = true; }, 3600);
}

function announce(message) {
    $('detail-status').textContent = message;
}

function safeUrl(value) {
    try {
        const url = new URL(String(value || ''));
        return url.protocol === 'https:' || url.protocol === 'http:' ? url.href : null;
    } catch { return null; }
}

function formatDate(value) {
    if (!value) return '';
    const date = new Date(value);
    return Number.isNaN(date.getTime()) ? '' : date.toLocaleDateString('pt-BR', { day: '2-digit', month: 'long', year: 'numeric' });
}

function isTokenExpired(jwt) {
    try {
        const payload = JSON.parse(atob(jwt.split('.')[1].replace(/-/g, '+').replace(/_/g, '/')));
        return typeof payload.exp === 'number' && payload.exp * 1000 <= Date.now();
    } catch { return true; }
}

function fetchWithTimeout(url, ms, options = {}) {
    const ctrl = new AbortController();
    const id = setTimeout(() => ctrl.abort(), ms);
    return fetch(url, { ...options, signal: ctrl.signal }).finally(() => clearTimeout(id));
}

/* ---------- sessão ---------- */
function persistSession(data) {
    token = data.token;
    refreshToken = data.refreshToken || refreshToken;
    localStorage.setItem(TOKEN_KEY, token);
    if (refreshToken) localStorage.setItem(REFRESH_KEY, refreshToken);
    if (data.name) localStorage.setItem('jobradar_user', data.name);
}

async function tryRefresh() {
    const stored = refreshToken || localStorage.getItem(REFRESH_KEY);
    if (!stored) return false;
    try {
        const res = await fetchWithTimeout(`${API}/api/Auth/refresh`, 20000, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ refreshToken: stored })
        });
        if (!res.ok) { clearSession(); return false; }
        persistSession(await res.json());
        return true;
    } catch { return false; }
}

function clearSession() {
    token = null;
    refreshToken = null;
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(REFRESH_KEY);
}

function authHeaders() {
    return token ? { Authorization: `Bearer ${token}` } : {};
}

/* ---------- favoritos ---------- */
function loadGuestFavorites() {
    try {
        const raw = JSON.parse(localStorage.getItem(GUEST_FAVORITES_KEY) || '[]');
        return Array.isArray(raw) ? raw.filter((job) => job && job.id).slice(0, 100) : [];
    } catch { return []; }
}

function saveGuestFavorites(list) {
    try { localStorage.setItem(GUEST_FAVORITES_KEY, JSON.stringify(list.slice(0, 100))); } catch { /* modo privado */ }
}

function isGuestFavorite(jobId) {
    return loadGuestFavorites().some((job) => job.id === jobId);
}

async function isAccountFavorite(jobId) {
    try {
        const res = await fetchWithTimeout(`${API}/api/Favorites`, 15000, { headers: authHeaders() });
        if (res.status === 401 && await tryRefresh()) {
            const retry = await fetchWithTimeout(`${API}/api/Favorites`, 15000, { headers: authHeaders() });
            if (!retry.ok) return false;
            return (await retry.json()).some((job) => job.id === jobId);
        }
        if (!res.ok) return false;
        return (await res.json()).some((job) => job.id === jobId);
    } catch { return false; }
}

async function applyFavoriteState(btn) {
    if (!currentJob) return;
    const active = token ? await isAccountFavorite(currentJob.id) : isGuestFavorite(currentJob.id);
    syncFavoriteButton(btn, active);
    btn.disabled = false;
}

function syncFavoriteButton(btn, active) {
    btn.classList.toggle('active', active);
    btn.setAttribute('aria-pressed', String(active));
    btn.setAttribute('aria-label', active ? 'Remover dos favoritos' : 'Salvar nos favoritos');
    const star = btn.querySelector('i');
    if (star) star.className = `${active ? 'fas' : 'far'} fa-star`;
    const label = btn.querySelector('.btn-favorite-label');
    if (label) label.textContent = active ? 'Salvo nos favoritos' : 'Salvar nos favoritos';
}

async function toggleFavorite(btn) {
    if (!currentJob) return;
    btn.disabled = true;

    if (!token) {
        const stored = loadGuestFavorites();
        const already = stored.some((job) => job.id === currentJob.id);
        const updated = already ? stored.filter((job) => job.id !== currentJob.id) : [...stored, currentJob];
        saveGuestFavorites(updated);
        syncFavoriteButton(btn, !already);
        toast(already
            ? 'Removido dos favoritos.'
            : 'Salvo neste navegador. Entre na página inicial para levar seus favoritos para a conta.');
        btn.disabled = false;
        return;
    }

    try {
        const already = await isAccountFavorite(currentJob.id);
        let res = await fetchWithTimeout(`${API}/api/Favorites/${currentJob.id}`, 20000, {
            method: already ? 'DELETE' : 'POST', headers: authHeaders()
        });
        if (res.status === 401 && await tryRefresh()) {
            res = await fetchWithTimeout(`${API}/api/Favorites/${currentJob.id}`, 20000, {
                method: already ? 'DELETE' : 'POST', headers: authHeaders()
            });
        }
        if (!res.ok && res.status !== 204) throw new Error(await readError(res));
        syncFavoriteButton(btn, !already);
        toast(already ? 'Removido dos favoritos.' : 'Salvo nos favoritos!');
    } catch (err) {
        toast(err.message || 'Não foi possível atualizar o favorito.');
    } finally {
        btn.disabled = false;
    }
}

async function readError(res) {
    try {
        const data = await res.json();
        if (data.errors) return Object.values(data.errors).flat().join(' ');
        return data.detail || data.title || `Erro ${res.status}`;
    } catch { return `Erro ${res.status}`; }
}

/* ---------- carregamento da vaga ---------- */
async function fetchJob(id) {
    // O plano gratuito hiberna: algumas tentativas antes de desistir.
    for (let attempt = 1; attempt <= 4; attempt++) {
        try {
            const res = await fetchWithTimeout(`${API}/api/Jobs/${encodeURIComponent(id)}`, 45000);
            if (res.status === 404) return { notFound: true };
            if (res.ok) return { job: await res.json() };
        } catch { /* servidor acordando */ }
        await new Promise((resolve) => setTimeout(resolve, attempt * 2000));
    }
    return { failed: true };
}

function updateMeta(job) {
    const title = `${job.title} — ${job.company || 'JobRadar'}`;
    document.title = `${title} | JobRadar`;
    $('breadcrumb-current').textContent = job.title || 'Vaga';

    const description = [job.title, job.company, job.location]
        .filter(Boolean)
        .join(' · ') + ' — detalhes, tecnologias e link para se candidatar no JobRadar.';

    const meta = document.querySelector('meta[name="description"]');
    if (meta) meta.setAttribute('content', description);
    const ogTitle = document.querySelector('meta[property="og:title"]');
    if (ogTitle) ogTitle.setAttribute('content', `${title} | JobRadar`);
    const ogDescription = document.querySelector('meta[property="og:description"]');
    if (ogDescription) ogDescription.setAttribute('content', description);
}

function renderDetail(job) {
    const container = $('job-detail');
    container.replaceChildren();
    container.setAttribute('aria-busy', 'false');

    const heading = el('h1', 'detail-title');
    heading.textContent = job.title || 'Vaga sem título';
    container.appendChild(heading);

    const meta = el('div', 'detail-meta');
    const company = el('span', 'detail-meta-item');
    company.append(icon('fas fa-building'), document.createTextNode(` ${job.company || 'Empresa não informada'}`));
    const location = el('span', 'detail-meta-item');
    location.append(icon('fas fa-map-marker-alt'), document.createTextNode(` ${job.location || 'Remoto'}`));
    const published = el('span', 'detail-meta-item');
    published.append(icon('fas fa-calendar'), document.createTextNode(` ${formatDate(job.publishedAt)}`));
    meta.append(company, location, published);

    const source = el('span', 'source-badge', job.source || '');
    source.dataset.source = job.source || '';
    source.title = SOURCE_DESCRIPTIONS[job.source] || `Fonte: ${job.source || ''}`;
    meta.appendChild(source);
    container.appendChild(meta);

    if (Array.isArray(job.technologies) && job.technologies.length > 0) {
        const tags = el('div', 'job-tags detail-tags');
        tags.setAttribute('aria-label', 'Tecnologias da vaga');
        job.technologies.slice(0, 20).forEach((tech) => {
            tags.appendChild(el('span', `tag ${TAG_CLASSES[String(tech).toLowerCase()] || 'tag-default'}`, String(tech)));
        });
        container.appendChild(tags);
    }

    const actions = el('div', 'detail-actions');
    const openUrl = safeUrl(job.url);
    if (openUrl) {
        const apply = el('a', 'btn-primary');
        apply.href = openUrl;
        apply.target = '_blank';
        apply.rel = 'noopener noreferrer';
        apply.append(document.createTextNode('Candidatar-se no site original '));
        apply.appendChild(icon('fas fa-arrow-up-right-from-square'));
        actions.appendChild(apply);
    }

    const favBtn = el('button', 'btn-outline btn-favorite-detail');
    favBtn.type = 'button';
    favBtn.disabled = true;
    favBtn.setAttribute('aria-pressed', 'false');
    favBtn.setAttribute('aria-label', 'Salvar nos favoritos');
    favBtn.appendChild(icon('far fa-star'));
    favBtn.appendChild(el('span', 'btn-favorite-label', 'Salvar nos favoritos'));
    favBtn.addEventListener('click', () => toggleFavorite(favBtn));
    actions.appendChild(favBtn);

    const shareBtn = el('button', 'btn-outline');
    shareBtn.type = 'button';
    shareBtn.append(icon('fas fa-share-nodes'), document.createTextNode(' Compartilhar'));
    shareBtn.addEventListener('click', () => shareJob(job));
    actions.appendChild(shareBtn);

    container.appendChild(actions);

    const sourceNote = el('p', 'detail-note');
    sourceNote.textContent = `Esta vaga foi coletada automaticamente de ${job.source || 'uma fonte externa'}. `
        + 'O JobRadar não faz parte do processo seletivo: a candidatura acontece no site original.';
    container.appendChild(sourceNote);

    const back = el('a', 'detail-back', '← Voltar para a lista de vagas');
    back.href = 'index.html';
    container.appendChild(back);

    applyFavoriteState(favBtn);
    return job.technologies && job.technologies.length > 0 ? job.technologies[0] : null;
}

async function shareJob(job) {
    const url = location.href;
    try {
        if (navigator.share) {
            await navigator.share({ title: `${job.title} — ${job.company}`, text: job.title, url });
            return;
        }
        await navigator.clipboard.writeText(url);
        toast('Link da vaga copiado.');
    } catch {
        toast('Não foi possível compartilhar. Copie o link da barra de endereço.');
    }
}

/* ---------- vagas parecidas ---------- */
function renderSimilarCard(job) {
    const card = el('article', 'job-card');
    const header = el('div', 'job-card-header');
    const link = el('a', 'job-title', job.title || 'Vaga sem título');
    link.href = `job.html?id=${encodeURIComponent(job.id)}`;
    header.appendChild(link);
    card.appendChild(header);

    const meta = el('div', 'job-meta');
    const company = el('div', 'job-company');
    company.append(icon('fas fa-building'), document.createTextNode(` ${job.company || 'Empresa não informada'}`));
    const locationLine = el('div', 'job-location');
    locationLine.append(icon('fas fa-map-marker-alt'), document.createTextNode(` ${job.location || 'Remoto'}`));
    meta.append(company, locationLine);
    card.appendChild(meta);

    const tags = el('div', 'job-tags');
    (job.technologies || []).slice(0, 3).forEach((tech) => {
        tags.appendChild(el('span', `tag ${TAG_CLASSES[String(tech).toLowerCase()] || 'tag-default'}`, String(tech)));
    });
    if (tags.childElementCount > 0) card.appendChild(tags);

    return card;
}

async function loadSimilar(technology, currentId) {
    if (!technology) return;
    try {
        const res = await fetchWithTimeout(
            `${API}/api/Jobs?technology=${encodeURIComponent(technology)}&page=1&pageSize=7`, 30000);
        if (!res.ok) return;
        const data = await res.json();
        const items = (data.items || []).filter((job) => job.id !== currentId).slice(0, 6);
        if (items.length === 0) return;

        const grid = $('similar-grid');
        grid.replaceChildren();
        items.forEach((job) => grid.appendChild(renderSimilarCard(job)));
        $('similar-section').hidden = false;
    } catch { /* seção opcional */ }
}

/* ---------- estados de erro ---------- */
function renderError(message, retry) {
    const container = $('job-detail');
    container.replaceChildren();
    container.setAttribute('aria-busy', 'false');

    container.appendChild(el('h1', 'detail-title', 'Vaga indisponível'));
    container.appendChild(el('p', 'detail-note', message));

    if (retry) {
        const btn = el('button', 'btn-primary', 'Tentar novamente');
        btn.type = 'button';
        btn.addEventListener('click', retry);
        const actions = el('div', 'detail-actions');
        actions.appendChild(btn);
        container.appendChild(actions);
    }

    const back = el('a', 'detail-back', '← Voltar para a lista de vagas');
    back.href = 'index.html';
    container.appendChild(back);
    announce(message);
}

/* ---------- início ---------- */
(async () => {
    const params = new URLSearchParams(location.search);
    const id = (params.get('id') || '').trim();

    if (!/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(id)) {
        renderError('O endereço não aponta para uma vaga válida. Volte à lista e escolha uma vaga.', null);
        announce('Endereço inválido.');
        return;
    }

    if (token && isTokenExpired(token)) {
        token = null;
        localStorage.removeItem(TOKEN_KEY);
        await tryRefresh();
    }

    const result = await fetchJob(id);

    if (result.notFound) {
        renderError('Esta vaga não existe mais: ela pode ter sido removida ou expirada.', null);
        announce('Vaga não encontrada.');
        return;
    }

    if (result.failed || !result.job) {
        renderError('Não foi possível carregar a vaga agora. O servidor pode estar acordando (plano gratuito).',
            () => location.reload());
        announce('Falha ao carregar a vaga.');
        return;
    }

    currentJob = result.job;
    updateMeta(currentJob);
    const technology = renderDetail(currentJob);
    announce('Detalhes da vaga carregados.');
    loadSimilar(technology, currentJob.id);
})();
