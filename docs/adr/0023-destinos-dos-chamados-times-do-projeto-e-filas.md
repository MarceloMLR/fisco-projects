---
status: aceita (filas, responsável na abertura e "Assumir chamado" substituídos pelo ADR-0068)
date: 2026-10-13
decision-makers: Marcelo Ribeiro
---

# ADR-0023: Chamados para o time funcional ou técnico do projeto, ou para filas compartilhadas, com responsável opcional

## Contexto e problema

No MVP, quem abre o chamado escolhe entre o time do projeto e a TI (pessoas com o papel global "Equipe de TI"). Pelo ADR-0003, "TI" não pode ser a única área de atendimento fixa no código. Marcelo quer que o solicitante escolha se o chamado é funcional ou técnico e, opcionalmente, a pessoa que vai atender (por exemplo, Lucas no funcional, Higor no técnico, ou o gerente do projeto). Além disso, há equipes compartilhadas, como a TI da empresa, que atendem todos os projetos sem fazer parte deles. Para onde vai um chamado e quem o atende?

## Fatores de decisão

- Nenhum chamado sem dono: todo destino precisa ter quem atenda.
- Nada específico de uma empresa fixo no código (ADR-0003).
- Equipes compartilhadas atendem todos os projetos sem precisar ser membros de cada um.
- Abertura simples, com a escolha da pessoa opcional.

## Opções consideradas

- Duas categorias fixas, como no MVP (time do projeto ou TI)
- Categorias livres, sem atendentes definidos
- Funcional como time do projeto e técnico como fila global
- Times funcional e técnico dentro de cada projeto, sem filas
- Times funcional e técnico dentro de cada projeto, mais filas compartilhadas opcionais

## Resultado da decisão

Opção escolhida: **times funcional e técnico dentro de cada projeto, mais filas compartilhadas opcionais**, porque atende tanto as pessoas dedicadas a um projeto quanto as equipes que servem a todos, e as filas são opcionais (sem elas, o sistema funciona só com os times do projeto).

Revisão de 17/10/2026 (ADR-0068): ficam só os times Funcional e Desenvolvimento (o "Técnico" passa a se chamar "Desenvolvimento"), sem filas; o chamado entra sem responsável, a primeira resposta de alguém do time o define (só quando ainda não há responsável) e não existe "Assumir chamado".

Escopo:

| Item | Classificação |
| --- | --- |
| Destinos na abertura: "Funcional" (time funcional do projeto), "Técnico" (time técnico do projeto) ou uma fila compartilhada | v1 (substituído pelo ADR-0068: só Funcional e Desenvolvimento) |
| Destino sem ninguém para atender não aparece na abertura (por exemplo, projeto sem time técnico ou fila sem atendentes) | v1 |
| Responsável opcional na abertura, escolhido entre quem atende o destino; o gerente do projeto sempre aparece na lista | v1 (substituído pelo ADR-0068: sem responsável na abertura) |
| Sem responsável, quem atende pode defini-lo depois ou usar "Assumir chamado" | v1 (substituído pelo ADR-0068: sem "Assumir"; só gestores definem ou trocam) |
| Atribuição automática: a primeira resposta de quem pode atender o destino (nunca o solicitante nem quem só acompanha o chamado) torna essa pessoa responsável | v1 (mantida no ADR-0068, só quando o chamado ainda não tem responsável) |
| Filas compartilhadas na Administração (opcionais): nome, descrição, atendentes e ativa ou inativa | v1 (substituído pelo ADR-0068: sem filas) |
| Atendente de fila vê os chamados da fila sem ser membro do projeto e sem acesso ao restante do projeto (documentos, acessos etc.) | v1 (substituído pelo ADR-0068: sem filas) |
| Instalação nova começa com a fila "TI", que pode ser renomeada ou excluída | v1 (substituído pelo ADR-0068: sem filas) |
| Transferir o chamado entre destinos (funcional, técnico, filas), com registro na linha do tempo | v1 (no ADR-0068, entre Funcional e Desenvolvimento, deixando o chamado sem responsável) |
| Lista geral de chamados filtrável por destino e por fila | v1 (no ADR-0068, filtrável por time) |
| Fila disponível só para alguns projetos | depois |
| Distribuição automática entre atendentes (rodízio) | depois |

Relação com decisões pendentes:

- A marcação de cada membro do projeto como do time funcional ou técnico (e se alguém pode ser dos dois) faz parte da decisão sobre membros do projeto.
- Um papel de liderança técnica (por exemplo, "gerente de desenvolvimento") e a provável substituição do papel global "Equipe de TI" pelas filas serão decididos na revisão de papéis.

### Consequências

- Bom, porque todo chamado vai para um destino com quem atenda.
- Bom, porque equipes compartilhadas atendem todos os projetos sem acesso ao restante do conteúdo deles.
- Bom, porque a atribuição automática reduz chamados sem responsável.
- Ruim, porque a abertura ganha uma escolha a mais (destino e, opcionalmente, pessoa).
- Ruim, porque as filas trazem regras de acesso próprias, além das do projeto.

### Confirmação

Testes cobrindo: a abertura oferece só destinos com quem atenda; a lista de responsáveis mostra apenas quem atende o destino, mais o gerente do projeto; a primeira resposta de quem atende define o responsável, e a do solicitante ou de quem só acompanha não define; a transferência entre destinos fica na linha do tempo; o atendente de uma fila vê os chamados da fila, mas não os documentos nem os acessos do projeto.

## Prós e contras das opções

### Duas categorias fixas, como no MVP

- Bom, porque é simples e já foi validado.
- Ruim, porque fixa "TI" no código, contrariando o ADR-0003.

### Categorias livres, sem atendentes

- Bom, porque é flexível.
- Ruim, porque ninguém é dono da fila e os chamados se perdem.

### Funcional no projeto, técnico como fila global

- Bom, porque mantém a fila única da TI.
- Ruim, porque não atende projetos com técnicos dedicados.

### Times funcional e técnico no projeto, sem filas

- Bom, porque reflete as pessoas dedicadas a cada projeto.
- Ruim, porque equipes compartilhadas precisariam ser membros de todos os projetos e veriam conteúdo de projetos em que não trabalham.

### Times no projeto mais filas compartilhadas opcionais

- Bom, porque cobre os dois casos e as filas são opcionais.
- Ruim, porque tem mais regras de destino e de acesso.
