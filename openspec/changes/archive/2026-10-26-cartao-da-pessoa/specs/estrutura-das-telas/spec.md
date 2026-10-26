## ADDED Requirements

### Requirement: Cartão da pessoa

O avatar de outra pessoa MUST abrir, pelo clique ou pelo teclado, o cartão dela junto do avatar: o avatar, o nome, o papel naquela tela quando a tela o conhece, o e-mail e "Copiar e-mail", que copia e confirma no alto da tela. Da pessoa anonimizada, o cartão MUST dizer que o nome e o e-mail foram removidos. Abrir o cartão numa linha de tabela MUST NOT escolher a linha (ADR-0118).

#### Scenario: Cartão pelo avatar
- **WHEN** alguém clica no avatar de Diego Torres numa mensagem do chamado
- **THEN** abre o cartão com o avatar, o nome, o papel dele no chamado e o e-mail

#### Scenario: Copiar o e-mail do cartão
- **WHEN** a pessoa escolhe "Copiar e-mail" no cartão
- **THEN** o e-mail vai para a área de transferência e a tela confirma "E-mail copiado."

#### Scenario: Cartão numa linha de tabela
- **WHEN** alguém abre o cartão pelo avatar numa linha de tabela que abre um item
- **THEN** só o cartão abre, sem abrir o item da linha

#### Scenario: Pessoa anonimizada no cartão
- **WHEN** alguém abre o cartão de uma pessoa anonimizada
- **THEN** o cartão mostra o identificador anônimo e diz que o nome e o e-mail foram removidos, sem "Copiar e-mail"
