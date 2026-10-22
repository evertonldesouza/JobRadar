# Changelog

Formato baseado em [Keep a Changelog](https://keepachangelog.com/pt-BR/), versionamento [SemVer](https://semver.org/lang/pt-BR/).

## [Não lançado]

### Adicionado
- Health checks `/health/live` e `/health/ready`; frontend com login/cadastro por email, alertas e toasts.
- CI com cobertura, build Docker, CodeQL e Dependabot; `CONTRIBUTING.md`, `SECURITY.md`, licença MIT.
- Registro de dependências por camada (`AddApplication()` / `AddInfrastructure()`), `Directory.Build.props`, `.editorconfig`, `global.json`.

### Alterado
- Erros agora seguem RFC 7807 (ProblemDetails) com `traceId`, via `IExceptionHandler`.
- Sync busca fontes em paralelo e deduplica em lote (fim do N+1); cache com invalidação thread-safe.
- Validadores FluentValidation agora atuam sobre os commands/queries do MediatR (antes validavam DTOs que o pipeline não enxergava); senha exige 8+ caracteres com letras e números.
- Token JWT com validade configurável (padrão 24h, antes 7 dias fixos).

### Segurança
- `POST /api/jobs/sync` e `DELETE /api/jobs/duplicates` agora exigem autenticação (antes eram públicos).
- Rate limiting efetivamente aplicado aos controllers; política estrita para login/registro.
- Segredo JWT removido do `appsettings.json` (a API falha ao subir sem um segredo forte), CORS por configuração, cabeçalhos de segurança, export CSV protegido contra CSV injection e correlation id por requisição.
- Conta do Google vinculada automaticamente a contas existentes com o mesmo email.
