# Proposta: modelos de projeto (Cronograma-8)

## Why

Cada empresa repete a mesma estrutura de cronograma em projetos parecidos, como uma implantação de ERP ou uma revisão tributária de filial, e hoje monta tudo à mão a cada projeto. O ADR-0017 decidiu os modelos de projeto a partir de um cronograma ("Salvar como modelo"):
- o modelo guarda fases, tarefas, marcos, dependências e durações, sem datas nem responsáveis;
- a Administração lista os modelos, para renomear, descrever, inativar e excluir;
- o "Novo projeto" cria o cronograma a partir de um modelo e de uma data de início.

O ADR-0043 deixou salvar como modelo para o administrador e o gerente, e o ADR-0046 deixou a lista de modelos só para o administrador.

## What Changes

- **Salvar como modelo** (`POST /api/projects/{id}/schedule/template`), no menu "⋮" da aba Cronograma, para o administrador e o gerente (ADR-0043):
  - o modelo guarda o nome, a descrição, as fases, as tarefas, as subtarefas, os marcos, as dependências e as durações, sem datas nem responsáveis;
  - cada item guarda a distância, em dias úteis, entre o início do cronograma e o início dele, para o modelo manter a forma do cronograma;
  - vale também com o projeto encerrado, o caso mais comum de origem de um modelo;
  - recusa um cronograma sem tarefas nem marcos (400) e um nome que outro modelo já tem (409).
- **Novo projeto a partir de um modelo:**
  - a seção "Cronograma" do "Novo projeto" oferece "Em branco" ou "A partir de um modelo", como no canvas;
  - escolhido o modelo, a pessoa diz a data de início, e a prévia mostra as fases e os itens;
  - ao criar o projeto, o servidor gera o cronograma, com as datas em dias úteis do calendário da instalação (ADR-0018, ADR-0051);
  - modelo inativo ou que não existe: 400.
- **Modelos de projeto na Administração** (`/admin/templates`), só do administrador (ADR-0046):
  - a lista mostra o nome, a descrição, as fases e os itens e a situação;
  - pelo menu da linha, o administrador edita o nome e a descrição, inativa ou reativa, e exclui com confirmação;
  - o modelo inativo não aparece no "Novo projeto".
- **Auditoria:**
  - salvar, alterar e excluir um modelo entram na auditoria como "Modelos de projeto", pelo nome;
  - os itens do modelo ficam fora, para salvar um modelo não gerar um registro por tarefa.
- **Dados de demonstração:** a gerente salva o ERP-FIN como "Implantação de ERP" e o TRIB-RS como "Revisão tributária de filial" (ADR-0017, ADR-0089).

## Non-goals

- Editor próprio de modelos, e modelos com pastas de documentos e links (ADR-0017, "depois" e "a decidir").
- Datas no modelo: só a distância em dias úteis.
- Mudar o modelo depois de salvo, além do nome, da descrição e da situação: para mudar a estrutura, cria-se um projeto a partir dele, ajusta-se o cronograma e salva-se de novo (ADR-0017).

## Capabilities

### New Capabilities

- `modelos-de-projeto`: salvar como modelo, criar projeto a partir de um modelo, a lista na Administração e a auditoria.

### Modified Capabilities

- `dados-de-demonstracao`: os dois modelos de exemplo.

## Impact

- **Backend (`Projects`):**
  - `ProjectTemplate` com as fases, os itens e as dependências, em quatro tabelas, e a migração;
  - `Schedule.ApplyTemplate`;
  - os casos de uso de salvar, listar, alterar e excluir modelos;
  - o `CreateProject` com o modelo e o início;
  - as rotas e as permissões.
- **Frontend:**
  - "Salvar como modelo" no "⋮";
  - a seção "Cronograma" do "Novo projeto", com a prévia;
  - a tela "Modelos de projeto" na Administração, desenhada como a tela de Clientes, porque a tela de cadastro ainda não foi desenhada (a change tem *);
  - o tipo na Auditoria.
- **Docs:** `CLAUDE.md`, glossário e roadmap.
