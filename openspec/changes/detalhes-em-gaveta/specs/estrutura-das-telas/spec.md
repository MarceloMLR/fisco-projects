## ADDED Requirements

### Requirement: Detalhes em gaveta sobreposta

Ao escolher um item de uma lista, com um clique ou com Enter, a tela MUST abrir os detalhes numa gaveta à direita, por cima da tela, com o fundo escurecido, e a lista MUST continuar com toda a largura. A gaveta MUST ter até 640 px de largura e, no celular, a largura da tela. Ela MUST fechar pelo botão Fechar, pela tecla Esc ou por um clique fora dela, e o foco MUST voltar para a lista.

#### Scenario: Detalhes abertos por cima da lista
- **WHEN** a pessoa escolhe um item de uma lista
- **THEN** os detalhes aparecem numa gaveta à direita, por cima da tela, com o fundo escurecido, e a lista não perde largura

#### Scenario: Fechar os detalhes
- **WHEN** a gaveta está aberta e a pessoa aperta Esc, clica fora dela ou no botão Fechar
- **THEN** a gaveta fecha e o foco volta para a lista

#### Scenario: Detalhes no celular
- **WHEN** a pessoa escolhe um item de uma lista no celular
- **THEN** a gaveta ocupa a largura da tela
