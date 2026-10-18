# Proposta: armazenamento de arquivos (Acesso-4)

## Why

Anexos dos chamados, documentos com versões, termos assinados e logos dos clientes precisam de um lugar para os arquivos. O ADR-0030 coloca tudo no SharePoint, atrás de uma interface com uma implementação local para desenvolvimento e testes (ADR-0004). Ainda não há tenant (ADR-0078), então os módulos dos grupos 3 a 7 precisam do contrato e da implementação local agora; o SharePoint entra no grupo Microsoft 365, sem mudar quem usa.

## What Changes

- Novo módulo `Files`, sem tabelas, com o contrato `IFileStorage` em `FiscoProjects.Files.Contracts` (ADR-0079, novo):
  - garantir uma pasta, criando as que faltam;
  - listar subpastas;
  - enviar um arquivo;
  - abrir a versão atual ou uma anterior;
  - excluir um arquivo com as versões;
  - excluir uma pasta só se estiver vazia (ADR-0041).
- Mesmo nome na mesma pasta: quem envia escolhe entre nova versão, com a anterior guardada (ADR-0031), e manter os dois, com outro nome.
- Nomes ajustados às regras do SharePoint e caminhos sempre dentro da raiz configurada.
- Implementação local em disco, com versões, escolhida em `FileStorage:Provider`, na pasta `FileStorage:Local:RootPath`. A aplicação não sobe em produção com ela.
- Testes de contrato que qualquer implementação de `IFileStorage` deve passar, para a do SharePoint reaproveitar depois.

## Non-goals

- Implementação no SharePoint pelo Microsoft Graph: grupo Microsoft 365 (M365), quando houver tenant.
- Estrutura padrão de pastas do projeto, ligação do projeto a uma pasta e registro dos arquivos no banco: changes de Projetos, Chamados e Documentos (ADR-0030, ADR-0031).
- Limite de tamanho e tipos de arquivo recusados: regras de cada uso (anexos, ADR-0026; logos, ADR-0015; documentos, ADR-0031), nas changes desses módulos.
- Rotas de envio e download e telas: chegam com os módulos que usam arquivos, com as permissões de cada um.

## Capabilities

### New Capabilities

- `armazenamento-de-arquivos`: como os módulos guardam, versionam, abrem e excluem arquivos e pastas atrás de um contrato único, com nomes e caminhos seguros e a implementação local fora da produção.

### Modified Capabilities

Nenhuma.

## Impact

- Código novo: `src/Modules/Files` (contrato, regras de nomes e caminhos, implementação local e registro) e `tests/FiscoProjects.Files.Tests` (testes de contrato e da implementação local).
- Código alterado: host da API (registro do módulo), `appsettings.Development.json` (pasta local), `.gitignore` (pasta local dos arquivos).
- Dependências novas: nenhuma.
- Documentação: ADR-0079 (proposta, aceito no arquivamento), guia de instalação (configuração dos arquivos), glossário e `CLAUDE.md`.
- ADRs implementados: 0004 (arquivos atrás de interface, implementação local) e 0030 (local configurado, pastas, só o que o sistema criou); apoio do 0026, 0031, 0035, 0041 e 0015.
