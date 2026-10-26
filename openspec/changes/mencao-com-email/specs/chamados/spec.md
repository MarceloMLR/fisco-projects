## MODIFIED Requirements

### Requirement: Mencionar ao digitar

No editor dos chamados, digitar "@" e parte do nome MUST mostrar, junto do texto, as pessoas que podem ser mencionadas, as mesmas do botão "@", pelo começo do nome ou de uma palavra dele, sem acento, até 8. Nas duas listas, cada pessoa MUST aparecer com o nome e, embaixo, o e-mail. As setas MUST marcar uma pessoa, Enter, Tab e o clique MUST trocar o texto digitado pela menção, e Esc MUST fechar a lista (ADR-0107).

#### Scenario: Mencionar ao digitar
- **WHEN** quem atende digita "@die" na resposta, aperta Enter, digita "@R", clica em Rafael Souza e envia
- **THEN** a mensagem vai com as menções de Diego Torres e de Rafael Souza, sem o texto digitado depois do "@"

#### Scenario: Pessoas pelo começo do nome
- **WHEN** alguém digita "@joao", "@cos" ou "@Diego T"
- **THEN** a lista mostra João Pereira, Marina Costa ou Diego Torres, e nada com um texto que não começa nenhum nome

#### Scenario: Pessoas de mesmo nome na menção
- **WHEN** duas pessoas chamadas Marina Costa podem ser mencionadas e alguém digita "@mar" ou abre o botão "@"
- **THEN** cada uma aparece com o próprio e-mail embaixo do nome, e a escolhida é a mencionada
