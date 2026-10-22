# Contribuindo com o JobRadar

Obrigado pelo interesse! 

## Ambiente

- .NET 10 SDK (versão fixada em `global.json`), Docker e Node 22 (só para checar o frontend).
- Suba tudo com `cp .env.example .env && docker compose up --build`, ou o banco com `docker compose up -d postgres` + `dotnet run --project src/JobRadar.API`.

## Fluxo

1. Crie uma branch a partir de `main` (`feat/...`, `fix/...`, `docs/...`).
2. Faça mudanças pequenas e focadas, seguindo a Clean Architecture: regras em `Domain`, use cases em `Application`, detalhes externos em `Infrastructure`, controllers finos na `API`.
3. Rode `dotnet build -c Release`, `dotnet test` e `node --check docs/script.js`.
4. Abra o PR descrevendo o problema, a solução e como testou. A CI (build, testes, cobertura, Docker e CodeQL) precisa passar.

## Convenções

- Controllers usam MediatR — não injete repositórios neles (exceção histórica: `AuthController` no callback do Google).
- Exceções de negócio: `InvalidOperationException` (409), `UnauthorizedAccessException` (401), `KeyNotFoundException` (404) — o handler global converte para ProblemDetails.
- Valide toda entrada com FluentValidation no pipeline; nunca exponha entidades de domínio — devolva DTOs.
- Não commite segredos: use `.env` (ignorado) ou User Secrets.
