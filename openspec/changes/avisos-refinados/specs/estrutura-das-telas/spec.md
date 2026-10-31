## MODIFIED Requirements

### Requirement: Confirmações no alto da tela

Toda confirmação curta MUST aparecer no alto, no meio: no computador, na faixa do cabeçalho e no meio da área de conteúdo, sem cobrir a barra lateral; no celular, logo abaixo da barra do topo; fora da estrutura das telas, no alto, no meio (ADR-0109). O aviso MUST ser um só para todas as telas, um de cada vez: um aviso novo MUST mandar embora o que está na tela antes de entrar, e o aviso MUST continuar na tela quando a tela que o deu fecha ou muda. Ele MUST ter o fundo de janela, a borda, a sombra de menu e o ícone de concluído, ou o ícone de erro quando diz uma falha, do tamanho do texto, até 420px, nos dois temas. Ele MUST entrar descendo 8px e esmaecendo em 200 ms e sair esmaecendo em 150 ms, com o texto; com "reduzir movimento" ligado no sistema, MUST só esmaecer, em 150 ms. Ele MUST ficar 5 segundos, 8 quando tem uma ação, parado enquanto o ponteiro ou o foco está nele, e MUST ter o botão "Fechar aviso" (ADR-0133). Nenhuma tela MUST ter o próprio aviso nem escolher a posição dele.

#### Scenario: Confirmação no alto, no meio
- **WHEN** uma tela confirma algo que acabou de dar certo
- **THEN** a confirmação aparece no alto, no meio da área de conteúdo, abaixo da barra do topo no celular, com o ícone de concluído

#### Scenario: Toda confirmação no mesmo lugar
- **WHEN** qualquer tela mostra uma confirmação
- **THEN** ela usa o aviso da estrutura, sem um aviso próprio

#### Scenario: Aviso de uma falha
- **WHEN** uma tela avisa que algo não deu certo
- **THEN** o aviso aparece no mesmo lugar, com o ícone de erro

#### Scenario: Um aviso de cada vez
- **WHEN** chega um aviso com outro na tela
- **THEN** o da tela sai e o novo entra depois, sem os dois juntos

#### Scenario: Aviso depois de mudar de página
- **WHEN** a pessoa exclui um projeto ou abre um chamado e a tela muda
- **THEN** a confirmação aparece na tela seguinte

#### Scenario: Tempo do aviso
- **WHEN** um aviso aparece
- **THEN** ele sai sozinho depois de 5 segundos, ou de 8 quando tem uma ação

#### Scenario: Aviso parado com o ponteiro
- **WHEN** o ponteiro ou o foco está no aviso
- **THEN** ele não sai até o ponteiro ou o foco sair dele, e então fica o tempo que faltava

#### Scenario: Fechar o aviso
- **WHEN** a pessoa escolhe "Fechar aviso"
- **THEN** o aviso sai na hora

#### Scenario: Aviso com movimento reduzido
- **WHEN** a pessoa pediu ao sistema para reduzir o movimento
- **THEN** o aviso só esmaece ao entrar e ao sair, sem descer
