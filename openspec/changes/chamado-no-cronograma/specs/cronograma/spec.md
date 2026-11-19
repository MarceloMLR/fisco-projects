## ADDED Requirements

### Requirement: Chamados do item

Cada tarefa e marco do cronograma MUST trazer os chamados em aberto (nem resolvidos nem fechados) ligados a ele, com o número, o título e o estado do prazo, e cada marco não atingido MUST trazer também os chamados em aberto que o ameaçam pelas dependências, com o item ligado por onde passam; para todos que veem o cronograma. O chamado resolvido ou fechado MUST não aparecer, e nada disso MUST mudar as datas do cronograma (ADR-0043, ADR-0051, ADR-0164).

#### Scenario: Chamados ligados ao item
- **WHEN** os chamados #104 e #107, em aberto, estão ligados à tarefa Integrar o emissor de notas
- **THEN** o cronograma traz os dois chamados na tarefa, com o número, o título e o prazo

#### Scenario: Chamado que ameaça o marco
- **WHEN** o chamado #104 está ligado a Testes integrados, e o marco Go-live depende de Testes integrados
- **THEN** o marco Go-live traz o chamado #104 como ameaça, passando por Testes integrados

#### Scenario: Chamado resolvido não aparece
- **WHEN** o chamado ligado à tarefa é resolvido
- **THEN** o cronograma não o traz mais na tarefa nem nos marcos

#### Scenario: Leitor vê os chamados do item
- **WHEN** um leitor do projeto abre o cronograma de uma tarefa com um chamado ligado
- **THEN** ele vê o chamado na tarefa

### Requirement: Chamados do item pela tela

A Lista e o cartão do Quadro MUST mostrar, junto do nome, a marca dos chamados em aberto ligados ao item, por ícone e quantidade, com "2 chamados em aberto" no nome acessível. O detalhe de cada tarefa e marco MUST ter a seção "Chamados", com cada chamado (número, título e prazo, por ícone, cor e texto) levando à página dele, e, no marco, "Ameaçado por", com os chamados que o ameaçam e o item por onde passam. Sem chamados, a seção MUST não aparecer (ADR-0065, ADR-0155, ADR-0164).

#### Scenario: Marca na Lista e no Quadro
- **WHEN** uma tarefa tem dois chamados em aberto ligados
- **THEN** a Lista e o cartão do Quadro mostram a marca com 2, e o nome acessível diz "2 chamados em aberto"

#### Scenario: Seção Chamados no detalhe
- **WHEN** alguém abre o detalhe da tarefa com o chamado #104 ligado e escolhe o chamado
- **THEN** a seção "Chamados" mostra o número, o título e o prazo, e a escolha leva à página do chamado #104

#### Scenario: Ameaçado por no marco
- **WHEN** alguém abre o detalhe do marco Go-live, ameaçado pelo chamado #104 por Testes integrados
- **THEN** o detalhe mostra "Ameaçado por" com o chamado #104 e Testes integrados

#### Scenario: Item sem chamados
- **WHEN** alguém abre o detalhe de uma tarefa sem chamados ligados nem ameaças
- **THEN** o detalhe não mostra a seção "Chamados", e a Lista não mostra a marca
