## Why

Para demonstrar o FiscoProjects, Marcelo hoje liga o próprio computador, sobe o banco, a API e um túnel do Cloudflare e passa o link. Com o computador desligado, não há demonstração, e montar o ambiente à mão em outro lugar leva muitos comandos. Marcelo pediu um jeito mais fácil e rápido de pôr a demonstração no ar e compartilhar o link (Cronograma-3.4, a pedido de Marcelo).

## What Changes

- Configuração do GitHub Codespaces no repositório (`.devcontainer/`): ao criar um Codespace, ele instala o SDK do .NET e o Node das versões fixadas, cria a senha do banco local, liga a API ao banco e compila as telas e a API.
- Ao abrir o Codespace, ele sobe o banco, a API com as telas na porta 5175 e carrega os dados de demonstração que faltam; deixa o link público e mostra o endereço e as pessoas para entrar.
- A chave do Syncfusion entra só como segredo do Codespaces na conta de Marcelo, nunca no repositório.
- O guia em `docs/demonstracao.md`: criar, compartilhar, parar e apagar o Codespace, e a cota gratuita.
- ADR-0113 com a decisão; os testes da verificação conferem que o Codespace usa as versões do `global.json` e do `.nvmrc` e a porta da API.

## Capabilities

### New Capabilities

### Modified Capabilities
- `dados-de-demonstracao`: a demonstração no Codespaces, montada e carregada sozinha, com o link para compartilhar.

## Impact

- `.devcontainer/` (configuração e os dois scripts), `docs/demonstracao.md`, ADR-0113, `CLAUDE.md` e o roadmap.
- `tests/FiscoProjects.Pipeline.Tests` (teste da configuração). Nenhuma mudança na aplicação nem na verificação do PR.
- Conta de Marcelo: o Codespace usa a cota gratuita mensal do GitHub (processamento e espaço) enquanto existir.
