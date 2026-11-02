## Why

As Configurações gerais juntaram, com o tempo, a marca da instalação, as regras gerais, a pasta dos projetos e o Microsoft 365, com a prévia da marca ao lado e uma barra de Salvar que vale só para uma parte delas. Em 02/11/2026, ao testar os avisos pelo Teams, Marcelo achou a página confusa e pediu que cada configuração fique onde é controlada: a marca junto com as cores, numa seção Visual, e a pasta dos projetos em Pastas padrão, que já cuida das pastas. As Configurações gerais ficam só com o que é geral, as regras e o Microsoft 365 (ADR-0150). Acesso-8.6, fora do roadmap aprovado: Marcelo aprovou a proposta antes da implementação.

## What Changes

- **Configurações gerais:** só as Regras gerais (fuso, horário do resumo diário, guarda das notificações e da auditoria, horas por dia de trabalho) e o Microsoft 365 (caixa de envio e Teams), numa coluna, sem a prévia da marca.
  - Descartar e Salvar alterações continuam valendo só para as regras; o Microsoft 365 continua gravando na hora.
- **Visual, no lugar de Cores:** a seção da Administração passa a se chamar Visual e ganha a Marca.
  - A Marca vem primeiro, com o nome exibido, o logo e o logo para o tema escuro; as três cores vêm depois.
  - A prévia mostra o nome, os logos e as cores na hora, antes de salvar.
  - Uma barra só salva tudo o que mudou, e Descartar volta tudo.
  - O endereço antigo de Cores leva para Visual.
- **Pasta dos projetos em Pastas padrão:** uma linha acima da lista diz onde ficam as pastas dos projetos novos, com "Alterar", que abre um diálogo com o campo, a ajuda de hoje e o motivo da recusa.
- **API:** cada tela grava só o que mostra.
  - `PUT /api/installation` fica só com as regras gerais.
  - `PUT /api/installation/name` troca o nome exibido.
  - `PUT /api/installation/projects-folder` troca a pasta dos projetos.
  - A auditoria registra cada mudança como hoje.
- **Documentação:** o ADR-0150, que substitui a tela do ADR-0146 e o lugar da pasta dos projetos do ADR-0121, e o passo de configuração do guia de instalação.

## Capabilities

### New Capabilities

Nenhuma.

### Modified Capabilities

- `configuracao-da-instalacao`: as Configurações gerais só com as regras e o Microsoft 365, a seção Visual com a marca e as cores, a pasta dos projetos em Pastas padrão e as rotas de gravação separadas.

## Non-goals

- Mudar alguma regra, validação ou valor padrão das configurações.
- Mudar a seção Microsoft 365, o seletor de cores ou a prévia das cores.
- Mudar a lista, o diálogo ou "Quem vê" das pastas padrão.
- Mover o Calendário e feriados ou os Prazos dos chamados.
- Dados de demonstração novos: nada novo para mostrar.

## Impact

- **Backend (`Administration`):** o `UpdateInstallation` só com as regras gerais; os casos de uso `RenameInstallation` e `ChangeProjectsFolder`, com as rotas novas; os testes da API.
- **Frontend:** a `SettingsPage` sem a Marca, a Prévia e os Arquivos; a `VisualPage` no lugar da `ColorsPage`, em `/admin/visual`, com a Marca; a pasta dos projetos no `DefaultFoldersPage`, com o `ProjectsFolderDialog`; os textos e os testes de tela.
- **Contrato:** `web/src/api/openapi.json` e `schema.ts` gerados de novo.
- **Docs:** ADR-0150, os ADR-0121 e 0146 apontando para ele, `docs/instalacao.md`, `CLAUDE.md`, `docs/glossario.md` e o roadmap.
