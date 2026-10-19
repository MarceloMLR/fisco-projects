## MODIFIED Requirements

### Requirement: Cabeçalho do projeto

A página do projeto MUST ter o cabeçalho do canvas ProjetoCabecalho: o logo do cliente ou as iniciais, o nome, o status, o código, o cliente, o gerente, o líder técnico e as datas do cronograma, ou "Sem cronograma" sem tarefas nem marcos, e as abas Visão geral, Cronograma e Membros, esta com o número de pessoas. As abas MUST NOT ter barra de rolagem; só rolam para o lado quando não cabem na largura, como no celular. A aba Visão geral MUST mostrar a descrição e a equipe por time: Gestão, Funcional e Desenvolvimento (ADR-0016, ADR-0066, ADR-0071, ADR-0072).

#### Scenario: Cabeçalho do projeto
- **WHEN** uma pessoa do projeto abre a página do projeto
- **THEN** o cabeçalho mostra o nome, o status, o código, o cliente, o gerente e o líder técnico, e a trilha mostra Projetos e o código

#### Scenario: Abas do projeto
- **WHEN** uma pessoa do projeto escolhe a aba Membros no cabeçalho
- **THEN** a aba Membros fica marcada, mostra o número de pessoas e abre as pessoas do projeto, e Visão geral volta à descrição e à equipe

#### Scenario: Abas sem barra de rolagem
- **WHEN** uma pessoa abre a página de um projeto no computador
- **THEN** a linha das abas não rola na vertical e não mostra barra de rolagem, e o traço da aba escolhida fica sobre a linha do cabeçalho

#### Scenario: Datas do cronograma no cabeçalho
- **WHEN** o cronograma do projeto vai de 05/10/2026 a 13/11/2026
- **THEN** o cabeçalho mostra "05/10 – 13/11/2026" no lugar de "Sem cronograma"
