const API = 'https://jobradar-api-rgcy.onrender.com';
let token = localStorage.getItem('jobradar_token');
let favorites = [];

async function wakeUpApi() {
    const grid = document.getElementById('jobs-grid');
    grid.innerHTML = `
        <div class="loading">
            <i class="fas fa-circle-notch fa-spin"></i>
            <p>Acordando o servidor...</p>
            <p class="loading-sub">O servidor hiberna quando não está em uso. Aguarde alguns segundos.</p>
            <div class="loading-bar"><div class="loading-bar-fill"></div></div>
        </div>
    `;

    const maxTentativas = 10;
    for (let i = 0; i < maxTentativas; i++) {
        try {
            const res = await fetch(`${API}/healthz`, { signal: AbortSignal.timeout(5000) });
            if (res.ok) return true;
        } catch {}
        await new Promise(r => setTimeout(r, 3000));
    }
    return false;
}

async function fetchJobs(technology = '', location = '') {
    const grid = document.getElementById('jobs-grid');

    const online = await wakeUpApi();
    if (!online) {
        grid.innerHTML = '<div class="loading">Servidor indisponível. Tente novamente em alguns minutos.</div>';
        return;
    }

    grid.innerHTML = '<div class="loading"><i class="fas fa-circle-notch fa-spin"></i> Carregando vagas...</div>';

    try {
        const params = new URLSearchParams();
        if (technology) params.append('technology', technology);
        if (location) params.append('location', location);

        const res = await fetch(`${API}/api/Jobs?${params}`);
        const jobs = await res.json();

        if (token) await fetchFavorites();

        grid.innerHTML = '';
        jobs.forEach(job => renderJobCard(job, grid));
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

function renderJobCard(job, container, isFavoriteSection = false) {
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
        <div class="job-company"><i class="fas fa-building"></i> ${job.company || 'Empresa não informada'}</div>
        <div class="job-location"><i class="fas fa-map-marker-alt"></i> ${job.location || 'Remoto'}</div>
        <div class="job-tags">
            ${(job.technologies || []).slice(0, 5).map(t => `<span class="tag">${t}</span>`).join('')}
        </div>
        <div class="job-footer">
            <span>${new Date(job.publishedAt).toLocaleDateString('pt-BR')}</span>
            <span class="source-badge">${job.source}</span>
        </div>
    `;

    const favBtn = card.querySelector('.btn-favorite');
    if (favBtn) {
        favBtn.addEventListener('click', () => toggleFavorite(job.id, favBtn));
    }

    container.appendChild(card);
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