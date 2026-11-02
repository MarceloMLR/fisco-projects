## Context

As Configurações gerais (canvas AdminConfiguracoes) nasceram com a Marca, as Regras gerais e a Prévia da marca. Depois ganharam a seção Arquivos, com a pasta dos projetos (ADR-0121), e o Microsoft 365, com a caixa de envio e o Teams (ADR-0123, ADR-0149). As cores foram para uma seção própria, Cores (ADR-0146), mas a marca ficou: o nome e os logos numa tela, as cores em outra, cada uma com a sua prévia. Tudo o que fica no formulário das Configurações gerais vai junto no `PUT /api/installation`, inclusive o nome e a pasta dos projetos.

A pasta dos projetos diz onde ficam as pastas dos projetos no armazenamento, e as Pastas padrão, que são do módulo `Documents`, dizem que pastas cada projeto ganha dentro da dele. A pasta dos projetos continua no `Administration` (`InstallationSettings.ProjectsFolder`): só a tela muda de lugar.

## Goals / Non-Goals

**Goals:**

- Cada configuração na tela onde ela é controlada, com uma gravação própria.
- Configurações gerais só com o que é geral: as Regras gerais e o Microsoft 365.
- Uma prévia só da marca, com o nome, os logos e as cores juntos.

**Non-Goals:**

- Mudar regras, validações, valores padrão ou a auditoria das configurações.
- Mudar a seção Microsoft 365, o seletor de cores, a prévia das cores ou a lista das pastas padrão.

## Decisions

### 1. As rotas por assunto

O `PUT /api/installation` (`UpdateInstallation`) fica só com as Regras gerais: o fuso, o horário do resumo, a guarda das notificações e da auditoria e as horas por dia. Saem dele o nome e a pasta dos projetos, que ganham rotas próprias, como as cores já têm (`PUT /api/installation/colors`, ADR-0146):

- `PUT /api/installation/name` (`RenameInstallation`, `{ name }`): 400 em `name` com o nome vazio ou acima de 60 caracteres;
- `PUT /api/installation/projects-folder` (`ChangeProjectsFolder`, `{ projectsFolder }`): 400 em `projectsFolder` com o caminho que o armazenamento não aceita.

As duas usam a política `SystemPolicies.Administrator`, como as outras rotas de gravação da instalação. A auditoria continua automática, pelos campos do `InstallationSettings`. A leitura não muda: o nome vem na marca (`GET /api/installation`) e a pasta dos projetos nas regras (`GET /api/installation/rules`).

*Alternativa:* manter o `PUT /api/installation` inteiro, com cada tela mandando também os valores que não mostra. Recusada, porque uma tela salva poderia desfazer o que outra acabou de gravar, e cada tela teria de conhecer os campos das outras.

### 2. A tela Visual

A `ColorsPage` vira a `VisualPage`, em `/admin/visual`, com o nome "Visual" na coluna da Administração e o mesmo ícone. A coluna da esquerda ganha, antes das cores, o cartão da Marca: o nome exibido, o logo e o logo para o tema escuro, com o `LogoField` que hoje está nas Configurações gerais (ele e o envio dos logos vão para um arquivo próprio, `BrandFields.tsx`). A prévia grande (`ColorsPreview`) já recebe o nome e os logos: passa a receber os digitados e escolhidos, no lugar dos salvos.

Salvar alterações grava, nesta ordem, só o que mudou: os logos (um arquivo recusado para tudo e mostra o motivo no campo), o nome e as cores. Descartar volta o nome, os logos e as cores. A rota `/admin/colors` passa a levar a `/admin/visual`, com `replace`.

A `BrandPreview`, a prévia pequena das Configurações gerais, sai: a prévia grande mostra o mesmo, com as cores que estão sendo escolhidas.

### 3. A pasta dos projetos em Pastas padrão

O `DefaultFoldersPage` ganha, entre o aviso do topo e a lista, uma linha com "Pasta dos projetos", onde elas ficam hoje (o caminho, ou "Na raiz do armazenamento") e "Alterar". "Alterar" abre o `ProjectsFolderDialog`, um `Dialog` com o campo, a ajuda de hoje e o motivo da recusa do servidor no campo. Salvar grava pela rota nova, avisa com "Pasta dos projetos salva." e pede de novo as regras e a auditoria.

### 4. As Configurações gerais

Ficam numa coluna, com a largura das seções de hoje: as Regras gerais, com Descartar e Salvar alterações, e o Microsoft 365, que grava na hora. O formulário perde o nome e a pasta dos projetos.

## Risks / Trade-offs

- **[Quem conhecia o lugar antigo da marca e da pasta]** → O endereço de Cores leva a Visual, e o guia de instalação e o CLAUDE.md passam a dizer o lugar novo.
- **[A rota `PUT /api/installation` muda de forma]** → Só a própria tela a usa; o contrato e os tipos da tela são gerados de novo, e os testes reprovam o contrato desatualizado.
- **[Duas gravações em Visual, a da marca e a das cores]** → Cada uma só quando algo dela mudou; uma falha avisa e deixa o que não foi gravado na tela, para tentar de novo.
