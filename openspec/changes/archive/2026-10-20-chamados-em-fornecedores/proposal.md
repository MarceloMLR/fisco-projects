# Proposta: chamados em fornecedores (Chamados-6)

## Why

Quando o problema depende de um fornecedor, quem atende hoje anota o chamado aberto lá numa mensagem, sem status nem destaque. O ADR-0028 decidiu um registro manual com destaque para o que está parado:
- **Cadastro:** a Administração cadastra os fornecedores, com nome, endereço do portal de suporte opcional e situação.
- **Registro no chamado:** o nosso chamado registra um ou mais chamados abertos em fornecedores, de fornecedores diferentes, com o número, a data de abertura, o status, o link e a observação.
- **Status do lado do fornecedor:** Aberto, Em análise, Aguardando nosso retorno, Resolvido e Fechado. Cada atualização tem anotação e vira evento na linha do tempo.
- **Na tela:**
  - o registro aparece no chamado, na lista e no filtro "Com fornecedor";
  - o que está sem atualização há 5 dias úteis, valor configurável, ganha destaque.
- **Prazo:** o nosso prazo continua correndo enquanto o fornecedor analisa.

## What Changes

- **Fornecedores** (`/api/admin/vendors`):
  - o administrador inclui, edita, desativa e reativa os fornecedores (nome único, portal opcional em http ou https);
  - o fornecedor inativo não aparece para novos registros;
  - a mudança entra na auditoria;
  - a tela "Fornecedores" na Administração é desenhada como "Modelos de projeto" e "Clientes".
- **Chamado no fornecedor** (`POST /api/tickets/{number}/vendor-tickets`):
  - quem atende o chamado (o administrador, os gestores e quem atende o time) registra o fornecedor, o número, a data de abertura, o status, o link e a observação;
  - um chamado aceita vários registros, de fornecedores diferentes;
  - o registro e cada atualização entram na conversa como eventos.
- **Atualizar** (`PUT /api/tickets/{number}/vendor-tickets/{id}`): quem atende muda o status, com uma anotação opcional que vai para o evento.
- **Parado:** um registro em aberto no fornecedor e sem atualização há `Tickets:VendorStaleBusinessDays` dias úteis (5) é marcado com os dias úteis sem retorno.
- **Na tela:**
  - a seção "No fornecedor" do painel do chamado, como no canvas, com "Registrar" e "Atualizar";
  - a visão "Com fornecedor" na página Chamados, com a contagem;
  - o fornecedor na linha de cada chamado da lista.
- **Demonstração:** dois fornecedores fictícios e um chamado registrado no fornecedor e atualizado.

## Capabilities

### New Capabilities

Nenhuma.

### Modified Capabilities

- `chamados`: fornecedores, chamados no fornecedor, destaque do que está parado e a visão "Com fornecedor".
- `dados-de-demonstracao`: fornecedores na demonstração.

## Impact

- **Backend:** módulo `Tickets`, com as tabelas `vendors` e `vendor_tickets`, os eventos novos e a migração.
- **Frontend:** página do chamado, lista e a nova página da Administração.
- **Fora do escopo:**
  - pausar o nosso prazo enquanto aguarda o fornecedor;
  - integração automática com o sistema do fornecedor (ADR-0028);
  - a trilha de etapas, que ainda não existe na página do chamado.
