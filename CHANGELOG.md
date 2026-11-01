# Changelog

Formato baseado em [Keep a Changelog](https://keepachangelog.com/pt-BR/), versionamento [SemVer](https://semver.org/lang/pt-BR/).

## [Não lançado]

### Adicionado (descoberta de vagas)
- `GET /api/jobs/{id}`: detalhe da vaga, com 404 em ProblemDetails para id inexistente.
- Busca por tecnologia tolerante a caixa e que também considera título e empresa; o termo buscado é destacado nos cards sem uso de `innerHTML`.
- Página de detalhes (`docs/job.html`) com empresas, tecnologias, data, CTA para o anúncio original, compartilhamento (Web Share/clipboard), vagas parecidas e meta tags dinâmicas.
- Favoritos para visitantes (armazenados no navegador) e `POST /api/favorites/merge` para levá-los à conta no login, de forma idempotente e limitada a 200 ids.

### Adicionado (sessão, email e testes)
- Login com Google agora redireciona ao frontend com os tokens no fragmento da URL (antes respondia JSON no navegador e o login nunca concluía no site).
- Refresh tokens rotativos com detecção de reuso, `POST /api/auth/refresh`, `POST /api/auth/logout`, `GET /api/auth/me` e recuperação de senha por email (`forgot/reset-password`, token de 1h).
- Envio real de email via SMTP (`Smtp:*`) para alertas e recuperação; sem SMTP configurado, mantém o mock de desenvolvimento.
- Testes de integração (`WebApplicationFactory` + EF InMemory) e retry com backoff nas fontes externas (timeout de 30s por fonte).
- Filtros de fonte/localização case-insensitive; testes de integração agora semeiam vagas e cobrem detalhe e sincronização de favoritos.


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
