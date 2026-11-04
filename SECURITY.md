# Política de Segurança

## Reportando uma vulnerabilidade

Não abra uma issue pública. Envie um email para o mantenedor (Everton L. de Souza, via GitHub [@evertonldesouza](https://github.com/evertonldesouza)) com a descrição, impacto e passos de reprodução. Você receberá uma resposta em até 5 dias úteis.

## Boas práticas adotadas

- Segredos apenas por variável de ambiente/User Secrets; a API não sobe sem `Jwt:Secret` forte.
- Senhas com BCrypt; JWT com expiração configurável (padrão 24h) e claim `jti`.
- Rate limiting por IP, cabeçalhos de segurança, CORS configurável e erros sem detalhes internos em produção (RFC 7807).
- Dependências monitoradas pelo Dependabot e análise estática com CodeQL.
