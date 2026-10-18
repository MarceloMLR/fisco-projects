---
status: aceita
date: 2026-10-13
decision-makers: Marcelo Ribeiro
---

# ADR-0018: Cronograma em dias úteis, com feriados nacionais calculados e feriados locais cadastrados

## Contexto e problema

Os modelos guardam durações (ADR-0017) e as datas do projeto vêm do cronograma (ADR-0016). Falta definir como uma duração vira data: se fins de semana e feriados contam ou não, e de onde vêm os feriados.

## Fatores de decisão

- Datas realistas, num país com muitos feriados.
- Nada específico de uma empresa fixo no código (ADR-0003): feriados locais e dias de trabalho variam por empresa.
- Não depender de serviços externos nem de licenças pagas para funcionar.
- Reaproveitar o mesmo calendário em outros módulos (por exemplo, prazos de chamados, a decidir no bloco de chamados).

## Opções consideradas

- A. Dias corridos
- B. Dias úteis (segunda a sexta), sem feriados
- C. Dias úteis com feriados

Para a origem dos feriados nacionais (opção C):

- C1. Calculados pelo próprio sistema (datas fixas e datas móveis baseadas na Páscoa)
- C2. Obtidos de uma API pública (por exemplo, BrasilAPI)
- C3. Biblioteca pronta (por exemplo, Nager.Date)

## Resultado da decisão

Opção escolhida: **C. Dias úteis com feriados**, com os **feriados nacionais calculados pelo próprio sistema (C1)**, porque as datas ficam realistas sem depender de internet, de projetos de terceiros ou de licença paga.

Escopo:

| Item | Classificação |
| --- | --- |
| Durações em dias inteiros, sem horas | v1 |
| Dias de trabalho configuráveis na instalação, padrão segunda a sexta | v1 |
| Feriados nacionais calculados automaticamente, considerados na criação do cronograma e em todo cálculo de datas | v1 |
| Pontos facultativos (Carnaval, Corpus Christi etc.): a empresa escolhe se considera cada um | v1 |
| Cadastro de feriados estaduais, municipais e da empresa (data e nome), com opção "repete todo ano"; o administrador pode desconsiderar um feriado | v1 |
| Fins de semana e feriados destacados no Gantt | v1 |
| Carga automática de feriados estaduais e municipais | fora (sem fonte gratuita confiável) |
| Calendário próprio por projeto (por exemplo, feriados da cidade de um cliente) | depois |
| Férias e ausências de cada pessoa | fora |

Regra: o cálculo dos feriados nacionais fica atrás de uma interface (como no ADR-0004); outro país no futuro é uma nova implementação.

### Consequências

- Bom, porque datas de tarefas, marcos e projeto caem em dias de trabalho reais.
- Bom, porque funciona sem internet e é fácil de testar.
- Bom, porque o mesmo calendário pode servir a outros módulos.
- Ruim, porque um novo feriado nacional criado por lei exige nova versão do sistema.
- Ruim, porque feriados estaduais e municipais dependem de cadastro manual.

### Confirmação

Testes cobrindo: soma de dias úteis pulando fins de semana e feriados; feriados móveis corretos para vários anos (Páscoa); ponto facultativo só conta quando a empresa o considera; feriado cadastrado com "repete todo ano" vale nos anos seguintes; feriado desconsiderado deixa de afetar as datas; dias de trabalho configurados mudam o cálculo.

## Prós e contras das opções

### A. Dias corridos

- Bom, porque é o cálculo mais simples.
- Ruim, porque tarefas e marcos caem em fins de semana e feriados.

### B. Dias úteis sem feriados

- Bom, porque reflete a semana de trabalho com cálculo simples.
- Ruim, porque entregas podem cair em feriados sem aviso.

### C. Dias úteis com feriados

- Bom, porque as datas ficam realistas.
- Ruim, porque exige manter feriados locais.

### C1. Feriados nacionais calculados pelo sistema

- Bom, porque não depende de terceiros nem de internet.
- Ruim, porque um feriado novo exige nova versão.

### C2. API pública

- Bom, porque as regras ficam com terceiros.
- Ruim, porque é um projeto comunitário, não oficial, e sem internet não há feriados.

### C3. Biblioteca pronta

- Ruim, porque a Nager.Date exige chave de licença (patrocínio) e não cobre feriados de cidades.

## Mais informações

- Pela Portaria MGI nº 11.460/2025 (feriados de 2026), Carnaval e Corpus Christi são pontos facultativos, e feriados estaduais e municipais valem só nas respectivas localidades.
- Nager.Date: https://github.com/nager/Nager.Date
