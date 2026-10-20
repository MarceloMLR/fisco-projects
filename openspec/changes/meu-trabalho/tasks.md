## 1. Decisão

- [x] 1.1 ADR-0096: fila montada no servidor pelo módulo `MyWork`, janela de 7 dias corridos e atraso do projeto pelo item mais atrasado

## 2. Backend

- [x] 2.1 Módulo `MyWork`: projetos, contrato `IMyWorkSource`, regra da fila (grupos, ações e ordem), `GET /api/my-work` e registro no host, com testes
- [x] 2.2 Fonte dos chamados (`Tickets`): as visões, o prazo da primeira resposta ou da resolução e o `CanAssign`, com testes
- [x] 2.3 Fonte das tarefas (`Projects`) e dos pedidos de aceite (`Documents`), com testes
- [x] 2.4 Projetos: o atraso na lista e os próximos marcos com o aceite pedido, com testes
- [x] 2.5 Testes da API: as visões, só itens da pessoa e dos projetos que ela vê, sem sessão

## 3. Frontend

- [ ] 3.1 Página Meu trabalho: a data, as visões, a fila com os grupos, as linhas e os textos
- [ ] 3.2 Ações pela fila: Concluir, Iniciar e Atualizar, Atribuir, e os links; o `?item=` do cronograma
- [ ] 3.3 Meus projetos e Próximos marcos, as mensagens das seções vazias e o celular
- [ ] 3.4 Testes das telas

## 4. Demonstração e documentação

- [ ] 4.1 Itens da fila nos dados de demonstração, com o teste
- [ ] 4.2 `CLAUDE.md`, glossário e roadmap
