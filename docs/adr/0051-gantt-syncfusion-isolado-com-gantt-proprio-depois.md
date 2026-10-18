---
status: aceita
date: 2026-10-15
decision-makers: Marcelo Ribeiro
---

# ADR-0051: Gantt com Syncfusion na v1, isolado num componente próprio, com Gantt próprio como evolução

## Contexto e problema

O cronograma precisa do que foi decidido nos ADR-0018, ADR-0019 e ADR-0021: arrastar e redimensionar barras, tabela ao lado, zoom, dependências, linha de base, caminho crítico, desfazer, calendário com dias úteis e exportação para o MS Project. O frontend é React (ADR-0049) com MUI (ADR-0050). Marcelo, como pessoa física num projeto pessoal (ADR-0002), se enquadra na licença comunitária do Syncfusion, e a empresa em vista tem faturamento abaixo do limite dessa licença. Qual biblioteca de Gantt usar?

## Fatores de decisão

- Cobrir os requisitos do cronograma na v1.
- Custo de licença agora e para quem adquirir o código.
- Facilidade de trocar o componente no futuro.
- Não duplicar regras de cálculo de datas entre servidor e tela.

## Opções consideradas

- A. SVAR React Gantt (núcleo MIT e edição PRO comercial)
- B. Bryntum Gantt (comercial)
- C. DHTMLX Gantt (comercial para recursos avançados)
- D. Syncfusion Gantt pela licença comunitária na v1, com Gantt próprio como evolução
- E. Construir o próprio Gantt desde o início

## Resultado da decisão

Opção escolhida: **D. Syncfusion na v1, isolado num componente próprio, com Gantt próprio como evolução**, por decisão de Marcelo: não tem custo agora, cobre os requisitos e a troca futura fica restrita a um componente.

Regras:

- O Gantt da v1 usa o Syncfusion React Gantt, sob a licença comunitária de Marcelo.
- Critérios da licença comunitária: faturamento anual abaixo de US$ 1 milhão, até 5 desenvolvedores, até 10 funcionários no total e nunca ter recebido mais de US$ 3 milhões de investimento externo. As licenças não são transferíveis: quem adquirir o código precisa da própria licença (comunitária, se se enquadrar, ou paga).
- A chave de licença do Syncfusion fica fora do repositório, na configuração ou nos segredos do ambiente.
- Isolamento: o restante do sistema usa apenas o componente próprio `CronogramaGantt`, com propriedades e eventos definidos pelo FiscoProjects; só esse componente importa pacotes do Syncfusion.
- O servidor é a fonte da verdade das datas: dias úteis, feriados, dependências empurradas e caminho crítico são calculados no backend (ADR-0018, ADR-0019); o Gantt exibe e envia as alterações.
- A exportação no formato XML do MS Project é gerada preferencialmente no servidor, para não depender do componente.
- O visual do Gantt segue o tema do MUI (tokens do design system), usando um tema do Syncfusion no estilo Material como base.
- O Gantt próprio será uma mudança própria no OpenSpec, depois da v1 ou antes de vender o código a quem não se enquadre na licença comunitária.

### Consequências

- Bom, porque não há custo de licença na fase atual.
- Bom, porque a troca futura afeta só o `CronogramaGantt`.
- Bom, porque o cálculo de datas no servidor evita dois motores de agendamento discordando.
- Ruim, porque cada comprador precisa da própria licença do Syncfusion enquanto o Gantt próprio não existir.
- Ruim, porque os pacotes do Syncfusion são grandes e os termos da licença comunitária podem mudar.

### Confirmação

Verificação no CI (lint ou teste de arquitetura): pacotes `@syncfusion/*` são importados apenas pelo componente `CronogramaGantt`; nenhuma chave de licença no repositório; as datas exibidas no Gantt vêm sempre do servidor.

## Prós e contras das opções

### A. SVAR React Gantt

- Bom, porque é feito para React e a edição PRO cobre todos os requisitos a partir de US$ 524 por desenvolvedor.
- Ruim, porque exige licença paga para os recursos avançados e a licença de desenvolvedor não permite SaaS hospedado.

### B. Bryntum Gantt

- Bom, porque é o mais completo.
- Ruim, porque custa cerca de US$ 940 por desenvolvedor e aplicações vendidas pedem licença OEM.

### C. DHTMLX Gantt

- Bom, porque é maduro.
- Ruim, porque caminho crítico e linha de base só existem na licença comercial.

### D. Syncfusion agora, Gantt próprio depois

- Bom, porque é gratuito na fase atual e completo.
- Ruim, porque compradores fora dos critérios pagam licença até existir o Gantt próprio.

### E. Gantt próprio desde o início

- Bom, porque não tem licença e segue o design system.
- Ruim, porque atrasa a v1 com a tela mais complexa do sistema.
