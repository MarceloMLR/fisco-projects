## MODIFIED Requirements

### Requirement: Linha do tempo sem licença de terceiros

A Linha do tempo MUST ser desenhada pela própria aplicação, sem componente pago nem chave de licença, completa e sem aviso de versão de avaliação em qualquer instalação; a instalação MUST NOT pedir, guardar nem entregar ao navegador uma chave de licença para ela (ADR-0134).

#### Scenario: Instalação sem chave de licença
- **WHEN** uma pessoa do projeto abre a Linha do tempo numa instalação sem a chave do Syncfusion
- **THEN** a Linha do tempo aparece completa, sem aviso de versão de avaliação

#### Scenario: Barra que volta na recusa
- **WHEN** o gerente arrasta uma tarefa e a API recusa a mudança
- **THEN** a barra volta para as datas de antes e a tela diz por quê

#### Scenario: Instalação que não guarda a chave
- **WHEN** quem opera monta ou atualiza uma instalação pelo guia e pelo Terraform, e o navegador pede a rota que entregava a chave
- **THEN** nada pede nem guarda no Key Vault uma chave do Syncfusion, e a rota responde 404
