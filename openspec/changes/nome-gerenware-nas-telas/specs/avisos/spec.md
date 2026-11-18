## ADDED Requirements

### Requirement: Marca nos e-mails

O topo de todo e-mail MUST mostrar a logo GerenWare, como imagem com o nome no texto alternativo, quando a instalação tem o nome do produto e a aplicação sabe o endereço do site; nos outros casos, MUST mostrar o nome da instalação escrito. O rodapé MUST dizer que o e-mail é automático, que as respostas não são lidas e onde a pessoa muda os avisos (ADR-0036, ADR-0160).

#### Scenario: Logo no topo do e-mail
- **WHEN** um e-mail sai de uma instalação que se chama GerenWare, com o endereço do site configurado
- **THEN** o topo do e-mail traz a logo, servida pelo próprio site, com "GerenWare" como texto alternativo

#### Scenario: Nome da instalação no topo do e-mail
- **WHEN** um e-mail sai de uma instalação com outro nome
- **THEN** o topo do e-mail traz o nome da instalação escrito, sem imagem

#### Scenario: E-mail sem o endereço do site
- **WHEN** um e-mail sai sem o endereço do site configurado
- **THEN** o topo do e-mail traz o nome escrito, sem imagem

#### Scenario: Rodapé do e-mail
- **WHEN** qualquer e-mail sai
- **THEN** o rodapé diz que o e-mail foi enviado automaticamente, que as respostas não são lidas e que os avisos mudam em Meu perfil
