# Tasks

## 1. Backend

- [x] 1.1 Criar o módulo `Calendar` e o `Contracts`, com:
  - `WorkCalendar`, `CalculatedHoliday` e `LocalHoliday`;
  - `INationalCalendar` com `BrazilianNationalCalendar`;
  - `BusinessDays`;
  - contexto, configurações, a migração `Initial` com os dados iniciais e o registro no host.

  Testar com `dotnet test`:
  - Páscoa e feriados móveis de 2024 a 2027;
  - Consciência Negra a partir de 2024;
  - os padrões do ADR-0083;
  - repetição dos feriados locais, inclusive 29/02;
  - somar e contar dias úteis;
  - ponto facultativo considerado;
  - feriado desconsiderado;
  - dias de trabalho, inclusive a recusa da semana vazia;
  - os testes de arquitetura passando com o módulo novo.
- [x] 1.2 Criar os casos de uso e a API `/api/calendar`, só do administrador:
  - calendário do ano;
  - dias de trabalho;
  - escolha de feriado calculado;
  - marcar e desmarcar várias datas de uma vez;
  - incluir, alterar e excluir feriado local.

  Testar com `dotnet test`:
  - a matriz de permissões (administrador, gerente de projeto, usuário e sem sessão);
  - os 400 e 404 de cada rota;
  - a escolha valendo em todos os anos;
  - "Marcar todos" com tudo ou nada;
  - a auditoria de cada mudança.

  Depois, atualizar o contrato com `npm run api:update`.

## 2. Frontend

- [x] 2.1 Criar a tela Calendário e feriados, como no canvas aprovado:
  - rota e item na Administração;
  - dias de trabalho e "Considerado" gravando na hora;
  - ano anterior e próximo;
  - filtro por tipo;
  - "Marcar todos" e "Desmarcar todos";
  - lista com linhas apagadas e rodapé;
  - versão do celular.

  Testar com `npm test` os cenários da tela: "Calendário e feriados na Administração", "Trocar o ano", "Filtrar por tipo", "Marcar considerado na lista", "Marcar todos", "Marcar todos de um tipo", "Desmarcar todos", o último dia de trabalho desabilitado e o erro ao salvar.
- [x] 2.2 Criar os diálogos "Novo feriado", "Editar feriado" e a confirmação de exclusão, e os textos da Auditoria para os três tipos de item, com o filtro.

  Testar com `npm test` e `npm run lint`:
  - "Novo feriado pela tela" e "Excluir com confirmação";
  - nome e data inválidos;
  - os títulos e valores traduzidos na Auditoria.

## 3. Verificação final

- [ ] 3.1 Conferir no navegador, com o build de produção, nos temas claro e escuro, no computador e no celular:
  - a tela;
  - os diálogos;
  - a troca de ano;
  - a Auditoria.

  Mandar as capturas para Marcelo.
- [ ] 3.2 Documentação e verificações:
  - glossário, `CLAUDE.md` (módulo `Calendar`) e roadmap;
  - rodar todos os scripts de `scripts/ci` sem erro;
  - abrir o PR e confirmar que a verificação `verify` passa no GitHub Actions.

## Workflow follow-up

- Marcar a Projetos-1 como "PR aberto" no roadmap.
- Depois da aprovação de Marcelo:
  - `/opsx:archive` na mesma branch;
  - ADR-0083 como aceito;
  - roadmap como "Concluída";
  - merge.
