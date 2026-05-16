const API = 'https://jobradar-api-rgcy.onrender.com';
let token = localStorage.getItem('jobradar_token');
let favorites = [];
let currentPage = 1;
let currentTech = '';
let currentLocation = '';
let totalPages = 1;

/** Timeout compatível com navegadores sem AbortSignal.timeout (ex.: Safari mais antigo). */
function fetchWithTimeout(url, ms) {
    const ctrl = new AbortController();
    const id = setTimeout(() => ctrl.abort(), ms);
    const opts = { signal: ctrl.signal, cache: 'no-store', credentials: 'omit' };
    return fetch(url, opts).finally(() => clearTimeout(id));
}

/** Ping isolado: se healthz disparar erro (ex.: CORS na página de erro do proxy), ainda tentamos /api/Jobs. */
async function pingApi(path, timeoutMs) {
    try {
        const res = await fetchWithTimeout(`${API}${path}`, timeoutMs);
        return res.ok;
    } catch {
        return false;
    }
}

async function wakeUpApi() {
    const grid = document.getElementById('jobs-grid');
    const started = Date.now();
    const totalBudgetMs = 8 * 60 * 1000;
    const deadline = started + totalBudgetMs;

    grid.innerHTML = `
        <div class="loading">
            <i class="fas fa-circle-notch fa-spin"></i>
            <p>Acordando o servidor...</p>
            <p class="loading-sub" id="wake-hint">No plano gratuito do Render o serviço pode levar <strong>2 a 4 minutos</strong> após ficar inativo. Esta página fica tentando até conseguir.</p>
            <p class="loading-sub wake-elapsed" id="wake-elapsed" aria-live="polite"></p>
            <div class="loading-bar"><div class="loading-bar-fill"></div></div>
        </div>
    `;

    const elapsedEl = () => document.getElementById('wake-elapsed');
    let attempt = 0;

    while (Date.now() < deadline) {
        attempt++;
        const sec = Math.floor((Date.now() - started) / 1000);
        const el = elapsedEl();
        if (el) el.textContent = `Tentativa ${attempt} · ${sec}s aguardando…`;

        const timeoutMs = attempt <= 3 ? 120000 : 75000;

        if (await pingApi('/healthz', timeoutMs)) return true;
        if (await pingApi('/api/Jobs?page=1&pageSize=1', timeoutMs)) return true;

        const waitMs = Math.max(
            3000,
            Math.min(25000, Math.round(3500 * Math.pow(1.22, attempt - 1)))
        );
        await new Promise(r => setTimeout(r, waitMs));
    }
    return false;
}

async function fetchJobs(technology = '', location = '', page = 1) {
    currentTech = technology;
    currentLocation = location;
    currentPage = page;

    const grid = document.getElementById('jobs-grid');

    if (page === 1) {
        const online = await wakeUpApi();
        if (!online) {
            grid.innerHTML = `
                <div class="loading loading-error">
                    <p>Servidor indisponível ou ainda acordando.</p>
                    <p class="loading-sub">Confirme no Render se o serviço está no ar; em seguida tente de novo.</p>
                    <button type="button" class="btn-primary" id="btn-retry-load">
                        <i class="fas fa-redo"></i> Tentar novamente
                    </button>
                </div>`;
            document.getElementById('btn-retry-load').addEventListener('click', () =>
                fetchJobs(currentTech, currentLocation, 1));
            return;
        }
        grid.innerHTML = '<div class="loading"><i class="fas fa-circle-notch fa-spin"></i> Carregando vagas...</div>';
    }

    try {
        const params = new URLSearchParams();
        if (technology) params.append('technology', technology);
        if (location) params.append('location', location);
        params.append('page', page);
        params.append('pageSize', 20);

        const res = await fetchWithTimeout(`${API}/api/Jobs?${params}`, 120000);
        const data = await res.json();

        totalPages = data.totalPages;

        if (token) await fetchFavorites();

        if (page === 1) grid.innerHTML = '';

        data.items.forEach(job => renderJobCard(job, grid));

        renderPagination();
        renderJobCount(data.totalCount, data.items.length, page);

    } catch {
        grid.innerHTML = '<div class="loading">Erro ao carregar vagas.</div>';
    }
}

async function fetchFavorites() {
    try {
        const res = await fetch(`${API}/api/Favorites`, {
            headers: { Authorization: `Bearer ${token}` }
        });
        const data = await res.json();
        favorites = data.map ? data.map(f => f.id) : (data.value || []).map(f => f.id);

        const favGrid = document.getElementById('favorites-grid');
        const favSection = document.getElementById('favorites-section');
        favGrid.innerHTML = '';

        const favJobs = data.value || data;
        if (favJobs.length > 0) {
            favSection.style.display = 'block';
            favJobs.forEach(job => renderJobCard(job, favGrid, true));
        }
    } catch {}
}

function getTagClass(tech) {
    const map = {
        'c#': 'tag-csharp', '.net': 'tag-dotnet', 'python': 'tag-python',
        'javascript': 'tag-javascript', 'js': 'tag-js', 'typescript': 'tag-typescript',
        'ts': 'tag-ts', 'react': 'tag-react', 'java': 'tag-java',
        'go': 'tag-go', 'docker': 'tag-docker', 'sql': 'tag-sql'
    };
    return map[tech.toLowerCase()] || 'tag-default';
}

