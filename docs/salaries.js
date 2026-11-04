'use strict';

/* Comparativo salarial por tecnologia. Mesma base de API das outras páginas. */
const API = 'https://jobradar-api-rgcy.onrender.com';

const $ = (id) => document.getElementById(id);

const CURRENCY_LABELS = {
    USD: 'Dólar americano (US$)',
    BRL: 'Real brasileiro (R$)',
    EUR: 'Euro (€)',
    GBP: 'Libra esterlina (£)',
    CAD: 'Dólar canadense',
    AUD: 'Dólar australiano',
    INR: 'Rupia indiana (₹)',
    CHF: 'Franco suíço',
    PLN: 'Zlóti polonês',
    MXN: 'Peso mexicano',
    ARS: 'Peso argentino',
    JPY: 'Iene japonês'
};

let currentCurrency = '';

function el(tag, className, text) {
    const node = document.createElement(tag);
    if (className) node.className = className;
    if (text !== undefined) node.textContent = text;
    return node;
}

function announce(message) {
    $('salary-status').textContent = message;
}

function formatMoney(value, currency) {
    try {
        return new Intl.NumberFormat('pt-BR', {
            style: 'currency',
            currency,
            maximumFractionDigits: 0
        }).format(value);
    } catch {
        return `${currency} ${Math.round(value).toLocaleString('pt-BR')}`;
    }
}

function fetchWithTimeout(url, ms, options = {}) {
    const ctrl = new AbortController();
    const id = setTimeout(() => ctrl.abort(), ms);
    return fetch(url, { ...options, signal: ctrl.signal }).finally(() => clearTimeout(id));
}

async function loadSummary(currency) {
    const params = new URLSearchParams({ minimumSamples: '3', maxTechnologies: '12' });
    if (currency) params.set('currency', currency);

    // O plano gratuito hiberna: tenta algumas vezes antes de desistir.
    for (let attempt = 1; attempt <= 4; attempt++) {
        try {
            const res = await fetchWithTimeout(`${API}/api/Salaries/summary?${params}`, 45000);
            if (res.ok) return await res.json();
        } catch { /* servidor acordando */ }
        await new Promise((resolve) => setTimeout(resolve, attempt * 2000));
    }
    return null;
}

function renderCurrencyOptions(data) {
    const select = $('salary-currency');
    if (select.dataset.ready === 'true') { select.value = data.currency; return; }

    select.replaceChildren();
    (data.availableCurrencies || []).forEach((option) => {
        const opt = document.createElement('option');
        opt.value = option.currency;
        const label = CURRENCY_LABELS[option.currency] || option.currency;
        opt.textContent = `${label} · ${option.samples} ${option.samples === 1 ? 'vaga' : 'vagas'}`;
        select.appendChild(opt);
    });
    select.value = data.currency;
    select.disabled = (data.availableCurrencies || []).length <= 1;
    select.dataset.ready = 'true';
    currentCurrency = data.currency;
}

function renderCoverage(data) {
    const box = $('salary-coverage');
    const { jobsWithSalary, totalJobs, percentage } = data.coverage || {};
    box.textContent = totalJobs
        ? `${jobsWithSalary} de ${totalJobs} vagas informam salário (${String(percentage).replace('.', ',')}%).`
        : 'Nenhuma vaga com salário informado ainda.';
}

function renderChart(data) {
    const container = $('salary-chart');
    container.replaceChildren();
    container.setAttribute('aria-busy', 'false');

    const technologies = data.technologies || [];
    if (technologies.length === 0) {
        const empty = el('div', 'empty-state');
        empty.append(
            el('i', 'fas fa-chart-simple'),
            el('p', null, 'Ainda não há amostras suficientes'),
            el('p', 'loading-sub', `Precisamos de pelo menos ${data.minimumSamples || 3} vagas com salário por tecnologia nesta moeda.`)
        );
        container.appendChild(empty);
        announce('Sem amostras suficientes para o comparativo.');
        return;
    }

    const max = Math.max(...technologies.map((t) => t.p75Annual || t.medianAnnual));

    const list = el('ol', 'salary-list');
    technologies.forEach((tech) => {
        const item = el('li', 'salary-row');

        const head = el('div', 'salary-row-head');
        head.append(
            el('span', 'salary-tech', tech.technology),
            el('span', 'salary-median', formatMoney(tech.medianAnnual, data.currency))
        );

        const track = el('div', 'salary-track');
        track.setAttribute('aria-hidden', 'true');

        // Faixa interquartil (p25 → p75)
        const spread = el('div', 'salary-spread');
        const left = (tech.p25Annual / max) * 100;
        const width = Math.max(((tech.p75Annual - tech.p25Annual) / max) * 100, 1.5);
        spread.style.left = `${Math.max(left, 0)}%`;
        spread.style.width = `${width}%`;

        // Marcador da mediana
        const marker = el('div', 'salary-marker');
        marker.style.left = `${Math.min((tech.medianAnnual / max) * 100, 99)}%`;

        track.append(spread, marker);

        const foot = el('div', 'salary-row-foot');
        foot.textContent = `p25 ${formatMoney(tech.p25Annual, data.currency)} · p75 ${formatMoney(tech.p75Annual, data.currency)} · ${tech.samples} ${tech.samples === 1 ? 'vaga' : 'vagas'}`;

        item.append(head, track, foot);
        list.appendChild(item);
    });

    container.appendChild(list);
    announce(`Comparativo com ${technologies.length} tecnologias carregado.`);
}

function renderError(message) {
    const container = $('salary-chart');
    container.replaceChildren();
    container.setAttribute('aria-busy', 'false');

    const box = el('div', 'loading loading-error');
    box.append(el('p', null, message),
        el('p', 'loading-sub', 'O servidor pode estar acordando (plano gratuito). Tente novamente em instantes.'));

    const retry = el('button', 'btn-primary', 'Tentar novamente');
    retry.type = 'button';
    retry.addEventListener('click', () => init(currentCurrency));
    box.appendChild(retry);

    container.appendChild(box);
    announce('Falha ao carregar o comparativo.');
}

async function init(currency) {
    const data = await loadSummary(currency);

    if (!data) {
        renderError('Não foi possível carregar o comparativo agora.');
        return;
    }

    renderCurrencyOptions(data);
    renderCoverage(data);
    renderChart(data);
    $('salary-disclaimer').textContent = data.disclaimer || '';
}

$('salary-currency').addEventListener('change', (event) => {
    currentCurrency = event.target.value;
    $('salary-chart').setAttribute('aria-busy', 'true');
    init(currentCurrency);
});

init('');
