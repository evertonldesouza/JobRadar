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
- Deploy automatizado com GitHub Actions

## Stack

| Camada | Tecnologia |
|---|---|
| Backend | C# / .NET 8, ASP.NET Core |
| Banco | PostgreSQL |
| Autenticação | JWT + Google OAuth |
| Infra | Docker, docker-compose |
| Deploy | Render (API) + GitHub Pages (Frontend) |
| CI/CD | GitHub Actions |

## Rodando localmente

**Pré-requisitos:** Docker e .NET 8 SDK instalados.

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

## Variáveis de ambiente

| Variável | Descrição |
|---|---|
| `ConnectionStrings__Default` | String de conexão PostgreSQL |
| `Jwt__Secret` | Chave secreta para geração de tokens JWT |
| `Google__ClientId` | Client ID do OAuth Google |
| `Google__ClientSecret` | Client Secret do OAuth Google |

## Autor

**Everton L. de Souza** — [LinkedIn](https://linkedin.com/in/evertonldesouza) · [GitHub](https://github.com/evertonldesouza)