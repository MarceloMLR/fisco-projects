# Design: dados-de-demonstracao (Cronograma-3.1)

## Context

- A entrada local (ADR-0078) aceita, sem senha e só fora da produção, o e-mail de uma pessoa do diretório de demonstração; na primeira entrada a pessoa vira usuária.
- As rotas da API já fazem tudo o que a demonstração precisa: papéis, clientes, projetos, membros, fases, tarefas, marcos, progresso, marcos atingidos e status.
- O frontend tem os tipos do contrato (`src/api/schema.ts`) e o cliente `openapi-fetch`.

## Goals / Non-Goals

**Goals:**

- Um comando que carrega os mesmos dados das capturas em qualquer instalação de desenvolvimento.
- Os dados passam pelas regras e permissões da API.
- Uma mudança do contrato que quebre o comando reprova a checagem de tipos.

**Non-Goals:**

- Apagar ou recarregar os dados, e dados de telas que ainda não existem.

## Decisions

### 1. Comando em TypeScript no frontend, pela API (ADR-0089)

- `web/scripts/demo/plan.ts` descreve os dados: pessoas, clientes, projetos e cronogramas, com as datas em dias a partir do primeiro dia do projeto principal. É código puro, sem rede.
- `web/scripts/demo/load.ts` carrega o plano pela API, com um `openapi-fetch` tipado pelo contrato para cada pessoa. Cada cliente guarda o cookie da sessão da pessoa e manda o `X-CSRF`.
- `web/scripts/demo/main.ts` lê o endereço da API (padrão `http://localhost:5175`), roda a carga e escreve o resumo.
- O Node 24 roda o TypeScript direto, tirando os tipos (`node scripts/demo/main.ts`), sem passo de build; por isso o código não usa `enum` nem `namespace`.
- `tsconfig.node.json` passa a incluir `scripts`, com os tipos do Node (`@types/node` da linha 24), para o lint e a checagem de tipos valerem para o comando.
- Alternativas: script SQL ou carga na subida da API. Foram rejeitadas no ADR-0089.

### 2. Quem faz o quê

- Cada pessoa entra pela entrada local, e o id dela vem de `GET /api/session`.
- Paulo, o primeiro administrador, dá o papel de gerente de projeto a Marina e Beatriz, cria os clientes e os projetos e inclui os membros.
- Marina monta os cronogramas e muda os status. Cada responsável atualiza o progresso da própria tarefa; sem responsável, Marina atualiza.
- Lucas Prado e Sofia Martins não entram, para continuarem como quem ainda não entrou (ADR-0078).

### 3. Datas relativas ao dia em que roda

- O dia vem de `GET /api/projects` (`today`, no fuso da instalação), e o primeiro dia do ERP-FIN é a segunda-feira de duas semanas antes da semana desse dia.
- Cada data do plano é esse dia mais um número de dias; o servidor leva cada início a um dia útil e calcula os fins (ADR-0018).
- Em 19/10/2026, o plano reproduz as datas das capturas da Cronograma-3.

### 4. Sem repetir e sem produção

- Antes de qualquer mudança, o comando consulta `GET /api/session/sign-in-options`. Sem entrada local, ele para com uma mensagem.
- Se `GET /api/projects?search=ERP-FIN` encontra o projeto, o comando avisa que os dados já estão carregados e termina sem mudar nada.

### 5. Testes

- `plan.test.ts` confere as datas relativas.
- `load.test.ts` roda a carga no ambiente `node` do Vitest contra a API simulada pelo MSW. Ele confere quem faz cada ação, a parada sem entrada local e a parada com os dados já lá.

## Risks / Trade-offs

- [O plano envelhece se as telas novas não acrescentarem dados] → O `CLAUDE.md` passa a pedir que cada change atualize o plano.
- [Uma falha no meio deixa a carga pela metade] → O comando diz onde parou; recriar o banco local (`docker compose down -v`) e rodar de novo resolve.

## Migration Plan

Sem migração. Quem já tem o banco local roda `npm run demo:data` com a API no ar.