function renderJobCard(job, container) {
    const isFav = favorites.includes(job.id);
    const card = document.createElement('div');
    card.className = 'job-card';
    card.innerHTML = `
        <div class="job-card-header">
            <a href="${job.url}" target="_blank" class="job-title">${job.title}</a>
            ${token ? `
                <button class="btn-favorite ${isFav ? 'active' : ''}" data-id="${job.id}">
                    <i class="fa${isFav ? 's' : 'r'} fa-star"></i>
                </button>
            ` : ''}
        </div>
        <div class="job-meta">
            <div class="job-company"><i class="fas fa-building"></i> ${job.company || 'Empresa não informada'}</div>
            <div class="job-location"><i class="fas fa-map-marker-alt"></i> ${job.location || 'Remoto'}</div>
        </div>
        <div class="job-tags">
            ${(job.technologies || []).slice(0, 5).map(t =>
                `<span class="tag ${getTagClass(t)}">${t}</span>`
            ).join('')}
        </div>
        <div class="job-footer">
            <span>${new Date(job.publishedAt).toLocaleDateString('pt-BR')}</span>
            <span class="source-badge">${job.source}</span>
        </div>
    `;

    const favBtn = card.querySelector('.btn-favorite');
    if (favBtn) favBtn.addEventListener('click', () => toggleFavorite(job.id, favBtn));

    container.appendChild(card);
}

function renderJobCount(total, showing, page) {
    let counter = document.getElementById('job-counter');
    if (!counter) {
        counter = document.createElement('p');
        counter.id = 'job-counter';
        counter.className = 'job-counter';
        document.querySelector('.filters-wrapper').insertAdjacentElement('afterend', counter);
    }
    const start = (page - 1) * 20 + 1;
    const end = start + showing - 1;
    counter.textContent = `Exibindo ${start}–${end} de ${total} vagas`;
}

function renderPagination() {
    let pagination = document.getElementById('pagination');
    if (!pagination) {
        pagination = document.createElement('div');
        pagination.id = 'pagination';
        pagination.className = 'pagination';
        document.getElementById('jobs-grid').insertAdjacentElement('afterend', pagination);
    }

    pagination.innerHTML = '';

    if (currentPage > 1) {
        const prev = document.createElement('button');
        prev.className = 'btn-outline';
        prev.innerHTML = '<i class="fas fa-chevron-left"></i> Anterior';
        prev.addEventListener('click', () => fetchJobs(currentTech, currentLocation, currentPage - 1));
        pagination.appendChild(prev);
    }

    const info = document.createElement('span');
    info.className = 'pagination-info';
    info.textContent = `Página ${currentPage} de ${totalPages}`;
    pagination.appendChild(info);

    if (currentPage < totalPages) {
        const next = document.createElement('button');
        next.className = 'btn-outline';
        next.innerHTML = 'Próxima <i class="fas fa-chevron-right"></i>';
        next.addEventListener('click', () => fetchJobs(currentTech, currentLocation, currentPage + 1));
        pagination.appendChild(next);
    }
}

async function toggleFavorite(jobId, btn) {
    const isFav = favorites.includes(jobId);
    try {
        if (isFav) {
            await fetch(`${API}/api/Favorites/${jobId}`, {
                method: 'DELETE',
                headers: { Authorization: `Bearer ${token}` }
            });
            favorites = favorites.filter(id => id !== jobId);
            btn.classList.remove('active');
            btn.innerHTML = '<i class="far fa-star"></i>';
        } else {
            await fetch(`${API}/api/Favorites/${jobId}`, {
                method: 'POST',
                headers: { Authorization: `Bearer ${token}` }
            });
            favorites.push(jobId);
            btn.classList.add('active');
            btn.innerHTML = '<i class="fas fa-star"></i>';
        }
        await fetchFavorites();
    } catch {}
}

function updateAuthSection() {
    const authSection = document.getElementById('auth-section');
    if (token) {
        authSection.innerHTML = `
            <span style="color: var(--text-muted); font-size: 14px;">
                <i class="fas fa-user-check"></i> Logado
            </span>
            <button class="btn-outline" onclick="logout()">
                <i class="fas fa-sign-out-alt"></i> Sair
            </button>
        `;
        document.getElementById('favorites-section').style.display = 'block';
    }
}

function logout() {
    localStorage.removeItem('jobradar_token');
    token = null;
    favorites = [];
    document.getElementById('favorites-section').style.display = 'none';
    updateAuthSection();
    fetchJobs();
}

document.getElementById('btn-search').addEventListener('click', () => {
    const tech = document.getElementById('filter-tech').value;
    const loc = document.getElementById('filter-location').value;
    fetchJobs(tech, loc);
});

const btnLogin = document.getElementById('btn-login');
if (btnLogin) {
    btnLogin.addEventListener('click', () => {
        window.location.href = `${API}/api/Auth/google`;
    });
}

const urlParams = new URLSearchParams(window.location.search);
const tokenParam = urlParams.get('token');
if (tokenParam) {
    localStorage.setItem('jobradar_token', tokenParam);
    token = tokenParam;
    window.history.replaceState({}, '', window.location.pathname);
}

updateAuthSection();
fetchJobs();