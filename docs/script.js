const API = 'https://jobradar-api-rgcy.onrender.com';
let token = localStorage.getItem('jobradar_token');
let favorites = [];
let currentPage = 1;
let currentTech = new URLSearchParams(location.search).get('tech') || '';
let currentLocation = new URLSearchParams(location.search).get('loc') || '';
let currentSource = new URLSearchParams(location.search).get('source') || '';
let totalPages = 1;
let isLoading = false;
let debounceTimer = null;

function fetchWithTimeout(url, ms, simple = false) {
    const ctrl = new AbortController();
    const id = setTimeout(() => ctrl.abort(), ms);
    const opts = simple
        ? { signal: ctrl.signal }
        : { signal: ctrl.signal, cache: 'no-store', credentials: 'omit' };
    return fetch(url, opts).finally(() => clearTimeout(id));
}

function skeletonCards(count = 6) {
    let html = '';
    for (let i = 0; i < count; i++) {
        html += `
            <div class="job-card skeleton-card">
                <div class="skeleton-line skeleton-title"></div>
                <div class="skeleton-line skeleton-company"></div>
                <div class="skeleton-line skeleton-location"></div>
                <div class="skeleton-tags">
                    <span class="skeleton-tag"></span>
                    <span class="skeleton-tag"></span>
                    <span class="skeleton-tag"></span>
                </div>
                <div class="skeleton-line skeleton-footer"></div>
            </div>`;
    }
    return html;
}

function emptyStateHTML() {
    return `
        <div class="empty-state">
            <i class="fas fa-search-minus"></i>
            <p>Nenhuma vaga encontrada</p>
            <p class="loading-sub">Tente ajustar os filtros de tecnologia ou localização</p>
        </div>`;
}

function updateURL() {
    const params = new URLSearchParams();
    if (currentTech) params.set('tech', currentTech);
    if (currentLocation) params.set('loc', currentLocation);
    if (currentSource) params.set('source', currentSource);
    if (currentPage > 1) params.set('page', currentPage);
    const url = params.toString()
        ? `${location.pathname}?${params}`
        : location.pathname;
    history.replaceState({}, '', url);
}

async function waitForJobsJson(url, budgetMs) {
    const started = Date.now();
    const deadline = started + budgetMs;
    let attempt = 0;

    const updateElapsed = () => {
        const el = document.getElementById('wake-elapsed');
        if (!el) return;
        const sec = Math.floor((Date.now() - started) / 1000);
        el.textContent = `Tentativa ${attempt} · ${sec}s aguardando…`;
    };

    while (Date.now() < deadline) {
        attempt++;
        updateElapsed();
        const timeoutMs = attempt <= 3 ? 120000 : 75000;
        try {
            const res = await fetchWithTimeout(url, timeoutMs, true);
            if (!res.ok) {
                await new Promise(r =>
                    setTimeout(r, Math.max(3000, Math.min(25000, Math.round(3500 * Math.pow(1.22, attempt - 1)))))
                );
                continue;
            }
            const data = await res.json();
            if (data && Array.isArray(data.items)) return data;
        } catch {}
        await new Promise(r =>
            setTimeout(r, Math.max(3000, Math.min(25000, Math.round(3500 * Math.pow(1.22, attempt - 1)))))
        );
    }
    return null;
}

