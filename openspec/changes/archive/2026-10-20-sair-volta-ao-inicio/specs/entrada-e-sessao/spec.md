## MODIFIED Requirements

### Requirement: Página de entrada

Sem sessão, toda tela MUST levar à página de entrada, que pede o e-mail. Depois de entrar, a pessoa MUST voltar à tela que tinha pedido, menos depois de "Sair" e quando entra outra pessoa no lugar de quem teve a sessão expirada: nesses casos, MUST ir ao Início. O último e-mail usado MUST ficar lembrado no navegador.

#### Scenario: Tela aberta sem sessão
- **WHEN** alguém sem sessão abre uma tela, como Projetos
- **THEN** a página de entrada é exibida e, depois de entrar, a pessoa volta para Projetos

#### Scenario: Mesma pessoa depois da sessão expirada
- **WHEN** a sessão de Marina expira na página Chamados e ela entra de novo
- **THEN** a aplicação volta para Chamados

#### Scenario: Outra pessoa depois da sessão expirada
- **WHEN** a sessão de Marina expira na página Chamados e Diego entra no lugar dela
- **THEN** a aplicação mostra o Início

#### Scenario: E-mail lembrado
- **WHEN** a pessoa volta à página de entrada depois de ter entrado uma vez
- **THEN** o campo de e-mail já vem preenchido com o último e-mail usado

#### Scenario: Página de entrada com sessão
- **WHEN** alguém que já tem sessão abre a página de entrada
- **THEN** a aplicação mostra o Início

### Requirement: Sair

"Sair" MUST encerrar só a sessão do FiscoProjects e levar à página de entrada; a entrada seguinte MUST levar ao Início, seja quem for (ADR-0009).

#### Scenario: Sair
- **WHEN** a pessoa escolhe Sair
- **THEN** a sessão é encerrada, a página de entrada é exibida e as chamadas seguintes recebem 401

#### Scenario: Entrar depois de Sair
- **WHEN** Marina escolhe Sair na página Projetos e Diego entra em seguida
- **THEN** a aplicação mostra o Início, e não Projetos
