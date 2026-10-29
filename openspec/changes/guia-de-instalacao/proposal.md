## Why

O `docs/instalacao.md` é uma lista de requisitos e os passos da instalação de Marcelo, feita a partir do repositório e publicada pelo pipeline dele. Para instalar o FiscoProjects na nuvem de um cliente, falta o caminho completo: quem instala, com que acesso, de onde vem a versão e como atualizar. Em 29/10/2026, Marcelo escolheu que a instalação parta de uma versão publicada, não de uma cópia do repositório, feita pelo fornecedor na assinatura do cliente (o padrão) ou pela TI do cliente (Operação-6). O ADR-0127 registra a escolha.

## What Changes

- Cada versão publicada no GitHub (`vX.Y.Z`) ganha o pacote `fiscoprojects-vX.Y.Z.zip`, montado e anexado pelo workflow `release`: a aplicação (`app.zip`), o Terraform sem os parâmetros dos ambientes de Marcelo, os scripts de `infra/scripts` e os guias de instalação, de restauração e de custos, só com o que está no Git.
- O script `infra/scripts/Publish-Version.ps1` publica a versão do pacote numa instalação atualizada à mão: aplica as migrações pelo comando `migrate` do pacote, com a conta do Azure CLI e só com ela, abrindo o firewall do banco só para o endereço de quem roda e só durante as migrações, e publica o `app.zip`, esperando o `/health`.
- O `docs/instalacao.md` vira o guia de instalação: o que o cliente providencia, quem instala e o acesso de cada um, o pacote da versão e os arquivos privados da instalação, a instalação passo a passo até a primeira entrada do administrador, a atualização para uma versão nova e a volta a uma versão anterior, com os requisitos de cada parte como referência.

## Capabilities

### New Capabilities

### Modified Capabilities
- `publicacao`: o pacote de cada versão e a publicação de uma versão à mão.
- `infraestrutura`: o guia de instalação a partir do pacote da versão.

## Non-goals

- Publicar instalações de clientes pelo pipeline deste repositório: com o repositório público, os nomes das instalações e os parâmetros delas apareceriam nos logs.
- Oferecer o FiscoProjects pelo Azure Marketplace (Azure Managed Application).
- Mudar a publicação da instalação de Marcelo, que continua pelo `deploy-dev` e pelo `deploy-prod`.
- Tornar o repositório privado, que é decisão de Marcelo.
- Controle de licença dentro da aplicação; o uso é regido pelo contrato.

## Impact

- `.github/workflows/release.yml`, `scripts/cd/release.sh`, `infra/scripts/Publish-Version.ps1`.
- `tests/FiscoProjects.Pipeline.Tests` (pacote, publicação à mão e guia).
- `docs/instalacao.md`, `docs/adr/0127-instalacao-a-partir-do-pacote-da-versao.md`, `docs/roadmap.md`, `docs/glossario.md`, `CLAUDE.md`.
- ADRs: 0002, 0003, 0030, 0036, 0053, 0055, 0114, 0115, 0116, 0125, 0127.
