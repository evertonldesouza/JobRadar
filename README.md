<h1 align="center">JobRadar</h1>

<p align="center">
  Agregador de vagas de tecnologia com autenticação JWT, OAuth com Google e API REST em .NET.
</p>

<p align="center">
  <a href="https://jobradar-rzbd.onrender.com" target="_blank">Ver Demo</a> ·
  <a href="https://github.com/evertonldesouza/JobRadar/issues">Reportar Bug</a>
</p>

> [!IMPORTANT]
> A demo está hospedada no **Render (free tier)**.  
> O servidor hiberna quando inativo — a **primeira carga pode levar de 20 a 40 segundos**.  
> Aguarde um instante, a aplicação vai carregar normalmente.

---

## Sobre

O JobRadar agrega vagas de tecnologia de múltiplas fontes, permite filtrar por tecnologia e localização, salvar favoritos e recebe atualizações automáticas via CI/CD.

## Funcionalidades

- Autenticação com JWT e OAuth via Google
- Listagem e filtragem de vagas por tecnologia e localização
- Sistema de favoritos por usuário
- Paginação e scoring de relevância
- 5 fontes de vagas agregadas (RemoteOK, Remotive, CareerNest, JobsBase, Adzuna)
- Sincronização manual via `POST /api/jobs/sync`
- Health check com verificação de banco de dados
- Exception middleware global com logging centralizado
- Validação com FluentValidation + pipeline MediatR
- Middleware de exceções com respostas padronizadas
- Testes unitários com xUnit + Moq
- Deploy automatizado com GitHub Actions

## Stack

| Camada | Tecnologia |
|---|---|
| Backend | C# / .NET 10, ASP.NET Core |
| Banco | PostgreSQL |
| Autenticação | JWT + Google OAuth |
| Infra | Docker, docker-compose |
| Deploy | Render (API) + GitHub Pages (Frontend) |
| CI/CD | GitHub Actions |

## Rodando localmente

**Pré-requisitos:** Docker e .NET 10 SDK instalados.

```bash
# Clone o repositório
git clone https://github.com/evertonldesouza/JobRadar.git
cd JobRadar

# Sobe o banco via Docker
docker-compose up -d

# Rode a API
dotnet run --project src/JobRadar.API
```

A API estará disponível em `http://localhost:5000`.

## Fontes de vagas

O JobRadar agrega vagas de **5 fontes públicas** — todas gratuitas e sem necessidade de chave de API:

| Fonte | Endpoint | Tipo | Chave necessária |
|---|---|---|---|
| [RemoteOK](https://remoteok.com) | `GET /api?limit=50` | API REST | ❌ |
| [Remotive](https://remotive.com) | `GET /api/remote-jobs` | API REST | ❌ |
| [CareerNest](https://careernest.cloud) | `GET /api/feed?limit=50` | API REST | ❌ |
| [JobsBase](https://jobsbase.io) | `GET /api/v1/jobs?limit=50` | API REST | ❌ |
| [Adzuna](https://adzuna.com) | `GET /api/v1/api/jobs/gb/search/1` | API REST | ✅ `Adzuna:AppId` + `Adzuna:AppKey` |

O sync ocorre via `POST /api/jobs/sync` — cada fonte é consultada em sequência com deduplicação por URL e título+empresa. Fontes que falham (timeout, erro HTTP) não interrompem as demais.

Para adicionar uma nova fonte, crie uma classe implementando `IJobSource` em `src/JobRadar.Infrastructure/Sources/` e registre-a em `Program.cs`:

```csharp
builder.Services.AddHttpClient<SeuNovoSource>();
builder.Services.AddTransient<IJobSource, SeuNovoSource>();
```

O handler `SyncJobsCommandHandler` descobre automaticamente todos os `IJobSource` registrados via DI.

## Variáveis de ambiente

| Variável | Descrição |
|---|---|
| `ConnectionStrings__Default` | String de conexão PostgreSQL |
| `Jwt__Secret` | Chave secreta para geração de tokens JWT |
| `Google__ClientId` | Client ID do OAuth Google |
| `Google__ClientSecret` | Client Secret do OAuth Google |
| `Adzuna__AppId` | App ID da API Adzuna (apenas se for usar a fonte Adzuna) |
| `Adzuna__AppKey` | App Key da API Adzuna (apenas se for usar a fonte Adzuna) |

## Autor

**Everton L. de Souza** — [LinkedIn](https://linkedin.com/in/evertonldesouza) · [GitHub](https://github.com/evertonldesouza)