async function fetchJobs(technology, location, source, page, append = false) {
    if (isLoading) return;
    isLoading = true;
    currentTech = technology || '';
    currentLocation = location || '';
    currentSource = source || '';
    currentPage = page;
    updateURL();

    const grid = document.getElementById('jobs-grid');
    const params = new URLSearchParams();
    if (technology) params.append('technology', technology);
    if (location) params.append('location', location);
    if (source) params.append('source', source);
    params.append('page', page);
    params.append('pageSize', 20);
    const listUrl = `${API}/api/Jobs?${params}`;

    if (!append) {
        const wasColdStart = !document.querySelector('.job-card') && page === 1;
        if (wasColdStart) {
            grid.innerHTML = `
                <div class="loading">
                    <i class="fas fa-circle-notch fa-spin"></i>
                    <p>Acordando o servidor e carregando vagas...</p>
                    <p class="loading-sub" id="wake-hint">No plano gratuito do Render isso pode levar vários minutos.</p>
                    <p class="loading-sub wake-elapsed" id="wake-elapsed" aria-live="polite"></p>
                    <div class="loading-bar"><div class="loading-bar-fill"></div></div>
                </div>`;
            const data = await waitForJobsJson(listUrl, 12 * 60 * 1000);
            if (!data) {
                grid.innerHTML = `
                    <div class="loading loading-error">
                        <p>Servidor indisponível ou ainda acordando.</p>
                        <p class="loading-sub">Confira o serviço no Render. Se não carregar, faça <strong>Ctrl+F5</strong> (ou limpe o cache) para pegar o JavaScript mais recente.</p>
                        <button type="button" class="btn-primary" id="btn-retry-load">
                            <i class="fas fa-redo"></i> Tentar novamente
                        </button>
                    </div>`;
                document.getElementById('btn-retry-load').addEventListener('click', () =>
                    fetchJobs(currentTech, currentLocation, currentSource, 1));
                isLoading = false;
                return;
            }
            totalPages = data.totalPages;
            if (token) await fetchFavorites();
            grid.innerHTML = '';
            if (data.items.length === 0) {
                grid.innerHTML = emptyStateHTML();
                renderJobCount(0, 0, 1);
                renderLoadMore();
                isLoading = false;
                return;
            }
            data.items.forEach(job => renderJobCard(job, grid));
            renderJobCount(data.totalCount, data.items.length, page);
            renderLoadMore();
            isLoading = false;
            return;
        }

        grid.innerHTML = skeletonCards(6);
        try {
            const res = await fetchWithTimeout(listUrl, 120000, true);
            if (!res.ok) {
                grid.innerHTML = `<div class="loading">Erro ao carregar vagas (HTTP ${res.status}).</div>`;
                isLoading = false;
                return;
            }
            const data = await res.json();
            totalPages = data.totalPages;
            if (token) await fetchFavorites();
            grid.innerHTML = '';
            if (data.items.length === 0) {
                grid.innerHTML = emptyStateHTML();
                renderJobCount(0, 0, 1);
                renderLoadMore();
                isLoading = false;
                return;
            }
            data.items.forEach((job, i) => {
                const card = renderJobCard(job, null);
                card.style.animationDelay = `${i * 40}ms`;
                grid.appendChild(card);
            });
            renderJobCount(data.totalCount, data.items.length, page);
            renderLoadMore();
        } catch {
            grid.innerHTML = '<div class="loading">Erro ao carregar vagas.</div>';
        }
        isLoading = false;
        return;
    }

    try {
        const btn = document.getElementById('btn-load-more');
        if (btn) {
            btn.disabled = true;
            btn.innerHTML = '<i class="fas fa-circle-notch fa-spin"></i> Carregando...';
        }
        const res = await fetchWithTimeout(listUrl, 120000, true);
        if (!res.ok) {
            if (btn) {
                btn.disabled = false;
                btn.innerHTML = '<i class="fas fa-redo"></i> Tentar novamente';
            }
            isLoading = false;
            return;
        }
        const data = await res.json();
        totalPages = data.totalPages;
        if (data.items.length === 0) {
            renderLoadMore();
            isLoading = false;
            return;
        }
        const firstNewCard = grid.children.length;
        data.items.forEach((job, i) => {
            const card = renderJobCard(job, null);
            card.style.animationDelay = `${i * 40}ms`;
            grid.appendChild(card);
        });
        renderJobCount(data.totalCount, grid.querySelectorAll('.job-card').length, 1);
        renderLoadMore();
        setTimeout(() => {
            const cards = grid.querySelectorAll('.job-card');
            if (cards.length > 0) {
                cards[firstNewCard]?.scrollIntoView({ behavior: 'smooth', block: 'start' });
            }
        }, 100);
    } catch {
        const btn = document.getElementById('btn-load-more');
        if (btn) {
            btn.disabled = false;
            btn.innerHTML = '<i class="fas fa-redo"></i> Tentar novamente';
        }
    }
    isLoading = false;
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

const sourceDescriptions = {
    'RemoteOK': 'Vagas remotas de tecnologia via RemoteOK',
    'Remotive': 'Vagas remotas curadas via Remotive',
    'CareerNest': 'Vagas globais agregadas via CareerNest',
    'JobsBase': 'Vagas para builders via JobsBase',
    'Adzuna': 'Vagas de tecnologia via API Adzuna'
};

function renderJobCard(job, container) {
    const isFav = favorites.includes(job.id);
    const card = document.createElement('div');
    card.className = 'job-card fade-in-up';
    const sourceDesc = sourceDescriptions[job.source] || `Fonte: ${job.source}`;
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
            <span class="source-badge" data-source="${job.source}" title="${sourceDesc}">${job.source}</span>
        </div>
    `;

    const favBtn = card.querySelector('.btn-favorite');
    if (favBtn) favBtn.addEventListener('click', () => toggleFavorite(job.id, favBtn));

    if (container) {
        container.appendChild(card);
    }
    return card;
}

function renderJobCount(total, showing, page) {
    let counter = document.getElementById('job-counter');
    if (!counter) {
        counter = document.createElement('p');
        counter.id = 'job-counter';
        counter.className = 'job-counter';
        document.querySelector('.filters-wrapper').insertAdjacentElement('afterend', counter);
    }
    if (total === 0) {
        counter.textContent = 'Nenhuma vaga encontrada';
        return;
    }
    const start = (page - 1) * 20 + 1;
    const end = Math.min(start + showing - 1, total);
    counter.textContent = `Exibindo ${start}–${end} de ${total} vagas`;
}

function renderLoadMore() {
    let container = document.getElementById('load-more-container');
    if (!container) {
        container = document.createElement('div');
        container.id = 'load-more-container';
        container.className = 'load-more-container';
        document.getElementById('jobs-grid').insertAdjacentElement('afterend', container);
    }

    const showing = document.querySelectorAll('.job-card').length;
    if (currentPage >= totalPages || totalPages === 0) {
        container.innerHTML = '';
        return;
    }

    container.innerHTML = `
        <button id="btn-load-more" class="btn-outline btn-load-more">
            <i class="fas fa-chevron-down"></i> Carregar mais (${showing} de ${document.getElementById('job-counter')?.textContent?.match(/de (\d+)/)?.[1] || '...'})
        </button>`;

    document.getElementById('btn-load-more').addEventListener('click', () => {
        currentPage++;
        fetchJobs(currentTech, currentLocation, currentSource, currentPage, true);
    });
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

function debounceSearch() {
    clearTimeout(debounceTimer);
    debounceTimer = setTimeout(() => {
        const tech = document.getElementById('filter-tech').value;
        const loc = document.getElementById('filter-location').value;
        const src = document.getElementById('filter-source').value;
        fetchJobs(tech, loc, src, 1);
    }, 400);
}

document.getElementById('btn-search').addEventListener('click', () => {
    clearTimeout(debounceTimer);
    const tech = document.getElementById('filter-tech').value;
    const loc = document.getElementById('filter-location').value;
    const src = document.getElementById('filter-source').value;
    fetchJobs(tech, loc, src, 1);
});

document.getElementById('filter-source').addEventListener('change', debounceSearch);

document.getElementById('filter-tech').addEventListener('input', debounceSearch);
document.getElementById('filter-location').addEventListener('input', debounceSearch);

document.getElementById('filter-tech').addEventListener('keydown', e => {
    if (e.key === 'Enter') {
        clearTimeout(debounceTimer);
        document.getElementById('btn-search').click();
    }
});
document.getElementById('filter-location').addEventListener('keydown', e => {
    if (e.key === 'Enter') {
        clearTimeout(debounceTimer);
        document.getElementById('btn-search').click();
    }
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

document.getElementById('filter-tech').value = currentTech;
document.getElementById('filter-location').value = currentLocation;
document.getElementById('filter-source').value = currentSource;

updateAuthSection();
fetchJobs(currentTech, currentLocation, currentSource, 1);
