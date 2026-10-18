---
status: aceita (atendentes de fila substituídos pelo ADR-0068; gerente na criação ajustado pelo ADR-0072)
date: 2026-10-15
decision-makers: Marcelo Ribeiro
---

# ADR-0040: Papéis no sistema e papéis em cada projeto

## Contexto e problema

O ADR-0006 decidiu que todos os papéis ficam no banco da aplicação e deixou o catálogo para depois dos módulos. O ideia.md listava papéis globais (Administrador, Gestor de projetos, Equipe de TI, Colaborador) e por projeto (gestor, membro, leitor). Depois dos módulos, ficaram pendentes: a marcação dos membros como time funcional ou técnico (ADR-0023), um papel de liderança técnica e a necessidade do papel "Equipe de TI" diante das filas. Marcelo propôs um modelo com Administrador, Gerente de projeto, Líder técnico, Colaborador e Desenvolvedor. Quais papéis existem, e em que nível valem?

## Fatores de decisão

- Refletir os cargos reais de um projeto (gerente, liderança técnica, funcional, técnico).
- Permitir que a mesma pessoa tenha papéis diferentes em projetos diferentes.
- Integrar os times funcional e técnico dos chamados (ADR-0023) sem marcação separada.
- Não duplicar o que as filas já resolvem.

## Opções consideradas

- A. Três papéis globais (Administrador, Gestor de projetos, Colaborador) e papéis por projeto do ideia.md (gestor, membro, leitor)
- B. Os quatro papéis globais do ideia.md, mantendo a Equipe de TI
- C. Dois papéis globais (Administrador e Usuário), com qualquer pessoa criando projetos
- D. Modelo de Marcelo: papéis no sistema (Administrador, Gerente de projeto, Usuário) e papéis por projeto (Gerente de projeto, Líder técnico, Colaborador, Desenvolvedor, Leitor)

## Resultado da decisão

Opção escolhida: **D. Papéis no sistema e papéis por projeto, no modelo de Marcelo**, porque reflete os cargos reais, liga os papéis aos times funcional e técnico e permite papéis diferentes em cada projeto.

Papéis no sistema:

| Papel | Pode |
| --- | --- |
| Administrador | Tudo, inclusive a Administração (usuários, auditoria, configurações e cadastros). Vê e age em todos os projetos. Único que exclui projetos (ADR-0041) |
| Gerente de projeto | Criar projetos; quem cria vira gerente do projeto criado (pelo ADR-0072, é o padrão: na criação, pode escolher outro gerente e o líder técnico). Não exclui projetos |
| Usuário | Papel padrão no primeiro login (ADR-0006). Nada além do que o papel em cada projeto permite |

Papéis em cada projeto:

| Papel | Pode |
| --- | --- |
| Gerente de projeto | Cronograma, termos de aceite, aprovar documentos, gerenciar as pessoas do projeto, ver e atuar em todos os chamados do projeto |
| Líder técnico | O mesmo que o gerente, menos alterar o cronograma (só vê) e os termos de aceite. Faz parte do time técnico |
| Colaborador | Time funcional: atende os chamados funcionais, abre chamados, atualiza as próprias tarefas e envia documentos |
| Desenvolvedor | Time técnico: o mesmo que o colaborador, atendendo os chamados técnicos |
| Leitor | Acompanha o projeto, abre e acompanha os próprios chamados e responde aos termos de aceite endereçados a ele; não atende chamados |

Regras:

- Os papéis de projeto valem por projeto: a mesma pessoa pode ser líder técnico num projeto e desenvolvedor em outro.
- O time funcional do projeto (ADR-0023) é formado pelos colaboradores; o time técnico, pelos líderes técnicos e desenvolvedores. O gerente do projeto sempre aparece na lista de responsáveis (ADR-0023). Com o ADR-0068, o time técnico passa a se chamar Desenvolvimento.
- "Todos os chamados" do gerente e do líder técnico são os chamados dos projetos em que têm esse papel; o administrador vê os de todos os projetos.
- O papel "Equipe de TI" deixa de existir: quem atende numa fila vem do cadastro da fila (ADR-0023). Substituído pelo ADR-0068: não há filas na v1; o papel "Equipe de TI" continua fora.
- O que cada papel pode fazer em cada módulo será detalhado na matriz de permissões, em ADR próprio.

Escopo:

| Item | Classificação |
| --- | --- |
| Papéis no sistema e papéis por projeto descritos acima | v1 |
| Papel "Equipe de TI" | fora (substituído pelas filas; sem filas na v1, ADR-0068) |
| Papel de leitura de todos os projetos, sem editar (por exemplo, para diretoria) | depois |

### Consequências

- Bom, porque os papéis refletem os cargos de um projeto e definem os times funcional e técnico sem marcação extra.
- Bom, porque a mesma pessoa pode ter papéis diferentes em cada projeto.
- Ruim, porque a matriz de permissões fica maior, com cinco papéis de projeto.
- Neutro, porque o nome "Gerente de projeto" aparece nos dois níveis: no sistema, dá o poder de criar projetos; no projeto, o de gerenciá-lo.

### Confirmação

Testes cobrindo: o Usuário não cria projetos e o Gerente de projeto cria e vira gerente do projeto criado; os colaboradores formam o time funcional e os líderes técnicos e desenvolvedores, o técnico; o líder técnico não altera o cronograma nem os termos de aceite; o leitor não atende chamados; gerente e líder técnico veem todos os chamados dos seus projetos, e não os de outros; o administrador vê todos.

## Prós e contras das opções

### A. Três papéis globais e gestor, membro e leitor por projeto

- Bom, porque é simples.
- Ruim, porque não distingue liderança técnica nem os times funcional e técnico.

### B. Quatro papéis globais do ideia.md

- Bom, porque já foi validado no MVP.
- Ruim, porque a Equipe de TI duplica as filas.

### C. Dois papéis globais

- Bom, porque é o mais simples.
- Ruim, porque qualquer pessoa cria projetos.

### D. Modelo de Marcelo

- Bom, porque reflete os cargos reais e integra os times dos chamados.
- Ruim, porque tem mais papéis para testar.
