# Design: modelos de projeto (Cronograma-8)

## Context

O ADR-0017 escolheu o "Salvar como modelo" a partir do cronograma de um projeto, com a lista de modelos na Administração e a criação do projeto a partir de um modelo e de uma data de início. O canvas `NovoProjeto` já desenhou:
- a seção "Cronograma" com "Em branco" ou "A partir de um modelo";
- o modelo e o início;
- a prévia com a faixa de fases e a lista de fases e itens.

A tela de cadastro dos modelos na Administração não foi desenhada (a change tem * no roadmap).

## Goals / Non-Goals

**Goals:**

- Salvar a estrutura de um cronograma e gerar outro igual em outra data.
- Administrar os modelos sem um editor próprio.

**Non-Goals:**

- Editor de modelos, pastas e links nos modelos (ADR-0017).

## Decisions

### 1. O modelo guarda a distância em dias úteis

- **`ProjectTemplate`** (agregado do módulo `Projects`): o nome (até 150 caracteres, único), a descrição (até 500) e a situação.
- **`ProjectTemplatePhase`:** o nome e a ordem.
- **`ProjectTemplateItem`:**
  - a fase, a tarefa em que está (`ParentId`), o tipo, o nome, a duração e a ordem;
  - a distância (`StartOffset`), em dias úteis, do primeiro início do cronograma ao início do item.
- **`ProjectTemplateDependency`:** o item anterior e o seguinte.
- Sem datas nem responsáveis (ADR-0017).
- Só com a duração e as dependências, os itens sem dependências começariam todos no início do projeto e perderiam a forma do cronograma, como "Treinar usuários" no meio da fase. A distância em dias úteis não é uma data, e a geração continua respeitando as dependências.
- **Alternativa:** só duração e dependências. Recusada pela perda da forma.

### 2. Gerar o cronograma

- `Schedule.ApplyTemplate(template, start, days, today)`:
  - inclui as fases na ordem;
  - inclui os itens na ordem do modelo (por distância, as tarefas antes das subtarefas delas), com o início `AddBusinessDays(início em dia útil, distância)` e as dependências já incluídas;
  - o domínio põe cada item no primeiro dia útil possível e recalcula as tarefas-resumo, como em qualquer inclusão (ADR-0019, ADR-0088).
- O `CreateProject` recebe `templateId` e `scheduleStart`:
  - com o modelo, o início é obrigatório, e o modelo precisa existir e estar ativo (400 em `templateId` ou `scheduleStart`);
  - o cronograma é gerado na mesma transação do projeto;
  - sem o modelo ("Em branco"), o projeto nasce sem cronograma.

### 3. Rotas e permissões

| Rota | Quem |
| --- | --- |
| `POST /api/projects/{id}/schedule/template` | administrador e gerente do projeto (`CanEditSchedule`, ADR-0043); vale com o projeto encerrado |
| `GET /api/project-templates` (ativos, com a estrutura para a prévia) | quem cria projetos (`ProjectCreators`) |
| `GET /api/admin/project-templates` (todos) | administrador (ADR-0046) |
| `PUT /api/admin/project-templates/{id}` (nome, descrição, situação) | administrador |
| `DELETE /api/admin/project-templates/{id}` | administrador |

- **Salvar como modelo:** responde 201 com o id; recusa o nome vazio ou longo (400 `name`), o cronograma sem itens (400 `schedule`) e o nome repetido (409 `template-name-in-use`).
- **Alterar:** o mesmo nome repetido também dá 409.
- **Projeto criado a partir de um modelo:** não passa pelo desfazer (ADR-0091), porque não é uma mudança do cronograma depois de criado.

### 4. Tela

- **"⋮" da aba Cronograma:** "Salvar como modelo" para quem edita, num diálogo com nome e descrição (React Hook Form e Zod). O aviso é "Modelo X salvo.".
- **"Novo projeto":**
  - a seção "Cronograma" usa as opções do canvas (`fp-opts`: "Em branco" e "A partir de um modelo");
  - o "Modelo" é um `Select` com os modelos ativos, e o "Início" é um campo de data;
  - sem modelos ativos, "A partir de um modelo" fica desligada, com a explicação;
  - a prévia ao lado mostra o nome do modelo, a faixa de fases (`ScheduleStrip`, com as posições pela distância em dias úteis), "N tarefas · M marcos" e a lista de fases com os itens;
  - a prévia não tem datas, porque elas saem do servidor (ADR-0051).
- **"Modelos de projeto" na Administração:**
  - fica no grupo "Cadastros", com a contagem;
  - é desenhada como a tela de Clientes, com a tabela, o menu da linha (Editar, Inativar ou Reativar, Excluir), o diálogo de edição e a confirmação da exclusão;
  - o canvas ainda não tem essa tela, e Marcelo confere no PR.

### 5. Auditoria

- `ProjectTemplate` é auditado pelo nome, com o nome, a descrição e a situação.
- As fases, os itens e as dependências do modelo ficam fora (`ExcludeFromAudit`).
- A tela de Auditoria ganha o tipo "Modelos de projeto".

## Risks / Trade-offs

- **[Calendário diferente]:** com outros feriados, as datas geradas mudam um pouco. → É o esperado: a distância é em dias úteis do calendário da instalação no momento da criação.
- **[Tela sem desenho]:** a tela da Administração segue a de Clientes. → Marcelo confere no PR, e um ajuste vira uma change de correção.
