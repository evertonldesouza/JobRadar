# JobRadar — Sessão opencode

## Skills
- dotnet-patterns
- csharp-testing
- api-design
- security-review
- postgres-patterns
- database-migrations
- docker-patterns

## Stack
- .NET 10 (net10.0)
- Clean Architecture (Domain, Application, Infrastructure, API)
- MediatR (CQRS)
- EF Core + PostgreSQL
- JWT + Google OAuth
- Scalar (OpenAPI)

## Commands
```bash
# rodar com banco local
docker-compose up -d
dotnet run --project src/JobRadar.API
# stack completa (API + banco)
docker compose up --build

# testes
dotnet test src/JobRadar.Tests

# build docker
docker build -t jobradar-api .
```

## Padrões
- Controllers usam MediatR (não injetam serviços de domínio diretamente)
- Registros de DI por camada: `AddApplication()` e `AddInfrastructure()` — Program.cs só compõe
- Erros em RFC 7807 via `GlobalExceptionHandler`; nunca exponha entidades, devolva DTOs
- Validadores atuam sobre commands/queries MediatR (não sobre DTOs)
- Exceções de negócio jogam `InvalidOperationException` ou `UnauthorizedAccessException`
- Middleware global captura exceções e retorna JSON padronizado
- Validators com FluentValidation aplicados via MediatR pipeline
- Testes com xUnit + Moq
